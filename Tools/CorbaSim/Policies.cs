using System;

namespace Corba.Tools
{
    /// <summary>A defender strategy: how much it likes each build, where it builds, how it adapts.</summary>
    public sealed class DefenderPolicy
    {
        public float[] BuildWeight = new float[Balance.DefenceCount];
        public float FrontBias;      // -1 favours pads near the Sofra, +1 favours early pads
        public float HighGroundBonus;
        public float UpgradeBias;
        public float Counter;
        public float TravelWeight;
        public float Skill;          // spill multiplier: lower is a steadier carrier
        public double Fitness;
        public int Games;

        public static DefenderPolicy Random(Random r)
        {
            var p = new DefenderPolicy();
            for (int i = 0; i < p.BuildWeight.Length; i++) p.BuildWeight[i] = (float)Math.Exp(r.NextDouble() * 2.4 - 1.2);
            p.FrontBias = (float)(r.NextDouble() * 2 - 1);
            p.HighGroundBonus = (float)(r.NextDouble() * 0.6);
            p.UpgradeBias = (float)(r.NextDouble() * 1.2);
            p.Counter = (float)r.NextDouble();
            p.TravelWeight = (float)(r.NextDouble() * 0.03);
            p.Skill = 1f;
            return p;
        }

        public DefenderPolicy Mutate(Random r, float s)
        {
            var p = (DefenderPolicy)MemberwiseClone();
            p.BuildWeight = (float[])BuildWeight.Clone();
            for (int i = 0; i < p.BuildWeight.Length; i++) p.BuildWeight[i] *= (float)Math.Exp((r.NextDouble() * 2 - 1) * s);
            p.FrontBias = Math.Clamp(p.FrontBias + (float)(r.NextDouble() * 2 - 1) * s * 0.5f, -1f, 1f);
            p.HighGroundBonus = Math.Clamp(p.HighGroundBonus + (float)(r.NextDouble() * 2 - 1) * s * 0.3f, 0f, 1f);
            p.UpgradeBias = Math.Clamp(p.UpgradeBias + (float)(r.NextDouble() * 2 - 1) * s * 0.5f, 0f, 2f);
            p.Counter = Math.Clamp(p.Counter + (float)(r.NextDouble() * 2 - 1) * s * 0.4f, 0f, 1f);
            p.TravelWeight = Math.Clamp(p.TravelWeight + (float)(r.NextDouble() * 2 - 1) * s * 0.01f, 0f, 0.05f);
            p.Fitness = 0;
            p.Games = 0;
            return p;
        }
    }

    /// <summary>An attacker strategy: unit mix, wave size, how often it escorts and which soups it throws.</summary>
    public sealed class AttackerPolicy
    {
        public float[] UnitWeight = new float[Balance.RecruitableUnitCount];
        public float[] LadleWeight = new float[5];
        public float SupportShare;
        public int WaveSize;
        public float TossThreshold;
        public float Counter;
        public float Skill;
        public double Fitness;
        public int Games;

        public static AttackerPolicy Random(Random r)
        {
            var p = new AttackerPolicy();
            for (int i = 0; i < p.UnitWeight.Length; i++) p.UnitWeight[i] = (float)Math.Exp(r.NextDouble() * 2.4 - 1.2);
            for (int i = 0; i < 5; i++) p.LadleWeight[i] = (float)Math.Exp(r.NextDouble() * 2 - 1);
            p.SupportShare = (float)(r.NextDouble() * 0.6);
            p.WaveSize = 3 + r.Next(10);
            p.TossThreshold = (float)(1.5 + r.NextDouble() * 2.5);
            p.Counter = (float)r.NextDouble();
            p.Skill = 1f;
            return p;
        }

        public AttackerPolicy Mutate(Random r, float s)
        {
            var p = (AttackerPolicy)MemberwiseClone();
            p.UnitWeight = (float[])UnitWeight.Clone();
            p.LadleWeight = (float[])LadleWeight.Clone();
            for (int i = 0; i < p.UnitWeight.Length; i++) p.UnitWeight[i] *= (float)Math.Exp((r.NextDouble() * 2 - 1) * s);
            for (int i = 0; i < 5; i++) p.LadleWeight[i] *= (float)Math.Exp((r.NextDouble() * 2 - 1) * s);
            p.SupportShare = Math.Clamp(p.SupportShare + (float)(r.NextDouble() * 2 - 1) * s * 0.2f, 0f, 0.8f);
            p.WaveSize = Math.Clamp(p.WaveSize + r.Next(-2, 3), 2, 14);
            p.TossThreshold = Math.Clamp(p.TossThreshold + (float)(r.NextDouble() * 2 - 1) * s, 1f, 5f);
            p.Counter = Math.Clamp(p.Counter + (float)(r.NextDouble() * 2 - 1) * s * 0.4f, 0f, 1f);
            p.Fitness = 0;
            p.Games = 0;
            return p;
        }
    }
}
