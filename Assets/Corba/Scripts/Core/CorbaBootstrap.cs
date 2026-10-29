using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Corba
{
    public enum GameMode { Online = 0, VsAi = 1, AiVsAi = 2, Solo = 3 }

    public class CorbaBootstrap : MonoBehaviour
    {
        public const ushort Port = 7777;
        public static CorbaBootstrap Instance { get; private set; }
        public static GameMode PendingMode { get; private set; }

        NetworkManager network;
        UnityTransport transport;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<CorbaBootstrap>() == null) new GameObject("Corba").AddComponent<CorbaBootstrap>();
        }

        void Awake()
        {
            Instance = this;
            Application.runInBackground = true;
            Application.targetFrameRate = 120;

            SetupCamera();
            SetupLighting();
            new GameObject("Map").AddComponent<CorbaMap>();
            SetupNetwork();
            SetupEventSystem();
            new GameObject("UI").AddComponent<CorbaUI>();

            CorbaMap.Instance.Build(Random.Range(1, int.MaxValue));
        }

        void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = GameData.Hex("BFD8E0");
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 260f;
            cam.transform.position = new Vector3(0, 52, -58);
            cam.transform.rotation = Quaternion.LookRotation(new Vector3(0, -56, 58));
            if (cam.GetComponent<CameraRig>() == null) cam.gameObject.AddComponent<CameraRig>();
            if (cam.GetComponent<Atmosphere>() == null) Atmosphere.Install(cam);
        }

        void SetupLighting()
        {
            Light sun = null;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sun = l; break; }
            if (sun == null) sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = GameData.Hex("FFE9C7");
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.55f;
            sun.transform.rotation = Quaternion.Euler(52f, -38f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = GameData.Hex("B9D3E8");
            RenderSettings.ambientEquatorColor = GameData.Hex("C9C3A0");
            RenderSettings.ambientGroundColor = GameData.Hex("6E6250");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = GameData.Hex("C9DDE0");
            RenderSettings.fogStartDistance = 60f;
            RenderSettings.fogEndDistance = 170f;
        }

        void SetupNetwork()
        {
            var go = new GameObject("NetworkManager");
            DontDestroyOnLoad(go);
            transport = go.AddComponent<UnityTransport>();
            network = go.AddComponent<NetworkManager>();
            network.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                ConnectionApproval = true,
                TickRate = 30,
                EnableSceneManagement = false,
            };
            foreach (var prefab in Resources.LoadAll<GameObject>("Corba/Net"))
                network.AddNetworkPrefab(prefab);

            network.ConnectionApprovalCallback = (request, response) =>
            {
                response.Approved = network.ConnectedClientsIds.Count < 2;
                response.CreatePlayerObject = false;
                if (!response.Approved) response.Reason = "The kitchen is full.";
            };
            network.OnClientStopped += _ => CorbaUI.Instance?.ShowMenu("Disconnected.");
        }

        void SetupEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        public bool Host(GameMode mode = GameMode.Online)
        {
            PendingMode = mode;
            transport.SetConnectionData(mode == GameMode.Online ? "0.0.0.0" : "127.0.0.1", Port, mode == GameMode.Online ? "0.0.0.0" : "127.0.0.1");
            if (!network.StartHost()) return false;
            var director = Instantiate(Resources.Load<GameObject>("Corba/Net/Director"));
            director.GetComponent<NetworkObject>().Spawn();
            return true;
        }

        public bool Join(string address)
        {
            transport.SetConnectionData(string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address.Trim(), Port);
            return network.StartClient();
        }

        public void Leave()
        {
            if (network.IsListening) network.Shutdown();
            CameraRig.Instance?.Follow(null);
        }

        public static bool Online => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        public static bool IsHost => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
    }
}
