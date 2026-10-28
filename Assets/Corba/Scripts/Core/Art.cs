using System.Collections.Generic;
using UnityEngine;

namespace Corba
{
    public static class Art
    {
        const string ModelPath = "Corba/Models/";
        const string TexturePath = "Corba/Textures/";
        const string UiPath = "Corba/UI/";

        static Material baseMaterial;
        static readonly Dictionary<string, Material> modelMaterials = new Dictionary<string, Material>();
        static readonly Dictionary<Color, Material> flatMaterials = new Dictionary<Color, Material>();
        static readonly Dictionary<Color, Material> soupMaterials = new Dictionary<Color, Material>();
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static MaterialPropertyBlock block;

        static Material Base
        {
            get
            {
                if (baseMaterial == null)
                {
                    baseMaterial = Resources.Load<Material>("Corba/HandPaintedBase");
                    if (baseMaterial == null)
                    {
                        var shader = Shader.Find("Corba/HandPainted") ?? Shader.Find("Universal Render Pipeline/Unlit");
                        baseMaterial = new Material(shader);
                    }
                }
                return baseMaterial;
            }
        }

        public static Material ForModel(string model)
        {
            if (modelMaterials.TryGetValue(model, out var m)) return m;
            m = new Material(Base) { name = "HP_" + model };
            var tex = Resources.Load<Texture2D>(TexturePath + model);
            if (tex != null) m.SetTexture("_BaseMap", tex);
            modelMaterials[model] = m;
            return m;
        }

        public static Material GroundMaterial(string texture, float vertexColorStrength)
        {
            string key = "ground:" + texture;
            if (modelMaterials.TryGetValue(key, out var m)) return m;
            m = new Material(Base) { name = key };
            var tex = Resources.Load<Texture2D>(TexturePath + texture);
            if (tex != null)
            {
                tex.wrapMode = TextureWrapMode.Repeat;
                m.SetTexture("_BaseMap", tex);
            }
            m.SetFloat("_VertexColorStrength", texture == "ground_grass" ? vertexColorStrength : 0f);
            m.SetFloat("_RimStrength", 0f);
            m.SetFloat("_LightInfluence", 0.45f);
            if (tex == null && texture == "ground_road") m.SetColor("_BaseColor", GameData.Hex("C9A46E"));
            modelMaterials[key] = m;
            return m;
        }

        public static Material Flat(Color c)
        {
            if (flatMaterials.TryGetValue(c, out var m)) return m;
            m = new Material(Base) { name = "Flat" };
            m.SetColor("_BaseColor", c);
            flatMaterials[c] = m;
            return m;
        }

        public static Material Soup(Color c)
        {
            if (soupMaterials.TryGetValue(c, out var m)) return m;
            m = new Material(Base) { name = "Soup" };
            var tex = Resources.Load<Texture2D>(TexturePath + "soup_surface");
            if (tex != null) m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Emission", 0.35f);
            m.SetFloat("_RimStrength", 0f);
            soupMaterials[c] = m;
            return m;
        }

        public static GameObject Spawn(string model, Transform parent, Color fallback)
        {
            var prefab = Resources.Load<GameObject>(ModelPath + model);
            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab, parent);
                go.name = model;
                var mat = ForModel(model);
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                    r.sharedMaterials = mats;
                }
                foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            }
            else
            {
                go = GameObject.CreatePrimitive(model.StartsWith("u_") || model.StartsWith("chef") ? PrimitiveType.Capsule : PrimitiveType.Cube);
                go.name = model + "_placeholder";
                Object.Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(parent, false);
                go.transform.localScale = Vector3.one * 0.7f;
                go.GetComponent<MeshRenderer>().sharedMaterial = Flat(fallback);
            }
            return go;
        }

        public static void Tint(GameObject go, Color c, float strength = 0.65f)
        {
            block ??= new MaterialPropertyBlock();
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                r.GetPropertyBlock(block);
                block.SetColor("_Tint", new Color(c.r, c.g, c.b, strength));
                r.SetPropertyBlock(block);
            }
        }

        public static void SetFloat(Renderer[] renderers, string prop, float value)
        {
            block ??= new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetFloat(prop, value);
                r.SetPropertyBlock(block);
            }
        }

        public static Sprite Sprite(string name)
        {
            if (sprites.TryGetValue(name, out var s)) return s;
            s = Resources.Load<Sprite>(UiPath + name);
            if (s == null)
            {
                var tex = Resources.Load<Texture2D>(UiPath + name);
                if (tex != null) s = UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            sprites[name] = s;
            return s;
        }
    }
}
