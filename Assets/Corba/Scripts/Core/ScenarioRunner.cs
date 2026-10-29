#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;

namespace Corba
{
    // Editor-only automation used by the CorbaBridge to capture play-mode screenshots.
    public class ScenarioRunner : MonoBehaviour
    {
        public static string Scenario { get; private set; } = "";
        static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Bridge"));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            Scenario = PlayerPrefs.GetString("Corba.Scenario", "");
            PlayerPrefs.DeleteKey("Corba.Scenario");
            if (string.IsNullOrEmpty(Scenario)) return;
            new GameObject("ScenarioRunner").AddComponent<ScenarioRunner>();
        }

        IEnumerator OpenBookLater()
        {
            yield return new WaitForSecondsRealtime(4f);
            SoupBook.Instance?.Toggle();
            yield return new WaitForSecondsRealtime(3f);
            SoupBook.Instance?.Toggle();
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(Path.Combine(Dir, "shots"));
            var parts = Scenario.Split(':');
            string mode = parts[0];
            float duration = parts.Length > 1 && float.TryParse(parts[1], out var d) ? d : 30f;
            float interval = parts.Length > 2 && float.TryParse(parts[2], out var i) ? i : 5f;
            Time.timeScale = parts.Length > 3 && float.TryParse(parts[3], out var ts) ? ts : 1f;

            yield return new WaitForSecondsRealtime(1.5f);
            if (mode == "export")
            {
                MapExporter.Export((int)duration, Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", "CorbaSim", "maps.json")));
                File.WriteAllText(Path.Combine(Dir, "done.txt"), "done");
                yield break;
            }
            switch (mode)
            {
                case "solo":
                    CorbaUI.Instance.SendMessage("OnSolo");
                    break;
                case "ai":
                    CorbaUI.Instance.SendMessage("OnVsAi");
                    break;
                case "aivai":
                    CorbaUI.Instance.SendMessage("OnAiVsAi");
                    break;
                case "book":
                    PlayerPrefs.SetInt("Corba.TutorialDone", 1);
                    CorbaUI.Instance.SendMessage("OnVsAi");
                    StartCoroutine(OpenBookLater());
                    break;
                case "tutorial":
                    PlayerPrefs.DeleteKey("Corba.TutorialDone");
                    CorbaUI.Instance.SendMessage("OnVsAi");
                    break;
            }

            float t = 0f;
            int n = 0;
            while (t <= duration)
            {
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(Dir, "shots", $"{mode}_{n:00}.png"));
                n++;
                yield return new WaitForSecondsRealtime(interval);
                t += interval;
            }
            yield return new WaitForSecondsRealtime(1f);
            File.WriteAllText(Path.Combine(Dir, "done.txt"), "done");
        }
    }
}
#endif
