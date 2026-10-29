#if UNITY_EDITOR
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Corba
{
    /// <summary>Bakes generated maps and writes the geometry the headless Monte Carlo tool needs.</summary>
    public static class MapExporter
    {
        public static void Export(int count, string path)
        {
            var map = CorbaMap.Instance;
            var sb = new StringBuilder();
            var ci = CultureInfo.InvariantCulture;
            string F(float v) => v.ToString("0.###", ci);
            sb.Append("[\n");
            int totalTried = 0, totalFailed = 0;
            for (int m = 0; m < count; m++)
            {
                int seed = 1000 + m * 7919;
                map.Build(seed);
                var nav = map.Nav;
                var kitchen = map.DefenderSpawn;
                Vector3 market = map.CellCenter(map.MarketCell);
                market.y = map.Height(market);
                var ocak = map.Ocak.Position;

                sb.Append("{\"seed\":").Append(seed).Append(",\"road\":[");
                for (int i = 0; i < map.RoadPoints.Count; i += 2)
                {
                    var p = map.RoadPoints[i];
                    if (i > 0) sb.Append(',');
                    sb.Append('[').Append(F(p.x)).Append(',').Append(F(p.z)).Append(',').Append(F(map.RoadDistances[i])).Append(']');
                }
                int last = map.RoadPoints.Count - 1;
                if (last % 2 != 0)
                    sb.Append(",[").Append(F(map.RoadPoints[last].x)).Append(',').Append(F(map.RoadPoints[last].z)).Append(',').Append(F(map.RoadDistances[last])).Append(']');
                sb.Append("],\"pads\":[");
                for (int i = 0; i < map.Pads.Count; i++)
                {
                    var pad = map.Pads[i];
                    float walk = nav.PathLength(kitchen, pad.TowerPosition);
                    if (walk < 0f) walk = CorbaMap.FlatDist(kitchen, pad.TowerPosition) * 1.6f;
                    if (i > 0) sb.Append(',');
                    sb.Append("{\"x\":").Append(F(pad.TowerPosition.x)).Append(",\"z\":").Append(F(pad.TowerPosition.z))
                      .Append(",\"d\":").Append(F(pad.RoadDistance)).Append(",\"high\":").Append(pad.HighGround ? "true" : "false")
                      .Append(",\"walk\":").Append(F(walk)).Append('}');
                }
                float marketToOcak = nav.PathLength(market, ocak);
                if (marketToOcak < 0f) marketToOcak = CorbaMap.FlatDist(market, ocak) * 1.6f;
                sb.Append("],\"marketToOcak\":").Append(F(marketToOcak)).Append(",\"marketToRoad\":[");
                bool first = true;
                for (float d = 0f; d <= map.RoadLength; d += 4f)
                {
                    var p = map.SampleRoad(d, 2.4f);
                    if (!nav.IsWalkable(p)) p = map.SampleRoad(d, -2.4f);
                    float w = nav.PathLength(market, p);
                    if (w < 0f) w = CorbaMap.FlatDist(market, p) * 1.6f;
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append(F(w));
                }
                sb.Append("]}");
                sb.Append(m < count - 1 ? ",\n" : "\n");
                int tried = 0, failed = 0;
                var fails = new System.Collections.Generic.List<(Vector3, Vector3)>();
                void Try(Vector3 from, Vector3 to)
                {
                    tried++;
                    if (nav.FindPath(from, to).Count != 0) return;
                    failed++;
                    fails.Add((from, to));
                    if (failed <= 6) Debug.Log("[Corba] nofail " + nav.Explain(from, to));
                }
                foreach (var pad in map.Pads) Try(kitchen, pad.TowerPosition);
                foreach (var st in map.Stations)
                {
                    if (st.Kind == StationKind.DefenderCauldron) Try(map.DefenderSpawn, st.Position);
                    if (st.Kind == StationKind.AttackerCauldron) { Try(map.AttackerSpawn, st.Position); Try(st.Position, ocak); }
                }
                Try(ocak, market);
                for (float d = 0f; d <= map.RoadLength; d += 8f) Try(market, map.SampleRoad(d, 2.4f));
                if (failed > 0)
                {
                    const int scale = 8;
                    var tex = nav.DebugTexture(scale);
                    foreach (var (a, b) in fails)
                    {
                        nav.Mark(tex, a, Color.cyan, scale, 3);
                        nav.Mark(tex, b, Color.red, scale, 3);
                    }
                    tex.Apply();
                    var dir = Path.Combine(Path.GetDirectoryName(path), "navdebug");
                    Directory.CreateDirectory(dir);
                    File.WriteAllBytes(Path.Combine(dir, $"map_{m:00}_{seed}.png"), tex.EncodeToPNG());
                    Object.Destroy(tex);
                }
                totalTried += tried;
                totalFailed += failed;
                Debug.Log($"[Corba] exported map {m + 1}/{count} seed {seed} road {map.RoadLength:0} pads {map.Pads.Count} paths {tried - failed}/{tried}");
            }
            sb.Append("]\n");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, sb.ToString());
            Debug.Log($"[Corba] maps written to {path}; path check {totalTried - totalFailed}/{totalTried} routes found");
        }
    }
}
#endif
