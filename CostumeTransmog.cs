using HarmonyLib;
using SPMod.Tool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.InputSystem;

namespace SPMod
{
    /// <summary>
    /// 服装幻化：只替换渲染用的 CostumeSkinEntity.skinPrefab，不改 currentCostume / currentCostumeSkin。
    /// 因此不会触发 UpdateCostumeData，服装属性、初始物品、专属效果都保持真实装备的那一套。
    /// 交互：服装面板里 Shift+点击一套服装 = 幻化成它的默认皮肤；皮肤图标上 Shift+点击 = 幻化成该皮肤。
    /// 再次 Shift+点击同一个目标 = 取消。
    /// </summary>
    [HarmonyPatch]
    public static class CostumeTransmog_Patch
    {
        public static readonly Dictionary<string, string> TransmogSkin = new Dictionary<string, string>();

        private static readonly FieldInfo costumePanelPlayerField = AccessTools.Field(typeof(UI_CostumePanel), "playerAvatar");
        private static readonly MethodInfo updateCostumeOutfitMethod = AccessTools.Method(typeof(PlayerAvatar), "UpdateCostumeOutfit");

        private static string SaveKey(string PlayerName) => "[Star's SephiriaMod]TransmogSkin_" + PlayerName;

        private static void LoadSaved(string PlayerName)
        {
            if (string.IsNullOrEmpty(PlayerName) || SaveManager.Current == null) return;
            string Saved = SaveManager.Current.GetString(SaveKey(PlayerName), "");
            if (string.IsNullOrEmpty(Saved)) TransmogSkin.SafeRemove(PlayerName);
            else TransmogSkin[PlayerName] = Saved;
        }

        public static bool IsTransmogCostume(string PlayerName, CostumeEntity Costume)
        {
            if (Costume == null || string.IsNullOrEmpty(PlayerName)) return false;
            if (!TransmogSkin.TryGetValue(PlayerName, out string SkinID)) return false;
            CostumeSkinEntity Skin = CostumeDatabase.GetCostumeSkinByID(SkinID);
            return Skin != null && Skin.relatedCostumeID == Costume.id;
        }

        public static void SetTransmog(PlayerAvatar Player, string SkinID)
        {
            if (Player == null || string.IsNullOrEmpty(Player.Name)) return;
            if (string.IsNullOrEmpty(SkinID)) TransmogSkin.SafeRemove(Player.Name);
            else TransmogSkin[Player.Name] = SkinID;
            SaveManager.Current?.SetString(SaveKey(Player.Name), SkinID ?? "");
            RefreshOutfit(Player);
        }

        public static void ClearTransmog(PlayerAvatar Player) => SetTransmog(Player, "");

        private static void RefreshOutfit(PlayerAvatar Player)
        {
            if (Player == null || updateCostumeOutfitMethod == null) return;
            if (!Player.TopdownActor || !Player.TopdownActor.body) return;
            try
            {
                updateCostumeOutfitMethod.Invoke(Player, new object[] { Player.currentCostumeSkin });
            }
            catch (Exception ex)
            {
                StarSephiriaMod.LogError($"[Transmog]刷新外观失败: {ex.Message}");
            }
        }

        private static string? GetLocalPlayerName()
        {
            if (PlayerSpawner.MultiplayerList == null) return null;
            PlayerSpawner? Fallback = null;
            foreach (PlayerSpawner Spawner in PlayerSpawner.MultiplayerList)
            {
                if (Spawner == null || Spawner.PlayerAvatar == null) continue;
                if (Spawner.isOwned) return Spawner.PlayerAvatar.Name;
                Fallback ??= Spawner;
            }
            return Fallback?.PlayerAvatar?.Name;
        }

        private static void ShowMessage(string Text)
        {
            if (string.IsNullOrEmpty(Text)) return;
            UI_SystemMessage? Message = UIManager.Instance?.GetElement<UI_SystemMessage>();
            Message?.Open(Text, 3f);
        }

        [HarmonyPatch(typeof(PlayerAvatar), "UpdateCostumeOutfit")]
        [HarmonyPrefix]
        static void UpdateCostumeOutfit_Prefix(PlayerAvatar __instance, ref string skinID)
        {
            if (!ModConfig.Instance.EnableCostumeTransmog) return;
            // 每次刷新都以存档为准，避免切换档案后残留上一个档案的幻化
            LoadSaved(__instance.Name);
            if (TransmogSkin.TryGetValue(__instance.Name, out string Target)
                && !string.IsNullOrEmpty(Target)
                && CostumeDatabase.GetCostumeSkinByID(Target) != null)
            {
                skinID = Target;
            }
        }

        [HarmonyPatch(typeof(UI_CostumePanel), "HandleClick")]
        [HarmonyPrefix]
        static bool HandleClick_Prefix(UI_CostumePanel __instance, UI_CostumeListElement e)
        {
            if (!ModConfig.Instance.EnableCostumeTransmog) return true;
            if (Keyboard.current == null || !Keyboard.current.shiftKey.isPressed) return true;
            if (e == null || e.Costume == null) return true;

            CostumeEntity Costume = e.Costume;
            bool Unlocked = Costume.costumeType == ECostumeUnlockType.Default
                || Costume.costumeType == ECostumeUnlockType.Dungreed
                || CostumeDatabase.IsUnlocked(Costume.id)
                || (ScreenFader.Instance != null && ScreenFader.Instance.IsTestMode);
            if (!Unlocked)
            {
                ShowMessage(Tools.GetLocalization("Instruction_TransmogLocked"));
                return false;
            }

            PlayerAvatar? Player = costumePanelPlayerField != null ? (PlayerAvatar?)costumePanelPlayerField.GetValue(__instance) : null;
            if (Player == null) return true;

            if (IsTransmogCostume(Player.Name, Costume))
            {
                ClearTransmog(Player);
                ShowMessage(Tools.GetLocalization("Instruction_TransmogClear"));
                return false;
            }

            CostumeSkinEntity? Skin = CostumeDatabase.GetAllRelatedSkins(Costume.id)?
                .FirstOrDefault(x => x.unlockType == CostumeSkinEntity.ECostumeUnlockType.Default);
            if (Skin == null) return true;

            SetTransmog(Player, Skin.skinID);
            ShowMessage(string.Format(Tools.GetLocalization("Instruction_TransmogSet"), Costume.aName.ToString()));
            return false;
        }

        [HarmonyPatch(typeof(UI_CostumePanel), "EquipSkin")]
        [HarmonyPrefix]
        static bool EquipSkin_Prefix(UI_CostumePanel __instance, UI_CostumeSkinIcon icon)
        {
            if (!ModConfig.Instance.EnableCostumeTransmog) return true;
            if (Keyboard.current == null || !Keyboard.current.shiftKey.isPressed) return true;
            if (icon == null || icon.skin == null) return true;

            bool Purchased = icon.skin.unlockType == CostumeSkinEntity.ECostumeUnlockType.Default
                || (SaveManager.Current != null && SaveManager.Current.GetBool("SkinPurchased_" + icon.skin.skinID, false))
                || (ScreenFader.Instance != null && ScreenFader.Instance.IsTestMode);
            if (!Purchased)
            {
                ShowMessage(Tools.GetLocalization("Instruction_TransmogLocked"));
                return false;
            }

            PlayerAvatar? Player = costumePanelPlayerField != null ? (PlayerAvatar?)costumePanelPlayerField.GetValue(__instance) : null;
            if (Player == null) return true;

            if (TransmogSkin.TryGetValue(Player.Name, out string CurrentSkin) && CurrentSkin == icon.skin.skinID)
            {
                ClearTransmog(Player);
                ShowMessage(Tools.GetLocalization("Instruction_TransmogClear"));
                return false;
            }

            SetTransmog(Player, icon.skin.skinID);
            ShowMessage(string.Format(Tools.GetLocalization("Instruction_TransmogSet"), icon.skin.aName.ToString()));
            return false;
        }

        [HarmonyPatch(typeof(UI_CostumeListElement), "Initialize")]
        [HarmonyPostfix]
        static void Initialize_Postfix(UI_CostumeListElement __instance)
        {
            if (!ModConfig.Instance.EnableCostumeTransmog) return;
            CostumeEntity Costume = __instance.Costume;
            string? PlayerName = GetLocalPlayerName();
            if (Costume == null || string.IsNullOrEmpty(PlayerName)) return;
            LoadSaved(PlayerName);
            if (!IsTransmogCostume(PlayerName, Costume)) return;
            __instance.nameText.text += Tools.GetLocalization("Instruction_TransmogMark");
        }
    }
}
