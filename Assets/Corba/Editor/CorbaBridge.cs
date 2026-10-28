using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Corba.EditorTools
{
    // Lets external tooling drive the editor while it is unfocused: drop a command into Bridge/cmd.txt.
    // Commands: refresh | play <scenario> | stop | build
    [InitializeOnLoad]
    public static class CorbaBridge
    {
        static readonly string Dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Bridge"));
        static string CmdPath => Path.Combine(Dir, "cmd.txt");
        static string LogPath => Path.Combine(Dir, "log.txt");
        static double nextPoll;

        static CorbaBridge()
        {
            Directory.CreateDirectory(Dir);
            EditorApplication.update += Poll;
            Application.logMessageReceived += OnLog;
            CompilationPipeline.assemblyCompilationFinished += (asm, messages) =>
            {
                foreach (var m in messages)
                    if (m.type == CompilerMessageType.Error) Append("COMPILE " + m.message);
            };
            CompilationPipeline.compilationFinished += _ => Append("COMPILATION FINISHED");
            EditorApplication.playModeStateChanged += s => Append("PLAYMODE " + s);
        }

        static void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                Append($"{type} {msg}\n{FirstLines(stack, 4)}");
            else if (msg.StartsWith("[Corba]"))
                Append(msg);
        }

        static string FirstLines(string s, int n)
        {
            var lines = s.Split('\n');
            return string.Join("\n", lines, 0, Mathf.Min(n, lines.Length));
        }

        public static void Append(string line)
        {
            try { File.AppendAllText(LogPath, System.DateTime.Now.ToString("HH:mm:ss ") + line + "\n"); }
            catch (IOException) { }
        }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 0.5;

            var done = Path.Combine(Dir, "done.txt");
            if (File.Exists(done))
            {
                File.Delete(done);
                Append("SCENARIO DONE");
                EditorApplication.isPlaying = false;
            }

            string cmd;
            try
            {
                cmd = File.ReadAllText(CmdPath).Trim();
                File.Delete(CmdPath);
            }
            catch (IOException) { return; }
            catch (System.UnauthorizedAccessException) { return; }
            if (string.IsNullOrEmpty(cmd)) return;

            Append("CMD " + cmd);
            var parts = cmd.Split(' ');
            switch (parts[0])
            {
                case "refresh":
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    break;
                case "reimport":
                    AssetDatabase.ImportAsset(parts.Length > 1 ? parts[1] : "Assets/Corba/Resources/Corba/Models", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
                    Append("REIMPORTED");
                    break;
                case "probe":
                    Probe(parts.Length > 1 ? parts[1] : "chef_a");
                    break;
                case "build":
                    CorbaBuilder.BuildAll();
                    break;
                case "play":
                    if (EditorApplication.isPlaying) break;
                    SessionState.SetString("CorbaScenario", parts.Length > 1 ? parts[1] : "menu");
                    EditorSceneManager.OpenScene("Assets/Corba/Scenes/Corba.unity");
                    EditorApplication.isPlaying = true;
                    break;
                case "stop":
                    EditorApplication.isPlaying = false;
                    break;
            }
        }

        static void Probe(string model)
        {
            var prefab = Resources.Load<GameObject>("Corba/Models/" + model);
            if (prefab == null) { Append("PROBE missing " + model); return; }
            var go = Object.Instantiate(prefab);
            try
            {
                var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
                Append($"PROBE {model} root rot={go.transform.localRotation.eulerAngles} scale={go.transform.localScale} smr={(smr != null)} bones={(smr != null ? smr.bones.Length : 0)} rootBone={(smr != null && smr.rootBone != null ? smr.rootBone.name : "-")}");
                foreach (var t in go.GetComponentsInChildren<Transform>())
                    Append($"  {t.name} lp={t.localPosition} lr={t.localRotation.eulerAngles} ls={t.localScale}");
                if (smr != null) Append($"  rest bounds c={smr.bounds.center} s={smr.bounds.size}");
                var anim = go.GetComponent<Animation>();
                if (anim != null)
                {
                    foreach (AnimationState st in anim)
                    {
                        st.clip.SampleAnimation(go, st.length * 0.25f);
                        var mesh = new Mesh();
                        smr.BakeMesh(mesh);
                        var b = mesh.bounds;
                        Append($"  clip {st.name} len={st.length:0.00} baked c={smr.transform.TransformPoint(b.center)} s={b.size}");
                    }
                }
            }
            finally { Object.DestroyImmediate(go); }
        }

        [InitializeOnEnterPlayMode]
        static void PassScenario()
        {
            var scenario = SessionState.GetString("CorbaScenario", "");
            SessionState.EraseString("CorbaScenario");
            PlayerPrefs.SetString("Corba.Scenario", scenario);
        }
    }
}
