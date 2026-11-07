using HarmonyLib;
using Mirror;
using SPMod.Tool;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using static ComboEffect_Debuff;
using static ComboEffectBase;
using static Miracle;
using static Miracle_StatusInstance;
using static SPMod.Tool.EffectConstant;
using static SPMod.Tool.ModuleComponents;
using Object = UnityEngine.Object;
using StatusGroup = Charm_StatusInstance.StatusGroup;

namespace SPMod.ModContent
{
    public static class LoadModContent
    {
        public static uint ModAssetId = 1000000000u;
        public static uint LoadModAssetId()
        {
            uint AssetId = ModAssetId;
            ModAssetId += 1u;
            return AssetId;
        }

        public static PropertyInfo AssetIdProperty = AccessTools.Property(typeof(NetworkIdentity), "assetId");
        public static void SetModAssetId(NetworkIdentity Target, uint AssetId)
        {
            AssetIdProperty.SetValue(Target, AssetId);
        }

        static LoadModContent()
        {
            ModNetwork.RegisterSwingFx(ModNetwork.SwingFxMoment, () => WindKatana_Moment.Data);
        }

        //模组根之退
        public static List<HardModeShardEntity> ModHardModeShard =
        [
            new ModHardModeShardEntity("BossRushMode", "[Star's Sephiria Mod]BossRushMode", EHardModeShardType.World, 1, [1], [0]).EndCreate(),
            new ModHardModeShardEntity("InfiniteMode", "[Star's Sephiria Mod]InfiniteMode", EHardModeShardType.World, 1, [1], [0]).EndCreate(),
            new ModHardModeShardEntity("HardMode", "[Star's Sephiria Mod]HardMode", EHardModeShardType.World, 3, [1, 2, 3], [5, 10, 15]).EndCreate(),
            new ModHardModeShardEntity("PollutedBlood", "[Star's Sephiria Mod]PollutedBlood", EHardModeShardType.Plan, 1, [50], [3]).EndCreate(),
            new ModHardModeShardEntity("EvilGuardian", "[Star's Sephiria Mod]EvilGuardian", EHardModeShardType.Demon, 1, [40], [4]).EndCreate(),
        ];
        //模组奇迹
        public static List<ModMiracle> ModMiraclesData =
        [
            new ModMiracle("Bard", "HURRICANE").SetStat("WIND_DAMAGE/5=Positive", "WIND_EROSION_DAMAGE/25=Positive").EndCreate(),
            new ModMiracle("Shaman", "CURSE").SetStat("DEBUFF_DAMAGE/20=Positive", "DEBUFF_DURATION/40=Positive").EndCreate(),
            new ModMiracle("Negotiator", "SAVVY").SetStat("NEGOTIATION/10=Positive", "LEAF_DROP/40=Positive").EndCreate(),
            new ModMiracle("Knight", "WEAPON").SetStat("BASIC_ATTACK_DAMAGE/20=Positive", "SPECIAL_ATTACK_DAMAGE/20=Positive").EndCreate(),
        ];
        public static Dictionary<string, List<StatInfo>> ModfyisMiracleStatus = new()
        {
            { "General", Tools.SetStatInfo("FOLLOWER_ATTACK_SPEED/35=Positive") },//将军
            { "Astronomer", Tools.SetStatInfo("PLANET_ATTACK_SPEED/30=Positive") },//天文学家
            { "Meteorologist", Tools.SetStatInfo("LIGHTNING_DAMAGE/8=Positive", "DARK_CLOUD_DAMAGE/30=Positive") },//气象学家
            { "Guard", Tools.SetStatInfo("FINAL_DAMAGE/12=Positive", "DEFENSE/12=Positive") },//警卫
            { "Elementalist", Tools.SetStatInfo("HIGHEST_ELEMENTAL_DAMAGE/8=Positive") },//元素师
            { "Assassin", Tools.SetStatInfo("PHYSICAL_DAMAGE/6=Positive", "CRITICAL_DAMAGE_RATE/24=Positive") },//刺客
            { "Duelist", Tools.SetStatInfo("ATTACK_SPEED/30=Positive", "WEAPON_RANGE/20=Positive") },//决斗家
            { "Rogue", Tools.SetStatInfo("PHYSICAL_DAMAGE/10=Positive", "EVASION/1000=Positive") },//盗贼
            { "Scholar", Tools.SetStatInfo("MAGIC_DAMAGE_BONUS/10=Positive", "MAGIC_CRITICAL_DAMAGE_RATE/30=Positive") },//学者
            { "IntelligenceAgent", Tools.SetStatInfo("DASH_COUNT/2=Positive", "DASH_ATTACK_DAMAGE/40=Positive") },//情报员
            { "AegisOfTree", Tools.SetStatInfo("PHYSICAL_DAMAGE/10=Positive", "DEFENSE/10=Positive") },//侍从
            { "FrostSmith", Tools.SetStatInfo("ICE_DAMAGE/8=Positive", "CRITICAL/1200=Positive") },//冰锻造师
            { "SnowSentinel", Tools.SetStatInfo("ICE_DAMAGE/8=Positive", "FREEZE_DAMAGE/20=Positive") },//雪原守卫
            { "Berserker", Tools.SetStatInfo("TRUE_DAMAGE/4=Positive", "FINAL_DAMAGE/15=Positive") },//狂战士
            { "Angler", Tools.SetStatInfo("HIGHEST_ELEMENTAL_DAMAGE/6=Positive", "FINAL_MP/22=Positive") },//钓鱼人
            { "FlameSword", Tools.SetStatInfo("FIRE_DAMAGE/8=Positive", "FLAME_SWORD_MAX/4=Positive", "FLAME_SWORD_FAST_FALL/20=Positive") },//陨铁铁匠
        };
        //模组减益
        public static ModCharacterDebuff WindErosion = new ModCharacterDebuff("WindErosion", "CharacterDebuff_WindErosion", "WindErosion", WindColor)
        .SetCustomDebuff(Target => Target.AddComponent<CharacterDebuff_WindErosion>())
        .SetVisual("BURN")
        .EndCreate(7);
        //模组连击
        public static List<ItemCategoryEntity> ModItemCategoryData =
        [
            new ModItemCategory("COMET", "Comet", "StarApple")
            .SetStat(new ComboStat() { comboCount = 2, status = ["DASH_RECOVERY_SPEED/10", "DASH_ATTACK_DAMAGE/20"] })
            .SetStat(new ComboStat() { comboCount = 4, status = ["DASH_COUNT/1", "DASH_ATTACK_DAMAGE/20"] })
            .EndCreate(),
            new ModItemCategory("FORTUNE", "Fortune", "Pomegranate")
            .SetStat(new ComboStat() { comboCount = 2, status = ["BOSS_REWARD_DICE/1"] })
            .SetStat(new ComboStat() { comboCount = 4, status = ["EXTRA_ITEM_CHOICES/1"] })
            .EndCreate(),
            new ModItemCategory("HURRICANE", "Hurricane", "FlowerButterflyFruit")
            .SetCustomComboEffect(Combo =>
            {
                ComboEffect_Debuff Effect = Combo.AddComponent<ComboEffect_Debuff>();
                Effect.defaultEffect = new LocalizedString("ComboEffectDefault_Glacier_Description");
                Effect.debuffActivateEffectNameString = new LocalizedString("ModKeyword_WindErosion_DebuffActivateName");
                Effect.debuffActivateEffectString = new LocalizedString("ComboEffect_Glacier_DebuffActivate");
                Effect.debuffHasteEffectString = new LocalizedString("ComboEffect_Glacier_DebuffHaste");
                Effect.damageElementalTargetType = Wind;
                Effect.cooldownInfos = [];
                Effect.debuffPrefab = WindErosion.Prefab;
                Effect.debuffName = "WindErosion";
                Effect.debuffActivateComboCount = 2;
                Effect.debuffHasteComboCount = 6;
                Effect.debuffHastePercent = 150;
                Effect.addStatByCombo =
                [
                    new() { comboCount = 4, status = ["WIND_DAMAGE/6"] },
                    new() { comboCount = 8, status = ["WIND_DAMAGE/8"] },
                    new() { comboCount = 10, status = ["WIND_EROSION_MULT/24"]},
                ];
                Effect.enabled = false;
            })
            .EndCreate(),
        ];
        //模组关键词
        public static List<ModKeyWord> ModKeywordEntityData =
        [
            new ModKeyWord("Stat_FinalAttackSpeed", true, AttackSpeedColor).UseKeyWordSprite("AttackSpeed"),
            new ModKeyWord("Stat_Determination", true, WeaponColor),
            new ModKeyWord("Stat_Reinforce", true, WeaponColor),
            new ModKeyWord("Stat_GuaranteedCrit", true, CritColor),
            new ModKeyWord("Stat_ElectricIce", true, IceColor),
            new ModKeyWord("Stat_ElementalDamage", true, ElementalColor).UseKeyWordSprite("HighestElementalDamage"),
            new ModKeyWord("Stat_DirectWeaponDamage", false, Color.white).UseKeyWordSprite("PhysicalDamage").CreateStatus("DIRECT_WEAPON_DAMAGE", "%"),
            new ModKeyWord("Stat_MeleeDamage", false, Color.white).UseKeyWordSprite("PhysicalDamage").CreateStatus("MELEE_DAMAGE", "%"),
            new ModKeyWord("Stat_MagicCooldownReduction", true, CooldownColor).UseKeyWordSprite("CooldownRecovery").CreateStatus("MAGIC_COOLDOWN_REDUCTION", "%"),
            new ModKeyWord("Stat_MaxDimensionPocket", false, Color.white).CreateStatus("MAX_DIMENSION_POCKET"),
            new ModKeyWord("Stat_GoldObtained", false, Color.white).CreateStatus("STAT_GOLDOBTAINED"),
            new ModKeyWord("Stat_BrInventorySlot", false, Color.white).CreateStatus("BR_INVENTORY_SLOT"),
            new ModKeyWord("Stat_CurrentRosion", false, Color.white).CreateStatus("CURRENT_ROSION"),
            new ModKeyWord("Stat_DefenseToAllDamage", false, Color.white).CreateStatus("DEFENSE_TO_ALLDAMAGE"),
            new ModKeyWord("Stat_AllPlayerDefense", true, DefenseColor).CreateStatus("ALL_PLAYER_DEFENSE"),
            new ModKeyWord("Stat_AllPlayerAllDamage", true, Color.white).CreateStatus("ALL_PLAYER_ALL_DAMAGE", "%"),
            new ModKeyWord("Stat_AllPlayerElementalDamage", true, ElementalColor).CreateStatus("ALL_PLAYER_ELEMENTAL_DAMAGE"),
            new ModKeyWord("Stat_AllPlayerCrit", true, CritColor).CreateStatus("ALL_PLAYER_CRIT", "%"),
            new ModKeyWord("Stat_StrikeBack", true, DefenseColor).UseKeyWordSprite("Thorns").CreateStatus("STRIKE_BACK", "%"),
            new ModKeyWord("Stat_WindErosionDamage", false, WindColor).CreateStatus("WIND_EROSION_DAMAGE", "%"),
            new ModKeyWord("Stat_WindErosionMult", false, WindColor).CreateStatus("WIND_EROSION_MULT", "%"),
            new ModKeyWord("Stat_WindErosionCount", false, WindColor).CreateStatus("WIND_EROSION_COUNT"),
            new ModKeyWord("Stat_WindErosionLuck", false, WindColor).CreateStatus("WIND_EROSION_LUCK", "%"),
            new ModKeyWord("Stat_WindErosionDot", false, WindColor).CreateStatus("WIND_EROSION_DOT"),
            new ModKeyWord("Stat_WindErosionOverFlow", false, WindColor).CreateStatus("WIND_EROSION_OVER_FLOW"),
            new ModKeyWord("Stat_DrinkPotionStack", true, ElementalColor).CreateStatus("DRINK_POTION_STACK"),

            new ModKeyWord("ModWeaponEffect_GreatswordOfExorcism", true, IceColor),
            new ModKeyWord("ModWeaponEffect_LegacyoftheIllusionist", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_EternalFrostGreatsword", true,  WeaponColor),
            new ModKeyWord("ModWeaponEffect_IcicleSword", true, IceColor),
            new ModKeyWord("ModWeaponEffect_CrimsonVortex", true, WoundColor),
            new ModKeyWord("ModWeaponEffect_SpikeClub", true, CritColor),
            new ModKeyWord("ModWeaponEffect_LightningGreatSwordE3G", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_LightningGreatSwordS3G", true, LightningColor),
            new ModKeyWord("ModWeaponEffect_Minimalism", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_Adonis", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_PurpleJacket", true, PoisonColor),
            new ModKeyWord("ModWeaponEffect_Heidi", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_Heidi2", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_BlazingHelbanus", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_YuisShortDagger", true, MagicColor),
            new ModKeyWord("ModWeaponEffect_YuisShortDagger2", true, MagicColor),
            new ModKeyWord("ModWeaponEffect_AdamahOath", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_BeastMarksSwordHou", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_WinterSmithsDraftblade", true, IceColor),
            new ModKeyWord("ModWeaponEffect_SilenceofEverfrost", true, IceColor),
            new ModKeyWord("ModWeaponEffect_SilenceofEverfrost2", true, IceColor),
            new ModKeyWord("ModWeaponEffect_FiaolasJudgmentStaff", true, FireColor),
            new ModKeyWord("ModWeaponEffect_FiaolasJudgmentStaff2", true, FireColor),
            new ModKeyWord("ModWeaponEffect_BreakplateBlade", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_PristineForm", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_LightlessBlade", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_FrozenSmelt", true, IceColor),
            new ModKeyWord("ModWeaponEffect_TheInquisitor", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_LightningsWrath", true, LightningColor),
            new ModKeyWord("ModWeaponEffect_LightningsWrath2", true, LightningColor),
            new ModKeyWord("ModWeaponEffect_GlacialBlade", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_ScaleOfTheAstralSea", true, ChaosColor),
            new ModKeyWord("ModWeaponEffect_BountyHunter", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_CarrotSword", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_GardenofNeedleIce", true, MagicColor),
            new ModKeyWord("ModWeaponEffect_BlindingSilence", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_StoneSword", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_SolisImber", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_Drífa", true, IceColor),
            new ModKeyWord("ModWeaponEffect_Minigun", true, LightningColor),
            new ModKeyWord("ModWeaponEffect_MarkStabilization", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_Negligence", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_Moment", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_AirValve", true, WeaponColor),
            new ModKeyWord("ModWeaponEffect_Vortex", true, WindColor),
            new ModKeyWord("ModWeaponEffect_Tornado", true, WeaponColor),

            new ModKeyWord("Stealth", true, WeaponColor),
            new ModKeyWord("WindDamage", false, WindColor).StringToSprite("Wind").CreateStatus("WIND_DAMAGE").SetDetailed("Status_EachElementalDamage_DetailedValue"),
            new ModKeyWord("WindDamageAmp", false, WindColor).StringToSprite("Wind").CreateStatusAmp("WIND_DAMAGE_AMP", "WindDamage", "%"),
            new ModKeyWord($"Elemental_{Wind}", false, WindColor).StringToSprite("Wind"),
            new ModKeyWord("WindErosion", true, WindColor).StringToSprite("Wind"),
        ];
        //模组伤害来源
        public static List<ModDamageKey> ModDamageKeyEntityData =
        [
            //神器
            new ModDamageKey("UnhallowedRitePaper", DamageIdEntity.ECategory.Charm),
            new ModDamageKey("Caliburn", DamageIdEntity.ECategory.Charm),
            new ModDamageKey("Excalibur", DamageIdEntity.ECategory.Charm),
            new ModDamageKey("Convergence", DamageIdEntity.ECategory.Charm),
            new ModDamageKey("CyanOrb", DamageIdEntity.ECategory.Charm),
            new ModDamageKey("ShadowWarrior", DamageIdEntity.ECategory.Charm),
            new ModDamageKey("WhiteEggshell", DamageIdEntity.ECategory.Charm),
            new ModDamageKey("AmplifierComponent", DamageIdEntity.ECategory.Charm),
            new ModDamageKey("SecretCloak", DamageIdEntity.ECategory.Charm),
            new ModDamageKey("SpearofZeus", DamageIdEntity.ECategory.Charm),
            new ModDamageKey("Caltrop", DamageIdEntity.ECategory.Charm),
            new ModDamageKey("Capricious", DamageIdEntity.ECategory.Charm),
            new ModDamageKey("BumpyFate", DamageIdEntity.ECategory.Charm),
            new ModDamageKey("AtmosClock", DamageIdEntity.ECategory.Charm),
            new ModDamageKey("WindSail", DamageIdEntity.ECategory.Charm),
            new ModDamageKey("DevilPact", DamageIdEntity.ECategory.Charm),
            new ModDamageKey("SylphsSoulPowder", DamageIdEntity.ECategory.Charm),
            new ModDamageKey("BottledLightning", DamageIdEntity.ECategory.Charm),
            new ModDamageKey("ElectricIce", DamageIdEntity.ECategory.Charm),
            new ModDamageKey("PlasmaCrystal", DamageIdEntity.ECategory.Charm),

            //武器
            new ModDamageKey("IceCrystal", DamageIdEntity.ECategory.Weapon),
            new ModDamageKey("IceBlade", DamageIdEntity.ECategory.Weapon),
            new ModDamageKey("GlacierSwordQi", DamageIdEntity.ECategory.Weapon),
            new ModDamageKey("CarrotMissile", DamageIdEntity.ECategory.Weapon),
            new ModDamageKey("Thunderhead", DamageIdEntity.ECategory.Weapon),
            new ModDamageKey("FlyingShield", DamageIdEntity.ECategory.Weapon),
            new ModDamageKey("Rend", DamageIdEntity.ECategory.Weapon),
            new ModDamageKey("ShadowThorns", DamageIdEntity.ECategory.Weapon),
            new ModDamageKey("FallingRock", DamageIdEntity.ECategory.Weapon),
            new ModDamageKey("LightningStrike", DamageIdEntity.ECategory.Weapon),
            new ModDamageKey("Vortex", DamageIdEntity.ECategory.Weapon),
            new ModDamageKey("IceSpike", DamageIdEntity.ECategory.Weapon),
            new ModDamageKey("AdditionalWeaponDamageWind", DamageIdEntity.ECategory.Weapon),
            new ModDamageKey("PoisonDetonation", DamageIdEntity.ECategory.Weapon),
            new ModDamageKey("EnchantedBlade", DamageIdEntity.ECategory.Weapon),

            //能力
            new ModDamageKey("KingsPower", DamageIdEntity.ECategory.Ability),
            new ModDamageKey("Counterattack", DamageIdEntity.ECategory.Ability),
            new ModDamageKey("WindErosion", DamageIdEntity.ECategory.Ability),

            //魔法
            new ModDamageKey("GardenofNeedleIce", DamageIdEntity.ECategory.Magic),
            new ModDamageKey("LightPrism", DamageIdEntity.ECategory.Magic),
            new ModDamageKey("VacuumBurst", DamageIdEntity.ECategory.Magic),
            new ModDamageKey("WindBlade", DamageIdEntity.ECategory.Magic),
            new ModDamageKey("WindyWeather", DamageIdEntity.ECategory.Magic),
        ];
        //模组HUD
        public static List<ModEffectHUD> ModEffectHUDData =
        [
            new ModEffectHUD("SpearofZeus", false, false),
            new ModEffectHUD("Rosion", true, true),
            new ModEffectHUD("Charge", true, true),
            new ModEffectHUD("ThunderBeast", false, true),
            new ModEffectHUD("YuisShortDagger", true, true),
        ];
        //模组减益列表
        public static List<ModCharacterDebuff> ModDeBuffData =
        [
            WindErosion,
        ];
        //模组物品
        public static List<int> DisableCharm =
        [
            1012,// 龙骨碎片
            1034,// 盾牌伙伴
            1035,// 云瓶
            1056,// 青铜镜碎片
            1058,// 图书馆分针微型模型
            1067,// 凯尔萨德尼钥匙
            1078,// 人工精灵伊菲尔
            1094,// 琥珀
            1111,// 丢弃的金戒指
            1112,// 绝对戒指
            1118,// 白蛋壳
            1223,// 弩箭袋
            1224,// 装填用木箱
            1287,// 反击手镯
            1301,// 旅人的祝福
            1302,// 炼金术士的烧瓶
            1313,// 褪色的盾徽
            1314,// 铁壁盾徽
            1315,// 生锈的刀格
            1316,// 名匠的刀格
            1317,// 见习决斗家肩章
            1318,// 决斗家肩章
            1323,// 生涩的曲柄
            1324,// 神射手的曲柄
            1326,// 瓦格纳的玻璃瓶
            1328,// 突破罗盘
        ];//隐藏原版物品
        public static Dictionary<int, bool> ModfyisItemUniqueEffect = new()
        {
            { 1002, false },  // 睿智护符
            { 1023, true },   // 枯萎的花
            { 1031, true },   // 恩多迪特的镇纸
            { 1036, false },  // 战士的证明 
            { 1049, true },   // 小太刀八云
            { 1068, true },   // 沙波特果实
            { 1069, true },   // 雷伊星碎片
            { 1126, false },  // 金色枫叶
            { 1132, true },   // 墨水瓶
            { 1140, true },   // 旗帜
            { 1163, true },   // 魔法眼镜
            { 1164, true },   // 小太刀八云
            { 1192, false },  // 荆棘壳
            { 1195, true },   // 万花筒
        };
        public static Dictionary<int, EItemRarity> ModfyisItemRarity = new()
        {
            { 1031, EItemRarity.Uncommon },  // 恩多迪特的书箭
            { 1033, EItemRarity.Uncommon },  // 剑耳环
            { 1063, EItemRarity.Common },    // 小魔法海螺
            { 1085, EItemRarity.Common },    // 蓝环
            { 1132, EItemRarity.Common },    // 墨水瓶
            { 1137, EItemRarity.Uncommon },  // 沃尔斯帕
            { 1139, EItemRarity.Uncommon },  // 冰雪藤
            { 1146, EItemRarity.Rare },      // 如意宝珠
            { 1147, EItemRarity.Rare },      // 彩虹羽毛
            { 1153, EItemRarity.Uncommon },  // 火焰草根
            { 1155, EItemRarity.Rare },      // 四叶草
            { 1157, EItemRarity.Uncommon },  // 兔子头盔
            { 1162, EItemRarity.Common },    // 诱惑的鱼饵
            { 1190, EItemRarity.Uncommon },  // 锅盖
            { 1194, EItemRarity.Common },    // 银手镯
            { 1195, EItemRarity.Legend },    // 万花筒
            { 1240, EItemRarity.Rare },      // 索利斯·德克里
            { 1300, EItemRarity.Uncommon },  // 生气的土豆
            { 1305, EItemRarity.Rare },      // 红蘑菇
        };
        public static Dictionary<int, List<StatusGroup>> ModfyisItemStatusInstance = new()
        {
            { 1010, new List<StatusGroup> { Tools.SetStatusGroup("SPECIAL_ATTACK_DAMAGE", 5, 10) } }, // 盾牌术
            { 1023, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("MAX_MP", 3, 6, 9, 12),
                        Tools.SetStatusGroup("CRITICAL", 250, 500, 750, 1000)
                    }
            }, // 枯萎的花
            { 1031, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("MAX_MP", 2, 4, 6, 8),
                        Tools.SetStatusGroup("MAX_HP", 2, 4, 6, 8),
                        Tools.SetStatusGroup("MP_SKILL_DAMAGE", 3, 6, 9, 12)
                    }
            }, // 恩多迪特的书箭
            { 1033, new List<StatusGroup> { Tools.SetStatusGroup("CRITICAL_DAMAGE_RATE", 8, 15, 22, 30) } }, // 剑耳环
            { 1035, new List<StatusGroup> { Tools.SetStatusGroup("DARK_CLOUD_SPEED", 10, 20, 30, 40) } }, // 云瓶
            { 1036, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("PHYSICAL_DAMAGE", 2, 4, 4, 6, 8),
                        Tools.SetStatusGroup("ATTACK_SPEED", 4, 4, 8, 8, 8)
                    }
            }, // 战士的证明
            { 1049, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("DASH_ATTACK_DAMAGE", 20, 40, 60, 80),
                        Tools.SetStatusGroup("DASH_RECOVERY_SPEED", 5, 10, 15, 20)
                    }
            }, // 小太刀八云
            { 1063, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("ICE_DAMAGE", 3, 6),
                        Tools.SetStatusGroup("FROST_RELIC_DAMAGE", 3, 6)
                    }
            }, // 小魔法海螺
            { 1068, new List<StatusGroup> { Tools.SetStatusGroup("MIN_DARK_CLOUD", 10, 20, 30) } }, // 沙波特果实
            { 1070, new List<StatusGroup> { Tools.SetStatusGroup("DARK_CLOUD_RESTORE_DURING_BATTLE", 50, 100, 150, 200) } }, // 尖尖的橡果
            { 1081, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("PHYSICAL_DAMAGE", 2, 4, 6, 8, 10),
                        Tools.SetStatusGroup("EVASION", 200, 400, 600, 800, 1000)
                    }
            }, // 标本喙
            { 1086, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("BASIC_ATTACK_DAMAGE", 5, 10),
                        Tools.SetStatusGroup("SPECIAL_ATTACK_DAMAGE", 5, 10)
                    }
            }, // 银盘子
            { 1092, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("EVASION", 300, 600, 900),
                        Tools.SetStatusGroup("ATTACK_SPEED", 4, 8, 12)
                    }
            }, // 风车
            { 1113, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("LIGHTNING_DAMAGE", 2, 4, 6, 8, 10),
                        Tools.SetStatusGroup("DARK_CLOUD_DAMAGE", 6, 12, 18, 24, 30),
                        Tools.SetStatusGroup("DARK_CLOUD_SPEED", 12, 24, 36, 48, 60),
                        Tools.SetStatusGroup("MIN_DARK_CLOUD", 6, 12, 18, 24, 30)
                    }
            }, // 避雷针
            { 1120, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("MAX_HP", 10, 20, 30),
                        Tools.SetStatusGroup("HPDAMAGERESTORE", 4, 8, 12)
                    }
            }, // 血石戒指
            { 1121, new List<StatusGroup> { Tools.SetStatusGroup("DEBUFF_DAMAGE", 4, 8, 12, 16) } }, // 中和剂
            { 1126, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("LUCK", 1, 2, 3),
                        Tools.SetStatusGroup("EXP_DROP", 8, 16, 24)
                    }
            }, // 金色枫叶
            { 1147, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("SPECIAL_ATTACK_DAMAGE", 5, 10, 15, 20),
                        Tools.SetStatusGroup("DAGGER_TRANCE_BONUS", 0, 0, 0, 1),
                        Tools.SetStatusGroup("FURY_INVINCIBLE_TIME", 1, 2, 3, 3)
                    }
            }, // 彩虹羽毛
            { 1154, new List<StatusGroup> { Tools.SetStatusGroup("ELITE_DAMAGE", 8, 16, 24, 32) } }, // 剧毒孢子袋
            { 1155, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("LUCK", 3, 6, 9, 12),
                        Tools.SetStatusGroup("EVASION", 300, 600, 900, 1200),
                        Tools.SetStatusGroup("CRITICAL", 300, 600, 900, 1200)
                    }
            }, // 四叶草
            { 1156, new List<StatusGroup> { Tools.SetStatusGroup("FINAL_WEAPONDAMAGE", 4, 8, 12) } }, // 玻璃锤
            { 1157, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("FINAL_WEAPONDAMAGE", 2, 4, 6, 8),
                        Tools.SetStatusGroup("DEFENSE", 4, 6, 8, 10)
                    }
            }, // 兔子头盔
            { 1162, [ Tools.SetStatusGroup("HIGHEST_ELEMENTAL_DAMAGE", 2, 4) ] }, // 诱惑的鱼饵
            { 1163, [ Tools.SetStatusGroup("EVASION", 500, 1000, 1500, 2000) ] }, // 魔法眼镜
            { 1164, [ Tools.SetStatusGroup("MAX_HP", 3, 6, 9, 12), Tools.SetStatusGroup("CRITICAL_DAMAGE_RATE", 5, 10, 15, 20) ] }, // 稻草人偶
            { 1166, [ Tools.SetStatusGroup("MAGIC_CRITICAL", 1000, 2000, 3000) ] }, // 宝石盔甲
            { 1181, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("PHYSICAL_DAMAGE", 2, 4, 6, 8, 10, 12, 14, 16, 18, 20),
                        Tools.SetStatusGroup("PHYSICAL_DAMAGE_AMP", 1, 1, 2, 2, 3, 3, 4, 4, 5, 5)
                    }
            }, // 阿格玛投影剑
            { 1189, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("ATTACK_SPEED", 5, 10, 15),
                        Tools.SetStatusGroup("MOVE_SPEED", 3, 6, 9)
                    }
            }, // 立方体
            { 1192, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("PHYSICAL_DAMAGE", 1, 2, 3, 4),
                        Tools.SetStatusGroup("DEFENSE", 2, 4, 6, 8),
                        Tools.SetStatusGroup("THORNS", 50, 100, 150, 200)
                    }
            }, // 荆棘壳
            { 1193, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("FIRE_DAMAGE", 2, 4, 6),
                        Tools.SetStatusGroup("ATTACK_SPEED", 2, 4, 6)
                    }
            }, // 索利斯·弗拉克斯
            { 1194, new List<StatusGroup> { Tools.SetStatusGroup("ATTACK_SPEED", 3, 6, 9) } }, // 银手镯
            { 1195, new List<StatusGroup> { Tools.SetStatusGroup("FREE_ITEM_REROLL", 1) } }, // 万花筒
            { 1173, new List<StatusGroup> { Tools.SetStatusGroup("CRITICAL", 1000) } }, // 贝鲁特之镰
            { 1210, new List<StatusGroup> { Tools.SetStatusGroup("CHARGING_CHARM_AMPLIFY", 0, 0, 0, 0, 0, 1) }}, // 冰星
            { 1212, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("FREEZE_DAMAGE", 10, 16, 22, 28, 34, 40),
                        Tools.SetStatusGroup("FROSTBITE_DAMAGE", 10, 16, 22, 28, 34, 40)
                    }
            }, // 冰甲虫
            { 1221, new List<StatusGroup> { Tools.SetStatusGroup("SPECIAL_ATTACK_DAMAGE", 10, 20, 30, 40, 50) } }, // 蓝色护腕
            { 1240, new List<StatusGroup> { Tools.SetStatusGroup("FLAME_SWORD_CRITICAL", 8, 15, 22, 29, 36, 43, 50) } }, // 索利斯·德克里
            { 1249, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("CRITICAL", 300, 600, 900),
                        Tools.SetStatusGroup("FOLLOWER_CRITICAL_CONTRIBUTE", 25, 50, 75)
                    }
            }, // 战术指南书
            { 1252, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("EVASION", 300, 600, 900),
                        Tools.SetStatusGroup("CRITICAL", 400, 800, 1200)
                    }
            }, // 常青斗篷
            { 1262, new List<StatusGroup> { Tools.SetStatusGroup("BASIC_ATTACK_DAMAGE", 10, 20, 30, 40, 50) } }, // 神秘摆锤
            { 1274, new List<StatusGroup> { Tools.SetStatusGroup("CRITICAL", 450, 900, 1350, 1800) } }, // 故障探测针
            { 1276, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("DEFENSE", 3, 6, 9, 12, 15),
                        Tools.SetStatusGroup("IGNORE_DEFENSE", 3, 6, 9, 12, 15)
                    }
            }, // 野兽之心
            { 1297, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("FIRE_DAMAGE", 2, 4, 6),
                        Tools.SetStatusGroup("HIGHEST_ELEMENTAL_DAMAGE", 1, 2, 3)
                    }
            }, // 石榴石徽章
            { 1298, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("LIGHTNING_DAMAGE", 2, 4, 6),
                        Tools.SetStatusGroup("HIGHEST_ELEMENTAL_DAMAGE", 1, 2, 3)
                    }
            }, // 黄玉徽章
            { 1299, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("ICE_DAMAGE", 2, 4, 6),
                        Tools.SetStatusGroup("HIGHEST_ELEMENTAL_DAMAGE", 1, 2, 3)
                    }
            }, // 钻石徽章
            { 1300, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("IGNORE_DEFENSE", 2, 4, 6, 8),
                        Tools.SetStatusGroup("LUCK", 2, 4, 6, 8),
                        Tools.SetStatusGroup("MOVE_SPEED", 5, 5, 10, 10)
                    }
            }, // 生气的土豆
           { 1321, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("FIRE_DAMAGE", 2, 4, 6, 8, 10),
                        Tools.SetStatusGroup("FLAME_SWORD_IGNORE_DEFENSE", 3, 6, 9, 12, 15),
                        Tools.SetStatusGroup("FLAME_SWORD_DAMAGE", 6, 12, 18, 24, 30),
                        Tools.SetStatusGroup("CRITICAL_DAMAGE_RATE", 6, 12, 18, 24, 30)
                    }
            }, // 正午磨刀石
        };
        public static Dictionary<string, ItemEntity> GetModItem = [];
        public static List<ModItem> ModItemEntityData =
        [
            new ModItem("BoxingGloves", EItemRarity.Common)
            .CreateCharm(Tools.SetCategories("STURDY"),
            [
                Tools.SetStatusGroup("PHYSICAL_DAMAGE", 2, 4),
                Tools.SetStatusGroup("DEFENSE", 2, 4)
            ], false),//拳击手套

            new ModItem("SwordOfKings", EItemRarity.Legend)
            .CreateCharm(Tools.SetCategories("STURDY"),
            [
                Tools.SetStatusGroup("DIRECT_WEAPON_DAMAGE", 8, 16, 24, 32, 40)
            ]),//王者之剑

            new ModItem("MeteoricIronMedal", EItemRarity.Legend)
            .CustomCreateCharm(Tools.SetCategories("FLAMESWORD"), Action =>
            {
                Charm_FlameSwordAutoAttack Effect = Action.resourcePrefab.AddComponent<Charm_FlameSwordAutoAttack>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_FlameSwordAutoAttack")];
                Effect.stats = [Tools.SetStatusGroup("FLAME_SWORD_MAX", 2, 3, 4, 5, 6)];
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//陨铁勋章

            new ModItem("MidasRing", EItemRarity.Rare)
            .CustomCreateCharm(Tools.SetCategories("SAVVY"), Action =>
            {
                Charm_GoldDamage Effect = Action.resourcePrefab.AddComponent<Charm_GoldDamage>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_GoldDamage")];
                Effect.stats = [Tools.SetStatusGroup("FINAL_DAMAGE", 8, 12, 16)];
                Effect.maxLevel = 2;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//迈达斯的黄金手镯

            new ModItem("TaxTreaty", EItemRarity.Legend)
            .CustomCreateCharm(Tools.SetCategories("SAVVY"), Action =>
            {
                Charm_ReturnGold Effect = Action.resourcePrefab.AddComponent<Charm_ReturnGold>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_ReturnGold")];
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//税收协定

            new ModItem("WindSpriteHairWax", EItemRarity.Legend)
            .CreateCharm(Tools.SetCategories("COMET"),
            [
                Tools.SetStatusGroup("DASH_COUNT", 1, 1, 2, 2, 2),
                Tools.SetStatusGroup("DASH_INVINCIBLE_TIME_BONUS", 0, 0, 0, 0, 100),
                Tools.SetStatusGroup("DASH_RECOVERY_SPEED", 6, 12, 18, 24, 30)
            ]),//风精灵的发蜡

            new ModItem("MasterBracelet", EItemRarity.Rare)
            .CustomCreateCharm(Tools.SetCategories("WEAPON"), Action =>
            {
                Charm_InvincibleDefense Effect = Action.resourcePrefab.AddComponent<Charm_InvincibleDefense>();
                Effect.isWeaponRelatedCharm = true;
                Effect.relatedWeapon = EWeaponType.Staff;
                Effect.effectsString = [new("ModCharmEffect_InvincibleDefense")];
                Effect.maxLevel = 3;
                Effect.stats = new List<StatusGroup>()
                {
                    Tools.SetStatusGroup("BASIC_ATTACK_DAMAGE", 5, 10, 15, 20),
                    Tools.SetStatusGroup("SPECIAL_ATTACK_COST_REDUCTION", 10, 20, 30, 40)
                }.ToArray();
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//大师手镯

            new ModItem("RedDogSymbolA", EItemRarity.Rare).SetCost(1000)
            .CustomCreateCharm(Tools.SetCategories("COMPANION"), Action =>
            {
                Action.aName =  new LocalizedString("Item_WeaselKnight_Name");
                Action.aFlavorText = new LocalizedString("Item_WeaselKnight_FlavorText");
                Action.activeType = EItemActiveType.Hidden;
                Charm_LeadNPC Effect = Action.resourcePrefab.AddComponent<Charm_LeadNPC>();
                Effect.isWeaponRelatedCharm = false;
                Effect.maxLevel = 2;
                int Value = 75;
                Effect.hpByLevel = [Value.IntMult(5f), Value.IntMult(5.5f), Value.IntMult(6f)];
                Value = 135;
                Effect.levelBonusByLevel = [Value.IntMult(5f), Value.IntMult(6f), Value.IntMult(7f)];
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//超级科林

            new ModItem("RedDogSymbolB", EItemRarity.Rare).SetCost(1500)
            .CustomCreateCharm(Tools.SetCategories("COMPANION"), Action =>
            {
                Action.aName =  new LocalizedString("Item_WeaselKnight_Name");
                Action.aFlavorText = new LocalizedString("Item_WeaselKnight_FlavorText");
                Action.activeType = EItemActiveType.Hidden;
                Charm_LeadNPC Effect = Action.resourcePrefab.AddComponent<Charm_LeadNPC>();
                Effect.isWeaponRelatedCharm = false;
                Effect.maxLevel = 2;
                int Value = 75;
                Effect.hpByLevel = [Value.IntMult(10f), Value.IntMult(11f), Value.IntMult(12f)];
                Value = 135;
                Effect.levelBonusByLevel = [Value.IntMult(10f), Value.IntMult(12f), Value.IntMult(14f)];
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//超级科林2

            new ModItem("RedDogSymbolC", EItemRarity.Rare).SetCost(2000)
            .CustomCreateCharm(Tools.SetCategories("COMPANION"), Action =>
            {
                Action.aName =  new LocalizedString("Item_WeaselKnight_Name");
                Action.aFlavorText = new LocalizedString("Item_WeaselKnight_FlavorText");
                Action.activeType = EItemActiveType.Hidden;
                Charm_LeadNPC Effect = Action.resourcePrefab.AddComponent<Charm_LeadNPC>();
                Effect.effectsString = [new("Charm_WeaselKnight_Effect")];
                Effect.isWeaponRelatedCharm = false;
                Effect.maxLevel = 2;
                int Value = 75;
                Effect.hpByLevel = [Value.IntMult(15f), Value.IntMult(16.5f), Value.IntMult(18f)];
                Value = 135;
                Effect.levelBonusByLevel = [Value.IntMult(15f), Value.IntMult(18f), Value.IntMult(21f)];
                Effect.defaultReviveTimer = new Timer(18f);
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//超级科林3

            new ModItem("CurseNail", EItemRarity.Legend)
            .CustomCreateCharm(Tools.SetCategories("CURSE"), Action =>
            {
                Charm_SuperDebuffDamage Effect = Action.resourcePrefab.AddComponent<Charm_SuperDebuffDamage>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_SuperDebuffDamage")];
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//诅咒之钉

            new ModItem("HeartofElements", EItemRarity.Rare)
            .CustomCreateCharm(Tools.SetCategories("ELEMENTAL"), Action =>
            {
                Charm_ElementAllocation Effect = Action.resourcePrefab.AddComponent<Charm_ElementAllocation>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_ElementAllocation")];
                Effect.stats = new List<StatusGroup>()
                {
                    Tools.SetStatusGroup("HIGHEST_ELEMENTAL_DAMAGE", 2, 4, 6)
                }.ToArray();
                Effect.maxLevel = 2;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//元素之心

            new ModItem("ShadowGuardian", EItemRarity.Legend)
            .CustomCreateCharm(Tools.SetCategories("SHADOW"), Action =>
            {
                Charm_DodgeShield Effect = Action.resourcePrefab.AddComponent<Charm_DodgeShield>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_DodgeShield")];
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//暗影守护者

            new ModItem("ScoreSymphony", EItemRarity.Legend)
            .CustomCreateCharm(Tools.SetCategories("WINDSONG"), Action =>
            {
                Charm_ScoreCountAttackSpeed Effect = Action.resourcePrefab.AddComponent<Charm_ScoreCountAttackSpeed>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_ScoreCountAttackSpeed")];
                Effect.maxLevel = 2;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//乐谱"交响曲"

            new ModItem("PureWhiteCore", EItemRarity.Legend)
            .CustomCreateCharm(Tools.SetCategories("LAKE"), Action =>
            {
                Charm_CustomStatus Effect = Action.resourcePrefab.AddComponent<Charm_CustomStatus>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_MPRegenDamage")];
                Effect.ValueByLevel = [3, 2, 1];
                Effect.ValueName = TextFile.MPRegenToDamage;
                Effect.stats = new List<StatusGroup>()
                {
                    Tools.SetStatusGroup("MP_SKILL_DAMAGE", 5)
                }.ToArray();
                Effect.maxLevel = 2;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//纯白核心

            new ModItem("SolarProminence").SetDual()
            .CustomCreateCharm(Tools.SetCategories("EMBER", "FLAMESWORD"), Action =>
            {
                Charm_BurnFlameSword Effect = Action.resourcePrefab.AddComponent<Charm_BurnFlameSword>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_BurnFlameSword")];
                Effect.stats = new List<StatusGroup>()
                {
                    Tools.SetStatusGroup("FIRE_DAMAGE", 2, 4, 6, 8, 10),
                    Tools.SetStatusGroup("BURN_SPEED", 10, 20, 30, 40, 50)
                }.ToArray();
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//日珥

            new ModItem("GrowthFund").SetDual()
            .CustomCreateCharm(Tools.SetCategories("FORTUNE", "SAVVY"), Action =>
            {
                Charm_CustomStatus Effect = Action.resourcePrefab.AddComponent<Charm_CustomStatus>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_CoinToExp")];
                Effect.ValueByLevel = [1];
                Effect.ValueName = TextFile.CoinToExp;
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//成长基金

            new ModItem("BumpyFate").SetDual()
            .CustomCreateCharm(Tools.SetCategories("GUARDIAN", "SHADOW"), Action =>
            {
                Charm_DodgeThorns Effect = Action.resourcePrefab.AddComponent<Charm_DodgeThorns>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_DodgeThorns")];
                Effect.stats = new List<StatusGroup>()
                {
                    Tools.SetStatusGroup("THORNS", 50, 100, 150, 200),
                    Tools.SetStatusGroup("STRIKE_BACK", 50, 100, 150, 200)
                }.ToArray();
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//命运坎坷

            new ModItem("FrostyAnvil").SetDual()
            .CustomCreateCharm(Tools.SetCategories("GLACIER", "FROST"), Action =>
            {
                Charm_FrozenFrostRelic Effect = Action.resourcePrefab.AddComponent<Charm_FrozenFrostRelic>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_FrozenFrostRelic")];
                Effect.Ending = "%";
                Effect.ValueName = TextFile.FrostRelicAddMult;
                Effect.ValueByLevel = [30, 60, 90, 120, 150, 180, 210, 240];
                Effect.maxLevel = 7;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//寒霜铁砧

            new ModItem("AmplifierComponent").SetDual()
            .CustomCreateCharm(Tools.SetCategories("PRECISION", "MYSTIC"), Action =>
            {
                Charm_IgnoreDefensiveCrit Effect = Action.resourcePrefab.AddComponent<Charm_IgnoreDefensiveCrit>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_IgnoreDefensiveCrit")];
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//增幅器组件

            new ModItem("HolyGuardian", EItemRarity.Common)
            .CreateCharm(Tools.SetCategories("PARTY"),
            [
                Tools.SetStatusGroup("ALL_PLAYER_DEFENSE", 2, 4),
            ], false),//神圣守护

            new ModItem("Support", EItemRarity.Uncommon)
            .CreateCharm(Tools.SetCategories("PARTY"),
            [
                Tools.SetStatusGroup("ALL_PLAYER_ELEMENTAL_DAMAGE", 1, 2, 3, 4),
            ], false),//支援

            new ModItem("NaptimePillow", EItemRarity.Uncommon)
            .CreateCharm(Tools.SetCategories("PARTY"),
            [
                Tools.SetStatusGroup("MAX_HP", 10, 20, 30, 40),
                Tools.SetStatusGroup("REVIVE_PLAYER_HASTE", 20)
            ]),//爱心睡眠抱枕

            new ModItem("WeaknessIndicator", EItemRarity.Rare)
            .CreateCharm(Tools.SetCategories("PARTY"),
            [
                Tools.SetStatusGroup("ALL_PLAYER_CRIT", 3, 6, 9, 12, 15),
            ]),//弱点标记

            new ModItem("UnityFlag", EItemRarity.Legend)
            .CustomCreateCharm(Tools.SetCategories("PARTY"), Action =>
            {
                Charm_MultiPlayerAddStatus Effect = Action.resourcePrefab.AddComponent<Charm_MultiPlayerAddStatus>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_MultiPlayerAddStatus")];
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//团结旗帜

            new ModItem("ArtificialSpiritIfiel", EItemRarity.Rare)
            .CustomCreateCharm([], Action =>
            {
                Charm_ShootBack Effect = Action.resourcePrefab.AddComponent<Charm_ShootBack>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_ShootBack")];
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//人工精灵伊菲尔

            new ModItem("UnhallowedRitePaper", EItemRarity.Uncommon)
            .CustomCreateCharm(Tools.SetCategories("COMPANION"), Action =>
            {
                Charm_BodyBomb Effect = Action.resourcePrefab.AddComponent<Charm_BodyBomb>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_BodyBomb")];
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//邪祭咒纸

            new ModItem("DevilPact", EItemRarity.Legend)
            .CustomCreateCharm(Tools.SetCategories("COMPANION"), Action =>
            {
                Charm_SummonUnit Effect = Action.resourcePrefab.AddComponent<Charm_SummonUnit>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_DevilPact")];
                Effect.followerDamageId = "[Star's Sephiria Mod]DevilPact";
                Effect.damageByLevel = [64, 128, 192, 256, 320];
                Effect.hpByLevel = [40, 80, 120, 160, 200];
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            })
            .SetCustomItemEffect(Action =>
            {
                Charm_SummonUnit Effect = Action.resourcePrefab.GetComponent<Charm_SummonUnit>();
                Effect.unitPrefab = UnitDatabase.GetSpawnEntity("SamuraiDemon").spawnTargets[0];
                UnitAvatar component = Effect.unitPrefab.GetComponent<UnitAvatar>();
                Effect.unitName = new LocalizedString(component.defaultNameKey);
                component.miniFaceChipSprite = Tools.LoadModSprite("Item_DevilPact_MiniIcon");
            }),//恶魔契约

            new ModItem("BottledLightning").SetDual()
            .CustomCreateCharm(Tools.SetCategories("MAGITECH", "DARKCLOUD"), Action =>
            {
                Charm_ElectricDarkCloud Effect = Action.resourcePrefab.AddComponent<Charm_ElectricDarkCloud>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_ElectricDarkClouds")];
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//瓶装闪电

            new ModItem("BluePointer", EItemRarity.Legend)
            .CustomCreateCharm([], Action =>
            {
                Charm_CharmStatusInheritance Effect = Action.resourcePrefab.AddComponent<Charm_CharmStatusInheritance>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_CharmStatusInheritance")];
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//指北的蓝色针

            new ModItem("EnhancedCompass", EItemRarity.Rare)
            .CustomSet(Set => Set.IsWitchItem = true)
            .CustomCreateCharm([], Action =>
            {
                Charm_EnhancedCompass Effect = Action.resourcePrefab.AddComponent<Charm_EnhancedCompass>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [ new("ModCharmEffect_EnhancedCompass") ];
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//强化罗盘

            new ModItem("SolisThunder").SetDual()
            .CustomCreateCharm(Tools.SetCategories("FLAMESWORD", "DARKCLOUD"), Action =>
            {
                Charm_ThunderFireSword Effect = Action.resourcePrefab.AddComponent<Charm_ThunderFireSword>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_ThunderFireSword")];
                Effect.stats = new List<StatusGroup>()
                {
                    Tools.SetStatusGroup("LIGHTNING_DAMAGE", 2, 4, 6, 8),
                    Tools.SetStatusGroup("FLAME_SWORD_DAMAGE", 5, 10, 15, 20)
                }.ToArray();
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//索利斯·桑德

            new ModItem("SpearofZeus").SetDual()
            .CustomCreateCharm(Tools.SetCategories("FROST", "DARKCLOUD"), Action =>
            {
                Charm_SpearofZeus Effect = Action.resourcePrefab.AddComponent<Charm_SpearofZeus>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_SpearofZeus")];
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;

                ChargingCharm Charging = Action.resourcePrefab.AddComponent<ChargingCharm>();
                Charging.defaultChargeTimer = 6f;
                Charging.enabled = false;
                Charging.hudId = "SpearofZeus";
                Effect.chargingCharm = Charging;

            })
            .SetCustomItemEffect(Action =>
            {
                ItemEntity Item = ItemDatabase.FindItemById(1014);
                ChargingCharm OrigEffect = Item.resourcePrefab.GetComponent<ChargingCharm>();
                ChargingCharm Effect = Action.resourcePrefab.GetComponent<ChargingCharm>();
                Effect.chargeCompleteFxPrefab = OrigEffect.chargeCompleteFxPrefab;
            }),//宙斯之矛

            new ModItem("SecretCloak", EItemRarity.Legend)
            .CustomCreateCharm(Tools.SetCategories("SHADOW"), Action =>
            {
                Charm_SilentStrike Effect = Action.resourcePrefab.AddComponent<Charm_SilentStrike>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_SilentStrike")];
                Effect.stats = new List<StatusGroup>()
                {
                    Tools.SetStatusGroup("EVASION", 300, 600, 900, 1200, 1500)
                }.ToArray();
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//隐秘斗篷

            new ModItem("Caliburn", EItemRarity.Legend)
            .CustomCreateCharm(Tools.SetCategories("LAKE"), Action =>
            {
                Charm_Caliburn Effect = Action.resourcePrefab.AddComponent<Charm_Caliburn>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_Caliburn")];
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//石中剑

            new ModItem("Excalibur", EItemRarity.Legend)
            .CustomCreateCharm(Tools.SetCategories("LAKE"), Action =>
            {
                Action.activeType = EItemActiveType.Hidden;
                Charm_Excalibur Effect = Action.resourcePrefab.AddComponent<Charm_Excalibur>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_Excalibur")];
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//湖中剑

            new ModItem("LuckyCoin").SetDual()
            .CustomCreateCharm(Tools.SetCategories("FORTUNE", "PRECISION"), Action =>
            {
                Charm_LuckCrit Effect = Action.resourcePrefab.AddComponent<Charm_LuckCrit>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_LuckCrit")];
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//幸运硬币

            new ModItem("AlchemistsFlask", EItemRarity.Rare)
            .CustomCreateCharm(Tools.SetCategories("ALCHEMY"), Action =>
            {
                Charm_Alchemy Effect = Action.resourcePrefab.AddComponent<Charm_Alchemy>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_Alchemy")];
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//炼金术士的烧瓶

            new ModItem("PhilosopherStone", EItemRarity.Legend)
            .CustomCreateCharm(Tools.SetCategories("ALCHEMY"), Action =>
            {
                Charm_Sage Effect = Action.resourcePrefab.AddComponent<Charm_Sage>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_Sage")];
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//贤者之石

            new ModItem("DeepSpaceObserver", EItemRarity.Legend)
            .CustomCreateCharm(Tools.SetCategories("PLANET"), Action =>
            {
                Charm_Telescope Effect = Action.resourcePrefab.AddComponent<Charm_Telescope>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_Telescope")];
                Effect.maxLevel = 1;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//深空观测者

            new ModItem("KeelFrgment", EItemRarity.Uncommon)
            .CreateCharm(Tools.SetCategories("MYSTIC"),
            [
                Tools.SetStatusGroup("MAX_HP", 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30),
                Tools.SetStatusGroup("FINAL_DAMAGE", 3, 6, 9, 12, 15, 18, 21, 24, 27, 30, 33, 36, 39, 42, 45)
            ]),//龙骨碎片

            new ModItem("Amber", EItemRarity.Uncommon)
            .CreateCharm(Tools.SetCategories("MYSTIC"),
            [
                Tools.SetStatusGroup("DEFENSE", 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30),
                Tools.SetStatusGroup("TRUE_DAMAGE", 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8)
            ]),//琥珀

            new ModItem("WhiteEggShell", EItemRarity.Rare)
            .CustomCreateCharm(Tools.SetCategories("MYSTIC"), Action =>
            {
                Charm_IgnoreDefensiveDamage Effect = Action.resourcePrefab.AddComponent<Charm_IgnoreDefensiveDamage>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_IgnoreDefensiveDamage")];
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//白蛋壳

            new ModItem("LightningBottle", EItemRarity.Uncommon)
            .CustomCreateCharm(Tools.SetCategories("DARKCLOUD"), Action =>
            {
                Charm_EXLightningBottle Effect = Action.resourcePrefab.AddComponent<Charm_EXLightningBottle>();
                Effect.isWeaponRelatedCharm = false;
                Effect.maxLevel = 3;
                Effect.stats = [Tools.SetStatusGroup("LIGHTNING_DAMAGE", 1, 2, 3, 4)];
                Effect.cloudByLevel = [1, 2, 3, 4];
                Effect.cooldownByLevel = [1f];
                Effect.effectsString = [new("Charm_LightningPouch_Effect"), new("ModCharmEffect_LightningPouch")];
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = false;
                Effect.enabled = false;
            }),//云瓶

            new ModItem("Valiya", EItemRarity.Rare)
            .CustomCreateCharm(Tools.SetCategories("WEAPON"), Action =>
            {
                Charm_InvincinleSpiceialAttack Effect = Action.resourcePrefab.AddComponent<Charm_InvincinleSpiceialAttack>();
                Effect.isWeaponRelatedCharm = true;
                Effect.relatedWeapon = EWeaponType.GreatSword;
                Effect.effectsString =
                [
                    new("Charm_SweepRange_Enhanced_Effect2"),
                    new("Charm_SweepRange_Enhanced_Effect"),
                    new("ModCharmEffect_InvincinleSpecialAttack")
                ];
                Effect.sweepRangeByLevel = [20, 40, 60];
                Effect.sweepDamageByLevel = [10, 20, 30];
                Effect.isGreatswordEquippedForFx = true;
                Effect.hasSweepQuest = false;
                Effect.maxLevel = 2;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            })
            .SetCustomItemEffect(Action =>
            {
                ItemEntity Item = ItemDatabase.FindItemById(1112);
                Charm_SweepRange OrigEffect = Item.resourcePrefab.GetComponent<Charm_SweepRange>();
                Charm_InvincinleSpiceialAttack Effect = Action.resourcePrefab.GetComponent<Charm_InvincinleSpiceialAttack>();
                Effect.weaponFxPrefab = OrigEffect.weaponFxPrefab;
            }),//绝对戒指

            new ModItem("ShieldCrest", EItemRarity.Rare)
            .CustomCreateCharm(Tools.SetCategories("WEAPON"), Action =>
            {
                Charm_AddPerfectGuardTime Effect = Action.resourcePrefab.AddComponent<Charm_AddPerfectGuardTime>();
                Effect.isWeaponRelatedCharm = true;
                Effect.relatedWeapon = EWeaponType.SwordAndShield;
                Effect.effectsString =
                [
                    new("Charm_SwordShieldGrowth_Enhanced_Effect"),
                    new("ModCharmEffect_AddPerfectGuardTime")
                ];
                Effect.guardCostReductionByLevel = [5, 10, 15, 20];
                Effect.guardRangeBonusByLevel = [];
                Effect.stats =
                [
                    Tools.SetStatusGroup("SPECIAL_ATTACK_DAMAGE", 10, 20, 30, 40),
                    Tools.SetStatusGroup("SPECIAL_ATTACK_COST_REDUCTION", 20, 40, 60, 80),
                ];
                Effect.hasGrowthQuest = false;
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            })
            .SetCustomItemEffect(Action =>
            {
                ItemEntity Item = ItemDatabase.FindItemById(1314);
                Charm_GrowthGuard OrigEffect = Item.resourcePrefab.GetComponent<Charm_GrowthGuard>();
                Charm_AddPerfectGuardTime Effect = Action.resourcePrefab.GetComponent<Charm_AddPerfectGuardTime>();
                Effect.equipFxPrefab = OrigEffect.equipFxPrefab;
            }),//铁壁盾徽

             new ModItem("Epaulette", EItemRarity.Uncommon)
            .CustomCreateCharm(Tools.SetCategories("STURDY"), Action =>
            {
                Charm_GrowthParry Effect = Action.resourcePrefab.AddComponent<Charm_GrowthParry>();
                Effect.isWeaponRelatedCharm = true;
                Effect.relatedWeapon = EWeaponType.Dagger;
                Effect.effectsString = [new("Charm_DaggerGrowth_Enhanced_Effect")];
                Effect.parryDamageByLevel = [100, 200, 300];
                Effect.hasGrowthQuest = false;
                Effect.hasExtraTriggers = true;
                Effect.maxLevel = 2;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            })
            .SetCustomItemEffect(Action =>
            {
                ItemEntity Item = ItemDatabase.FindItemById(1318);
                Charm_GrowthParry OrigEffect = Item.resourcePrefab.GetComponent<Charm_GrowthParry>();
                Charm_GrowthParry Effect = Action.resourcePrefab.GetComponent<Charm_GrowthParry>();
                Effect.equipFxPrefab = OrigEffect.equipFxPrefab;
                Effect.bulletPrefab = OrigEffect.bulletPrefab;
            }),//决斗家肩章

            new ModItem("KatanaClash", EItemRarity.Rare)
            .CustomCreateCharm(Tools.SetCategories("WEAPON"), Action =>
            {
                Charm_GrowthKatana Effect = Action.resourcePrefab.AddComponent<Charm_GrowthKatana>();
                Effect.isWeaponRelatedCharm = true;
                Effect.relatedWeapon = EWeaponType.Katana;
                Effect.effectsString = [new("Charm_KatanaGrowth_Enhanced_Effect")];
                Effect.sheathInvincibleBonusPercentByLevel = [12, 24, 36, 48, 60];
                Effect.stats =
                [
                    Tools.SetStatusGroup("BASIC_ATTACK_DAMAGE", 4, 8, 12, 16, 20),
                    Tools.SetStatusGroup("SPECIAL_ATTACK_DAMAGE", 4, 8, 12, 16, 20),
                    Tools.SetStatusGroup("ATTACK_SPEED", 4, 8, 12, 16, 20),
                ];
                Effect.hasGrowthQuest = false;
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            })
            .SetCustomItemEffect(Action =>
            {
                ItemEntity Item = ItemDatabase.FindItemById(1316);
                Charm_GrowthKatana OrigEffect = Item.resourcePrefab.GetComponent<Charm_GrowthKatana>();
                Charm_GrowthKatana Effect = Action.resourcePrefab.GetComponent<Charm_GrowthKatana>();
                Effect.equipFxPrefab = OrigEffect.equipFxPrefab;
            }),//名匠的刀格

            new ModItem("BoltHead", EItemRarity.Rare)
            .CustomCreateCharm(Tools.SetCategories("WEAPON"), Action =>
            {
                Charm_InvincinleForReLoad Effect = Action.resourcePrefab.AddComponent<Charm_InvincinleForReLoad>();
                Effect.isWeaponRelatedCharm = true;
                Effect.relatedWeapon = EWeaponType.Crossbow;
                Effect.effectsString = [new("ModCharmEffect_InvincinleForReLoad")];
                Effect.stats =
                [
                    Tools.SetStatusGroup("BASIC_ATTACK_DAMAGE", 2, 4, 6, 8, 8, 10, 12, 12),
                    Tools.SetStatusGroup("CROSSBOW_RELOADSPEED", 0, 20, 40, 60, 60, 80, 100, 100),
                    Tools.SetStatusGroup("CROSSBOW_ADDITIONAL_MAGAZINE", 0, 0, 0, 0, 1, 1, 1, 2)
                ];
                Effect.hasGrowthQuest = false;
                Effect.hasReloadBuff = false;
                Effect.maxLevel = 7;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            })
            .SetCustomItemEffect(Action =>
            {
                ItemEntity Item = ItemDatabase.FindItemById(1324);
                Charm_GrowthCrossbow OrigEffect = Item.resourcePrefab.GetComponent<Charm_GrowthCrossbow>();
                Charm_InvincinleForReLoad Effect = Action.resourcePrefab.GetComponent<Charm_InvincinleForReLoad>();
                Effect.equipFxPrefab = OrigEffect.equipFxPrefab;
            }),//神射手的曲柄

            new ModItem("EcologicalSphere").SetDual()
            .CustomCreateCharm(Tools.SetCategories("PLANET", "LAKE"), Action =>
            {
                Charm_CustomStatus Effect = Action.resourcePrefab.AddComponent<Charm_CustomStatus>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_EcologicalSphere")];
                Effect.ValueByLevel = [15, 20, 25, 30];
                Effect.ValueName = TextFile.EcologicalSphereEffect;
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//生态球

            new ModItem("CometaryHydrogenCorona").SetDual()
            .CustomCreateCharm(Tools.SetCategories("COMET", "GUARDIAN"), Action =>
            {
                Charm_DashShield Effect = Action.resourcePrefab.AddComponent<Charm_DashShield>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_DashShield")];
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//氢冕

            new ModItem("CounterAttackBlade", EItemRarity.Uncommon)
            .CreateCharm(Tools.SetCategories("SHADOW"),
            [
                Tools.SetStatusGroup("PHYSICAL_DAMAGE", 1, 2, 3, 4),
                Tools.SetStatusGroup("EVASION", 200, 400, 600, 800),
                Tools.SetStatusGroup("STRIKE_BACK", 50, 100, 150, 200)
            ], false),//反击之刃

            new ModItem("ShadowWarrior").SetDual()
            .CustomCreateCharm(Tools.SetCategories("STURDY", "SHADOW"), Action =>
            {
                Charm_ActivelyRetaliate Effect = Action.resourcePrefab.AddComponent<Charm_ActivelyRetaliate>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_ActivelyRetaliate")];
                Effect.stats =
                [
                    Tools.SetStatusGroup("STRIKE_BACK", 50, 100, 150, 200)
                ];
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//影武者

            new ModItem("ChaosGlyph", EItemRarity.Uncommon)
            .CustomCreateCharm(Tools.SetCategories("ACADEMY"), Action =>
            {
                Charm_CharosMagic Effect = Action.resourcePrefab.AddComponent<Charm_CharosMagic>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_CharosMagic")];
                Effect.stats =
                [
                    Tools.SetStatusGroup("MAGIC_DAMAGE_BONUS", 5, 10)
                ];
                Effect.maxLevel = 1;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//混沌符文

            new ModItem("ArcaneBookMark", EItemRarity.Rare)
            .CustomCreateCharm(Tools.SetCategories("ACADEMY"), Action =>
            {
                Charm_MagicBookAddDamage Effect = Action.resourcePrefab.AddComponent<Charm_MagicBookAddDamage>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_MagicBookAddDamage")];
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//奥术书签

            new ModItem("BondVessel", EItemRarity.Rare)
            .CustomSet(Set => Set.IsWitchItem = true)
            .CustomCreateCharm([], Action =>
            {
                Charm_BondVessel Effect = Action.resourcePrefab.AddComponent<Charm_BondVessel>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_BondVessel")];
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = false;
                Effect.enabled = false;
            }),//羁绊容器

            new ModItem("ElectricIce").SetDual()
            .CustomCreateCharm(Tools.SetCategories("MAGITECH", "GLACIER"), Action =>
            {
                Charm_CustomStatus Effect = Action.resourcePrefab.AddComponent<Charm_CustomStatus>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_ElectricIce")];
                Effect.ValueByLevel = [1];
                Effect.ValueName = TextFile.ElectricIce;
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//电冰体

            new ModItem("EnergyRecoveryDevice", EItemRarity.Uncommon)
            .CustomCreateCharm([], Action =>
            {
                Charm_RemoveArtifact Effect = Action.resourcePrefab.AddComponent<Charm_RemoveArtifact>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_RemoveArtifact")];
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//能量回收装置

            new ModItem("CommemorativeCoin", EItemRarity.Uncommon)
            .CustomCreateCharm(Tools.SetCategories("SAVVY"), Action =>
            {
                Charm_ChangeMoney Effect = Action.resourcePrefab.AddComponent<Charm_ChangeMoney>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_CommemorativeCoin")];
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//纪念币

            new ModItem("PotionTransactionProof").SetDual()
            .CustomCreateCharm(Tools.SetCategories("SAVVY", "ALCHEMY"), Action =>
            {
                Charm_PotionTransactionProof Effect = Action.resourcePrefab.AddComponent<Charm_PotionTransactionProof>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_PotionTransactionProof")];
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//药水交易凭证

            new ModItem("CounterAttackTacticsGuide").SetDual()
            .CustomCreateCharm(Tools.SetCategories("COMPANION", "SHADOW"), Action =>
            {
                Charm_CustomStatus Effect = Action.resourcePrefab.AddComponent<Charm_CustomStatus>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_CounterAttackTacticsGuide")];
                Effect.ValueByLevel = [12, 24, 36, 48];
                Effect.ValueName = TextFile.CompanionStrikeBack;
                Effect.Ending = "%";
                Effect.stats = new List<StatusGroup>()
                {
                    Tools.SetStatusGroup("STRIKE_BACK", 50, 100, 150, 200)
                }.ToArray();
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//反击战术指南

            new ModItem("MysteryBox", EItemRarity.Common)
            .CustomCreateCharm(Tools.SetCategories("FORTUNE"), Action =>
            {
                Charm_Lottery Effect = Action.resourcePrefab.AddComponent<Charm_Lottery>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_Lottery")];
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//神秘盒子

            new ModItem("MysteryBox_Random", EItemRarity.Uncommon)
            .CustomCreateCharm([], Action =>
            {
                Action.activeType = EItemActiveType.Hidden;
                Charm_Lottery Effect = Action.resourcePrefab.AddComponent<Charm_Lottery>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_Lottery_Random")];
                Effect.LuckMult = 0.75f;
                Effect.RandomLottery = true;
                Effect.AddStatus =
                [
                    Tools.SetStatusGroup("FINAL_DAMAGE", 1, 2, 3, 4),
                    Tools.SetStatusGroup("LUCK", 0, 1, 2, 3),
                    Tools.SetStatusGroup("EXP_DROP", 6, 12, 18, 24),
                ];
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = false;
                Effect.enabled = false;
            }),//神秘盒子(金)

            new ModItem("Caltrop", EItemRarity.Rare)
            .CustomCreateCharm(Tools.SetCategories("GUARDIAN"), Action =>
            {
                Charm_WeaponThorns Effect = Action.resourcePrefab.AddComponent<Charm_WeaponThorns>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_WeaponThorns")];
                Effect.stats = new List<StatusGroup>()
                {
                    Tools.SetStatusGroup("THORNS", 50, 100, 150, 200),
                }.ToArray();
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//铁蒺藜

            new ModItem("Capricious", EItemRarity.Common)
            .CustomCreateCharm(Tools.SetCategories("HURRICANE"), Action =>
            {
                Charm_ModFrostiumRing Effect = Action.resourcePrefab.AddComponent<Charm_ModFrostiumRing>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("Charm_FrostiumRing_Effect")];
                Effect.damageId = "[Star's Sephiria Mod]Capricious";
                Effect.ElementalName = "WindDamage";
                Effect.maxLevel = 1;
                Effect.elementalType = Wind;
                Effect.damageByLevel = [15, 30];
                Effect.cooldownTimer.time = 0.2f;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//变幻无常

            new ModItem("StrongWindAmulet", EItemRarity.Common)
            .CreateCharm(Tools.SetCategories("HURRICANE"),
            [
                Tools.SetStatusGroup("WIND_DAMAGE", 2, 4, 6, 8),
            ], false),//疾风护符

            new ModItem("CyanOrb", EItemRarity.Uncommon)
            .CustomCreateCharm(Tools.SetCategories("HURRICANE"), Action =>
            {
                Charm_CyanOrb Effect = Action.resourcePrefab.AddComponent<Charm_CyanOrb>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_CyanOrb")];
                Effect.maxLevel = 2;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//青色宝珠

            new ModItem("WindVane", EItemRarity.Uncommon)
            .CustomCreateCharm(Tools.SetCategories("HURRICANE"), Action =>
            {
                Charm_Burn Effect = Action.resourcePrefab.AddComponent<Charm_Burn>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_WindVane")];
                Effect.stats = [Tools.SetStatusGroup("ATTACK_SPEED", 4, 8, 12)];
                Effect.targetElementalType = Wind;
                Effect.addCriticalDamageByLevel = [11, 22, 33];
                Effect.maxLevel = 2;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//风向标

            new ModItem("AtmosClock", EItemRarity.Uncommon)
            .CustomCreateCharm(Tools.SetCategories("HURRICANE"), Action =>
            {
                Charm_HarmfulValue Effect = Action.resourcePrefab.AddComponent<Charm_HarmfulValue>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_AtmosClock")];
                Effect.StatusName = "WIND_EROSION_DOT";
                Effect.Ending = "%";
                Effect.BaseValueByLevel = [60, 120, 180];
                Effect.stats = [Tools.SetStatusGroup("WIND_DAMAGE", 2, 4, 6)];
                Effect.maxLevel = 2;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//空气钟

            new ModItem("WindJellyFish", EItemRarity.Uncommon)
            .CustomCreateCharm(Tools.SetCategories("HURRICANE", "COMPANION"), Action =>
            {
                Charm_SummonUnit Effect = Action.resourcePrefab.AddComponent<Charm_SummonUnit>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_WindJelly")];
                Effect.isJellyfish = true;
                Effect.followerDamageId = "[Star's Sephiria Mod]SylphsSoulPowder";
                Effect.damageByLevel = [15, 30, 45, 60, 75];
                Effect.hpByLevel = [10, 20, 30, 40, 50];
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            })
            .SetCustomItemEffect(Action =>
            {
                Charm_SummonUnit Charm = Action.resourcePrefab.GetComponent<Charm_SummonUnit>();
                GameObject Vanilla = ItemDatabase.FindItemById(1198).resourcePrefab.GetComponent<Charm_SummonUnit>().unitPrefab;
                Charm.unitPrefab = new ModUnitAvatar("WindJelly", Vanilla)
                .SetAvatar(Unit =>
                {
                    Unit.defaultNameKey = "ModUnit_WindJelly";
                    Unit.miniFaceChipSprite = Tools.LoadModSprite("Item_WindJellyFish_MiniIcon");
                })
                .SetCustom(Unit =>
                {
                    if (Unit.TryGetComponent<Unit_Grenadier>(out Unit_Grenadier Jelly))
                    {
                        Jelly.bulletDebuffPrefab = WindErosion.Prefab;
                        if (Jelly.fireData is NewWeaponFireData_Bullet VanillaFire)
                        {
                            GameObject WindBullet = new ModBullet("WindJellyBullet", VanillaFire.bulletPrefab)
                            .SetSpriteSet("Bullet",
                                "JellyFish_IceBullet0", "JellyFish_IceBullet1", "JellyFish_IceBullet2")
                            .SetCreateFxSprites("EffectFx",
                                "JellyFish_IceAttackShootFX0", "JellyFish_IceAttackShootFX1", "JellyFish_IceAttackShootFX2",
                                "JellyFish_IceAttackShootFX3", "JellyFish_IceAttackShootFX4", "JellyFish_IceAttackShootFX5")
                            .SetDestroyFxSprites("EffectFx",
                                "JellyFish_IceBullet_FX0", "JellyFish_IceBullet_FX1", "JellyFish_IceBullet_FX2",
                                "JellyFish_IceBullet_FX3", "JellyFish_IceBullet_FX4", "JellyFish_IceBullet_FX5", "JellyFish_IceBullet_FX6")
                            .SetTailFxSprites("Bullet",
                                "JellyFish_IceBulletParticle0", "JellyFish_IceBulletParticle1", "JellyFish_IceBulletParticle2",
                                "JellyFish_IceBulletParticle3", "JellyFish_IceBulletParticle4", "JellyFish_IceBulletParticle5")
                            .EndCreate();
                            NewWeaponFireData_Bullet Fire = Object.Instantiate(VanillaFire);
                            Fire.bulletPrefab = WindBullet;
                            Fire.damageElementalType = Wind; // 伤害属性改成风
                            Jelly.fireData = Fire;
                        }
                    }
                })
                .SetAnimationSprites("UnitAvatar", "JellyFish_Ice_", "JellyFish_Wind_", ("Idle", 10), ("Attack", 12), ("Airborne", 3), ("Down", 1))
                .EndCreate();
                Charm.unitName = new LocalizedString("ModUnit_WindJelly");
            }),//希尔芙的灵魂粉末

            new ModItem("WindSail", EItemRarity.Uncommon)
            .CustomCreateCharm(Tools.SetCategories("HURRICANE"), Action =>
            {
                Charm_HarmfulValue Effect = Action.resourcePrefab.AddComponent<Charm_HarmfulValue>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [ new("ModCharmEffect_WindSail") ];
                Effect.StatusName = "WIND_EROSION_OVER_FLOW";
                Effect.Ending = "%";
                Effect.BaseValueByLevel = [10, 20, 30, 40, 50];
                Effect.stats = [Tools.SetStatusGroup("WIND_DAMAGE", 1, 2, 3, 4, 5)];
                Effect.AddValue = TextFile.WindErosionOverFlowAdd;
                Effect.FindMult = 3f;
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//风帆

            new ModItem("CyanScarf", EItemRarity.Rare)
            .CreateCharm(Tools.SetCategories("HURRICANE"),
            [
                Tools.SetStatusGroup("WIND_EROSION_DAMAGE", 10, 20, 30, 40, 50),
            ]),//青色围巾

            new ModItem("WhirlWindBug", EItemRarity.Rare)
            .CreateCharm(Tools.SetCategories("HURRICANE"),
            [
                Tools.SetStatusGroup("WIND_EROSION_MULT", 4, 8, 12, 16, 20, 24),
            ]),//旋风虫

            new ModItem("Organ", EItemRarity.Rare)
            .CustomCreateCharm(Tools.SetCategories("HURRICANE"), Action =>
            {
                Charm_DebuffsPenetrationDefense Effect = Action.resourcePrefab.AddComponent<Charm_DebuffsPenetrationDefense>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_DebuffsPenetrationDefense")];
                Effect.stats = new List<StatusGroup>()
                {
                    Tools.SetStatusGroup("WIND_EROSION_COUNT", 0, 1, 1, 2),
                }.ToArray();
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//风琴

            new ModItem("WeatherForEcast", EItemRarity.Legend)
            .CreateCharm(Tools.SetCategories("HURRICANE"),
            [
                Tools.SetStatusGroup("WIND_DAMAGE", 2, 4, 6, 8),
                Tools.SetStatusGroup("WIND_EROSION_COUNT", 0, 1, 1, 2),
                Tools.SetStatusGroup("WIND_EROSION_LUCK", 15, 30, 45, 60),
            ]),//天气预报

            new ModItem("Convergence").SetDual()
            .CustomCreateCharm(Tools.SetCategories("HURRICANE", "WINDSONG"), Action =>
            {
                Charm_Convergence Effect = Action.resourcePrefab.AddComponent<Charm_Convergence>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [new("ModCharmEffect_Convergence")];
                Effect.ValueByLevel = [1];
                Effect.ValueName = TextFile.ScoreCount;
                Effect.stats = new List<StatusGroup>()
                {
                    Tools.SetStatusGroup("ATTACK_SPEED", 4, 8, 12)
                }.ToArray();
                Effect.maxLevel = 2;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//乐谱<辐合>

            new ModItem("Potion_Wind", EItemRarity.Rare)
            .CreatePotion(Tools.SetCategories("ALCHEMY"), "WIND_DAMAGE", 4)
            .SetCustomItemEffect(Action =>
            {
                Action.cost = ItemDatabase.FindItemById(28).cost;
                Action.CopyPotionFX(28);
            }),//阿依伦的吐息

            new ModItem("Potion_Wind2", EItemRarity.Uncommon)
            .CreatePotion(Tools.SetCategories("ALCHEMY"), "WIND_DAMAGE", 2)
            .SetCustomItemEffect(Action =>
            {
                Action.cost = ItemDatabase.FindItemById(40).cost;
                Action.CopyPotionFX(40);
            }),//赛松之低语

            new ModItem("ElementKey", EItemRarity.Uncommon)
            .CustomCreateCharm([], Action =>
            {
                Charm_ModKey Effect = Action.resourcePrefab.AddComponent<Charm_ModKey>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [ new("ModCharmEffect_ModKey") ];
                Effect.stats = [ new StatusGroup() { statusID = "PHYSICAL_DAMAGE", valuesByLevel = Effect.MaxByLevel } ];
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            })
            .SetCustomItemEffect(Action =>
            {
                List<Sprite?> SpriteList = 
                [
                    Tools.LoadModSprite("Item_ElementKey_1"),
                    Tools.LoadModSprite("Item_ElementKey_2"),
                    Tools.LoadModSprite("Item_ElementKey_3"),
                    Tools.LoadModSprite("Item_ElementKey_4"),
                    Tools.LoadModSprite("Item_ElementKey_5"),
                ];
                List<Sprite> TrueSpriteList = [];
                foreach (var Use in SpriteList)
                {
                    if (Use != null)
                        TrueSpriteList.Add(Use);
                }
                Action.resourcePrefab.GetComponent<Charm_ModKey>().ItemIcons = TrueSpriteList;
            }),//凯尔萨德尼钥匙

            new ModItem("Homunculus", EItemRarity.Rare)
            .CustomCreateCharm(Tools.SetCategories("ALCHEMY"), Action =>
            {
                Charm_RemovePotion Effect = Action.resourcePrefab.AddComponent<Charm_RemovePotion>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = [ new("ModCharmEffect_RemovePotion") ];
                Effect.stats = [Tools.SetStatusGroup("FINAL_DAMAGE", 8, 12, 16)];
                Effect.maxLevel = 2;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//瓦格纳的玻璃瓶

            new ModItem("VacuumBurst", EItemRarity.Legend)
            .CustomCreateCharm(Tools.SetCategories("HURRICANE"), Item => VacuumBurst!.BindCharm(Item, 4, "ModCharmEffect_VacuumBurst")),//真空弹

            new ModItem("WindBlade", EItemRarity.Uncommon)
            .CustomCreateCharm(Tools.SetCategories("HURRICANE"), Item => WindBlade!.BindCharm(Item, 4, "ModCharmEffect_WindBlade")),//风刃术

            new ModItem("WindyWeather", EItemRarity.Rare)
            .CustomCreateCharm(Tools.SetCategories("HURRICANE"), Item => WindyWeather!.BindCharm(Item, 4, "ModCharmEffect_WindyWeather")),//大风天
        ];
        public static void CreateModItems()
        {
            foreach (ModItem Data in ModItemEntityData)
                Data.CreateItem();
        }
        //模组弹幕
        public static readonly ModBullet WindArrow = new ModBullet("WindArrow", () =>
        {
            WeaponEntity? Entity = WeaponDatabase.FindWeaponById(WeaponIDs.PurpleJacket);
            WeaponSimple? Simple = Entity?.mainWeaponPrefab?.GetComponent<WeaponSimple>();
            if (Simple == null || Simple.basicComboAttacks.Length == 0) return null;
            return (Simple.basicComboAttacks[0] as NewWeaponFireData_Bullet)?.bulletPrefab;
        })
        .SetBullet(Action =>
        {
            Action.pierceCreatureCount = 3;
            Action.speedScale *= 1.5f;
        })
        .SetSpriteSet("Bullet", "PoisonBolt_00", "PoisonBolt_01", "PoisonBolt_02")
        .SetAttackSucceededFxSprites("EffectFx", "JellyFish_Bullet_FX0", "JellyFish_Bullet_FX1", "JellyFish_Bullet_FX2",
            "JellyFish_Bullet_FX3", "JellyFish_Bullet_FX4", "JellyFish_Bullet_FX5", "JellyFish_Bullet_FX6")
        .SetDestroyFxSprites("Bullet", "JellyFish_BulletParticle0", "JellyFish_BulletParticle1", "JellyFish_BulletParticle2",
            "JellyFish_BulletParticle3", "JellyFish_BulletParticle4", "JellyFish_BulletParticle5")
        .SetTailFxSprites("Bullet", "JellyFish_BulletParticle0", "JellyFish_BulletParticle1", "JellyFish_BulletParticle2",
            "JellyFish_BulletParticle3", "JellyFish_BulletParticle4", "JellyFish_BulletParticle5");

        public static readonly ModBullet AirArrow = new ModBullet("AirArrow", () =>
        {
            WeaponEntity? Entity = WeaponDatabase.FindWeaponById(WeaponIDs.PurpleJacket);
            WeaponSimple? Simple = Entity?.mainWeaponPrefab?.GetComponent<WeaponSimple>();
            if (Simple == null || Simple.specialAttacks.Length == 0) return null;
            return (Simple.specialAttacks[0] as NewWeaponFireData_Bullet)?.bulletPrefab;
        })
        .SetBullet(Action =>
        {
            Action.pierceCreatureCount = 1;
            Action.speedScale *= 2f;
            if (Action.bulletChildParticle != null)
            {
                Action.bulletChildParticle.gameObject.SetActive(false);
                Action.bulletChildParticle = null;
            }
        })
        .SetSpriteSet("Bullet", "Bolt_Special_Poison0", "Bolt_Special_Poison1", "Bolt_Special_Poison2",
            "Bolt_Special_Poison3", "Bolt_Special_Poison4", "Bolt_Special_Poison5")
        .SetAttackSucceededFxSprites("EffectFx", "JellyFish_Bullet_FX0", "JellyFish_Bullet_FX1", "JellyFish_Bullet_FX2",
            "JellyFish_Bullet_FX3", "JellyFish_Bullet_FX4", "JellyFish_Bullet_FX5", "JellyFish_Bullet_FX6");

        public static readonly ModBullet VacuumBurstBullet = new ModBullet("VacuumBurstBullet", () =>
        {
            ItemEntity? AirBook = ItemDatabase.FindItemById(3000);
            Charm_Magic? AirCharm = AirBook?.resourcePrefab?.GetComponent<Charm_Magic>();
            ActiveSkill_Bolt? Bolt = AirCharm?.ContainedMagic?.magicPrefab?.GetComponent<ActiveSkill_Bolt>();
            return Bolt?.boltBulletPrefab;
        })
        .SetBullet(Action =>
        {
            Action.pierceCreatureCount = 1;
            Action.attackSucceededFxPrefab = null;
            Action.createFxPrefab = null;
            if (Action.DestroyModule != null) Action.DestroyModule.destroyFxPrefab = null;
        })
        .SetSpeedScale(2f)
        .SetSpriteFrames("Bullet", "VacuumBurstBullet0", "VacuumBurstBullet1", "VacuumBurstBullet2", "VacuumBurstBullet3", "VacuumBurstBullet4");

        public static readonly ModBullet WindBladeBullet = new ModBullet("WindBladeBullet", () =>
        {
            if (Tools.GetPro("AirSlashMini_Flame", out GameObject? Pro) && Pro != null) return Pro;
            if (StarSephiriaPatches.Projectile.TryGetValue("AirSlashMini_Flame", out GameObject? Direct) && Direct != null) return Direct;
            if (Tools.GetPro("AirSlashMini", out GameObject? Fallback) && Fallback != null) return Fallback;
            if (StarSephiriaPatches.Projectile.TryGetValue("AirSlashMini", out GameObject? FallbackDirect) && FallbackDirect != null) return FallbackDirect;
            return null;
        })
        .SetBullet(Action =>
        {
            if (Action.bulletChildParticle != null)
            {
                Action.bulletChildParticle.gameObject.SetActive(false);
                Action.bulletChildParticle = null;
            }
        })
        .SetSpeedScale(1.5f)
        .SetSpriteFrames("Bullet",
            "ScabardOfExorcism_MiniFireBullet0", "ScabardOfExorcism_MiniFireBullet1",
            "ScabardOfExorcism_MiniFireBullet2", "ScabardOfExorcism_MiniFireBullet3",
            "ScabardOfExorcism_MiniFireBullet4", "ScabardOfExorcism_MiniFireBullet5")
        .SetAttackSucceededFxSprites("EffectFx",
            "FireSpear_SlashFX00", "FireSpear_SlashFX01",
            "FireSpear_SlashFX02", "FireSpear_SlashFX03",
            "FireSpear_SlashFX04", "FireSpear_SlashFX05",
            "FireSpear_SlashFX06")
        .SetDestroyFxSprites("EffectFx",
            "ScabardOfExorcism_MiniFireFX0", "ScabardOfExorcism_MiniFireFX1",
            "ScabardOfExorcism_MiniFireFX2", "ScabardOfExorcism_MiniFireFX3",
            "ScabardOfExorcism_MiniFireFX4", "ScabardOfExorcism_MiniFireFX5",
            "ScabardOfExorcism_MiniFireFX6", "ScabardOfExorcism_MiniFireFX7");

        private static void ApplyCastTemplate(ActiveSkillEntity Entity)
        {
            ActiveSkillEntity? AirSkill = ItemDatabase.FindItemById(3000)?.resourcePrefab?.GetComponent<Charm_Magic>()?.ContainedMagic;
            ActiveSkillEntity? FireSkill = ItemDatabase.FindItemById(3012)?.resourcePrefab?.GetComponent<Charm_Magic>()?.ContainedMagic;
            ActiveSkillEntity? CastTemplate = null;
            if (AirSkill?.chagingFxByTier is { Length: > 0 } AirFx && AirFx[0] != null) CastTemplate = AirSkill;
            else if (FireSkill?.chagingFxByTier is { Length: > 0 } FireFx && FireFx[0] != null) CastTemplate = FireSkill;
            if (CastTemplate == null) return;

            Entity.chagingFxByTier = CastTemplate.chagingFxByTier;
            Entity.completeChargeFxByTier = CastTemplate.completeChargeFxByTier;
            Entity.castingCircleOverride = CastTemplate.castingCircleOverride;
            Entity.castGroundOverride = CastTemplate.castGroundOverride;
            Entity.requireCastingAnimation = CastTemplate.requireCastingAnimation;
            Entity.magicDashCurveType = CastTemplate.magicDashCurveType;
            Entity.magicDashDirectionType = CastTemplate.magicDashDirectionType;
            Entity.magicDashTime = CastTemplate.magicDashTime;
            Entity.magicDashSpeed = CastTemplate.magicDashSpeed;
            Entity.magicDashAngle = CastTemplate.magicDashAngle;
        }

        public static readonly ModActiveSkill VacuumBurst = new ModActiveSkill("VacuumBurst", typeof(ActiveSkill_VacuumBurst))
        .SetSkill<ActiveSkill_VacuumBurst>(Skill =>
        {
            Skill.boltBulletPrefab = VacuumBurstBullet.Template;
            Skill.damageId = "[Star's Sephiria Mod]VacuumBurst";
            Skill.relatedDamage = "WindDamage";
            Skill.elementalType = Wind;
            Skill.defaultDamageByLevel = [6f, 9f, 12f, 15f, 18f];
            Skill.damagePercentByLevel = [50f, 75f, 100f, 125f, 150f];
            Skill.ExplosionOffsetYPercent = 0.25f;
            Skill.effectString = new LocalizedString("ModSkill_VacuumBurst_Effect");
            Skill.specsString = new LocalizedString("ModSkill_VacuumBurst_Specs");
            Skill.specsValueString = new LocalizedString("ModSkill_VacuumBurst_SpecsValue");
        })
        .SetEntity(Entity =>
        {
            Entity.rarity = EItemRarity.Legend;
            Entity.magicClasses = [EMagicClass.Air];
            Entity.mpCostsByLevel = [12, 14, 16, 18, 20];
            Entity.cooldownTime = 20f;
            Entity.ammo = 1;
            Entity.castingType = ECastingType.SingleShot;
            Entity.chargingTime = 0.5f;
            Entity.maxChargingPower = 3;
            Entity.magicLabelColor = WindColor;
            Entity.icon = Tools.LoadModSprite("Skill_VacuumBurst");
            ApplyCastTemplate(Entity);
        });

        public static readonly ModActiveSkill WindBlade = new ModActiveSkill("WindBlade", typeof(ActiveSkill_WindBlade))
        .SetSkill<ActiveSkill_WindBlade>(Skill =>
        {
            Skill.boltBulletPrefab = WindBladeBullet.Template;
            Skill.damageId = "[Star's Sephiria Mod]WindBlade";
            Skill.relatedDamage = "WindDamage";
            Skill.elementalType = Wind;
            Skill.defaultDamageByLevel = [10f, 15f, 20f, 25f, 30f];
            Skill.damagePercentByLevel = [70f, 105f, 140f, 175f, 210f];
            Skill.fireInterval = 0.08f;
            Skill.effectString = new LocalizedString("ModSkill_WindBlade_Effect");
            Skill.specsString = new LocalizedString("ModSkill_WindBlade_Specs");
            Skill.specsValueString = new LocalizedString("ModSkill_WindBlade_SpecsValue");
        })
        .SetEntity(Entity =>
        {
            Entity.rarity = EItemRarity.Uncommon;
            Entity.magicClasses = [EMagicClass.Air];
            Entity.mpCostsByLevel = [6, 7, 8, 9, 10];
            Entity.cooldownTime = 1f;
            Entity.ammo = 8;
            Entity.castingType = ECastingType.SingleShot;
            Entity.chargingTime = 0.5f;
            Entity.maxChargingPower = 3;
            Entity.magicLabelColor = WindColor;
            Entity.icon = Tools.LoadModSprite("Skill_WindBlade");
            ApplyCastTemplate(Entity);
        });

        public static readonly ModActiveSkill WindyWeather = new ModActiveSkill("WindyWeather", typeof(ActiveSkill_WindyWeather))
        .SetSkill<ActiveSkill_WindyWeather>(Skill =>
        {
            Skill.damageId = "[Star's Sephiria Mod]WindyWeather";
            Skill.relatedDamage = "WindDamage";
            Skill.elementalType = Wind;
            Skill.defaultDamageByLevel = [6f, 12f, 18f, 24f, 30f];
            Skill.damagePercentByLevel = [20f, 40f, 60f, 80f, 100f];
            Skill.effectString = new LocalizedString("ModSkill_WindyWeather_Effect");
            Skill.specsString = new LocalizedString("ModSkill_WindyWeather_Specs");
            Skill.specsValueString = new LocalizedString("ModSkill_WindyWeather_SpecsValue");
        })
        .SetEntity(Entity =>
        {
            Entity.rarity = EItemRarity.Rare;
            Entity.magicClasses = [EMagicClass.Air];
            Entity.mpCostsByLevel = [18, 20, 22, 24, 26];
            Entity.cooldownTime = 25f;
            Entity.ammo = 1;
            Entity.castingType = ECastingType.SingleShot;
            Entity.chargingTime = 0.5f;
            Entity.maxChargingPower = 3;
            Entity.magicLabelColor = WindColor;
            Entity.icon = Tools.LoadModSprite("Skill_WindyWeather");
            ApplyCastTemplate(Entity);
        });

        // 模组攻击数据
        public static List<ModFireData> WindGreatSword_Basic =
        [
            ModFireData.Basic(WeaponIDs.TheEmberforged, 0).SetSwingSpriteFolder("Weapon.GreatSword_Wind"),
            ModFireData.Basic(WeaponIDs.TheEmberforged, 1).SetSwingSpriteFolder("Weapon.GreatSword_Wind"),
            ModFireData.Basic(WeaponIDs.TheEmberforged, 2).SetSwingSpriteFolder("Weapon.GreatSword_Wind"),
        ];
        public static ModFireData WindGreatSword_Special = ModFireData.Special(WeaponIDs.TheEmberforged, 0).SetSwingSpriteFolder("Weapon.GreatSword_Wind");
        public static ModFireData WindGreatSword_Dash = ModFireData.Dash(WeaponIDs.TheEmberforged, 0).SetSwingSpriteFolder("Weapon.GreatSword_Wind");
        public static ModFireData WindGreatSword_Tornado = ModFireData.Special(WeaponIDs.TheEmberforged, 0).SetElement(Wind).SetDamage(1f)
            .SetMeleeSwingSpriteFolder("Weapon.GreatSword_Tornado", "GreatSword0_TurnSwing_Fire_Minor", true, 1, 1.5f)
            .SetMeleeMultiHit(5, 0.1f, 0.6f);

        public static List<ModFireData> WindDagger_Basic =
        [
            ModFireData.Basic(WeaponIDs.FireDagger, 0).SetSwingSpriteFolder("Weapon.Dagger_Wind"),
            ModFireData.Basic(WeaponIDs.FireDagger, 1).SetSwingSpriteFolder("Weapon.Dagger_Wind"),
            ModFireData.Basic(WeaponIDs.FireDagger, 2).SetSwingSpriteFolder("Weapon.Dagger_Wind"),
            ModFireData.Basic(WeaponIDs.FireDagger, 3).SetSwingSpriteFolder("Weapon.Dagger_Wind"),
            ModFireData.Basic(WeaponIDs.FireDagger, 4).SetSwingSpriteFolder("Weapon.Dagger_Wind"),
        ];
        public static ModFireData WindDagger_Fury = ModFireData.Special(WeaponIDs.FireDagger, 0).SetSwingSpriteFolder("Weapon.Dagger_Wind");
        public static ModFireData WindDagger_Parry = ModFireData.Special(WeaponIDs.FireDagger, 1).SetSwingSpriteFolder("Weapon.Dagger_Wind");
        public static ModFireData WindDagger_Dash = ModFireData.Dash(WeaponIDs.FireDagger, 0).SetSwingSpriteFolder("Weapon.Dagger_Wind");
        public static ModFireData WindDagger_Vortex = ModFireData.BladeZone(WeaponIDs.ShadowofHope).SetElement(Wind).SetBladeZoneFxSprites("Weapon.Dagger_Vortex")
             .CustomSet(Data =>
             {
                 Data.attackDashAngle = 0f;
                 Data.attackDashSpeed = 0f;
                 Data.attackDashTime = 0f;
             });

        public static ModFireData WindCrossbow_Basic = ModFireData.Basic(WeaponIDs.ColossalCrossbow, 0).SetDamage(0.8f).SetElement(Wind).SetRelatedStat("WINDDAMAGE")
            .CustomSet(Data =>
            {
                if (Data is NewWeaponFireData_Bullet BulletData)
                {
                    BulletData.bulletPrefab = WindArrow.Template;
                }
            });
        public static ModFireData WindCrossbow_AirValve = ModFireData.Special(WeaponIDs.ColossalCrossbow, 0).SetDamage(3.2f).SetElement(Wind).SetRelatedStat("WINDDAMAGE")
            .CustomSet(Data =>
            {
                if (Data is NewWeaponFireData_Bullet BulletData)
                {
                    BulletData.bulletPrefab = AirArrow.Template;
                }
            });

        public static List<ModFireData> WindKatana_Basic =
        [
            ModFireData.Basic(WeaponIDs.Helbanus, 0).SetDamage(0.88f).SetElement(Wind).SetRelatedStat("WINDDAMAGE").SetSwingSpriteFolder("Weapon.Katana_Wind"),
            ModFireData.Basic(WeaponIDs.Helbanus, 1).SetDamage(0.88f).SetElement(Wind).SetRelatedStat("WINDDAMAGE").SetSwingSpriteFolder("Weapon.Katana_Wind"),
            ModFireData.Basic(WeaponIDs.Helbanus, 2).SetDamage(1.32f).SetElement(Wind).SetRelatedStat("WINDDAMAGE").SetSwingSpriteFolder("Weapon.Katana_Wind"),
        ];
        public static List<ModFireData> WindKatana_Special =
        [
            ModFireData.Special(WeaponIDs.Helbanus, 0).SetDamage(1.88f).SetElement(Wind).SetRelatedStat("WINDDAMAGE").SetSwingSpriteFolder("Weapon.Katana_Wind"),
            ModFireData.Special(WeaponIDs.Helbanus, 1).SetDamage(1.88f).SetElement(Wind).SetRelatedStat("WINDDAMAGE").SetSwingSpriteFolder("Weapon.Katana_Wind"),
        ];
        public static ModFireData WindKatana_Dash = ModFireData.Dash(WeaponIDs.Helbanus, 0).SetDamage(1.88f)
            .SetElement(Wind).SetRelatedStat("WINDDAMAGE").SetSwingSpriteFolder("Weapon.Katana_Wind");
        public static ModFireData WindKatana_Moment = ModFireData.GreatDraw(WeaponIDs.RendbladeHeavenrend)
            .SetSwingRangeScaling(false).SetSwingScale(0.5f).SetSwingSpriteFolder("Weapon.Katana_Moment");
        public static ModFireData WindKatana_Moment2 = ModFireData.Basic(WeaponIDs.Helbanus, 0).SetDamage(1.32f)
            .SetElement(Wind).SetRelatedStat("WINDDAMAGE").SetSwingSpriteFolder("Weapon.Katana_Wind");

        public static List<ModFireData> WindStaff_Basic =
        [
            ModFireData.Basic(WeaponIDs.StaffOfEmbers, 0).SetElement(Wind).SetRelatedStat("WINDDAMAGE").SetSwingSpriteFolder("Weapon.Staff_Wind"),
            ModFireData.Basic(WeaponIDs.StaffOfEmbers, 1).SetElement(Wind).SetRelatedStat("WINDDAMAGE").SetSwingSpriteFolder("Weapon.Staff_Wind"),
            ModFireData.Basic(WeaponIDs.StaffOfEmbers, 2).SetElement(Wind).SetRelatedStat("WINDDAMAGE").SetSwingSpriteFolder("Weapon.Staff_Wind"),
            ModFireData.Basic(WeaponIDs.StaffOfEmbers, 3).SetElement(Wind).SetRelatedStat("WINDDAMAGE").SetSwingSpriteFolder("Weapon.Staff_Wind"),
            ModFireData.Basic(WeaponIDs.StaffOfEmbers, 4).SetElement(Wind).SetRelatedStat("WINDDAMAGE").SetSwingSpriteFolder("Weapon.Staff_Wind"),
            ModFireData.Basic(WeaponIDs.StaffOfEmbers, 5).SetElement(Wind).SetRelatedStat("WINDDAMAGE").SetSwingSpriteFolder("Weapon.Staff_Wind"),
        ];
        public static ModFireData WindStaff_Special = ModFireData.Special(WeaponIDs.StaffOfEmbers, 0)
            .SetElement(Wind).SetRelatedStat("WINDDAMAGE").SetSwingSpriteFolder("Weapon.Staff_Wind");
        public static ModFireData WindStaff_Special_Base = ModFireData.SecondSpecial(WeaponIDs.StaffOfEmbers)
            .SetElement(Wind).SetRelatedStat("WINDDAMAGE").SetSwingSpriteFolder("Weapon.Staff_Wind");
        public static ModFireData WindStaff_Special_Enhanced = ModFireData.SecondSpecial(WeaponIDs.StaffOfEmbers, true)
            .SetElement(Wind).SetRelatedStat("WINDDAMAGE").SetSwingSpriteFolder("Weapon.Staff_Wind");
        public static ModFireData WindStaff_Dash = ModFireData.Dash(WeaponIDs.StaffOfEmbers, 0)
            .SetElement(Wind).SetRelatedStat("WINDDAMAGE").SetSwingSpriteFolder("Weapon.Staff_Wind");
        // 模组武器
        public static List<ModWeapon> ModWeaponData =
        [
            new ModWeapon("WindSword", WeaponIDs.WindSword, WeaponIDs.DefaultSwordandShield)
            .SetEnhanceFrom(WeaponIDs.DefaultSwordandShield)
            .SetSprite("Sword_Tier1", "ModWeapon_WindSword", "Weapon")
            .SetSprite("Shield_Tier1", "ModWeapon_WindSword_Shield", "Weapon")
            .CustomCreateWeapon(Weapon =>
            {
                List<WeaponAddon> Addons = [];
                WeaponAddonCommon_StatusUnsafe Effect = Weapon.gameObject.AddComponent<WeaponAddonCommon_StatusUnsafe>();
                Effect.parent = Weapon;
                Effect.effectText = new LocalizedString("ModWeapon_WindSword_Effect");
                Effect.status =
                [
                    new() {
                        statId = TextFile.WindDamage,
                        value = 5,
                        displayMultiplier = 1f,
                        displaySymbol = "",
                    },
                ];
                Addons.Add(Effect);
                Weapon.addons = Addons.ToArray();
            }),

            new ModWeapon("SpiralCloudBand", WeaponIDs.SpiralCloudBand, WeaponIDs.DefaultSwordandShield)
            .SetEnhanceFrom(WeaponIDs.WindSword)
            .SetSprite("Sword_Tier1", "ModWeapon_SpiralCloudBand", "Weapon")
            .SetSprite("Shield_Tier1", "ModWeapon_SpiralCloudBand_Shield", "Weapon")
            .CustomCreateWeapon(Weapon =>
            {
                List<WeaponAddon> Addons = [];
                WeaponAddonCommon_StatusUnsafe Effect = Weapon.gameObject.AddComponent<WeaponAddonCommon_StatusUnsafe>();
                Effect.parent = Weapon;
                Effect.effectText = new LocalizedString("ModWeapon_WindSword_Effect");
                Effect.status =
                [
                    new() {
                        statId = TextFile.WindDamage,
                        value = 5,
                        displayMultiplier = 1f,
                        displaySymbol = "",
                    },
                    new() {
                        statId = TextFile.CyanOrbUP,
                        value = 1,
                        displayMultiplier = 1f,
                        displaySymbol = "",
                    },
                ];
                Addons.Add(Effect);

                WeaponAddon_MoveSpeed Effect2 = Weapon.gameObject.AddComponent<WeaponAddon_MoveSpeed>();
                Effect2.parent = Weapon;
                Effect2.effectText = new LocalizedString("ModWeapon_SpiralCloudBand_Effect");
                Effect2.Bonus = 0.1f;
                Addons.Add(Effect2);
                Weapon.addons = Addons.ToArray();
            }),

            new ModWeapon("WindGreatSword", WeaponIDs.WindGreatSword, WeaponIDs.TheEmberforged)
            .SetEnhanceFrom(WeaponIDs.SteelGreatsword)
            .SetSprite("GreatSword_Fire_Minor_0", "ModWeapon_WindGreatSword", "Weapon")
            .SetSprite("GreatSword_Fire_Minor_Blade", "ModWeapon_WindGreatSword_1", "Weapon")
            .SetBasicAttacks(WindGreatSword_Basic.ToArray())
            .SetSpecialAttack(0, WindGreatSword_Special)
            .SetDashAttack(0, WindGreatSword_Dash)
            .CustomCreateWeapon(Weapon =>
            {
                List<WeaponAddon> Addons = [];
                WeaponAddon_ModAddElemental Effect = Weapon.gameObject.AddComponent<WeaponAddon_ModAddElemental>();
                Effect.parent = Weapon;
                Effect.DamageId = "[Star's Sephiria Mod]AdditionalWeaponDamageWind";
                Effect.ValueName = TextFile.WindDamage;
                Effect.DamageMult = 20;
                Effect.elementalType = Wind;
                Effect.effectText = new LocalizedString("ModWeapon_WindGreatSword_Effect");
                Addons.Add(Effect);
                Weapon.addons = Addons.ToArray();
            }),

            new ModWeapon("Tornado", WeaponIDs.Tornado, WeaponIDs.TheEmberforged)
            .SetEnhanceFrom(WeaponIDs.WindGreatSword)
            .SetSprite("GreatSword_Fire_Minor", "ModWeapon_Tornado", "Weapon")
            .SetSprite("GreatSword_Fire_Minor_Blade", "ModWeapon_Tornado_1", "Weapon")
            .SetBasicAttacks(WindGreatSword_Basic.ToArray())
            .SetSpecialAttack(0, WindGreatSword_Tornado)
            .SetDashAttack(0, WindGreatSword_Dash)
            .CustomCreateWeapon(Weapon =>
            {
                if (Weapon is WeaponSimple_GreatSword GreatSword)
                {
                    GreatSword.sweepCost = 3;
                    GreatSword.sweepTimer.time = 0.466f;
                }

                List<WeaponAddon> Addons = [];
                WeaponAddon_ModAddElemental Effect = Weapon.gameObject.AddComponent<WeaponAddon_ModAddElemental>();
                Effect.parent = Weapon;
                Effect.DamageId = "[Star's Sephiria Mod]AdditionalWeaponDamageWind";
                Effect.ValueName = TextFile.WindDamage;
                Effect.DamageMult = 20;
                Effect.elementalType = Wind;
                Effect.effectText = new LocalizedString("ModWeapon_WindGreatSword_Effect");
                Addons.Add(Effect);

                WeaponAddonCommon_StatusUnsafe Effect2 = Weapon.gameObject.AddComponent<WeaponAddonCommon_StatusUnsafe>();
                Effect2.parent = Weapon;
                Effect2.effectText = new LocalizedString("ModWeapon_Tornado_Effect");
                Addons.Add(Effect2);
                Weapon.addons = Addons.ToArray();
            }),

            new ModWeapon("WindDagger", WeaponIDs.WindDagger, WeaponIDs.LightningDagger)
            .SetEnhanceFrom(WeaponIDs.Dagger)
            .SetSprite("Dagger_Tier2_Lightning", "ModWeapon_WindDagger", "Weapon")
            .SetSprite("Dagger_Tier2_Lightning_Blade", "ModWeapon_WindDagger_1", "Weapon")
            .SetBasicAttacks(WindDagger_Basic.ToArray())
            .SetSpecialAttack(0, WindDagger_Fury)
            .SetSpecialAttack(1, WindDagger_Parry)
            .SetDashAttack(0, WindDagger_Dash)
            .CustomCreateWeapon(Weapon =>
            {
                List<WeaponAddon> Addons = [];
                WeaponAddon_ModAddElemental Effect = Weapon.gameObject.AddComponent<WeaponAddon_ModAddElemental>();
                Effect.parent = Weapon;
                Effect.DamageId = "[Star's Sephiria Mod]AdditionalWeaponDamageWind";
                Effect.ValueName = TextFile.WindDamage;
                Effect.DamageMult = 40;
                Effect.elementalType = Wind;
                Effect.effectText = new LocalizedString("ModWeapon_WindDagger_Effect");
                Addons.Add(Effect);
                Weapon.addons = Addons.ToArray();
            }),

            new ModWeapon("Vortex", WeaponIDs.Vortex, WeaponIDs.LightningDagger)
            .SetEnhanceFrom(WeaponIDs.WindDagger)
            .SetSprite("Dagger_Tier2_Lightning", "ModWeapon_Vortex", "Weapon")
            .SetSprite("Dagger_Tier2_Lightning_Blade", "ModWeapon_Vortex_1", "Weapon")
            .SetBasicAttacks(WindDagger_Basic.ToArray())
            .SetSpecialAttack(0, WindDagger_Fury)
            .SetSpecialAttack(1, WindDagger_Parry)
            .SetDashAttack(0, WindDagger_Dash)
            .CustomCreateWeapon(Weapon =>
            {
                List<WeaponAddon> Addons = [];
                WeaponAddon_ModAddElemental Effect = Weapon.gameObject.AddComponent<WeaponAddon_ModAddElemental>();
                Effect.parent = Weapon;
                Effect.DamageId = "[Star's Sephiria Mod]AdditionalWeaponDamageWind";
                Effect.ValueName = TextFile.WindDamage;
                Effect.DamageMult = 40;
                Effect.elementalType = Wind;
                Effect.effectText = new LocalizedString("ModWeapon_WindDagger_Effect");
                Addons.Add(Effect);

                WeaponAddon_VortexEffect Effect2 = Weapon.gameObject.AddComponent<WeaponAddon_VortexEffect>();
                Effect2.parent = Weapon;
                Effect2.effectText = new LocalizedString("ModWeapon_Vortex_Effect");
                Addons.Add(Effect2);
                Weapon.addons = Addons.ToArray();
            }),

            new ModWeapon("VacuumBlade", WeaponIDs.VacuumBlade, WeaponIDs.LightningDagger)
            .SetEnhanceFrom(WeaponIDs.WindDagger)
            .SetSprite("Dagger_Tier2_Lightning", "ModWeapon_VacuumBlade", "Weapon")
            .SetSprite("Dagger_Tier2_Lightning_Blade", "ModWeapon_VacuumBlade_1", "Weapon")
            .SetBasicAttacks(WindDagger_Basic.ToArray())
            .SetSpecialAttack(0, WindDagger_Fury)
            .SetSpecialAttack(1, WindDagger_Parry)
            .SetDashAttack(0, WindDagger_Dash)
            .CustomCreateWeapon(Weapon =>
            {
                List<WeaponAddon> Addons = [];
                WeaponAddon_ModAddElemental Effect = Weapon.gameObject.AddComponent<WeaponAddon_ModAddElemental>();
                Effect.parent = Weapon;
                Effect.DamageId = "[Star's Sephiria Mod]AdditionalWeaponDamageWind";
                Effect.ValueName = TextFile.WindDamage;
                Effect.DamageMult = 40;
                Effect.elementalType = Wind;
                Effect.effectText = new LocalizedString("ModWeapon_WindDagger_Effect");
                Addons.Add(Effect);

                WeaponAddon_BasicSlash Effect2 = Weapon.gameObject.AddComponent<WeaponAddon_BasicSlash>();
                Effect2.parent = Weapon;
                Effect2.status =
                [
                    new() {
                        statId = TextFile.AdditionalElementalDamageBonus,
                        value = 60,
                        displayMultiplier = 1f,
                        displaySymbol = "%",
                    },
                ];
                Effect2.effectText = new LocalizedString("ModWeapon_VacuumBlade_Effect");
                Addons.Add(Effect2);
                Weapon.addons = Addons.ToArray();
            }),

            new ModWeapon("WindCrossbow", WeaponIDs.WindCrossbow, WeaponIDs.ColossalCrossbow)
            .SetEnhanceFrom(WeaponIDs.ColossalCrossbow)
            .SetSprite("Crossbow_Tier1_0", "ModWeapon_WindCrossbow_0", "Weapon")
            .SetSprite("Crossbow_Tier1_1", "ModWeapon_WindCrossbow_1", "Weapon")
            .SetSprite("Crossbow_Tier1_2", "ModWeapon_WindCrossbow_2", "Weapon")
            .SetSprite("Crossbow_Tier1_3", "ModWeapon_WindCrossbow_3", "Weapon")
            .SetBasicAttack(0, WindCrossbow_Basic)
            .SetBasicAttack(1, WindCrossbow_Basic)
            .SetCrossBowValue(0.2f, 1, 1, 6)
            .CustomCreateWeapon(Weapon =>
            {
                List<WeaponAddon> Addons = [];
                WeaponAddonCommon_StatusUnsafe Effect = Weapon.gameObject.AddComponent<WeaponAddonCommon_StatusUnsafe>();
                Effect.parent = Weapon;
                Effect.effectText = new LocalizedString("ModWeapon_WindCrossbow_Effect");
                Addons.Add(Effect);
                Weapon.addons = Addons.ToArray();
            }),

            new ModWeapon("AirValve", WeaponIDs.AirValve, WeaponIDs.ColossalCrossbow)
            .SetEnhanceFrom(WeaponIDs.WindCrossbow)
            .SetSprite("Crossbow_Tier1_0", "ModWeapon_AirValve_0", "Weapon")
            .SetSprite("Crossbow_Tier1_1", "ModWeapon_AirValve_1", "Weapon")
            .SetSprite("Crossbow_Tier1_2", "ModWeapon_AirValve_2", "Weapon")
            .SetSprite("Crossbow_Tier1_3", "ModWeapon_AirValve_3", "Weapon")
            .SetBasicAttack(0, WindCrossbow_Basic)
            .SetBasicAttack(1, WindCrossbow_Basic)
            .SetSpecialAttack(0, WindCrossbow_AirValve)
            .SetCrossBowValue(0.125f, 1.2f, 1, 6)
            .CustomCreateWeapon(Weapon =>
            {
                List<WeaponAddon> Addons = [];
                WeaponAddonCommon_StatusUnsafe Effect = Weapon.gameObject.AddComponent<WeaponAddonCommon_StatusUnsafe>();
                Effect.parent = Weapon;
                Effect.effectText = new LocalizedString("ModWeapon_WindCrossbow_Effect");
                Addons.Add(Effect);

                WeaponAddonCommon_StatusUnsafe Effect2 = Weapon.gameObject.AddComponent<WeaponAddonCommon_StatusUnsafe>();
                Effect2.parent = Weapon;
                Effect2.effectText = new LocalizedString("ModWeapon_AirValve_Effect");
                Addons.Add(Effect2);
                Weapon.addons = Addons.ToArray();
            }),

            new ModWeapon("WindKatana", WeaponIDs.WindKatana, WeaponIDs.Helbanus)
            .SetEnhanceFrom(WeaponIDs.Blade)
            .SetSprite("Katana_Fire_Tier2_0", "ModWeapon_WindKatana", "Weapon")
            .SetSprite("katana_Fire_Tier2_1", "ModWeapon_WindKatana_1", "Weapon")
            .SetSprite("katana_Fire_Tier2_2", "ModWeapon_WindKatana_2", "Weapon")
            .SetSprite("katana_Fire_Tier2_3", "ModWeapon_WindKatana_3", "Weapon")
            .SetBasicAttacks(WindKatana_Basic.ToArray())
            .SetSpecialAttacks(WindKatana_Special.ToArray())
            .SetDashAttack(0, WindKatana_Dash)
            .CustomCreateWeapon(Weapon =>
            {
                List<WeaponAddon> Addons = [];
                WeaponAddonCommon_StatusUnsafe Effect = Weapon.gameObject.AddComponent<WeaponAddonCommon_StatusUnsafe>();
                Effect.parent = Weapon;
                Effect.effectText = new LocalizedString("ModWeapon_WindKatana_Effect");
                Addons.Add(Effect);
                Weapon.addons = Addons.ToArray();
            }),

            new ModWeapon("Moment", WeaponIDs.Moment, WeaponIDs.BeastMarksSwordHou)
            .SetEnhanceFrom(WeaponIDs.WindKatana)
            .SetSprite("katana_Sheath_Range_0", "ModWeapon_Moment", "Weapon")
            .SetSprite("katana_Sheath_Range_1", "ModWeapon_Moment_1", "Weapon")
            .SetSprite("katana_Sheath_Range_2", "ModWeapon_Moment_2", "Weapon")
            .SetSprite("katana_Sheath_Range_3", "ModWeapon_Moment_3", "Weapon")
            .SetBasicAttacks(WindKatana_Basic.ToArray())
            .SetSpecialAttacks(WindKatana_Special.ToArray())
            .SetDashAttack(0, WindKatana_Dash)
            .SetKatanaQuickDraw(WindKatana_Moment2)
            .CustomCreateWeapon(Weapon =>
            {
                if (Weapon is WeaponSimple_Katana Katana)
                {
                    Katana.useQuickDraw = true;
                    Katana.quickDrawFireData.swingSoundEvent = Katana.specialAttacks[0].swingSoundEvent;
                }

                List<WeaponAddon> Addons = [];
                WeaponAddonCommon_StatusUnsafe Effect = Weapon.gameObject.AddComponent<WeaponAddonCommon_StatusUnsafe>();
                Effect.parent = Weapon;
                Effect.effectText = new LocalizedString("ModWeapon_WindKatana_Effect");
                Addons.Add(Effect);

                WeaponAddon_BasicDeBuff Effect2 = Weapon.gameObject.AddComponent<WeaponAddon_BasicDeBuff>();
                Effect2.parent = Weapon;
                Effect2.debuffPrefab = WindErosion.Prefab;
                Effect2.effectText = new LocalizedString("ModWeapon_Moment_Effect");
                Addons.Add(Effect2);
                Weapon.addons = Addons.ToArray();
            }),

            new ModWeapon("WindStaff", WeaponIDs.WindStaff, WeaponIDs.StaffOfEmbers)
            .SetEnhanceFrom(WeaponIDs.Quarterstaff)
            .SetSprite("StaffFire_Tier2", "ModWeapon_WindStaff", "Weapon")
            .SetSprite("StaffFire_Tier2_In", "ModWeapon_WindStaff_1", "Weapon")
            .SetBasicAttacks(WindStaff_Basic.ToArray())
            .SetSpecialAttack(0, WindStaff_Special)
            .SetDashAttack(0, WindStaff_Dash)
            .CustomCreateWeapon(Weapon =>
            {
                if (Weapon is WeaponSimple_QuartterStaff Staff)
                {
                    Staff.secondSpecialAttackFireData = WindStaff_Special_Base.Data;
                    Staff.secondSpecialAttackEnhancedFireData = WindStaff_Special_Enhanced.Data;
                }

                List<WeaponAddon> Addons = [];
                WeaponAddonCommon_StatusUnsafe Effect = Weapon.gameObject.AddComponent<WeaponAddonCommon_StatusUnsafe>();
                Effect.parent = Weapon;
                Effect.effectText = new LocalizedString("ModWeapon_WindStaff_Effect");
                Addons.Add(Effect);
                Weapon.addons = Addons.ToArray();
            }),

            new ModWeapon("Oars", WeaponIDs.Oars, WeaponIDs.StaffOfEmbers)
            .SetEnhanceFrom(WeaponIDs.WindStaff)
            .SetSprite("StaffFire_Tier2", "ModWeapon_Oars", "Weapon")
            .SetSprite("StaffFire_Tier2_In", "ModWeapon_Oars_1", "Weapon")
            .SetBasicAttacks(WindStaff_Basic.ToArray())
            .SetSpecialAttack(0, WindStaff_Special)
            .SetDashAttack(0, WindStaff_Dash)
            .CustomCreateWeapon(Weapon =>
            {
                if (Weapon is WeaponSimple_QuartterStaff Staff)
                {
                    Staff.secondSpecialAttackFireData = WindStaff_Special_Base.Data;
                    Staff.secondSpecialAttackEnhancedFireData = WindStaff_Special_Enhanced.Data;
                }

                List<WeaponAddon> Addons = [];
                WeaponAddonCommon_StatusUnsafe Effect = Weapon.gameObject.AddComponent<WeaponAddonCommon_StatusUnsafe>();
                Effect.parent = Weapon;
                Effect.status =
                [
                    new() {
                        statId = TextFile.WindErosionOverFlowAdd,
                        value = 20,
                        displayMultiplier = 1f,
                        displaySymbol = "",
                    },
                    new() {
                        statId = TextFile.WindErosionOverFlowBase,
                        value = 80,
                        displayMultiplier = 1f,
                        displaySymbol = "",
                    },
                    new() {
                        statId = TextFile.WeaponRange,
                        value = 30,
                        displayMultiplier = 1f,
                        displaySymbol = "",
                    },
                ];
                Effect.effectText = new LocalizedString("ModWeapon_WindStaff_Effect");
                Addons.Add(Effect);

                WeaponAddon_DeBuffAttack Effect2 = Weapon.gameObject.AddComponent<WeaponAddon_DeBuffAttack>();
                Effect2.parent = Weapon;
                Effect2.debuffPrefab = WindErosion.Prefab;
                Effect2.effectText = new LocalizedString("ModWeapon_Oars_Effect");
                Addons.Add(Effect2);
                Weapon.addons = Addons.ToArray();
            }),
        ];
    }
}