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

        internal static void Grant(PlayerSpawner Spawner)
        {
            var Run = SaveManager.CurrentRun;
            var Player = Spawner.PlayerAvatar;
            if (Run == null || Player == null || !Player.isServer || Player.Inventory == null || !Player.Inventory.isServer)
                return;
            if (Player.orphanedStatusInstancesServerside.Any(Status =>
                Status is StatusInstance_InventorySlot && Status.ID == "INVENTORY_SLOT" && Status.Value == 10))
                return;

            int Version = Run.GetInt("SaveVersion", 0);
            int Index = Spawner.currentPlayerIdxForSave;
            if (Version != 0 && Index < 0)
            {
                Pending.Remove(Spawner);
                Pending.Add(Spawner, Run);
                return;
            }

            // Before Initialize replays a continued player's statuses, check that player's saved reward.
            // The only other SPMod +10 grant is reincarnation, which cannot enter the first-floor grant path.
            if ((Version != 0 || Player.isLocalPlayer) && HasSavedReward(Run, Version, Index)) return;
            var GrantStatus = StatusDatabase.CreateStatusEntity(Reward);
            if (GrantStatus != null) Player.AddOrphanedStatusInstance(GrantStatus);
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

        [HarmonyPatch(typeof(PlayerSpawner), "Initialize")]
        [HarmonyPostfix]
        private static void GrantAfterPlayerInitialization(PlayerSpawner __instance, bool __result)
        {
            if (!__result || !Pending.TryGetValue(__instance, out var Run)) return;
            Pending.Remove(__instance);
            if (ReferenceEquals(Run, SaveManager.CurrentRun)) Grant(__instance);
        }
    }
}
