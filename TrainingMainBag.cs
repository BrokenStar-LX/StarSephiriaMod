using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        private const int Limit = 30;
        private const string SlotId = "INVENTORY_SLOT";
        private const string SaveKey = "[Star's Sephiria Mod]TrainingMainBag";
        private static readonly object Ownership = new();
        private static readonly ConditionalWeakTable<StatusInstance, object> ClaimedSlots = new();
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

        private static string[] OccupiedTail(GridInventory Inventory, int Capacity) =>
            Inventory.inventoryMatrix.Where(Row => Row.Value != null && Row.Key.x >= 0 && Row.Key.y is >= 0 and < 100 &&
                Inventory.PosToIdx(Row.Key) >= Capacity)
                .OrderBy(Row => Inventory.PosToIdx(Row.Key))
                .Select(Row => Text("Position", Row.Key.x + 1, Row.Key.y + 1)).ToArray();

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
                if (Inventory.CurrentInventoryStorage != Before) Slot.RemoveStatus();
                ClaimedSlots.Remove(Slot);
                Player.orphanedStatusInstancesServerside.Remove(Slot);
                Slot.ClearTarget();
                Message("Unavailable");
                return;
            }
            Message("Claimed", Inventory.CurrentInventoryStorage, Limit);
        }

        private static bool Reset(PlayerAvatar Player, bool Notify)
        {
            var Slots = Owned(Player);
            if (Slots.Length == 0) return true;
            var Inventory = Player.Inventory;
            int Capacity = Inventory.CurrentInventoryStorage - Slots.Length;
            if (!HasNativeGrid(Inventory) || Capacity <= 0)
            {
                Message("Unavailable");
                return false;
            }
            var Occupied = OccupiedTail(Inventory, Capacity);
            if (Occupied.Length != 0)
            {
                Message("Occupied", string.Join(", ", Occupied));
                return false;
            }
            foreach (var Slot in Slots) Revoke(Player, Slot);
            if (Notify) Message("ResetDone", Inventory.CurrentInventoryStorage);
            return true;
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
            if (!ModConfig.Instance.EnableTrainingMainBag)
                return Reset(Player, Notify: false);
            var Menu = UIManager.Instance?.GetElement<UI_MessageBox_List>();
            if (Menu == null) return Reset(Player, Notify: false);
            var Inventory = Player.Inventory;
            var Options = new List<(string Label, Action Apply)>();
            if (HasNativeGrid(Inventory) && Inventory.CurrentInventoryStorage < Limit)
                Options.Add((Text("Claim"), () => Claim(Player)));
            if (HasNativeGrid(Inventory) && Owned(Player).Length != 0)
                Options.Add((Text("Reset"), () => Reset(Player, Notify: true)));
            if (Options.Count == 0) return Reset(Player, Notify: false);
            Options.Add((Text("Leave"), () =>
            {
                if (!Reset(Player, Notify: false)) return;
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
                    !ModConfig.Instance.EnableTrainingMainBag ||
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

        private static int SaveVersion(SaveData Run) => Run.GetInt("SaveVersion", 0);

        [HarmonyPatch(typeof(PlayerSpawner), nameof(PlayerSpawner.SaveCurrentSessionData), new Type[] { })]
        [HarmonyPostfix]
        private static void SaveOwnership(PlayerSpawner __instance, bool __runOriginal)
        {
            var Player = __instance.PlayerAvatar;
            var Run = SaveManager.CurrentRun;
            int Index = __instance.currentPlayerIdxForSave;
            if (!__runOriginal || Player == null || !Player.isServer || !Player.isLocalPlayer || Run == null) return;
            int Version = SaveVersion(Run);
            if (Version != 0 && Index < 0) return;
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

        [HarmonyPatch(typeof(PlayerSpawner), "Initialize",
            new[] { typeof(int), typeof(string), typeof(string), typeof(int), typeof(ERestartPlacement) })]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void LoadOwnership(PlayerSpawner __instance, bool __result)
        {
            var Run = SaveManager.CurrentRun;
            var Player = __instance.PlayerAvatar;
            if (!__result || Player == null || !Player.isServer || Player.Inventory == null || Run == null) return;
            int Version = SaveVersion(Run);
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
