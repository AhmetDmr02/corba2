using System.Collections.Generic;
using UnityEngine;

namespace Corba
{
    public class FX : MonoBehaviour
    {
        static FX instance;

        class Particle
        {
            public Transform T;
            public MeshRenderer R;
            public Vector3 Velocity;
            public float Gravity;
            public float Life, MaxLife;
            public float Size, EndSize;
            public Color Color;
            public bool Flat;
            public bool FadeIn;
            public bool Active;
        }

        class Shot
        {
            public Vector3 From, To;
            public float Flight, T;
            public bool Lob;
            public Color Color;
            public Transform Ball;
        }

        class Cloudy
        {
            public Vector3 At;
            public float Radius, Until;
            public Color Color;
        }

        readonly List<Particle> pool = new List<Particle>();
        readonly List<Shot> shots = new List<Shot>();
        readonly List<Cloudy> clouds = new List<Cloudy>();
        readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        MaterialPropertyBlock block;
        Mesh quad;
        Font font;

        public static Mesh Quad => Instance.quad;

        static FX Instance
        {
            get
            {
                if (instance == null) instance = new GameObject("FX").AddComponent<FX>();
                return instance;
            }
        }

        void Awake()
        {
            instance = this;
            block = new MaterialPropertyBlock();
            quad = BuildQuad();
            font = CorbaUI.BodyFont;
        }

        static Mesh BuildQuad()
        {
            var m = new Mesh { name = "FxQuad" };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            m.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            m.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        public static Material Mat(string texture)
        {
            var fx = Instance;
            if (fx.mats.TryGetValue(texture, out var m)) return m;
            var baseMat = Resources.Load<Material>("Corba/PaintedFxBase");
            m = baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Corba/PaintedFx"));
            var tex = Resources.Load<Texture2D>("Corba/Textures/" + texture);
            if (tex != null) m.SetTexture("_BaseMap", tex);
            fx.mats[texture] = m;
            return m;
        }

        Particle Get(string texture)
        {
            Particle p = null;
            foreach (var q in pool)
                if (!q.Active) { p = q; break; }
            if (p == null)
            {
                var go = new GameObject("fx");
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = quad;
                var r = go.AddComponent<MeshRenderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                p = new Particle { T = go.transform, R = r };
                pool.Add(p);
            }
            p.R.sharedMaterial = Mat(texture);
            p.Active = true;
            p.T.gameObject.SetActive(true);
            p.Flat = false;
            p.FadeIn = false;
            return p;
        }

        static Particle Emit(string tex, Vector3 pos, Vector3 vel, float gravity, float life, float size, float endSize, Color c, bool flat = false)
        {
            var p = Instance.Get(tex);
            p.T.position = pos;
            p.Velocity = vel;
            p.Gravity = gravity;
            p.Life = p.MaxLife = life;
            p.Size = size;
            p.EndSize = endSize;
            p.Color = c;
            p.Flat = flat;
            return p;
        }

        /// <summary>Raw particle for one-off set pieces (e.g. the Sofra toppling) that need their own mix.</summary>
        public static void Raw(string tex, Vector3 pos, Vector3 vel, float gravity, float life, float size, float endSize, Color c, bool flat = false)
            => Emit(tex, pos, vel, gravity, life, size, endSize, c, flat);

        public static void Splash(Vector3 pos, Color c, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var v = Random.insideUnitSphere * 2.5f;
                v.y = Mathf.Abs(v.y) * 1.5f + 2.5f;
                Emit("fx_blob", pos, v, 12f, Random.Range(0.45f, 0.8f), Random.Range(0.18f, 0.32f), 0.05f, c);
            }
        }

        public static void Drip(Vector3 pos, Color c)
        {
            var v = Random.insideUnitSphere * 1.2f;
            v.y = Random.Range(0.5f, 1.5f);
            Emit("fx_blob", pos, v, 14f, 0.45f, Random.Range(0.12f, 0.2f), 0.06f, c);
            if (Random.value < 0.25f && Physics.Raycast(pos, Vector3.down, out var hit, 4f))
                Emit("fx_splat", hit.point + Vector3.up * 0.08f, Vector3.zero, 0f, 6f, Random.Range(0.4f, 0.7f), 0.5f, new Color(c.r, c.g, c.b, 0.85f), true);
        }

        /// <summary>Soup flung over the pot rim: a thick arcing blob that leaves a puddle where it lands.</summary>
        public static void Spill(Vector3 pos, Vector3 dir, Color c)
        {
            var v = dir * Random.Range(1.8f, 3.2f) + Vector3.up * Random.Range(1.5f, 2.8f) + Random.insideUnitSphere * 0.6f;
            Emit("fx_blob", pos, v, 14f, Random.Range(0.4f, 0.6f), Random.Range(0.22f, 0.36f), 0.1f, c);
            if (Random.value < 0.25f && Physics.Raycast(pos + dir * 0.7f, Vector3.down, out var hit, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                Emit("fx_splat", hit.point + Vector3.up * 0.08f, Vector3.zero, 0f, 6f, Random.Range(0.5f, 0.8f), Random.Range(0.9f, 1.3f), new Color(c.r, c.g, c.b, 0.92f), true);
        }

        /// <summary>Tower firing flash: a coloured burst and a few droplets at the barrel.</summary>
        public static void Muzzle(Vector3 pos, Color c, bool lob)
        {
            var light = Color.Lerp(c, Color.white, 0.55f);
            for (int i = 0; i < (lob ? 7 : 4); i++)
                Emit("fx_puff", pos + Random.insideUnitSphere * 0.25f, Random.insideUnitSphere * 1.4f + Vector3.up * 0.8f, -1f, Random.Range(0.35f, 0.55f), 0.4f, lob ? 1.4f : 1f, new Color(light.r, light.g, light.b, 0.75f));
            Emit("fx_star", pos, Vector3.zero, 0f, 0.18f, lob ? 1.4f : 0.9f, 0.1f, new Color(1f, 0.97f, 0.85f, 0.95f));
            for (int i = 0; i < 3; i++) Drip(pos, c);
        }

        /// <summary>Shot landing: splash, a flat shock ring on the ground and, for lobbed shots, a lasting stain.</summary>
        public static void Impact(Vector3 pos, Color c, bool lob)
        {
            Splash(pos, c, lob ? 18 : 8);
            Emit("fx_star", pos, Vector3.zero, 0f, 0.2f, lob ? 1.8f : 1.1f, 0.2f, new Color(1f, 0.97f, 0.85f, 0.9f));
            Vector3 ground = pos;
            if (Physics.Raycast(pos + Vector3.up, Vector3.down, out var hit, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) ground = hit.point;
            Emit("fx_ring", ground + Vector3.up * 0.1f, Vector3.zero, 0f, 0.45f, 0.3f, lob ? 4f : 1.8f, new Color(c.r, c.g, c.b, 0.95f), true);
            for (int i = 0; i < (lob ? 6 : 3); i++)
                Emit("fx_puff", pos + Random.insideUnitSphere * 0.3f, Random.insideUnitSphere * 1.6f + Vector3.up * 0.6f, -0.5f, Random.Range(0.4f, 0.6f), 0.4f, 1.1f, new Color(c.r, c.g, c.b, 0.6f));
            if (lob) Splat(pos, c, 1.6f);
        }

        public static void Mote(Vector3 pos)
        {
            var c = new Color(1f, 0.93f, 0.7f, 0.55f);
            Emit("fx_blob", pos, new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.05f, 0.25f), Random.Range(-0.3f, 0.3f)), -0.02f, Random.Range(4f, 7f), 0.09f, 0.05f, c);
        }

        /// <summary>Low drifting haze puff that fades in and out, used to thicken the air near the ground.</summary>
        public static void Mist(Vector3 pos)
        {
            var c = new Color(0.93f, 0.95f, 0.96f, 0.16f);
            var v = new Vector3(Random.Range(0.15f, 0.45f), 0.02f, Random.Range(-0.15f, 0.15f));
            Emit("fx_puff", pos, v, 0f, Random.Range(6f, 9f), Random.Range(3.5f, 5f), Random.Range(6f, 8f), c).FadeIn = true;
        }

        public static void Splat(Vector3 pos, Color c, float size)
        {
            if (Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out var hit, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                Emit("fx_splat", hit.point + Vector3.up * 0.07f, Vector3.zero, 0f, 7f, size * 0.6f, size, new Color(c.r, c.g, c.b, 0.9f), true);
        }

        public static void Dust(Vector3 pos)
        {
            Emit("fx_puff", pos + Random.insideUnitSphere * 0.2f, new Vector3(Random.Range(-0.4f, 0.4f), 0.5f, Random.Range(-0.4f, 0.4f)), 0f, 0.6f, 0.25f, 0.7f, new Color(0.85f, 0.76f, 0.6f, 0.6f));
        }

        public static void PopText(Vector3 pos, string text, Color c, float size)
        {
            FloatText(pos, text, c, size);
        }

        public static void Bubble(Vector3 pos, Color c)
        {
            Emit("fx_bubble", pos, Vector3.up * 0.15f, 0f, Random.Range(0.4f, 0.8f), 0.05f, Random.Range(0.25f, 0.45f), c);
        }

        public static void Steam(Vector3 pos)
        {
            Emit("fx_puff", pos, new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.8f, 1.4f), Random.Range(-0.2f, 0.2f)), -0.3f, Random.Range(1.4f, 2.2f), 0.5f, 1.8f, new Color(1f, 1f, 1f, 0.35f));
        }

        public static void Puff(Vector3 pos, Color c)
        {
            for (int i = 0; i < 7; i++)
                Emit("fx_puff", pos + Random.insideUnitSphere * 0.4f, Random.insideUnitSphere * 1.2f + Vector3.up, -1f, Random.Range(0.6f, 0.9f), 0.5f, 1.2f, c);
        }

        public static void Ember(Vector3 pos)
        {
            Emit("fx_blob", pos + Random.insideUnitSphere * 0.3f, new Vector3(Random.Range(-0.3f, 0.3f), 1.5f, Random.Range(-0.3f, 0.3f)), -0.5f, 0.6f, 0.14f, 0.02f, GameData.Hex("FF8A2E"));
        }

        public static void Sparkle(Vector3 pos, Color c)
        {
            Emit("fx_star", pos + Random.insideUnitSphere * 0.4f, Vector3.up * 0.8f, 0f, 0.6f, 0.25f, 0.05f, c);
        }

        public static void Ring(Vector3 pos, Color c, float radius)
        {
            Emit("fx_ring", pos, Vector3.zero, 0f, 0.75f, 0.5f, radius * 2f, new Color(c.r, c.g, c.b, 1f), true);
            Emit("fx_ring", pos + Vector3.up * 0.05f, Vector3.zero, 0f, 0.5f, 0.3f, radius * 1.4f, new Color(Mathf.Lerp(c.r, 1f, 0.5f), Mathf.Lerp(c.g, 1f, 0.5f), Mathf.Lerp(c.b, 1f, 0.5f), 0.8f), true);
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Emit("fx_puff", pos + dir * 0.4f, dir * radius * 1.4f + Vector3.up * 0.3f, 0f, 0.6f, 0.4f, 0.9f, new Color(c.r, c.g, c.b, 0.7f));
            }
        }

        public static void Cloud(Vector3 pos, Color c, float radius, float duration)
        {
            Instance.clouds.Add(new Cloudy { At = pos, Radius = radius, Until = Time.time + duration, Color = c });
        }

        public static void Projectile(Vector3 from, Vector3 to, Color c, float flight, bool lob)
        {
            var fx = Instance;
            var ball = new GameObject("shot").transform;
            ball.SetParent(fx.transform, false);
            ball.position = from;
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(mesh.GetComponent<Collider>());
            mesh.transform.SetParent(ball, false);
            mesh.transform.localScale = Vector3.one * (lob ? 0.75f : 0.42f);
            mesh.GetComponent<MeshRenderer>().sharedMaterial = Art.Soup(c);
            fx.shots.Add(new Shot { From = from, To = to, Flight = Mathf.Max(flight, 0.05f), Lob = lob, Color = c, Ball = ball });
        }

        public static void FloatText(Vector3 pos, string text, Color c) => FloatText(pos, text, c, 1f);

        public static void FloatText(Vector3 pos, string text, Color c, float size)
        {
            var fx = Instance;
            var go = new GameObject("float");
            go.transform.position = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = fx.font;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = 0.06f * size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = c;
            tm.fontStyle = FontStyle.Bold;
            go.GetComponent<MeshRenderer>().sharedMaterial = fx.font.material;
            go.AddComponent<FloatTextMover>();
            go.transform.Pop(0.2f, 0.35f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var cam = Camera.main;
            Quaternion face = cam != null ? cam.transform.rotation : Quaternion.identity;

            foreach (var p in pool)
            {
                if (!p.Active) continue;
                p.Life -= dt;
                if (p.Life <= 0f)
                {
                    p.Active = false;
                    p.T.gameObject.SetActive(false);
                    continue;
                }
                p.Velocity.y -= p.Gravity * dt;
                p.T.position += p.Velocity * dt;
                float k = 1f - p.Life / p.MaxLife;
                p.T.localScale = Vector3.one * Mathf.Lerp(p.Size, p.EndSize, k);
                p.T.rotation = p.Flat ? Quaternion.Euler(90f, 0, 0) : face;
                var c = p.Color;
                c.a *= p.FadeIn ? Mathf.Sin(k * Mathf.PI) : p.Flat ? Mathf.Clamp01(p.Life / Mathf.Min(1.2f, p.MaxLife)) : 1f - k * k;
                block.SetColor("_BaseColor", c);
                p.R.SetPropertyBlock(block);
            }

            for (int i = shots.Count - 1; i >= 0; i--)
            {
                var s = shots[i];
                s.T += dt / s.Flight;
                if (s.T >= 1f || s.Ball == null)
                {
                    if (s.Ball != null)
                    {
                        Impact(s.To, s.Color, s.Lob);
                        Destroy(s.Ball.gameObject);
                    }
                    shots.RemoveAt(i);
                    continue;
                }
                Vector3 p = Vector3.Lerp(s.From, s.To, s.T);
                if (s.Lob) p.y += Mathf.Sin(s.T * Mathf.PI) * 3.5f;
                s.Ball.position = p;
                // A thick trail plus a bright core so every shot reads at a glance, even in a busy raid.
                // Frame-rate independent (~70 blobs a second) so busy raids stay cheap on fast machines.
                if (Random.value < dt * 70f) Emit("fx_blob", p + Random.insideUnitSphere * 0.08f, Vector3.zero, 2f, 0.35f, s.Lob ? 0.36f : 0.26f, 0.02f, s.Color);
                if (Random.value < dt * 25f) Emit("fx_star", p, Vector3.zero, 0f, 0.22f, s.Lob ? 0.34f : 0.24f, 0.02f, Color.Lerp(s.Color, Color.white, 0.65f));
            }

            for (int i = clouds.Count - 1; i >= 0; i--)
            {
                var c = clouds[i];
                if (Time.time > c.Until) { clouds.RemoveAt(i); continue; }
                if (Random.value < dt * 30f)
                {
                    var off = Random.insideUnitCircle * c.Radius;
                    Emit("fx_puff", c.At + new Vector3(off.x, Random.Range(0f, 0.6f), off.y), Vector3.up * 0.4f, 0f, 1.2f, 0.8f, 1.8f, new Color(c.Color.r, c.Color.g, c.Color.b, 0.55f));
                }
            }
        }
    }

    public class FloatTextMover : MonoBehaviour
    {
        float life = 1.1f;

        void Update()
        {
            life -= Time.deltaTime;
            transform.position += Vector3.up * Time.deltaTime * 1.4f;
            if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;
            if (life <= 0f) Destroy(gameObject);
        }
    }

    /// <summary>
    /// Camera-facing bar above a unit or build: a health fill with a lagging damage trail, and an optional thin timer strip
    /// underneath for re-arm or disable countdowns.
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        static readonly Color Frame = new Color(0.16f, 0.09f, 0.05f, 0.92f);
        static readonly Color Trail = new Color(1f, 0.94f, 0.78f, 0.95f);
        static readonly Color Full = GameData.Hex("7FD45A");
        static readonly Color Half = GameData.Hex("F2C53D");
        static readonly Color Low = GameData.Hex("E5432B");

        MaterialPropertyBlock block;
        Transform fill, trail, timerFill, bars;
        MeshRenderer fillR, trailR, timerR;
        GameObject healthRoot, timerRoot;
        float width, shown = 1f, trailFrac = 1f, trailHold, flash;
        Color flashColor;
        bool hasValue;

        public static HealthBar Create(Transform parent, float height, float width, bool health = true)
        {
            var go = new GameObject("HealthBar");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.up * height;
            var hb = go.AddComponent<HealthBar>();
            hb.width = width;
            hb.block = new MaterialPropertyBlock();
            hb.bars = new GameObject("Bars").transform;
            hb.bars.SetParent(go.transform, false);

            hb.healthRoot = new GameObject("Health");
            hb.healthRoot.transform.SetParent(hb.bars, false);
            hb.Quad(hb.healthRoot.transform, new Vector3(width + 0.14f, 0.3f, 1f), Vector3.zero, Frame);
            hb.trail = hb.Quad(hb.healthRoot.transform, new Vector3(width, 0.18f, 1f), new Vector3(0, 0, -0.02f), Trail, out hb.trailR);
            hb.fill = hb.Quad(hb.healthRoot.transform, new Vector3(width, 0.18f, 1f), new Vector3(0, 0, -0.04f), Full, out hb.fillR);
            hb.healthRoot.SetActive(health);

            hb.timerRoot = new GameObject("Timer");
            hb.timerRoot.transform.SetParent(hb.bars, false);
            hb.timerRoot.transform.localPosition = new Vector3(0, health ? -0.24f : 0f, 0);
            hb.Quad(hb.timerRoot.transform, new Vector3(width + 0.1f, 0.16f, 1f), Vector3.zero, Frame);
            hb.timerFill = hb.Quad(hb.timerRoot.transform, new Vector3(width, 0.08f, 1f), new Vector3(0, 0, -0.04f), Color.white, out hb.timerR);
            hb.timerRoot.SetActive(false);
            return hb;
        }

        Transform Quad(Transform parent, Vector3 scale, Vector3 pos, Color c) => Quad(parent, scale, pos, c, out _);

        Transform Quad(Transform parent, Vector3 scale, Vector3 pos, Color c, out MeshRenderer r)
        {
            var q = new GameObject("q");
            q.transform.SetParent(parent, false);
            q.transform.localPosition = pos;
            q.transform.localScale = scale;
            q.AddComponent<MeshFilter>().sharedMesh = FX.Quad;
            r = q.AddComponent<MeshRenderer>();
            r.sharedMaterial = FX.Mat("ui_bar_world");
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            Paint(r, c);
            return q.transform;
        }

        void Paint(MeshRenderer r, Color c)
        {
            block.SetColor("_BaseColor", c);
            r.SetPropertyBlock(block);
        }

        static void Stretch(Transform t, float width, float frac, float z)
        {
            var s = t.localScale;
            t.localScale = new Vector3(Mathf.Max(0.0001f, width * frac), s.y, 1f);
            t.localPosition = new Vector3(-width * (1f - frac) * 0.5f, 0f, z);
        }

        public void Set(float frac)
        {
            frac = Mathf.Clamp01(frac);
            if (!hasValue)
            {
                hasValue = true;
                shown = trailFrac = frac;
            }
            if (frac < shown - 0.001f)
            {
                trailHold = 0.4f;
                Tween.Punch(bars, 0.18f, 0.25f);
            }
            else if (frac > shown + 0.01f) Flash(GameData.Hex("C8FFB0"));
            shown = frac;
        }

        /// <summary>Timer strip: fraction of the countdown already elapsed, or negative to hide it.</summary>
        public void SetTimer(float frac, Color c)
        {
            bool show = frac >= 0f;
            if (timerRoot.activeSelf != show)
            {
                timerRoot.SetActive(show);
                if (show) timerRoot.transform.Pop(0.3f, 0.3f);
            }
            if (!show) return;
            Stretch(timerFill, width, Mathf.Clamp01(frac), -0.04f);
            Paint(timerR, c);
        }

        public void Flash(Color c)
        {
            flash = 1f;
            flashColor = c;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            trailHold -= dt;
            if (trailHold <= 0f) trailFrac = Mathf.MoveTowards(trailFrac, shown, dt * 0.9f);
            if (trailFrac < shown) trailFrac = shown;
            Stretch(fill, width, shown, -0.04f);
            Stretch(trail, width, trailFrac, -0.02f);

            Color c = shown > 0.5f ? Color.Lerp(Half, Full, (shown - 0.5f) * 2f) : Color.Lerp(Low, Half, shown * 2f);
            flash = Mathf.MoveTowards(flash, 0f, dt * 4f);
            Paint(fillR, Color.Lerp(c, flashColor, flash));

            if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;
        }
    }
}
