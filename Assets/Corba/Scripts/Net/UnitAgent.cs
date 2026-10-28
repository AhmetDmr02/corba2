using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Corba
{
    public class UnitAgent : NetworkBehaviour
    {
        public static readonly List<UnitAgent> All = new List<UnitAgent>();

        [System.Flags]
        public enum Status : byte { None = 0, Slowed = 1, Burning = 2, Feared = 4, Rooted = 8, Sour = 16, Healing = 32, Attacking = 64 }

        public readonly NetworkVariable<UnitId> Kind = new NetworkVariable<UnitId>();
        public readonly NetworkVariable<float> Distance = new NetworkVariable<float>();
        public readonly NetworkVariable<float> Lateral = new NetworkVariable<float>();
        public readonly NetworkVariable<float> Hp = new NetworkVariable<float>();
        public readonly NetworkVariable<Status> Flags = new NetworkVariable<Status>();

        public UnitInfo Info => GameData.Unit(Kind.Value);
        public Vector3 WorldPosition => CorbaMap.Instance.SampleRoad(Distance.Value, Lateral.Value);

        float slowUntil, slowAmount;
        float burnUntil, burnDps;
        float rootUntil;
        float fearRemaining;
        int sourStacks;
        float sourUntil;
        float burstUntil;
        int sabotageCharges = GameData.SaboteurCharges;
        float emberTimer;
        float hitFlash;
        float lastHp;

        Transform visual;
        Renderer[] renderers;
        HealthBar bar;
        CharacterAnimator animator;
        float bobPhase;
        float attackAnimTimer;
        bool serverAttacking;

        public override void OnNetworkSpawn()
        {
            All.Add(this);
            var info = Info;
            visual = Art.Spawn(info.Model, transform, GameData.Soup(info.Soup).Identity).transform;
            visual.localScale *= info.Scale;
            renderers = visual.GetComponentsInChildren<Renderer>();
            animator = CharacterAnimator.Attach(visual.gameObject);
            animator.SetBase("Walk", info.Speed / 2.6f);
            visual.Pop(0.1f, 0.5f);
            bar = HealthBar.Create(transform, 1.6f * info.Scale + 0.4f, 0.9f);
            bobPhase = Random.value * 10f;
            lastHp = Hp.Value;
            transform.position = WorldPosition;
            if (Kind.Value == UnitId.Cloud) visual.localPosition = Vector3.up * 0.8f;
        }

        public override void OnNetworkDespawn()
        {
            All.Remove(this);
            if (CorbaMap.Instance != null && Distance.Value < CorbaMap.Instance.RoadLength - 0.5f)
            {
                FX.Splash(transform.position + Vector3.up * 0.6f, GameData.Soup(Info.Soup).Liquid, 10);
                FX.Puff(transform.position + Vector3.up * 0.5f, GameData.Hex("F4ECD8"));
            }
        }

        public bool IsFearImmune => Kind.Value == UnitId.Brute;
        public bool IsSlowImmune => Kind.Value == UnitId.Golem || Kind.Value == UnitId.MiniGolem;
        public bool IsTrapImmune => Kind.Value == UnitId.Saboteur;
        public bool IsRooted => Time.time < rootUntil;

        public void ServerDamage(float amount, bool fromTrap = false)
        {
            if (!IsServer || Hp.Value <= 0f) return;
            if (fromTrap && IsTrapImmune) return;
            float mult = 1f + (Time.time < sourUntil ? sourStacks * GameData.Defence(DefenceId.Ferment).Magnitude : 0f);
            amount *= mult * (1f - Info.Armor);
            Hp.Value -= amount;
            if (Kind.Value == UnitId.Runner) burstUntil = Time.time + GameData.RunnerBurstTime;
            if (Hp.Value <= 0f) Die();
        }

        public void ServerSlow(float amount, float duration)
        {
            if (IsSlowImmune) return;
            if (Time.time > slowUntil || amount >= slowAmount) slowAmount = amount;
            slowUntil = Mathf.Max(slowUntil, Time.time + duration);
        }

        public void ServerBurn(float dps, float duration)
        {
            burnDps = Mathf.Max(burnDps * (Time.time < burnUntil ? 1f : 0f), dps);
            burnUntil = Time.time + duration;
        }

        public void ServerRoot(float duration)
        {
            if (IsFearImmune) return;
            rootUntil = Mathf.Max(rootUntil, Time.time + duration);
        }

        public void ServerFear(float meters)
        {
            if (IsFearImmune) return;
            fearRemaining = Mathf.Max(fearRemaining, meters);
        }

        public void ServerSour(float duration)
        {
            sourStacks = Time.time < sourUntil ? Mathf.Min(sourStacks + 1, 5) : 1;
            sourUntil = Time.time + duration;
        }

        public void ServerHeal(float amount)
        {
            Hp.Value = Mathf.Min(Info.Hp, Hp.Value + amount);
        }

        public bool TrySabotage(DefenceBuild trap)
        {
            if (Kind.Value != UnitId.Saboteur || sabotageCharges <= 0) return false;
            sabotageCharges--;
            trap.ServerDisable(GameData.SaboteurDisarmTime);
            return true;
        }

        void Die()
        {
            if (Kind.Value == UnitId.Golem)
            {
                var d = MatchDirector.Instance;
                d.SpawnUnit(UnitId.MiniGolem, Distance.Value, Lateral.Value - 0.5f);
                d.SpawnUnit(UnitId.MiniGolem, Mathf.Max(0f, Distance.Value - 0.8f), Lateral.Value + 0.5f);
            }
            NetworkObject.Despawn(true);
        }

        void Update()
        {
            if (!IsSpawned || CorbaMap.Instance == null) return;
            if (IsServer) ServerTick(Time.deltaTime);
            ClientVisual(Time.deltaTime);
        }

        void ServerTick(float dt)
        {
            var map = CorbaMap.Instance;
            var info = Info;
            float t = Time.time;

            if (t < burnUntil) ServerDamage(burnDps * dt);
            if (!IsSpawned) return;

            float speed = info.Speed;
            if (Kind.Value == UnitId.Runner && t < burstUntil) speed *= GameData.RunnerBurstMult;
            if (t < slowUntil && !IsSlowImmune) speed *= 1f - slowAmount;
            if (IsRooted) speed = 0f;

            float d = Distance.Value;
            if (fearRemaining > 0f)
            {
                float back = Mathf.Min(fearRemaining, 8f * dt);
                fearRemaining -= back;
                d = Mathf.Max(0f, d - back);
                speed = 0f;
            }

            Vector3 pos = map.SampleRoad(d, Lateral.Value);
            serverAttacking = false;
            var blocker = DefenceBuild.BlockerAhead(d);
            if (blocker != null)
            {
                float stop = blocker.RoadDistance - 1.1f;
                if (d + speed * dt >= stop)
                {
                    speed = Mathf.Clamp((stop - d) / Mathf.Max(dt, 1e-4f), 0f, speed);
                    float dps = Kind.Value == UnitId.Grunt ? GameData.GruntBarricadeDps : GameData.BarricadeOtherDps;
                    blocker.ServerDamage(dps * dt);
                    serverAttacking = true;
                }
            }

            d += speed * dt;
            Distance.Value = d;

            switch (Kind.Value)
            {
                case UnitId.Healer:
                    foreach (var u in All)
                        if (u != this && CorbaMap.FlatDist(u.WorldPosition, pos) < GameData.HealerRadius)
                            u.ServerHeal(GameData.HealerHps * dt);
                    break;
                case UnitId.Ember:
                {
                    var target = DefenceBuild.NearestDamageable(pos, GameData.EmberRange);
                    if (target != null)
                    {
                        target.ServerDamage(GameData.EmberDps * dt);
                        serverAttacking = true;
                        emberTimer -= dt;
                        if (emberTimer <= 0f)
                        {
                            emberTimer = 0.9f;
                            target.ShotFxRpc(pos + Vector3.up * 1.2f, target.transform.position + Vector3.up, GameData.Hex("FF7A2E"), 0.5f, false);
                        }
                    }
                    break;
                }
                case UnitId.Brute:
                {
                    var target = DefenceBuild.NearestDamageable(pos, GameData.BruteRange);
                    if (target != null)
                    {
                        target.ServerDamage(GameData.BruteDps * dt);
                        serverAttacking = true;
                    }
                    break;
                }
                case UnitId.Bomber:
                {
                    var target = DefenceBuild.NearestTower(pos, GameData.BomberSeekRange);
                    if (target != null)
                    {
                        Vector3 tp = target.transform.position;
                        target.BurstFxRpc(tp + Vector3.up, GameData.Hex("D7E37A"), GameData.BomberDisableRadius);
                        target.ServerDamage(GameData.BomberDamage);
                        foreach (var b in new List<DefenceBuild>(DefenceBuild.All))
                            if (b.IsSpawned && b.Info.Kind == DefenceKind.Tower && CorbaMap.FlatDist(b.transform.position, tp) < GameData.BomberDisableRadius)
                                b.ServerDisable(GameData.BomberDisableTime);
                        Hp.Value = 0f;
                        NetworkObject.Despawn(true);
                        return;
                    }
                    break;
                }
            }

            var flags = Status.None;
            if (t < slowUntil && !IsSlowImmune) flags |= Status.Slowed;
            if (t < burnUntil) flags |= Status.Burning;
            if (fearRemaining > 0f) flags |= Status.Feared;
            if (IsRooted) flags |= Status.Rooted;
            if (t < sourUntil) flags |= Status.Sour;
            if (serverAttacking) flags |= Status.Attacking;
            if (Flags.Value != flags) Flags.Value = flags;

            if (d >= map.RoadLength - 0.2f)
            {
                MatchDirector.Instance.ServerBite(this);
                if (IsSpawned) NetworkObject.Despawn(true);
            }
        }

        void ClientVisual(float dt)
        {
            var map = CorbaMap.Instance;
            Vector3 target = map.SampleRoad(Distance.Value, Lateral.Value);
            map.SampleRoad(Distance.Value, out var tan);
            bool flying = Kind.Value == UnitId.Cloud;
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-12f * dt));
            if ((Flags.Value & Status.Feared) == 0 && tan.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(tan), 8f * dt);

            var f0 = Flags.Value;
            bool stopped = (f0 & (Status.Rooted | Status.Attacking)) != 0;
            float speedMult = (f0 & Status.Slowed) != 0 ? 0.55f : 1f;
            animator.SetBase(stopped && (f0 & Status.Attacking) == 0 ? "Idle" : "Walk", Info.Speed / 2.6f * speedMult);
            if ((f0 & Status.Attacking) != 0)
            {
                attackAnimTimer -= dt;
                if (attackAnimTimer <= 0f)
                {
                    attackAnimTimer = 0.9f;
                    animator.Trigger("Attack", 1.3f);
                }
            }
            if (flying)
            {
                bobPhase += dt;
                visual.localPosition = new Vector3(0, 0.8f + Mathf.Sin(bobPhase * 2f) * 0.15f, 0);
            }
            if (!animator.HasRig && !flying)
            {
                bobPhase += dt * Info.Speed * 3.2f * (stopped ? 0f : 1f);
                visual.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(bobPhase)) * 0.14f, 0);
            }

            if (Hp.Value < lastHp - 0.5f)
            {
                if (hitFlash < 0.3f)
                {
                    animator.Trigger("Hit", 1.5f);
                    Tween.Squash(visual, 0.15f, 0.25f);
                }
                hitFlash = 1f;
            }
            lastHp = Hp.Value;
            hitFlash = Mathf.MoveTowards(hitFlash, 0f, dt * 6f);
            Art.SetFloat(renderers, "_Flash", hitFlash * 0.7f);

            bar.Set(Hp.Value / Info.Hp, GameData.Soup(Info.Soup).Identity);

            var f = Flags.Value;
            if ((f & Status.Burning) != 0 && Random.value < dt * 12f) FX.Ember(transform.position + Vector3.up * 1f);
            if ((f & Status.Slowed) != 0 && Random.value < dt * 6f) FX.Sparkle(transform.position + Vector3.up * 0.4f, GameData.Hex("BFF2D0"));
            if ((f & Status.Sour) != 0 && Random.value < dt * 5f) FX.Sparkle(transform.position + Vector3.up * 1.4f, GameData.Hex("C77A35"));
            if ((f & Status.Rooted) != 0 && Random.value < dt * 5f) FX.Sparkle(transform.position + Vector3.up * 0.2f, GameData.Hex("A88FC9"));
        }
    }
}
