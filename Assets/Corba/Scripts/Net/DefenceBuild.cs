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

        public SimBuild Sim { get; set; }
        public DefenceInfo Info => GameData.Defence(Kind.Value);
        public float StatMult => GameData.LevelStatMult[Mathf.Clamp(Level.Value - 1, 0, GameData.MaxLevel - 1)];
        public float MaxHp => Info.Hp * (Info.Kind == DefenceKind.Blocker ? StatMult : 1f);
        public bool IsDamageable => Info.Kind != DefenceKind.Trap;

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
            if (shownLevel != 0 && Level.Value > shownLevel) visual.Pop(0.6f, 0.45f);
            shownLevel = Level.Value;
        }

        public void Recoil() => Tween.Squash(visual, 0.12f, 0.25f);

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
            }
            ClientVisual(Time.deltaTime);
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
