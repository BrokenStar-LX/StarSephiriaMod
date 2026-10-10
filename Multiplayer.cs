using HarmonyLib;
using Mirror;
using Mirror.FizzySteam;

namespace SPMod
{
    [HarmonyPatch]
    public static class Multiplayer
    {
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