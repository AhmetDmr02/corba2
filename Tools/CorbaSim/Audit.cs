using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Corba.Tools
{
    /// <summary>
    /// Stress-tests the current balance instead of trusting the tuner: how varied is the evolved meta,
    /// how far can a dedicated counter-strategy beat it, and does any single soup carry a side on its own.
    /// </summary>
    public sealed class Audit
    {
        readonly List<MapData> maps;
        readonly int seed;
        readonly StringBuilder md = new StringBuilder();

        public Audit(List<MapData> maps, int seed)
        {
            this.maps = maps;
            this.seed = seed;
        }

        void Line(string s = "")
        {
            Console.WriteLine(s);
            md.AppendLine(s);
        }

        /// <summary>Mean attacker raid fraction of each attacker against a fixed set of defenders, on common seeds.</summary>
        double[] Score(IList<AttackerPolicy> atk, IList<DefenderPolicy> def, int games, int salt)
        {
            var sums = new double[atk.Count];
            Parallel.For(0, atk.Count * games, job =>
            {
                int a = job / games, g = job % games;
                var r = new Random(unchecked(seed * 7 + salt * 1000003 + g));
                var d = def[r.Next(def.Count)];
                var map = maps[r.Next(maps.Count)];
                double f = HalfSim.Play(map, d, atk[a], r.Next(), Bonus.None).Fraction;
                lock (sums) sums[a] += f;
            });
            return sums.Select(x => x / games).ToArray();
        }

        double[] ScoreDef(IList<DefenderPolicy> def, IList<AttackerPolicy> atk, int games, int salt)
        {
            var sums = new double[def.Count];
            Parallel.For(0, def.Count * games, job =>
            {
                int d = job / games, g = job % games;
                var r = new Random(unchecked(seed * 11 + salt * 1000003 + g));
                var a = atk[r.Next(atk.Count)];
                var map = maps[r.Next(maps.Count)];
                double f = HalfSim.Play(map, def[d], a, r.Next(), Bonus.None).Fraction;
                lock (sums) sums[d] += f;
            });
            return sums.Select(x => x / games).ToArray();
        }

        public void Run(string reportPath)
        {
            var start = DateTime.Now;
            var tuner = new Tuner(maps, 200, seed);
            for (int it = 0; it < 6; it++) tuner.Evolve(4, it);
            var (atkPool, defPool) = tuner.Elites(24);

            md.AppendLine("# Corba balance audit");
            md.AppendLine();
            md.AppendLine($"Generated {DateTime.Now:yyyy-MM-dd HH:mm} from the costs currently in `BalanceTuning.g.cs`. All numbers are attacker raid score as a fraction of the Sofra (higher is better for the attacker).");
            md.AppendLine();

            // Meta diversity from elite-vs-elite games.
            var unitSpend = new double[Balance.RecruitableUnitCount];
            var defSpend = new double[Balance.DefenceCount];
            var ladle = new double[5];
            double baseline = 0;
            int n = 2000;
            var results = new HalfResult[n];
            Parallel.For(0, n, i =>
            {
                var r = new Random(seed + i);
                results[i] = HalfSim.Play(maps[r.Next(maps.Count)], defPool[r.Next(defPool.Count)], atkPool[r.Next(atkPool.Count)], r.Next(), Bonus.None);
            });
            foreach (var res in results)
            {
                baseline += res.Fraction;
                for (int k = 0; k < unitSpend.Length; k++) unitSpend[k] += res.UnitSpend[k];
                for (int k = 0; k < defSpend.Length; k++) defSpend[k] += res.DefSpend[k];
                for (int k = 0; k < 5; k++) ladle[k] += res.LadleSpend[k];
            }
            baseline /= n;
            double Eff(double[] v) { double t = v.Sum(); return t <= 0 ? 0 : 1.0 / v.Sum(x => (x / t) * (x / t)); }
            var soupAtk = new double[5];
            var soupDef = new double[5];
            for (int u = 0; u < unitSpend.Length; u++) soupAtk[(int)Balance.Units[u].Soup] += unitSpend[u];
            for (int d = 0; d < defSpend.Length; d++) soupDef[(int)Balance.Defences[d].Soup] += defSpend[d];

            Line("## 1. How varied is the meta?");
            Line();
            Line($"Elite vs elite raid score: **{baseline:P0}**.");
            Line();
            Line("\"Effective options\" is 1 / sum of squared spend shares: 10 means every option is used equally, 1 means one option is everything.");
            Line();
            Line($"| | Effective options | Out of |");
            Line($"|---|---|---|");
            Line($"| Attack units | {Eff(unitSpend):0.0} | 10 |");
            Line($"| Defences | {Eff(defSpend):0.0} | 10 |");
            Line($"| Attacker soups | {Eff(soupAtk):0.0} | 5 |");
            Line($"| Defender soups | {Eff(soupDef):0.0} | 5 |");
            Line($"| Ladle soups | {Eff(ladle):0.0} | 5 |");
            Line();
            Line("| Soup | Attacker spend | Defender spend | Ladle tosses |");
            Line("|---|---|---|---|");
            for (int s = 0; s < 5; s++)
                Line($"| {(SoupType)s} | {soupAtk[s] / Math.Max(1, soupAtk.Sum()):P0} | {soupDef[s] / Math.Max(1, soupDef.Sum()):P0} | {ladle[s] / Math.Max(1, ladle.Sum()):P0} |");
            Line();

            // Exploitability: hill-climb a best response against the frozen pool.
            Line("## 2. Can a dedicated counter-strategy break the meta?");
            Line();
            var eliteAtk = Score(atkPool, defPool, 300, 1).Average();
            var eliteDef = ScoreDef(defPool, atkPool, 300, 2).Average();
            var rng = new Random(seed);
            var bestA = atkPool[0];
            double bestAScore = Score(new[] { bestA }, defPool, 600, 3)[0];
            for (int step = 0; step < 40; step++)
            {
                var cands = Enumerable.Range(0, 8).Select(_ => bestA.Mutate(rng, 0.6f)).ToArray();
                var scores = Score(cands, defPool, 300, 100 + step);
                int k = Array.IndexOf(scores, scores.Max());
                double confirm = Score(new[] { cands[k] }, defPool, 600, 3)[0];
                if (confirm > bestAScore) { bestA = cands[k]; bestAScore = confirm; }
            }
            var bestD = defPool[0];
            double bestDScore = ScoreDef(new[] { bestD }, atkPool, 600, 4)[0];
            for (int step = 0; step < 40; step++)
            {
                var cands = Enumerable.Range(0, 8).Select(_ => bestD.Mutate(rng, 0.6f)).ToArray();
                var scores = ScoreDef(cands, atkPool, 300, 200 + step);
                int k = Array.IndexOf(scores, scores.Min());
                double confirm = ScoreDef(new[] { cands[k] }, atkPool, 600, 4)[0];
                if (confirm < bestDScore) { bestD = cands[k]; bestDScore = confirm; }
            }
            Line($"- Average elite attacker scores **{eliteAtk:P0}**; the best counter-attacker found scores **{bestAScore:P0}** (+{(bestAScore - eliteAtk) * 100:0} points).");
            Line($"- Average elite defender concedes **{eliteDef:P0}**; the best counter-defender found concedes **{bestDScore:P0}** ({(bestDScore - eliteDef) * 100:0} points).");
            Line($"- Best counter-attacker spends most on: {Top(bestA.UnitWeight, u => Balance.Units[u].English)}. Waves of {bestA.WaveSize}, escorts {bestA.SupportShare:P0} of raid trips.");
            Line($"- Best counter-defender prefers: {Top(bestD.BuildWeight, d => Balance.Defences[d].English)}.");
            Line();
            Line("A gap under ~10 points means the meta is hard to exploit. A big gap means the elites are beatable by a specific plan, which is either a balance hole or a sign the bots are not playing well.");
            Line();

            // Mono-soup strategies.
            Line("## 3. Can one soup carry a side on its own?");
            Line();
            Line("| Soup | Attacker using only this soup | Defender using only this soup |");
            Line("|---|---|---|");
            var monoA = new List<AttackerPolicy>();
            var monoD = new List<DefenderPolicy>();
            for (int s = 0; s < 5; s++)
            {
                var a = atkPool[0].Mutate(rng, 0f);
                for (int u = 0; u < a.UnitWeight.Length; u++) a.UnitWeight[u] = (int)Balance.Units[u].Soup == s ? 1f : 0f;
                for (int l = 0; l < 5; l++) a.LadleWeight[l] = l == s ? 1f : 0f;
                monoA.Add(a);
                var d = defPool[0].Mutate(rng, 0f);
                for (int k = 0; k < d.BuildWeight.Length; k++) d.BuildWeight[k] = (int)Balance.Defences[k].Soup == s ? 1f : 0.0001f;
                monoD.Add(d);
            }
            var monoAScores = Score(monoA, defPool, 400, 5);
            var monoDScores = ScoreDef(monoD, atkPool, 400, 6);
            for (int s = 0; s < 5; s++)
                Line($"| {(SoupType)s} | {monoAScores[s]:P0} (elite {eliteAtk:P0}) | concedes {monoDScores[s]:P0} (elite {eliteDef:P0}) |");
            Line();
            Line("A single-soup plan should do clearly worse than a mixed elite. If one matches or beats the elites, that soup is a solved strategy.");
            Line();
            Line($"_Audit took {(DateTime.Now - start).TotalSeconds:0}s._");
            File.WriteAllText(reportPath, md.ToString());
        }

        static string Top(float[] w, Func<int, string> name)
        {
            double t = w.Sum();
            return string.Join(", ", w.Select((v, i) => (v, i)).OrderByDescending(x => x.v).Take(3).Select(x => $"{name(x.i)} {x.v / t:P0}"));
        }
    }
}
