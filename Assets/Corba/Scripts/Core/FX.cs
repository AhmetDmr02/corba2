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
            return p;
        }

        static void Emit(string tex, Vector3 pos, Vector3 vel, float gravity, float life, float size, float endSize, Color c, bool flat = false)
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
        }

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
            Emit("fx_ring", pos, Vector3.zero, 0f, 0.55f, 0.5f, radius * 2f, new Color(c.r, c.g, c.b, 0.9f), true);
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
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
            mesh.transform.localScale = Vector3.one * (lob ? 0.55f : 0.28f);
            mesh.GetComponent<MeshRenderer>().sharedMaterial = Art.Soup(c);
            fx.shots.Add(new Shot { From = from, To = to, Flight = Mathf.Max(flight, 0.05f), Lob = lob, Color = c, Ball = ball });
        }

        public static void FloatText(Vector3 pos, string text, Color c)
        {
            var fx = Instance;
            var go = new GameObject("float");
            go.transform.position = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = fx.font;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = 0.06f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = c;
            tm.fontStyle = FontStyle.Bold;
            go.GetComponent<MeshRenderer>().sharedMaterial = fx.font.material;
            go.AddComponent<FloatTextMover>();
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
                c.a *= p.Flat ? Mathf.Clamp01(p.Life / Mathf.Min(1.2f, p.MaxLife)) : 1f - k * k;
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
                        Splash(s.To, s.Color, s.Lob ? 10 : 4);
                        Destroy(s.Ball.gameObject);
                    }
                    shots.RemoveAt(i);
                    continue;
                }
                Vector3 p = Vector3.Lerp(s.From, s.To, s.T);
                if (s.Lob) p.y += Mathf.Sin(s.T * Mathf.PI) * 3.5f;
                s.Ball.position = p;
                if (Random.value < 0.5f) Emit("fx_blob", p, Vector3.zero, 2f, 0.25f, 0.18f, 0.02f, s.Color);
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

    public class HealthBar : MonoBehaviour
    {
        Transform fill;
        MeshRenderer fillRenderer;
        MaterialPropertyBlock block;
        float width;

        public static HealthBar Create(Transform parent, float height, float width)
        {
            var go = new GameObject("HealthBar");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.up * height;
            var hb = go.AddComponent<HealthBar>();
            hb.width = width;
            hb.block = new MaterialPropertyBlock();

            var bg = new GameObject("bg");
            bg.transform.SetParent(go.transform, false);
            bg.transform.localScale = new Vector3(width + 0.12f, 0.24f, 1f);
            bg.AddComponent<MeshFilter>().sharedMesh = FX.Quad;
            var bgr = bg.AddComponent<MeshRenderer>();
            bgr.sharedMaterial = FX.Mat("ui_bar_world");
            bgr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var b = new MaterialPropertyBlock();
            b.SetColor("_BaseColor", new Color(0.24f, 0.15f, 0.1f, 0.9f));
            bgr.SetPropertyBlock(b);

            var f = new GameObject("fill");
            f.transform.SetParent(go.transform, false);
            f.transform.localPosition = new Vector3(0, 0, -0.01f);
            f.AddComponent<MeshFilter>().sharedMesh = FX.Quad;
            hb.fillRenderer = f.AddComponent<MeshRenderer>();
            hb.fillRenderer.sharedMaterial = FX.Mat("ui_bar_world");
            hb.fillRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hb.fill = f.transform;
            hb.Set(1f, Color.green);
            return hb;
        }

        public void Set(float frac, Color c)
        {
            frac = Mathf.Clamp01(frac);
            fill.localScale = new Vector3(width * frac, 0.16f, 1f);
            fill.localPosition = new Vector3(-width * (1f - frac) * 0.5f, 0, -0.01f);
            block.SetColor("_BaseColor", Color.Lerp(GameData.Hex("D9442B"), c, frac));
            fillRenderer.SetPropertyBlock(block);
            gameObject.SetActive(frac < 0.995f);
        }

        void LateUpdate()
        {
            if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;
        }
    }
}
