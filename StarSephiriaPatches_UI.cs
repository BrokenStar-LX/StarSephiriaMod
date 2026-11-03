using FMOD.Studio;
using HarmonyLib;
using HeathenEngineering.SteamworksIntegration.API;
using Mirror;
using SPMod.ModContent;
using SPMod.MonsterAI;
using SPMod.Tool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static AvatarStatsHooker;
using static ComboEffect_Debuff;
using static SPMod.ModContent.LoadModContent;
using static SPMod.MonsterAI.ArmadilloGolem;
using static SPMod.Tool.ModuleComponents;
using static UnitAvatar;
using Object = UnityEngine.Object;
using Random = System.Random;


namespace SPMod
{
    public static partial class StarSephiriaPatches
    {
        [HarmonyPatch(typeof(UI_DimensionPocketPanel))]
        public static class UI_DimensionPocketPanel_Patch
        {
            [HarmonyPatch("Open")]
            [HarmonyPrefix]
            static void Open_Postfix(UI_DimensionPocketPanel __instance, PlayerAvatar playerAvatar, ref int cachedDimensionPocketStorage, List<ItemMetadata> cachedDimensionPocketItem)
            {
                List<ItemMetadata> cachedItems = cachedDimensionPocketItem;
                foreach (int itemId in BondingItems)
                {
                    var Item = ItemDatabase.FindItemById(itemId);
                    if (Item == null) continue;
                    if (Item.cannotBeReward) Item.cannotBeReward = false;
                    if (Item.activeType == EItemActiveType.Hidden) Item.activeType = EItemActiveType.Default;

                    bool exists = false;
                    foreach (ItemMetadata item in cachedItems)
                    {
                        if (item.entityID == itemId)
                        {
                            exists = true;
                            StarSephiriaMod.LogError($"许愿池中已有：{Item.Name}");
                            break;
                        }
                    }
                    if (!exists)
                    {
                        int Stack = 3;
                        if (Item.rarity == EItemRarity.Uncommon) Stack = 4;
                        else if (Item.rarity == EItemRarity.Rare) Stack = 5;
                        else if (Item.rarity == EItemRarity.Legend) Stack = 6;
                        if (cachedDimensionPocketStorage >= Stack)
                            cachedDimensionPocketItem.Add(new ItemMetadata(-1, itemId, 1));
                    }
                }
            }
        }

        [HarmonyPatch(typeof(UI_ShopPanel))]
        public static class UI_ShopPanel_Patch
        {
            private static readonly MethodInfo UpdateReplenishmentIconMethod = AccessTools.Method(typeof(UI_ShopPanel), "UpdateReplenishmentIcon");

            [HarmonyPatch("DoReplenishment")]
            [HarmonyPrefix]
            static bool DoReplenishment_Prefix(UI_ShopPanel __instance)
            {
                if (!ModConfig.Instance.ModifyMerchant || UpdateReplenishmentIconMethod == null)
                {
                    return true;
                }

                if (GameCamera.Instance.Observer != __instance.BuyerCharacter ||
                    UIManager.Instance.GetElement<UI_NewItemPicker>().CurrentAny ||
                    UIManager.Instance.GetElement<UI_NewItemPicker_Controller>().CurrentAny)
                {
                    return false;
                }

                UnitAI_NewBasic component = __instance.ShopCharacter.GetComponent<UnitAI_NewBasic>();
                if (!component)
                {
                    return false;
                }

                if (component.replenishments.Count > 0)
                {
                    bool flag = true;
                    foreach (UnitAI_NewBasic.ReplenishmentItem replenishment in component.replenishments)
                    {
                        if (!replenishment.purchased)
                        {
                            flag = false;
                            break;
                        }
                    }

                    if (flag)
                    {
                        UIManager.Instance.GetElement<UI_SystemMessage>().Open(__instance.allReplenishmentPurchasedText.ToString(), 3f);
                        __instance.notEnoughSapphireSoundEvent.Play();
                        return false;
                    }
                }

                int Use = 0;
                if (component.replenishmentTryCount >= 5) Use = int.MaxValue;
                if (GameCamera.Instance.Observer.TryGetComponent<PlayerLocalDataStorage>(out var component2) && GameCamera.Instance.Observer.TryGetComponent<PlayerSpawner>(out var _))
                {
                    if (component2.GetSapphire() >= Use)
                    {
                        __instance.replenishmentFlash.transform.SetAsLastSibling();
                        __instance.replenishmentFlash.gameObject.SetActive(value: true);
                        __instance.replenishmentFlash.ResetAnimation();
                        __instance.rerollSoundEvent.Play();
                        UIManager.Instance.GetElement<UI_FlashScreen>().Flash(new Color(0f, 0.765f, 1f, 0.12f), 0.1f, EFlashScreenTImeType.UNSCALED);
                        component.AddReplenishmentItemsClientside(2, 1);
                        UpdateReplenishmentIconMethod.Invoke(__instance, null);
                    }
                    else
                    {
                        UIManager.Instance.GetElement<UI_SystemMessage>().Open(__instance.notEnoughSapphireText.ToString(), 3f);
                        __instance.notEnoughSapphireSoundEvent.Play();
                    }
                }
                return false;
            }

            [HarmonyPatch("UpdateReplenishmentCost")]
            [HarmonyPrefix]
            static bool UpdateReplenishmentCost_Prefix(UI_ShopPanel __instance)
            {
                if (!ModConfig.Instance.ModifyMerchant)
                {
                    return true;
                }
                UnitAI_NewBasic component = __instance.ShopCharacter.GetComponent<UnitAI_NewBasic>();
                if (!component)
                {
                    return false;
                }
                string Use = (5 - component.replenishmentTryCount).ToString();
                __instance.replenishmentCostText.text = Use;
                return false;
            }
        }

        [HarmonyPatch(typeof(UI_StatsPanel))]
        public static class UI_StatsPanel_Patch
        {
            [HarmonyPatch("Connect")]
            [HarmonyPostfix]
            static void Connect_Postfix(UI_StatsPanel __instance, GameObject playerObject)
            {
                if (playerObject.TryGetComponent<AvatarStatsHooker>(out AvatarStatsHooker Hooker))
                {
                    UI_StatusTooltipOpener? Exists = __instance.statElements.FirstOrDefault(x => x.statHookID == TextFile.WindDamage);
                    if (Exists != null) { Exists.SetAvatar(Hooker); return; }
                    UI_StatusTooltipOpener? Template = __instance.statElements.FirstOrDefault(x => x.statHookID == "LIGHTNING_DAMAGE");
                    if (Template == null) return;

                    GameObject Clone = Object.Instantiate(Template.gameObject, Template.transform.parent);
                    Clone.name = "StatRow_WindDamage";
                    Clone.transform.SetSiblingIndex(Template.transform.GetSiblingIndex() + 1);

                    if (Clone.transform is RectTransform CloneRect)
                    {
                        List<RectTransform> Group = [CloneRect];
                        foreach (UI_StatusTooltipOpener Row in __instance.statElements)
                        {
                            if (Row.transform.parent != Template.transform.parent) continue;
                            if (!ElementHooks.Contains(Row.statHookID)) continue;
                            if (Row.transform is not RectTransform RowRect) continue;
                            if (Group.Contains(RowRect)) continue; // 面板里同一行可能在数组中出现两次
                            Group.Add(RowRect);
                        }
                        Group.Sort((A, B) => A.GetSiblingIndex().CompareTo(B.GetSiblingIndex()));
                        if (Group.Count > 1)
                        {
                            Vector2 First = Group[0].anchoredPosition;
                            Vector2 Last = Group[^1].anchoredPosition;
                            Vector2 Dir = (Last - First).normalized;
                            bool Horizontal = Mathf.Abs(Last.x - First.x) >= Mathf.Abs(Last.y - First.y);
                            float Extent = Group.Max(R => Horizontal ? R.rect.width : R.rect.height);

                            Vector2 Center = (First + Last) * 0.5f;
                            float HalfSpan = (Last - First).magnitude * 0.5f;
                            if (Template.transform.parent is RectTransform Container)
                            {
                                float ContainerSize = Horizontal ? Container.rect.width : Container.rect.height;
                                if (ContainerSize > 0f) HalfSpan = Mathf.Max((ContainerSize - Extent) * 0.5f, 0f);
                            }
                            float Step = HalfSpan * 2f / (Group.Count - 1);
                            for (int i = 0; i < Group.Count; i++)
                                Group[i].anchoredPosition = Center + Dir * (-HalfSpan + Step * i);
                        }
                    }

                    UI_StatusTooltipOpener Opener = Clone.GetComponent<UI_StatusTooltipOpener>();
                    Opener.statHookID = TextFile.WindDamage;
                    Opener.statKeyword = $"WindDamage";
                    KeywordEntity WindKeyword = KeywordDatabase.GetEntity($"WindDamage");
                    KeywordEntity? LightningKeyword = KeywordDatabase.GetEntity("LightningDamage");
                    Sprite? LightIcon = LightningKeyword?.keywordImage;
                    Image[] Icons = Opener.GetComponentsInChildren<Image>(true);
                    Image? Icon = Icons.FirstOrDefault(I => I.sprite != null && I.sprite == LightIcon) ?? Icons.FirstOrDefault(I => I.sprite != null);
                    if (Icon != null && WindKeyword != null && WindKeyword.keywordImage != null) Icon.sprite = WindKeyword.keywordImage;

                    Opener.applySignColor = false;
                    Opener.statusValueText.color = EffectConstant.WindColor;

                    __instance.statElements = __instance.statElements.Append(Opener).ToArray();
                    Opener.SetAvatar(Hooker);
                }
            }

            static readonly string[] ElementHooks = ["PHYSICAL_DAMAGE", "FIRE_DAMAGE", "ICE_DAMAGE", "LIGHTNING_DAMAGE"];
        }

        [HarmonyPatch(typeof(UI_StatusTooltipOpener))]
        public static class UI_StatusTooltipOpener_Patch
        {
            public static FieldInfo hookerField = AccessTools.Field(typeof(UI_StatusTooltipOpener), "hooker");

            [HarmonyPatch("GetKeywords")]
            [HarmonyPrefix]
            static bool GetKeywords(UI_StatusTooltipOpener __instance, ref Loc.KeywordValue[] __result)
            {
                if (hookerField != null)
                {
                    AvatarStatsHooker hooker = (AvatarStatsHooker)hookerField.GetValue(__instance);
                    if (hooker != null)
                    {
                        if (__instance.statHookID == TextFile.WindDamage)
                        {
                            __result =
                            [
                                new("BASE", hooker.HookStat(TextFile.WindDamage + "_BASE", out _, out _)),
                                new("AMP", hooker.HookStat(TextFile.WindDamage + "_AMP", out _, out _)),
                            ];
                            return false;
                        }
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UI_CostumePanel))]
        public static class UI_CostumePanel_Patch
        {
            private static readonly FieldInfo PlayerField = AccessTools.Field(typeof(UI_CostumePanel), "playerAvatar");
            private static readonly FieldInfo startingItemIconsField = AccessTools.Field(typeof(UI_CostumePanel), "startingItemIcons");

            [HarmonyPatch("UpdateData")]
            [HarmonyPostfix]
            static void UpdateData_Postfix(UI_CostumePanel __instance, CostumeEntity costume)
            {
                if (!ModConfig.Instance.EnableCostumeModifier) return;
                try
                {
                    if (PlayerField == null || startingItemIconsField == null)
                    {
                        StarSephiriaMod.LogError("[UI_CostumePanel][UpdateData]反射获取失败");
                        return;
                    }
                    PlayerAvatar Player = (PlayerAvatar)PlayerField.GetValue(__instance);
                    List<UI_ItemIcon> startingItemIcons = (List<UI_ItemIcon>)startingItemIconsField.GetValue(__instance);

                    bool Unlocked = costume && (costume.costumeType == ECostumeUnlockType.Default
                        || costume.costumeType == ECostumeUnlockType.Dungreed
                        || CostumeDatabase.IsUnlocked(costume.id)
                        || (ScreenFader.Instance && ScreenFader.Instance.IsTestMode));
                    StringBuilder stringBuilder = new();
                    if (Unlocked && costume != null)
                    {
                        string FlavorText = costume.aFlavorText.ToString();
                        bool UseModText = false;
                        TextFile.LoadText();
                        if (StarSephiriaMod.CostumeType.TryGetValue(Player.Name, out string useCostume) && useCostume == costume.id)
                        {
                            StarSephiriaMod.CostumeSkinType.TryGetValue(Player.Name, out string skin);
                            string Name = "";
                            if (!string.IsNullOrEmpty(skin) && StarSephiriaMod.CostumeSkinIdToName.TryGetValue(skin, out string FoundName))
                                Name = FoundName;
                            if (TextFile.CostumeTextMap.TryGetValue(Name, out string Effect))
                            {
                                FlavorText = Effect;
                                UseModText = true;
                            }
                        }
                        if (UseModText)
                        {
                            string[] stats = costume.stats;
                            ItemEntity[] item = costume.startingItems;
                            bool UseEffect = true;
                            StarSephiriaMod.ModifiedCostume(Player.Name, ref stats, ref item, ref UseEffect);
                            foreach (UI_ItemIcon ui_ItemIcon in startingItemIcons)
                            {
                                if (ui_ItemIcon)
                                {
                                    Object.Destroy(ui_ItemIcon.gameObject);
                                }
                            }
                            startingItemIcons.Clear();

                            bool flag2 = false;
                            foreach (ItemEntity itemEntity in item)
                            {
                                UI_ItemIcon ui_ItemIcon2 = Object.Instantiate<UI_ItemIcon>(__instance.startingItemIcon, __instance.startingItemContentsZone);
                                ui_ItemIcon2.SetItem(new ItemMetadata(-1, itemEntity.id, 1), null);
                                startingItemIcons.Add(ui_ItemIcon2);
                                flag2 = true;
                            }
                            __instance.startingItem.gameObject.SetActive(flag2);
                            __instance.tooltipEffectText.text = FlavorText;
                        }
                    }
                    else
                    {
                        __instance.startingItem.gameObject.SetActive(false);
                    }
                    if (Unlocked && costume != null && ModConfig.Instance.EnableCostumeTransmog)
                    {
                        string Hint = Tools.GetLocalization("Instruction_TransmogHint");
                        if (!string.IsNullOrEmpty(Hint))
                        {
                            __instance.tooltipEffectText.text = __instance.tooltipEffectText.text.TrimEnd()
                                + "\n- <indent=10>" + Hint + "</indent>";
                        }
                    }
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[UI_CostumePanel][UpdateData]错误{ex}");
                }
            }
        }

        [HarmonyPatch(typeof(UI_PresetPanel))]
        public static class UI_PresetPanel_Patch
        {
            private static readonly FieldInfo costumeStartingItemIconsField = AccessTools.Field(typeof(UI_PresetPanel), "costumeStartingItemIcons");

            [HarmonyPatch("UpdateCostumeInfo")]
            [HarmonyPostfix]
            static void UpdateCostumeInfo_Postfix(UI_PresetPanel __instance, string forwardKey)
            {
                if (!ModConfig.Instance.EnableCostumeModifier) return;

                if (costumeStartingItemIconsField == null)
                {
                    StarSephiriaMod.LogError("[UpdateCostumeInfo][UI_PresetPanel]反射获取失败");
                    return;
                }

                List<UI_ItemIcon> StartingItemIcons = (List<UI_ItemIcon>)costumeStartingItemIconsField.GetValue(__instance);

                string @string = SaveManager.Current.GetString(forwardKey + "PlayerCostume", "PinkRabbit");
                CostumeEntity CostumeEntity = CostumeDatabase.FindCostumeByID(@string);
                string string2 = SaveManager.Current.GetString(forwardKey + "PlayerCostume_CurrentSkin_" + @string, "");
                CostumeSkinEntity CostumeSkin = CostumeDatabase.GetCostumeSkinByID(string2);
                if (string.IsNullOrWhiteSpace(string2))
                {
                    CostumeSkinEntity FindCostume = CostumeDatabase.FindDefaultCostumeSkin(@string);
                    if (FindCostume) CostumeSkin = FindCostume;
                }

                bool Unlocked = CostumeEntity && (CostumeEntity.costumeType == ECostumeUnlockType.Default
                || CostumeEntity.costumeType == ECostumeUnlockType.Dungreed
                || CostumeDatabase.IsUnlocked(CostumeEntity.id)
                || (ScreenFader.Instance && ScreenFader.Instance.IsTestMode));
                StringBuilder stringBuilder = new();
                if (Unlocked && CostumeEntity != null)
                {
                    string FlavorText = CostumeEntity.aFlavorText.ToString();
                    bool UseModText = false;
                    TextFile.LoadText();
                    string Name = "";
                    if (CostumeSkin != null)
                    {
                        string SkinKey = CostumeSkin.skinID.ToSafeString();
                        if (!string.IsNullOrEmpty(SkinKey) && StarSephiriaMod.CostumeSkinIdToName.TryGetValue(SkinKey, out string FoundName))
                            Name = FoundName;
                    }
                    if (TextFile.CostumeTextMap.TryGetValue(Name, out string Effect))
                    {
                        FlavorText = Effect;
                        UseModText = true;
                    }
                    if (UseModText)
                    {
                        string[] stats = CostumeEntity.stats;
                        ItemEntity[] item = CostumeEntity.startingItems;
                        bool UseEffect = true;
                        StarSephiriaMod.LoadModCostumeEffect(Name, ref stats, ref item, ref UseEffect);
                        foreach (UI_ItemIcon ui_ItemIcon in StartingItemIcons)
                        {
                            if (ui_ItemIcon)
                            {
                                Object.Destroy(ui_ItemIcon.gameObject);
                            }
                        }
                        StartingItemIcons.Clear();

                        bool flag2 = false;
                        foreach (ItemEntity itemEntity in item)
                        {
                            UI_ItemIcon ui_ItemIcon2 = Object.Instantiate<UI_ItemIcon>(__instance.costumeStartingItemIcon, __instance.costumeStartingItemContentsZone);
                            ui_ItemIcon2.SetItem(new ItemMetadata(-1, itemEntity.id, 1), null);
                            StartingItemIcons.Add(ui_ItemIcon2);
                            flag2 = true;
                        }
                        __instance.costumeStartingItem.gameObject.SetActive(flag2);
                        __instance.costumeEffectText.text = FlavorText;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(UI_WeaponEnhancementButton))]
        public static class UI_WeaponEnhancementButton_Patch
        {
            [HarmonyPatch("SetWeaponMethod")]
            [HarmonyPostfix]
            static void SetWeaponMethod_Postfix(UI_WeaponEnhancementButton __instance, UI_WeaponEnhancementPanel panel, WeaponSimple currentWeapon, EnhancementMetadata enhancementMetadata)
            {
                try
                {
                    TextFile.LoadText();
                    if (TextFile.WeaponTextMap.TryGetValue(enhancementMetadata.enhanced.id, out var value) && ModConfig.Instance.EnableWeaponModifier && value.Item1 != "")
                    {
                        __instance.effectText.text = value.Item1;
                    }
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[UI_WeaponEnhancementButton][SetWeaponMethod]错误{ex}");
                }
            }
        }

        [HarmonyPatch(typeof(UI_WeaponTooltip))]
        public static class UI_WeaponTooltip_Patch
        {
            private static readonly FieldInfo openKeywordsCoroutineField = AccessTools.Field(typeof(UI_WeaponTooltip), "openKeywordsCoroutine");
            private static readonly MethodInfo openKeywordsCoroutineMethod = AccessTools.Method(typeof(UI_WeaponTooltip), "OpenKeywordsCoroutine");
            private static GameObject? HardLevel = null;
            private static TextMeshProUGUI? HardLevelText = null;

            [HarmonyPatch("Open")]
            [HarmonyPostfix]
            static void Open_Postfix(UI_WeaponTooltip __instance, IUITooltipOpener target, RectTransform attached, Vector2 offset, ITooltip data)
            {
                if (!ModConfig.Instance.EnableWeaponModifier) return;
                if (openKeywordsCoroutineField == null || openKeywordsCoroutineMethod == null)
                {
                    StarSephiriaMod.LogError("[UI_WeaponTooltip][Open]反射获取失败");
                    return;
                }
                Coroutine coroutine = (Coroutine)openKeywordsCoroutineField.GetValue(__instance);
                IEnumerator result = (IEnumerator)openKeywordsCoroutineMethod.Invoke(__instance, null);

                WeaponEntity? weaponEntity = null;
                WeaponAddon[]? array2 = null;
                WeaponSimple? weaponSimple = data as WeaponSimple;
                if (weaponSimple != null)
                {
                    weaponEntity = WeaponDatabase.FindWeaponById(weaponSimple.entityId);
                    array2 = weaponSimple.addons;
                }
                else
                {
                    WeaponEntity? weaponEntity2 = data as WeaponEntity;
                    if (weaponEntity2 != null)
                    {
                        weaponEntity = weaponEntity2;
                        if (weaponEntity.mainWeaponPrefab != null)
                        {
                            WeaponSimple component = weaponEntity.mainWeaponPrefab.GetComponent<WeaponSimple>();
                            if (component != null) array2 = component.addons;
                        }
                    }
                }
                if (weaponEntity != null && array2 != null)
                {
                    bool falg = false;
                    int Stack = 1;
                    string[] ModifyWeaponToopTip = ["", ""];
                    TextFile.LoadText();
                    WeaponEntity Orig = weaponEntity;
                    while (TextFile.WeaponTextMap.TryGetValue(weaponEntity.id, out var value))
                    {
                        falg = true;
                        ModifyWeaponToopTip[Stack] = value.Item1;
                        weaponEntity = WeaponDatabase.FindWeaponById(value.Item2);
                        Stack -= 1;
                        if (Stack < 0) break;
                        continue;
                    }
                    if (!TextFile.WeaponTextMap.TryGetValue(weaponEntity.id, out _) && array2.Length <= 1 && ModifyWeaponToopTip[0] == "" && ModifyWeaponToopTip[1] != "")
                    {
                        ModifyWeaponToopTip[0] = ModifyWeaponToopTip[1];
                    }
                    if (falg)
                    {
                        Stack = 0;
                        int NoTooptip = 0;
                        foreach (WeaponAddon weaponAddon in array2)
                        {
                            if (NoTooptip > 1)
                            {
                                continue;
                            }
                            if (ModifyWeaponToopTip[Stack] != "")
                            {
                                __instance.upgradeTextBoxes[Stack].SetText(ModifyWeaponToopTip[Stack]);
                                Stack = 1;
                                NoTooptip += 1;
                                continue;
                            }
                            __instance.upgradeTextBoxes[Stack].SetText(weaponAddon.GetEffectText());
                            Stack = 1;
                            NoTooptip += 1;
                        }
                    }
                    if (coroutine != null)
                    {
                        __instance.StopCoroutine(coroutine);
                        openKeywordsCoroutineField.SetValue(__instance, null);
                    }
                    if (!__instance.attachedWorldTarget)
                    {
                        openKeywordsCoroutineField.SetValue(__instance, __instance.StartCoroutine(result));
                    }

                    if (HardLevel == null)
                    {
                        Sprite? Icon = Tools.LoadModSprite("HardIcon");
                        if(Icon != null)
                        {
                            int Value = SaveManager.Current.GetInt("[Star's Sephiria Mod]HardModePoint_Weapon_" + Orig.id, 0);
                            if (Value > 0)
                            {
                                HardLevel = CreateIconTextGroup(__instance.weaponNameText, Value.ToString(), Icon);
                            }
                        }
                    }
                    else
                    {
                        int Value = SaveManager.Current.GetInt("[Star's Sephiria Mod]HardModePoint_Weapon_" + Orig.id, 0);
                        if (Value > 0 && HardLevelText != null)
                        {
                            HardLevelText.text = Value.ToString();
                            Color TextColor = Color.Lerp(Color.white, Color.gold, Mathf.Clamp(Value, 0, 99) * 0.01f);
                            if (Value >= 100) TextColor = Color.red;
                            HardLevelText.color = TextColor;
                            HardLevel.SetActive(true);
                        }
                        else HardLevel.SetActive(false);
                    }
                }
            }

            public static GameObject CreateIconTextGroup(TextMeshProUGUI originalText, string leftContent, Sprite iconSprite, float iconSize = 24, float gapIconText = 4, float gapTextOrigin = 5)
            {
                RectTransform originRt = originalText.rectTransform;
                GameObject container = new("IconTextGroup");
                container.transform.SetParent(originRt.parent, false);
                RectTransform containerRt = container.AddComponent<RectTransform>();
                containerRt.anchorMin = originRt.anchorMin;
                containerRt.anchorMax = originRt.anchorMax;
                containerRt.pivot = originRt.pivot;
                containerRt.anchoredPosition = originRt.anchoredPosition;

                HorizontalLayoutGroup hlg = container.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = gapIconText;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;

                GameObject iconObj = new("Icon");
                iconObj.transform.SetParent(container.transform, false);
                Image icon = iconObj.AddComponent<Image>();
                icon.sprite = iconSprite;
                icon.preserveAspect = true;
                RectTransform iconRt = icon.rectTransform;
                iconRt.sizeDelta = new Vector2(iconSize, iconSize);

                GameObject textObj = new("LeftText");
                textObj.transform.SetParent(container.transform, false);
                TextMeshProUGUI leftTMP = textObj.AddComponent<TextMeshProUGUI>();
                leftTMP.font = originalText.font;
                leftTMP.fontSize = originalText.fontSize;
                leftTMP.color = originalText.color;
                leftTMP.text = leftContent;
                leftTMP.alignment = TextAlignmentOptions.Midline;
                HardLevelText = leftTMP;

                originalText.ForceMeshUpdate();
                leftTMP.ForceMeshUpdate();
                containerRt.anchoredPosition = new Vector2(originRt.anchoredPosition.x - gapTextOrigin, originRt.anchoredPosition.y);
                return container;
            }
        }

        [HarmonyPatch(typeof(UI_PlayerHP))]
        public static class UI_PlayerHP_Patch
        {
            [HarmonyPatch("Connect")]
            [HarmonyPostfix]
            static void Connect_Postfix(UI_PlayerHP __instance, GameObject playerObject)
            {
                PlayerAvatar Player = playerObject.GetComponent<PlayerAvatar>();

                GameObject textObj = new("CustomText");
                GameObject textObj3 = new("CustomText3");
                textObj.transform.SetParent(__instance.transform);
                textObj3.transform.SetParent(__instance.transform);
                // 查找或创建新的文本组件
                TextMeshProUGUI? customText = textObj.AddComponent<TextMeshProUGUI>();
                TextMeshProUGUI? customText3 = textObj3.AddComponent<TextMeshProUGUI>();

                // 设置样式
                customText.font = customText3.font = __instance.pingText.font;
                customText.fontSize = 16;
                customText.color = Color.cyan;
                customText.alignment = TextAlignmentOptions.TopLeft;
                // 设置位置
                RectTransform rect = textObj.GetComponent<RectTransform>();
                RectTransform rect3 = textObj3.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(200, 50);
                rect.anchorMin = rect3.anchorMin = new Vector2(1, 1);
                rect.anchorMax = rect3.anchorMax = new Vector2(1, 1);
                rect.pivot = rect3.pivot = new Vector2(1, 1);

                rect3.sizeDelta = new Vector2(400, 100);
                customText3.color = Color.white;
                customText3.fontSize = 18;
                customText3.alignment = TextAlignmentOptions.TopLeft;

                // 存储引用供后续使用
                CustomTextController ModText = __instance.gameObject.AddComponent<CustomTextController>();
                ModText.CustomText = customText;
                ModText.CustomText3 = customText3;
                ModText.Player = Player;
            }
        }

        [HarmonyPatch(typeof(UI_ScrollToSelectionForInventory))]
        public static class UI_ScrollToSelectionForInventory_Patch
        {
            [HarmonyPatch("UpdateVerticalScrollPosition")]
            [HarmonyPrefix]
            static bool UpdateVerticalScrollPosition_Prefix(UI_ScrollToSelectionForInventory __instance, RectTransform selection)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(UI_HardModePanel))]
        public static class UI_HardModePanel_Patch
        {
            [HarmonyPatch("Awake")]
            [HarmonyPostfix]
            static void Awake_Postfix(UI_HardModePanel __instance)
            {
                GameObject template = __instance.shardLevelIcons[0];
                Transform parent = template.transform.parent;
                GameObject[] newIcons = new GameObject[10];
                __instance.shardLevelIcons.CopyTo(newIcons, 0);
                for (int i = __instance.shardLevelIcons.Length; i < 10; i++)
                {
                    GameObject newIcon = Object.Instantiate(template, parent);
                    newIcon.name = "ShardLevelIcon_" + i;
                    RectTransform rt = newIcon.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(i * 20f, 0f);
                    newIcon.SetActive(false);
                    newIcons[i] = newIcon;
                }
                __instance.shardLevelIcons = newIcons;
            }
        }

        [HarmonyPatch(typeof(UI_BossHPBar))]
        public static class UI_BossHPBar_Patch
        {
            private static readonly FieldInfo bossAvatarField = AccessTools.Field(typeof(UI_BossHPBar), "bossAvatar");
            private static readonly FieldInfo bossAIField = AccessTools.Field(typeof(UI_BossHPBar), "bossAI");
            public static TextMeshProUGUI? LifeText;
            public static GameObject? HPValueText;
            public static float OldLife = -1;
            public static float ExMaxLife = 0;
            private static UnitAvatar? LastNameAvatar;
            private static UnitAvatar? LastNameTarget;
            private static string LastNameText = "";
            private static UI_BossHPBar? CachedBar;
            private static UnitAI_BossBasic? CachedBossAI;
            private static UnitAvatar? CachedBossAvatar;
            private static float NextFieldRefresh;

            [HarmonyPatch("OnOpened")]
            [HarmonyPostfix]
            private static void OnOpened_Postfix(UI_BossHPBar __instance)
            {
                if (__instance == null || __instance.barImage == null) return;
                CachedBar = null; // 重新打开时强制刷新 boss 引用

                if (LifeText == null)
                {
                    HPValueText = new GameObject("HPValueText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(Canvas));
                    HPValueText.transform.SetParent(__instance.barImage.transform, false);
                    HPValueText.transform.SetAsLastSibling();
                    Canvas canvas = HPValueText.GetComponent<Canvas>();
                    canvas.overrideSorting = true;
                    canvas.sortingOrder = 9999;
                    LifeText = HPValueText.GetComponent<TextMeshProUGUI>();
                    if (__instance.nameText != null)
                    {
                        LifeText.font = __instance.nameText.font;
                        LifeText.fontSharedMaterial = __instance.nameText.fontSharedMaterial;
                    }

                    LifeText.fontSize = 10;
                    LifeText.color = Color.white;
                    LifeText.outlineColor = Color.black;
                    LifeText.outlineWidth = 0.15f;
                    LifeText.alignment = TextAlignmentOptions.Center;

                    RectTransform rect = LifeText.rectTransform;
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = new Vector2(200, 24);
                }
            }

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            static void Update_Postfix(UI_BossHPBar __instance)
            {
                if (bossAvatarField == null || bossAIField == null)
                {
                    StarSephiriaMod.LogError("[UI_BossHPBar][UpdateHP]报错");
                    return;
                }

                if (CachedBar != __instance || Time.unscaledTime >= NextFieldRefresh)
                {
                    CachedBar = __instance;
                    CachedBossAI = (UnitAI_BossBasic)bossAIField.GetValue(__instance);
                    CachedBossAvatar = (UnitAvatar)bossAvatarField.GetValue(__instance);
                    NextFieldRefresh = Time.unscaledTime + 0.25f;
                }

                UnitAI_BossBasic? bossAI = CachedBossAI;
                if (bossAI != null)
                {
                    // 客机的 CurrentTarget 只在服务器端计算，不会同步；用 ModNetwork 广播的 netId 反查。
                    UnitAvatar? CurrentTarget = bossAI.CurrentTarget;
                    if (CurrentTarget == null
                        && ModNetwork.BossTargetNetIds.TryGetValue(bossAI.netId, out uint TargetNetId)
                        && NetworkClient.spawned.TryGetValue(TargetNetId, out NetworkIdentity TargetIdentity)
                        && TargetIdentity != null)
                    {
                        CurrentTarget = TargetIdentity.GetComponent<UnitAvatar>();
                    }
                    if (bossAI.Avatar != LastNameAvatar || CurrentTarget != LastNameTarget)
                    {
                        LastNameAvatar = bossAI.Avatar;
                        LastNameTarget = CurrentTarget;
                        string NameText = KeywordDatabase.Convert(bossAI.Avatar.Name, false, false);
                        if (CurrentTarget != null)
                        {
                            NameText += " -> " + CurrentTarget.Name;
                        }
                        if (LastNameText != NameText)
                        {
                            LastNameText = NameText;
                            __instance.nameText.text = NameText;
                        }
                    }
                }

                UnitAvatar? bossAvatar = CachedBossAvatar;
                if (bossAvatar != null)
                {
                    bool falg = false;
                    if (bossAvatar.name.Replace("(Clone)", "") == "MadArmadillo")
                    {
                        if (Unit_ArmadilloGolem_Patch.Golem != null)
                        {
                            bossAvatar = Unit_ArmadilloGolem_Patch.Golem;
                            falg = true;
                        }
                    }
                    if (LifeText != null)
                    {
                        if (ExMaxLife <= 0)
                        {
                            ExMaxLife = bossAvatar.MaxHp;
                        }
                        else if (bossAvatar.hp > OldLife && OldLife > 0)
                        {
                            ExMaxLife += bossAvatar.hp - OldLife;
                        }
                        OldLife = bossAvatar.hp;
                        string NewLine = falg ? "\n" : "";
                        LifeText.text = NewLine + $"{GetHpToString(bossAvatar.hp)} / {GetHpToString(bossAvatar.MaxHp)}";
                        if (ExMaxLife > bossAvatar.MaxHp)
                        {
                            LifeText.text += $" ({GetHpToString(ExMaxLife)})";
                        }
                    }
                }
            }

            private static string GetHpToString(float Value)
            {
                if (Value < 100000000)
                    return Value.ToString("#0");
                return Value.ToString("E2");
            }

            [HarmonyPatch("OnClosed")]
            [HarmonyPostfix]
            private static void OnClosed_Postfix(UI_BossHPBar __instance)
            {
                LifeText = null;
                HPValueText = null;
                ExMaxLife = 0;
                OldLife = -1;
            }
        }

        [HarmonyPatch(typeof(UI_GameOver_DealLabel))]
        public static class UI_GameOver_DealLabel_Patch
        {
            [HarmonyPatch("Initialize")]
            [HarmonyPostfix]
            static void Initialize_Postfix(UI_GameOver_DealLabel __instance, EDamageElementalType elementalType, float value)
            {
                if (elementalType != EffectConstant.Wind) return;
                __instance.damageValueText.text = $"<voffset=-1.01><size=10><sprite=\"keyword\" name=KeywordIcon_Wind></size></voffset>{value:0}";
                __instance.dealRatioImage.color = EffectConstant.WindColor;
            }
        }

        [HarmonyPatch(typeof(UI_GameOverLabel))]
        public static class UI_GameOverLabel_Patch
        {
            [HarmonyPatch("CaptureRunHistoryDetails")]
            [HarmonyPostfix]
            static void CaptureRunHistoryDetails_Postfix(PlayerSpawner spawner, RunHistoryPlayerData data)
            {
                if (spawner == null || data == null) return;
                AvatarStatsHooker? Hooker = spawner.GetComponent<AvatarStatsHooker>();
                if (Hooker == null) return;
                if (data.stats.Any(Stat => Stat != null && Stat.id == TextFile.WindDamage)) return;
                string Value = Hooker.HookStat(TextFile.WindDamage, out AvatarStatsHooker.EStatValueSign Sign, out _);
                data.stats.Add(new RunHistoryStatData { id = TextFile.WindDamage, value = Value ?? "", sign = (int)Sign });
            }
        }

        [HarmonyPatch(typeof(UI_RunResultPanel))]
        public static class UI_RunResultPanel_Patch
        {
            [HarmonyPatch("BindStats")]
            [HarmonyPrefix]
            static void BindStats_Prefix(UI_RunResultPanel __instance)
            {
                EnsureWindColor(__instance);
                EnsureWindStat(__instance);
            }

            [HarmonyPatch("BindDamageView")]
            [HarmonyPrefix]
            static void BindDamageView_Prefix(UI_RunResultPanel __instance)
            {
                EnsureWindColor(__instance);
            }

            private static void EnsureWindColor(UI_RunResultPanel Instance)
            {
                int WindIndex = (int)EffectConstant.Wind;
                Color[]? Colors = Instance.dealColorsByElemental;
                if (Colors != null && Colors.Length > WindIndex) return;
                Color[] NewColors = new Color[WindIndex + 1];
                for (int i = 0; i < NewColors.Length; i++) NewColors[i] = Color.white;
                if (Colors != null) Array.Copy(Colors, NewColors, Colors.Length);
                NewColors[WindIndex] = EffectConstant.WindColor;
                Instance.dealColorsByElemental = NewColors;
            }

            private static void EnsureWindStat(UI_RunResultPanel Instance)
            {
                string[]? Hooks = Instance.elementalHookIds;
                TextMeshProUGUI[]? Values = Instance.elementalValueTexts;
                if (Hooks == null || Values == null || Hooks.Length <= 0 || Values.Length <= 0) return;
                if (Array.IndexOf(Hooks, TextFile.WindDamage) >= 0) return;
                int OriginalCount = Math.Min(Hooks.Length, Values.Length);

                int TemplateIndex = Array.IndexOf(Hooks, "LIGHTNING_DAMAGE");
                if (TemplateIndex < 0 || TemplateIndex >= OriginalCount) TemplateIndex = OriginalCount - 1;
                TextMeshProUGUI? Template = Values[TemplateIndex];
                if (Template == null) return;

                Transform? Block = FindElementBlock(Template.transform);
                if (Block == null || Block.parent == null || Block == Template.transform) return;

                GameObject Clone = Object.Instantiate(Block.gameObject, Block.parent);
                Clone.name = "Elemental_WindDamage";
                Clone.transform.SetSiblingIndex(Block.GetSiblingIndex() + 1);

                UI_LocalizationStringText? Label = Clone.GetComponentsInChildren<UI_LocalizationStringText>(true)
                    .FirstOrDefault(Text => Text != null && IsElementKey(Text.valueString?.key));
                if (Label != null) Label.UpdateKey("Elemental_Wind");
                else
                {
                    LocalizedText? LegacyLabel = Clone.GetComponentsInChildren<LocalizedText>(true)
                        .FirstOrDefault(Text => Text != null && IsElementKey(Text.str?.key));
                    if (LegacyLabel != null) LegacyLabel.ChangeKey("Elemental_Wind");
                }

                Sprite? WindIcon = KeywordDatabase.GetEntity("WindDamage")?.keywordImage;
                Sprite? LightningIcon = KeywordDatabase.GetEntity("LightningDamage")?.keywordImage;
                Image[] BlockImages = Block.GetComponentsInChildren<Image>(true);
                Image? TemplateIcon = LightningIcon != null
                    ? BlockImages.FirstOrDefault(Icon => Icon.sprite == LightningIcon)
                    : null;
                if (TemplateIcon == null)
                {
                    List<Image> IconCandidates = BlockImages.Where(Icon => Icon.sprite != null).ToList();
                    if (IconCandidates.Count == 1) TemplateIcon = IconCandidates[0];
                }
                if (WindIcon != null && TemplateIcon != null && TemplateIcon.sprite != null)
                {
                    Sprite TemplateSprite = TemplateIcon.sprite;
                    Image? CloneIcon = Clone.GetComponentsInChildren<Image>(true)
                        .FirstOrDefault(Icon => Icon.sprite == TemplateSprite);
                    if (CloneIcon != null) CloneIcon.sprite = WindIcon;
                }

                Transform? CloneValueTransform = FindRelative(Clone.transform, Block, Template.transform);
                TextMeshProUGUI? CloneValue = CloneValueTransform != null ? CloneValueTransform.GetComponent<TextMeshProUGUI>() : null;
                if (CloneValue == null)
                {
                    Object.Destroy(Clone);
                    return;
                }
                CloneValue.color = EffectConstant.WindColor;

                Instance.elementalValueTexts = Values.Append(CloneValue).ToArray();
                Instance.elementalHookIds = Hooks.Append(TextFile.WindDamage).ToArray();

                RelayoutElementBlocks(Instance, Block, Clone.transform, Values, OriginalCount);
            }

            private static bool IsElementKey(string? Key)
            {
                return !string.IsNullOrEmpty(Key) && Key.StartsWith("Elemental_");
            }

            private static int CountElementLabels(Transform Root)
            {
                int Count = Root.GetComponentsInChildren<UI_LocalizationStringText>(true)
                    .Count(Text => Text != null && IsElementKey(Text.valueString?.key));
                Count += Root.GetComponentsInChildren<LocalizedText>(true)
                    .Count(Text => Text != null && IsElementKey(Text.str?.key));
                return Count;
            }

            private static Transform? FindElementBlock(Transform Value)
            {
                Sprite? LightningIcon = KeywordDatabase.GetEntity("LightningDamage")?.keywordImage;
                Transform Current = Value;
                Transform Last = Value;
                for (int Depth = 0; Depth < 6 && Current != null; Depth++)
                {
                    if (Current != Value)
                    {
                        if (CountElementLabels(Current) == 1) return Current;
                        Image[] Images = Current.GetComponentsInChildren<Image>(true);
                        if (LightningIcon != null && Images.Any(Icon => Icon.sprite == LightningIcon)) return Current;
                        int TextCount = Current.GetComponentsInChildren<TextMeshProUGUI>(true).Length;
                        int IconCount = Images.Count(Icon => Icon.sprite != null);
                        if (TextCount <= 2 && IconCount >= 1 && IconCount <= 2) return Current;
                    }
                    Last = Current;
                    Current = Current.parent;
                }
                return Value.parent;
            }

            private static Transform? FindRelative(Transform Root, Transform OriginalRoot, Transform Target)
            {
                List<int> Path = [];
                Transform? Current = Target;
                while (Current != null && Current != OriginalRoot)
                {
                    Path.Add(Current.GetSiblingIndex());
                    Current = Current.parent;
                }
                if (Current == null) return null;
                Transform Result = Root;
                for (int i = Path.Count - 1; i >= 0; i--)
                    Result = Result.GetChild(Path[i]);
                return Result;
            }

            private static void RelayoutElementBlocks(UI_RunResultPanel Instance, Transform TemplateBlock, Transform Clone, TextMeshProUGUI[] OriginalValues, int OriginalCount)
            {
                Transform? Parent = TemplateBlock.parent;
                if (Parent == null) return;
                if (Parent.GetComponent<LayoutGroup>() != null) return;

                List<RectTransform> Group = [];
                List<RectTransform> Originals = [];
                foreach (Transform Child in Parent)
                {
                    if (Child is not RectTransform Rect) continue;
                    bool IsClone = Child == Clone;
                    bool HasOriginal = false;
                    for (int i = 0; i < OriginalCount && i < OriginalValues.Length; i++)
                    {
                        TextMeshProUGUI? Value = OriginalValues[i];
                        if (Value != null && Value.transform.IsChildOf(Child))
                        {
                            HasOriginal = true;
                            break;
                        }
                    }
                    if (IsClone || HasOriginal)
                    {
                        if (!Group.Contains(Rect)) Group.Add(Rect);
                        if (HasOriginal && !Originals.Contains(Rect)) Originals.Add(Rect);
                    }
                }
                if (Group.Count <= 1 || Originals.Count < 2) return;
                Group.Sort((A, B) => A.GetSiblingIndex().CompareTo(B.GetSiblingIndex()));
                Originals.Sort((A, B) => A.GetSiblingIndex().CompareTo(B.GetSiblingIndex()));

                // 沿用原 4 个元素的间距，只把整组居中扩展到 5 个，不按容器宽度拉满
                Vector2 First = Originals[0].anchoredPosition;
                Vector2 Last = Originals[^1].anchoredPosition;
                Vector2 Dir = (Last - First).normalized;
                float Step = (Last - First).magnitude / (Originals.Count - 1);
                Vector2 Center = (First + Last) * 0.5f;
                float HalfSpan = Step * (Group.Count - 1) * 0.5f;
                for (int i = 0; i < Group.Count; i++)
                    Group[i].anchoredPosition = Center + Dir * (-HalfSpan + Step * i);
            }
        }
    }
}
