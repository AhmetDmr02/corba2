using System.Collections.Generic;
using UnityEngine;

namespace Corba
{
    public class TerrainMeshBuilder
    {
        enum Layer { Grass, Dirt, Plaza, Cliff, Water, Wood, Count }

        class Part
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Vector2> UV = new List<Vector2>();
            public readonly List<Color> C = new List<Color>();
            public readonly List<int> I = new List<int>();
        }

        const float BaseDepth = -3f;
        const float WaterLevel = -0.35f;

        static readonly Color[] TierGrass = { GameData.Hex("78AB4C"), GameData.Hex("8FBA52"), GameData.Hex("A8C35C") };

        readonly CorbaMap map;
        readonly Part[] parts = new Part[(int)Layer.Count];

        public TerrainMeshBuilder(CorbaMap map)
        {
            this.map = map;
            for (int i = 0; i < parts.Length; i++) parts[i] = new Part();
        }

        float[] Corners(Vector2Int c)
        {
            var t = map.At(c);
            if (t.Type == TileType.Water) return new[] { -0.9f, -0.9f, -0.9f, -0.9f };
            float top = t.Tier * CorbaMap.TierHeight;
            float low = top - CorbaMap.TierHeight;
            var h = new[] { top, top, top, top };
            switch (t.SlopeUp)
            {
                case 0: h[0] = low; h[3] = low; break;
                case 1: h[0] = low; h[1] = low; break;
                case 2: h[1] = low; h[2] = low; break;
                case 3: h[2] = low; h[3] = low; break;
            }
            return h;
        }

        // Corner order: 0=(x0,z0) 1=(x1,z0) 2=(x1,z1) 3=(x0,z1). Edge e uses corners per EdgeCorners.
        static readonly int[][] EdgeCorners = { new[] { 1, 2 }, new[] { 2, 3 }, new[] { 3, 0 }, new[] { 0, 1 } };
        static readonly Vector2Int[] Dirs = { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1) };

        Vector3 CornerPos(Vector2Int c, int corner, float h)
        {
            float x0 = c.x * CorbaMap.T - CorbaMap.HalfX, z0 = c.y * CorbaMap.T - CorbaMap.HalfZ;
            float x = corner == 1 || corner == 2 ? x0 + CorbaMap.T : x0;
            float z = corner == 2 || corner == 3 ? z0 + CorbaMap.T : z0;
            return new Vector3(x, h, z);
        }

        public void Build(Transform root)
        {
            for (int x = 0; x < CorbaMap.W; x++)
            for (int z = 0; z < CorbaMap.H; z++)
            {
                var c = new Vector2Int(x, z);
                var tile = map.At(c);
                var h = Corners(c);
                AddTop(c, tile, h);
                for (int e = 0; e < 4; e++) AddWall(c, e, h);
            }

            var go = new GameObject("Terrain");
            go.transform.SetParent(root, false);
            var mesh = new Mesh { name = "Terrain", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var cols = new List<Color>();
            var mats = new List<Material>();
            var subs = new List<List<int>>();
            for (int i = 0; i < parts.Length; i++)
            {
                var p = parts[i];
                if (p.I.Count == 0) continue;
                int offset = verts.Count;
                verts.AddRange(p.V);
                uvs.AddRange(p.UV);
                cols.AddRange(p.C);
                var idx = new List<int>(p.I.Count);
                foreach (var k in p.I) idx.Add(k + offset);
                subs.Add(idx);
                mats.Add(MaterialFor((Layer)i));
            }
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(cols);
            mesh.subMeshCount = subs.Count;
            for (int i = 0; i < subs.Count; i++) mesh.SetTriangles(subs[i], i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = mats.ToArray();
            go.AddComponent<MeshCollider>().sharedMesh = mesh;

            for (int x = 0; x < CorbaMap.W; x++)
            for (int z = 0; z < CorbaMap.H; z++)
            {
                var c = new Vector2Int(x, z);
                if (map.At(c).Type != TileType.Water) continue;
                var block = new GameObject("WaterBlock");
                block.transform.SetParent(root, false);
                var center = map.CellCenter(c);
                block.transform.position = new Vector3(center.x, 1f, center.z);
                block.AddComponent<BoxCollider>().size = new Vector3(CorbaMap.T, 4f, CorbaMap.T);
            }
            AddBorderWalls(root);
        }

        void AddBorderWalls(Transform root)
        {
            float h = 8f;
            void Wall(Vector3 center, Vector3 size)
            {
                var go = new GameObject("Bound");
                go.transform.SetParent(root, false);
                go.transform.position = center;
                go.AddComponent<BoxCollider>().size = size;
            }
            Wall(new Vector3(0, h * 0.5f, CorbaMap.HalfZ + 0.5f), new Vector3(CorbaMap.HalfX * 2, h, 1));
            Wall(new Vector3(0, h * 0.5f, -CorbaMap.HalfZ - 0.5f), new Vector3(CorbaMap.HalfX * 2, h, 1));
            Wall(new Vector3(CorbaMap.HalfX + 0.5f, h * 0.5f, 0), new Vector3(1, h, CorbaMap.HalfZ * 2));
            Wall(new Vector3(-CorbaMap.HalfX - 0.5f, h * 0.5f, 0), new Vector3(1, h, CorbaMap.HalfZ * 2));
        }

        static Material MaterialFor(Layer layer)
        {
            switch (layer)
            {
                case Layer.Grass: return Art.GroundMaterial("ground_grass", 1f);
                case Layer.Dirt: return Art.GroundMaterial("ground_dirt", 0f);
                case Layer.Plaza: return Art.GroundMaterial("ground_plaza", 0f);
                case Layer.Cliff: return Art.GroundMaterial("ground_cliff", 0.6f);
                case Layer.Water: return Art.GroundMaterial("ground_water", 0f);
                default: return Art.GroundMaterial("ground_wood", 0f);
            }
        }

        Layer TopLayer(TileType type)
        {
            switch (type)
            {
                case TileType.Road: return Layer.Dirt;
                case TileType.Plaza:
                case TileType.Pad: return Layer.Plaza;
                case TileType.Water: return Layer.Water;
                case TileType.Bridge:
                case TileType.Stairs: return Layer.Wood;
                default: return Layer.Grass;
            }
        }

        void AddTop(Vector2Int c, Tile tile, float[] h)
        {
            var layer = TopLayer(tile.Type);
            var p = parts[(int)layer];
            Color baseCol = layer == Layer.Grass ? TierGrass[Mathf.Clamp(tile.Tier, 0, 2)] : Color.white;
            if (layer == Layer.Grass)
            {
                float jitter = Mathf.PerlinNoise(c.x * 0.7f, c.y * 0.7f) * 0.08f - 0.04f;
                baseCol = new Color(baseCol.r + jitter, baseCol.g + jitter, baseCol.b + jitter * 0.5f);
            }
            if (tile.Type == TileType.Water)
            {
                for (int i = 0; i < 4; i++) h[i] = WaterLevel;
            }

            var corners = new Vector3[4];
            for (int i = 0; i < 4; i++) corners[i] = CornerPos(c, i, h[i]);
            Vector3 center = (corners[0] + corners[1] + corners[2] + corners[3]) * 0.25f;
            bool stairs = tile.Type == TileType.Stairs || (tile.Type == TileType.Bridge);

            int b = p.V.Count;
            p.V.Add(center);
            foreach (var v in corners) p.V.Add(v);
            for (int i = 0; i < 5; i++)
            {
                var v = p.V[b + i];
                Vector2 uv = new Vector2(v.x, v.z) * (layer == Layer.Grass ? 0.11f : 0.16f);
                if (stairs && tile.SlopeUp >= 0)
                {
                    var d = Dirs[tile.SlopeUp];
                    uv = new Vector2(Vector2.Dot(new Vector2(v.x, v.z), new Vector2(-d.y, d.x)), Vector2.Dot(new Vector2(v.x, v.z), new Vector2(d.x, d.y))) * 0.31f;
                }
                p.UV.Add(uv);
                p.C.Add(i == 0 ? baseCol * 1.07f : baseCol * 0.9f);
            }
            for (int i = 0; i < 4; i++)
            {
                p.I.Add(b);
                p.I.Add(b + 1 + (i + 1) % 4);
                p.I.Add(b + 1 + i);
            }
        }

        void AddWall(Vector2Int c, int edge, float[] h)
        {
            var tile = map.At(c);
            var n = c + Dirs[edge];
            float[] other;
            bool border = !map.InGrid(n);
            if (border) other = new[] { BaseDepth, BaseDepth, BaseDepth, BaseDepth };
            else other = Corners(n);

            int a0 = EdgeCorners[edge][0], a1 = EdgeCorners[edge][1];
            float top0 = tile.Type == TileType.Water ? WaterLevel : h[a0];
            float top1 = tile.Type == TileType.Water ? WaterLevel : h[a1];
            float bot0, bot1;
            if (border)
            {
                bot0 = bot1 = BaseDepth;
            }
            else
            {
                // The neighbour's matching corners on the shared edge.
                int opposite = (edge + 2) % 4;
                int b0 = EdgeCorners[opposite][1], b1 = EdgeCorners[opposite][0];
                bot0 = map.At(n).Type == TileType.Water ? -0.9f : other[b0];
                bot1 = map.At(n).Type == TileType.Water ? -0.9f : other[b1];
            }
            if (top0 <= bot0 + 0.01f && top1 <= bot1 + 0.01f) return;
            bot0 = Mathf.Min(bot0, top0);
            bot1 = Mathf.Min(bot1, top1);

            var layer = tile.Type == TileType.Bridge ? Layer.Wood : Layer.Cliff;
            var p = parts[(int)layer];
            Vector3 t0 = CornerPos(c, a0, top0), t1 = CornerPos(c, a1, top1);
            Vector3 l0 = CornerPos(c, a0, bot0), l1 = CornerPos(c, a1, bot1);
            int b = p.V.Count;
            p.V.Add(l0); p.V.Add(l1); p.V.Add(t1); p.V.Add(t0);
            float u0 = (t0.x + t0.z) * 0.25f, u1 = (t1.x + t1.z) * 0.25f;
            p.UV.Add(new Vector2(u0, l0.y * 0.3f));
            p.UV.Add(new Vector2(u1, l1.y * 0.3f));
            p.UV.Add(new Vector2(u1, t1.y * 0.3f));
            p.UV.Add(new Vector2(u0, t0.y * 0.3f));
            Color dark = new Color(0.6f, 0.58f, 0.66f), lit = Color.white;
            p.C.Add(dark); p.C.Add(dark); p.C.Add(lit); p.C.Add(lit);
            p.I.Add(b); p.I.Add(b + 2); p.I.Add(b + 1);
            p.I.Add(b); p.I.Add(b + 3); p.I.Add(b + 2);
        }
    }
}
