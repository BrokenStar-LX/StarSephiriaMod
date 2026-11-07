using HarmonyLib;
using HeathenEngineering.SteamworksIntegration;
using Mirror;
using Mirror.FizzySteam;
using SPMod.Tool;
using System.Reflection;
using UnityEngine;

namespace SPMod
{
    [HarmonyPatch]
    public static class Multiplayer
    {
        public const int MaxConnections = 50;

        /// <summary>
        /// 工坊 DLL 在 MainWorld 才加载，主机在标题界面 StartHost 时 FizzySteamworks 已经把
        /// NetworkManager.maxConnections 拷贝进 NextServer/LegacyServer，之后改 Mirror 的上限没用。
        /// 进图后把 Mirror、Steam 传输、Steam 大厅三处的人数上限一起抬到 MaxConnections。
        /// </summary>
        public static void ApplyLiveConnectionLimits()
        {
            if (!NetworkServer.active) return;

            NetworkServer.maxConnections = MaxConnections;
            if (NetworkManager.singleton != null)
                NetworkManager.singleton.maxConnections = MaxConnections;

            FieldInfo? ServerField = AccessTools.Field(typeof(FizzySteamworks), "server");
            object? Server = ServerField?.GetValue(null);
            if (Server != null)
            {
                FieldInfo? MaxConnectionsField = AccessTools.Field(Server.GetType(), "maxConnections");
                MaxConnectionsField?.SetValue(Server, MaxConnections);
            }

            LobbyManager? Manager = UnityEngine.Object.FindFirstObjectByType<LobbyManager>();
            if (Manager != null && Manager.HasLobby && Manager.IsPlayerOwner)
            {
                LobbyData CurrentLobby = Manager.Lobby;
                CurrentLobby.MaxMembers = MaxConnections;
            }
        }

        [HarmonyPatch(typeof(FizzySteamworks))]
        public static class FizzySteamworks_Patch
        {
            [HarmonyPatch("ServerStart")]
            [HarmonyPrefix]
            static void ServerStart_Prefix(FizzySteamworks __instance)
            {
                if (NetworkManager.singleton != null)
                {
                    NetworkServer.maxConnections = 50;
                    NetworkManager.singleton.maxConnections = 50;
                }
            }
        }

        [HarmonyPatch(typeof(NetworkManager))]
        public static class NetworkManager_Patch
        {
            [HarmonyPatch("StartServer")]
            [HarmonyPrefix]
            static void StartServer_Prefix(NetworkManager __instance)
            {
                NetworkServer.maxConnections = 50;
                NetworkManager.singleton.maxConnections = 50;
                __instance.maxConnections = 50;
            }

            [HarmonyPatch("StartHost")]
            [HarmonyPrefix]
            static void StartHost_Prefix(NetworkManager __instance)
            {
                NetworkServer.maxConnections = 50;
                NetworkManager.singleton.maxConnections = 50;
                __instance.maxConnections = 50;
            }
        }

        [HarmonyPatch(typeof(NetworkServer))]
        public static class NetworkServer_Patch
        {
            [HarmonyPatch("IsConnectionAllowed")]
            [HarmonyPrefix]
            static void IsConnectionAllowed_Prefix(int connectionId, string address)
            {
                NetworkServer.maxConnections = 50;
            }
        }

        [HarmonyPatch(typeof(NetworkManager))]
        public static class NetworkManager_ModNetwork_Patch
        {
            [HarmonyPatch("RegisterClientMessages")]
            [HarmonyPostfix]
            static void RegisterClientMessages_Postfix()
            {
                ModNetwork.RegisterClientHandlers();
            }
        }

        [HarmonyPatch(typeof(UI_HorizontalSelectionBox_MultiplayerNumber))]
        public static class UI_HorizontalSelectionBox_MultiplayerNumber_Patch
        {
            [HarmonyPatch("OnEnable")]
            [HarmonyPostfix]
            static void OnEnable_Postfix(UI_HorizontalSelectionBox_MultiplayerNumber __instance)
            {
                int MaxStack = 50;
                if (__instance.box.numberOfElements < MaxStack - 1)
                    __instance.box.numberOfElements = MaxStack - 1;
            }
        }
    }
}