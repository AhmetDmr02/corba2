using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Corba
{
    public class BoilingPot : MonoBehaviour
    {
        Transform surface;
        Color color;
        float phase;
        Vector3 baseScale, basePos;

        public void Init(Transform surface, Color color)
        {
            this.surface = surface;
            this.color = color;
            phase = Random.value * 10f;
            baseScale = surface.localScale;
            basePos = surface.localPosition;
        }

        void Update()
        {
            if (surface == null) return;
            phase += Time.deltaTime;
            surface.localPosition = basePos + Vector3.up * Mathf.Sin(phase * 2.3f) * 0.025f;
            float wob = 1f + Mathf.Sin(phase * 3.1f) * 0.012f;
            surface.localScale = new Vector3(baseScale.x * wob, baseScale.y, baseScale.z * (2f - wob));
            surface.localRotation = Quaternion.Euler(Mathf.Sin(phase * 1.7f) * 1.5f, phase * 8f, Mathf.Cos(phase * 1.3f) * 1.5f);
            if (Random.value < Time.deltaTime * 7f)
            {
                var r = Random.insideUnitCircle * baseScale.x * 0.38f;
                FX.Bubble(surface.position + new Vector3(r.x, 0.02f, r.y), Color.Lerp(color, Color.white, 0.25f));
            }
            if (Random.value < Time.deltaTime * 3f)
                FX.Steam(surface.position + new Vector3(Random.Range(-0.5f, 0.5f), 0.2f, Random.Range(-0.5f, 0.5f)));
        }
    }

    /// <summary>
    /// The Sofra's cauldron. Visual only: its soup level mirrors the Sofra's hunger, and every bite
    /// a raider takes is shown as soup ladled out of it and flung down the road to the raider.
    /// </summary>
    public class SofraPot : MonoBehaviour
    {
        /// <summary>The Sofra pot is bigger than the kitchen and market cauldrons so it reads as the goal.</summary>
        public const float Scale = 1.15f;
        public const float FullHeight = 1.28f;
        const float EmptyHeight = 0.42f;

        public static SofraPot Instance { get; private set; }

        Transform surface;
        Color color;
        Vector3 baseScale;
        float shown = 1f, phase, wobble;

        // Finale: when the Sofra's hunger hits zero the pot boils over and topples toward the road,
        // then rights itself when the next half refills it. Driven only by synced state (phase + hunger),
        // so it plays at the same moment on every screen without extra network messages.
        enum PotState { Standing, Toppling, Toppled, Rising }
        PotState state = PotState.Standing;
        const float ToppleAngle = 94f;
        Vector3 restPos, tipDir, tipAxis, pivot;
        Quaternion restRot;
        float lyingAngle;
        static readonly Color[] Confetti =
        {
            GameData.Hex("F5B92E"), GameData.Hex("E8412C"), GameData.Hex("47C46B"), GameData.Hex("9A57D6"), GameData.Hex("38A3DB"), GameData.Hex("FFF4DC")
        };

        /// <summary>True while the pot is toppling, lying on its side or getting back up.</summary>
        public bool IsDown => state != PotState.Standing;

        /// <summary>Time the pot came to rest on its side (for the result screen to wait on); -1 while standing.</summary>
        public float ToppledAt { get; private set; } = -1f;

        bool matchFocused;

        public Vector3 SurfacePosition => surface != null ? surface.position : transform.position + Vector3.up * 1.2f;

        public void Init(Transform surface, Color color)
        {
            this.surface = surface;
            this.color = color;
            baseScale = surface.localScale;
            phase = Random.value * 10f;
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        float Target()
        {
            var d = MatchDirector.Instance;
            if (d == null || !d.IsSpawned || GameData.SofraHunger <= 0f) return 1f;
            return Mathf.Clamp01(d.Hunger.Value / GameData.SofraHunger);
        }

        void Update()
        {
            if (surface == null) return;
            float dt = Time.deltaTime;
            phase += dt;

            var director = MatchDirector.Instance;
            bool live = director != null && director.IsSpawned;
            // Topples when the Sofra is emptied, and always as the match-end finale (like a Nexus going down).
            bool fell = live && ((director.Phase.Value == MatchPhase.HalfResult && director.Hunger.Value <= 0.01f) || director.Phase.Value == MatchPhase.MatchOver);
            bool matchOver = live && director.Phase.Value == MatchPhase.MatchOver;
            if (state == PotState.Standing && fell) StartCoroutine(Topple(matchOver));
            else if (state == PotState.Toppled && live && director.InPlay && Target() > 0.5f) StartCoroutine(StandUp());
            // Pot already went over at the end of the last half: still lock the camera on it for the result.
            if (matchOver && state == PotState.Toppled && !matchFocused)
            {
                matchFocused = true;
                CameraRig.Instance?.Focus(restPos + tipDir * 1.5f, 600f, 0.6f);
            }
            if (!matchOver) matchFocused = false;
            if (state != PotState.Standing)
            {
                if (state == PotState.Toppled)
                {
                    Vector3 mouth = MouthPosition();
                    if (Random.value < dt * 4f) FX.Drip(mouth, color);
                    if (Random.value < dt * 2.5f) FX.Steam(mouth + Vector3.up * 0.3f);
                }
                return;
            }

            float target = Target();
            // Drains quickly when bitten, refills slowly (with sparkle) when a new half starts.
            bool refilling = target > shown + 0.05f;
            shown = refilling ? Mathf.MoveTowards(shown, target, dt * 0.6f) : Mathf.Lerp(shown, target, 1f - Mathf.Exp(-6f * dt));
            if (refilling && Random.value < dt * 12f) FX.Sparkle(surface.position, GameData.Hex("FFE08A"));

            wobble = Mathf.MoveTowards(wobble, 0f, dt * 1.6f);
            bool empty = shown < 0.02f;
            surface.gameObject.SetActive(!empty);
            if (empty) return;

            float h = Mathf.Lerp(EmptyHeight, FullHeight, shown);
            surface.localPosition = new Vector3(0f, h + Mathf.Sin(phase * 2.3f) * 0.02f, 0f);
            // The cauldron narrows toward the bottom, so a low surface is a smaller disc.
            float width = Mathf.Lerp(0.72f, 1f, shown) * (1f + Mathf.Sin(phase * 3.1f) * 0.012f);
            surface.localScale = new Vector3(baseScale.x * width, baseScale.y, baseScale.z * width);
            float slosh = wobble * 14f;
            surface.localRotation = Quaternion.Euler(Mathf.Sin(phase * 9f) * slosh + Mathf.Sin(phase * 1.7f) * 1.5f, phase * 8f, Mathf.Cos(phase * 7f) * slosh);

            if (Random.value < dt * 6f)
            {
                var r = Random.insideUnitCircle * baseScale.x * 0.3f * width;
                FX.Bubble(surface.position + new Vector3(r.x, 0.02f, r.y), Color.Lerp(color, Color.white, 0.25f));
            }
            if (Random.value < dt * 2.5f) FX.Steam(surface.position + new Vector3(Random.Range(-0.4f, 0.4f), 0.2f, Random.Range(-0.4f, 0.4f)));

            // Running low: the pot rattles now and then so the danger reads from across the map.
            bool low = shown < 0.3f;
            if (low && Random.value < dt * 1.2f) Tween.Punch(transform, 0.08f, 0.25f);
        }

        Vector3 MouthPosition() => restPos + tipDir * (1.15f * Scale + 0.5f) + Vector3.up * 0.5f;

        void SetTilt(float angle)
        {
            var q = Quaternion.AngleAxis(angle, tipAxis);
            transform.SetPositionAndRotation(pivot + q * (restPos - pivot), q * restRot);
        }

        IEnumerator Topple(bool matchEnd)
        {
            state = PotState.Toppling;
            ToppledAt = -1f;

            restPos = transform.position;
            restRot = transform.rotation;
            var map = CorbaMap.Instance;
            Vector3 toward = map != null ? map.SampleRoad(Mathf.Max(0f, map.RoadLength - 8f), 0f) - restPos : transform.forward;
            toward.y = 0f;
            tipDir = toward.sqrMagnitude > 0.01f ? toward.normalized : Vector3.forward;
            tipAxis = Vector3.Cross(Vector3.up, tipDir);
            pivot = restPos + tipDir * (1.15f * Scale);
            // Lock the camera, centred between the pot and where the soup will run. At match end it stays
            // locked through the result screen (released when the next match starts or on leaving).
            matchFocused = matchEnd;
            CameraRig.Instance?.Focus(restPos + tipDir * 1.5f, matchEnd ? 600f : 5.5f, 0.6f);
            // Give the camera time to arrive (and the last raider time to finish its dive).
            yield return new WaitForSeconds(matchEnd ? 0.9f : 0.7f);
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            surface.gameObject.SetActive(true);

            // 1) Boil-over: the pot rattles harder and harder, the soup heaves up over the rim.
            const float boil = 1.1f;
            for (float t = 0f; t < boil; t += Time.deltaTime)
            {
                float k = t / boil;
                float dt = Time.deltaTime;
                transform.SetPositionAndRotation(restPos + new Vector3(Random.Range(-1f, 1f), Random.Range(0f, 0.5f), Random.Range(-1f, 1f)) * 0.07f * k,
                    restRot * Quaternion.Euler(Random.Range(-4f, 4f) * k, 0f, Random.Range(-4f, 4f) * k));
                surface.localPosition = new Vector3(0f, FullHeight + 0.18f * k + Mathf.Sin(t * 30f) * 0.04f * k, 0f);
                float pulse = 1f + Mathf.Sin(t * 22f) * 0.05f * k;
                surface.localScale = new Vector3(baseScale.x * pulse, baseScale.y, baseScale.z * pulse);
                if (Random.value < dt * (20f + 40f * k)) FX.Bubble(surface.position + new Vector3(Random.Range(-0.7f, 0.7f), 0.05f, Random.Range(-0.7f, 0.7f)), Color.Lerp(color, Color.white, 0.3f));
                if (Random.value < dt * (6f + 14f * k)) FX.Steam(surface.position + new Vector3(Random.Range(-0.6f, 0.6f), 0.2f, Random.Range(-0.6f, 0.6f)));
                if (Random.value < dt * 12f * k)
                {
                    var d = Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward;
                    FX.Spill(surface.position + d * 0.9f * Scale, d, color);
                }
                CameraRig.Instance?.Shake(0.12f + 0.3f * k);
                yield return null;
            }
            FX.Raw("fx_star", surface.position + Vector3.up * 0.4f, Vector3.zero, 0f, 0.25f, 0.6f, 4.5f, new Color(1f, 0.95f, 0.75f, 0.9f));

            // 2) Tip: falls over toward the road, accelerating like a real topple, soup pouring out of the mouth.
            const float tip = 0.42f;
            for (float t = 0f; t < tip; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / tip);
                SetTilt(ToppleAngle * k * k);
                if (k > 0.35f)
                    for (int i = 0; i < 3; i++)
                        FX.Spill(surface.position, (tipDir + Random.insideUnitSphere * 0.35f).normalized, color);
                yield return null;
            }
            SetTilt(ToppleAngle);

            // 3) Impact.
            Impact();

            // 4) Settle: a couple of heavy rocks on its side.
            const float settle = 0.7f;
            for (float t = 0f; t < settle; t += Time.deltaTime)
            {
                float k = t / settle;
                SetTilt(ToppleAngle - Mathf.Sin(k * Mathf.PI * 2.5f) * (1f - k) * 10f);
                yield return null;
            }
            SetTilt(ToppleAngle);
            lyingAngle = ToppleAngle;
            state = PotState.Toppled;
            ToppledAt = Time.time;
        }

        void Impact()
        {
            CameraRig.Instance?.Shake(1.5f);
            surface.gameObject.SetActive(false);
            shown = 0f;
            Vector3 mouth = MouthPosition();
            Vector3 ground = new Vector3(restPos.x, restPos.y + 0.15f, restPos.z);

            FX.Raw("fx_star", mouth + Vector3.up * 0.6f, Vector3.zero, 0f, 0.35f, 1f, 7f, new Color(1f, 0.97f, 0.85f, 0.95f));
            FX.Ring(ground, color, 6f);
            FX.Ring(ground + tipDir * 2f, new Color(1f, 0.95f, 0.8f), 9f);
            FX.Splash(mouth, color, 45);
            for (int i = 0; i < 26; i++)
                FX.Spill(mouth, (Quaternion.Euler(0, Random.Range(-60f, 60f), 0) * tipDir + Vector3.up * Random.Range(0f, 0.4f)).normalized, color);
            for (int i = 0; i < 48; i++)
            {
                var v = Random.insideUnitSphere * Random.Range(3f, 7f);
                v.y = Random.Range(5f, 10f);
                FX.Raw("fx_star", restPos + Vector3.up * 1.6f, v, 9f, Random.Range(1.3f, 2f), Random.Range(0.25f, 0.42f), 0.12f, Confetti[Random.Range(0, Confetti.Length)]);
            }
            for (int i = 0; i < 10; i++) FX.Steam(mouth + Random.insideUnitSphere * 1.2f);
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                FX.Raw("fx_puff", ground + d * 0.8f, d * 7f + Vector3.up * 0.4f, 0f, 0.8f, 0.6f, 1.8f, new Color(0.9f, 0.82f, 0.66f, 0.6f));
            }
            StartCoroutine(SpreadWave(mouth));
            CorbaUI.Instance?.Announce(Loc.T("announce.sofraFell"), GameData.Hex("F2B33D"));
        }

        /// <summary>The spilled soup rolls down the road as a widening wave, leaving a big puddle behind.</summary>
        IEnumerator SpreadWave(Vector3 from)
        {
            for (int i = 0; i < 14; i++)
            {
                float dist = 0.8f + i * 0.55f;
                int count = 1 + i / 4;
                for (int j = 0; j < count; j++)
                {
                    var dir = Quaternion.Euler(0, Random.Range(-38f, 38f), 0) * tipDir;
                    var p = from + dir * dist;
                    FX.Splat(p, color, Random.Range(1.4f, 2.2f));
                    FX.Raw("fx_blob", p + Vector3.up * 0.2f, dir * 2f + Vector3.up * Random.Range(1.5f, 3f), 12f, 0.5f, Random.Range(0.2f, 0.35f), 0.05f, color);
                }
                FX.Raw("fx_puff", from + tipDir * dist + Vector3.up * 0.3f, tipDir * 1.5f, 0f, 0.5f, 0.6f, 1.4f, new Color(1f, 1f, 1f, 0.35f));
                yield return new WaitForSeconds(0.05f);
            }
        }

        IEnumerator StandUp()
        {
            state = PotState.Rising;
            ToppledAt = -1f;
            const float dur = 0.75f;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float k = t / dur;
                SetTilt(lyingAngle * (1f - Tween.Evaluate(Ease.OutBack, k)));
                if (Random.value < Time.deltaTime * 20f) FX.Sparkle(transform.position + Vector3.up * 1.5f, GameData.Hex("FFE08A"));
                yield return null;
            }
            transform.SetPositionAndRotation(restPos, restRot);
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
            FX.Ring(restPos + Vector3.up * 0.15f, GameData.Hex("FFE08A"), 3f);
            shown = 0f;
            surface.gameObject.SetActive(true);
            state = PotState.Standing;
        }

        /// <summary>A raider reached the feast: the pot shudders as it is bitten (the raider itself dives in, see <see cref="PotDiver"/>).</summary>
        public void Bite(int amount)
        {
            if (surface == null) return;
            wobble = Mathf.Max(wobble, 0.4f);
            FX.Steam(surface.position + Vector3.up * 0.1f);
        }

        /// <summary>Something landed in the soup: big splash over the rim, the pot jolts and the surface sloshes.</summary>
        public void Dunk(Color raiderColor, float size)
        {
            if (surface == null) return;
            wobble = 1f;
            Tween.Punch(transform, 0.25f, 0.4f);
            Vector3 lip = surface.position + Vector3.up * 0.15f;
            FX.Splash(lip, color, Mathf.RoundToInt(14 * size));
            FX.Splash(lip, raiderColor, Mathf.RoundToInt(6 * size));
            FX.Ring(lip, color, 1.6f * size);
            for (int i = 0; i < 4; i++) FX.Steam(lip + Random.insideUnitSphere * 0.5f);
            for (int i = 0; i < 5; i++)
            {
                var dir = Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward;
                FX.Spill(lip + dir * 0.9f * Scale, dir, color);
            }
        }
    }

    /// <summary>
    /// A raider's model, detached when the raider reaches the Sofra: it hops in an arc into the cauldron,
    /// tumbling like a dropped ingredient, and is gone in a splash. Purely cosmetic.
    /// </summary>
    public class PotDiver : MonoBehaviour
    {
        Vector3 from, to;
        Quaternion startRot;
        float t, duration, peak, size;
        Vector3 startScale;
        Color soupColor;

        public static void Launch(Transform visual, Color soupColor, float size)
        {
            var pot = SofraPot.Instance;
            if (visual == null || pot == null) return;
            Tween.KillAll(visual);  // stop hit punches / bobs so they do not fight the dive
            visual.SetParent(null, true);
            var d = visual.gameObject.AddComponent<PotDiver>();
            d.from = visual.position;
            d.to = pot.SurfacePosition + Random.insideUnitSphere * 0.3f;
            d.to.y = pot.SurfacePosition.y - 0.1f;
            d.peak = Mathf.Max(d.from.y, d.to.y) + 2.4f;
            d.duration = Random.Range(0.5f, 0.65f);
            d.startRot = visual.rotation;
            d.startScale = visual.localScale;
            d.soupColor = soupColor;
            d.size = size;
            FX.Dust(d.from);
        }

        void Update()
        {
            t += Time.deltaTime / duration;
            if (t >= 1f)
            {
                SofraPot.Instance?.Dunk(soupColor, Mathf.Clamp(size, 0.7f, 1.6f));
                Destroy(gameObject);
                return;
            }
            // Parabola through the peak, ending in the soup.
            Vector3 p = Vector3.Lerp(from, to, t);
            float arc = 4f * t * (1f - t);
            p.y = Mathf.Lerp(from.y, to.y, t) + arc * (peak - Mathf.Lerp(from.y, to.y, 0.5f));
            transform.position = p;
            Vector3 flat = to - from;
            flat.y = 0f;
            var face = flat.sqrMagnitude > 0.01f ? Quaternion.LookRotation(flat) : startRot;
            // Forward somersault into the pot.
            transform.rotation = face * Quaternion.Euler(t * 360f, 0f, 0f);
            // Squash at take-off, then shrink as it sinks in at the very end.
            float s = t < 0.15f ? Mathf.Lerp(0.85f, 1.1f, t / 0.15f) : t > 0.8f ? Mathf.Lerp(1f, 0.35f, (t - 0.8f) / 0.2f) : 1f;
            transform.localScale = startScale * s;
        }
    }

    public class PadMarker : MonoBehaviour
    {
        BuildPad pad;
        Transform ring, icon;
        MeshRenderer ringRenderer, iconRenderer;
        MaterialPropertyBlock block;
        float t;
        bool wasVisible;

        public void Init(BuildPad p)
        {
            pad = p;
            block = new MaterialPropertyBlock();
            ring = Quad("Ring", "fx_ring", 3.2f, true);
            ring.localPosition = Vector3.up * 0.12f;
            icon = Quad("Icon", p.HighGround ? "icon_pad_high" : "icon_pad", 1.3f, false);
            icon.localPosition = Vector3.up * 2.2f;
            ringRenderer = ring.GetComponent<MeshRenderer>();
            iconRenderer = icon.GetComponent<MeshRenderer>();
            ring.gameObject.SetActive(false);
            icon.gameObject.SetActive(false);
        }

        Transform Quad(string name, string tex, float size, bool flat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * size;
            if (flat) go.transform.localRotation = Quaternion.Euler(90, 0, 0);
            go.AddComponent<MeshFilter>().sharedMesh = FX.Quad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = FX.Mat(tex);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        void Update()
        {
            var director = MatchDirector.Instance;
            var me = ChefController.Local;
            bool defender = director != null && me != null && director.IsDefender(me.Slot.Value) && director.InPlay;
            bool occupied = director != null && director.PadBuild(pad.Index) != null;
            bool carrying = me != null && me.Soup.Value >= 0;
            bool visible = defender && !occupied;
            if (visible != wasVisible)
            {
                ring.gameObject.SetActive(visible);
                icon.gameObject.SetActive(visible);
                if (visible) icon.Pop(0.2f, 0.5f);
                wasVisible = visible;
            }
            if (!visible) return;

            t += Time.deltaTime;
            float pulse = carrying ? 0.75f + Mathf.Sin(t * 5f) * 0.25f : 0.35f;
            var col = pad.HighGround ? GameData.Hex("FFD86A") : GameData.Hex("FFF4DC");
            block.SetColor("_BaseColor", new Color(col.r, col.g, col.b, pulse));
            ringRenderer.SetPropertyBlock(block);
            block.SetColor("_BaseColor", new Color(1, 1, 1, carrying ? 1f : 0.6f));
            iconRenderer.SetPropertyBlock(block);
            ring.localScale = Vector3.one * (3.2f + (carrying ? Mathf.Sin(t * 5f) * 0.15f : 0f));
            icon.localPosition = Vector3.up * (2.2f + Mathf.Sin(t * 3f) * 0.18f);
            if (Camera.main != null) icon.rotation = Camera.main.transform.rotation;
        }
    }

    public class WorldLabel : MonoBehaviour
    {
        public static WorldLabel Create(Transform parent, Vector3 pos, System.Func<string> title, System.Func<string> subtitle, Color color, string glyph = null, float fadeRange = 0f)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(360, 120);
            rt.localScale = Vector3.one * 0.012f;

            var bg = new GameObject("Bg", typeof(RectTransform));
            bg.transform.SetParent(go.transform, false);
            var brt = (RectTransform)bg.transform;
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            var img = bg.AddComponent<Image>();
            img.sprite = Art.Sprite("ui_chip");
            img.type = Image.Type.Sliced;
            img.color = color;

            var label = go.AddComponent<WorldLabel>();
            if (!string.IsNullOrEmpty(glyph))
            {
                var icon = new GameObject("Glyph", typeof(RectTransform));
                icon.transform.SetParent(go.transform, false);
                label.glyph = (RectTransform)icon.transform;
                label.glyph.sizeDelta = new Vector2(86, 86);
                var iimg = icon.AddComponent<Image>();
                iimg.sprite = Art.Sprite(glyph);
                iimg.preserveAspect = true;
                iimg.color = new Color(1f, 0.97f, 0.88f);
            }
            label.titleText = AddText(go.transform, 50, new Color(1f, 0.97f, 0.88f), CorbaUI.TitleFont);
            label.subText = AddText(go.transform, 24, new Color(1f, 0.93f, 0.8f), CorbaUI.BodyFont);
            label.title = title;
            label.subtitle = subtitle;
            label.Refresh();
            Loc.Changed += label.Refresh;
            label.fadeRange = fadeRange;
            label.group = go.AddComponent<CanvasGroup>();
            if (fadeRange > 0f) label.group.alpha = 0f;
            return label;
        }

        static Text AddText(Transform parent, int size, Color c, Font font)
        {
            var go = new GameObject("T", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(360, 60);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = c;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var o = go.AddComponent<Outline>();
            o.effectColor = new Color(0.16f, 0.08f, 0.03f, 0.9f);
            o.effectDistance = new Vector2(2, -2);
            return t;
        }

        System.Func<string> title, subtitle;
        Text titleText, subText;
        RectTransform glyph;

        /// <summary>Re-reads both lines and grows the chip so the longer one always sits inside it.</summary>
        void Refresh()
        {
            titleText.text = title();
            subText.text = subtitle();
            float glyphRoom = glyph != null ? 100f : 0f;
            float textWidth = Mathf.Max(titleText.preferredWidth, subText.preferredWidth);
            float width = Mathf.Max(300f, textWidth + 64f + glyphRoom);
            ((RectTransform)transform).sizeDelta = new Vector2(width, 120);
            float textX = glyphRoom * 0.5f;
            titleText.rectTransform.sizeDelta = subText.rectTransform.sizeDelta = new Vector2(textWidth + 20f, 60);
            titleText.rectTransform.anchoredPosition = new Vector2(textX, 16);
            subText.rectTransform.anchoredPosition = new Vector2(textX, -34);
            if (glyph != null) glyph.anchoredPosition = new Vector2(-width * 0.5f + 64f, 0);
        }

        void OnDestroy() => Loc.Changed -= Refresh;

        static WorldLabel focus, nextFocus;
        static float nextFocusDist;
        static int focusFrame;

        float t;
        float fadeRange;
        CanvasGroup group;

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            t += Time.deltaTime;
            if (fadeRange > 0f)
            {
                // Neighbouring cauldron labels overlap, so only the one nearest the local cook is shown (decided a frame late).
                if (focusFrame != Time.frameCount)
                {
                    focusFrame = Time.frameCount;
                    focus = nextFocus;
                    nextFocus = null;
                    nextFocusDist = float.MaxValue;
                }
                var me = ChefController.Local;
                float d = me != null ? CorbaMap.FlatDist(me.transform.position, transform.position) : float.MaxValue;
                if (d < fadeRange && d < nextFocusDist)
                {
                    nextFocus = this;
                    nextFocusDist = d;
                }
                float target = focus == this ? 1f : 0f;
                group.alpha = Mathf.MoveTowards(group.alpha, target, Time.deltaTime * 4f);
                if (group.alpha <= 0.001f) return;
            }
            transform.rotation = cam.transform.rotation;
            float dist = Vector3.Distance(cam.transform.position, transform.position);
            transform.localScale = Vector3.one * Mathf.Clamp(dist * 0.0006f, 0.008f, 0.03f) * (1f + Mathf.Sin(t * 2f) * 0.02f);
        }
    }
}
