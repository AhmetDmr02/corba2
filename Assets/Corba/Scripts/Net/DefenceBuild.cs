using System.Collections.Generic;
using Corba.Sim;
using Unity.Netcode;
using UnityEngine;

namespace Corba
{
    /// <summary>Network view of a <see cref="SimBuild"/>. Server mirrors sim state; clients render it.</summary>
    public class DefenceBuild : NetworkBehaviour
    {
        public static readonly List<DefenceBuild> All = new List<DefenceBuild>();

        public readonly NetworkVariable<DefenceId> Kind = new NetworkVariable<DefenceId>();
        public readonly NetworkVariable<int> Pad = new NetworkVariable<int>(-1);
        public readonly NetworkVariable<int> Level = new NetworkVariable<int>(1);
        public readonly NetworkVariable<float> Hp = new NetworkVariable<float>();
        public readonly NetworkVariable<bool> Disabled = new NetworkVariable<bool>();
        public readonly NetworkVariable<bool> Armed = new NetworkVariable<bool>(true);
        // Countdown shown under the bar: re-arm for traps, stun/disarm for anything disabled. End is in MatchDirector.Now time.
        public readonly NetworkVariable<double> TimerEnd = new NetworkVariable<double>();
        public readonly NetworkVariable<float> TimerLength = new NetworkVariable<float>();

        public SimBuild Sim { get; set; }
        public DefenceInfo Info => GameData.Defence(Kind.Value);
        public float StatMult => GameData.LevelStatMult[Mathf.Clamp(Level.Value - 1, 0, GameData.MaxLevel - 1)];
        public float MaxHp => Info.Hp * (Info.Kind == DefenceKind.Blocker ? StatMult : 1f);
        public bool IsDamageable => Info.Kind != DefenceKind.Trap;

        static readonly Color HitColor = new Color(1f, 0.95f, 0.88f);
        static readonly Color DisabledColor = GameData.Hex("B48CFF");
        static readonly Color RearmColor = GameData.Hex("6FC3F2");

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
            visual.Pop(0.2f, 0.55f);
            lastHp = Hp.Value;
            FX.Splash(transform.position + Vector3.up, GameData.Soup(Info.Soup).Liquid, 16);
        }

        public override void OnNetworkDespawn()
        {
            All.Remove(this);
            FX.Puff(transform.position + Vector3.up, GameData.Hex("C9B79C"));
            if (bar != null) Destroy(bar.gameObject);
            Art.SetFlash(renderers, 0.8f, HitColor);
            Art.Vanish(visual, 0.4f);
        }

        void RebuildVisual()
        {
            if (visual != null) Destroy(visual.gameObject);
            var info = Info;
            visual = Art.Spawn(info.Model, transform, GameData.Soup(info.Soup).Identity).transform;
            visual.localScale *= 1f + (Level.Value - 1) * 0.12f;
            baseScale = visual.localScale;
            Tween.SetRest(visual, baseScale);
            renderers = visual.GetComponentsInChildren<Renderer>();
            if (bar == null) bar = HealthBar.Create(transform, info.Kind == DefenceKind.Blocker ? 2.2f : info.Kind == DefenceKind.Trap ? 1.3f : 3.8f, 1.4f, IsDamageable);
            if (levelBadge != null) Destroy(levelBadge);
            levelBadge = new GameObject("Level");
            levelBadge.transform.SetParent(transform, false);
            for (int i = 0; i < Level.Value; i++)
            {
                var pip = Art.Spawn("pip", levelBadge.transform, GameData.Hex("F2B33D"));
                pip.transform.localPosition = new Vector3((i - (Level.Value - 1) * 0.5f) * 0.35f, 0.1f, info.Kind == DefenceKind.Tower ? -1.3f : -1.9f);
                pip.transform.localScale = Vector3.one * 0.25f;
            }
            if (shownLevel != 0 && Level.Value > shownLevel) visual.Pop(0.6f, 0.45f);
            shownLevel = Level.Value;
        }

        public void Recoil() => Tween.Squash(visual, 0.12f, 0.25f);

        void SyncTimer(float simNow, bool disabled)
        {
            float left = disabled ? Sim.DisabledEnd - simNow : !Sim.Armed && Info.Interval > 0f ? Sim.RearmTime - simNow : 0f;
            if (left <= 0f) return;
            double end = MatchDirector.Now + left;
            // Only push when the countdown restarts; sim and network clocks drift a little while it runs.
            if (System.Math.Abs(end - TimerEnd.Value) < 0.3) return;
            TimerEnd.Value = end;
            TimerLength.Value = left;
        }

        float TimerElapsed01()
        {
            double left = TimerEnd.Value - MatchDirector.Now;
            if (left <= 0.0 || TimerLength.Value <= 0f) return -1f;
            return 1f - Mathf.Clamp01((float)(left / TimerLength.Value));
        }

        void Update()
        {
            if (!IsSpawned || CorbaMap.Instance == null) return;
            if (IsServer && Sim != null)
            {
                var now = MatchDirector.Instance != null ? MatchDirector.Instance.SimTime : 0f;
                if (!Mathf.Approximately(Hp.Value, Sim.Hp)) Hp.Value = Sim.Hp;
                if (Level.Value != Sim.Level) Level.Value = Sim.Level;
                bool disabled = Sim.Disabled(now);
                if (Disabled.Value != disabled) Disabled.Value = disabled;
                if (Armed.Value != Sim.Armed) Armed.Value = Sim.Armed;
                SyncTimer(now, disabled);
            }
            ClientVisual(Time.deltaTime);
        }

        void ClientVisual(float dt)
        {
            if (shownLevel != Level.Value) RebuildVisual();
            if (Hp.Value < lastHp - 0.5f)
            {
                if (hitFlash < 0.4f) Tween.Squash(visual, 0.08f, 0.2f);
                hitFlash = 1f;
            }
            lastHp = Hp.Value;
            hitFlash = Mathf.MoveTowards(hitFlash, 0f, dt * 5f);
            if (hitFlash > 0.05f || !Disabled.Value) Art.SetFlash(renderers, hitFlash * 0.8f, HitColor);
            else Art.SetFlash(renderers, 0.35f + Mathf.Sin(Time.time * 12f) * 0.15f, DisabledColor);
            if (IsDamageable) bar.Set(Hp.Value / Mathf.Max(1f, MaxHp));
            bar.SetTimer(TimerElapsed01(), Disabled.Value ? DisabledColor : RearmColor);
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
