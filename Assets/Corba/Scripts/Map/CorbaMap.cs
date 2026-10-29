using System.Collections.Generic;
using UnityEngine;

namespace Corba
{
    public enum StationKind { DefenderCauldron, AttackerCauldron, Ocak, Sofra }

    public class Station
    {
        public StationKind Kind;
        public SoupType Soup;
        public Vector3 Position;
        public int Index;
        public Transform Surface;
    }

    public class BuildPad
    {
        public int Index;
        public float RoadDistance;
        public Vector3 TowerPosition;
        public Vector3 TrapPosition;
        public Quaternion TrapRotation;
        public int Side;
        public bool HighGround;
        public Vector2Int Cell;
        public Transform Marker;
    }

    public class Bump
    {
        public Vector3 Position;
        public float Radius;
        public bool Mud;
    }

    public enum TileType : byte { Grass, Road, Water, Bridge, Plaza, Pad, Stairs }

    public class Tile
    {
        public TileType Type;
        public int Tier;
        // Slope: -1 none, else 0..3 = direction the slope rises toward (+X, +Z, -X, -Z).
        public int SlopeUp = -1;
        public bool Blocked;
    }

    public class CorbaMap : MonoBehaviour
    {
        public static CorbaMap Instance { get; private set; }

        public const int W = 22;
        public const int H = 14;
        public const float T = 3.2f;
        public const float TierHeight = 1.25f;
        public const float HalfX = W * T * 0.5f;
        public const float HalfZ = H * T * 0.5f;
        const float WaterDepth = 0.9f;

        static readonly Vector2Int[] Dirs = { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1) };

        public int Seed { get; private set; } = -1;
        public float RoadLength { get; private set; }
        public readonly List<Station> Stations = new List<Station>();
        public readonly List<BuildPad> Pads = new List<BuildPad>();
        public readonly List<Bump> Bumps = new List<Bump>();
        public readonly List<Vector2Int> RoadCells = new List<Vector2Int>();
        public Vector3 AttackerSpawn { get; private set; }
        public Vector3 DefenderSpawn { get; private set; }
        public Tile[,] Tiles { get; private set; }
        public Vector2Int OcakCell { get; private set; }
        public Vector2Int SofraCell { get; private set; }
        public Vector2Int KitchenCell { get; private set; }
        public Vector2Int MarketCell { get; private set; }

        readonly List<Vector3> roadPoints = new List<Vector3>();
        readonly List<float> roadDist = new List<float>();
        public IReadOnlyList<Vector3> RoadPoints => roadPoints;
        NavGrid nav;
        public NavGrid Nav => nav ??= new NavGrid(this);
        public IReadOnlyList<float> RoadDistances => roadDist;
        Transform root;
        System.Random rng;

        public event System.Action Rebuilt;

        void Awake() => Instance = this;

        #region Public queries

        public Vector3 CellCenter(Vector2Int c) => new Vector3((c.x + 0.5f) * T - HalfX, 0f, (c.y + 0.5f) * T - HalfZ);

        public Vector2Int CellOf(Vector3 p) => new Vector2Int(Mathf.Clamp(Mathf.FloorToInt((p.x + HalfX) / T), 0, W - 1), Mathf.Clamp(Mathf.FloorToInt((p.z + HalfZ) / T), 0, H - 1));

        public bool InGrid(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < W && c.y < H;

        public Tile At(Vector2Int c) => Tiles[c.x, c.y];

        public float Height(float x, float z)
        {
            if (Tiles == null) return 0f;
            var c = CellOf(new Vector3(x, 0, z));
            var t = At(c);
            if (t.Type == TileType.Water) return -WaterDepth;
            float baseH = t.Tier * TierHeight;
            if (t.SlopeUp < 0) return baseH;
            Vector3 center = CellCenter(c);
            Vector2 d = Dirs[t.SlopeUp];
            float along = ((x - center.x) * d.x + (z - center.z) * d.y) / T + 0.5f;
            return baseH - TierHeight * (1f - Mathf.Clamp01(along));
        }

        public float Height(Vector3 p) => Height(p.x, p.z);

        public Vector3 SampleRoad(float distance, out Vector3 tangent)
        {
            distance = Mathf.Clamp(distance, 0f, RoadLength);
            int lo = 0, hi = roadDist.Count - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) >> 1;
                if (roadDist[mid] <= distance) lo = mid; else hi = mid;
            }
            float span = Mathf.Max(roadDist[hi] - roadDist[lo], 1e-4f);
            float t = (distance - roadDist[lo]) / span;
            tangent = roadPoints[hi] - roadPoints[lo];
            tangent.y = 0f;
            tangent.Normalize();
            return Vector3.Lerp(roadPoints[lo], roadPoints[hi], t);
        }

        public Vector3 SampleRoad(float distance, float lateral)
        {
            Vector3 p = SampleRoad(distance, out var tan);
            return p + new Vector3(tan.z, 0f, -tan.x) * lateral;
        }

        public float DistanceToRoad(Vector3 pos, out float roadDistance)
        {
            float best = float.MaxValue;
            roadDistance = 0f;
            Vector2 q = new Vector2(pos.x, pos.z);
            for (int i = 0; i < roadPoints.Count - 1; i++)
            {
                Vector2 a = new Vector2(roadPoints[i].x, roadPoints[i].z);
                Vector2 b = new Vector2(roadPoints[i + 1].x, roadPoints[i + 1].z);
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-5f));
                float d = (a + ab * t - q).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    roadDistance = Mathf.Lerp(roadDist[i], roadDist[i + 1], t);
                }
            }
            return Mathf.Sqrt(best);
        }

        public static float FlatDist(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public Transform OcakObject { get; private set; }
        public Transform SofraObject { get; private set; }
        public Station Ocak => Stations.Find(s => s.Kind == StationKind.Ocak);
        public Station Sofra => Stations.Find(s => s.Kind == StationKind.Sofra);

        public bool Walkable(Vector2Int c) => InGrid(c) && At(c).Type != TileType.Water && !At(c).Blocked;

        /// <summary>Can a cook step from cell a to the adjacent cell b without climbing a cliff?</summary>
        public bool CanStep(Vector2Int a, Vector2Int b)
        {
            if (!Walkable(a) || !Walkable(b)) return false;
            int dir = DirIndex(b - a);
            return Mathf.Abs(EdgeHeight(a, dir) - EdgeHeight(b, (dir + 2) % 4)) < 0.3f;
        }

        public bool CanDrop(Vector2Int a, Vector2Int b)
        {
            if (!Walkable(a) || !Walkable(b)) return false;
            int dir = DirIndex(b - a);
            return EdgeHeight(a, dir) > EdgeHeight(b, (dir + 2) % 4) + 0.3f;
        }

        static int DirIndex(Vector2Int d)
        {
            for (int i = 0; i < 4; i++) if (Dirs[i] == d) return i;
            return 0;
        }

        float EdgeHeight(Vector2Int c, int dir)
        {
            var t = At(c);
            if (t.Type == TileType.Water) return -WaterDepth;
            float top = t.Tier * TierHeight;
            if (t.SlopeUp < 0) return top;
            if (dir == t.SlopeUp) return top;
            if (dir == (t.SlopeUp + 2) % 4) return top - TierHeight;
            return top - TierHeight * 0.5f;
        }

        /// <summary>Walkable A* over cells. Drops off ledges are allowed one way, as a cook can jump down.</summary>
        public List<Vector3> FindPath(Vector3 from, Vector3 to)
        {
            var start = CellOf(from);
            var goal = CellOf(to);
            var open = new List<Vector2Int> { start };
            var came = new Dictionary<Vector2Int, Vector2Int>();
            var g = new Dictionary<Vector2Int, float> { [start] = 0f };
            var closed = new HashSet<Vector2Int>();
            while (open.Count > 0)
            {
                int bi = 0;
                float bf = float.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    float f = g[open[i]] + Vector2Int.Distance(open[i], goal);
                    if (f < bf) { bf = f; bi = i; }
                }
                var cur = open[bi];
                open.RemoveAt(bi);
                if (cur == goal) break;
                closed.Add(cur);
                foreach (var d in Dirs)
                {
                    var n = cur + d;
                    if (!InGrid(n) || closed.Contains(n)) continue;
                    bool step = CanStep(cur, n);
                    bool drop = !step && CanDrop(cur, n);
                    if (!step && !drop) continue;
                    var tile = At(n);
                    float cost = 1f + (tile.Type == TileType.Road || tile.Type == TileType.Bridge ? -0.3f : 0f) + (drop ? 2.5f : 0f);
                    float ng = g[cur] + cost;
                    if (g.TryGetValue(n, out var old) && old <= ng) continue;
                    g[n] = ng;
                    came[n] = cur;
                    if (!open.Contains(n)) open.Add(n);
                }
            }
            var path = new List<Vector3>();
            if (!came.ContainsKey(goal) && start != goal) return path;
            var c = goal;
            path.Add(to);
            while (c != start)
            {
                c = came[c];
                if (c != start) path.Add(CellCenter(c));
            }
            path.Reverse();
            for (int i = 0; i < path.Count; i++) path[i] = new Vector3(path[i].x, Height(path[i]), path[i].z);
            return path;
        }

        #endregion

        public void Build(int seed)
        {
            if (seed == Seed && root != null) return;
            Seed = seed;
            // Props can still wall off a region the tile check thought was open, so the nav grid has the final word.
            // Rerolls are deterministic, so every peer lands on the same layout.
            for (int layout = 0; layout < 12; layout++)
            {
                BuildLayout(seed + layout * 1000003);
                if (Nav.AllConnected(KeyPoints())) break;
                if (layout == 11) Debug.LogWarning($"[Corba] map {seed}: no fully connected layout found, using the last one");
            }
            Rebuilt?.Invoke();
        }

        List<Vector3> KeyPoints()
        {
            var pts = new List<Vector3> { AttackerSpawn, DefenderSpawn, CellCenter(MarketCell), CellCenter(KitchenCell) };
            foreach (var s in Stations) pts.Add(s.Position);
            foreach (var p in Pads) pts.Add(p.TowerPosition);
            return pts;
        }

        void BuildLayout(int layoutSeed)
        {
            if (root != null) DestroyImmediate(root.gameObject);
            nav = null;
            root = new GameObject("Map_" + layoutSeed).transform;
            root.SetParent(transform, false);
            rng = new System.Random(layoutSeed);

            for (int attempt = 0; attempt < 20; attempt++)
            {
                GenerateTiers();
                if (CarveRoad()) break;
            }
            PlacePlazas();
            PlacePads();
            EnsureConnectivity();
            BuildRoadCurve();
            PlaceStations();

            var builder = new TerrainMeshBuilder(this);
            builder.Build(root);
            DecorateRoad();
            PlaceBumpsAndDecor();
            BuildLabels();
        }

        float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        #region Generation

        void GenerateTiers()
        {
            Tiles = new Tile[W, H];
            float ox = Rand(0, 500), oz = Rand(0, 500);
            var raw = new float[W, H];
            for (int x = 0; x < W; x++)
            for (int z = 0; z < H; z++)
            {
                float n = Mathf.PerlinNoise(ox + x * 0.16f, oz + z * 0.16f) * 0.75f + Mathf.PerlinNoise(ox * 2 + x * 0.35f, oz + z * 0.35f) * 0.25f;
                raw[x, z] = n;
            }
            for (int x = 0; x < W; x++)
            for (int z = 0; z < H; z++)
            {
                int tier = raw[x, z] < 0.42f ? 0 : raw[x, z] < 0.6f ? 1 : 2;
                Tiles[x, z] = new Tile { Tier = tier, Blocked = x == 0 || z == 0 || x == W - 1 || z == H - 1 };
            }
            for (int pass = 0; pass < 2; pass++)
            {
                var copy = new int[W, H];
                for (int x = 0; x < W; x++)
                for (int z = 0; z < H; z++)
                {
                    var counts = new int[3];
                    for (int dx = -1; dx <= 1; dx++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        var c = new Vector2Int(x + dx, z + dz);
                        if (InGrid(c)) counts[Tiles[c.x, c.y].Tier]++;
                    }
                    int best = Tiles[x, z].Tier;
                    for (int t = 0; t < 3; t++) if (counts[t] >= 5) best = t;
                    copy[x, z] = best;
                }
                for (int x = 0; x < W; x++)
                for (int z = 0; z < H; z++)
                    Tiles[x, z].Tier = copy[x, z];
            }
            for (int x = 0; x < W; x++)
            for (int z = 0; z < H; z++)
                if (Tiles[x, z].Blocked) Tiles[x, z].Tier = Mathf.Max(Tiles[x, z].Tier, 1);

            if (rng.NextDouble() < 0.75)
            {
                int rx = W / 2 + rng.Next(-3, 4);
                for (int z = 0; z < H; z++)
                {
                    Tiles[rx, z] = new Tile { Type = TileType.Water, Tier = 0 };
                    if (z < H - 1 && rng.NextDouble() < 0.35)
                    {
                        int nx = Mathf.Clamp(rx + (rng.Next(2) == 0 ? -1 : 1), 4, W - 5);
                        Tiles[nx, z] = new Tile { Type = TileType.Water, Tier = 0 };
                        rx = nx;
                    }
                }
            }
        }

        bool CarveRoad()
        {
            RoadCells.Clear();
            int startRow = rng.Next(2, H - 2);
            int endRow = rng.Next(2, H - 2);
            var start = new Vector2Int(1, startRow);
            var end = new Vector2Int(W - 2, endRow);
            var waypoints = new List<Vector2Int> { start };
            int wpCount = 2;
            for (int i = 1; i <= wpCount; i++)
            {
                int x = Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, i / (float)(wpCount + 1)));
                int row = i % 2 == 1 ? (startRow < H / 2 ? rng.Next(H - 4, H - 2) : rng.Next(2, 4)) : (startRow < H / 2 ? rng.Next(2, 4) : rng.Next(H - 4, H - 2));
                waypoints.Add(new Vector2Int(x, row));
            }
            waypoints.Add(end);

            var used = new HashSet<Vector2Int>();
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                var leg = RoadAStar(waypoints[i], waypoints[i + 1], used);
                if (leg == null) return false;
                foreach (var c in leg)
                {
                    if (RoadCells.Count > 0 && RoadCells[RoadCells.Count - 1] == c) continue;
                    RoadCells.Add(c);
                    used.Add(c);
                }
            }
            RoadCells.Insert(0, new Vector2Int(0, startRow));
            RoadCells.Add(new Vector2Int(W - 1, endRow));
            if (RoadCells.Count < 34) return false;

            int current = Tiles[RoadCells[0].x, RoadCells[0].y].Type == TileType.Water ? 0 : Tiles[RoadCells[0].x, RoadCells[0].y].Tier;
            int lastChange = -10;
            for (int i = 0; i < RoadCells.Count; i++)
            {
                var c = RoadCells[i];
                var tile = Tiles[c.x, c.y];
                bool water = tile.Type == TileType.Water;
                int desired = water ? current : tile.Tier;
                bool straight = i > 0 && i < RoadCells.Count - 1 && (RoadCells[i + 1] - c) == (c - RoadCells[i - 1]);
                int slope = -1;
                int tier = current;
                if (!water && desired != current && straight && i - lastChange >= 3)
                {
                    int dir = DirIndex(RoadCells[i + 1] - c);
                    if (desired > current)
                    {
                        tier = current + 1;
                        slope = dir;
                    }
                    else
                    {
                        tier = current;
                        slope = (dir + 2) % 4;
                        current -= 1;
                        Tiles[c.x, c.y] = new Tile { Type = TileType.Road, Tier = tier, SlopeUp = slope };
                        lastChange = i;
                        continue;
                    }
                    current = tier;
                    lastChange = i;
                }
                Tiles[c.x, c.y] = new Tile { Type = water ? TileType.Bridge : TileType.Road, Tier = tier, SlopeUp = slope };
            }
            for (int i = 0; i < RoadCells.Count - 1; i++)
                if (!CanStepRoad(RoadCells[i], RoadCells[i + 1])) return false;
            return true;
        }

        bool CanStepRoad(Vector2Int a, Vector2Int b)
        {
            int dir = DirIndex(b - a);
            return Mathf.Abs(EdgeHeight(a, dir) - EdgeHeight(b, (dir + 2) % 4)) < 0.3f;
        }

        List<Vector2Int> RoadAStar(Vector2Int from, Vector2Int to, HashSet<Vector2Int> used)
        {
            var noiseOff = Rand(0, 100);
            var open = new List<Vector2Int> { from };
            var came = new Dictionary<Vector2Int, Vector2Int>();
            var g = new Dictionary<Vector2Int, float> { [from] = 0 };
            var closed = new HashSet<Vector2Int>();
            while (open.Count > 0)
            {
                int bi = 0;
                float bf = float.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    float f = g[open[i]] + (Mathf.Abs(open[i].x - to.x) + Mathf.Abs(open[i].y - to.y));
                    if (f < bf) { bf = f; bi = i; }
                }
                var cur = open[bi];
                open.RemoveAt(bi);
                if (cur == to)
                {
                    var path = new List<Vector2Int> { cur };
                    while (came.TryGetValue(cur, out var p)) { cur = p; path.Add(cur); }
                    path.Reverse();
                    return path;
                }
                closed.Add(cur);
                foreach (var d in Dirs)
                {
                    var n = cur + d;
                    if (n.x < 1 || n.y < 1 || n.x > W - 2 || n.y > H - 2 || closed.Contains(n)) continue;
                    if (used.Contains(n) && n != to) continue;
                    float cost = 1f + Mathf.PerlinNoise(noiseOff + n.x * 0.4f, n.y * 0.4f) * 2.5f;
                    foreach (var dd in Dirs)
                        if (used.Contains(n + dd) && n + dd != cur && n != to) cost += 6f;
                    var ta = Tiles[cur.x, cur.y];
                    var tb = Tiles[n.x, n.y];
                    if (tb.Type == TileType.Water) cost += ta.Type == TileType.Water ? 30f : 4f;
                    else if (ta.Type != TileType.Water && ta.Tier != tb.Tier) cost += 3f;
                    if (came.TryGetValue(cur, out var prev) && (cur - prev) != d) cost += 0.6f;
                    float ng = g[cur] + cost;
                    if (g.TryGetValue(n, out var old) && old <= ng) continue;
                    g[n] = ng;
                    came[n] = cur;
                    if (!open.Contains(n)) open.Add(n);
                }
            }
            return null;
        }

        void Flatten(Vector2Int center, int rx, int rz, int tier, TileType type)
        {
            for (int x = center.x - rx; x <= center.x + rx; x++)
            for (int z = center.y - rz; z <= center.y + rz; z++)
            {
                var c = new Vector2Int(x, z);
                if (!InGrid(c)) continue;
                var t = Tiles[x, z];
                if (t.Type == TileType.Road || t.Type == TileType.Bridge) continue;
                Tiles[x, z] = new Tile { Type = type, Tier = tier };
            }
        }

        void PlacePlazas()
        {
            var first = RoadCells[0];
            var last = RoadCells[RoadCells.Count - 1];
            OcakCell = first;
            SofraCell = last;
            int ocakTier = Tiles[RoadCells[1].x, RoadCells[1].y].Tier;
            int sofraTier = Tiles[RoadCells[RoadCells.Count - 2].x, RoadCells[RoadCells.Count - 2].y].Tier;
            Tiles[first.x, first.y] = new Tile { Type = TileType.Plaza, Tier = ocakTier };
            Tiles[last.x, last.y] = new Tile { Type = TileType.Plaza, Tier = sofraTier };
            Flatten(first, 0, 1, ocakTier, TileType.Plaza);
            Flatten(last, 0, 1, sofraTier, TileType.Plaza);

            KitchenCell = FindPlazaSpot(last, 2, 4, sofraTier, 3);
            Flatten(KitchenCell, 1, 1, sofraTier, TileType.Plaza);

            int third = RoadCells.Count / 3;
            MarketCell = FindPlazaSpot(RoadCells[third], 3, 5, Tiles[RoadCells[third].x, RoadCells[third].y].Tier, 4);
            Flatten(MarketCell, 1, 1, Tiles[MarketCell.x, MarketCell.y].Type == TileType.Water ? 0 : Tiles[MarketCell.x, MarketCell.y].Tier, TileType.Plaza);
        }

        Vector2Int FindPlazaSpot(Vector2Int near, int minDist, int maxDist, int preferTier, int margin)
        {
            Vector2Int best = near;
            float bestScore = float.MaxValue;
            for (int x = 2; x < W - 2; x++)
            for (int z = 2; z < H - 2; z++)
            {
                var c = new Vector2Int(x, z);
                float d = Vector2Int.Distance(c, near);
                if (d < minDist || d > maxDist) continue;
                bool clash = false;
                for (int dx = -1; dx <= 1 && !clash; dx++)
                for (int dz = -1; dz <= 1 && !clash; dz++)
                {
                    var t = Tiles[x + dx, z + dz];
                    if (t.Type == TileType.Road || t.Type == TileType.Bridge || t.Type == TileType.Plaza || t.Type == TileType.Water) clash = true;
                }
                if (clash) continue;
                float score = Mathf.Abs(Tiles[x, z].Tier - preferTier) * 2f + Mathf.Abs(d - (minDist + maxDist) * 0.5f) + (float)rng.NextDouble();
                if (x < margin || x > W - 1 - margin) score += 0.5f;
                if (score < bestScore) { bestScore = score; best = c; }
            }
            return best;
        }

        void PlacePads()
        {
            Pads.Clear();
            var candidates = new List<(Vector2Int cell, int roadIndex, Vector2Int road)>();
            for (int i = 3; i < RoadCells.Count - 3; i++)
            {
                var rc = RoadCells[i];
                if (Tiles[rc.x, rc.y].Type != TileType.Road || Tiles[rc.x, rc.y].SlopeUp >= 0) continue;
                foreach (var d in Dirs)
                {
                    var c = rc + d;
                    if (!InGrid(c)) continue;
                    var t = Tiles[c.x, c.y];
                    if (t.Type != TileType.Grass || t.SlopeUp >= 0) continue;
                    candidates.Add((c, i, rc));
                }
            }
            int want = 10;
            int segment = Mathf.Max(1, (RoadCells.Count - 6) / want);
            var taken = new HashSet<Vector2Int>();
            for (int s = 0; s < want; s++)
            {
                int lo = 3 + s * segment, hi = lo + segment;
                (Vector2Int cell, int roadIndex, Vector2Int road) pick = default;
                float bestScore = float.MinValue;
                bool found = false;
                foreach (var cand in candidates)
                {
                    if (cand.roadIndex < lo || cand.roadIndex >= hi || taken.Contains(cand.cell)) continue;
                    bool near = false;
                    foreach (var t in taken) if (Mathf.Abs(t.x - cand.cell.x) + Mathf.Abs(t.y - cand.cell.y) < 2) near = true;
                    if (near) continue;
                    float score = (Tiles[cand.cell.x, cand.cell.y].Tier - Tiles[cand.road.x, cand.road.y].Tier) * 2f + (float)rng.NextDouble();
                    if (score > bestScore) { bestScore = score; pick = cand; found = true; }
                }
                if (!found) continue;
                taken.Add(pick.cell);
                var padTile = Tiles[pick.cell.x, pick.cell.y];
                padTile.Type = TileType.Pad;
                var roadTile = Tiles[pick.road.x, pick.road.y];
                Vector3 trap = CellCenter(pick.road);
                Vector3 next = CellCenter(RoadCells[Mathf.Min(pick.roadIndex + 1, RoadCells.Count - 1)]);
                Vector3 prev = CellCenter(RoadCells[Mathf.Max(pick.roadIndex - 1, 0)]);
                Pads.Add(new BuildPad
                {
                    Index = Pads.Count,
                    Cell = pick.cell,
                    TowerPosition = CellCenter(pick.cell),
                    TrapPosition = trap,
                    TrapRotation = Quaternion.LookRotation(next - prev),
                    HighGround = padTile.Tier > roadTile.Tier,
                    RoadDistance = pick.roadIndex,
                });
            }
        }

        void EnsureConnectivity()
        {
            for (int iteration = 0; iteration < 30; iteration++)
            {
                var reach = Reachable(SofraCell);
                bool all = true;
                Vector2Int target = default;
                foreach (var key in new[] { OcakCell, KitchenCell, MarketCell })
                    if (!reach.Contains(key)) { all = false; target = key; break; }
                if (all)
                {
                    foreach (var p in Pads)
                        if (!reach.Contains(p.Cell)) { all = false; target = p.Cell; break; }
                }
                if (all)
                {
                    var back = Reachable(OcakCell);
                    foreach (var key in new[] { SofraCell, KitchenCell, MarketCell })
                        if (!back.Contains(key)) { all = false; target = key; break; }
                }
                if (all) break;
                if (!AddStairsToward(target, reach)) break;
            }

            for (int iteration = 0; iteration < 40; iteration++)
            {
                var main = Reachable(SofraCell);
                bool fixedOne = false;
                for (int x = 0; x < W && !fixedOne; x++)
                for (int z = 0; z < H && !fixedOne; z++)
                {
                    var c = new Vector2Int(x, z);
                    if (!Walkable(c) || main.Contains(c)) continue;
                    if (AddStairsToward(c, main)) fixedOne = true;
                    else Tiles[x, z].Blocked = true;
                }
                if (!fixedOne) break;
            }
        }

        HashSet<Vector2Int> Reachable(Vector2Int from)
        {
            var seen = new HashSet<Vector2Int> { from };
            var q = new Queue<Vector2Int>();
            q.Enqueue(from);
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                foreach (var d in Dirs)
                {
                    var n = c + d;
                    if (!InGrid(n) || seen.Contains(n) || !CanStep(c, n)) continue;
                    seen.Add(n);
                    q.Enqueue(n);
                }
            }
            return seen;
        }

        bool AddStairsToward(Vector2Int target, HashSet<Vector2Int> mainRegion)
        {
            var region = Reachable(target);
            foreach (var c in region)
            {
                var tc = Tiles[c.x, c.y];
                if (tc.Type != TileType.Grass || tc.SlopeUp >= 0) continue;
                for (int dir = 0; dir < 4; dir++)
                {
                    var n = c + Dirs[dir];
                    var behind = c - Dirs[dir];
                    if (!InGrid(n) || !InGrid(behind) || !mainRegion.Contains(n)) continue;
                    var tn = Tiles[n.x, n.y];
                    if (tn.Type == TileType.Water || tn.SlopeUp >= 0) continue;
                    if (tn.Tier == tc.Tier + 1 && region.Contains(behind) && Tiles[behind.x, behind.y].Tier == tc.Tier && Tiles[behind.x, behind.y].SlopeUp < 0)
                    {
                        Tiles[c.x, c.y] = new Tile { Type = TileType.Stairs, Tier = tn.Tier, SlopeUp = dir };
                        return true;
                    }
                    if (tn.Tier == tc.Tier - 1)
                    {
                        Tiles[c.x, c.y] = new Tile { Type = TileType.Stairs, Tier = tc.Tier, SlopeUp = (dir + 2) % 4 };
                        if (CanStep(c, n) && CanStep(c, behind)) return true;
                        Tiles[c.x, c.y] = tc;
                    }
                }
            }
            foreach (var c in region)
            {
                var tc = Tiles[c.x, c.y];
                if (tc.Type != TileType.Grass || tc.SlopeUp >= 0) continue;
                foreach (var d in Dirs)
                {
                    var n = c + d;
                    if (InGrid(n) && mainRegion.Contains(n) && Tiles[n.x, n.y].Type == TileType.Grass && Tiles[n.x, n.y].SlopeUp < 0)
                    {
                        tc.Tier = Tiles[n.x, n.y].Tier;
                        return true;
                    }
                }
            }
            return false;
        }

        void BuildRoadCurve()
        {
            roadPoints.Clear();
            roadDist.Clear();
            var centers = new List<Vector3>();
            foreach (var c in RoadCells)
            {
                var p = CellCenter(c);
                p.y = Height(p.x, p.z);
                centers.Add(p);
            }


            var dense = new List<Vector3>();
            for (int i = 0; i < centers.Count - 1; i++)
            {
                Vector3 p0 = centers[Mathf.Max(i - 1, 0)], p1 = centers[i], p2 = centers[i + 1], p3 = centers[Mathf.Min(i + 2, centers.Count - 1)];
                for (int s = 0; s < 8; s++)
                {
                    float t = s / 8f;
                    var p = CatmullRom(p0, p1, p2, p3, t);
                    p.y = Mathf.Lerp(p1.y, p2.y, t) + 0.05f;
                    dense.Add(p);
                }
            }
            dense.Add(centers[centers.Count - 1] + Vector3.up * 0.05f);
            float acc = 0f;
            for (int i = 0; i < dense.Count; i++)
            {
                if (i > 0) acc += Vector3.Distance(dense[i - 1], dense[i]);
                roadPoints.Add(dense[i]);
                roadDist.Add(acc);
            }
            RoadLength = acc;

            foreach (var pad in Pads)
            {
                DistanceToRoad(pad.TrapPosition, out float rd);
                pad.RoadDistance = rd;
                SampleRoad(rd, out var tan);
                pad.TrapRotation = Quaternion.LookRotation(tan);
                pad.TrapPosition = SampleRoad(rd, 0f);
                pad.TowerPosition.y = Height(pad.TowerPosition);
                Vector3 toTower = pad.TowerPosition - pad.TrapPosition;
                pad.Side = Vector3.Dot(toTower, new Vector3(tan.z, 0, -tan.x)) > 0 ? 1 : -1;
            }
            Pads.Sort((a, b) => a.RoadDistance.CompareTo(b.RoadDistance));
            for (int i = 0; i < Pads.Count; i++) Pads[i].Index = i;
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        void PlaceStations()
        {
            Stations.Clear();
            Vector3 ocak = CellCenter(OcakCell);
            ocak.y = Height(ocak);
            Vector3 sofra = CellCenter(SofraCell);
            sofra.y = Height(sofra);
            Stations.Add(new Station { Kind = StationKind.Ocak, Position = ocak, Index = 0 });
            Stations.Add(new Station { Kind = StationKind.Sofra, Position = sofra, Index = 1 });
            AddKitchen(StationKind.DefenderCauldron, KitchenCell);
            AddKitchen(StationKind.AttackerCauldron, MarketCell);

            AttackerSpawn = ocak + new Vector3(0, 0, OcakCell.y > H / 2 ? -T : T);
            AttackerSpawn = new Vector3(AttackerSpawn.x, Height(AttackerSpawn) + 0.1f, AttackerSpawn.z);
            DefenderSpawn = CellCenter(KitchenCell) + (Sofra.Position - CellCenter(KitchenCell)).normalized * 1.5f;
            DefenderSpawn = new Vector3(DefenderSpawn.x, Height(DefenderSpawn) + 0.1f, DefenderSpawn.z);
        }

        void AddKitchen(StationKind kind, Vector2Int cell)
        {
            Vector3 c = CellCenter(cell);
            Vector3 toward = (kind == StationKind.DefenderCauldron ? Sofra.Position : Ocak.Position) - c;
            toward.y = 0f;
            toward = toward.sqrMagnitude > 0.01f ? toward.normalized : Vector3.forward;
            Vector3 arcCenter = c + toward * 1.4f;
            for (int i = 0; i < 5; i++)
            {
                float angle = -64f + 32f * i;
                var p = arcCenter + Quaternion.Euler(0, angle, 0) * -toward * 4.4f;
                p.y = Height(p);
                Stations.Add(new Station { Kind = kind, Soup = (SoupType)i, Position = p, Index = Stations.Count });
            }
        }

        #endregion

        #region Dressing

        GameObject Place(string model, Vector3 pos, Quaternion rot, float scale, Color fallback)
        {
            var go = Art.Spawn(model, root, fallback);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale *= scale;
            return go;
        }

        void DecorateRoad()
        {
            var arrowMat = FX.Mat("road_arrow");
            for (float d = 6f; d < RoadLength - 4f; d += 7f)
            {
                var p = SampleRoad(d, out var tan);
                var go = new GameObject("Arrow");
                go.transform.SetParent(root, false);
                go.transform.position = p + Vector3.up * 0.04f;
                go.transform.rotation = Quaternion.LookRotation(Vector3.down, tan);
                go.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
                go.AddComponent<MeshFilter>().sharedMesh = FX.Quad;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = arrowMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            foreach (var s in Stations)
            {
                switch (s.Kind)
                {
                    case StationKind.Ocak:
                        var ocakGo = Place("ocak", s.Position, LookAtRoad(s.Position), 1f, GameData.Hex("8E5A3A"));
                        ocakGo.AddComponent<BoxCollider>().size = new Vector3(3, 2, 3);
                        OcakObject = ocakGo.transform;
                        Place("banner", s.Position + new Vector3(1.8f, 0, 1.8f), Quaternion.identity, 1f, GameData.Hex("C04A2C"));
                        break;
                    case StationKind.Sofra:
                        SofraObject = Place("sofra", s.Position, LookAtRoad(s.Position), 1f, GameData.Hex("B98A55")).transform;
                        break;
                    default:
                        var cauldron = Place("cauldron", s.Position, Quaternion.Euler(0, s.Index * 47f, 0), 1f, GameData.Hex("3D3A3A"));
                        var col = cauldron.AddComponent<CapsuleCollider>();
                        col.radius = 1.05f;
                        col.height = 2.5f;
                        col.center = new Vector3(0, 1.2f, 0);
                        s.Surface = SoupSurface(cauldron.transform, GameData.Soup(s.Soup).Liquid, 1.28f, 1.9f);
                        cauldron.AddComponent<BoilingPot>().Init(s.Surface, GameData.Soup(s.Soup).Liquid);
                        var center = CellCenter(s.Kind == StationKind.DefenderCauldron ? KitchenCell : MarketCell);
                        Vector3 outward = (s.Position - center);
                        outward.y = 0;
                        var sign = Place("soup_sign", s.Position + outward.normalized * 1.55f, Quaternion.LookRotation(-outward.normalized), 1f, GameData.Soup(s.Soup).Identity);
                        Art.Tint(sign, GameData.Soup(s.Soup).Identity, 0.75f);
                        var soupInfo = GameData.Soup(s.Soup);
                        string role = s.Kind == StationKind.DefenderCauldron
                            ? $"{soupInfo.Keyword}: {GameData.Defence(GameData.DefenceOption(s.Soup, 0)).English} / {GameData.Defence(GameData.DefenceOption(s.Soup, 1)).English}"
                            : $"{soupInfo.Keyword}: {GameData.Unit(GameData.UnitOption(s.Soup, 0)).English} / {GameData.Unit(GameData.UnitOption(s.Soup, 1)).English}";
                        WorldLabel.Create(root, s.Position + Vector3.up * 3.1f, soupInfo.Name, role, soupInfo.Identity, soupInfo.Glyph, 9f);
                        break;
                }
            }

            foreach (var p in Pads)
            {
                var pad = Place("pad", p.TowerPosition + Vector3.up * 0.02f, Quaternion.identity, 1.1f, GameData.Hex("9C9184"));
                var marker = new GameObject("PadMarker").AddComponent<PadMarker>();
                marker.transform.SetParent(root, false);
                marker.transform.position = p.TowerPosition;
                marker.Init(p);
                p.Marker = marker.transform;
                if (p.HighGround) Place("flag_high", p.TowerPosition + new Vector3(1.1f, 0, 1.1f), Quaternion.identity, 1f, GameData.Hex("F2B33D"));
            }
        }

        Quaternion LookAtRoad(Vector3 p)
        {
            DistanceToRoad(p, out float rd);
            Vector3 dir = SampleRoad(rd, out _) - p;
            dir.y = 0;
            return dir.sqrMagnitude > 0.01f ? Quaternion.LookRotation(dir) : Quaternion.identity;
        }

        public static Transform SoupSurface(Transform parent, Color c, float height, float diameter)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(disc.GetComponent<Collider>());
            disc.name = "SoupSurface";
            disc.transform.SetParent(parent, false);
            disc.transform.localPosition = new Vector3(0, height, 0);
            disc.transform.localScale = new Vector3(diameter, 0.01f, diameter);
            disc.GetComponent<MeshRenderer>().sharedMaterial = Art.Soup(c);
            return disc.transform;
        }

        bool NearStation(Vector3 p, float r)
        {
            foreach (var s in Stations)
                if (FlatDist(p, s.Position) < r) return true;
            return false;
        }

        void PlaceBumpsAndDecor()
        {
            Bumps.Clear();
            string[] trees = { "tree_a", "tree_b", "tree_c", "tree_a" };
            string[] small = { "bush", "flowers", "bush", "mushroom", "rock_big", "flowers" };
            for (int x = 0; x < W; x++)
            for (int z = 0; z < H; z++)
            {
                var c = new Vector2Int(x, z);
                var t = Tiles[x, z];
                if (t.Type != TileType.Grass || t.SlopeUp >= 0) continue;
                Vector3 center = CellCenter(c);
                center.y = Height(center);
                bool edge = x == 0 || z == 0 || x == W - 1 || z == H - 1;
                bool nearKey = NearStation(center, 5f);
                bool besideRoad = false;
                foreach (var d in Dirs)
                {
                    var n = c + d;
                    if (InGrid(n) && (Tiles[n.x, n.y].Type == TileType.Road || Tiles[n.x, n.y].Type == TileType.Pad)) besideRoad = true;
                }
                double roll = rng.NextDouble();
                if (t.Blocked)
                {
                    var tree = Place(trees[rng.Next(trees.Length)], center + RandOffset(0.6f), Quaternion.Euler(0, Rand(0, 360), 0), Rand(0.9f, 1.35f), GameData.Hex("4E7F3A"));
                    AddTreeCollider(tree);
                    continue;
                }
                if (nearKey || edge) continue;
                if (!besideRoad && roll < 0.12)
                {
                    var corner = new Vector3(rng.Next(2) == 0 ? -1.25f : 1.25f, 0, rng.Next(2) == 0 ? -1.25f : 1.25f);
                    var tree = Place(trees[rng.Next(trees.Length)], center + corner, Quaternion.Euler(0, Rand(0, 360), 0), Rand(0.8f, 1.1f), GameData.Hex("4E7F3A"));
                    AddTreeCollider(tree);
                    continue;
                }
                if (roll < 0.36)
                {
                    bool mud = rng.NextDouble() < 0.3;
                    var p = center + RandOffset(0.8f);
                    p.y = Height(p);
                    Bumps.Add(new Bump { Position = p, Radius = mud ? 1.3f : 0.55f, Mud = mud });
                    if (mud) Place("mud", p + Vector3.up * 0.03f, Quaternion.Euler(0, Rand(0, 360), 0), 1.3f, GameData.Hex("6B4A2E"));
                    else Place(rng.NextDouble() < 0.5 ? "rock_small" : "root", p, Quaternion.Euler(0, Rand(0, 360), 0), Rand(0.9f, 1.2f), GameData.Hex("8B8378"));
                }
                if (roll > 0.55)
                {
                    int count = rng.Next(1, 3);
                    for (int i = 0; i < count; i++)
                    {
                        var p = center + RandOffset(1.3f);
                        p.y = Height(p);
                        Place(small[rng.Next(small.Length)], p, Quaternion.Euler(0, Rand(0, 360), 0), Rand(0.7f, 1.1f), GameData.Hex("5E9440"));
                    }
                }
            }
        }

        void AddTreeCollider(GameObject tree)
        {
            var col = tree.AddComponent<CapsuleCollider>();
            col.radius = 0.45f;
            col.height = 3f;
            col.center = new Vector3(0, 1.5f, 0);
        }

        Vector3 RandOffset(float r) => new Vector3(Rand(-r, r), 0, Rand(-r, r));

        void BuildLabels()
        {
            WorldLabel.Create(root, Ocak.Position + Vector3.up * 3.6f, "OCAK", "Attacker camp", GameData.Hex("E07A5A"));
            WorldLabel.Create(root, Sofra.Position + Vector3.up * 2.6f, "SOFRA", "Defend the table", GameData.Hex("6FA0D8"));
            WorldLabel.Create(root, CellCenter(KitchenCell) + Vector3.up * (Height(CellCenter(KitchenCell)) + 5.6f) - (Sofra.Position - CellCenter(KitchenCell)).normalized * 3.5f, "KITCHEN", "Defender soups", GameData.Hex("6FA0D8"));
            WorldLabel.Create(root, CellCenter(MarketCell) + Vector3.up * (Height(CellCenter(MarketCell)) + 5.6f) - (Ocak.Position - CellCenter(MarketCell)).normalized * 3.5f, "MARKET", "Attacker soups", GameData.Hex("E07A5A"));
        }

        #endregion
    }
}
