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
        public const float Scale = 1.3f;
        public const float FullHeight = 1.28f;
        const float EmptyHeight = 0.42f;

        public static SofraPot Instance { get; private set; }

        Transform surface;
        Color color;
        Vector3 baseScale;
        float shown = 1f, phase, wobble;

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
