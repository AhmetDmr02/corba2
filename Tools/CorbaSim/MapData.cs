using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using Corba.Sim;

namespace Corba.Tools
{
    public sealed class MapData
    {
        public int Seed;
        public Vector2[] Road;
        public float[] RoadDist;
        public SimPad[] Pads;
        public float[] PadWalk;
        public float MarketToOcak;
        public float[] MarketToRoad;
        public float RoadLength => RoadDist[RoadDist.Length - 1];

        public SimWorld NewWorld() => new SimWorld(new SimRoad(Road, RoadDist), Pads);

        /// <summary>Walking distance from the market to the road near distance d.</summary>
        public float MarketWalkTo(float d)
        {
            int i = System.Math.Clamp((int)(d / 4f), 0, MarketToRoad.Length - 1);
            return MarketToRoad[i];
        }

        public static List<MapData> Load(string path)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var maps = new List<MapData>();
            foreach (var m in doc.RootElement.EnumerateArray())
            {
                var road = m.GetProperty("road");
                int n = road.GetArrayLength();
                var pts = new Vector2[n];
                var dist = new float[n];
                int i = 0;
                foreach (var p in road.EnumerateArray())
                {
                    pts[i] = new Vector2(p[0].GetSingle(), p[1].GetSingle());
                    dist[i] = p[2].GetSingle();
                    i++;
                }
                var padsEl = m.GetProperty("pads");
                var pads = new SimPad[padsEl.GetArrayLength()];
                var walk = new float[pads.Length];
                i = 0;
                foreach (var p in padsEl.EnumerateArray())
                {
                    pads[i] = new SimPad
                    {
                        Tower = new Vector2(p.GetProperty("x").GetSingle(), p.GetProperty("z").GetSingle()),
                        RoadDistance = p.GetProperty("d").GetSingle(),
                        HighGround = p.GetProperty("high").GetBoolean(),
                    };
                    walk[i] = p.GetProperty("walk").GetSingle();
                    i++;
                }
                var mtr = new List<float>();
                foreach (var v in m.GetProperty("marketToRoad").EnumerateArray()) mtr.Add(v.GetSingle());
                maps.Add(new MapData
                {
                    Seed = m.GetProperty("seed").GetInt32(),
                    Road = pts,
                    RoadDist = dist,
                    Pads = pads,
                    PadWalk = walk,
                    MarketToOcak = m.GetProperty("marketToOcak").GetSingle(),
                    MarketToRoad = mtr.ToArray(),
                });
            }
            return maps;
        }
    }
}
