using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Corba
{
    public struct CookInput
    {
        public Vector3 Move;
        public bool Sprint;
        public bool Interact;
        public bool Option1;
        public bool Option2;
        public bool Shove;
        public bool Ladle;
        public bool SendWave;
        public Vector3 Aim;
    }

    [RequireComponent(typeof(CharacterController))]
    public class ChefController : NetworkBehaviour
    {
        public static readonly ChefController[] BySlot = new ChefController[2];
        public static ChefController Local { get; private set; }

        public readonly NetworkVariable<int> Slot = new NetworkVariable<int>(-1);
        public readonly NetworkVariable<int> Soup = new NetworkVariable<int>(-1);
        public readonly NetworkVariable<float> Fill = new NetworkVariable<float>();
        public readonly NetworkVariable<bool> IsBot = new NetworkVariable<bool>();

        // Natural slosh frequency over the peak response of a damped impulse; converts "desired peak" impulses into slosh velocity.
        const float ImpulseScale = 9.7f;

        CharacterController controller;
        Vector3 velocity;
        Vector3 knockback;
        Vector2 slosh, sloshVelocity;
        float pendingSpill, spillSendTimer;
        float verticalSpeed;
        bool wasGrounded = true;
        double shoveReadyAt;
        int insideBump = -1;
        float lastSeenFill;
        Vector3 lastRemotePos;
        float remoteSpeed;

        Transform visual, potRoot, soupSurface;
        Renderer[] surfaceRenderers;
        CharacterAnimator animator;
        MatchDirector Director => MatchDirector.Instance;

        public System.Func<CookInput> Brain;
        public bool IsSpilling { get; private set; }
        public float ShoveCooldown01 => Mathf.Clamp01((float)((shoveReadyAt - MatchDirector.Now) / GameData.ShoveCooldown));
        public bool ShoveReady => MatchDirector.Now >= shoveReadyAt;
        public float DisplayFill => Mathf.Max(0f, Fill.Value - pendingSpill);
        public Vector2 SloshVector => slosh;
        public Vector3 Velocity => velocity;
        public string Prompt { get; private set; } = "";
        public int PromptPad { get; private set; } = -1;
        public float Capacity => Director != null && Slot.Value >= 0 ? Director.Capacity(Slot.Value) : GameData.PotCapacity;
        public event System.Action<string> Acted;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        public override void OnNetworkSpawn()
        {
            if (Slot.Value >= 0) BySlot[Slot.Value] = this;
            BuildVisual();
            if (IsOwner && !IsBot.Value)
            {
                Local = this;
                CameraRig.Instance?.Follow(transform);
            }
            controller.enabled = IsOwner;
            lastSeenFill = Fill.Value;
            lastRemotePos = transform.position;
            visual.Pop(0.2f, 0.6f);
        }

        public override void OnNetworkDespawn()
        {
            if (Slot.Value >= 0 && BySlot[Slot.Value] == this) BySlot[Slot.Value] = null;
            if (Local == this) Local = null;
        }

        void BuildVisual()
        {
            var model = Art.Spawn(Slot.Value == 0 ? "chef_a" : "chef_b", transform, Slot.Value == 0 ? GameData.Hex("D9442B") : GameData.Hex("3F7FBF"));
            visual = model.transform;
            animator = CharacterAnimator.Attach(model);
            potRoot = Art.Spawn("pot", transform, GameData.Hex("6B6B6B")).transform;
            potRoot.localPosition = new Vector3(0f, 0.7f, 0.5f);
            soupSurface = CorbaMap.SoupSurface(potRoot, Color.white, 0.32f, 0.5f);
            surfaceRenderers = soupSurface.GetComponentsInChildren<Renderer>();
        }

        public void ResetPot()
        {
            Soup.Value = -1;
            Fill.Value = 0f;
        }

        [Rpc(SendTo.Owner)]
        public void TeleportRpc(Vector3 pos)
        {
            controller.enabled = false;
            transform.position = pos;
            controller.enabled = true;
            velocity = Vector3.zero;
            knockback = Vector3.zero;
            slosh = sloshVelocity = Vector2.zero;
            pendingSpill = 0f;
            visual.Pop(0.3f, 0.5f);
        }

        void Update()
        {
            if (!IsSpawned) return;
            if (IsOwner) OwnerUpdate();
            UpdateVisual();
        }

        CookInput ReadHuman()
        {
            var input = new CookInput();
            var kb = Keyboard.current;
            if (kb == null || CorbaUI.TypingInField) return input;
            Vector2 axis = Vector2.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) axis.y += 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) axis.y -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) axis.x += 1;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) axis.x -= 1;
            axis = Vector2.ClampMagnitude(axis, 1f);
            Vector3 camF = Vector3.forward, camR = Vector3.right;
            if (Camera.main != null)
            {
                camF = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
                camR = Vector3.Cross(Vector3.up, camF);
            }
            input.Move = camF * axis.y + camR * axis.x;
            input.Sprint = kb.leftShiftKey.isPressed;
            input.Interact = kb.eKey.wasPressedThisFrame;
            input.Option1 = kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame;
            input.Option2 = kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame;
            input.Shove = kb.qKey.wasPressedThisFrame;
            var mouse = Mouse.current;
            input.Ladle = kb.rKey.wasPressedThisFrame || (mouse != null && mouse.rightButton.wasPressedThisFrame);
            input.SendWave = kb.gKey.wasPressedThisFrame;
            input.Aim = AimPoint();
            return input;
        }

        public Vector3 AimPoint()
        {
            var cam = Camera.main;
            var mouse = Mouse.current;
            Vector3 fallback = transform.position + transform.forward * 8f;
            if (cam == null || mouse == null) return fallback;
            var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out var hit, 200f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                Vector3 p = hit.point;
                Vector3 d = p - transform.position;
                d.y = 0f;
                if (d.magnitude > Balance.LadleRange) p = transform.position + d.normalized * Balance.LadleRange;
                p.y = CorbaMap.Instance != null ? CorbaMap.Instance.Height(p) : p.y;
                return p;
            }
            return fallback;
        }

        [Rpc(SendTo.Server)]
        void LadleRpc(Vector3 target)
        {
            if (Director != null && Director.ServerLadle(this, target)) LadleAnimRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        void LadleAnimRpc()
        {
            animator.Trigger("Shove", 1.6f);
            if (potRoot != null) Tween.Punch(potRoot, 0.3f, 0.3f);
        }

        void OwnerUpdate()
        {
            float dt = Time.deltaTime;
            var director = Director;
            bool canAct = director != null && (director.InPlay || director.Phase.Value == MatchPhase.Lobby);
            var map = CorbaMap.Instance;
            var input = canAct ? (IsBot.Value && Brain != null ? Brain() : IsBot.Value ? default : ReadHuman()) : default;

            float speed = GameData.CookSpeed * (input.Sprint ? GameData.SprintMult : 1f);
            if (Soup.Value >= 0) speed *= GameData.Soup((SoupType)Soup.Value).MoveMult;
            bool inMud = false;
            int bumpHit = -1;
            if (map != null)
            {
                for (int i = 0; i < map.Bumps.Count; i++)
                {
                    var b = map.Bumps[i];
                    if (CorbaMap.FlatDist(transform.position, b.Position) > b.Radius || Mathf.Abs(transform.position.y - b.Position.y) > 1f) continue;
                    if (b.Mud) inMud = true; else bumpHit = i;
                }
            }
            if (inMud) speed *= 0.6f;
            var rival = BySlot[1 - Mathf.Clamp(Slot.Value, 0, 1)];
            if (rival != null && rival.Soup.Value == (int)SoupType.Iskembe && CorbaMap.FlatDist(rival.transform.position, transform.position) < GameData.IskembeAuraRadius)
                speed *= 1f - GameData.IskembeAuraSlow;

            Vector3 target = Vector3.ClampMagnitude(input.Move, 1f) * speed;
            Vector3 prevVel = velocity;
            velocity = Vector3.MoveTowards(velocity, target, GameData.CookAccel * dt);
            Vector3 accel = (velocity - prevVel) / Mathf.Max(dt, 1e-4f);

            if (bumpHit >= 0 && bumpHit != insideBump && velocity.sqrMagnitude > 1f)
            {
                AddSloshImpulse(Random.insideUnitCircle.normalized * GameData.BumpImpulse);
                velocity *= 0.55f;
                Tween.Squash(visual, 0.18f, 0.3f);
            }
            insideBump = bumpHit;

            knockback = Vector3.MoveTowards(knockback, Vector3.zero, 10f * dt);
            bool grounded = controller.isGrounded;
            if (grounded && !wasGrounded && verticalSpeed < -5f)
            {
                float drop = Mathf.InverseLerp(5f, 14f, -verticalSpeed);
                AddSloshImpulse(Random.insideUnitCircle.normalized * Mathf.Lerp(1.2f, 2.6f, drop));
                Tween.Squash(visual, 0.3f, 0.45f);
                if (!IsBot.Value) CameraRig.Instance?.Shake(0.4f + drop);
                FX.Puff(transform.position, GameData.Hex("D8C8A8"));
            }
            wasGrounded = grounded;
            verticalSpeed = grounded ? -2f : verticalSpeed - 25f * dt;
            controller.Move((velocity + knockback + Vector3.up * verticalSpeed) * dt);

            if (velocity.sqrMagnitude > 0.05f)
            {
                var look = Quaternion.LookRotation(new Vector3(velocity.x, 0, velocity.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, look, 14f * dt);
            }

            SimulateSlosh(dt, accel, input.Sprint);

            spillSendTimer -= dt;
            if (spillSendTimer <= 0f && pendingSpill > 0.05f)
            {
                ReportSpillRpc(pendingSpill);
                pendingSpill = 0f;
                spillSendTimer = 0.1f;
            }

            UpdatePromptAndInteract(input);

            if (director != null && director.Phase.Value == MatchPhase.Raid && !director.IsDefender(Slot.Value))
            {
                if (input.Ladle && Soup.Value >= 0 && DisplayFill >= Balance.LadleCost) LadleRpc(input.Aim);
                if (input.SendWave)
                {
                    if (IsServer) director.ServerSendWave(Slot.Value);
                    else director.SendWaveRpc();
                }
            }

            if (input.Shove && canAct && MatchDirector.Now >= shoveReadyAt)
            {
                shoveReadyAt = MatchDirector.Now + GameData.ShoveCooldown;
                ShoveRpc();
            }
        }

        void SimulateSlosh(float dt, Vector3 accel, bool sprint)
        {
            Vector3 local = transform.InverseTransformDirection(accel);
            Vector2 drive = new Vector2(local.x, local.z) * GameData.SloshAccelGain * (sprint ? GameData.SprintSloshGain : 1f);
            int steps = Mathf.CeilToInt(dt / 0.008f);
            float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                Vector2 force = -GameData.SloshStiffness * slosh - GameData.SloshDamping * sloshVelocity - drive;
                sloshVelocity += force * h;
                slosh += sloshVelocity * h;
            }

            IsSpilling = false;
            float mag = slosh.magnitude;
            if (mag > GameData.SloshRim && DisplayFill > 0.01f && Soup.Value >= 0)
            {
                float mult = GameData.Soup((SoupType)Soup.Value).SpillMult;
                float amount = Mathf.Min((mag - GameData.SloshRim) * GameData.SpillRate * mult * dt, DisplayFill);
                pendingSpill += amount;
                IsSpilling = true;
                slosh *= 1f - Mathf.Clamp01(1.5f * dt);
            }
        }

        void AddSloshImpulse(Vector2 peak)
        {
            sloshVelocity += peak * ImpulseScale;
        }

        [Rpc(SendTo.Server)]
        void ReportSpillRpc(float amount)
        {
            if (amount <= 0f || Fill.Value <= 0f) return;
            amount = Mathf.Min(amount, Fill.Value);
            Fill.Value -= amount;
            Director?.AddSpill(Slot.Value, amount);
            if (Fill.Value < 0.5f)
            {
                Fill.Value = 0f;
                Soup.Value = -1;
            }
        }

        public Station NearestStation(StationKind kind, float range)
        {
            var map = CorbaMap.Instance;
            Station nearest = null;
            float best = range;
            foreach (var s in map.Stations)
            {
                if (s.Kind != kind) continue;
                float d = CorbaMap.FlatDist(transform.position, s.Position);
                if (d < best) { best = d; nearest = s; }
            }
            return nearest;
        }

        void UpdatePromptAndInteract(CookInput input)
        {
            Prompt = "";
            PromptPad = -1;
            var director = Director;
            var map = CorbaMap.Instance;
            if (director == null || map == null || !director.InPlay) return;
            bool defender = director.IsDefender(Slot.Value);

            var nearest = NearestStation(defender ? StationKind.DefenderCauldron : StationKind.AttackerCauldron, GameData.InteractRange + 0.6f);
            if (nearest != null)
            {
                var info = GameData.Soup(nearest.Soup);
                bool same = Soup.Value == (int)nearest.Soup;
                Prompt = same ? $"[E] Top up {info.Name}" : Soup.Value >= 0 ? $"[E] Swap to {info.Name} (dumps your pot!)" : $"[E] Fill pot with {info.Name}";
                if (input.Interact) FillRpc(nearest.Index);
                return;
            }

            if (!defender)
            {
                if (Soup.Value >= 0 && CorbaMap.FlatDist(transform.position, map.Ocak.Position) < GameData.InteractRange + 2f)
                {
                    Prompt = $"[E] Pour {Mathf.FloorToInt(DisplayFill)} ladles into the Ocak";
                    if (input.Interact) PourRpc();
                }
                else if (Soup.Value >= 0 && director.Phase.Value == MatchPhase.Raid && DisplayFill >= Balance.LadleCost)
                {
                    var l = Balance.Ladles[Soup.Value];
                    Prompt = $"[R] / Right-click: toss {l.Name} ladle ({Balance.LadleCost}) • {l.Blurb}";
                }
                return;
            }

            if (Soup.Value < 0) return;
            BuildPad pad = null;
            float best = GameData.InteractRange + 0.8f;
            foreach (var p in map.Pads)
            {
                float d = CorbaMap.FlatDist(transform.position, p.TowerPosition);
                if (d < best && Mathf.Abs(transform.position.y - p.TowerPosition.y) < 1f) { best = d; pad = p; }
            }
            if (pad == null) return;
            PromptPad = pad.Index;
            var soup = (SoupType)Soup.Value;
            var existing = director.PadBuild(pad.Index);
            if (existing != null)
            {
                var info = existing.Info;
                if (info.Soup != soup) { Prompt = $"Needs {GameData.Soup(info.Soup).Name} to upgrade"; return; }
                if (existing.Level.Value >= GameData.MaxLevel) { Prompt = $"{info.Name} is max level"; return; }
                int cost = GameData.UpgradeCost(info.Id, existing.Level.Value);
                Prompt = $"[E] Upgrade {info.Name} to Lv{existing.Level.Value + 1} ({cost})";
                if (input.Interact) BuildRpc(pad.Index, 0);
                return;
            }
            var a = GameData.Defence(GameData.DefenceOption(soup, 0));
            var b = GameData.Defence(GameData.DefenceOption(soup, 1));
            Prompt = $"[1] {a.Name} ({director.BuildCost(a.Id)})     [2] {b.Name} ({director.BuildCost(b.Id)})";
            if (input.Option1) BuildRpc(pad.Index, 0);
            else if (input.Option2) BuildRpc(pad.Index, 1);
        }

        [Rpc(SendTo.Server)]
        void FillRpc(int station)
        {
            if (Director != null && Director.ServerFill(this, station)) FeedbackRpc(0);
        }

        [Rpc(SendTo.Server)]
        void PourRpc()
        {
            if (Director != null && Director.ServerPour(this)) FeedbackRpc(1);
        }

        [Rpc(SendTo.Server)]
        void BuildRpc(int pad, int option)
        {
            if (Director != null && Director.ServerBuild(this, pad, option)) FeedbackRpc(2);
            else FeedbackRpc(3);
        }

        [Rpc(SendTo.Owner)]
        void FeedbackRpc(int code)
        {
            if (code == 3)
            {
                if (!IsBot.Value) CorbaUI.Instance?.Flash("Not enough soup in the pot!");
                return;
            }
            pendingSpill = 0f;
            Tween.Squash(visual, 0.2f, 0.35f);
            Acted?.Invoke(code == 0 ? "fill" : code == 1 ? "pour" : "build");
        }

        double serverShoveReady;

        [Rpc(SendTo.Server)]
        void ShoveRpc()
        {
            double now = MatchDirector.Now;
            if (now < serverShoveReady - 0.3) return;
            serverShoveReady = now + GameData.ShoveCooldown;
            ShoveAnimRpc();
            int other = 1 - Slot.Value;
            var victim = other >= 0 && other < 2 ? BySlot[other] : null;
            SelfSloshRpc();
            if (victim == null) return;
            Vector3 to = victim.transform.position - transform.position;
            to.y = 0f;
            if (to.magnitude > GameData.ShoveRange || Vector3.Dot(transform.forward, to.normalized) < 0.2f) return;
            victim.ShovedRpc(to.normalized);
        }

        [Rpc(SendTo.Owner)]
        void SelfSloshRpc()
        {
            AddSloshImpulse(new Vector2(0f, -GameData.ShoveSelfImpulse));
        }

        [Rpc(SendTo.Owner)]
        public void ShovedRpc(Vector3 direction)
        {
            knockback = direction * (GameData.ShoveKnockback * 6f);
            Vector3 local = transform.InverseTransformDirection(direction);
            AddSloshImpulse(new Vector2(local.x, local.z).normalized * GameData.ShoveImpulse);
            if (!IsBot.Value)
            {
                CorbaUI.Instance?.Flash("Dirsek! You got elbowed!");
                CameraRig.Instance?.Shake(0.8f);
            }
            HitAnimRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        void HitAnimRpc()
        {
            animator.Trigger("Hit");
            Tween.Squash(visual, 0.3f, 0.4f);
        }

        [Rpc(SendTo.ClientsAndHost)]
        void ShoveAnimRpc()
        {
            animator.Trigger("Shove", 1.4f);
        }

        public void Cheer() => animator.Trigger("Cheer");

        Transform reticle;

        void UpdateReticle()
        {
            var d = Director;
            bool show = IsOwner && !IsBot.Value && d != null && d.Phase.Value == MatchPhase.Raid && !d.IsDefender(Slot.Value) && Soup.Value >= 0 && DisplayFill >= Balance.LadleCost;
            if (show && reticle == null)
            {
                var go = new GameObject("LadleReticle");
                go.AddComponent<MeshFilter>().sharedMesh = FX.Quad;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = FX.Mat("fx_ring");
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                reticle = go.transform;
                reticle.rotation = Quaternion.Euler(90, 0, 0);
            }
            if (reticle == null) return;
            reticle.gameObject.SetActive(show);
            if (!show) return;
            reticle.position = AimPoint() + Vector3.up * 0.15f;
            float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.05f;
            reticle.localScale = Vector3.one * Balance.LadleRadius * 2f * pulse;
        }

        void UpdateVisual()
        {
            UpdateReticle();
            if (visual == null) return;
            float dt = Time.deltaTime;
            float spd;
            if (IsOwner) spd = new Vector3(velocity.x, 0, velocity.z).magnitude;
            else
            {
                float raw = CorbaMap.FlatDist(transform.position, lastRemotePos) / Mathf.Max(dt, 1e-4f);
                lastRemotePos = transform.position;
                remoteSpeed = Mathf.Lerp(remoteSpeed, Mathf.Min(raw, 12f), 1f - Mathf.Exp(-10f * dt));
                spd = remoteSpeed;
            }

            bool carrying = Soup.Value >= 0;
            string state = spd < 0.4f ? "Idle" : spd < GameData.CookSpeed * 1.15f ? "Walk" : "Run";
            if (carrying) state = "Carry" + state;
            float animSpeed = state.EndsWith("Idle") ? 1f : Mathf.Clamp(spd / (state.EndsWith("Run") ? 8.5f : 5.5f), 0.6f, 1.6f);
            animator.SetBase(state, animSpeed);

            potRoot.gameObject.SetActive(carrying);
            float fill01 = Mathf.Clamp01(DisplayFill / Capacity);
            bool hasSoup = carrying && fill01 > 0.005f;
            soupSurface.gameObject.SetActive(hasSoup);
            if (hasSoup)
            {
                Vector2 s = IsOwner ? slosh : Vector2.zero;
                soupSurface.localPosition = new Vector3(0, Mathf.Lerp(0.08f, 0.36f, fill01), 0);
                soupSurface.localRotation = Quaternion.Euler(s.y * 22f, 0, -s.x * 22f);
                var mat = Art.Soup(GameData.Soup((SoupType)Soup.Value).Liquid);
                foreach (var r in surfaceRenderers) if (r.sharedMaterial != mat) r.sharedMaterial = mat;
                potRoot.localRotation = Quaternion.Euler(-s.y * 6f, 0, s.x * 6f);
            }

            bool spilling = IsOwner ? IsSpilling : Fill.Value < lastSeenFill - 0.05f && carrying;
            if (Soup.Value >= 0 && Fill.Value > lastSeenFill + 5f) Tween.Punch(potRoot, 0.35f, 0.4f);
            lastSeenFill = Fill.Value;
            if (spilling && carrying && Random.value < 0.6f)
                FX.Drip(potRoot.position + Vector3.up * 0.3f, GameData.Soup((SoupType)Soup.Value).Liquid);
        }
    }
}
