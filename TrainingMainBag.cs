using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Mirror;
using SPMod.Tool;
using UnityEngine;

namespace SPMod
{
    [HarmonyPatch]
    internal static class TrainingMainBag
    {
        private const int Limit = GridInventory.MaxWidth * GridInventory.MaxHeight;
        private const string SlotId = "INVENTORY_SLOT";
        private const string SaveKey = "[Star's Sephiria Mod]TrainingMainBag";
        private static readonly object Ownership = new();
        private static readonly ConditionalWeakTable<StatusInstance, object> ClaimedSlots = new();
        private static readonly PropertyInfo? StatusTarget = AccessTools.Property(typeof(StatusInstance), "CurrentTarget");
        private static bool Leaving;

        private static bool CanUse(PlayerAvatar? Player)
        {
            if (!NetworkServer.active || Player == null || !Player.isServer || !Player.isLocalPlayer ||
                Player.Inventory == null || !Player.Inventory.isServer || Player.Inventory.UnitAvatar != Player ||
                GameCamera.Instance == null || GameCamera.Instance.Observer != Player)
                return false;
            var Floor = FloorGenerator.FindByGuid(Player.currentFloorGuid);
            return Floor != null && Floor.isTrainingFloor && Floor.aName.key == "Floor_TrainingSchool";
        }

        private static bool HasNativeGrid(GridInventory Inventory) =>
            Inventory.Width == GridInventory.MaxWidth && Inventory.CurrentInventoryStorage > 0;

        private static StatusInstance[] Owned(PlayerAvatar Player) =>
            Player.orphanedStatusInstancesServerside.Where(Status => ClaimedSlots.TryGetValue(Status, out _)).ToArray();

        private static bool TailIsEmpty(GridInventory Inventory, int Capacity) =>
            !Inventory.inventoryMatrix.Keys.Any(Position => Position.x >= 0 && Position.y is >= 0 and < 100 &&
                Inventory.PosToIdx(Position) >= Capacity);

        private static string Text(string Key, params object[] Args) =>
            string.Format(Tools.GetLocalization("TrainingMainBag_" + Key), Args);

        private static void Claim(PlayerAvatar Player)
        {
            var Inventory = Player.Inventory;
            int Before = Inventory.CurrentInventoryStorage;
            if (!HasNativeGrid(Inventory) || Before >= Limit) return;
            if (StatusDatabase.CreateStatusEntity(SlotId, 1) is not StatusInstance_InventorySlot Slot)
            {
                Message("Unavailable");
                return;
            }
            ClaimedSlots.Add(Slot, Ownership);
            Player.AddOrphanedStatusInstance(Slot);
            if (Inventory.CurrentInventoryStorage != Before + 1)
            {
                ClaimedSlots.Remove(Slot);
                Player.orphanedStatusInstancesServerside.Remove(Slot);
                Slot.ClearTarget();
                Message("Unavailable");
                return;
            }
            Message("Claimed", Inventory.CurrentInventoryStorage, Limit);
        }

        private static void Reset(PlayerAvatar Player)
        {
            var Slots = Owned(Player);
            if (Slots.Length == 0) return;
            var Inventory = Player.Inventory;
            int Capacity = Inventory.CurrentInventoryStorage - Slots.Length;
            if (!HasNativeGrid(Inventory) || Capacity <= 0 || !TailIsEmpty(Inventory, Capacity))
            {
                Message("Occupied");
                return;
            }
            foreach (var Slot in Slots) Revoke(Player, Slot);
            Message("ResetDone", Inventory.CurrentInventoryStorage);
        }

        private static void Revoke(PlayerAvatar Player, StatusInstance Slot)
        {
            Slot.RemoveStatus();
            Slot.ClearTarget();
            Player.orphanedStatusInstancesServerside.Remove(Slot);
            ClaimedSlots.Remove(Slot);
        }

        private static void Message(string Key, params object[] Args) =>
            UIManager.Instance?.GetElement<UI_SystemMessage>()?.Open(Text(Key, Args), 4f, false);

        [HarmonyPatch(typeof(TrainingSchoolLeaveDoor), nameof(TrainingSchoolLeaveDoor.MoveTo), new[] { typeof(GameObject) })]
        [HarmonyPrefix]
        private static bool OpenMenu(TrainingSchoolLeaveDoor __instance, GameObject __0)
        {
            if (Leaving || __0 == null || !__0.TryGetComponent<PlayerAvatar>(out var Player) || !CanUse(Player))
                return true;
            var Menu = UIManager.Instance?.GetElement<UI_MessageBox_List>();
            if (Menu == null) return true;
            var Inventory = Player.Inventory;
            var Options = new List<(string Label, Action Apply)>();
            if (HasNativeGrid(Inventory) && Inventory.CurrentInventoryStorage < Limit)
                Options.Add((Text("Claim"), () => Claim(Player)));
            if (HasNativeGrid(Inventory) && Owned(Player).Length != 0)
                Options.Add((Text("Reset"), () => Reset(Player)));
            if (Options.Count == 0) return true;
            Options.Add((Text("Leave"), () =>
            {
                Leaving = true;
                try { __instance.MoveTo(__0); }
                finally { Leaving = false; }
            }));
            Options.Add((Text("Cancel"), () => { }));

            string Floor = Player.currentFloorGuid;
            bool Used = false;
            Menu.Open(Text("Menu", Inventory.CurrentInventoryStorage, Limit), Choice =>
            {
                if (Used || __instance == null || __0 == null || !CanUse(Player) ||
                    Player.currentFloorGuid != Floor || Choice < 0 || Choice >= Options.Count) return;
                Used = true;
                Options[Choice].Apply();
            }, Options.Select(Option => Option.Label).ToArray());
            return false;
        }

        private static string SlotPrefix(int Version, int Index) =>
            Version == 0 ? "" : "Player" + Index.ToString(CultureInfo.InvariantCulture);

        private static StatusInstance[] NativeSlots(PlayerAvatar Player) => Player.orphanedStatusInstancesServerside
            .Where(Status => Status is StatusInstance_InventorySlot && Status.ID == SlotId).ToArray();

        [HarmonyPatch(typeof(StatusInstance_InventorySlot), "ApplyStatusInner")]
        [HarmonyPrefix]
        private static void BeforeNativeUpgrade(StatusInstance_InventorySlot __instance,
            out (PlayerAvatar Player, int Before)? __state)
        {
            __state = null;
            if (ClaimedSlots.TryGetValue(__instance, out _) || __instance.Value <= 0 ||
                StatusTarget?.GetValue(__instance) is not PlayerAvatar Player || Player == null || !Player.isServer ||
                Player.Inventory == null || !HasNativeGrid(Player.Inventory)) return;
            __state = (Player, Player.Inventory.CurrentInventoryStorage);
        }

        [HarmonyPatch(typeof(StatusInstance_InventorySlot), "ApplyStatusInner")]
        [HarmonyPostfix]
        private static void AfterNativeUpgrade((PlayerAvatar Player, int Before)? __state)
        {
            if (__state is not { } State) return;
            var Inventory = State.Player.Inventory;
            int Capacity = Inventory.CurrentInventoryStorage;
            if (Capacity <= Limit || Capacity <= State.Before) return;

            // A normal upgrade earns its full capacity. It can replace overlapping practice cells.
            var Overlap = Owned(State.Player).Take(Math.Min(Capacity - Limit, Capacity - State.Before)).ToArray();
            if (Overlap.Length == 0 || !TailIsEmpty(Inventory, Capacity - Overlap.Length)) return;
            foreach (var Slot in Overlap) Revoke(State.Player, Slot);
        }

        [HarmonyPatch(typeof(PlayerSpawner), nameof(PlayerSpawner.SaveCurrentSessionData))]
        [HarmonyPostfix]
        private static void SaveOwnership(PlayerSpawner __instance, bool __runOriginal)
        {
            var Player = __instance.PlayerAvatar;
            var Run = SaveManager.CurrentRun;
            int Index = __instance.currentPlayerIdxForSave;
            int Version = SaveManager.SaveVersion;
            if (!__runOriginal || Player == null || !Player.isServer || !Player.isLocalPlayer || Run == null ||
                Version != 0 && Index < 0) return;
            string Prefix = SlotPrefix(Version, Index);
            string Key = SaveKey + Prefix;
            var Slots = NativeSlots(Player);
            var Ordinals = Enumerable.Range(0, Slots.Length)
                .Where(Ordinal => ClaimedSlots.TryGetValue(Slots[Ordinal], out _))
                .Select(Ordinal => Ordinal.ToString(CultureInfo.InvariantCulture)).ToArray();
            if (Ordinals.Length != 0 && MatchesNativeSave(Run, Prefix, Slots))
                Run.SetString(Key, string.Join(",", Ordinals));
            else
                Run.RemoveKey(Key);
        }

        [HarmonyPatch(typeof(PlayerSpawner), "Initialize")]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void LoadOwnership(PlayerSpawner __instance, bool __result)
        {
            var Run = SaveManager.CurrentRun;
            var Player = __instance.PlayerAvatar;
            if (!__result || Player == null || !Player.isServer || Player.Inventory == null || Run == null) return;
            int Version = Run.GetInt("SaveVersion", 0);
            int Index = __instance.currentPlayerIdxForSave;
            if (Version != 0 && Index < 0) return;
            string Prefix = SlotPrefix(Version, Index);
            string Stored = Run.GetString(SaveKey + Prefix, "");
            if (Stored.Length == 0) return;
            var Slots = NativeSlots(Player);
            if (!MatchesNativeSave(Run, Prefix, Slots)) return;
            var Ordinals = Stored.Split(',').Select(Value =>
                int.TryParse(Value, NumberStyles.None, CultureInfo.InvariantCulture, out int Ordinal) ? Ordinal : -1)
                .ToArray();
            if (Ordinals.Distinct().Count() != Ordinals.Length ||
                Ordinals.Length >= Player.Inventory.CurrentInventoryStorage ||
                Ordinals.Any(Ordinal => Ordinal < 0 || Ordinal >= Slots.Length || Slots[Ordinal].Value != 1)) return;
            foreach (int Ordinal in Ordinals) ClaimedSlots.GetValue(Slots[Ordinal], _ => Ownership);
        }

        private static bool MatchesNativeSave(SaveData Run, string Prefix, StatusInstance[] Slots)
        {
            int Count = Run.GetInt(Prefix + "AddedStatsInSessionCount", 0);
            if (Count < Slots.Length) return false;
            var Native = Enumerable.Range(0, Count).Select(Index =>
                    Run.GetString(Prefix + "AddedStatsInSession" + Index.ToString(CultureInfo.InvariantCulture), ""))
                .Where(Metadata => Metadata.StartsWith(SlotId + "/", StringComparison.Ordinal));
            return Native.SequenceEqual(Slots.Select(Slot => Slot.ToMetadata()));
        }
    }
}
