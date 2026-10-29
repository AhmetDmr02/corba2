using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Corba.Tools
{
    public sealed class Tuner
    {
        const int PopSize = 64;
        const float UnitBonusLadles = 60f;
        // A defence must fit in one pot (100 ladles), with room for a little spill.
        const int MaxDefenceCost = 95;

        readonly List<MapData> maps;
        readonly int samples;
        readonly int seed;
        List<AttackerPolicy> attackers = new List<AttackerPolicy>();
        List<DefenderPolicy> defenders = new List<DefenderPolicy>();
        // Fictitious play: elites from earlier iterations stay in the measuring pool so prices track the whole meta, not its latest swing.
        readonly List<AttackerPolicy> attackerArchive = new List<AttackerPolicy>();
        readonly List<DefenderPolicy> defenderArchive = new List<DefenderPolicy>();
        const int ArchiveSize = 96;
        long simsRun;
        readonly StringBuilder log = new StringBuilder();

        public Tuner(List<MapData> maps, int samples, int seed)
        {
            this.maps = maps;
            this.samples = samples;
            this.seed = seed;
            var r = new Random(seed);
            for (int i = 0; i < PopSize; i++)
            {
                attackers.Add(AttackerPolicy.Random(r));
                defenders.Add(DefenderPolicy.Random(r));
            }
        }

        void Log(string s)
        {
            Console.WriteLine(s);
            log.AppendLine(s);
        }

        public sealed class Measurement
        {
            public double MeanFraction, FellRate;
            public double[] UnitValue = new double[Balance.RecruitableUnitCount];
            public double[] DefValue = new double[Balance.DefenceCount];
            public double[] LadleValue = new double[5];
            public double[] UnitShare = new double[Balance.RecruitableUnitCount];
            public double[] DefShare = new double[Balance.DefenceCount];
            public double[] LadleShare = new double[5];
            public double UnitsPerHalf, BuildsPerHalf;
        }

        static int Mix(int a, int b) => unchecked(a * 73856093 ^ b * 19349663);

        #region Co-evolution

        public void Evolve(int generations, int iteration)
        {
            for (int g = 0; g < generations; g++)
            {
                foreach (var a in attackers) { a.Fitness = 0; a.Games = 0; }
                foreach (var d in defenders) { d.Fitness = 0; d.Games = 0; }
                int games = PopSize * 16;
                var pairs = new (int a, int d, int map, int s)[games];
                var pr = new Random(Mix(seed, iteration * 100 + g));
                for (int i = 0; i < games; i++) pairs[i] = (i % PopSize, pr.Next(PopSize), pr.Next(maps.Count), pr.Next());
                var fractions = new float[games];
                Parallel.For(0, games, i =>
                {
                    var p = pairs[i];
                    fractions[i] = HalfSim.Play(maps[p.map], defenders[p.d], attackers[p.a], p.s, Bonus.None).Fraction;
                });
                Interlocked.Add(ref simsRun, games);
                for (int i = 0; i < games; i++)
                {
                    var p = pairs[i];
                    attackers[p.a].Fitness += fractions[i];
                    attackers[p.a].Games++;
                    defenders[p.d].Fitness -= fractions[i];
                    defenders[p.d].Games++;
                }
                attackers = Select(attackers, x => x.Fitness / Math.Max(1, x.Games), (x, r) => x.Mutate(r, 0.35f), pr);
                defenders = Select(defenders, x => x.Fitness / Math.Max(1, x.Games), (x, r) => x.Mutate(r, 0.35f), pr);
                for (int k = 0; k < 6; k++)
                {
                    attackers[PopSize - 1 - k] = AttackerPolicy.Random(pr);
                    defenders[PopSize - 1 - k] = DefenderPolicy.Random(pr);
                }
            }
        }

        static List<T> Select<T>(List<T> pop, Func<T, double> fit, Func<T, Random, T> mutate, Random r)
        {
            var sorted = pop.OrderByDescending(fit).ToList();
            var next = sorted.Take(pop.Count / 2).ToList();
            int elite = next.Count;
            while (next.Count < pop.Count)
            {
                var parent = next[r.Next(Math.Max(1, elite / 2))];
                next.Add(mutate(parent, r));
            }
            if (r.NextDouble() < 0.5) next[next.Count - 1] = mutate(next[next.Count - 1], r);
            return next;
        }

        #endregion

        #region Measurement

        void Archive()
        {
            attackerArchive.AddRange(attackers.Take(8).Select(a => a.Mutate(new Random(1), 0f)));
            defenderArchive.AddRange(defenders.Take(8).Select(d => d.Mutate(new Random(1), 0f)));
            if (attackerArchive.Count > ArchiveSize) attackerArchive.RemoveRange(0, attackerArchive.Count - ArchiveSize);
            if (defenderArchive.Count > ArchiveSize) defenderArchive.RemoveRange(0, defenderArchive.Count - ArchiveSize);
        }

        public Measurement Measure(int iteration)
        {
            var m = new Measurement();
            Archive();
            var poolA = attackers.Take(16).Concat(attackerArchive).ToList();
            var poolD = defenders.Take(16).Concat(defenderArchive).ToList();
            int eliteA = poolA.Count, eliteD = poolD.Count;
            int arms = 1 + Balance.RecruitableUnitCount + Balance.DefenceCount + 5;
            var outcome = new float[samples, arms];
            var ghostSpent = new float[samples, 5];
            var baseResults = new HalfResult[samples];
            var r = new Random(Mix(seed, 7777 + iteration));
            var draws = new (int a, int d, int map, int s)[samples];
            for (int i = 0; i < samples; i++) draws[i] = (r.Next(eliteA), r.Next(eliteD), r.Next(maps.Count), r.Next());

            Parallel.For(0, samples * arms, job =>
            {
                int i = job / arms, arm = job % arms;
                var dr = draws[i];
                var bonus = Bonus.None;
                if (arm >= 1 && arm <= Balance.RecruitableUnitCount) { bonus.Unit = arm - 1; bonus.UnitLadles = UnitBonusLadles; }
                else if (arm > Balance.RecruitableUnitCount && arm <= Balance.RecruitableUnitCount + Balance.DefenceCount) bonus.Defence = arm - 1 - Balance.RecruitableUnitCount;
                else if (arm > Balance.RecruitableUnitCount + Balance.DefenceCount) bonus.LadleSoup = arm - 1 - Balance.RecruitableUnitCount - Balance.DefenceCount;
                var res = HalfSim.Play(maps[dr.map], poolD[dr.d], poolA[dr.a], dr.s, bonus);
                outcome[i, arm] = res.Score;
                if (bonus.LadleSoup >= 0) ghostSpent[i, bonus.LadleSoup] = res.GhostSpent;
                if (arm == 0) baseResults[i] = res;
            });
            Interlocked.Add(ref simsRun, (long)samples * arms);

            double unitSpend = 0, defSpend = 0, ladleSpend = 0;
            foreach (var b in baseResults)
            {
                m.MeanFraction += b.Fraction;
                m.FellRate += b.Fell ? 1 : 0;
                m.UnitsPerHalf += b.UnitsSent;
                m.BuildsPerHalf += b.Builds;
                for (int u = 0; u < m.UnitShare.Length; u++) { m.UnitShare[u] += b.UnitSpend[u]; unitSpend += b.UnitSpend[u]; }
                for (int d = 0; d < m.DefShare.Length; d++) { m.DefShare[d] += b.DefSpend[d]; defSpend += b.DefSpend[d]; }
                for (int s = 0; s < 5; s++) { m.LadleShare[s] += b.LadleSpend[s]; ladleSpend += b.LadleSpend[s]; }
            }
            m.MeanFraction /= samples;
            m.FellRate /= samples;
            m.UnitsPerHalf /= samples;
            m.BuildsPerHalf /= samples;
            for (int u = 0; u < m.UnitShare.Length; u++) m.UnitShare[u] /= Math.Max(1, unitSpend);
            for (int d = 0; d < m.DefShare.Length; d++) m.DefShare[d] /= Math.Max(1, defSpend);
            for (int s = 0; s < 5; s++) m.LadleShare[s] /= Math.Max(1, ladleSpend);

            for (int i = 0; i < samples; i++)
            {
                float b0 = outcome[i, 0];
                for (int u = 0; u < Balance.RecruitableUnitCount; u++) m.UnitValue[u] += (outcome[i, 1 + u] - b0) / UnitBonusLadles;
                for (int d = 0; d < Balance.DefenceCount; d++)
                    m.DefValue[d] += -(outcome[i, 1 + Balance.RecruitableUnitCount + d] - b0) / Balance.Defences[d].Cost;
                for (int s = 0; s < 5; s++) m.LadleValue[s] += (outcome[i, 1 + Balance.RecruitableUnitCount + Balance.DefenceCount + s] - b0) / Math.Max(10f, ghostSpent[i, s]);
            }
            for (int u = 0; u < m.UnitValue.Length; u++) m.UnitValue[u] /= samples;
            for (int d = 0; d < m.DefValue.Length; d++) m.DefValue[d] /= samples;
            for (int s = 0; s < 5; s++) m.LadleValue[s] /= samples;
            return m;
        }

        #endregion

        #region Adjustment

        static double Cv(double[] v)
        {
            double mean = v.Average();
            if (Math.Abs(mean) < 1e-9) return 0;
            return Math.Sqrt(v.Select(x => (x - mean) * (x - mean)).Average()) / Math.Abs(mean);
        }

        static double Median(double[] v)
        {
            var o = v.OrderBy(x => x).ToArray();
            return o.Length % 2 == 1 ? o[o.Length / 2] : (o[o.Length / 2 - 1] + o[o.Length / 2]) * 0.5;
        }

        /// <summary>Value relative to the median, floored so dead options get cheaper gradually instead of exploding the update.</summary>
        static double Ratio(double v, double median) => Math.Max(v, 0.3 * median) / median;

        /// <summary>Scales what a defence actually does when its cost can't move any further.</summary>
        static void Power(ref DefenceInfo info, float k)
        {
            switch (info.Kind)
            {
                case DefenceKind.Blocker: info.Hp *= k; break;
                case DefenceKind.Tower: info.Damage *= k; break;
                default:
                    if (info.Damage > 0f) info.Damage *= k;
                    else info.Magnitude = Math.Min(0.8f, info.Magnitude * k);
                    if (info.Duration > 0f) info.Duration *= (float)Math.Sqrt(k);
                    break;
            }
        }

        bool adjustHunger = true;

        public void Adjust(Measurement m, float rate)
        {
            double meanU = Math.Max(1e-4, Median(m.UnitValue));
            double fairU = 1.0 / m.UnitValue.Length;
            for (int u = 0; u < Balance.RecruitableUnitCount; u++)
            {
                double ratio = Ratio(m.UnitValue[u], meanU);
                double share = m.UnitShare[u] / fairU;
                double f = Math.Pow(ratio, 0.5 * rate) * (share > 2.0 ? Math.Pow(share / 2.0, 0.15 * rate) : 1.0);
                f = Math.Clamp(f, 1 - 0.15 * rate, 1 + 0.18 * rate);
                ref var info = ref Balance.Units[u];
                int newCost = Math.Clamp((int)Math.Round(info.Cost * f), 8, 140);
                if (newCost == info.Cost && f < 0.97 && info.Cost <= 8) info.Hp *= 1.08f;
                if (newCost == info.Cost && f > 1.03 && info.Cost >= 140) info.Hp *= 0.93f;
                info.Cost = newCost;
            }

            double meanD = Math.Max(1e-4, Median(m.DefValue));
            double fairD = 1.0 / m.DefValue.Length;
            for (int d = 0; d < Balance.DefenceCount; d++)
            {
                double ratio = Ratio(m.DefValue[d], meanD);
                double share = m.DefShare[d] / fairD;
                double f = Math.Pow(ratio, 0.5 * rate) * (share > 2.0 ? Math.Pow(share / 2.0, 0.15 * rate) : 1.0);
                f = Math.Clamp(f, 1 - 0.15 * rate, 1 + 0.18 * rate);
                ref var info = ref Balance.Defences[d];
                int newCost = Math.Clamp((int)Math.Round(info.Cost * f), 25, MaxDefenceCost);
                if (newCost == info.Cost && f < 0.97 && info.Cost <= 25) Power(ref info, 1.1f);
                if (newCost == info.Cost && f > 1.03 && info.Cost >= MaxDefenceCost) Power(ref info, 0.92f);
                info.Cost = newCost;
            }

            double meanL = Math.Max(1e-4, Median(m.LadleValue));
            for (int s = 0; s < 5; s++)
            {
                double f = Math.Clamp(Math.Pow(1.0 / Ratio(m.LadleValue[s], meanL), 0.4 * rate), 1 - 0.12 * rate, 1 + 0.15 * rate);
                ref var l = ref Balance.Ladles[s];
                switch ((SoupType)s)
                {
                    case SoupType.Mercimek: l.Magnitude = (float)Math.Clamp(l.Magnitude * f, 0.15, 0.75); break;
                    case SoupType.Ezogelin: l.Magnitude = (float)Math.Clamp(l.Magnitude * f, 0.15, 1.2); break;
                    case SoupType.Yayla: l.Magnitude = (float)Math.Clamp(l.Magnitude * f, 25, 260); break;
                    case SoupType.Tarhana: l.Magnitude = (float)Math.Clamp(l.Magnitude * f, 0.15, 0.85); break;
                    default: l.Magnitude = (float)Math.Clamp(l.Magnitude * f, 0.3, 3); break;
                }
            }

            double lu = Math.Max(1e-4, Median(m.LadleValue)) / meanU;
            Balance.LadleCost = Math.Clamp((int)Math.Round(Balance.LadleCost * Math.Clamp(Math.Pow(lu, 0.5 * rate), 0.85, 1.15)), 5, 30);

            const double target = 0.55;
            double hf = Math.Clamp(Math.Pow(Math.Max(0.05, m.MeanFraction) / target, 0.6 * rate), 0.88, 1.12);
            if (adjustHunger) Balance.SofraHunger = (float)Math.Clamp(Math.Round(Balance.SofraHunger * hf), 30, 400);
        }

        #endregion

        public void Run(int iterations, string tuningPath, string reportPath)
        {
            var start = DateTime.Now;
            Log($"Corba Monte Carlo tuner: {maps.Count} maps, {samples} samples/iteration, {Environment.ProcessorCount} threads");
            Measurement m = null;
            for (int it = 0; it < iterations; it++)
            {
                Evolve(it == 0 ? 8 : 4, it);
                m = Measure(it);
                Log($"\n== iteration {it + 1}/{iterations}  ({simsRun:N0} sims, {(DateTime.Now - start).TotalSeconds:0.0}s)");
                Log($"raid fraction {m.MeanFraction:0.000}  sofra fell {m.FellRate:P0}  units/half {m.UnitsPerHalf:0.0}  builds/half {m.BuildsPerHalf:0.0}  sofra {Balance.SofraHunger}");
                Log($"value spread: units CV {Cv(m.UnitValue):0.00}  defences CV {Cv(m.DefValue):0.00}  ladles CV {Cv(m.LadleValue):0.00}");
                Log("units:    " + string.Join("  ", Enumerable.Range(0, 10).Select(u => $"{Balance.Units[u].English.Split(' ').Last()} {Balance.Units[u].Cost} v{m.UnitValue[u] * 100:0.0} s{m.UnitShare[u]:P0}")));
                Log("defences: " + string.Join("  ", Enumerable.Range(0, 10).Select(d => $"{Balance.Defences[d].English.Split(' ').Last()} {Balance.Defences[d].Cost} v{m.DefValue[d] * 100:0.0} s{m.DefShare[d]:P0}")));
                Log("ladles:   " + string.Join("  ", Enumerable.Range(0, 5).Select(s => $"{Balance.Ladles[s].Name} v{m.LadleValue[s] * 100:0.0} s{m.LadleShare[s]:P0}")));
                adjustHunger = it < iterations * 9 / 10;
                if (it < iterations - 1) Adjust(m, (float)(1.0 / Math.Sqrt(1.0 + it / 3.0)));
            }
            for (int c = 0; c < 3; c++)
            {
                double scale = Math.Clamp(m.MeanFraction / 0.55, 0.6, 1.6);
                Balance.SofraHunger = (float)Math.Clamp(Math.Round(Balance.SofraHunger * scale), 30, 400);
                m = Measure(iterations + c);
                Log($"calibration {c + 1}: sofra {Balance.SofraHunger} -> raid fraction {m.MeanFraction:0.000}, fell {m.FellRate:P0}");
            }
            WriteTuning(tuningPath, m);
            WriteReport(reportPath, m, DateTime.Now - start);
            Log($"\nwrote {tuningPath}\nwrote {reportPath}\ntotal {simsRun:N0} simulated halves in {(DateTime.Now - start).TotalSeconds:0.0}s");
        }

        static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture) + "f";

        void WriteTuning(string path, Measurement m)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// Generated by Tools/CorbaSim. Do not edit by hand; rerun the tuner instead.");
            sb.AppendLine($"// {DateTime.Now:yyyy-MM-dd HH:mm}  raid fraction {m.MeanFraction:0.000}  sims {simsRun:N0}");
            sb.AppendLine("namespace Corba");
            sb.AppendLine("{");
            sb.AppendLine("    public static partial class Balance");
            sb.AppendLine("    {");
            sb.AppendLine("        static partial void ApplyTuning()");
            sb.AppendLine("        {");
            sb.AppendLine($"            SofraHunger = {F(Balance.SofraHunger)};");
            sb.AppendLine($"            LadleCost = {Balance.LadleCost};");
            for (int u = 0; u < Balance.RecruitableUnitCount; u++)
            {
                sb.AppendLine($"            Units[(int)UnitId.{(UnitId)u}].Cost = {Balance.Units[u].Cost};");
                sb.AppendLine($"            Units[(int)UnitId.{(UnitId)u}].Hp = {F(Balance.Units[u].Hp)};");
            }
            for (int d = 0; d < Balance.DefenceCount; d++)
            {
                var x = Balance.Defences[d];
                sb.AppendLine($"            Defences[(int)DefenceId.{(DefenceId)d}].Cost = {x.Cost};");
                sb.AppendLine($"            Defences[(int)DefenceId.{(DefenceId)d}].Hp = {F(x.Hp)};");
                sb.AppendLine($"            Defences[(int)DefenceId.{(DefenceId)d}].Damage = {F(x.Damage)};");
                sb.AppendLine($"            Defences[(int)DefenceId.{(DefenceId)d}].Magnitude = {F(x.Magnitude)};");
                sb.AppendLine($"            Defences[(int)DefenceId.{(DefenceId)d}].Duration = {F(x.Duration)};");
            }
            for (int s = 0; s < 5; s++)
            {
                sb.AppendLine($"            Ladles[{s}].Magnitude = {F(Balance.Ladles[s].Magnitude)};");
                sb.AppendLine($"            Ladles[{s}].Duration = {F(Balance.Ladles[s].Duration)};");
            }
            double mu = Math.Max(1e-4, m.UnitValue.Average());
            for (int u = 0; u < Balance.RecruitableUnitCount; u++)
                sb.AppendLine($"            AiUnitValue[{u}] = {F(Math.Clamp(m.UnitValue[u] / mu, 0.2, 3))};");
            double md = Math.Max(1e-4, Enumerable.Range(0, 10).Average(d => m.DefValue[d] * Balance.Defences[d].Cost));
            for (int d = 0; d < Balance.DefenceCount; d++)
                sb.AppendLine($"            AiDefenceValue[{d}] = {F(Math.Clamp(m.DefValue[d] * Balance.Defences[d].Cost / md, 0.2, 3))};");
            double ml = Math.Max(1e-4, m.LadleValue.Average());
            for (int s = 0; s < 5; s++)
                sb.AppendLine($"            AiLadleValue[{s}] = {F(Math.Clamp(m.LadleValue[s] / ml, 0.2, 3))};");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            File.WriteAllText(path, sb.ToString());
        }

        void WriteReport(string path, Measurement m, TimeSpan took)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Corba balance report");
            sb.AppendLine();
            sb.AppendLine($"Generated {DateTime.Now:yyyy-MM-dd HH:mm} by `Tools/CorbaSim` ({simsRun:N0} simulated halves in {took.TotalSeconds:0}s on {Environment.ProcessorCount} threads, {maps.Count} real exported maps).");
            sb.AppendLine();
            sb.AppendLine("**Method.** Attacker and defender strategy populations co-evolve against each other, so the numbers describe what *good* play does, not random play. " +
                          "Each value is a paired A/B test on the same matchup and seed: give one side a little extra of one thing and measure the change in raid score. " +
                          "Costs are then nudged until every option is worth about the same per ladle of soup.");
            sb.AppendLine();
            sb.AppendLine($"- Mean raid score: **{m.MeanFraction:P0}** of the Sofra (target 55%); Sofra emptied in {m.FellRate:P0} of halves; Sofra hunger {Balance.SofraHunger}");
            sb.AppendLine($"- Per half: {m.UnitsPerHalf:0.0} raiders sent, {m.BuildsPerHalf:0.0} defences built; ladle toss costs {Balance.LadleCost}");
            sb.AppendLine($"- Value spread (coefficient of variation, lower is more even): units {Cv(m.UnitValue):0.00}, defences {Cv(m.DefValue):0.00}, ladles {Cv(m.LadleValue):0.00}");
            sb.AppendLine();
            sb.AppendLine("## Attack units");
            sb.AppendLine();
            sb.AppendLine("| Unit | Soup | Cost | Hunger per 100 ladles | Share of winning attackers' spend |");
            sb.AppendLine("|---|---|---|---|---|");
            for (int u = 0; u < 10; u++)
                sb.AppendLine($"| {Balance.Units[u].English} | {Balance.Units[u].Soup} | {Balance.Units[u].Cost} | {m.UnitValue[u] * 100:0.0} | {m.UnitShare[u]:P0} |");
            sb.AppendLine();
            sb.AppendLine("## Defences");
            sb.AppendLine();
            sb.AppendLine("| Defence | Soup | Kind | Cost | Hunger saved per 100 ladles | Share of winning defenders' spend |");
            sb.AppendLine("|---|---|---|---|---|---|");
            for (int d = 0; d < 10; d++)
                sb.AppendLine($"| {Balance.Defences[d].English} | {Balance.Defences[d].Soup} | {Balance.Defences[d].Kind} | {Balance.Defences[d].Cost} | {m.DefValue[d] * 100:0.0} | {m.DefShare[d]:P0} |");
            sb.AppendLine();
            sb.AppendLine("## Ladle tosses");
            sb.AppendLine();
            sb.AppendLine("| Soup | Effect | Magnitude | Duration | Hunger per 100 ladles thrown | Share of tosses |");
            sb.AppendLine("|---|---|---|---|---|---|");
            for (int s = 0; s < 5; s++)
                sb.AppendLine($"| {(SoupType)s} | {Balance.Ladles[s].Name} | {Balance.Ladles[s].Magnitude:0.##} | {Balance.Ladles[s].Duration:0.#}s | {m.LadleValue[s] * 100:0.0} | {m.LadleShare[s]:P0} |");
            sb.AppendLine();
            sb.AppendLine("## Tuning log");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.Append(log);
            sb.AppendLine("```");
            File.WriteAllText(path, sb.ToString());
        }
    }
}
