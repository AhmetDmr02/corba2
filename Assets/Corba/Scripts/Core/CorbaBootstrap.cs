using System;
using System.Threading.Tasks;
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

        /// <summary>The Relay room code while hosting or joined through Unity services; null for direct IP and offline modes.</summary>
        public static string RoomCode { get; private set; }
        public static bool Busy { get; private set; }

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

            CorbaMap.Instance.Build(UnityEngine.Random.Range(1, int.MaxValue));
            // Sign in early in the background so "Host Online" and "Join" answer faster; failures are reported when used.
            _ = CorbaRelay.EnsureSignedIn().ContinueWith(t => { if (t.IsFaulted) Debug.LogWarning("[Corba] Unity Services sign-in failed: " + t.Exception?.GetBaseException().Message); });
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
            cam.backgroundColor = GameData.Hex("C7D6D9");
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
                if (!response.Approved) response.Reason = Loc.T("net.full");
            };
            network.OnClientStopped += _ =>
            {
                RoomCode = null;
                CorbaUI.Instance?.ShowMenu(Loc.T("menu.disconnected"));
            };
        }

        void SetupEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>Offline modes (vs AI, practice, AI vs AI) and direct-IP LAN hosting.</summary>
        public bool Host(GameMode mode = GameMode.Online)
        {
            PendingMode = mode;
            RoomCode = null;
            transport.SetConnectionData(mode == GameMode.Online ? "0.0.0.0" : "127.0.0.1", Port, mode == GameMode.Online ? "0.0.0.0" : "127.0.0.1");
            return StartHostAndDirector();
        }

        bool StartHostAndDirector()
        {
            if (!network.StartHost()) return false;
            var director = Instantiate(Resources.Load<GameObject>("Corba/Net/Director"));
            director.GetComponent<NetworkObject>().Spawn();
            return true;
        }

        /// <summary>Direct-IP join, kept for LAN play.</summary>
        public bool Join(string address)
        {
            RoomCode = null;
            transport.SetConnectionData(string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address.Trim(), Port);
            return network.StartClient();
        }

        /// <summary>Opens an online room through Unity Relay. Returns null on success, else a message for the menu.</summary>
        public async Task<string> HostRoom()
        {
            if (Busy || network.IsListening) return null;
            Busy = true;
            try
            {
                var room = await CorbaRelay.CreateRoom();
                if (this == null) return null;
                PendingMode = GameMode.Online;
                transport.SetRelayServerData(room.Data);
                if (!StartHostAndDirector()) return Loc.T("net.relayFailed");
                RoomCode = room.Code;
                return null;
            }
            catch (Exception e)
            {
                return CorbaRelay.Describe(e, false);
            }
            finally
            {
                Busy = false;
            }
        }

        /// <summary>Joins an online room by its code. Returns null on success, else a message for the menu.</summary>
        public async Task<string> JoinRoom(string code)
        {
            if (Busy || network.IsListening) return null;
            code = CorbaRelay.NormaliseCode(code);
            if (code.Length == 0) return Loc.T("net.enterCode");
            Busy = true;
            try
            {
                var room = await CorbaRelay.JoinRoom(code);
                if (this == null) return null;
                transport.SetRelayServerData(room.Data);
                if (!network.StartClient()) return Loc.T("menu.errClient");
                RoomCode = room.Code;
                return null;
            }
            catch (Exception e)
            {
                return CorbaRelay.Describe(e, true);
            }
            finally
            {
                Busy = false;
            }
        }

        public void Leave()
        {
            if (network.IsListening) network.Shutdown();
            RoomCode = null;
            CameraRig.Instance?.Follow(null);
        }

        public static bool Online => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        public static bool IsHost => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
    }
}
