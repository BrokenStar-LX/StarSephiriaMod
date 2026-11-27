using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace SPMod
{
    [HarmonyPatch]
    internal static class BossRushFirstFloorStorage
    {
        private const string Reward = "INVENTORY_SLOT/10";
        private static readonly ConditionalWeakTable<PlayerSpawner, SaveData> Pending = new();
        private static readonly ConditionalWeakTable<PlayerSpawner, SaveData> Initialized = new();
        private static SaveData? ResumingRun;

        internal static void Grant(PlayerSpawner Spawner)
        {
            var Run = SaveManager.CurrentRun;
            if (Run == null) return;
            var Player = Spawner.PlayerAvatar;
            if (Player == null || Player.Inventory == null)
            {
                Defer(Spawner, Run);
                return;
            }
            if (!Player.isServer || !Player.Inventory.isServer) return;
            if (Player.orphanedStatusInstancesServerside.Any(Status =>
                Status is StatusInstance_InventorySlot && Status.ID == "INVENTORY_SLOT" && Status.Value == 10))
                return;

            if (ReferenceEquals(ResumingRun, Run) &&
                !(Initialized.TryGetValue(Spawner, out var InitializedRun) && ReferenceEquals(InitializedRun, Run)))
            {
                int Version = Run.GetInt("SaveVersion", 0);
                int Index = Spawner.currentPlayerIdxForSave;
                if (Version != 0 && Index < 0)
                {
                    Defer(Spawner, Run);
                    return;
                }

                // Version zero has a single unindexed save record, which belongs to the host.
                if ((Version != 0 || Player.isLocalPlayer) && HasSavedReward(Run, Version, Index)) return;
            }

            var GrantStatus = StatusDatabase.CreateStatusEntity(Reward);
            if (GrantStatus != null) Player.AddOrphanedStatusInstance(GrantStatus);
        }

        private static void Defer(PlayerSpawner Spawner, SaveData Run)
        {
            Pending.Remove(Spawner);
            Pending.Add(Spawner, Run);
        }

        private static bool HasSavedReward(SaveData Run, int Version, int Index)
        {
            string Prefix = Version == 0 ? "" : "Player" + Index.ToString(CultureInfo.InvariantCulture);
            int Count = Run.GetInt(Prefix + "AddedStatsInSessionCount", 0);
            for (int Slot = 0; Slot < Count; Slot++)
                if (Run.GetString(Prefix + "AddedStatsInSession" + Slot.ToString(CultureInfo.InvariantCulture), "") == Reward)
                    return true;
            return false;
        }

        [HarmonyPatch(typeof(SaveManager), "LoadTMP", new[] { typeof(string) })]
        [HarmonyPostfix]
        private static void MarkResume(bool __result) => ResumingRun = __result ? SaveManager.CurrentRun : null;

        [HarmonyPatch(typeof(PlayerSpawner), "Initialize",
            new[] { typeof(int), typeof(string), typeof(string), typeof(int), typeof(ERestartPlacement) })]
        [HarmonyPostfix]
        private static void GrantAfterPlayerInitialization(PlayerSpawner __instance, bool __result)
        {
            var CurrentRun = SaveManager.CurrentRun;
            if (!__result || CurrentRun == null) return;
            Initialized.Remove(__instance);
            Initialized.Add(__instance, CurrentRun);
            if (!Pending.TryGetValue(__instance, out var Run)) return;
            Pending.Remove(__instance);
            if (ReferenceEquals(Run, CurrentRun)) Grant(__instance);
        }
    }
}
