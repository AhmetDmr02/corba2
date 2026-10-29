using System;
using System.Collections.Generic;
using System.Numerics;
using Corba.Sim;

namespace Corba.Tools
{
    /// <summary>Extra resources injected into one side so we can measure what one more of something is worth.</summary>
    public struct Bonus
    {
        public int Unit;
        public float UnitLadles;
        public int Defence;
        public int LadleSoup;

        public static Bonus None => new Bonus { Unit = -1, Defence = -1, LadleSoup = -1 };
    }

    public sealed class HalfResult
    {
        public float Score;
        public float Fraction;
        public bool Fell;
        public readonly float[] UnitSpend = new float[Balance.RecruitableUnitCount];
        public readonly float[] DefSpend = new float[Balance.DefenceCount];
        public readonly float[] LadleSpend = new float[5];
        public readonly float[] DefDamage = new float[Balance.DefenceCount];
        public int UnitsSent;
        public float GhostSpent;
        public int Builds;
        public float AttackerDelivered, DefenderDelivered;
    }

    /// <summary>Plays one prep + raid half headlessly: two scripted cooks feeding the shared SimWorld.</summary>
    public sealed class HalfSim
    {
        const float Dt = Balance.SimStep;
        const float Overhead = 0.8f;
        const float CarryFactor = 0.8f;
        const float EmptySpeedMult = 1.3f;

        readonly MapData map;
        readonly DefenderPolicy dp;
        readonly AttackerPolicy ap;
        readonly Bonus bonus;
        readonly Random defRng, atkRng, fieldRng;
        readonly SimWorld world;
        readonly HalfResult result = new HalfResult();
        float t;
        float hunger;

        // defender cook
        int defAt = -1;
        int defSoup = -1;
        float defFill;
        float defBusyUntil;
        Action defOnArrive;
        (int pad, DefenceId id, bool upgrade) defPlan = (-1, default, false);

        // attacker cook
        enum AtkLoc { Market, Ocak, Field }
        AtkLoc atkAt = AtkLoc.Market;
        int atkSoup = -1;
        float atkFill;
        float atkBusyUntil;
        Action atkOnArrive;
        bool supporting;
        float supportDist;
        float nextToss;
        float idleField;
        readonly int[] bank = new int[5];
        readonly int[] recruited = new int[Balance.RecruitableUnitCount];

        int OwnedOnPads(DefenceId id)
        {
            int n = 0;
            foreach (var b in world.Builds) if (b.Kind == id && b.Pad >= 0) n++;
            return n;
        }
        readonly List<UnitId> wave = new List<UnitId>();
        readonly Queue<UnitId> marching = new Queue<UnitId>();
        float waveStart, nextSpawn;

        // bonus ladle supporter
        int ghostFill;
        float ghostNextToss;

        public HalfSim(MapData map, DefenderPolicy dp, AttackerPolicy ap, int seed, Bonus bonus)
        {
            this.map = map;
            this.dp = dp;
            this.ap = ap;
            this.bonus = bonus;
            defRng = new Random(seed * 31 + 1);
            atkRng = new Random(seed * 31 + 2);
            fieldRng = new Random(seed * 31 + 3);
            world = map.NewWorld();
            world.RecordEvents = false;
            hunger = Balance.SofraHunger;
        }

        float Spill(int soup, float dist, float skill, Random r)
        {
            float mu = (0.04f + 0.0025f * dist) * Balance.SoupSpillMult[soup] * skill;
            float noise = (float)(r.NextDouble() + r.NextDouble() - 1.0) * 0.06f;
            return Math.Clamp(mu + noise, 0f, 0.6f);
        }

        static float CarrySpeed(int soup) => Balance.CookSpeed * Balance.SoupMoveMult[soup] * CarryFactor;
        static float EmptySpeed => Balance.CookSpeed * EmptySpeedMult;

        public HalfResult Run()
        {
            if (bonus.Defence >= 0) PlaceBonusDefence();
            float end = Balance.PrepSeconds + Balance.RaidSeconds;
            bool raidStarted = false;
            for (t = 0f; t < end; t += Dt)
            {
                bool raid = t >= Balance.PrepSeconds;
                if (raid && !raidStarted)
                {
                    raidStarted = true;
                    StartRaid();
                }

                if (t >= defBusyUntil)
                {
                    var arrive = defOnArrive;
                    defOnArrive = null;
                    arrive?.Invoke();
                    if (defOnArrive == null) DefenderDecide();
                }
                if (t >= atkBusyUntil && !supporting)
                {
                    var arrive = atkOnArrive;
                    atkOnArrive = null;
                    arrive?.Invoke();
                    if (atkOnArrive == null && !supporting) AttackerDecide(raid);
                }

                if (!raid) continue;

                if (wave.Count > 0 && (wave.Count >= ap.WaveSize || t >= waveStart + Balance.AutoWaveSeconds)) SendWave();
                if (marching.Count > 0 && t >= nextSpawn)
                {
                    world.SpawnUnit(marching.Dequeue(), 0f, (float)(fieldRng.NextDouble() * 2.2 - 1.1));
                    result.UnitsSent++;
                    nextSpawn = t + Balance.SpawnInterval;
                }

                if (supporting) SupportTick();
                else world.RallyActive = false;
                if (ghostFill > 0) GhostTick();

                int bitten = world.Tick(Dt);
                if (bitten > 0)
                {
                    float dmg = Math.Min(bitten, hunger);
                    hunger -= dmg;
                    result.Score += dmg;
                    if (hunger <= 0.01f)
                    {
                        result.Fell = true;
                        result.Score += (float)Math.Floor((end - t) / 2f);
                        break;
                    }
                }
            }
            foreach (var b in world.Builds) result.DefDamage[(int)b.Kind] += b.DamageDealt;
            result.Fraction = result.Score / Balance.SofraHunger;
            return result;
        }

        #region Defender

        float DefTravel(int from, int toPad)
        {
            if (from == -1) return map.PadWalk[toPad];
            if (toPad == -1) return map.PadWalk[from];
            return Vector2.Distance(map.Pads[from].Tower, map.Pads[toPad].Tower) * 1.35f;
        }

        float[] AttackerMix()
        {
            var mix = new float[11];
            float total = 0.001f;
            foreach (var u in world.Units) { mix[(int)u.Kind] += 1f; total += 1f; }
            foreach (var u in wave) { mix[(int)u] += 1f; total += 1f; }
            for (int s = 0; s < 5; s++)
            {
                float b = bank[s] / 30f;
                mix[s * 2] += b * 0.5f;
                mix[s * 2 + 1] += b * 0.5f;
                total += b;
            }
            for (int i = 0; i < mix.Length; i++) mix[i] /= total;
            return mix;
        }

        static float CounterScore(DefenceId id, float[] mix)
        {
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
            if (Balance.Defence(id).Kind == DefenceKind.Trap) m -= mix[(int)UnitId.Saboteur] * 0.7f;
            return Math.Max(0.3f, m);
        }

        float BuildUtility(DefenceId id, int pad, bool upgrade, float[] mix)
        {
            var info = Balance.Defence(id);
            var p = map.Pads[pad];
            float progress = p.RoadDistance / map.RoadLength;
            float front = 1f + dp.FrontBias * (progress - 0.5f);
            float hg = info.Kind == DefenceKind.Tower && p.HighGround ? 1f + dp.HighGroundBonus : 1f;
            float counter = 1f + dp.Counter * (CounterScore(id, mix) - 1f);
            float u = dp.BuildWeight[(int)id] * front * hg * counter;
            if (upgrade)
            {
                var b = world.BuildOnPad(pad);
                return u * dp.UpgradeBias * 0.6f / Balance.UpgradeCost(id, b.Level) - dp.TravelWeight * map.PadWalk[pad];
            }
            int same = OwnedOnPads(id);
            return u / (1f + same * 0.4f) / Balance.BuildCost(id, same) - dp.TravelWeight * map.PadWalk[pad];
        }

        /// <summary>Best action overall (ignoring what is in the pot), or the best one the pot can pay for.</summary>
        (int pad, DefenceId id, bool upgrade) BestAction(int soupFilter, float fill)
        {
            var mix = AttackerMix();
            float best = float.MinValue;
            (int, DefenceId, bool) plan = (-1, default, false);
            for (int pad = 0; pad < map.Pads.Length; pad++)
            {
                var existing = world.BuildOnPad(pad);
                if (existing == null)
                {
                    for (int d = 0; d < Balance.DefenceCount; d++)
                    {
                        var id = (DefenceId)d;
                        var info = Balance.Defence(id);
                        if (soupFilter >= 0 && (int)info.Soup != soupFilter) continue;
                        if (Balance.BuildCost(id, OwnedOnPads(id)) > (soupFilter >= 0 ? fill : PotFor(defSoup))) continue;
                        float u = BuildUtility(id, pad, false, mix);
                        if (u > best) { best = u; plan = (pad, id, false); }
                    }
                }
                else if (existing.Level < Balance.MaxLevel)
                {
                    if (soupFilter >= 0 && (int)existing.Info.Soup != soupFilter) continue;
                    int cost = Balance.UpgradeCost(existing.Kind, existing.Level);
                    if (cost > (soupFilter >= 0 ? fill : Balance.PotCapacity)) continue;
                    float u = BuildUtility(existing.Kind, pad, true, mix);
                    if (u > best) { best = u; plan = (pad, existing.Kind, true); }
                }
            }
            return plan;
        }

        static float PotFor(int soup) => Balance.PotCapacity;

        void DefenderDecide()
        {
            if (defSoup >= 0 && defFill >= 1f)
            {
                var act = BestAction(defSoup, defFill);
                if (act.pad >= 0)
                {
                    float travel = DefTravel(defAt, act.pad);
                    if (defAt >= 0) defFill *= 1f - Spill(defSoup, travel, dp.Skill, defRng);
                    defBusyUntil = t + travel / CarrySpeed(defSoup) + Overhead;
                    defOnArrive = () => DefenderAct(act);
                    return;
                }
            }
            var plan = BestAction(-1, 0f);
            if (plan.pad < 0)
            {
                defBusyUntil = t + 1f;
                return;
            }
            defPlan = plan;
            float toKitchen = defAt == -1 ? 0f : map.PadWalk[defAt];
            defBusyUntil = t + toKitchen / EmptySpeed + Overhead;
            defOnArrive = () =>
            {
                defAt = -1;
                defSoup = (int)Balance.Defence(defPlan.id).Soup;
                float carry = map.PadWalk[defPlan.pad];
                defFill = Balance.PotCapacity * (1f - Spill(defSoup, carry, dp.Skill, defRng));
                result.DefenderDelivered += defFill;
            };
        }

        void DefenderAct((int pad, DefenceId id, bool upgrade) act)
        {
            defAt = act.pad;
            var existing = world.BuildOnPad(act.pad);
            if (act.upgrade)
            {
                if (existing == null || existing.Kind != act.id || existing.Level >= Balance.MaxLevel) return;
                int cost = Balance.UpgradeCost(act.id, existing.Level);
                if (defFill + 0.01f < cost) return;
                defFill -= cost;
                world.Upgrade(existing);
                result.DefSpend[(int)act.id] += cost;
                return;
            }
            if (existing != null) return;
            int buildCost = Balance.BuildCost(act.id, OwnedOnPads(act.id));
            if (defFill + 0.01f < buildCost) return;
            defFill -= buildCost;
            PlaceBuild(act.id, act.pad);
            result.DefSpend[(int)act.id] += buildCost;
        }

        void PlaceBuild(DefenceId id, int pad)
        {
            var info = Balance.Defence(id);
            var p = map.Pads[pad];
            Vector2 pos = info.Kind == DefenceKind.Tower ? p.Tower : world.Road.Sample(p.RoadDistance, 0f);
            world.AddBuild(id, pad, pos);
            result.Builds++;
        }

        /// <summary>Adds the probe build where the defender's own policy would put it, without taking the pad.</summary>
        void PlaceBonusDefence()
        {
            var id = (DefenceId)bonus.Defence;
            var info = Balance.Defence(id);
            var mix = AttackerMix();
            int bestPad = 0;
            float best = float.MinValue;
            for (int pad = 0; pad < map.Pads.Length; pad++)
            {
                float u = BuildUtility(id, pad, false, mix);
                if (u > best) { best = u; bestPad = pad; }
            }
            var p = map.Pads[bestPad];
            Vector2 pos = info.Kind == DefenceKind.Tower ? p.Tower : world.Road.Sample(p.RoadDistance, 0f);
            world.AddFreeBuild(id, p.RoadDistance, pos, p.HighGround);
        }

        #endregion

        #region Attacker

        float UnitCounter(UnitId id)
        {
            int towers = 0, traps = 0, slows = 0, fears = 0;
            foreach (var b in world.Builds)
            {
                if (b.Pad < 0) continue;
                if (b.Info.Kind == DefenceKind.Tower) towers++; else traps++;
                if (b.Kind == DefenceId.Mint || b.Kind == DefenceId.Yogurt) slows++;
                if (b.Kind == DefenceId.Garlic || b.Kind == DefenceId.Snare) fears++;
            }
            float c;
            switch (id)
            {
                case UnitId.Saboteur: c = 0.6f + traps * 0.4f; break;
                case UnitId.Golem: c = 1f + slows * 0.35f; break;
                case UnitId.Brute: c = 1f + fears * 0.35f + towers * 0.1f; break;
                case UnitId.Bomber:
                case UnitId.Ember: c = 0.7f + towers * 0.2f; break;
                case UnitId.Cloud: c = 0.7f + towers * 0.15f; break;
                case UnitId.Runner: c = 1f / (1f + slows * 0.4f); break;
                default: c = 1f; break;
            }
            return 1f + ap.Counter * (c - 1f);
        }

        int Weighted(float[] w, Random r)
        {
            float total = 0f;
            foreach (var x in w) total += Math.Max(0f, x);
            if (total <= 0f) return r.Next(w.Length);
            float pick = (float)r.NextDouble() * total;
            for (int i = 0; i < w.Length; i++)
            {
                pick -= Math.Max(0f, w[i]);
                if (pick <= 0f) return i;
            }
            return w.Length - 1;
        }

        void AttackerDecide(bool raid)
        {
            float lead = LeadDist();
            bool support = raid && (world.Units.Count > 0 || wave.Count > 0 || marching.Count > 0) && atkRng.NextDouble() < ap.SupportShare;
            float toMarket = atkAt == AtkLoc.Ocak ? map.MarketToOcak : atkAt == AtkLoc.Field ? map.MarketWalkTo(supportDist) : 0f;
            float arriveMarket = t + toMarket / EmptySpeed + Overhead;

            if (support)
            {
                var w = (float[])ap.LadleWeight.Clone();
                int towers = 0, traps = 0;
                foreach (var b in world.Builds) if (b.Pad >= 0) { if (b.Info.Kind == DefenceKind.Tower) towers++; else traps++; }
                w[(int)SoupType.Tarhana] *= 1f + ap.Counter * towers * 0.15f;
                w[(int)SoupType.Iskembe] *= 1f + ap.Counter * traps * 0.25f;
                int soup = Weighted(w, atkRng);
                float target = Math.Max(0f, lead - 3f);
                float walk = map.MarketWalkTo(target);
                atkBusyUntil = arriveMarket + walk / CarrySpeed(soup);
                atkOnArrive = () =>
                {
                    atkSoup = soup;
                    atkFill = Balance.PotCapacity * (1f - Spill(soup, walk, ap.Skill, atkRng));
                    result.AttackerDelivered += atkFill;
                    atkAt = AtkLoc.Field;
                    supportDist = target;
                    supporting = true;
                    idleField = 0f;
                };
                return;
            }

            var sw = new float[5];
            for (int s = 0; s < 5; s++)
                sw[s] = ap.UnitWeight[s * 2] * UnitCounter((UnitId)(s * 2)) + ap.UnitWeight[s * 2 + 1] * UnitCounter((UnitId)(s * 2 + 1));
            int econ = Weighted(sw, atkRng);
            atkBusyUntil = arriveMarket + map.MarketToOcak / CarrySpeed(econ) + Overhead;
            atkOnArrive = () =>
            {
                float fill = Balance.PotCapacity * (1f - Spill(econ, map.MarketToOcak, ap.Skill, atkRng));
                result.AttackerDelivered += fill;
                bank[econ] += (int)fill;
                atkAt = AtkLoc.Ocak;
                Recruit(econ);
            };
        }

        void Recruit(int soup)
        {
            for (int guard = 0; guard < 40; guard++)
            {
                var a = Balance.UnitOption((SoupType)soup, 0);
                var b = Balance.UnitOption((SoupType)soup, 1);
                int costA = Balance.RecruitCost(a, recruited[(int)a]), costB = Balance.RecruitCost(b, recruited[(int)b]);
                bool canA = bank[soup] >= costA, canB = bank[soup] >= costB;
                if (!canA && !canB) return;
                float wa = canA ? ap.UnitWeight[(int)a] * UnitCounter(a) : 0f;
                float wb = canB ? ap.UnitWeight[(int)b] * UnitCounter(b) : 0f;
                var pick = atkRng.NextDouble() * (wa + wb) < wa ? a : b;
                int cost = pick == a ? costA : costB;
                bank[soup] -= cost;
                recruited[(int)pick]++;
                result.UnitSpend[(int)pick] += cost;
                if (wave.Count == 0) waveStart = t;
                wave.Add(pick);
            }
        }

        void StartRaid()
        {
            if (bonus.Unit >= 0)
            {
                var id = (UnitId)bonus.Unit;
                float n = bonus.UnitLadles / Balance.Unit(id).Cost;
                int whole = (int)n;
                if (fieldRng.NextDouble() < n - whole) whole++;
                for (int i = 0; i < whole; i++) wave.Add(id);
            }
            SendWave();
            nextSpawn = t;
        }

        void SendWave()
        {
            foreach (var u in wave) marching.Enqueue(u);
            wave.Clear();
        }

        float LeadDist()
        {
            float lead = 0f;
            foreach (var u in world.Units) lead = Math.Max(lead, u.Dist);
            return lead;
        }

        void SupportTick()
        {
            float lead = LeadDist();
            float goal = Math.Max(0f, lead - 3f);
            float step = CarrySpeed(atkSoup) * Dt;
            supportDist += Math.Clamp(goal - supportDist, -step, step);
            var pos = world.Road.Sample(supportDist, 2.4f);
            world.RallyActive = true;
            world.RallyPoint = pos;

            if (world.Units.Count == 0) idleField += Dt; else idleField = 0f;
            if (t >= nextToss && atkFill >= Balance.LadleCost)
            {
                if (TryToss(atkSoup, pos, ap.TossThreshold))
                {
                    atkFill -= Balance.LadleCost;
                    result.LadleSpend[atkSoup] += Balance.LadleCost;
                    nextToss = t + Balance.LadleCooldown;
                }
            }
            if (atkFill < Balance.LadleCost || idleField > 4f)
            {
                supporting = false;
                world.RallyActive = false;
                atkAt = AtkLoc.Field;
                atkSoup = -1;
                atkFill = 0f;
                atkBusyUntil = t;
            }
        }

        void GhostTick()
        {
            if (t < ghostNextToss) return;
            float lead = LeadDist();
            var pos = world.Road.Sample(Math.Max(0f, lead - 3f), 2.4f);
            if (TryToss(bonus.LadleSoup, pos, ap.TossThreshold))
            {
                ghostFill -= Balance.LadleCost;
                result.GhostSpent += Balance.LadleCost;
                ghostNextToss = t + Balance.LadleCooldown;
            }
        }

        public void ArmGhost() => ghostFill = bonus.LadleSoup >= 0 ? (int)Balance.PotCapacity : 0;

        bool TryToss(int soup, Vector2 from, float threshold)
        {
            Vector2 best = default;
            float bestScore = 0f;
            foreach (var u in world.Units)
            {
                if (Vector2.Distance(u.Pos, from) > Balance.LadleRange - 0.5f) continue;
                float s = LadleScore(soup, u.Pos);
                if (s > bestScore) { bestScore = s; best = u.Pos; }
            }
            if (bestScore < threshold) return false;
            world.Ladle((SoupType)soup, best);
            return true;
        }

        float LadleScore(int soup, Vector2 at)
        {
            float units = 0f, hurt = 0f, threatened = 0f;
            foreach (var u in world.Units)
            {
                if (Vector2.Distance(u.Pos, at) > Balance.LadleRadius) continue;
                units += 1f;
                hurt += 1f - u.Hp / Math.Max(1f, u.Info.Hp);
                foreach (var b in world.Builds)
                    if (b.Info.Kind == DefenceKind.Tower && Vector2.Distance(b.Pos, at) < b.Range + 2f) { threatened += 1f; break; }
            }
            int towersNear = 0, trapsNear = 0;
            foreach (var b in world.Builds)
            {
                float d = Vector2.Distance(b.Pos, at);
                if (b.Info.Kind == DefenceKind.Tower && d < Balance.LadleRadius + 1.5f) towersNear++;
                if (b.Info.Kind == DefenceKind.Trap && d < Balance.LadleRadius + 3f) trapsNear++;
            }
            switch ((SoupType)soup)
            {
                case SoupType.Mercimek: return threatened * 1.0f + units * 0.3f;
                case SoupType.Ezogelin: return threatened * 0.8f + units * 0.4f;
                case SoupType.Yayla: return hurt * 2.5f;
                case SoupType.Tarhana: return towersNear * 2.5f + (towersNear > 0 ? units * 0.2f : 0f);
                default: return trapsNear * 2.2f + threatened * 0.4f;
            }
        }

        #endregion

        public static HalfResult Play(MapData map, DefenderPolicy dp, AttackerPolicy ap, int seed, Bonus bonus)
        {
            var sim = new HalfSim(map, dp, ap, seed, bonus);
            sim.ArmGhost();
            return sim.Run();
        }
    }
}
