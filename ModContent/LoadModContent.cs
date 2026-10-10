using HarmonyLib;
using Mirror;
using SPMod.Tool;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static ComboEffectBase;
using static Miracle_StatusInstance;
using static SPMod.Tool.EffectConstant;
using StatusGroup = Charm_StatusInstance.StatusGroup;
using static SPMod.Tool.ModuleComponents;
using System;

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
        //模组根之退
        public static List<HardModeShardEntity> ModHardModeShard = new List<HardModeShardEntity>()
        {
            new ModHardModeShardEntity("BossRushMode", "[Star's Sephiria Mod]BossRushMode", EHardModeShardType.World, 1, new int[]{ 1 }, new int[]{ 0 }).EndCreate(),
            new ModHardModeShardEntity("InfiniteMode", "[Star's Sephiria Mod]InfiniteMode", EHardModeShardType.World, 1, new int[]{ 1 }, new int[]{ 0 }).EndCreate(),
            new ModHardModeShardEntity("HardMode", "[Star's Sephiria Mod]HardMode", EHardModeShardType.World, 3, new int[]{ 1, 2, 3 }, new int[]{ 5, 10, 15 }).EndCreate(),
            new ModHardModeShardEntity("PollutedBlood", "[Star's Sephiria Mod]PollutedBlood", EHardModeShardType.Plan, 1, new int[]{ 50 }, new int[]{ 3 }).EndCreate(),
            new ModHardModeShardEntity("EvilGuardian", "[Star's Sephiria Mod]EvilGuardian", EHardModeShardType.Demon, 1, new int[]{ 40 }, new int[]{ 4 }).EndCreate(),
        };
        //模组奇迹
        public static List<ModMiracle> ModMiraclesData = new List<ModMiracle>()
        {
            new ModMiracle("Shaman", "CURSE").SetStat("DEBUFF_DAMAGE/20=Positive", "DEBUFF_DURATION/40=Positive").EndCreate(),
            new ModMiracle("Negotiator", "SAVVY").SetStat("NEGOTIATION/10=Positive", "LEAF_DROP/40=Positive").EndCreate(),
            new ModMiracle("Knight", "WEAPON").SetStat("BASIC_ATTACK_DAMAGE/20=Positive", "SPECIAL_ATTACK_DAMAGE/20=Positive").EndCreate(),
        };
        public static Dictionary<string, List<StatInfo>> ModfyisMiracleStatus = new Dictionary<string, List<StatInfo>>()
        {
            { "General", Tools.SetStatInfo("FOLLOWER_ATTACK_SPEED/35=Positive") },//将军
            { "Astronomer", Tools.SetStatInfo("PLANET_ATTACK_SPEED/30=Positive") },//天文学家
            { "Meteorologist", Tools.SetStatInfo("LIGHTNING_DAMAGE/8=Positive", "DARK_CLOUD_DAMAGE/30=Positive") },//气象学家
            { "Guard", Tools.SetStatInfo("FINAL_DAMAGE/12=Positive", "DEFENSE/12=Positive") },//警卫
            { "Elementalist", Tools.SetStatInfo("HIGHEST_ELEMENTAL_DAMAGE/8=Positive") },//元素师
            { "Assassin", Tools.SetStatInfo("PHYSICAL_DAMAGE/6=Positive", "CRITICAL_DAMAGE_RATE/24=Positive") },//刺客
            { "Duelist", Tools.SetStatInfo("ATTACK_SPEED/30=Positive", "WEAPON_RANGE/10=Positive") },//决斗家
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
        //模组连击
        public static List<ItemCategoryEntity> ModItemCategoryData = new List<ItemCategoryEntity>()
        {
            new ModItemCategory("COMET", "Comet", "StarApple")
            .SetStat(new ComboStat() { comboCount = 2, status = new string[2] { "DASH_RECOVERY_SPEED/10", "DASH_ATTACK_DAMAGE/20" } })
            .SetStat(new ComboStat() { comboCount = 4, status = new string[2] { "DASH_COUNT/1", "DASH_ATTACK_DAMAGE/20" } })
            .UseStat().EndCreate(),
            new ModItemCategory("FORTUNE", "Fortune", "Pomegranate")
            .SetStat(new ComboStat() { comboCount = 2, status = new string[1] { "BOSS_REWARD_DICE/1" } })
            .SetStat(new ComboStat() { comboCount = 4, status = new string[1] { "EXTRA_ITEM_CHOICES/1" } })
            .UseStat().EndCreate(),
        };
        //模组关键词
        public static List<ModKeyWord> ModKeywordEntityData = new List<ModKeyWord>()
        {
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
            new ModKeyWord("Stat_DrinkPotionStack", false, Color.white).CreateStatus("DRINK_POTION_STACK"),
            new ModKeyWord("Stat_StrikeBack", true, DefenseColor).UseKeyWordSprite("Thorns").CreateStatus("STRIKE_BACK", "%"),
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
            new ModKeyWord("Stealth", true, WeaponColor),
        };
        //模组HUD
        public static List<ModEffectHUD> ModEffectHUDData = new List<ModEffectHUD>()
        {
            new ModEffectHUD("SpearofZeus", false, false),
            new ModEffectHUD("Rosion", true, true),
            new ModEffectHUD("Charge", true, true),
            new ModEffectHUD("ThunderBeast", false, true),
        };
        //模组物品
        public static List<int> DisableCharm = new List<int>
        {
            1012,// 龙骨碎片
            1034,// 盾牌伙伴
            1035,// 云瓶
            1056,// 青铜镜碎片
            1058,// 图书馆分针微型模型
            1078,//人工精灵伊菲尔
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
        };//隐藏原版物品
        public static Dictionary<int, bool> ModfyisItemUniqueEffect = new Dictionary<int, bool>()
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
        public static Dictionary<int, EItemRarity> ModfyisItemRarity = new Dictionary<int, EItemRarity>()
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
        public static Dictionary<int, List<StatusGroup>> ModfyisItemStatusInstance = new Dictionary<int, List<StatusGroup>>()
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
            { 1162, new List<StatusGroup> { Tools.SetStatusGroup("HIGHEST_ELEMENTAL_DAMAGE", 2, 4) } }, // 诱惑的鱼饵
            { 1163, new List<StatusGroup> { Tools.SetStatusGroup("EVASION", 500, 1000, 1500, 2000) } }, // 魔法眼镜
            { 1164, new List<StatusGroup>
                    {
                        Tools.SetStatusGroup("MAX_HP", 3, 6, 9, 12),
                        Tools.SetStatusGroup("CRITICAL_DAMAGE_RATE", 5, 10, 15, 20)
                    }
            }, // 稻草人偶
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
        public static Dictionary<string, ItemEntity> GetModItem = new Dictionary<string, ItemEntity>();
        public static Dictionary<string, Action<ItemEntity>> GetModAction = new Dictionary<string, Action<ItemEntity>>();
        public static List<ItemEntity> ModItemEntityData = new List<ItemEntity>()
        {
            new ModItem("BoxingGloves", EItemRarity.Common).CreateItem()
            .CreateCharm(Tools.SetCategories("STURDY"), new List<StatusGroup>()
            {
                Tools.SetStatusGroup("PHYSICAL_DAMAGE", 2, 4),
                Tools.SetStatusGroup("DEFENSE", 2, 4)
            }, false),//拳击手套

            new ModItem("SwordOfKings", EItemRarity.Legend).CreateItem()
            .CreateCharm(Tools.SetCategories("STURDY"), new List<StatusGroup>()
            {
                Tools.SetStatusGroup("DIRECT_WEAPON_DAMAGE", 8, 16, 24, 32, 40)
            }),//王者之剑

            new ModItem("MeteoricIronMedal", EItemRarity.Legend).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("FLAMESWORD"), Action =>
            {
                Charm_FlameSwordAutoAttack Effect = Action.resourcePrefab.AddComponent<Charm_FlameSwordAutoAttack>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[1] { new LocalizedString("ModCharmEffect_FlameSwordAutoAttack") };
                Effect.stats = new StatusGroup[] { Tools.SetStatusGroup("FLAME_SWORD_MAX", 2, 3, 4, 5, 6) };
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//陨铁勋章

            new ModItem("MidasRing", EItemRarity.Rare).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("SAVVY"), Action =>
            {
                Charm_GoldDamage Effect = Action.resourcePrefab.AddComponent<Charm_GoldDamage>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[1] { new LocalizedString("ModCharmEffect_GoldDamage") };
                Effect.stats = new StatusGroup[] { Tools.SetStatusGroup("FINAL_DAMAGE", 8, 12, 16) };
                Effect.maxLevel = 2;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//迈达斯的黄金手镯

            new ModItem("TaxTreaty", EItemRarity.Legend).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("SAVVY"), Action =>
            {
                Charm_ReturnGold Effect = Action.resourcePrefab.AddComponent<Charm_ReturnGold>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[1] { new LocalizedString("ModCharmEffect_ReturnGold") };
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//税收协定

            new ModItem("WindSpriteHairWax", EItemRarity.Legend).CreateItem()
            .CreateCharm(Tools.SetCategories("COMET"), new List<StatusGroup>()
            {
                Tools.SetStatusGroup("DASH_COUNT", 1, 1, 2, 2, 2),
                Tools.SetStatusGroup("DASH_INVINCIBLE_TIME_BONUS", 0, 0, 0, 0, 100),
                Tools.SetStatusGroup("DASH_RECOVERY_SPEED", 6, 12, 18, 24, 30)
            }),//风精灵的发蜡

            new ModItem("MasterBracelet", EItemRarity.Rare).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("WEAPON"), Action =>
            {
                Charm_InvincibleDefense Effect = Action.resourcePrefab.AddComponent<Charm_InvincibleDefense>();
                Effect.isWeaponRelatedCharm = true;
                Effect.relatedWeapon = EWeaponType.Staff;
                Effect.effectsString = new LocalizedString[1] { new LocalizedString("ModCharmEffect_InvincibleDefense") };
                Effect.maxLevel = 3;
                Effect.stats = new List<StatusGroup>()
                {
                    Tools.SetStatusGroup("BASIC_ATTACK_DAMAGE", 5, 10, 15, 20),
                    Tools.SetStatusGroup("SPECIAL_ATTACK_COST_REDUCTION", 10, 20, 30, 40)
                }.ToArray();
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//大师手镯

            new ModItem("RedDogSymbolA", EItemRarity.Rare).SetCost(1000).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("COMPANION"), Action =>
            {
                Action.aName =  new LocalizedString("Item_WeaselKnight_Name");
                Action.aFlavorText = new LocalizedString("Item_WeaselKnight_FlavorText");
                Action.activeType = EItemActiveType.Hidden;
                Charm_LeadNPC Effect = Action.resourcePrefab.AddComponent<Charm_LeadNPC>();
                Effect.isWeaponRelatedCharm = false;
                Effect.maxLevel = 2;
                int Value = 75;
                Effect.hpByLevel = new int[]{ Value.IntMult(5f), Value.IntMult(5.5f), Value.IntMult(6f) };
                Value = 135;
                Effect.levelBonusByLevel = new int[] { Value.IntMult(5f), Value.IntMult(6f), Value.IntMult(7f) };
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//超级科林

            new ModItem("RedDogSymbolB", EItemRarity.Rare).SetCost(1500).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("COMPANION"), Action =>
            {
                Action.aName =  new LocalizedString("Item_WeaselKnight_Name");
                Action.aFlavorText = new LocalizedString("Item_WeaselKnight_FlavorText");
                Action.activeType = EItemActiveType.Hidden;
                Charm_LeadNPC Effect = Action.resourcePrefab.AddComponent<Charm_LeadNPC>();
                Effect.isWeaponRelatedCharm = false;
                Effect.maxLevel = 2;
                int Value = 75;
                Effect.hpByLevel = new int[]{ Value.IntMult(10f), Value.IntMult(11f), Value.IntMult(12f) };
                Value = 135;
                Effect.levelBonusByLevel = new int[] { Value.IntMult(10f), Value.IntMult(12f), Value.IntMult(14f) };
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//超级科林2

            new ModItem("RedDogSymbolC", EItemRarity.Rare).SetCost(2000).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("COMPANION"), Action =>
            {
                Action.aName =  new LocalizedString("Item_WeaselKnight_Name");
                Action.aFlavorText = new LocalizedString("Item_WeaselKnight_FlavorText");
                Action.activeType = EItemActiveType.Hidden;
                Charm_LeadNPC Effect = Action.resourcePrefab.AddComponent<Charm_LeadNPC>();
                Effect.effectsString = new LocalizedString[] { new LocalizedString("Charm_WeaselKnight_Effect") };
                Effect.isWeaponRelatedCharm = false;
                Effect.maxLevel = 2;
                int Value = 75;
                Effect.hpByLevel = new int[]{ Value.IntMult(15f), Value.IntMult(16.5f), Value.IntMult(18f) };
                Value = 135;
                Effect.levelBonusByLevel = new int[] { Value.IntMult(15f), Value.IntMult(18f), Value.IntMult(21f) };
                Effect.defaultReviveTimer = new Timer(18f);
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//超级科林3

            new ModItem("CurseNail", EItemRarity.Legend).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("CURSE"), Action =>
            {
                Charm_SuperDebuffDamage Effect = Action.resourcePrefab.AddComponent<Charm_SuperDebuffDamage>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[1] { new LocalizedString("ModCharmEffect_SuperDebuffDamage") };
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//诅咒之钉

            new ModItem("HeartofElements", EItemRarity.Rare).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("ELEMENTAL"), Action =>
            {
                Charm_ElementAllocation Effect = Action.resourcePrefab.AddComponent<Charm_ElementAllocation>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[1] { new LocalizedString("ModCharmEffect_ElementAllocation") };
                Effect.stats = new List<StatusGroup>()
                {
                    Tools.SetStatusGroup("HIGHEST_ELEMENTAL_DAMAGE", 2, 4, 6)
                }.ToArray();
                Effect.maxLevel = 2;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//元素之心

            new ModItem("ShadowGuardian", EItemRarity.Legend).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("SHADOW"), Action =>
            {
                Charm_DodgeShield Effect = Action.resourcePrefab.AddComponent<Charm_DodgeShield>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[1] { new LocalizedString("ModCharmEffect_DodgeShield") };
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//暗影守护者

            new ModItem("ScoreSymphony", EItemRarity.Legend).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("WINDSONG"), Action =>
            {
                Charm_ScoreCountAttackSpeed Effect = Action.resourcePrefab.AddComponent<Charm_ScoreCountAttackSpeed>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[1] { new LocalizedString("ModCharmEffect_ScoreCountAttackSpeed") };
                Effect.maxLevel = 2;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//乐谱"交响曲"

            new ModItem("PureWhiteCore", EItemRarity.Legend).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("LAKE"), Action =>
            {
                Charm_CustomStatus Effect = Action.resourcePrefab.AddComponent<Charm_CustomStatus>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[1] { new LocalizedString("ModCharmEffect_MPRegenDamage") };
                Effect.ValueByLevel = new int[] { 3, 2, 1 };
                Effect.ValueName = TextFile.MPRegenToDamage;
                Effect.stats = new List<StatusGroup>()
                {
                    Tools.SetStatusGroup("MP_SKILL_DAMAGE", 5)
                }.ToArray();
                Effect.maxLevel = 2;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//纯白核心

            new ModItem("SolarProminence").CreateItem(true)
            .CustomCreateCharm(Tools.SetCategories("EMBER", "FLAMESWORD"), Action =>
            {
                Charm_BurnFlameSword Effect = Action.resourcePrefab.AddComponent<Charm_BurnFlameSword>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[1] { new LocalizedString("ModCharmEffect_BurnFlameSword") };
                Effect.stats = new List<StatusGroup>()
                {
                    Tools.SetStatusGroup("FIRE_DAMAGE", 2, 4, 6, 8, 10),
                    Tools.SetStatusGroup("BURN_SPEED", 10, 20, 30, 40, 50)
                }.ToArray();
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//日珥

            new ModItem("GrowthFund").CreateItem(true)
            .CustomCreateCharm(Tools.SetCategories("FORTUNE", "SAVVY"), Action =>
            {
                Charm_CustomStatus Effect = Action.resourcePrefab.AddComponent<Charm_CustomStatus>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[1] { new LocalizedString("ModCharmEffect_CoinToExp") };
                Effect.ValueByLevel = new int[] { 1 };
                Effect.ValueName = TextFile.CoinToExp;
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//成长基金

            new ModItem("BumpyFate").CreateItem(true)
            .CustomCreateCharm(Tools.SetCategories("GUARDIAN", "SHADOW"), Action =>
            {
                Charm_DodgeThorns Effect = Action.resourcePrefab.AddComponent<Charm_DodgeThorns>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[1] { new LocalizedString("ModCharmEffect_DodgeThorns") };
                Effect.stats = new List<StatusGroup>()
                {
                    Tools.SetStatusGroup("THORNS", 50, 100, 150, 200),
                    Tools.SetStatusGroup("STRIKE_BACK", 50, 100, 150, 200)
                }.ToArray();
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//命运坎坷

            new ModItem("FrostyAnvil").CreateItem(true)
            .CustomCreateCharm(Tools.SetCategories("GLACIER", "FROST"), Action =>
            {
                Charm_FrozenFrostRelic Effect = Action.resourcePrefab.AddComponent<Charm_FrozenFrostRelic>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_FrozenFrostRelic") };
                Effect.Ending = "%";
                Effect.ValueName = TextFile.FrostRelicAddMult;
                Effect.ValueByLevel = new int[]{ 30, 60, 90, 120, 150, 180, 210, 240 };
                Effect.maxLevel = 7;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//寒霜铁砧

            new ModItem("AmplifierComponent").CreateItem(true)
            .CustomCreateCharm(Tools.SetCategories("PRECISION", "MYSTIC"), Action =>
            {
                Charm_IgnoreDefensiveCrit Effect = Action.resourcePrefab.AddComponent<Charm_IgnoreDefensiveCrit>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_IgnoreDefensiveCrit") };
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//增幅器组件

            new ModItem("HolyGuardian", EItemRarity.Common).CreateItem()
            .CreateCharm(Tools.SetCategories("PARTY"), new List<StatusGroup>()
            {
                Tools.SetStatusGroup("ALL_PLAYER_DEFENSE", 2, 4),
            }, false),//神圣守护

            new ModItem("Support", EItemRarity.Uncommon).CreateItem()
            .CreateCharm(Tools.SetCategories("PARTY"), new List<StatusGroup>()
            {
                Tools.SetStatusGroup("ALL_PLAYER_ELEMENTAL_DAMAGE", 1, 2, 3, 4),
            }, false),//支援

            new ModItem("NaptimePillow", EItemRarity.Uncommon).CreateItem()
            .CreateCharm(Tools.SetCategories("PARTY"), new List<StatusGroup>()
            {
                Tools.SetStatusGroup("MAX_HP", 10, 20, 30, 40),
                Tools.SetStatusGroup("REVIVE_PLAYER_HASTE", 20)
            }),//爱心睡眠抱枕

            new ModItem("WeaknessIndicator", EItemRarity.Rare).CreateItem()
            .CreateCharm(Tools.SetCategories("PARTY"), new List<StatusGroup>()
            {
                Tools.SetStatusGroup("ALL_PLAYER_CRIT", 3, 6, 9, 12, 15),
            }),//弱点标记

            new ModItem("UnityFlag", EItemRarity.Legend).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("PARTY"), Action =>
            {
                Charm_MultiPlayerAddStatus Effect = Action.resourcePrefab.AddComponent<Charm_MultiPlayerAddStatus>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_MultiPlayerAddStatus") };
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//团结旗帜

            new ModItem("ArtificialSpiritIfiel", EItemRarity.Rare).CreateItem()
            .CustomCreateCharm(new List<string>() { }, Action =>
            {
                Charm_ShootBack Effect = Action.resourcePrefab.AddComponent<Charm_ShootBack>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_ShootBack") };
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//人工精灵伊菲尔

            new ModItem("UnhallowedRitePaper", EItemRarity.Uncommon).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("COMPANION"), Action =>
            {
                Charm_BodyBomb Effect = Action.resourcePrefab.AddComponent<Charm_BodyBomb>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_BodyBomb") };
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//邪祭咒纸

            new ModItem("DevilPact", EItemRarity.Legend).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("COMPANION"), Action =>
            {
                Charm_SummonUnit Effect = Action.resourcePrefab.AddComponent<Charm_SummonUnit>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_DevilPact") };
                Effect.followerDamageId = "[Star's Sephiria Mod]恶魔契约";
                Effect.damageByLevel = new int[] { 64, 128, 192, 256, 320 };
                Effect.hpByLevel = new int[] { 40, 80, 120, 160, 200 };
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
                component.miniFaceChipSprite = Tools.LoadEmbeddedSprite("Item_DevilPact_MiniIcon");
            }),//恶魔契约

            new ModItem("BottledLightning").CreateItem(true)
            .CustomCreateCharm(Tools.SetCategories("MAGITECH", "DARKCLOUD"), Action =>
            {
                Charm_ElectricDarkCloud Effect = Action.resourcePrefab.AddComponent<Charm_ElectricDarkCloud>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_ElectricDarkClouds") };
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//瓶装闪电

            new ModItem("BluePointer", EItemRarity.Legend).CreateItem()
            .CustomCreateCharm(new List<string>() { }, Action =>
            {
                CharmStatusInheritance Effect = Action.resourcePrefab.AddComponent<CharmStatusInheritance>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_CharmStatusInheritance") };
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//指北的蓝色针

            new ModItem("SolisThunder").CreateItem(true)
            .CustomCreateCharm(Tools.SetCategories("FLAMESWORD", "DARKCLOUD"), Action =>
            {
                Charm_ThunderFireSword Effect = Action.resourcePrefab.AddComponent<Charm_ThunderFireSword>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_ThunderFireSword") };
                Effect.stats = new List<StatusGroup>()
                {
                    Tools.SetStatusGroup("LIGHTNING_DAMAGE", 2, 4, 6, 8),
                    Tools.SetStatusGroup("FLAME_SWORD_DAMAGE", 5, 10, 15, 20)
                }.ToArray();
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//索利斯·桑德

            new ModItem("SpearofZeus").CreateItem(true)
            .CustomCreateCharm(Tools.SetCategories("FROST", "DARKCLOUD"), Action =>
            {
                Charm_SpearofZeus Effect = Action.resourcePrefab.AddComponent<Charm_SpearofZeus>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_SpearofZeus") };
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

            new ModItem("SecretCloak", EItemRarity.Legend).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("SHADOW"), Action =>
            {
                Charm_SilentStrike Effect = Action.resourcePrefab.AddComponent<Charm_SilentStrike>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[1] { new LocalizedString("ModCharmEffect_SilentStrike") };
                Effect.stats = new List<StatusGroup>()
                {
                    Tools.SetStatusGroup("EVASION", 300, 600, 900, 1200, 1500)
                }.ToArray();
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//隐秘斗篷

            new ModItem("Caliburn", EItemRarity.Legend).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("LAKE"), Action =>
            {
                Charm_Caliburn Effect = Action.resourcePrefab.AddComponent<Charm_Caliburn>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[1] { new LocalizedString("ModCharmEffect_Caliburn") };
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//石中剑

            new ModItem("Excalibur", EItemRarity.Legend).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("LAKE"), Action =>
            {
                Action.activeType = EItemActiveType.Hidden;
                Charm_Excalibur Effect = Action.resourcePrefab.AddComponent<Charm_Excalibur>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[1] { new LocalizedString("ModCharmEffect_Excalibur") };
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//湖中剑

            new ModItem("LuckyCoin").CreateItem(true)
            .CustomCreateCharm(Tools.SetCategories("FORTUNE", "PRECISION"), Action =>
            {
                Charm_LuckCrit Effect = Action.resourcePrefab.AddComponent<Charm_LuckCrit>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_LuckCrit") };
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//幸运硬币

            new ModItem("AlchemistsFlask", EItemRarity.Rare).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("ALCHEMY"), Action =>
            {
                Charm_Alchemy Effect = Action.resourcePrefab.AddComponent<Charm_Alchemy>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[1] { new LocalizedString("ModCharmEffect_Alchemy") };
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//炼金术士的烧瓶

            new ModItem("PhilosopherStone", EItemRarity.Legend).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("ALCHEMY"), Action =>
            {
                Charm_Sage Effect = Action.resourcePrefab.AddComponent<Charm_Sage>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_Sage") };
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//贤者之石

            new ModItem("DeepSpaceObserver", EItemRarity.Legend).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("PLANET"), Action =>
            {
                Charm_Telescope Effect = Action.resourcePrefab.AddComponent<Charm_Telescope>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_Telescope") };
                Effect.maxLevel = 1;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//深空观测者

            new ModItem("KeelFrgment", EItemRarity.Uncommon).CreateItem()
            .CreateCharm(Tools.SetCategories("MYSTIC"), new List<StatusGroup>()
            {
                Tools.SetStatusGroup("MAX_HP", 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30),
                Tools.SetStatusGroup("FINAL_DAMAGE", 3, 6, 9, 12, 15, 18, 21, 24, 27, 30, 33, 36, 39, 42, 45)
            }),//龙骨碎片

            new ModItem("Amber", EItemRarity.Uncommon).CreateItem()
            .CreateCharm(Tools.SetCategories("MYSTIC"), new List<StatusGroup>()
            {
                Tools.SetStatusGroup("DEFENSE", 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30),
                Tools.SetStatusGroup("TRUE_DAMAGE", 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8)
            }),//琥珀

            new ModItem("WhiteEggShell", EItemRarity.Rare).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("MYSTIC"), Action =>
            {
                Charm_IgnoreDefensiveDamage Effect = Action.resourcePrefab.AddComponent<Charm_IgnoreDefensiveDamage>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_IgnoreDefensiveDamage") };
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//白蛋壳

            new ModItem("LightningBottle", EItemRarity.Uncommon).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("DARKCLOUD"), Action =>
            {
                Charm_EXLightningBottle Effect = Action.resourcePrefab.AddComponent<Charm_EXLightningBottle>();
                Effect.isWeaponRelatedCharm = false;
                Effect.maxLevel = 3;
                Effect.stats = new StatusGroup[] { Tools.SetStatusGroup("LIGHTNING_DAMAGE", 1, 2, 3, 4) };
                Effect.cloudByLevel = new int[4] { 1, 2, 3, 4 };
                Effect.cooldownByLevel = new float[] { 1f };
                Effect.effectsString = new LocalizedString[] { new LocalizedString("Charm_LightningPouch_Effect"), new LocalizedString("ModCharmEffect_LightningPouch") };
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = false;
                Effect.enabled = false;
            }),//云瓶

            new ModItem("Valiya", EItemRarity.Rare).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("WEAPON"), Action =>
            {
                Charm_InvincinleSpiceialAttack Effect = Action.resourcePrefab.AddComponent<Charm_InvincinleSpiceialAttack>();
                Effect.isWeaponRelatedCharm = true;
                Effect.relatedWeapon = EWeaponType.GreatSword;
                Effect.effectsString = new LocalizedString[]
                {
                    new LocalizedString("Charm_SweepRange_Enhanced_Effect2"),
                    new LocalizedString("Charm_SweepRange_Enhanced_Effect"),
                    new LocalizedString("ModCharmEffect_InvincinleSpecialAttack")
                };
                Effect.sweepRangeByLevel = new int[3] { 20, 40, 60 };
                Effect.sweepDamageByLevel = new int[3] { 10, 20, 30 };
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

            new ModItem("ShieldCrest", EItemRarity.Rare).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("WEAPON"), Action =>
            {
                Charm_AddPerfectGuardTime Effect = Action.resourcePrefab.AddComponent<Charm_AddPerfectGuardTime>();
                Effect.isWeaponRelatedCharm = true;
                Effect.relatedWeapon = EWeaponType.SwordAndShield;
                Effect.effectsString = new LocalizedString[]
                {
                    new LocalizedString("Charm_SwordShieldGrowth_Enhanced_Effect"),
                    new LocalizedString("ModCharmEffect_AddPerfectGuardTime")
                };
                Effect.guardCostReductionByLevel = new int[] { 5, 10, 15, 20 };
                Effect.guardRangeBonusByLevel = new int[0];
                Effect.stats = new StatusGroup[]
                {
                    Tools.SetStatusGroup("SPECIAL_ATTACK_DAMAGE", 10, 20, 30, 40),
                    Tools.SetStatusGroup("SPECIAL_ATTACK_COST_REDUCTION", 20, 40, 60, 80),
                };
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

             new ModItem("Epaulette", EItemRarity.Uncommon).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("STURDY"), Action =>
            {
                Charm_GrowthParry Effect = Action.resourcePrefab.AddComponent<Charm_GrowthParry>();
                Effect.isWeaponRelatedCharm = true;
                Effect.relatedWeapon = EWeaponType.Dagger;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("Charm_DaggerGrowth_Enhanced_Effect") };
                Effect.parryDamageByLevel = new int[] { 100, 200, 300 };
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

            new ModItem("KatanaClash", EItemRarity.Rare).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("WEAPON"), Action =>
            {
                Charm_GrowthKatana Effect = Action.resourcePrefab.AddComponent<Charm_GrowthKatana>();
                Effect.isWeaponRelatedCharm = true;
                Effect.relatedWeapon = EWeaponType.Katana;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("Charm_KatanaGrowth_Enhanced_Effect") };
                Effect.sheathInvincibleBonusPercentByLevel = new int[] { 12, 24, 36, 48, 60 };
                Effect.stats = new StatusGroup[]
                {
                    Tools.SetStatusGroup("BASIC_ATTACK_DAMAGE", 4, 8, 12, 16, 20),
                    Tools.SetStatusGroup("SPECIAL_ATTACK_DAMAGE", 4, 8, 12, 16, 20),
                    Tools.SetStatusGroup("ATTACK_SPEED", 4, 8, 12, 16, 20),
                };
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

            new ModItem("BoltHead", EItemRarity.Rare).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("WEAPON"), Action =>
            {
                Charm_InvincinleForReLoad Effect = Action.resourcePrefab.AddComponent<Charm_InvincinleForReLoad>();
                Effect.isWeaponRelatedCharm = true;
                Effect.relatedWeapon = EWeaponType.Crossbow;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_InvincinleForReLoad") };
                Effect.stats = new StatusGroup[]
                {
                    Tools.SetStatusGroup("BASIC_ATTACK_DAMAGE", 2, 4, 6, 8, 8, 10, 12, 12),
                    Tools.SetStatusGroup("CROSSBOW_RELOADSPEED", 0, 20, 40, 60, 60, 80, 100, 100),
                    Tools.SetStatusGroup("CROSSBOW_ADDITIONAL_MAGAZINE", 0, 0, 0, 0, 1, 1, 1, 2)
                };
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

            new ModItem("EcologicalSphere").CreateItem(true)
            .CustomCreateCharm(Tools.SetCategories("PLANET", "LAKE"), Action =>
            {
                Charm_CustomStatus Effect = Action.resourcePrefab.AddComponent<Charm_CustomStatus>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_EcologicalSphere") };
                Effect.ValueByLevel = new int[] { 15, 20, 25, 30 };
                Effect.ValueName = TextFile.EcologicalSphereEffect;
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//生态球

            new ModItem("CometaryHydrogenCorona").CreateItem(true)
            .CustomCreateCharm(Tools.SetCategories("COMET", "GUARDIAN"), Action =>
            {
                Charm_DashShield Effect = Action.resourcePrefab.AddComponent<Charm_DashShield>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_DashShield") };
                Effect.maxLevel = 4;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//氢冕

            new ModItem("CounterAttackBlade", EItemRarity.Uncommon).CreateItem()
            .CreateCharm(Tools.SetCategories("SHADOW"), new List<StatusGroup>()
            {
                Tools.SetStatusGroup("PHYSICAL_DAMAGE", 1, 2, 3, 4),
                Tools.SetStatusGroup("EVASION", 200, 400, 600, 800),
                Tools.SetStatusGroup("STRIKE_BACK", 50, 100, 150, 200)
            }, false),//反击之刃

            new ModItem("ShadowWarrior").CreateItem(true)
            .CustomCreateCharm(Tools.SetCategories("STURDY", "SHADOW"), Action =>
            {
                Charm_ActivelyRetaliate Effect = Action.resourcePrefab.AddComponent<Charm_ActivelyRetaliate>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_ActivelyRetaliate") };
                Effect.stats = new StatusGroup[]
                {
                    Tools.SetStatusGroup("STRIKE_BACK", 50, 100, 150, 200)
                };
                Effect.maxLevel = 3;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//影武者

            new ModItem("ChaosGlyph", EItemRarity.Uncommon).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("ACADEMY"), Action =>
            {
                Charm_CharosMagic Effect = Action.resourcePrefab.AddComponent<Charm_CharosMagic>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_CharosMagic") };
                Effect.stats = new StatusGroup[]
                {
                    Tools.SetStatusGroup("MAGIC_DAMAGE_BONUS", 5, 10)
                };
                Effect.maxLevel = 1;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//混沌符文

            new ModItem("ArcaneBookMark", EItemRarity.Rare).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("ACADEMY"), Action =>
            {
                Charm_MagicBookAddDamage Effect = Action.resourcePrefab.AddComponent<Charm_MagicBookAddDamage>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_MagicBookAddDamage") };
                Effect.maxLevel = 2;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//奥术书签

            new ModItem("BondVessel", EItemRarity.Rare).CreateItem()
            .CustomCreateCharm(new List<string>(), Action =>
            {
                Charm_BondVessel Effect = Action.resourcePrefab.AddComponent<Charm_BondVessel>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_BondVessel") };
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = false;
                Effect.enabled = false;
            }),//羁绊容器

            new ModItem("ElectricIce").CreateItem(true)
            .CustomCreateCharm(Tools.SetCategories("MAGITECH", "GLACIER"), Action =>
            {
                Charm_CustomStatus Effect = Action.resourcePrefab.AddComponent<Charm_CustomStatus>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[1] { new LocalizedString("ModCharmEffect_ElectricIce") };
                Effect.ValueByLevel = new int[] { 1 };
                Effect.ValueName = TextFile.ElectricIce;
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//电冰体

            new ModItem("EnergyRecoveryDevice", EItemRarity.Uncommon).CreateItem()
            .CustomCreateCharm(new List<string>(), Action =>
            {
                Charm_RemoveArtifact Effect = Action.resourcePrefab.AddComponent<Charm_RemoveArtifact>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_RemoveArtifact") };
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//能量回收装置

            new ModItem("CommemorativeCoin", EItemRarity.Uncommon).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("SAVVY"), Action =>
            {
                Charm_ChangeMoney Effect = Action.resourcePrefab.AddComponent<Charm_ChangeMoney>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_CommemorativeCoin") };
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//纪念币

            new ModItem("PotionTransactionProof").CreateItem(true)
            .CustomCreateCharm(Tools.SetCategories("SAVVY", "ALCHEMY"), Action =>
            {
                Charm_PotionTransactionProof Effect = Action.resourcePrefab.AddComponent<Charm_PotionTransactionProof>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[1] { new LocalizedString("ModCharmEffect_PotionTransactionProof") };
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//药水交易凭证

            new ModItem("CounterAttackTacticsGuide").CreateItem(true)
            .CustomCreateCharm(Tools.SetCategories("COMPANION", "SHADOW"), Action =>
            {
                Charm_CustomStatus Effect = Action.resourcePrefab.AddComponent<Charm_CustomStatus>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[1] { new LocalizedString("ModCharmEffect_CounterAttackTacticsGuide") };
                Effect.ValueByLevel = new int[] { 12, 24, 36, 48 };
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

            new ModItem("MysteryBox", EItemRarity.Common).CreateItem()
            .CustomCreateCharm(Tools.SetCategories("FORTUNE"), Action =>
            {
                Charm_Lottery Effect = Action.resourcePrefab.AddComponent<Charm_Lottery>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_Lottery") };
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = true;
                Effect.enabled = false;
            }),//神秘盒子

            new ModItem("MysteryBox_Random", EItemRarity.Rare).CreateItem()
            .CustomCreateCharm(new List<string>(), Action =>
            {
                Action.activeType = EItemActiveType.Hidden;
                Charm_Lottery Effect = Action.resourcePrefab.AddComponent<Charm_Lottery>();
                Effect.isWeaponRelatedCharm = false;
                Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_Lottery_Random") };
                Effect.LuckMult = 0.75f;
                Effect.RandomLottery = true;
                Effect.AddStatus = new List<StatusGroup>()
                {
                    Tools.SetStatusGroup("LUCK", 0, 1, 2, 3),
                    Tools.SetStatusGroup("EXP_DROP", 6, 12, 18, 24),
                };
                Effect.maxLevel = 0;
                Effect.isUniqueEffect = false;
                Effect.enabled = false;
            }),//神秘盒子(金)
        };
    }
}