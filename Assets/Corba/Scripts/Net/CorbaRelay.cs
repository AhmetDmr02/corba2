using System;
using System.Threading.Tasks;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Corba
{
    /// <summary>
    /// Unity Gaming Services glue: signs in anonymously and creates / joins Relay rooms.
    /// A room is a Relay allocation; its join code is the room code players share.
    /// Needs the project linked to a Unity Cloud project (Edit > Project Settings > Services)
    /// and Relay enabled for it in the Unity Dashboard.
    /// </summary>
    public static class CorbaRelay
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        const string ConnectionType = "wss";
#else
        const string ConnectionType = "dtls";
#endif

        /// <summary>Corba is 1v1: the host plus one guest.</summary>
        const int MaxGuests = 1;

        static Task signInTask;

        public struct Room
        {
            public string Code;
            public RelayServerData Data;
        }

        /// <summary>Initialises Unity Services and signs in anonymously once per run.</summary>
        public static Task EnsureSignedIn()
        {
            if (signInTask == null || signInTask.IsFaulted || signInTask.IsCanceled) signInTask = SignIn();
            return signInTask;
        }

        static async Task SignIn()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                // A fresh profile per launch gives every running copy (two builds on one PC, Multiplayer
                // Play Mode clones) its own anonymous player. Corba keeps no progress on the account,
                // so nothing is lost by not reusing the cached one.
                var options = new InitializationOptions().SetProfile("cook" + Guid.NewGuid().ToString("N").Substring(0, 12));
                await UnityServices.InitializeAsync(options);
            }
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        /// <summary>Reserves a Relay server for the host and returns the room code to share.</summary>
        public static async Task<Room> CreateRoom()
        {
            await EnsureSignedIn();
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxGuests);
            string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return new Room { Code = code, Data = AllocationUtils.ToRelayServerData(allocation, ConnectionType) };
        }

        /// <summary>Looks up a room by its code and returns the Relay data the guest connects with.</summary>
        public static async Task<Room> JoinRoom(string code)
        {
            await EnsureSignedIn();
            code = NormaliseCode(code);
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(code);
            return new Room { Code = code, Data = AllocationUtils.ToRelayServerData(allocation, ConnectionType) };
        }

        public static string NormaliseCode(string code) => (code ?? "").Trim().Replace(" ", "").ToUpperInvariant();

        /// <summary>An IPv4 address or host:port goes the old direct way; anything else is treated as a room code.</summary>
        public static bool LooksLikeAddress(string text)
        {
            text = (text ?? "").Trim();
            return text.Length == 0 || text.Contains(".") || text.Contains(":") || text.Equals("localhost", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Turns a services failure into a short line for the menu; the full exception goes to the console.</summary>
        public static string Describe(Exception e, bool joining)
        {
            Debug.LogException(e);
            switch (e)
            {
                case RelayServiceException _:
                    return Loc.T(joining ? "net.roomNotFound" : "net.relayFailed");
                case AuthenticationException _:
                case ServicesInitializationException _:
                    return Loc.T("net.servicesFailed");
                case RequestFailedException _:
                    return Loc.T("net.offline");
                default:
                    return Loc.T("net.servicesFailed");
            }
        }
    }
}
