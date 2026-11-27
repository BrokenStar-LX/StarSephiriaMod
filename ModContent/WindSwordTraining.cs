using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Mirror;
using SPMod.Tool;

namespace SPMod.ModContent
{
    [HarmonyPatch]
    internal static class WindSwordTraining
    {
        private static readonly FieldInfo? ItemInstanceId = AccessTools.Field(typeof(Charm_Basic), "itemInstanceID");
        private static bool OpeningNativeClear;

        internal static bool CanPractice(PlayerAvatar? Player)
        {
            if (ItemInstanceId == null || !NetworkServer.active || Player == null || !Player.isServer || !Player.isLocalPlayer ||
                Player.Inventory == null || !Player.Inventory.isServer || Player.Inventory.UnitAvatar != Player ||
                GameCamera.Instance == null || GameCamera.Instance.Observer != Player) return false;
            var Floor = FloorGenerator.FindByGuid(Player.currentFloorGuid);
            return Floor != null && Floor.isTrainingFloor && Floor.aName.key == "Floor_TrainingSchool";
        }

        // Connect assigns the instance ID before the inventory-backed Item can be read.
        internal static bool IsPracticeItem(Charm_Basic Charm) =>
            ItemInstanceId?.GetValue(Charm) is int Id && GridInventory.IsDestructibleItem(Id);

        internal static void Message(string Key) =>
            UIManager.Instance?.GetElement<UI_SystemMessage>()?.Open(Tools.GetLocalization(Key), 4f, false);

        [HarmonyPatch(typeof(UI_ItemBoxPanel), "IsItemVisible")]
        [HarmonyPostfix]
        private static void ShowPracticeItems(UI_ItemBoxPanel __instance, PlayerAvatar ___avatar, ItemEntity __0,
            ref bool __result)
        {
            if (!__result && !__instance.IsEmeraldShop && CanPractice(___avatar) && __0 != null &&
                __0.name is "ModItemEntity_WindSword" or "ModItemEntity_RunesAccurate" or
                    "ModItemEntity_RunesSharp" or "ModItemEntity_RunesRapidly")
                __result = true;
        }

        [HarmonyPatch(typeof(UI_ItemBoxPanel), "OnClearInventoryButtonClicked")]
        [HarmonyPrefix]
        private static bool ChooseReset(UI_ItemBoxPanel __instance, PlayerAvatar ___avatar, MethodBase __originalMethod)
        {
            if (OpeningNativeClear || __instance.IsEmeraldShop || !CanPractice(___avatar)) return true;
            var Swords = ___avatar.Inventory.charms.Values.OfType<Charm_WindSword>()
                .Where(Sword => Sword != null && Sword.HasPracticeRunes).ToArray();
            var Menu = UIManager.Instance?.GetElement<UI_MessageBox_List>();
            if (Swords.Length == 0 || Menu == null) return true;

            string Floor = ___avatar.currentFloorGuid;
            bool Used = false;
            Menu.Open(Tools.GetLocalization("TrainingWindSword_Menu"), Choice =>
            {
                if (Used || __instance == null || !CanPractice(___avatar) || ___avatar.currentFloorGuid != Floor) return;
                Used = true;
                if (Choice == 0)
                {
                    foreach (var Sword in Swords)
                    {
                        if (Sword != null && Sword.NetworkAvatar == ___avatar && Sword.Item is { } Item &&
                            ___avatar.Inventory.FindItem(Item.XIdx, Item.YIdx)?.Charm == Sword)
                            Sword.ResetPracticeRunes();
                    }
                }
                else if (Choice == 1)
                {
                    OpeningNativeClear = true;
                    try { __originalMethod.Invoke(__instance, null); }
                    finally { OpeningNativeClear = false; }
                }
            }, new[] { Tools.GetLocalization("TrainingWindSword_Reset"), Tools.GetLocalization("TrainingWindSword_Clear"),
                Tools.GetLocalization("TrainingWindSword_Cancel") });
            return false;
        }

        [HarmonyPatch(typeof(PlayerSpawner), nameof(PlayerSpawner.SaveCurrentSessionData))]
        [HarmonyPostfix]
        private static void ExcludePracticeItems(PlayerSpawner __instance, bool __runOriginal)
        {
            PlayerAvatar Player = __instance.PlayerAvatar;
            SaveData Save = SaveManager.CurrentRun;
            if (!__runOriginal || !NetworkServer.active || Player == null || !Player.isLocalPlayer ||
                Player.Inventory == null || Save == null) return;
            int PlayerIndex = __instance.currentPlayerIdxForSave;
            if (SaveManager.SaveVersion != 0 && PlayerIndex < 0) return;
            string Prefix = SaveManager.SaveVersion == 0 ? "Player" :
                "Player" + PlayerIndex.ToString(CultureInfo.InvariantCulture);
            foreach (var Position in Player.Inventory.inventoryMatrix.Keys)
            {
                var Item = Player.Inventory.FindItem(Position.x, Position.y);
                if (Item == null || Item.Charm is not (Charm_WindSword or Charm_WindSwordRune) ||
                    !GridInventory.IsDestructibleItem(Item.InstanceID)) continue;
                string Key = FormattableString.Invariant($"{Prefix}Inventory{Position.x},{Position.y}");
                if (Save.GetInt(Key + "_Entity", -1) == Item.EntityID &&
                    Save.GetInt(Key + "_InstanceID", -1) == Item.InstanceID)
                    Save.SetInt(Key + "_Entity", -1);
            }
        }
    }
}
