using System.Collections.Generic;
using UnityEngine;

namespace Corba
{
    /// <summary>
    /// Fine walkability grid for bot cooks. Terrain is checked analytically by sampling height continuity along
    /// each move, so ramp side walls and cliff corners are exact; props (cauldrons, trees, the Ocak) come from physics.
    /// </summary>
    public class NavGrid
    {
        public const float Cell = 0.8f;
        const float StepTolerance = 0.3f;
        const float MaxDrop = 3.2f;
        const float AgentRadius = 0.45f;
        const float DropCost = 5f;
        const float WallHugCost = 2.5f;

        public readonly int W, H;
        readonly CorbaMap map;
        readonly float originX, originZ;
        readonly bool[] walkable;
        readonly bool[] nearWall;
        readonly float[] height;
        readonly float[] penalty;
        readonly int[] regionSize;
        readonly int[] region;
        // Pockets of floor smaller than this (gaps between props) are never used as path endpoints.
        const int MinRegion = 40;

        static readonly Collider[] overlap = new Collider[16];
        static readonly int[] DX = { 1, -1, 0, 0, 1, 1, -1, -1 };
        static readonly int[] DZ = { 0, 0, 1, -1, 1, -1, 1, -1 };

        public NavGrid(CorbaMap map)
        {
            this.map = map;
            originX = -CorbaMap.HalfX;
            originZ = -CorbaMap.HalfZ;
            W = Mathf.CeilToInt(CorbaMap.HalfX * 2f / Cell);
            H = Mathf.CeilToInt(CorbaMap.HalfZ * 2f / Cell);
            walkable = new bool[W * H];
            nearWall = new bool[W * H];
            height = new float[W * H];
            penalty = new float[W * H];
            regionSize = new int[W * H];
            region = new int[W * H];
            Physics.SyncTransforms();
            int mask = Physics.DefaultRaycastLayers;
            for (int z = 0; z < H; z++)
            for (int x = 0; x < W; x++)
            {
                int i = z * W + x;
                Vector3 p = CellCenter(x, z);
                var tile = map.At(map.CellOf(p));
                float h = map.Height(p.x, p.z);
                height[i] = h;
                if (tile.Type == TileType.Water) continue;
                Vector3 a = new Vector3(p.x, h + 0.65f, p.z), b = new Vector3(p.x, h + 1.7f, p.z);
                int hits = Physics.OverlapCapsuleNonAlloc(a, b, AgentRadius, overlap, mask, QueryTriggerInteraction.Ignore);
                bool blocked = false;
                for (int k = 0; k < hits && !blocked; k++) blocked = IsProp(overlap[k]);
                walkable[i] = !blocked;
            }
            for (int z = 0; z < H; z++)
            for (int x = 0; x < W; x++)
            {
                int i = z * W + x;
                if (!walkable[i]) continue;
                Vector3 p = CellCenter(x, z);
                for (int k = 0; k < 8 && !nearWall[i]; k++)
                {
                    var q = p + new Vector3(DX[k], 0, DZ[k]).normalized * (AgentRadius + 0.1f);
                    if (!Continuous(p, q, out _) || !WalkableAt(q)) nearWall[i] = true;
                }
            }
            LabelRegions();
        }

        /// <summary>Flood-fills two-way walkable regions so path endpoints never snap into sealed pockets.</summary>
        void LabelRegions()
        {
            var label = new int[W * H];
            var queue = new Queue<int>();
            var members = new List<int>();
            int next = 0;
            for (int start = 0; start < W * H; start++)
            {
                if (!walkable[start] || label[start] != 0) continue;
                next++;
                members.Clear();
                label[start] = next;
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    int cur = queue.Dequeue();
                    members.Add(cur);
                    int cx = cur % W, cz = cur / W;
                    for (int k = 0; k < 4; k++)
                    {
                        int nx = cx + DX[k], nz = cz + DZ[k];
                        if (nx < 0 || nz < 0 || nx >= W || nz >= H) continue;
                        int n = nz * W + nx;
                        if (label[n] != 0 || Edge(cur, n) != 1 || Edge(n, cur) != 1) continue;
                        label[n] = next;
                        queue.Enqueue(n);
                    }
                }
                foreach (var m in members)
                {
                    regionSize[m] = members.Count;
                    region[m] = next;
                }
            }
        }

        static bool IsProp(Collider c)
        {
            if (c is CharacterController) return false;
            string n = c.gameObject.name;
            return n != "Terrain" && n != "Bound";
        }

        public Vector3 CellCenter(int x, int z) => new Vector3(originX + (x + 0.5f) * Cell, 0f, originZ + (z + 0.5f) * Cell);

        public Vector2Int CellOf(Vector3 p) => new Vector2Int(
            Mathf.Clamp(Mathf.FloorToInt((p.x - originX) / Cell), 0, W - 1),
            Mathf.Clamp(Mathf.FloorToInt((p.z - originZ) / Cell), 0, H - 1));

        bool WalkableAt(Vector3 p)
        {
            if (p.x < originX || p.z < originZ || p.x >= -originX || p.z >= -originZ) return false;
            var c = CellOf(p);
            return walkable[c.y * W + c.x];
        }

        public bool IsWalkable(Vector3 p) => WalkableAt(p);

        /// <summary>True when every point can reach every other one and come back.</summary>
        public bool AllConnected(List<Vector3> points)
        {
            int id = -1;
            foreach (var p in points)
            {
                int cell = Nearest(CellOf(p));
                if (cell < 0) return false;
                if (id < 0) id = region[cell];
                else if (region[cell] != id) return false;
            }
            return true;
        }

#if UNITY_EDITOR
        public string Explain(Vector3 from, Vector3 to)
        {
            var a = CellOf(from);
            var b = CellOf(to);
            int sa = Nearest(a), sb = Nearest(b);
            string Info(int idx) => idx < 0 ? "none" : $"({idx % W},{idx / W}) h{height[idx]:0.00} region{regionSize[idx]}";
            return $"from cell ({a.x},{a.y}) walk {walkable[a.y * W + a.x]} -> start {Info(sa)} | to cell ({b.x},{b.y}) walk {walkable[b.y * W + b.x]} -> goal {Info(sb)}";
        }

        /// <summary>Editor debug picture: terrain height shading, props black, wall-hugging cells darker.</summary>
        public Texture2D DebugTexture(int scale)
        {
            var tex = new Texture2D(W * scale, H * scale, TextureFormat.RGB24, false);
            for (int z = 0; z < H; z++)
            for (int x = 0; x < W; x++)
            {
                int i = z * W + x;
                var tile = map.At(map.CellOf(CellCenter(x, z)));
                Color c;
                if (tile.Type == TileType.Water) c = new Color(0.2f, 0.4f, 0.9f);
                else if (!walkable[i]) c = Color.black;
                else
                {
                    float t = Mathf.InverseLerp(-1f, 2.6f, height[i]);
                    c = Color.Lerp(new Color(0.3f, 0.6f, 0.25f), new Color(0.95f, 0.9f, 0.6f), t);
                    if (tile.SlopeUp >= 0) c = Color.Lerp(c, new Color(0.9f, 0.5f, 0.9f), 0.5f);
                    if (nearWall[i]) c *= 0.75f;
                }
                for (int sy = 0; sy < scale; sy++)
                for (int sx = 0; sx < scale; sx++)
                    tex.SetPixel(x * scale + sx, z * scale + sy, c);
            }
            return tex;
        }

        public void Mark(Texture2D tex, Vector3 p, Color c, int scale, int size = 2)
        {
            var cell = CellOf(p);
            for (int dy = -size; dy <= size; dy++)
            for (int dx = -size; dx <= size; dx++)
                tex.SetPixel(cell.x * scale + scale / 2 + dx, cell.y * scale + scale / 2 + dy, c);
        }
#endif

        /// <summary>
        /// Samples terrain height along a straight move. Returns false if any step up or down is bigger than a cook can walk.
        /// biggestDrop reports the largest single downward step, so callers can treat it as a jump.
        /// </summary>
        bool Continuous(Vector3 a, Vector3 b, out float biggestDrop)
        {
            biggestDrop = 0f;
            float len = CorbaMap.FlatDist(a, b);
            int n = Mathf.Max(1, Mathf.CeilToInt(len / 0.15f));
            float prev = map.Height(a.x, a.z);
            bool ok = true;
            for (int s = 1; s <= n; s++)
            {
                var p = Vector3.Lerp(a, b, s / (float)n);
                float h = map.Height(p.x, p.z);
                float dh = h - prev;
                if (dh > StepTolerance) return false;
                if (dh < -StepTolerance)
                {
                    ok = false;
                    biggestDrop = Mathf.Max(biggestDrop, -dh);
                }
                prev = h;
            }
            return ok;
        }

        // 0 = blocked, 1 = walk, 2 = one-way drop
        int Edge(int from, int to)
        {
            if (!walkable[to]) return 0;
            Vector3 a = CellCenter(from % W, from / W), b = CellCenter(to % W, to / W);
            if (Continuous(a, b, out float drop)) return 1;
            if (drop > 0f && drop <= MaxDrop && Mathf.Abs(height[to] - height[from]) > StepTolerance && height[to] < height[from]) return 2;
            return 0;
        }

        public void Penalize(Vector3 p, float amount)
        {
            var c = CellOf(p);
            for (int dz = -1; dz <= 1; dz++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int x = c.x + dx, z = c.y + dz;
                if (x < 0 || z < 0 || x >= W || z >= H) continue;
                penalty[z * W + x] = Mathf.Min(penalty[z * W + x] + amount * (dx == 0 && dz == 0 ? 1f : 0.5f), 40f);
            }
        }

        public void DecayPenalties(float dt)
        {
            float k = Mathf.Exp(-dt / 20f);
            for (int i = 0; i < penalty.Length; i++) penalty[i] *= k;
        }

        int Nearest(Vector2Int c, int maxRadius = 8, int inRegion = -1)
        {
            int best = -1;
            float bestScore = float.MaxValue;
            for (int r = 0; r <= maxRadius; r++)
            {
                for (int dz = -r; dz <= r; dz++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dz) != r) continue;
                    int x = c.x + dx, z = c.y + dz;
                    if (x < 0 || z < 0 || x >= W || z >= H || !walkable[z * W + x] || regionSize[z * W + x] < MinRegion) continue;
                    if (inRegion >= 0 && region[z * W + x] != inRegion) continue;
                    float score = dx * dx + dz * dz + (nearWall[z * W + x] ? 0.5f : 0f);
                    if (score < bestScore) { bestScore = score; best = z * W + x; }
                }
                if (best >= 0) return best;
            }
            return -1;
        }

        readonly List<int> heap = new List<int>();
        float[] g, f;
        int[] came;
        int[] stamp;
        int search;

        /// <summary>Path as world waypoints; empty when the target cannot be reached.</summary>
        public List<Vector3> FindPath(Vector3 from, Vector3 to)
        {
            var result = new List<Vector3>();
            int start = Nearest(CellOf(from));
            if (start < 0) return result;
            int goal = Nearest(CellOf(to), 8, region[start]);
            if (goal < 0) goal = Nearest(CellOf(to));
            if (goal < 0) return result;
            if (g == null)
            {
                g = new float[W * H];
                f = new float[W * H];
                came = new int[W * H];
                stamp = new int[W * H];
            }
            search++;
            heap.Clear();
            g[start] = 0f;
            f[start] = Heuristic(start, goal);
            came[start] = -1;
            stamp[start] = search;
            Push(start);
            bool found = false;
            int expanded = 0;
            while (heap.Count > 0 && expanded < 30000)
            {
                int cur = Pop();
                expanded++;
                if (cur == goal) { found = true; break; }
                int cx = cur % W, cz = cur / W;
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + DX[k], nz = cz + DZ[k];
                    if (nx < 0 || nz < 0 || nx >= W || nz >= H) continue;
                    int n = nz * W + nx;
                    int edge = Edge(cur, n);
                    if (edge == 0) continue;
                    if (k >= 4 && (Edge(cur, cz * W + nx) != 1 || Edge(cur, nz * W + cx) != 1)) continue;
                    float step = (k >= 4 ? 1.4142f : 1f) * Cell * (1f + (nearWall[n] ? WallHugCost : 0f)) + penalty[n] * Cell + (edge == 2 ? DropCost : 0f);
                    float ng = g[cur] + step;
                    if (stamp[n] == search && ng >= g[n]) continue;
                    stamp[n] = search;
                    g[n] = ng;
                    f[n] = ng + Heuristic(n, goal);
                    came[n] = cur;
                    Push(n);
                }
            }
            if (!found) return result;

            var cells = new List<int>();
            for (int c = goal; c != -1; c = came[c]) cells.Add(c);
            cells.Reverse();

            var pts = new List<Vector3>(cells.Count);
            foreach (var c in cells) pts.Add(Point(c));
            var target = new Vector3(to.x, map.Height(to.x, to.z), to.z);
            if (Continuous(Point(goal), target, out _)) pts.Add(target);

            int i = 0;
            while (i < pts.Count - 1)
            {
                int j = pts.Count - 1;
                while (j > i + 1 && !Clear(pts[i], pts[j])) j--;
                result.Add(pts[j]);
                i = j;
            }
            return result;
        }

        Vector3 Point(int idx)
        {
            var p = CellCenter(idx % W, idx / W);
            p.y = height[idx];
            return p;
        }

        float Heuristic(int a, int b)
        {
            int dx = Mathf.Abs(a % W - b % W), dz = Mathf.Abs(a / W - b / W);
            return (Mathf.Max(dx, dz) + 0.4142f * Mathf.Min(dx, dz)) * Cell;
        }

        /// <summary>A straight walk the cook's whole body fits along: continuous terrain at the centre and both shoulders.</summary>
        bool Clear(Vector3 a, Vector3 b)
        {
            Vector3 d = b - a;
            d.y = 0f;
            float len = d.magnitude;
            if (len < 1e-3f) return true;
            Vector3 side = new Vector3(d.z, 0f, -d.x) / len * AgentRadius;
            if (!Continuous(a, b, out _) || !Continuous(a + side, b + side, out _) || !Continuous(a - side, b - side, out _)) return false;
            int n = Mathf.CeilToInt(len / (Cell * 0.4f));
            for (int s = 1; s < n; s++)
            {
                var p = Vector3.Lerp(a, b, s / (float)n);
                if (!WalkableAt(p) || !WalkableAt(p + side) || !WalkableAt(p - side)) return false;
                var c = CellOf(p);
                if (penalty[c.y * W + c.x] > 2f) return false;
            }
            return true;
        }

        public float PathLength(Vector3 from, Vector3 to)
        {
            var path = FindPath(from, to);
            if (path.Count == 0) return -1f;
            float len = 0f;
            Vector3 last = from;
            foreach (var p in path)
            {
                len += CorbaMap.FlatDist(last, p);
                last = p;
            }
            return len;
        }

        void Push(int n)
        {
            heap.Add(n);
            int i = heap.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (f[heap[parent]] <= f[heap[i]]) break;
                (heap[parent], heap[i]) = (heap[i], heap[parent]);
                i = parent;
            }
        }

        int Pop()
        {
            int top = heap[0];
            int last = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);
            if (heap.Count == 0) return top;
            heap[0] = last;
            int i = 0;
            while (true)
            {
                int l = i * 2 + 1, r = l + 1, m = i;
                if (l < heap.Count && f[heap[l]] < f[heap[m]]) m = l;
                if (r < heap.Count && f[heap[r]] < f[heap[m]]) m = r;
                if (m == i) break;
                (heap[m], heap[i]) = (heap[i], heap[m]);
                i = m;
            }
            return top;
        }
    }
}
