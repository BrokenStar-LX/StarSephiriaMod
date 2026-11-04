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
        [HarmonyPatch(typeof(KeywordDatabase))]
        public static class KeywordDatabase_Patch
        {
            private static readonly FieldInfo constField = AccessTools.Field(typeof(KeywordDatabase), "constValueDictionary");
            private static readonly FieldInfo floatField = AccessTools.Field(typeof(KeywordDatabase), "constFloatValueDictionary");

            [HarmonyPatch("Convert")]
            [HarmonyPrefix]
            static void Convert_Prefix(ref bool useColor)
            {
                useColor = true;
            }

            [HarmonyPatch("Initialize")]
            [HarmonyPostfix]
            static void Initialize_Postfix()
            {

                if (constField == null || floatField == null)
                {
                    StarSephiriaMod.LogError($"[KeywordDatabase][Initialize]反射常量数据失败");
                    return;
                }

                Dictionary<string, int> ConstValueDictionary = (Dictionary<string, int>)constField.GetValue(null);
                Dictionary<string, float> ConstFloatValueDictionary = (Dictionary<string, float>)floatField.GetValue(null);

                foreach (var kvp in ConstValueDictionary)
                {
                    StarSephiriaMod.LogInfoProjectile($"[整型常量] {kvp.Key} = {kvp.Value}");
                }
                foreach (var kvp in ConstFloatValueDictionary)
                {
                    StarSephiriaMod.LogInfoProjectile($"[浮点常量] {kvp.Key} = {kvp.Value}");
                }
                if (ModConfig.Instance.EnableWeaponModifier)
                {
                    ConstValueDictionary["DIMENSIONPOCKETLIMIT"] = 24;//许愿池硬上限
                    ConstValueDictionary["CHARGEDSWEEPTIME"] = 1;//恐惧高涨冷却
                    ConstValueDictionary["KATANAECLIPSEDAMAGEBONUSBYCONSUMESWORDCOUNT"] = 8;//日食武器伤害加成
                    ConstValueDictionary["KATANACONSUMESWORDCOUNTLIMIT"] = 99;//日食最多消耗剑数量
                    ConstValueDictionary["SOLISMISSIODECREASEMOVESPEEDONGUARD"] = 0;//索利斯使命举盾减速
                    ConstValueDictionary["GREATSWORDMONEYWHIRLWINDCOST"] = 10;//叶爆消耗叶子
                    ConstValueDictionary["CROSSBOWC4BOMBEXPLOSIONDEFAULTDAMAGE"] = 40;//藤壶基础伤害
                    ConstValueDictionary["CROSSBOWC4BOMBEXPLOSIONDEFENSEPERCENT"] = 200;//藤壶伤害倍率
                    ConstValueDictionary["KATANASHEATHHARDENINGMAXCOUNT"] = 20;//破甲最大层数
                    ConstValueDictionary["KATANASHEATHHARDENINGTIMER_DIV10"] = 6;//破甲最大层数
                }
                if (ModConfig.Instance.ModifyCharm)
                {
                    ConstValueDictionary["GOLDHANDMAX"] = 40;//谈判4加成上限
                }
                if (ModConfig.Instance.ModifyMerchant)
                {
                    ConstValueDictionary["MAXWITCHHATITEMS"] = 8;//女巫出售物品数量
                }
                constField.SetValue(null, ConstValueDictionary);
                floatField.SetValue(null, ConstFloatValueDictionary);
            }
        }

        [HarmonyPatch(typeof(ItemDatabase))]
        public static class ItemDatabase_Patch
        {
            private static readonly FieldInfo? itemIDListField = AccessTools.Field(typeof(ItemDatabase), "itemIDList");
            private static readonly FieldInfo? itemDictionaryField = AccessTools.Field(typeof(ItemDatabase), "itemDictionary");
            private static readonly FieldInfo? itemCategoriesField = AccessTools.Field(typeof(ItemDatabase), "itemCategories");
            private static readonly FieldInfo? allWitchHatItemsField = AccessTools.Field(typeof(ItemDatabase), "allWitchHatItems");

            [HarmonyPatch("Initialize")]
            [HarmonyPostfix]
            public static void Initialize_Postfix()
            {
                if (itemIDListField == null || itemDictionaryField == null || itemCategoriesField == null || allWitchHatItemsField == null) return;
                {
                    List<ItemEntity> allWitchHatItems = (List<ItemEntity>)allWitchHatItemsField.GetValue(null);
                    List<int> itemIDList = (List<int>)itemIDListField.GetValue(null);
                    Dictionary<int, ItemEntity> itemDictionary = (Dictionary<int, ItemEntity>)itemDictionaryField.GetValue(null);
                    Dictionary<string, ItemCategoryEntity> itemCategories = (Dictionary<string, ItemCategoryEntity>)itemCategoriesField.GetValue(null);
                    foreach (ItemCategoryEntity Data in ModItemCategoryData)
                    {
                        itemCategories[Data.id] = Data;
                    }
                    foreach (int DisableId in DisableCharm)
                    {
                        itemDictionary[DisableId].activeType = EItemActiveType.Hidden;
                    }
                    if (ModConfig.Instance.ModifyCharm)
                    {
                        foreach (ItemCategoryEntity itemCategoryEntity in itemCategories.Values)
                        {
                            if (itemCategoryEntity.id == "DARKCLOUD")
                            {
                                if (itemCategoryEntity.comboEffectPrefab.GetComponent<ComboEffect_DarkCloud>() is ComboEffect_DarkCloud DarkCloud)
                                {
                                    DarkCloud.defaultRestorePercent = 10;
                                    DarkCloud.addStatByCombo =
                                    [
                                        new(){ comboCount = 4, status = ["MIN_DARK_CLOUD/25"] },
                                        new(){ comboCount = 6, status = ["MIN_DARK_CLOUD/25"] },
                                        new(){ comboCount = 8, status = ["MIN_DARK_CLOUD/25"] },
                                        new(){ comboCount = 10, status = ["DARK_CLOUD_MULTISHOT/2"] },
                                    ];
                                }
                            }//乌云
                            else if (itemCategoryEntity.id == "LAKE")
                            {
                                if (itemCategoryEntity.comboEffectPrefab.GetComponent<ComboEffectBase>() is ComboEffectBase Lake)
                                {
                                    Lake.addStatByCombo =
                                    [
                                        new(){ comboCount = 2, status = ["MAX_MP/10", "MP_REGEN/10"] },
                                        new(){ comboCount = 4, status = ["MAX_MP/15", "MP_REGEN/20"] },
                                        new(){ comboCount = 6, status = ["MAX_MP/20", "MP_SKILL_DAMAGE/5"] },
                                        new(){ comboCount = 8, status = ["MAX_MP/25", "MP_SKILL_DAMAGE/10"] },
                                        new(){ comboCount = 10, status = ["MAX_MP/30", "MP_SKILL_DAMAGE/20"] },
                                    ];
                                }
                            }//湖泊
                            else if (itemCategoryEntity.id == "PARTY")
                            {
                                if (itemCategoryEntity.comboEffectPrefab.GetComponent<ComboEffectBase>() is ComboEffectBase PARTY)
                                {
                                    PARTY.addStatByCombo =
                                    [
                                        new(){ comboCount = 2, status = ["ALL_PLAYER_DEFENSE/8"] },
                                        new(){ comboCount = 4, status = ["ALL_PLAYER_ALL_DAMAGE/8"] },
                                        new(){ comboCount = 6, status = ["ALL_PLAYER_ELEMENTAL_DAMAGE/8"] }
                                    ];
                                }
                            }//派对
                            else if (itemCategoryEntity.id == "SHADOW")
                            {
                                if (itemCategoryEntity.comboEffectPrefab.GetComponent<ComboEffectBase>() is ComboEffectBase SHADOW)
                                {
                                    SHADOW.addStatByCombo =
                                    [
                                        new(){ comboCount = 2, status = ["EVASION/600"] },
                                        new(){ comboCount = 4, status = ["EVASION/700"] },
                                        new(){ comboCount = 6, status = ["EVASION/800"] },
                                        new(){ comboCount = 8, status = ["EVASION/1000"] },
                                        new(){ comboCount = 10, status = ["EVASION/1200", "IGNORE_DEFENSE/5"] },
                                    ];
                                }
                            }//影子
                            else if (itemCategoryEntity.id == "ELEMENTAL")
                            {
                                if (itemCategoryEntity.comboEffectPrefab.GetComponent<ComboEffectBase>() is ComboEffectBase ELEMENTAL)
                                {
                                    foreach (ComboEffectBase.ComboStat stat in ELEMENTAL.addStatByCombo)
                                    {
                                        if (stat.replaceEffectText)
                                        {
                                            stat.status =
                                            [
                                                "PHYSICAL_DAMAGE_AMP/10",
                                                "FIRE_DAMAGE_AMP/10",
                                                "ICE_DAMAGE_AMP/10",
                                                "LIGHTNING_DAMAGE_AMP/10",
                                                "WIND_DAMAGE_AMP/10",
                                            ];
                                            break;
                                        }
                                    }
                                }
                            }//元素
                            else if (itemCategoryEntity.id == "ACADEMY")
                            {
                                if (itemCategoryEntity.comboEffectPrefab.GetComponent<ComboEffectBase>() is ComboEffectBase ACADEMY)
                                {
                                    ACADEMY.addStatByCombo =
                                    [
                                        new(){ comboCount = 2, status = ["MP_REGEN/10", "MP_STEAL/5"] },
                                        new(){ comboCount = 4, status = ["COOLDOWN_RECOVERY_SPEED/15"] },
                                        new(){ comboCount = 6, status = ["MAGIC_DAMAGE_BONUS/10"] },
                                        new(){ comboCount = 8, status = ["COOLDOWN_RECOVERY_SPEED/30"] },
                                        new(){ comboCount = 10, status = ["MAGIC_DAMAGE_BONUS/20"] },
                                    ];
                                }
                            }//学院
                        }
                    }

                    List<int> DisplayItems = [ 1305 ];
                    foreach (ItemEntity itemEntity in Resources.LoadAll<ItemEntity>("Item"))
                    {
                        if (itemIDList.Contains(itemEntity.id))
                        {
                            if (itemEntity.isDual && itemEntity.activeType != EItemActiveType.Hidden) BondingItems.Add(itemEntity.id);
                            else if (DisplayItems.Contains(itemEntity.id) && !BondingItems.Contains(itemEntity.id))
                            {
                                if (itemEntity.cannotBeReward) itemEntity.cannotBeReward = false;
                                if (itemEntity.activeType == EItemActiveType.Hidden) itemEntity.activeType = EItemActiveType.Default;
                                BondingItems.Add(itemEntity.id);
                            }
                        }
                        if (itemIDList.Contains(itemEntity.id) || DisableCharm.Contains(itemEntity.id))
                        {
                            continue;
                        }
                        if (ModConfig.Instance.DisplayDisabledItems || DisplayItems.Contains(itemEntity.id))
                        {
                            if (!itemIDList.Contains(itemEntity.id)) itemIDList.Add(itemEntity.id);
                            if (!itemDictionary.ContainsKey(itemEntity.id)) itemDictionary.Add(itemEntity.id, itemEntity);
                        }
                    }
                    if (ModConfig.Instance.DisableUniqueEffects)
                    {
                        foreach (ItemEntity item in itemDictionary.Values.Where(i => i.type == EItemType.Charm && i.resourcePrefab != null))
                        {
                            Charm_Basic charmBasic = item.resourcePrefab.GetComponent<Charm_Basic>();
                            if (charmBasic != null)
                            {
                                charmBasic.isUniqueEffect = false;
                            }
                        }
                    }
                    int LastId = 0;
                    foreach (ItemEntity AllItem in itemDictionary.Values)
                    {
                        if (LastId < AllItem.id)
                            LastId = AllItem.id;
                    }
                    foreach (ModItem Data in ModItemEntityData)
                    {
                        ItemEntity? ModItem = Data.Item;
                        if (ModItem == null) continue;

                        int NewId = LastId + 1000;
                        while (itemDictionary.ContainsKey(NewId)) NewId++;
                        LastId = NewId - 1000 + 1;

                        try
                        {
                            ModItem.id = NewId;
                            if (ModItem.type == EItemType.Potion && itemDictionary.TryGetValue(40, out ItemEntity PotionTemplate))
                                ModItem.iconInWorld = PotionTemplate.iconInWorld;
                            else if (itemDictionary.TryGetValue(1000, out ItemEntity WorldTemplate))
                                ModItem.iconInWorld = WorldTemplate.iconInWorld;
                            string GetName = ModItem.name.Replace("ModItemEntity_", "");
                            if (!GetModItem.ContainsKey(GetName)) GetModItem.Add(GetName, ModItem);
                            if (Data.IsWitchItem) allWitchHatItems.Add(ModItem);
                            Data.ItemEffect?.Invoke(ModItem);
                            if (ModConfig.Instance.DisableUniqueEffects)
                            {
                                if (ModItem.resourcePrefab.TryGetComponent<Charm_Basic>(out var Basic))
                                {
                                    Basic.isUniqueEffect = false;
                                }
                            }
                            ItemDatabase.Register(ModItem);
                            if (ModItem.isDual && !BondingItems.Contains(ModItem.id))
                                BondingItems.Add(ModItem.id);
                        }
                        catch (Exception ex)
                        {
                            StarSephiriaMod.LogError($"[StarSephiria]物品注册失败{ModItem.name}: {ex}");
                        }
                    }

                    itemCategoriesField.SetValue(null, itemCategories);
                    itemIDListField.SetValue(null, itemIDList);
                    itemDictionaryField.SetValue(null, itemDictionary);
                    allWitchHatItemsField.SetValue(null, allWitchHatItems);
                }
            }

            [HarmonyPatch("GetRandomRarity")]
            [HarmonyPostfix]
            static void GetRandomRarity_Postfix(Random seed, float uncommon, float rare, float legend, ref EItemRarity __result)
            {
                if (!ModConfig.Instance.MonochromeMode_TABLET && (CurrentSephiriteType == Sephirite.Type.TABLET_BOSS || CurrentSephiriteType == Sephirite.Type.TABLET)) return;

                int Value = ModConfig.Instance.MonochromeMode;
                if (Value > 0)
                {
                    switch (Value)
                    {
                        case 1:
                            __result = EItemRarity.Common;
                            break;
                        case 2:
                            __result = EItemRarity.Uncommon;
                            break;
                        case 3:
                            __result = EItemRarity.Rare;
                            break;
                        case 4:
                            __result = EItemRarity.Legend;
                            break;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SocialIDDatabase))]
        public static class SocialIDDatabase_Patch
        {
            [HarmonyPatch("Initialize")]
            [HarmonyPostfix]
            public static void Initialize_Postfix()
            {
                SocialIDEntity social = SocialIDDatabase.FindByName("Traveler_Mercenary_WeaselKnight");
                if (social)
                {
                    foreach (ModItem Data in ModItemEntityData)
                    {
                        ItemEntity? ModItem = Data.Item;
                        if (ModItem == null) continue;
                        switch (ModItem.name)
                        {
                            case "ModItemEntity_RedDogSymbolA":
                            case "ModItemEntity_RedDogSymbolB":
                            case "ModItemEntity_RedDogSymbolC":
                                Charm_LeadNPC Effect = ModItem.resourcePrefab.GetComponent<Charm_LeadNPC>();
                                Effect.npcSocialID = SocialIDDatabase.FindByName("Traveler_Mercenary_WeaselKnight");
                                break;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(HardModeDatebase))]
        public static class HardModeDatebase_Patch
        {
            [HarmonyPatch("Initialize")]
            [HarmonyPostfix]
            public static void Initialize_Postfix()
            {
                foreach (HardModeShardEntity shard in HardModeDatebase.GetAll())
                {
                    StarSephiriaMod.LogInfoProjectile($"困难模式条目：{shard.Name} ID：{shard.shardKey}");
                }
                foreach (HardModeShardEntity shard in ModHardModeShard)
                {
                    if (shard.shardKey == "[Star's Sephiria Mod]HardMode")
                    {
                        shard.shardMaxLevel = 4;
                        shard.effectPerLevel = [1, 2, 3, 4];
                        shard.stepIncreasePerLevel = [10, 10, 10, 1];
                    }
                    HardModeDatebase.data[shard.shardKey] = shard;
                }
                if (HardModeDatebase.data.TryGetValue("DIZZINESS", out HardModeShardEntity Dizziness))
                {
                    Dizziness.shardMaxLevel = 3;
                    Dizziness.effectPerLevel = [1, 2, 3];
                    Dizziness.stepIncreasePerLevel = [2, 2, 2];
                }
            }
        }

        [HarmonyPatch(typeof(MiracleDatabase))]
        public static class MiracleDatabase_Patch
        {
            [HarmonyPatch("Initialize")]
            [HarmonyPostfix]
            public static void Initialize_Postfix(MiracleDatabase __instance)
            {
                try
                {
                    if (ModConfig.Instance.DisplayDisabledItems)
                    {
                        GameObject[] array = Resources.LoadAll<GameObject>("Miracle");
                        for (int i = 0; i < array.Length; i++)
                        {
                            Miracle component = array[i].GetComponent<Miracle>();
                            if (component && !component.isEnabled)
                            {
                                RegisterInternalMethod.Invoke(null, [component]);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[MiracleDatabase]添加隐藏奇迹失败: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        [HarmonyPatch(typeof(WeaponDatabase))]
        public static class WeaponDatabase_Patch
        {
            private static FieldInfo? weaponDictionaryField = null;

            [HarmonyPatch("Initialize")]
            [HarmonyPostfix]
            public static void Initialize_Postfix()
            {
                if (!ModConfig.Instance.DisplayDisabledItems) return;
                try
                {
                    if (weaponDictionaryField == null)
                    {
                        weaponDictionaryField = AccessTools.Field(typeof(WeaponDatabase), "weaponDictionary");
                    }

                    Dictionary<int, WeaponEntity> weaponDictionary = (Dictionary<int, WeaponEntity>)weaponDictionaryField.GetValue(null);
                    foreach (WeaponEntity weaponEntity in Resources.LoadAll<WeaponEntity>("Weapon"))
                    {
                        if (!weaponEntity.enabled)
                        {
                            weaponDictionary.Add(weaponEntity.id, weaponEntity);
                        }
                    }
                    weaponDictionaryField.SetValue(null, weaponDictionary);
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[WeaponDatabase]添加隐藏武器失败: {ex.Message}\n{ex.StackTrace}");
                }
            }

            [HarmonyPatch("GetDefaultWeapons")]
            [HarmonyPostfix]
            public static void GetDefaultWeapons_Postfix(ref List<WeaponEntity> __result)
            {
                if (ModConfig.Instance.ModifyRandomWeaponOnStart)
                {
                    List<WeaponEntity> UseRandmon = [];
                    foreach (WeaponEntity Weapon in WeaponDatabase.GetAll())
                    {
                        if (Weapon.isDefaultWeapon) continue;
                        var EnhancementsList = WeaponDatabase.GetWeaponEnhancements(Weapon.id);
                        if (EnhancementsList?.Count > 0)
                        {
                            foreach (EnhancementMetadata metadata in EnhancementsList)
                            {
                                UseRandmon.Add(metadata.enhanced);
                            }
                        }
                    }
                    __result = UseRandmon;
                }
            }
        }

        [HarmonyPatch(typeof(EffectHUDDatabase))]
        public static class EffectHUDDatabase_Patch
        {
            private static readonly FieldInfo? effectHUDDictionaryField = AccessTools.Field(typeof(EffectHUDDatabase), "effectHUDDictionary");

            [HarmonyPatch("Initialize")]
            [HarmonyPostfix]
            public static void Initialize_Postfix()
            {
                if (effectHUDDictionaryField == null) return;
                Dictionary<string, EffectHUDEntity> effectHUDDictionary = (Dictionary<string, EffectHUDEntity>)effectHUDDictionaryField.GetValue(null);
                GameObject? Orig = null;
                foreach (EffectHUDEntity Entity in effectHUDDictionary.Values)
                {
                    if (Entity.hudPrefab.TryGetComponent<UI_EffectHUD_Stack>(out var Target) && Target != null)
                    {
                        Orig = Entity.hudPrefab;
                        break;
                    }
                }
                if (Orig != null)
                {
                    foreach(ModEffectHUD Effect in ModEffectHUDData)
                    {
                        GameObject NewHud = Object.Instantiate(Orig);
                        Effect.SetEffectHUD(NewHud);
                        effectHUDDictionary[Effect.Name.ToUpperInvariant()] = Effect.EndCreate(NewHud);
                    }
                }
                effectHUDDictionaryField.SetValue(null, effectHUDDictionary);
            }
        }

        [HarmonyPatch(typeof(UnitDatabase))]
        public static class UnitDatabase_Patch
        {
            private static readonly FieldInfo? debuffEntitiesField = AccessTools.Field(typeof(UnitDatabase), "debuffEntities");

            [HarmonyPatch("Initialize")]
            [HarmonyPostfix]
            public static void Initialize_Postfix()
            {
                if (debuffEntitiesField == null) return;
                Dictionary<string, CharacterDebuff> DeBuffList = (Dictionary<string, CharacterDebuff>)debuffEntitiesField.GetValue(null);
                foreach (ModCharacterDebuff Entity in ModDeBuffData)
                {
                    CharacterDebuff? Target = Entity.Prefab;
                    if (Target != null) DeBuffList[Target.ID] = Target;
                }
                debuffEntitiesField.SetValue(null, DeBuffList);
            }
        }

    }
}
