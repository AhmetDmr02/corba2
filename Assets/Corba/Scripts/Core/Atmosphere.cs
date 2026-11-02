using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Corba
{
    /// <summary>Post-processing grade, painted sky, distance haze and drifting air motes. Built in code so the scene stays minimal.</summary>
    public class Atmosphere : MonoBehaviour
    {
        Camera cam;
        float moteTimer, mistTimer;
        float focusDistance = 40f;

        public static void Install(Camera cam)
        {
            var a = cam.gameObject.AddComponent<Atmosphere>();
            a.cam = cam;
            a.SetupCamera();
            a.SetupVolume();
            a.SetupSky();
        }

        void SetupCamera()
        {
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            // A single NaN pixel gets smeared across the whole frame by bloom; filter them out.
            data.stopNaN = true;
            data.dithering = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.Medium;
        }

        void SetupVolume()
        {
            var go = new GameObject("PostProcess");
            go.transform.SetParent(transform, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);

            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.12f);
            color.contrast.Override(24f);
            color.saturation.Override(12f);
            color.colorFilter.Override(new Color(1f, 0.97f, 0.92f));

            var white = profile.Add<WhiteBalance>(true);
            white.temperature.Override(9f);
            white.tint.Override(3f);

            var split = profile.Add<SplitToning>(true);
            split.shadows.Override(new Color(0.42f, 0.4f, 0.62f));
            split.highlights.Override(new Color(1f, 0.86f, 0.62f));
            split.balance.Override(-15f);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.55f);
            bloom.scatter.Override(0.72f);
            bloom.tint.Override(new Color(1f, 0.9f, 0.75f));

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.3f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(new Color(0.22f, 0.12f, 0.08f));

            var aberration = profile.Add<ChromaticAberration>(true);
            aberration.intensity.Override(0.08f);

            var grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(0.14f);
            grain.response.Override(0.8f);
        }

        void SetupSky()
        {
            var shader = Shader.Find("Skybox/Procedural");
            if (shader == null) return;
            var sky = new Material(shader);
            sky.SetFloat("_SunSize", 0.035f);
            sky.SetFloat("_SunSizeConvergence", 6f);
            sky.SetFloat("_AtmosphereThickness", 0.7f);
            sky.SetColor("_SkyTint", new Color(0.62f, 0.78f, 0.92f));
            sky.SetColor("_GroundColor", new Color(0.55f, 0.6f, 0.45f));
            sky.SetFloat("_Exposure", 1.15f);
            RenderSettings.skybox = sky;
            cam.clearFlags = CameraClearFlags.Skybox;
            RenderSettings.fogColor = FogColor;
        }

        static readonly Color FogColor = new Color(0.78f, 0.84f, 0.85f);

        void Update()
        {
            var t = cam.transform;
            float hit = t.forward.y < -0.05f ? t.position.y / -t.forward.y : 60f;
            focusDistance = Mathf.Lerp(focusDistance, Mathf.Clamp(hit, 8f, 120f), 1f - Mathf.Exp(-3f * Time.deltaTime));
            var focus = t.position + t.forward * focusDistance;

            // Haze starts just behind the focus point so the play area stays crisp and the far terraces melt away.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogStartDistance = focusDistance * 0.7f;
            RenderSettings.fogEndDistance = focusDistance * 3.2f;

            mistTimer -= Time.deltaTime;
            if (mistTimer <= 0f)
            {
                mistTimer = 0.18f;
                var off = Random.insideUnitCircle * focusDistance * 1.1f;
                FX.Mist(new Vector3(focus.x + off.x, Random.Range(0.3f, 1.6f), focus.z + off.y));
            }

            moteTimer -= Time.deltaTime;
            if (moteTimer > 0f) return;
            moteTimer = 0.12f;
            var p = t.position + t.forward * 18f + new Vector3(Random.Range(-16f, 16f), Random.Range(-4f, 6f), Random.Range(-10f, 10f));
            FX.Mote(p);
        }
    }
}
