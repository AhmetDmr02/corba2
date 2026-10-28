using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Corba
{
    public class DefenceBuild : NetworkBehaviour
    {
        public static readonly List<DefenceBuild> All = new List<DefenceBuild>();
        static readonly List<UnitAgent> scratch = new List<UnitAgent>();

        public readonly NetworkVariable<DefenceId> Kind = new NetworkVariable<DefenceId>();
        public readonly NetworkVariable<int> Pad = new NetworkVariable<int>(-1);
        public readonly NetworkVariable<int> Level = new NetworkVariable<int>(1);
        public readonly NetworkVariable<float> Hp = new NetworkVariable<float>();
        public readonly NetworkVariable<bool> Disabled = new NetworkVariable<bool>();
        public readonly NetworkVariable<bool> Armed = new NetworkVariable<bool>(true);

        public DefenceInfo Info => GameData.Defence(Kind.Value);
        public float StatMult => GameData.LevelStatMult[Mathf.Clamp(Level.Value - 1, 0, GameData.MaxLevel - 1)];
        public float MaxHp => Info.Hp * (Info.Kind == DefenceKind.Blocker ? StatMult : 1f);
        public float RoadDistance => CorbaMap.Instance.Pads[Pad.Value].RoadDistance;
        public bool IsDamageable => Info.Kind != DefenceKind.Trap;
        public float RangeMult => Pad.Value >= 0 && Pad.Value < CorbaMap.Instance.Pads.Count && CorbaMap.Instance.Pads[Pad.Value].HighGround ? 1.25f : 1f;

        float cooldown;
        float disabledUntil;
        float rearmAt;
        float cloudUntil;
        float hitFlash, lastHp;
        int shownLevel;
        Vector3 baseScale;

        Transform visual;
        Renderer[] renderers;
        HealthBar bar;
        GameObject levelBadge;

        public override void OnNetworkSpawn()
        {
            All.Add(this);
            RebuildVisual();
            lastHp = Hp.Value;
            FX.Splash(transform.position + Vector3.up, GameData.Soup(Info.Soup).Liquid, 16);
        }

        public override void OnNetworkDespawn()
        {
            All.Remove(this);
            if (IsServer) MatchDirector.Instance?.ServerReleasePad(Pad.Value);
            FX.Puff(transform.position + Vector3.up, GameData.Hex("C9B79C"));
        }

        void RebuildVisual()
        {
            if (visual != null) Destroy(visual.gameObject);
            var info = Info;
            visual = Art.Spawn(info.Model, transform, GameData.Soup(info.Soup).Identity).transform;
            float scale = 1f + (Level.Value - 1) * 0.12f;
            visual.localScale *= scale;
            baseScale = visual.localScale;
            renderers = visual.GetComponentsInChildren<Renderer>();
            if (bar == null && IsDamageable) bar = HealthBar.Create(transform, info.Kind == DefenceKind.Blocker ? 2f : 3.6f, 1.4f);
            if (levelBadge != null) Destroy(levelBadge);
            levelBadge = new GameObject("Level");
            levelBadge.transform.SetParent(transform, false);
            for (int i = 0; i < Level.Value; i++)
            {
                var pip = Art.Spawn("pip", levelBadge.transform, GameData.Hex("F2B33D"));
                pip.transform.localPosition = new Vector3((i - (Level.Value - 1) * 0.5f) * 0.35f, 0.1f, info.Kind == DefenceKind.Tower ? -1.3f : -1.9f);
                pip.transform.localScale = Vector3.one * 0.25f;
            }
            shownLevel = Level.Value;
        }

        #region Queries

        public static DefenceBuild BlockerAhead(float unitDistance)
        {
            foreach (var b in All)
            {
                if (!b.IsSpawned || b.Info.Kind != DefenceKind.Blocker) continue;
                float gap = b.RoadDistance - unitDistance;
                if (gap > 0.6f && gap < 3f) return b;
            }
            return null;
        }

        public static DefenceBuild NearestDamageable(Vector3 pos, float range)
        {
            DefenceBuild best = null;
            float bestD = range;
            foreach (var b in All)
            {
                if (!b.IsSpawned || !b.IsDamageable) continue;
                float d = CorbaMap.FlatDist(pos, b.transform.position);
                if (d < bestD) { bestD = d; best = b; }
            }
            return best;
        }

        public static DefenceBuild NearestTower(Vector3 pos, float range)
        {
            DefenceBuild best = null;
            float bestD = range;
            foreach (var b in All)
            {
                if (!b.IsSpawned || b.Info.Kind != DefenceKind.Tower) continue;
                float d = CorbaMap.FlatDist(pos, b.transform.position);
                if (d < bestD) { bestD = d; best = b; }
            }
            return best;
        }

        #endregion

        public void ServerDamage(float amount)
        {
            if (!IsServer || !IsDamageable || !IsSpawned) return;
            Hp.Value -= amount;
            if (Hp.Value <= 0f) NetworkObject.Despawn(true);
        }

        public void ServerDisable(float seconds)
        {
            disabledUntil = Mathf.Max(disabledUntil, Time.time + seconds);
        }

        void Update()
        {
            if (!IsSpawned || CorbaMap.Instance == null) return;
            if (IsServer) ServerTick(Time.deltaTime);
            ClientVisual(Time.deltaTime);
        }

        void ServerTick(float dt)
        {
            bool disabled = Time.time < disabledUntil;
            if (Disabled.Value != disabled) Disabled.Value = disabled;
            if (MatchDirector.Instance == null || MatchDirector.Instance.Phase.Value != MatchPhase.Raid) return;

            scratch.Clear();
            scratch.AddRange(UnitAgent.All);
            var info = Info;
            float mult = StatMult;
            float range = info.Range * RangeMult;

            if (info.Kind == DefenceKind.Trap)
            {
                TrapTick(info, mult, dt, disabled);
                return;
            }
            if (info.Kind != DefenceKind.Tower || disabled) return;

            float slowFactor = 1f;
            foreach (var u in scratch)
                if (u.IsSpawned && u.Kind.Value == UnitId.Cloud && CorbaMap.FlatDist(u.WorldPosition, transform.position) < GameData.CloudAuraRadius)
                {
                    slowFactor = 1f - GameData.CloudAuraSlow;
                    break;
                }

            cooldown -= dt * slowFactor;
            if (cooldown > 0f) return;

            Vector3 muzzle = transform.position + Vector3.up * 2.6f;
            switch (info.Id)
            {
                case DefenceId.Catapult:
                {
                    var target = PickTarget(range);
                    if (target == null) return;
                    cooldown = info.Interval;
                    Vector3 land = target.WorldPosition;
                    ShotFxRpc(muzzle, land, GameData.Soup(info.Soup).Liquid, 0.6f, true);
                    StartCoroutine(Splash(land, info.Damage * mult, info.Radius, 0.6f));
                    break;
                }
                case DefenceId.Chili:
                {
                    var target = PickTarget(range);
                    if (target == null) return;
                    cooldown = info.Interval;
                    ShotFxRpc(muzzle, target.transform.position + Vector3.up * 0.8f, GameData.Hex("E0402A"), 0.12f, false);
                    target.ServerBurn(info.Magnitude * mult, info.Duration);
                    target.ServerDamage(info.Damage * mult);
                    break;
                }
                case DefenceId.Ferment:
                {
                    var target = PickTarget(range);
                    if (target == null) return;
                    cooldown = info.Interval;
                    ShotFxRpc(muzzle, target.transform.position + Vector3.up * 0.8f, GameData.Hex("C77A35"), 0.2f, false);
                    target.ServerSour(info.Duration);
                    target.ServerDamage(info.Damage * mult);
                    break;
                }
                case DefenceId.Mint:
                {
                    if (!AnyInRange(range)) return;
                    cooldown = info.Interval;
                    BurstFxRpc(transform.position + Vector3.up * 0.5f, GameData.Hex("9FE3B5"), range);
                    foreach (var u in scratch)
                    {
                        if (!u.IsSpawned || CorbaMap.FlatDist(u.WorldPosition, transform.position) > range) continue;
                        u.ServerSlow(info.Magnitude, info.Duration);
                        u.ServerDamage(info.Damage * mult);
                    }
                    break;
                }
                case DefenceId.Garlic:
                {
                    if (!AnyInRange(range)) return;
                    cooldown = info.Interval;
                    BurstFxRpc(transform.position + Vector3.up * 0.5f, GameData.Hex("C9B3E6"), range);
                    foreach (var u in scratch)
                    {
                        if (!u.IsSpawned || CorbaMap.FlatDist(u.WorldPosition, transform.position) > range) continue;
                        u.ServerFear(info.Magnitude);
                        u.ServerDamage(info.Damage * mult);
                    }
                    break;
                }
            }
        }

        void TrapTick(DefenceInfo info, float mult, float dt, bool disabled)
        {
            if (!Armed.Value && Time.time >= rearmAt && info.Interval > 0f) Armed.Value = true;
            float center = RoadDistance;
            float half = info.Id == DefenceId.Barrel ? info.Range : info.Radius;

            foreach (var u in scratch)
            {
                if (!u.IsSpawned) continue;
                float gap = Mathf.Abs(u.Distance.Value - center);
                if (gap > half) continue;
                if (!disabled && Armed.Value && u.TrySabotage(this))
                {
                    BurstFxRpc(transform.position + Vector3.up * 0.3f, GameData.Hex("C77A35"), 1.5f);
                    return;
                }
            }
            if (disabled) return;

            switch (info.Id)
            {
                case DefenceId.Griddle:
                    foreach (var u in scratch)
                        if (u.IsSpawned && Mathf.Abs(u.Distance.Value - center) <= half)
                            u.ServerDamage(info.Damage * mult * dt, true);
                    break;
                case DefenceId.Yogurt:
                    foreach (var u in scratch)
                        if (u.IsSpawned && !u.IsTrapImmune && Mathf.Abs(u.Distance.Value - center) <= half)
                            u.ServerSlow(info.Magnitude, info.Duration);
                    break;
                case DefenceId.Barrel:
                    if (Time.time < cloudUntil)
                    {
                        foreach (var u in scratch)
                            if (u.IsSpawned && CorbaMap.FlatDist(u.WorldPosition, transform.position) <= info.Radius)
                                u.ServerDamage(info.Damage * mult * dt, true);
                    }
                    else if (Armed.Value && AnyOnRoadWithin(center, half))
                    {
                        Armed.Value = false;
                        rearmAt = Time.time + info.Interval;
                        cloudUntil = Time.time + info.Duration;
                        CloudFxRpc(transform.position + Vector3.up * 0.4f, info.Radius, info.Duration);
                    }
                    break;
                case DefenceId.Snare:
                    if (!Armed.Value) break;
                    int caught = 0;
                    foreach (var u in scratch)
                    {
                        if (!u.IsSpawned || u.IsTrapImmune || u.IsFearImmune || Mathf.Abs(u.Distance.Value - center) > half) continue;
                        u.ServerRoot(info.Duration);
                        u.ServerDamage(info.Damage * mult, true);
                        if (++caught >= (int)info.Magnitude) break;
                    }
                    if (caught > 0)
                    {
                        Armed.Value = false;
                        rearmAt = Time.time + info.Interval;
                        BurstFxRpc(transform.position + Vector3.up * 0.3f, GameData.Hex("A88FC9"), 2f);
                    }
                    break;
            }
        }

        UnitAgent PickTarget(float range)
        {
            UnitAgent best = null, taunt = null;
            float bestDist = -1f;
            foreach (var u in scratch)
            {
                if (!u.IsSpawned) continue;
                if (CorbaMap.FlatDist(u.WorldPosition, transform.position) > range) continue;
                if (u.Kind.Value == UnitId.Shield && CorbaMap.FlatDist(u.WorldPosition, transform.position) < GameData.ShieldTauntRange) taunt = u;
                if (u.Distance.Value > bestDist) { bestDist = u.Distance.Value; best = u; }
            }
            return taunt != null ? taunt : best;
        }

        bool AnyInRange(float range)
        {
            foreach (var u in scratch)
                if (u.IsSpawned && CorbaMap.FlatDist(u.WorldPosition, transform.position) <= range) return true;
            return false;
        }

        bool AnyOnRoadWithin(float center, float half)
        {
            foreach (var u in scratch)
                if (u.IsSpawned && !u.IsTrapImmune && Mathf.Abs(u.Distance.Value - center) <= half) return true;
            return false;
        }

        IEnumerator Splash(Vector3 at, float damage, float radius, float delay)
        {
            yield return new WaitForSeconds(delay);
            var list = new List<UnitAgent>(UnitAgent.All);
            foreach (var u in list)
                if (u.IsSpawned && CorbaMap.FlatDist(u.WorldPosition, at) <= radius) u.ServerDamage(damage);
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void ShotFxRpc(Vector3 from, Vector3 to, Color color, float flight, bool lob)
        {
            FX.Projectile(from, to, color, flight, lob);
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void BurstFxRpc(Vector3 at, Color color, float radius)
        {
            FX.Ring(at, color, radius);
        }

        [Rpc(SendTo.ClientsAndHost)]
        void CloudFxRpc(Vector3 at, float radius, float duration)
        {
            FX.Cloud(at, GameData.Hex("B9C86A"), radius, duration);
        }

        void ClientVisual(float dt)
        {
            if (shownLevel != Level.Value) RebuildVisual();
            if (Hp.Value < lastHp - 0.5f) hitFlash = 1f;
            lastHp = Hp.Value;
            hitFlash = Mathf.MoveTowards(hitFlash, 0f, dt * 5f);
            Art.SetFloat(renderers, "_Flash", Disabled.Value ? 0.35f + Mathf.Sin(Time.time * 12f) * 0.15f : hitFlash * 0.6f);
            if (bar != null) bar.Set(Hp.Value / Mathf.Max(1f, MaxHp), GameData.Soup(Info.Soup).Identity);
            if (Info.Kind == DefenceKind.Trap)
            {
                float target = Armed.Value && !Disabled.Value ? 1f : 0.55f;
                visual.localScale = Vector3.Lerp(visual.localScale, baseScale * target, dt * 6f);
            }
            if (Info.Id == DefenceId.Griddle && !Disabled.Value && Random.value < dt * 4f)
                FX.Ember(transform.position + new Vector3(Random.Range(-1f, 1f), 0.2f, Random.Range(-1f, 1f)));
        }
    }
}
