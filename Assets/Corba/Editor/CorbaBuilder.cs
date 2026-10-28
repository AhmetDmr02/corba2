using System.IO;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Corba.EditorTools
{
    [InitializeOnLoad]
    public static class CorbaBuilder
    {
        const string ResourcesRoot = "Assets/Corba/Resources/Corba";
        const string NetRoot = ResourcesRoot + "/Net";
        const string ScenePath = "Assets/Corba/Scenes/Corba.unity";

        static CorbaBuilder()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(NetRoot + "/Director.prefab")) BuildAll();
            };
        }

        [MenuItem("Corba/Rebuild Prefabs and Scene")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(NetRoot);
            Directory.CreateDirectory("Assets/Corba/Scenes");
            Directory.CreateDirectory(ResourcesRoot + "/Models");
            Directory.CreateDirectory(ResourcesRoot + "/Textures");
            Directory.CreateDirectory(ResourcesRoot + "/UI");

            CreateMaterial(ResourcesRoot + "/HandPaintedBase.mat", "Corba/HandPainted");
            CreateMaterial(ResourcesRoot + "/PaintedFxBase.mat", "Corba/PaintedFx");

            SavePrefab("Director", go => go.AddComponent<MatchDirector>());
            SavePrefab("Unit", go => go.AddComponent<UnitAgent>());
            SavePrefab("Build", go => go.AddComponent<DefenceBuild>());
            SavePrefab("Chef", go =>
            {
                var cc = go.AddComponent<CharacterController>();
                cc.height = 1.8f;
                cc.radius = 0.42f;
                cc.center = new Vector3(0f, 0.92f, 0f);
                cc.stepOffset = 0.45f;
                cc.slopeLimit = 55f;
                var nt = go.AddComponent<ClientNetworkTransform>();
                nt.SyncRotAngleX = false;
                nt.SyncRotAngleZ = false;
                nt.SyncScaleX = false;
                nt.SyncScaleY = false;
                nt.SyncScaleZ = false;
                go.AddComponent<ChefController>();
            });

            BuildScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Corba] Prefabs, materials and scene built.");
        }

        static void CreateMaterial(string path, string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("[Corba] Shader not found: " + shaderName);
                return;
            }
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = shader;
                EditorUtility.SetDirty(existing);
                return;
            }
            AssetDatabase.CreateAsset(new Material(shader), path);
        }

        static void SavePrefab(string name, System.Action<GameObject> configure)
        {
            var go = new GameObject(name);
            go.AddComponent<NetworkObject>();
            configure(go);
            PrefabUtility.SaveAsPrefabAsset(go, $"{NetRoot}/{name}.prefab");
            Object.DestroyImmediate(go);
        }

        static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera") { tag = "MainCamera" };
            cam.AddComponent<Camera>();
            cam.AddComponent<AudioListener>();
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            new GameObject("Corba").AddComponent<CorbaBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorBuildSettings.scenes = scenes;
        }
    }
}
