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
                FX.Splash(transform.position + Vector3.up * 0.6f, GameData.Soup(Info.Soup).Liquid, 10);
                FX.Puff(transform.position + Vector3.up * 0.5f, GameData.Hex("F4ECD8"));
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
                if (hitFlash < 0.3f)
                {
                    animator.Trigger("Hit", 1.5f);
                    Tween.Squash(visual, 0.15f, 0.25f);
                }
                hitFlash = 1f;
            }
            if (Hp.Value > lastHp + 20f) FX.Sparkle(transform.position + Vector3.up * 1.2f, GameData.Hex("BFF2D0"));
            lastHp = Hp.Value;
            hitFlash = Mathf.MoveTowards(hitFlash, 0f, dt * 6f);
            Art.SetFloat(renderers, "_Flash", hitFlash * 0.7f);
            bar.Set(Hp.Value / Info.Hp, GameData.Soup(Info.Soup).Identity);

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
