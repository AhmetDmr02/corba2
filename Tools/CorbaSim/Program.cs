using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Corba.Tools
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            string root = FindRoot();
            string mapsPath = Path.Combine(root, "Tools", "CorbaSim", "maps.json");
            if (!File.Exists(mapsPath))
            {
                Console.Error.WriteLine($"maps.json not found at {mapsPath}. Export maps from Unity first (bridge: play export:40:1).");
                return 1;
            }
            var maps = MapData.Load(mapsPath);
            string cmd = args.Length > 0 ? args[0] : "tune";
            int Arg(string name, int def)
            {
                for (int i = 1; i < args.Length - 1; i++) if (args[i] == name && int.TryParse(args[i + 1], out var v)) return v;
                return def;
            }

            switch (cmd)
            {
                case "bench":
                {
                    int n = Arg("--sims", 20000);
                    var r = new Random(1);
                    var dps = new DefenderPolicy[64];
                    var aps = new AttackerPolicy[64];
                    for (int i = 0; i < 64; i++) { dps[i] = DefenderPolicy.Random(r); aps[i] = AttackerPolicy.Random(r); }
                    var sw = Stopwatch.StartNew();
                    double total = 0;
                    object gate = new object();
                    Parallel.For(0, n, () => 0.0, (i, _, acc) =>
                        acc + HalfSim.Play(maps[i % maps.Count], dps[i % 64], aps[(i * 7) % 64], i, Bonus.None).Fraction,
                        acc => { lock (gate) total += acc; });
                    sw.Stop();
                    Console.WriteLine($"{n:N0} halves in {sw.Elapsed.TotalSeconds:0.00}s = {n / sw.Elapsed.TotalSeconds:N0} halves/s; mean raid fraction {total / n:0.000}");
                    return 0;
                }
                case "diag":
                {
                    int n = Arg("--sims", 4000);
                    var r = new Random(3);
                    double frac = 0, sent = 0, builds = 0, atk = 0, def = 0, fell = 0;
                    var unitSpend = new double[10];
                    var defSpend = new double[10];
                    var ladle = new double[5];
                    var defDmg = new double[10];
                    for (int i = 0; i < n; i++)
                    {
                        var res = HalfSim.Play(maps[i % maps.Count], DefenderPolicy.Random(r), AttackerPolicy.Random(r), i, Bonus.None);
                        frac += res.Fraction; sent += res.UnitsSent; builds += res.Builds; atk += res.AttackerDelivered; def += res.DefenderDelivered; fell += res.Fell ? 1 : 0;
                        for (int k = 0; k < 10; k++) { unitSpend[k] += res.UnitSpend[k]; defSpend[k] += res.DefSpend[k]; defDmg[k] += res.DefDamage[k]; }
                        for (int k = 0; k < 5; k++) ladle[k] += res.LadleSpend[k];
                    }
                    Console.WriteLine($"frac {frac / n:0.000} fell {fell / n:P0} unitsSent {sent / n:0.0} builds {builds / n:0.0} atkDelivered {atk / n:0} defDelivered {def / n:0}");
                    for (int k = 0; k < 10; k++) Console.WriteLine($"  {Balance.Units[k].English,-22} spend {unitSpend[k] / n,6:0}   {Balance.Defences[k].English,-20} spend {defSpend[k] / n,6:0} dmg {defDmg[k] / n,7:0}");
                    for (int k = 0; k < 5; k++) Console.WriteLine($"  ladle {Balance.Ladles[k].Name,-10} {ladle[k] / n:0}");
                    return 0;
                }
                case "probe":
                {
                    int def = Arg("--def", 1);
                    int n = Arg("--sims", 3000);
                    var r = new Random(5);
                    double d0 = 0, d1 = 0, s0 = 0, s1 = 0, k0 = 0, k1 = 0;
                    for (int i = 0; i < n; i++)
                    {
                        var dp = DefenderPolicy.Random(r); var ap = AttackerPolicy.Random(r); var m = maps[i % maps.Count];
                        var a = HalfSim.Play(m, dp, ap, i, Bonus.None);
                        var b = HalfSim.Play(m, dp, ap, i, new Bonus { Unit = -1, Defence = def, LadleSoup = -1 });
                        s0 += a.Score; s1 += b.Score; k0 += a.UnitsSent; k1 += b.UnitsSent;
                        d0 += a.AttackerDelivered; d1 += b.AttackerDelivered;
                    }
                    Console.WriteLine($"{Balance.Defences[def].English}: score {s0 / n:0.00} -> {s1 / n:0.00}  unitsSent {k0 / n:0.0} -> {k1 / n:0.0}  atkDelivered {d0 / n:0} -> {d1 / n:0}");
                    return 0;
                }
                case "tune":
                {
                    int iterations = Arg("--iters", 10);
                    int samples = Arg("--samples", 400);
                    int seed = Arg("--seed", 12345);
                    var tuner = new Tuner(maps, samples, seed);
                    tuner.Run(iterations,
                        Path.Combine(root, "Assets", "Corba", "Scripts", "Sim", "BalanceTuning.g.cs"),
                        Path.Combine(root, "Docs", "Balance_Report.md"));
                    return 0;
                }
                default:
                    Console.WriteLine("usage: CorbaSim [bench --sims N] [tune --iters N --samples N --seed N]");
                    return 1;
            }
        }

        static string FindRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets"))) dir = dir.Parent;
            return dir?.FullName ?? Directory.GetCurrentDirectory();
        }
    }
}
