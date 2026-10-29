using System.Collections.Generic;
using Corba.Sim;
using UnityEngine;

namespace Corba
{
    /// <summary>Server-side bot cook. Plans with the sim state and drives the same input path a human uses.</summary>
    [RequireComponent(typeof(ChefController))]
    public class CookAI : MonoBehaviour
    {
        enum Task { None, Fill, Pour, Build, Support }

        ChefController chef;
        Task task;
        Station fillStation;
        BuildPad buildPad;
        int buildOption;
        SoupType planSoup;
        Task afterFill;

        List<Vector3> path = new List<Vector3>();
        int pathIndex;
        Vector3 pathTarget;
        float repathTimer, progressTimer, recruitTimer, actCooldown, waveTimer;
        Vector3 progressPos;
        int stuckCount;
        int roleKey = -1;
        MatchPhase plannedPhase;
        readonly Dictionary<Vector3, float> unreachableUntil = new Dictionary<Vector3, float>();
        readonly System.Random rng = new System.Random();

        MatchDirector D => MatchDirector.Instance;
        CorbaMap Map => CorbaMap.Instance;
        int Slot => chef.Slot.Value;
        bool Defending => D.IsDefender(Slot);
        bool HasSoup => chef.Soup.Value >= 0 && chef.DisplayFill > 1f;

        void Awake()
        {
            chef = GetComponent<ChefController>();
            chef.Brain = Think;
            chef.Acted += OnActed;
        }

        void OnActed(string what)
        {
            if (what == "fill" && afterFill != Task.None)
            {
                task = afterFill;
                afterFill = Task.None;
                path.Clear();
                return;
            }
            task = Task.None;
        }

        CookInput Think()
        {
            var input = new CookInput();
            if (D == null || Map == null || !D.InPlay || D.Paused.Value) return input;
            float dt = Time.deltaTime;
            int key = (D.Course.Value * 2 + D.Half.Value) * 2 + (Defending ? 1 : 0);
            if (key != roleKey)
            {
                roleKey = key;
                ResetPlan();
                unreachableUntil.Clear();
            }
            actCooldown -= dt;
            Map.Nav.DecayPenalties(dt);

            if (!Defending)
            {
                recruitTimer -= dt;
                if (recruitTimer <= 0f)
                {
                    recruitTimer = 0.35f;
                    Recruit();
                }
                input.SendWave = ShouldSendWave(dt);
            }

            // Commit to a decision until it is carried out. Re-planning mid-walk with a fresh random pick
            // made bots swerve between cauldrons. Only a phase change (prep -> raid) re-opens an attacker's fill choice.
            var phase = D.Phase.Value;
            bool phaseChanged = phase != plannedPhase;
            plannedPhase = phase;
            if (task == Task.None || (phaseChanged && task == Task.Fill && !Defending)) Plan();
            if (task == Task.Support && !HasSoup) task = Task.None;
            if (task == Task.None) return input;

            Vector3 dest = Destination(out float reach);
            float dist = CorbaMap.FlatDist(transform.position, dest);
            if (task == Task.Support && TryLadle(ref input)) return input;
            if (dist <= reach)
            {
                if (actCooldown <= 0f && task != Task.Support)
                {
                    actCooldown = 0.5f;
                    if (task == Task.Build && D.PadBuild(buildPad.Index) == null)
                    {
                        if (buildOption == 0) input.Option1 = true; else input.Option2 = true;
                    }
                    else input.Interact = true;
                }
                MaybeShove(ref input);
                return input;
            }

            Steer(ref input, dest, dist, dt);
            MaybeShove(ref input);
            return input;
        }

        void ResetPlan()
        {
            task = Task.None;
            afterFill = Task.None;
            afterFillWasSupport = false;
            path.Clear();
            stuckCount = 0;
        }

        bool Reachable(Vector3 target) => !unreachableUntil.TryGetValue(target, out var until) || Time.time >= until;

        #region Navigation

        void Steer(ref CookInput input, Vector3 dest, float dist, float dt)
        {
            repathTimer -= dt;
            if (path.Count == 0 || repathTimer <= 0f || CorbaMap.FlatDist(pathTarget, dest) > 1.2f)
            {
                path = Map.Nav.FindPath(transform.position, dest);
                pathIndex = 0;
                pathTarget = dest;
                repathTimer = 4f;
                if (path.Count == 0)
                {
                    // Never walk blind into a wall: remember the target as unreachable for a while and re-plan.
                    unreachableUntil[dest] = Time.time + 20f;
                    ResetPlan();
                    return;
                }
            }

            Vector3 waypoint = dest;
            while (pathIndex < path.Count - 1 && CorbaMap.FlatDist(transform.position, path[pathIndex]) < 0.7f) pathIndex++;
            if (pathIndex < path.Count) waypoint = path[pathIndex];
            Vector3 dir = waypoint - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) dir = dest - transform.position;
            dir.y = 0f;

            bool carrying = chef.Soup.Value >= 0 && chef.DisplayFill > 5f;
            float throttle = 1f;
            if (carrying)
            {
                float turn = chef.Velocity.sqrMagnitude > 1f ? Vector3.Angle(chef.Velocity, dir) : 0f;
                throttle = turn > 55f ? 0.4f : turn > 25f ? 0.7f : 0.9f;
                if (chef.SloshVector.magnitude > 0.7f) throttle *= 0.55f;
                if (pathIndex < path.Count - 1)
                {
                    float nextTurn = Vector3.Angle(dir, path[pathIndex + 1] - waypoint);
                    float toCorner = CorbaMap.FlatDist(transform.position, waypoint);
                    if (nextTurn > 60f && toCorner < 2.5f) throttle = Mathf.Min(throttle, 0.55f);
                }
            }
            input.Move = dir.normalized * throttle;
            input.Sprint = !carrying && dist > 8f;

            progressTimer += dt;
            if (progressTimer >= 0.9f)
            {
                if (CorbaMap.FlatDist(progressPos, transform.position) < 0.35f)
                {
                    stuckCount++;
                    Map.Nav.Penalize(transform.position + dir.normalized * 0.8f, 6f + stuckCount * 3f);
                    path.Clear();
                    if (stuckCount >= 4 && task == Task.Support)
                    {
                        unreachableUntil[pathTarget] = Time.time + 6f;
                        ResetPlan();
                        task = Task.Support;
                    }
                    if (stuckCount >= 4)
                    {
                        Debug.Log($"[Corba] AI{Slot} hard-stuck at {transform.position} task {task}");
                        input.Move = Quaternion.Euler(0, rng.Next(2) == 0 ? 110 : -110, 0) * dir.normalized;
                        stuckCount = 0;
                    }
                }
                else stuckCount = 0;
                progressPos = transform.position;
                progressTimer = 0f;
            }
        }

        Vector3 Destination(out float reach)
        {
            reach = GameData.InteractRange * 0.75f;
            switch (task)
            {
                case Task.Fill:
                    reach = GameData.InteractRange * 0.9f;
                    return fillStation.Position;
                case Task.Pour:
                    reach = GameData.InteractRange + 1.4f;
                    return Map.Ocak.Position;
                case Task.Build:
                    reach = GameData.InteractRange * 0.65f;
                    return buildPad.TowerPosition;
                case Task.Support:
                    reach = 1.5f;
                    return SupportSpot();
                default:
                    return transform.position;
            }
        }

        #endregion

        #region Planning

        void Plan()
        {
            if (Defending) PlanDefence();
            else PlanAttack();
        }

        void Fill(StationKind kind, SoupType soup, Task then)
        {
            planSoup = soup;
            fillStation = Map.Stations.Find(s => s.Kind == kind && s.Soup == soup && Reachable(s.Position))
                          ?? Map.Stations.Find(s => s.Kind == kind && Reachable(s.Position));
            if (fillStation == null) { task = Task.None; return; }
            if (chef.Soup.Value == (int)soup && chef.DisplayFill >= chef.Capacity * 0.9f)
            {
                task = then;
                return;
            }
            task = Task.Fill;
            afterFill = then;
        }

        // ---------------- defender

        void PlanDefence()
        {
            if (HasSoup && PickBuildFor((SoupType)chef.Soup.Value, chef.DisplayFill)) return;
            var (soup, pad, option) = BestBuildPlan();
            if (pad == null)
            {
                task = Task.None;
                return;
            }
            buildPad = pad;
            buildOption = option;
            Fill(StationKind.DefenderCauldron, soup, Task.Build);
        }

        (SoupType, BuildPad, int) BestBuildPlan()
        {
            float best = float.MinValue;
            (SoupType, BuildPad, int) plan = (SoupType.Mercimek, null, 0);
            for (int s = 0; s < 5; s++)
            for (int o = 0; o < 2; o++)
            {
                var id = GameData.DefenceOption((SoupType)s, o);
                foreach (var pad in Map.Pads)
                {
                    if (D.PadBuild(pad.Index) != null || !Reachable(pad.TowerPosition)) continue;
                    float score = BuildScore(id, pad);
                    if (score > best) { best = score; plan = ((SoupType)s, pad, o); }
                }
            }
            foreach (var b in DefenceBuild.All)
            {
                if (b.Level.Value >= GameData.MaxLevel) continue;
                var pad = Map.Pads[b.Pad.Value];
                float score = BuildScore(b.Info.Id, pad) * 0.75f + b.Level.Value * 0.2f - 0.4f;
                if (score > best) { best = score; plan = (b.Info.Soup, pad, 0); }
            }
            return plan;
        }

        bool PickBuildFor(SoupType soup, float fill)
        {
            float best = float.MinValue;
            BuildPad bestPad = null;
            int bestOption = 0;
            foreach (var pad in Map.Pads)
            {
                if (!Reachable(pad.TowerPosition)) continue;
                var existing = D.PadBuild(pad.Index);
                if (existing == null)
                {
                    for (int o = 0; o < 2; o++)
                    {
                        var id = GameData.DefenceOption(soup, o);
                        if (fill + 0.01f < D.BuildCost(id)) continue;
                        float score = BuildScore(id, pad);
                        if (score > best) { best = score; bestPad = pad; bestOption = o; }
                    }
                }
                else if (existing.Info.Soup == soup && existing.Level.Value < GameData.MaxLevel && fill + 0.01f >= GameData.UpgradeCost(existing.Info.Id, existing.Level.Value))
                {
                    float score = BuildScore(existing.Info.Id, pad) * 0.75f;
                    if (score > best) { best = score; bestPad = pad; bestOption = 0; }
                }
            }
            if (bestPad == null) return false;
            buildPad = bestPad;
            buildOption = bestOption;
            task = Task.Build;
            return true;
        }

        float BuildScore(DefenceId id, BuildPad pad)
        {
            var info = GameData.Defence(id);
            float value = Balance.AiDefenceValue[(int)id];
            if (info.Kind == DefenceKind.Tower && pad.HighGround) value *= Balance.HighGroundRangeMult * 1.1f;
            float progress = pad.RoadDistance / Mathf.Max(1f, Map.RoadLength);
            value *= 0.75f + 0.5f * (1f - Mathf.Abs(progress - 0.6f) * 1.6f);

            int same = 0, kindCount = 0;
            foreach (var b in DefenceBuild.All)
            {
                if (b.Kind.Value == id) same++;
                if (b.Info.Kind == info.Kind) kindCount++;
            }
            value *= 1f / (1f + same * 0.45f);
            if (info.Kind != DefenceKind.Tower && kindCount > DefenceBuild.All.Count * 0.5f) value *= 0.7f;
            value *= Counter(id);

            float travel = CorbaMap.FlatDist(Map.CellCenter(Map.KitchenCell), pad.TowerPosition);
            return value / D.BuildCost(id) * 100f - travel * 0.04f;
        }

        float Counter(DefenceId id)
        {
            var mix = AttackerMix();
            float m = 1f;
            switch (id)
            {
                case DefenceId.Catapult:
                case DefenceId.Griddle: m += mix[(int)UnitId.Grunt] * 0.6f + mix[(int)UnitId.Runner] * 0.3f; break;
                case DefenceId.Mint:
                case DefenceId.Yogurt: m += mix[(int)UnitId.Runner] * 0.8f - mix[(int)UnitId.Golem] * 0.6f; break;
                case DefenceId.Ferment: m += mix[(int)UnitId.Shield] * 0.6f + mix[(int)UnitId.Golem] * 0.5f + mix[(int)UnitId.Brute] * 0.5f; break;
                case DefenceId.Chili: m += mix[(int)UnitId.Golem] * 0.4f + mix[(int)UnitId.Healer] * 0.3f; break;
                case DefenceId.Garlic: m += mix[(int)UnitId.Runner] * 0.5f - mix[(int)UnitId.Brute] * 0.6f; break;
                case DefenceId.Barricade: m += mix[(int)UnitId.Brute] * 0.3f + mix[(int)UnitId.Shield] * 0.2f; break;
            }
            if (DefenceInfoIsTrap(id)) m -= mix[(int)UnitId.Saboteur] * 0.7f;
            return Mathf.Max(0.3f, m);
        }

        static bool DefenceInfoIsTrap(DefenceId id) => GameData.Defence(id).Kind == DefenceKind.Trap;

        float[] AttackerMix()
        {
            var mix = new float[11];
            float total = 0.001f;
            foreach (var u in UnitAgent.All) { mix[(int)u.Kind.Value] += 1f; total += 1f; }
            for (int s = 0; s < 5; s++)
            {
                float bank = D.BankOf(D.AttackerSlot, (SoupType)s) / 30f;
                mix[s * 2] += bank * 0.5f;
                mix[s * 2 + 1] += bank * 0.5f;
                total += bank;
            }
            for (int i = 0; i < mix.Length; i++) mix[i] /= total;
            return mix;
        }

        // ---------------- attacker

        void PlanAttack()
        {
            bool raid = D.Phase.Value == MatchPhase.Raid;
            bool late = D.Phase.Value == MatchPhase.Prep && D.TimeLeft < 14f;
            if (HasSoup)
            {
                if ((raid || late) && task != Task.Pour && planSoup == (SoupType)chef.Soup.Value && afterFillWasSupport)
                {
                    task = Task.Support;
                    return;
                }
                task = Task.Pour;
                return;
            }

            bool wantSupport = (raid && (UnitAgent.All.Count > 0 || D.WaveCount > 0) && rng.NextDouble() < 0.6) || (late && D.WaveCount >= 3);
            if (wantSupport)
            {
                afterFillWasSupport = true;
                Fill(StationKind.AttackerCauldron, SupportSoup(), Task.Support);
                return;
            }
            afterFillWasSupport = false;
            Fill(StationKind.AttackerCauldron, EconomySoup(), Task.Pour);
        }

        bool afterFillWasSupport;

        SoupType EconomySoup()
        {
            int towers = 0, traps = 0, slows = 0;
            foreach (var b in DefenceBuild.All)
            {
                if (b.Info.Kind == DefenceKind.Tower) towers++; else traps++;
                if (b.Kind.Value == DefenceId.Mint || b.Kind.Value == DefenceId.Yogurt) slows++;
            }
            var w = new float[5];
            for (int s = 0; s < 5; s++) w[s] = Balance.AiUnitValue[s * 2] + Balance.AiUnitValue[s * 2 + 1];
            w[(int)SoupType.Tarhana] *= 1f + traps * 0.35f;
            w[(int)SoupType.Iskembe] *= 1f + towers * 0.2f;
            w[(int)SoupType.Yayla] *= 1f + slows * 0.4f;
            return WeightedPick(w);
        }

        SoupType SupportSoup()
        {
            int towers = 0, traps = 0;
            foreach (var b in DefenceBuild.All) if (b.Info.Kind == DefenceKind.Tower) towers++; else traps++;
            var w = (float[])Balance.AiLadleValue.Clone();
            w[(int)SoupType.Tarhana] *= 1f + towers * 0.15f;
            w[(int)SoupType.Iskembe] *= 1f + traps * 0.25f;
            return WeightedPick(w);
        }

        SoupType WeightedPick(float[] w)
        {
            float total = 0f;
            for (int i = 0; i < w.Length; i++) { w[i] = Mathf.Max(0.02f, w[i]); total += w[i]; }
            float r = (float)rng.NextDouble() * total;
            for (int i = 0; i < w.Length; i++)
            {
                r -= w[i];
                if (r <= 0f) return (SoupType)i;
            }
            return SoupType.Mercimek;
        }

        void Recruit()
        {
            for (int s = 0; s < 5; s++)
            {
                var soup = (SoupType)s;
                int bank = D.BankOf(Slot, soup);
                if (bank <= 0) continue;
                var a = GameData.UnitOption(soup, 0);
                var b = GameData.UnitOption(soup, 1);
                float va = Balance.AiUnitValue[(int)a] * UnitCounter(a), vb = Balance.AiUnitValue[(int)b] * UnitCounter(b);
                var pick = rng.NextDouble() < va / Mathf.Max(0.01f, va + vb) ? a : b;
                if (bank < D.RecruitCost(pick)) pick = pick == a ? b : a;
                if (bank < D.RecruitCost(pick)) continue;
                if (D.ServerRecruit(Slot, pick)) return;
            }
        }

        float UnitCounter(UnitId id)
        {
            int towers = 0, traps = 0, slows = 0, fears = 0;
            foreach (var b in DefenceBuild.All)
            {
                if (b.Info.Kind == DefenceKind.Tower) towers++; else traps++;
                if (b.Kind.Value == DefenceId.Mint || b.Kind.Value == DefenceId.Yogurt) slows++;
                if (b.Kind.Value == DefenceId.Garlic || b.Kind.Value == DefenceId.Snare) fears++;
            }
            switch (id)
            {
                case UnitId.Saboteur: return 0.6f + traps * 0.4f;
                case UnitId.Golem: return 1f + slows * 0.35f;
                case UnitId.Brute: return 1f + fears * 0.35f + towers * 0.1f;
                case UnitId.Bomber:
                case UnitId.Ember: return 0.7f + towers * 0.2f;
                case UnitId.Cloud: return 0.7f + towers * 0.15f;
                case UnitId.Runner: return 1f / (1f + slows * 0.4f);
                default: return 1f;
            }
        }

        bool ShouldSendWave(float dt)
        {
            if (D.Phase.Value != MatchPhase.Raid || D.WaveCount == 0) return false;
            waveTimer += dt;
            bool escortReady = task == Task.Support && HasSoup;
            int target = escortReady ? 5 : 8;
            if (D.WaveCount >= target || (D.WaveCount >= 3 && D.TimeLeft < 25f))
            {
                waveTimer = 0f;
                return true;
            }
            return false;
        }

        Vector3 SupportSpot()
        {
            var front = LeadUnit();
            if (front == null) return Map.Ocak.Position;
            // Stay clear of the Ocak plaza while the wave is still leaving it.
            float d = Mathf.Max(9f, front.Distance.Value - 3f);
            var p = Map.SampleRoad(d, 2.4f);
            if (!Map.Nav.IsWalkable(p)) p = Map.SampleRoad(d, -2.4f);
            if (!Map.Nav.IsWalkable(p)) p = Map.SampleRoad(d, 0f);
            return p;
        }

        UnitAgent LeadUnit()
        {
            UnitAgent best = null;
            foreach (var u in UnitAgent.All) if (best == null || u.Distance.Value > best.Distance.Value) best = u;
            return best;
        }

        bool TryLadle(ref CookInput input)
        {
            if (!HasSoup || chef.DisplayFill < Balance.LadleCost || D.Phase.Value != MatchPhase.Raid || !D.LadleReady(Slot)) return false;
            var soup = (SoupType)chef.Soup.Value;
            Vector3 best = default;
            float bestScore = 0f;
            foreach (var u in UnitAgent.All)
            {
                Vector3 c = u.transform.position;
                if (CorbaMap.FlatDist(c, transform.position) > Balance.LadleRange - 0.5f) continue;
                float score = LadleScore(soup, c);
                if (score > bestScore) { bestScore = score; best = c; }
            }
            if (bestScore < 2.2f) return false;
            input.Ladle = true;
            input.Aim = best;
            return true;
        }

        float LadleScore(SoupType soup, Vector3 at)
        {
            float units = 0f, hurt = 0f, threatened = 0f;
            foreach (var u in UnitAgent.All)
            {
                if (CorbaMap.FlatDist(u.transform.position, at) > Balance.LadleRadius) continue;
                units += 1f;
                hurt += 1f - u.Hp.Value / Mathf.Max(1f, u.Info.Hp);
                foreach (var b in DefenceBuild.All)
                    if (b.Info.Kind == DefenceKind.Tower && CorbaMap.FlatDist(b.transform.position, at) < b.Info.Range + 2f) { threatened += 1f; break; }
            }
            int towersNear = 0, trapsNear = 0;
            foreach (var b in DefenceBuild.All)
            {
                float d = CorbaMap.FlatDist(b.transform.position, at);
                if (b.Info.Kind == DefenceKind.Tower && d < Balance.LadleRadius + 1.5f) towersNear++;
                if (b.Info.Kind == DefenceKind.Trap && d < Balance.LadleRadius + 3f) trapsNear++;
            }
            switch (soup)
            {
                case SoupType.Mercimek: return threatened * 1.0f + units * 0.3f;
                case SoupType.Ezogelin: return threatened * 0.8f + units * 0.4f;
                case SoupType.Yayla: return hurt * 2.5f;
                case SoupType.Tarhana: return towersNear * 2.5f + (towersNear > 0 ? units * 0.2f : 0f);
                default: return trapsNear * 2.2f + threatened * 0.4f;
            }
        }

        void MaybeShove(ref CookInput input)
        {
            var rival = ChefController.BySlot[1 - Slot];
            if (rival == null || !chef.ShoveReady) return;
            Vector3 to = rival.transform.position - transform.position;
            to.y = 0f;
            if (to.magnitude > GameData.ShoveRange * 0.9f || Vector3.Dot(transform.forward, to.normalized) < 0.4f) return;
            if (rival.DisplayFill > 35f && chef.DisplayFill < 40f && rng.NextDouble() < 0.5) input.Shove = true;
        }

        #endregion
    }
}
