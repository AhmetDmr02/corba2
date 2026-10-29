using System;
using System.Collections.Generic;
using System.Numerics;

namespace Corba.Sim
{
    public sealed class SimRoad
    {
        public readonly Vector2[] Points;
        public readonly float[] Dist;
        public readonly float Length;

        public SimRoad(Vector2[] points)
        {
            Points = points;
            Dist = new float[points.Length];
            for (int i = 1; i < points.Length; i++) Dist[i] = Dist[i - 1] + Vector2.Distance(points[i - 1], points[i]);
            Length = Dist[points.Length - 1];
        }

        /// <summary>Uses caller-measured distances so sim distance matches the 3D road the views walk on.</summary>
        public SimRoad(Vector2[] points, IReadOnlyList<float> distances)
        {
            Points = points;
            Dist = new float[points.Length];
            for (int i = 0; i < points.Length; i++) Dist[i] = distances[i];
            Length = Dist[points.Length - 1];
        }

        public Vector2 Sample(float d, float lateral)
        {
            d = Math.Clamp(d, 0f, Length);
            int lo = 0, hi = Dist.Length - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) >> 1;
                if (Dist[mid] <= d) lo = mid; else hi = mid;
            }
            float span = Math.Max(Dist[hi] - Dist[lo], 1e-4f);
            float t = (d - Dist[lo]) / span;
            Vector2 p = Vector2.Lerp(Points[lo], Points[hi], t);
            if (lateral == 0f) return p;
            Vector2 tan = Points[hi] - Points[lo];
            float len = tan.Length();
            if (len < 1e-5f) return p;
            tan /= len;
            return p + new Vector2(tan.Y, -tan.X) * lateral;
        }
    }

    public struct SimPad
    {
        public Vector2 Tower;
        public float RoadDistance;
        public bool HighGround;
    }

    [Flags]
    public enum UnitStatus : ushort
    {
        None = 0, Slowed = 1, Burning = 2, Feared = 4, Rooted = 8, Sour = 16, Healing = 32, Attacking = 64,
        Fortified = 128, Hasted = 256, Immune = 512, Rallied = 1024,
    }

    public sealed class SimUnit
    {
        public int Id;
        public UnitId Kind;
        public UnitInfo Info;
        public float Dist, Lateral, Hp;
        public Vector2 Pos;
        public bool Alive = true;
        public bool Bit;
        public int SabotageCharges = Balance.SaboteurCharges;
        public UnitStatus Status;
        internal float SlowUntil, SlowAmount, BurnUntil, BurnDps, RootUntil, FearRemaining, SourUntil, BurstUntil;
        internal int SourStacks;
        internal float FortifyUntil, HasteUntil, ImmuneUntil;
        internal bool Rallied;
        public float DamageTaken;

        public bool FearImmune(float now) => Kind == UnitId.Brute || now < ImmuneUntil;
        public bool SlowImmune => Kind == UnitId.Golem || Kind == UnitId.MiniGolem;
        public bool TrapImmune => Kind == UnitId.Saboteur;
    }

    public sealed class SimBuild
    {
        public int Id;
        public DefenceId Kind;
        public DefenceInfo Info;
        public int Pad;
        public int Level = 1;
        public float Hp;
        public Vector2 Pos;
        public float RoadDistance;
        public bool HighGround;
        public bool Alive = true;
        public bool Armed = true;
        internal float Cooldown, DisabledUntil, RearmAt, CloudUntil, FogUntil;
        public float DamageDealt;
        public int Kills;

        public float StatMult => Balance.LevelStatMult[Math.Clamp(Level - 1, 0, Balance.MaxLevel - 1)];
        public float MaxHp => Info.Hp * (Info.Kind == DefenceKind.Blocker ? StatMult : 1f);
        public bool Damageable => Info.Kind != DefenceKind.Trap;
        public bool Disabled(float now) => now < DisabledUntil;
        public float Range => Info.Range * (HighGround ? Balance.HighGroundRangeMult : 1f);
    }

    public enum SimEventType : byte { Shot, Lob, Pulse, Cloud, Sabotage, Explode, Bite, UnitSpawned, UnitDied, BuildDestroyed, Ladle, Snare }

    public struct SimEvent
    {
        public SimEventType Type;
        public int Unit;
        public int Build;
        public Vector2 From, To;
        public float Value;
        public int Tag;
    }

    /// <summary>Deterministic fixed-step raid combat. No engine dependencies; ticked by the Unity server and the headless tools.</summary>
    public sealed class SimWorld
    {
        public readonly SimRoad Road;
        public readonly SimPad[] Pads;
        public readonly List<SimUnit> Units = new List<SimUnit>();
        public readonly List<SimBuild> Builds = new List<SimBuild>();
        public readonly List<SimEvent> Events = new List<SimEvent>();
        public float Time { get; private set; }
        public bool RallyActive;
        public Vector2 RallyPoint;
        public bool RecordEvents = true;

        readonly List<SimUnit> pendingSpawns = new List<SimUnit>();
        readonly List<(float at, Vector2 pos, float damage, float radius, SimBuild source)> splashes = new List<(float, Vector2, float, float, SimBuild)>();
        int nextId = 1;

        public SimWorld(SimRoad road, SimPad[] pads)
        {
            Road = road;
            Pads = pads;
        }

        void Emit(SimEvent e)
        {
            if (RecordEvents) Events.Add(e);
        }

        public SimUnit SpawnUnit(UnitId kind, float dist, float lateral)
        {
            var u = MakeUnit(kind, dist, lateral);
            Units.Add(u);
            return u;
        }

        public SimBuild AddBuild(DefenceId kind, int pad, Vector2 pos)
        {
            var info = Balance.Defence(kind);
            var p = Pads[pad];
            var b = new SimBuild
            {
                Id = nextId++, Kind = kind, Info = info, Pad = pad, Pos = pos, RoadDistance = p.RoadDistance, HighGround = p.HighGround,
            };
            b.Hp = b.MaxHp;
            Builds.Add(b);
            return b;
        }

        /// <summary>A build that occupies no pad, used by the balancing tools to add one without displacing others.</summary>
        public SimBuild AddFreeBuild(DefenceId kind, float roadDistance, Vector2 pos, bool highGround)
        {
            var b = new SimBuild
            {
                Id = nextId++, Kind = kind, Info = Balance.Defence(kind), Pad = -1, Pos = pos, RoadDistance = roadDistance, HighGround = highGround,
            };
            b.Hp = b.MaxHp;
            Builds.Add(b);
            return b;
        }

        public void Upgrade(SimBuild b)
        {
            if (b.Level >= Balance.MaxLevel) return;
            b.Level++;
            b.Hp = b.MaxHp;
        }

        public SimBuild BuildOnPad(int pad)
        {
            foreach (var b in Builds) if (b.Alive && b.Pad == pad) return b;
            return null;
        }

        #region Damage and status

        void Damage(SimUnit u, float amount, SimBuild source, bool fromTrap = false)
        {
            if (!u.Alive) return;
            if (fromTrap && u.TrapImmune) return;
            float sour = Time < u.SourUntil ? u.SourStacks * Balance.Defence(DefenceId.Ferment).Magnitude : 0f;
            float reduction = u.Info.Armor;
            if (Time < u.FortifyUntil) reduction = 1f - (1f - reduction) * (1f - Balance.Ladles[(int)SoupType.Mercimek].Magnitude);
            if (u.Rallied) reduction = 1f - (1f - reduction) * (1f - Balance.RallyArmor);
            reduction = Math.Min(reduction, Balance.MaxDamageReduction);
            bool controlled = Time < u.SlowUntil && !u.SlowImmune || Time < u.RootUntil || u.FearRemaining > 0f;
            amount *= (1f + sour) * (1f - reduction) * (controlled ? 1f + Balance.ControlVulnerability : 1f);
            amount = Math.Min(amount, u.Hp);
            u.Hp -= amount;
            u.DamageTaken += amount;
            if (source != null) source.DamageDealt += amount;
            if (u.Kind == UnitId.Runner) u.BurstUntil = Time + Balance.RunnerBurstTime;
            if (u.Hp <= 0.001f)
            {
                if (source != null) source.Kills++;
                Kill(u);
            }
        }

        void Kill(SimUnit u)
        {
            if (!u.Alive) return;
            u.Alive = false;
            u.Hp = 0f;
            Emit(new SimEvent { Type = SimEventType.UnitDied, Unit = u.Id, To = u.Pos });
            if (u.Kind == UnitId.Golem)
            {
                pendingSpawns.Add(MakeUnit(UnitId.MiniGolem, u.Dist, u.Lateral - 0.5f));
                pendingSpawns.Add(MakeUnit(UnitId.MiniGolem, Math.Max(0f, u.Dist - 0.8f), u.Lateral + 0.5f));
            }
        }

        SimUnit MakeUnit(UnitId kind, float dist, float lateral)
        {
            var info = Balance.Unit(kind);
            info.Hp = Balance.UnitMaxHp(kind);
            var u = new SimUnit { Id = nextId++, Kind = kind, Info = info, Dist = dist, Lateral = lateral };
            u.Hp = info.Hp;
            u.Pos = Road.Sample(dist, lateral);
            return u;
        }

        void Slow(SimUnit u, float amount, float duration)
        {
            if (u.SlowImmune) return;
            if (Time > u.SlowUntil || amount >= u.SlowAmount) u.SlowAmount = amount;
            u.SlowUntil = Math.Max(u.SlowUntil, Time + duration);
        }

        void Burn(SimUnit u, float dps, float duration)
        {
            u.BurnDps = Math.Max(Time < u.BurnUntil ? u.BurnDps : 0f, dps);
            u.BurnUntil = Time + duration;
        }

        void Root(SimUnit u, float duration)
        {
            if (Time < u.ImmuneUntil) return;
            u.RootUntil = Math.Max(u.RootUntil, Time + duration);
        }

        void Fear(SimUnit u, float meters)
        {
            if (u.FearImmune(Time)) return;
            u.FearRemaining = Math.Max(u.FearRemaining, meters);
        }

        void Sour(SimUnit u, float duration)
        {
            u.SourStacks = Time < u.SourUntil ? Math.Min(u.SourStacks + 1, (int)Balance.SourStackMax) : 1;
            u.SourUntil = Time + duration;
        }

        public void DamageBuild(SimBuild b, float amount)
        {
            if (!b.Alive || !b.Damageable) return;
            b.Hp -= amount;
            if (b.Hp <= 0f)
            {
                b.Alive = false;
                Emit(new SimEvent { Type = SimEventType.BuildDestroyed, Build = b.Id, To = b.Pos });
            }
        }

        #endregion

        #region Ladle toss

        /// <summary>Attacker throws a ladle of soup at a point. Affects raiders and defences in the splash.</summary>
        public void Ladle(SoupType soup, Vector2 at)
        {
            var info = Balance.Ladles[(int)soup];
            float r = Balance.LadleRadius;
            Emit(new SimEvent { Type = SimEventType.Ladle, To = at, Tag = (int)soup, Value = r });
            switch (soup)
            {
                case SoupType.Mercimek:
                    foreach (var u in Units) if (u.Alive && Vector2.Distance(u.Pos, at) <= r) u.FortifyUntil = Time + info.Duration;
                    break;
                case SoupType.Ezogelin:
                    foreach (var u in Units) if (u.Alive && Vector2.Distance(u.Pos, at) <= r) u.HasteUntil = Time + info.Duration;
                    break;
                case SoupType.Yayla:
                    foreach (var u in Units)
                    {
                        if (!u.Alive || Vector2.Distance(u.Pos, at) > r) continue;
                        u.Hp = Math.Min(u.Info.Hp, u.Hp + info.Magnitude);
                        u.SlowUntil = u.BurnUntil = u.RootUntil = 0f;
                        u.FearRemaining = 0f;
                    }
                    break;
                case SoupType.Tarhana:
                    foreach (var b in Builds) if (b.Alive && Vector2.Distance(b.Pos, at) <= r + 1.5f) b.FogUntil = Time + info.Duration;
                    break;
                case SoupType.Iskembe:
                    foreach (var b in Builds)
                    {
                        if (!b.Alive) continue;
                        if (b.Info.Kind == DefenceKind.Trap && Vector2.Distance(Road.Sample(b.RoadDistance, 0f), at) <= r + 1f)
                            b.DisabledUntil = Math.Max(b.DisabledUntil, Time + info.Duration);
                        else if (b.Info.Kind == DefenceKind.Tower && Vector2.Distance(b.Pos, at) <= r + 1.5f)
                            b.DisabledUntil = Math.Max(b.DisabledUntil, Time + info.Magnitude);
                    }
                    foreach (var u in Units) if (u.Alive && Vector2.Distance(u.Pos, at) <= r) u.ImmuneUntil = Time + info.Duration;
                    break;
            }
        }

        #endregion

        /// <summary>Advances the battle. Returns the hunger bitten off this step.</summary>
        public int Tick(float dt)
        {
            Time += dt;
            int bitten = 0;

            for (int i = splashes.Count - 1; i >= 0; i--)
            {
                var s = splashes[i];
                if (Time < s.at) continue;
                splashes.RemoveAt(i);
                foreach (var u in Units) if (u.Alive && Vector2.Distance(u.Pos, s.pos) <= s.radius) Damage(u, s.damage, s.source);
            }

            foreach (var u in Units)
            {
                if (!u.Alive) continue;
                bitten += TickUnit(u, dt);
            }
            foreach (var b in Builds)
            {
                if (!b.Alive) continue;
                if (b.Info.Kind == DefenceKind.Trap) TickTrap(b, dt);
                else if (b.Info.Kind == DefenceKind.Tower) TickTower(b, dt);
            }

            Units.RemoveAll(u => !u.Alive);
            Builds.RemoveAll(b => !b.Alive);
            foreach (var p in pendingSpawns)
            {
                Units.Add(p);
                Emit(new SimEvent { Type = SimEventType.UnitSpawned, Unit = p.Id, To = p.Pos, Tag = (int)p.Kind });
            }
            pendingSpawns.Clear();
            return bitten;
        }

        int TickUnit(SimUnit u, float dt)
        {
            float t = Time;
            if (t < u.BurnUntil) Damage(u, u.BurnDps * dt, null);
            if (!u.Alive) return 0;

            u.Rallied = RallyActive && Vector2.Distance(u.Pos, RallyPoint) <= Balance.RallyRadius;
            float speed = u.Info.Speed;
            if (u.Kind == UnitId.Runner && t < u.BurstUntil) speed *= Balance.RunnerBurstMult;
            if (t < u.HasteUntil) speed *= 1f + Balance.Ladles[(int)SoupType.Ezogelin].Magnitude;
            if (u.Rallied) speed *= 1f + Balance.RallySpeed;
            if (t < u.SlowUntil && !u.SlowImmune) speed *= 1f - u.SlowAmount;
            if (t < u.RootUntil) speed = 0f;

            float d = u.Dist;
            if (u.FearRemaining > 0f)
            {
                float back = Math.Min(u.FearRemaining, 8f * dt);
                u.FearRemaining -= back;
                d = Math.Max(0f, d - back);
                speed = 0f;
            }

            bool attacking = false;
            SimBuild blocker = null;
            foreach (var b in Builds)
            {
                if (!b.Alive || b.Info.Kind != DefenceKind.Blocker) continue;
                float gap = b.RoadDistance - d;
                if (gap > 0.6f && gap < 3f) { blocker = b; break; }
            }
            if (blocker != null)
            {
                float stop = blocker.RoadDistance - 1.1f;
                if (d + speed * dt >= stop)
                {
                    speed = Math.Clamp((stop - d) / Math.Max(dt, 1e-4f), 0f, speed);
                    DamageBuild(blocker, (u.Kind == UnitId.Grunt ? Balance.GruntBarricadeDps : Balance.BarricadeOtherDps) * dt);
                    attacking = true;
                }
            }

            d += speed * dt;
            u.Dist = d;
            u.Pos = Road.Sample(d, u.Lateral);

            switch (u.Kind)
            {
                case UnitId.Healer:
                    foreach (var o in Units)
                        if (o != u && o.Alive && Vector2.Distance(o.Pos, u.Pos) < Balance.HealerRadius)
                            o.Hp = Math.Min(o.Info.Hp, o.Hp + Balance.HealerHps * dt);
                    break;
                case UnitId.Ember:
                {
                    var target = NearestBuild(u.Pos, Balance.EmberRange, true);
                    if (target != null)
                    {
                        DamageBuild(target, Balance.EmberDps * dt);
                        attacking = true;
                    }
                    break;
                }
                case UnitId.Brute:
                {
                    var target = NearestBuild(u.Pos, Balance.BruteRange, true);
                    if (target != null)
                    {
                        DamageBuild(target, Balance.BruteDps * dt);
                        attacking = true;
                    }
                    break;
                }
                case UnitId.Bomber:
                {
                    var target = NearestBuild(u.Pos, Balance.BomberSeekRange, false);
                    if (target != null)
                    {
                        Emit(new SimEvent { Type = SimEventType.Explode, Unit = u.Id, Build = target.Id, From = u.Pos, To = target.Pos, Value = Balance.BomberDisableRadius });
                        Vector2 tp = target.Pos;
                        DamageBuild(target, Balance.BomberDamage);
                        foreach (var b in Builds)
                            if (b.Alive && b.Info.Kind == DefenceKind.Tower && Vector2.Distance(b.Pos, tp) < Balance.BomberDisableRadius)
                                b.DisabledUntil = Math.Max(b.DisabledUntil, t + Balance.BomberDisableTime);
                        u.Alive = false;
                        u.Hp = 0f;
                        Emit(new SimEvent { Type = SimEventType.UnitDied, Unit = u.Id, To = u.Pos, Tag = 1 });
                        return 0;
                    }
                    break;
                }
            }

            var s = UnitStatus.None;
            if (t < u.SlowUntil && !u.SlowImmune) s |= UnitStatus.Slowed;
            if (t < u.BurnUntil) s |= UnitStatus.Burning;
            if (u.FearRemaining > 0f) s |= UnitStatus.Feared;
            if (t < u.RootUntil) s |= UnitStatus.Rooted;
            if (t < u.SourUntil) s |= UnitStatus.Sour;
            if (attacking) s |= UnitStatus.Attacking;
            if (t < u.FortifyUntil) s |= UnitStatus.Fortified;
            if (t < u.HasteUntil) s |= UnitStatus.Hasted;
            if (t < u.ImmuneUntil) s |= UnitStatus.Immune;
            if (u.Rallied) s |= UnitStatus.Rallied;
            u.Status = s;

            if (d >= Road.Length - 0.2f)
            {
                u.Alive = false;
                u.Bit = true;
                Emit(new SimEvent { Type = SimEventType.Bite, Unit = u.Id, Value = u.Info.Bite, To = u.Pos });
                return u.Info.Bite;
            }
            return 0;
        }

        SimBuild NearestBuild(Vector2 pos, float range, bool anyDamageable)
        {
            SimBuild best = null;
            float bestD = range;
            foreach (var b in Builds)
            {
                if (!b.Alive) continue;
                if (anyDamageable ? !b.Damageable : b.Info.Kind != DefenceKind.Tower) continue;
                float d = Vector2.Distance(pos, b.Pos);
                if (d < bestD) { bestD = d; best = b; }
            }
            return best;
        }

        void TickTower(SimBuild b, float dt)
        {
            if (b.Disabled(Time)) return;
            var info = b.Info;
            float range = b.Range;
            float slow = 1f;
            foreach (var u in Units)
                if (u.Alive && u.Kind == UnitId.Cloud && Vector2.Distance(u.Pos, b.Pos) < Balance.CloudAuraRadius)
                {
                    slow = 1f - Balance.CloudAuraSlow;
                    break;
                }
            if (Time < b.FogUntil) slow = Math.Min(slow, 1f - Balance.Ladles[(int)SoupType.Tarhana].Magnitude);
            b.Cooldown -= dt * slow;
            if (b.Cooldown > 0f) return;
            float mult = b.StatMult;

            switch (info.Id)
            {
                case DefenceId.Catapult:
                {
                    var target = PickTarget(b, range);
                    if (target == null) return;
                    b.Cooldown = info.Interval;
                    Emit(new SimEvent { Type = SimEventType.Lob, Build = b.Id, From = b.Pos, To = target.Pos, Value = 0.6f });
                    splashes.Add((Time + 0.6f, target.Pos, info.Damage * mult, info.Radius, b));
                    break;
                }
                case DefenceId.Chili:
                case DefenceId.Ferment:
                {
                    var target = PickTarget(b, range);
                    if (target == null) return;
                    b.Cooldown = info.Interval;
                    Emit(new SimEvent { Type = SimEventType.Shot, Build = b.Id, From = b.Pos, To = target.Pos, Unit = target.Id, Value = info.Id == DefenceId.Chili ? 0.12f : 0.2f });
                    if (info.Id == DefenceId.Chili) Burn(target, info.Magnitude * mult, info.Duration);
                    else Sour(target, info.Duration);
                    Damage(target, info.Damage * mult, b);
                    break;
                }
                case DefenceId.Mint:
                case DefenceId.Garlic:
                {
                    if (!AnyInRange(b.Pos, range)) return;
                    b.Cooldown = info.Interval;
                    Emit(new SimEvent { Type = SimEventType.Pulse, Build = b.Id, To = b.Pos, Value = range, Tag = (int)info.Id });
                    foreach (var u in Units)
                    {
                        if (!u.Alive || Vector2.Distance(u.Pos, b.Pos) > range) continue;
                        if (info.Id == DefenceId.Mint) Slow(u, info.Magnitude, info.Duration);
                        else Fear(u, info.Magnitude);
                        Damage(u, info.Damage * mult, b);
                    }
                    break;
                }
            }
        }

        void TickTrap(SimBuild b, float dt)
        {
            var info = b.Info;
            if (!b.Armed && Time >= b.RearmAt && info.Interval > 0f) b.Armed = true;
            bool disabled = b.Disabled(Time);
            float center = b.RoadDistance;
            float half = info.Id == DefenceId.Barrel ? info.Range : info.Radius;
            float mult = b.StatMult;

            if (!disabled && b.Armed)
            {
                foreach (var u in Units)
                {
                    if (!u.Alive || u.Kind != UnitId.Saboteur || u.SabotageCharges <= 0 || Math.Abs(u.Dist - center) > half) continue;
                    u.SabotageCharges--;
                    b.DisabledUntil = Time + Balance.SaboteurDisarmTime;
                    Emit(new SimEvent { Type = SimEventType.Sabotage, Build = b.Id, To = b.Pos });
                    return;
                }
            }
            if (disabled) return;

            switch (info.Id)
            {
                case DefenceId.Griddle:
                    foreach (var u in Units)
                        if (u.Alive && Math.Abs(u.Dist - center) <= half) Damage(u, info.Damage * mult * dt, b, true);
                    break;
                case DefenceId.Yogurt:
                    foreach (var u in Units)
                        if (u.Alive && !u.TrapImmune && Math.Abs(u.Dist - center) <= half) Slow(u, info.Magnitude, info.Duration);
                    break;
                case DefenceId.Barrel:
                    if (Time < b.CloudUntil)
                    {
                        foreach (var u in Units)
                            if (u.Alive && Vector2.Distance(u.Pos, b.Pos) <= info.Radius) Damage(u, info.Damage * mult * dt, b, true);
                    }
                    else if (b.Armed && AnyOnRoad(center, half))
                    {
                        b.Armed = false;
                        b.RearmAt = Time + info.Interval;
                        b.CloudUntil = Time + info.Duration;
                        Emit(new SimEvent { Type = SimEventType.Cloud, Build = b.Id, To = b.Pos, Value = info.Radius, Tag = (int)(info.Duration * 10) });
                    }
                    break;
                case DefenceId.Snare:
                {
                    if (!b.Armed) break;
                    int caught = 0;
                    foreach (var u in Units)
                    {
                        if (!u.Alive || u.TrapImmune || Time < u.ImmuneUntil || Math.Abs(u.Dist - center) > half) continue;
                        Root(u, info.Duration);
                        Damage(u, info.Damage * mult, b, true);
                        if (++caught >= (int)info.Magnitude) break;
                    }
                    if (caught > 0)
                    {
                        b.Armed = false;
                        b.RearmAt = Time + info.Interval;
                        Emit(new SimEvent { Type = SimEventType.Snare, Build = b.Id, To = b.Pos });
                    }
                    break;
                }
            }
        }

        SimUnit PickTarget(SimBuild b, float range)
        {
            SimUnit best = null, taunt = null;
            float bestDist = -1f;
            foreach (var u in Units)
            {
                if (!u.Alive) continue;
                float d = Vector2.Distance(u.Pos, b.Pos);
                if (d > range) continue;
                if (u.Kind == UnitId.Shield && d < Balance.ShieldTauntRange) taunt = u;
                if (u.Dist > bestDist) { bestDist = u.Dist; best = u; }
            }
            return taunt ?? best;
        }

        bool AnyInRange(Vector2 pos, float range)
        {
            foreach (var u in Units) if (u.Alive && Vector2.Distance(u.Pos, pos) <= range) return true;
            return false;
        }

        bool AnyOnRoad(float center, float half)
        {
            foreach (var u in Units) if (u.Alive && !u.TrapImmune && Math.Abs(u.Dist - center) <= half) return true;
            return false;
        }

        public SimUnit FindUnit(int id)
        {
            foreach (var u in Units) if (u.Id == id) return u;
            return null;
        }
    }
}
