using System.Collections.Generic;
using Corba.Sim;
using Unity.Netcode;
using UnityEngine;

namespace Corba
{
    /// <summary>Network view of a <see cref="SimUnit"/>. The server mirrors sim state; clients only animate.</summary>
    public class UnitAgent : NetworkBehaviour
    {
        public static readonly List<UnitAgent> All = new List<UnitAgent>();

        public readonly NetworkVariable<UnitId> Kind = new NetworkVariable<UnitId>();
        public readonly NetworkVariable<float> Distance = new NetworkVariable<float>();
        public readonly NetworkVariable<float> Lateral = new NetworkVariable<float>();
        public readonly NetworkVariable<float> Hp = new NetworkVariable<float>();
        public readonly NetworkVariable<UnitStatus> Flags = new NetworkVariable<UnitStatus>();

        public SimUnit Sim { get; set; }
        public UnitInfo Info => GameData.Unit(Kind.Value);
        public Vector3 WorldPosition => CorbaMap.Instance.SampleRoad(Distance.Value, Lateral.Value);

        float hitFlash, lastHp;
        Transform visual;
        Renderer[] renderers;
        HealthBar bar;
        CharacterAnimator animator;
        float bobPhase, attackAnimTimer;
        bool reachedEnd;
        float damageTally, damageTimer, dustTimer;
        Color shownTint = Color.clear;

        public override void OnNetworkSpawn()
        {
            All.Add(this);
            var info = Info;
            visual = Art.Spawn(info.Model, transform, GameData.Soup(info.Soup).Identity).transform;
            visual.localScale *= info.Scale;
            renderers = visual.GetComponentsInChildren<Renderer>();
            animator = CharacterAnimator.Attach(visual.gameObject);
            animator.SetBase("Walk", info.Speed / 3.6f);
            visual.Pop(0.1f, 0.5f);
            FX.Dust(transform.position);
            FX.Dust(transform.position);
            bar = HealthBar.Create(transform, 1.6f * info.Scale + 0.4f, 0.9f);
            bobPhase = Random.value * 10f;
            lastHp = Hp.Value;
            transform.position = WorldPosition;
            if (Kind.Value == UnitId.Cloud) visual.localPosition = Vector3.up * 0.8f;
        }

        public override void OnNetworkDespawn()
        {
            All.Remove(this);
            if (CorbaMap.Instance != null && Distance.Value < CorbaMap.Instance.RoadLength - 0.5f && !reachedEnd)
            {
                var c = GameData.Soup(Info.Soup).Liquid;
                FX.Splash(transform.position + Vector3.up * 0.6f, c, 16);
                FX.Puff(transform.position + Vector3.up * 0.5f, GameData.Hex("F4ECD8"));
                FX.Splat(transform.position, c, 1.4f * Info.Scale);
                FX.PopText(transform.position + Vector3.up * 2f, "poof!", GameData.Hex("FFF4DC"), 0.8f);
            }
        }

        public void MarkBite() => reachedEnd = true;

        void Update()
        {
            if (!IsSpawned || CorbaMap.Instance == null) return;
            if (IsServer && Sim != null)
            {
                if (!Mathf.Approximately(Distance.Value, Sim.Dist)) Distance.Value = Sim.Dist;
                if (!Mathf.Approximately(Hp.Value, Sim.Hp)) Hp.Value = Sim.Hp;
                if (Flags.Value != Sim.Status) Flags.Value = Sim.Status;
            }
            ClientVisual(Time.deltaTime);
        }

        void UpdateBuffTint(UnitStatus f)
        {
            Color want = Color.clear;
            if ((f & UnitStatus.Fortified) != 0) want = GameData.Soup(SoupType.Mercimek).Identity;
            else if ((f & UnitStatus.Hasted) != 0) want = GameData.Soup(SoupType.Ezogelin).Identity;
            else if ((f & UnitStatus.Immune) != 0) want = GameData.Soup(SoupType.Iskembe).Identity;
            if (want == shownTint) return;
            shownTint = want;
            Art.Tint(visual.gameObject, want == Color.clear ? Color.white : want, want == Color.clear ? 0f : 0.35f);
            if (want != Color.clear) Tween.Punch(visual, 0.2f, 0.3f);
        }

        void ClientVisual(float dt)
        {
            var map = CorbaMap.Instance;
            Vector3 target = map.SampleRoad(Distance.Value, Lateral.Value);
            map.SampleRoad(Distance.Value, out var tan);
            bool flying = Kind.Value == UnitId.Cloud;
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-12f * dt));
            var f = Flags.Value;
            if ((f & UnitStatus.Feared) == 0 && tan.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(tan), 8f * dt);

            bool attacking = (f & UnitStatus.Attacking) != 0;
            bool rooted = (f & UnitStatus.Rooted) != 0;
            float speedMult = (f & UnitStatus.Slowed) != 0 ? 0.55f : (f & UnitStatus.Hasted) != 0 ? 1.45f : 1f;
            animator.SetBase(rooted && !attacking ? "Idle" : "Walk", Info.Speed / 3.6f * speedMult);
            if (attacking)
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
            else if (!animator.HasRig)
            {
                bobPhase += dt * Info.Speed * 3.2f * (rooted ? 0f : 1f);
                visual.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(bobPhase)) * 0.14f, 0);
            }

            if (Hp.Value < lastHp - 0.5f)
            {
                damageTally += lastHp - Hp.Value;
                if (hitFlash < 0.3f)
                {
                    animator.Trigger("Hit", 1.5f);
                    Tween.Squash(visual, 0.18f, 0.25f);
                    Tween.LocalMove(visual, new Vector3(0, visual.localPosition.y, -0.18f), 0.06f, Ease.OutQuad)
                        .OnDone(() => { if (visual) Tween.LocalMove(visual, new Vector3(0, visual.localPosition.y, 0f), 0.2f, Ease.OutBack); });
                }
                hitFlash = 1f;
            }
            damageTimer -= dt;
            if (damageTimer <= 0f && damageTally >= 6f)
            {
                FX.PopText(transform.position + Vector3.up * (1.8f * Info.Scale + 0.3f) + Random.insideUnitSphere * 0.3f,
                    Mathf.RoundToInt(damageTally).ToString(), damageTally >= 40f ? GameData.Hex("FF9A6A") : GameData.Hex("FFE7C2"), damageTally >= 40f ? 1.1f : 0.75f);
                damageTally = 0f;
                damageTimer = 0.35f;
            }
            dustTimer -= dt;
            if (dustTimer <= 0f && !flying && (f & UnitStatus.Rooted) == 0)
            {
                dustTimer = Random.Range(0.35f, 0.7f);
                FX.Dust(transform.position);
            }
            UpdateBuffTint(f);
            if (Hp.Value > lastHp + 20f) FX.Sparkle(transform.position + Vector3.up * 1.2f, GameData.Hex("BFF2D0"));
            lastHp = Hp.Value;
            hitFlash = Mathf.MoveTowards(hitFlash, 0f, dt * 6f);
            Art.SetFloat(renderers, "_Flash", hitFlash * 0.7f);
            bar.Set(Hp.Value / Balance.UnitMaxHp(Kind.Value), GameData.Soup(Info.Soup).Identity);

            Vector3 p = transform.position;
            if ((f & UnitStatus.Burning) != 0 && Random.value < dt * 12f) FX.Ember(p + Vector3.up * 1f);
            if ((f & UnitStatus.Slowed) != 0 && Random.value < dt * 6f) FX.Sparkle(p + Vector3.up * 0.4f, GameData.Hex("BFF2D0"));
            if ((f & UnitStatus.Sour) != 0 && Random.value < dt * 5f) FX.Sparkle(p + Vector3.up * 1.4f, GameData.Hex("C77A35"));
            if ((f & UnitStatus.Rooted) != 0 && Random.value < dt * 5f) FX.Sparkle(p + Vector3.up * 0.2f, GameData.Hex("A88FC9"));
            if ((f & UnitStatus.Fortified) != 0 && Random.value < dt * 6f) FX.Sparkle(p + Vector3.up * 0.9f, GameData.Hex("F2B33D"));
            if ((f & UnitStatus.Hasted) != 0 && Random.value < dt * 10f) FX.Ember(p + Vector3.up * 0.2f);
            if ((f & UnitStatus.Immune) != 0 && Random.value < dt * 5f) FX.Sparkle(p + Vector3.up * 1.6f, GameData.Hex("C9B3E6"));
            if ((f & UnitStatus.Rallied) != 0 && Random.value < dt * 3f) FX.Sparkle(p + Vector3.up * 1.8f, GameData.Hex("FFE48A"));
        }
    }
}
