using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Corba
{
    /// <summary>Post-processing grade, painted sky and drifting air motes. Built in code so the scene stays minimal.</summary>
    public class Atmosphere : MonoBehaviour
    {
        Camera cam;
        float moteTimer;

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
            color.postExposure.Override(0.15f);
            color.contrast.Override(14f);
            color.saturation.Override(18f);
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
            RenderSettings.fogColor = new Color(0.8f, 0.86f, 0.84f);
        }

        void Update()
        {
            moteTimer -= Time.deltaTime;
            if (moteTimer > 0f) return;
            moteTimer = 0.12f;
            var focus = cam.transform.position + cam.transform.forward * 18f;
            var p = focus + new Vector3(Random.Range(-16f, 16f), Random.Range(-4f, 6f), Random.Range(-10f, 10f));
            FX.Mote(p);
        }
    }
}
