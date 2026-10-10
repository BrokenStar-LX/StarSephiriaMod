using SPMod.ModContent;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static SPMod.Tool.ModuleComponents;

namespace SPMod.Tool
{
    public static class TextFile
    {
        #region 属性文本
        public const string PhysicalDamage = "PHYSICALDAMAGE";                                              // 物理伤害
        public const string FireDamage = "FIREDAMAGE";                                                      // 火焰伤害
        public const string IceDamage = "ICEDAMAGE";                                                        // 冰霜伤害
        public const string LightningDamage = "LIGHTNINGDAMAGE";                                            // 闪电伤害
        public const string TrueDamage = "TRUEDAMAGE";                                                      // 无视防御伤害
        public const string AllDamageBonus = "ALLDAMAGEBONUS";                                              // 所有伤害加成
        public const string HpSteal = "HPSTEAL";                                                            // 生命偷取
        public const string HpRegen = "HPREGEN";                                                            // 生命恢复
        public const string GiveUpRewardDice = "GIVEUPREWARDDICE";                                          // 放弃奖励骰子
        public const string BossRewardDice = "BOSSREWARDDICE";                                              // Boss奖励骰子
        public const string FreeItemReroll = "FREEITEMREROLL";                                              // 抽卡免费刷新
        public const string ExtraItemChoices = "EXTRAITEMCHOICES";                                          // 额外物品选择
        public const string ExtraMiracleChoices = "EXTRAMIRACLECHOICES";                                    // 额外奇迹选择
        public const string ExtraWeaponChoices = "EXTRAWEAPONCHOICES";                                      // 额外武器选择
        public const string Luck = "LUCK";                                                                  // 幸运值
        public const string FruitCount = "FRUITCOUNT";                                                      // 水果串数量上限
        public const string EliteDamage = "ELITEDAMAGE";                                                    // 对Boss伤害加成
        public const string SapphireBonus = "SAPPHIREBONUS";                                                // 蓝宝石掉落增加
        public const string FinalWeaponDamage = "FINALWEAPONDAMAGE";                                        // 武器伤害
        public const string SpecialAttackDamageBonus = "SPECIALATTACKDAMAGEBONUS";                          // 特殊攻击伤害
        public const string BasicAttackDamageBonus = "BASICATTACKDAMAGEBONUS";                              // 普通攻击伤害
        public const string LastBasicAttackDamage = "LASTBASICATTACKDAMAGE";                                // 最后一击造成的伤害
        public const string CrossbowAmmo = "CROSSBOWAMMO";                                                  // 十字弩弹匣容量
        public const string CrossbowAdditionalMagazine = "CROSSBOWADDITIONALMAGAZINE";                      // 十字弩额外弹夹
        public const string SpecialAttackCostReduction = "SPECIALATTACKCOSTREDUCTION";                      // 特殊攻击消耗减少
        public const string CrossbowReloadSpeed = "CROSSBOWRELOADSPEED";                                    // 十字弩装填速度
        public const string SpecialAttackRange = "SPECIALATTACKRANGE";                                      // 特殊攻击范围
        public const string SpecialAttackSpeed = "SPECIALATTACKSPEED";                                      // 特殊攻击速度
        public const string SpecialAttackChargeSpeed = "SPECIALATTACKCHARGESPEED";                          // 旋风斩蓄力速度
        public const string WeaponDamageBonusByDashCount = "WEAPONDAMAGEBONUSBYDASHCOUNT";                  // 冲刺次数转化为武器造成的伤害
        public const string AttackSpeed = "ATTACKSPEED";                                                    // 攻击速度
        public const string WeaponRange = "WEAPONRANGE";                                                    // 武器范围
        public const string DashAttackDamageBonus = "DASHATTACKDAMAGEBONUS";                                // 冲刺攻击伤害
        public const string DaggerTranceBonus = "DAGGERTRANCEBONUS";                                        // 专注获取次数
        public const string DashCount = "DASHCOUNT";                                                        // 冲刺次数
        public const string DashRecovery = "DASHRECOVERY";                                                  // 冲刺恢复速度
        public const string DashInvincibleTimeBonus = "DASHINVINCIBLETIMEBONUS";                            // 冲刺无敌时间
        public const string DashSpeedBonusPercent = "DASHSPEEDBONUSPERCENT";                                // 冲刺速度加成
        public const string InfinityMP = "INFINITYMP";                                                      // 无限魔力
        public const string FinalMP = "FINALMP";                                                            // 最大魔法加成
        public const string MPRegen = "MPREGEN";                                                            // MP回复速度
        public const string MPSteal = "MPSTEAL";                                                            // 吸取MP
        public const string MPSkillDamage = "MPSKILLDAMAGE";                                                // 消耗MP伤害
        public const string MPRegenMultiple = "MPREGENMULTIPLE";                                            // MP恢复效率
        public const string MagicDamageBonus = "MAGICDAMAGEBONUS";                                          // 魔法书伤害
        public const string MagicCritical = "MAGICCRITICAL";                                                // 魔法暴击率
        public const string MagicCriticalDamageBonus = "MAGICCRITICALDAMAGEBONUS";                          // 魔法暴击伤害
        public const string CooldownRecoverySpeed = "COOLDOWNRECOVERYSPEED";                                // 魔法书回复速度
        public const string Evasion = "EVASION";                                                            // 闪避率
        public const string DefenseToAttack = "DEFENSETOATTACK";                                            // 防御转伤害
        public const string DamageReduction = "DAMAGEREDUCTION";                                            // 防御力
        public const string Thorns = "THORNS";                                                              // 荆棘
        public const string Toughness = "TOUGHNESS";                                                        // 韧性
        public const string FlameSwordAdditionalAttack = "FLAMESWORDADDITIONALATTACK";                      // 太阳剑额外触发次数
        public const string FlameSwordAdditionalAttackFromWeapon = "FLAMESWORDADDITIONALATTACKFROMWEAPON";  // 太阳剑额外触发次数-武器
        public const string FlameSwordAdditionalAttackFromMagic = "FLAMESWORDADDITIONALATTACKFROMMAGIC";    // 太阳剑额外触发次数-魔法
        public const string FlameSwordCritical = "FLAMESWORDCRITICAL";                                      // 太阳剑暴击率
        public const string FlameSwordIgnoreDefense = "FLAMESWORDIGNOREDEFENSE";                            // 太阳剑无视防御
        public const string FlameSwordLuck = "FLAMESWORDLUCK";                                              // 太阳剑概率增强
        public const string FlameSwordFastFall = "FLAMESWORDFASTFALL";                                      // 太阳剑下落速度
        public const string FlameSwordMax = "FLAMESWORDMAX";                                                // 太阳剑最大数量
        public const string FlameSwordCriticalDamageRate = "FLAMESWORDCRITICALDAMAGERATE";                  // 太阳剑暴击伤害
        public const string FlameSwordDamage = "FLAMESWORDDAMAGE";                                          // 太阳剑伤害
        public const string FlameSwordPickBonus = "FLAMESWORDPICKBONUS";                                    // 太阳剑额外获得
        public const string Critical = "CRITICAL";                                                          // 暴击率
        public const string CriticalDamageBonus = "CRITICALDAMAGEBONUS";                                    // 暴击伤害
        public const string WeaponCritical = "WEAPONCRITICAL";                                              // 武器暴击率
        public const string WeaponCriticalDamage = "WEAPONCRITICALDAMAGE";                                  // 武器暴击伤害
        public const string IgnoreSuperArmor = "IGNORESUPERARMOR";                                          // 超级护甲穿透
        public const string DarkCloudSpeed = "DARKCLOUDSPEED";                                              // 乌云消耗速度
        public const string DarkCloudRestoreDuringBattle = "DARKCLOUDRESTOREDURINGBATTLE";                  // 战斗中乌云恢复速度
        public const string DarkCloudDamage = "DARKCLOUDDAMAGE";                                            // 乌云伤害
        public const string DarkCloudKeep = "DARKCLOUDKEEP";                                                // 乌云消耗概率
        public const string DarkCloudMultishot = "DARKCLOUDMULTISHOT";                                      // 乌云多重射击
        public const string DarkCloudAtkSpeedBonus = "DARKCLOUDATKSPEEDBONUS";                              // 乌云攻击速度加成
        public const string DarkCloudLuck = "DARKCLOUDLUCK";                                                // 乌云概率强化
        public const string FollowerDamage = "FOLLOWERDAMAGE";                                              // 同伴伤害
        public const string FollowerAttackSpeed = "FOLLOWERATTACKSPEED";                                    // 同伴攻击速度
        public const string FollowerRevive = "FOLLOWERREVIVE";                                              // 同伴复活速度
        public const string FollowerDefense = "FOLLOWERDEFENSE";                                            // 同伴防御
        public const string FollowerCriticalContribute = "FOLLOWERCRITICALCONTRIBUTE";                      // 同伴暴击效率
        public const string BurnSpeed = "BURNSPEED";                                                        // 燃烧速度
        public const string BurnDamage = "BURNDAMAGE";                                                      // 燃烧伤害
        public const string BurnAdd = "BURNADD";                                                            // 燃烧施加次数
        public const string BurnStack = "BURNSTACK";                                                        // 燃烧叠加层数
        public const string PlanetAttackSpeed = "PLANETATTACKSPEED";                                        // 行星攻击速度
        public const string PlanetDamage = "PLANETDAMAGE";                                                  // 行星伤害
        public const string ChargingCharmBonus = "CHARGINGCHARMBONUS";                                      // 冰霜武具充能速度
        public const string AirSlashHaste = "AIRSLASHHASTE";                                                // 祈祷剑鞘充能速度
        public const string VoluspaHaste = "VOLUSPAHASTE";                                                  // 沃尔斯帕充能速度
        public const string FrostRelicDamage = "FROSTRELICDAMAGE";                                          // 冰霜武具伤害
        public const string ChargingCharmAmplify = "CHARGINGCHARMAMPLIFY";                                  // 冰霜武具触发次数
        public const string FrostRelicMPDamage = "FROSTRELICMPDAMAGE";                                      // 冰霜武具MP伤害加成
        public const string ElectricLuck = "ELECTRICLUCK";                                                  // 触电施加时额外触电概率
        public const string ElectricQuickness = "ELECTRICQUICKNESS";                                        // 触电触发速度
        public const string ElectricStack = "ELECTRICSTACK";                                                // 触电叠加层数
        public const string ElectricDamage = "ELECTRICDAMAGE";                                              // 触电伤害
        public const string HPPotionBonus = "HPPOTIONBONUS";                                                // 生命药水加成
        public const string FreezeThreshold = "FREEZETHRESHOLD";                                            // 冰冻所需次数减少
        public const string FreezeDamage = "FREEZEDAMAGE";                                                  // 冰冻伤害
        public const string MoneyDrop = "MONEYDROP";                                                        // 叶子掉落
        public const string EXPDrop = "EXPDROP";                                                            // 经验掉落
        public const string Negotiation = "NEGOTIATION";                                                    // 谈判力
        public const string DebuffDamage = "DEBUFFDAMAGE";                                                  // 减益伤害
        public const string BuffDuration = "BUFFDURATION";                                                  // 增益持续时间
        public const string DebuffDuration = "DEBUFFDURATION";                                              // 减益持续时间
        public const string ModAddCustomerIntensity = "MODADDCUSTOMERINTENSITY";                            // 模组自定义属性增加
        public const string Tablet = "TABLET";                                                              // 启用石板
        public const string Replenishment = "REPLENISHMENT";                                                // 启用补给
        public const string HardModeUnlock = "HARDMODEUNLOCK";                                              // 困难模式解锁
        public const string MinDarkCloud = "MINDARKCLOUD";                                                  // 迷你黑暗乌云
        public const string CountMinibossKill = "COUNTMINIBOSSKILL";                                        // 红茶袋计数
        public const string ComboBonusDamage = "COMBOBONUSDAMAGE";                                          // 启用机智20增伤
        public const string IgnoreDefense = "IGNOREDEFENSE";                                                // 无视防御
        public const string GuardResist = "GUARDRESIST";                                                    // 防御消耗减少
        public const string DashAttackIceHammer = "DASHATTACKICEHAMMER";                                    // 启用冰锤效果
        public const string EnhancedMiniballista = "ENHANCEDMINIBALLISTA";                                  // 启用弩炮剑效果
        public const string DarkCloudSweep = "DARKCLOUDSWEEP";                                              // 启用超导体效果
        public const string RedSnakeEyeAtkCooldownBonus = "REDSNAKEEYEATKCOOLDOWNBONUS";                    // 普攻减少红蛇之眼冷却
        public const string RedSnakeEyeDamageBonus = "REDSNAKEEYEDAMAGEBONUS";                              // 红蛇之眼造成的伤害
        public const string WoundDamage = "WOUNDDAMAGE";                                                    // 伤口恶化堆叠
        public const string WoundStack = "WOUNDSTACK";                                                      // 伤口恶化堆叠
        public const string WoundExplosion = "WOUNDEXPLOSION";                                              // 启用裁判官效果
        public const string EvasionFury = "EVASIONFURY";                                                    // 启用碧玉残影效果
        public const string EvasionFuryNext = "EVASIONFURY_NEXT";                                           // 启用无尽沉浸效果
        public const string CrossbowMG = "CROSSBOWMG";                                                      // 启用加速核心效果
        public const string MoveSpeedToAttackSpeed = "MOVESPEEDTOATTACKSPEED";                              // 启用双子效果
        public const string AddAmmoWhenCritical = "ADDAMMOWHENCRITICAL";                                    // 启用火药合成效果
        public const string LightningCrossbow = "LIGHTNINGCROSSBOW";                                        // 弩发射闪电箭概率
        public const string LightningCrossbowCloud = "LIGHTNINGCROSSBOW_CLOUD";                             // 闪电箭命中恢复乌云
        public const string LightningCrossbowElectric = "LIGHTNINGCROSSBOWELECTRIC";                        // 闪电箭命中施加触电
        public const string PoisonStack = "POISONSTACK";                                                    // 中毒叠加
        public const string JellyfishDoubleAttack = "JELLYFISHDOUBLEATTACK";                                // 水母双重攻击
        public const string JellyfishBasicDamage = "JELLYFISHBASICDAMAGE";                                  // 水母伤害增加
        public const string SpeedSheath = "SPEEDSHEATH";                                                    // 太刀特攻受攻速加成
        public const string FastSheath = "FASTSHEATH";                                                      // 启用村正效果
        public const string RangeSheath = "RANGESHEATH";                                                    // 启用怪刀伤害
        public const string EvasionSheath = "EVASIONSHEATH";                                                // 太刀特攻受闪避加成
        public const string KatanaDashStack = "KATANADASHSTACK";                                            // 启用海蒂效果
        public const string KatanaGhostCounter = "KATANAGHOSTCOUNTER";                                      // 启用图书馆效果
        public const string EchoOfTheGlacierUpgrade = "ECHOOFTHEGLACIERUPGRADE";                            // 冰川的回声伤害倍率
        public const string EchoOfTheGlacierParry = "ECHOOFTHEGLACIERPARRY";                                // 启用永霜的沉默效果
        public const string DeflectingIce = "DEFLECTINGICE";                                                // 启用冰上滑行
        public const string KatanaElectricCharge = "KATANAELECTRICCHARGE";                                  // 启用电光一闪效果
        public const string FixedAttackSpeed = "FIXEDATTACKSPEED";                                          // 最终攻击速度
        public const string ExFixedAttackSpeed = "EXFIXEDATTACKSPEED";                                      // 模组海蒂额外攻击速度
        public const string DashEvasion = "DASHEVASION";                                                    // 冲刺无敌判定为闪避
        public const string AbsoluteEvasion = "ABSOLUTEEVASION";                                            // 必定闪避值
        public const string BlueBurnChange = "BLUEBURNCHANGE";                                              // 启用封印的特加斯效果
        public const string DarkCloudIce = "DARKCLOUDICE";                                                  // 启用雷霜蝶效果
        public const string FlameSwordReturn = "FLAMESWORDRETURN";                                          // 启用陨铁耳环效果
        public const string FlameSwordCallbackFrost = "FLAMESWORDCALLBACKFROST";                            // 启用永恒蚀效果：冰霜武具触发太阳剑
        public const string FlameSwordFrost = "FLAMESWORDFROST";                                            // 启用永恒蚀效果：太阳剑转为冰属性
        public const string FrostRelicFlame = "FROSTRELICFLAME";                                            // 启用永恒蚀效果：冰霜武具转为火属性
        public const string MagicQuickCast = "MAGICQUICKCAST";                                              // 启用冥想书效果
        public const string RayetStarFragment = "RAYETSTARFRAGMENT";                                        // 启用雷伊星碎片效果
        public const string BoltMagicHoming = "BOLTMAGICHOMING";                                            // 启用鼠法师的吊坠效果
        public const string Execution = "EXECUTION";                                                        // 启用贝鲁特之镰效果
        public const string AdditionalElementalDamageBonus = "ADDITIONALELEMENTALDAMAGEBONUS";              // 武器附加伤害增加
        public const string SuperPlanet = "SUPERPLANET";                                                    // 启用乐谱银河效果
        public const string IceCrossbowBuff = "ICECROSSBOWBUFF";                                            // 启用霜之帷幕效果
        public const string PoisonDebuffDamageBonus = "POISONDEBUFFDAMAGEBONUS";                            // 毒减益伤害
        public const string FixedDash = "FIXEDDASH";                                                        // 最终冲刺次数
        public const string StaffRollingProtection = "STAFFROLLINGPROTECTION";                              // 使用棍获得防御力%护盾
        public const string BlockCastMagic = "BLOCKCASTMAGIC";                                              // 禁止主动释放魔法
        public const string StaffExtend = "STAFFEXTEND";                                                    // 棍长度延伸
        public const string DefenseKatana = "DEFENSEKATANA";                                                // 破甲刃防御力增加普攻伤害
        public const string StaffAllGuard = "STAFFALLGUARD";                                                // 棍全方向防御
        public const string SpearDashAttackBonusByLastDash = "SPEARDASHATTACKBONUSBYLASTDASH";              // 消耗冲刺获得枪冲刺伤害增加
        public const string IceSpearWithWeaponAttack = "ICESPEARWITHWEAPONATTACK";                          // 武器攻击触发沃尔斯帕
        public const string EnhancedChakram = "ENHANCEDCHAKRAM";                                            // 查克拉姆造成的伤害
        public const string AtkSpChakram = "ATKSPDCHAKRAM";                                                 // 查克拉姆受攻速影响
        public const string HomingChakram = "HOMINGCHAKRAM";                                                // 鼠标控制查克拉姆位置
        public const string HealingPenalty = "HEALINGPENALTY";                                              // 治疗效果衰减
        public const string PartyBuff = "PARTYBUFF";                                                        // 翼耳兔共享增益效果
        public const string RevivePlayerHaste = "REVIVEPLAYERHASTE";                                        // 复活队友的速度
        public const string BurnEvo = "BURNEVO";                                                            // 强化灼烧效果 10余烬
        public const string FuryInvincibilitTime = "FURYINVINCIBLETIME";                                    // 狂怒无敌时间
        public const string IceBuffDecreaseAttackSpeed = "ICEBUFF_DECREASEATTACKSPEED";                     // 扩展装置减少攻击速度
        public const string PlasmaActive = "PLASMAACTIVE";                                                  // 启用等离子效果
        public const string PlasmaDamage = "PLASMADAMAGE";                                                  // 等离子伤害
        public const string IceHammerScythe = "ICEHAMMERSCYTHE";                                            // 冰锤变成冰镰刀
        public const string WeaponCriticalDamageAmplify = "WEAPONCRITICALDAMAGEAMPLIFY";                    // 武器暴击伤害放大
        public const string DashCountDamage = "DASHCOUNTDAMAGE";                                            // 冲刺计数伤害
        public const string HardModeHealingPenalty = "HARDMODEHEALINGPENALTY";                              // 困难模式治疗衰减
        public const string ReceivedDamageIncrease = "RECEIVEDDAMAGEINCREASE";                              // 通用易伤
        public const string ReceivedDamageReduction = "RECEIVEDDAMAGEREDUCTION";                            // 通用免伤
        public const string WeaponDamageBonus = "WEAPONDAMAGEBONUS";                                        // 武器倍率增加
        public const string CharmDamageBonus = "CHARMDAMAGEBONUS";                                          // 神器伤害
        public const string PhysicalDefense = "PHYSICALDEFENSE";                                            // 物理抗性
        public const string FireDefense = "FIREDEFENSE";                                                    // 火焰抗性
        public const string IceDefense = "ICEDEFENSE";                                                      // 冰抗性
        public const string LighningDefense = "LIGHTNINGDEFENSE";                                           // 闪电抗性
        public const string RiPostElaser = "RIPOSTELASER";                                                  // 强化还击剑碎片
        public const string IcicleVineOrbit = "ICICLEVINEORBIT";                                            // 霜针花园效果
        public const string ElectricEarringDamage = "ELECTRICEARRINGDAMAGE";                                // 桑德耳环伤害
        public const string ElectricEarringCount = "ELECTRICEARRINGCOUNT";                                  // 桑德耳环攻击次数
        public const string ElectricEarringSweep = "ELECTRICEARRINGSWEEP";                                  // 耀目的寂静效果

        public const string CurrentRosion = "STAT_CURRENTROSION";                                           // 模组属性：侵蚀层数
        public const string DirectWeaponDamage = "STAT_DIRECTWEAPONDAMAGE";                                 // 模组属性：武器造成的直接伤害
        public const string MeleeDamage = "STAT_MELEEDAMAGE";                                               // 模组属性：近战伤害
        public const string CoinToDamage = "STAT_COINTODAMAGE";                                             // 模组属性：每获得x叶子增加1%所有伤害放大
        public const string ReturnCoin = "STAT_RETURNCOIN";                                                 // 模组属性：消耗叶子时返还
        public const string CoinToExp = "STAT_COINTOEXP";                                                   // 模组属性：获得叶子时获得经验
        public const string ScoreCount = "STAT_SCORECOUNT";                                                 // 模组属性：乐谱数量
        public const string MaxDimensionPocket = "STAT_MAXDIMENSIONPOCKET";                                 // 模组属性：许愿池额外容量计算
        public const string BrInventorySlot = "STAT_BRINVENTORYSLOT";                                       // 模组属性：背包额外槽位
        public const string MagicCooldownReduction = "STAT_MAGICCOOLDOWNREDUCTION";                         // 模组属性：魔法书冷却减少
        public const string DefenseToAllDamage = "STAT_DEFENSETOALLDAMAGE";                                 // 模组属性：防御力转所有伤害
        public const string MPRegenToDamage = "STAT_MPREGENTODAMAGE";                                       // 模组属性：每获得xMP再生增加1%消耗MP伤害
        public const string FrostRelicAddMult = "STAT_FROSTRELICADDMULT";                                   // 模组属性：冰霜武具额外倍率
        public const string AllPlayerDefense = "STAT_ALLPLAYERDEFENSE";                                     // 模组属性：全体玩家防御
        public const string AllPlayerAllDamage = "STAT_ALLPLAYERALLDAMAGE";                                 // 模组属性：全体玩家所有伤害
        public const string AllPlayerElementalDamage = "STAT_ALLPLAYERELEMENTALDAMAGE";                     // 模组属性：全体玩家元素伤害
        public const string AllPlayerCrit = "STAT_ALLPLAYERCRIT";                                           // 模组属性：全体玩家暴击几率
        public const string InvincinleSpiceialAttack = "STAT_INVINCIBLESPECIALATTACK";                      // 模组属性：绝对戒指效果
        public const string DrinkPotionStack = "STAT_DRINKPOTIONSTACK";                                     // 模组属性：喝下高级药水数量
        public const string TelescopeCount = "STAT_TELESCOPECOUNT";                                         // 模组属性：望远镜数量
        public const string ScaleOfTheAstralSeaEffect = "STAT_SCALEOFTHEASTRALSEAEFFECT";                   // 模组属性：圣海碎片效果计数器
        public const string EcologicalSphereEffect = "STAT_ECOLOGICALSPHEREEFFECT";                         // 模组属性：生态球效果
        public const string StrikeBack = "STAT_STRIKEBACK";                                                 // 模组属性：反击
        public const string ElectricIce = "STAT_ELECTRICICE";                                               // 模组属性：电冰体效果
        public const string GoldObtained = "STAT_GOLDOBTAINED";                                             // 模组属性：总获取叶子数量
        public const string CompanionStrikeBack = "STAT_COMPANIONSTRIKEBACK";                               // 模组属性：同伴继承反击
        #endregion
        #region 服装文本
        public static string Costume_PinkRabbit_2 => Tools.GetLocalization(nameof(Costume_PinkRabbit_2));
        public static string Costume_PinkRabbit_3 => Tools.GetLocalization(nameof(Costume_PinkRabbit_3));
        public static string Costume_MandarinRabbit_2 => Tools.GetLocalization(nameof(Costume_MandarinRabbit_2));
        public static string Costume_MandarinRabbit_3 => Tools.GetLocalization(nameof(Costume_MandarinRabbit_3));
        public static string Costume_BrownRabbit_2 => Tools.GetLocalization(nameof(Costume_BrownRabbit_2));
        public static string Costume_BrownRabbit_3 => Tools.GetLocalization(nameof(Costume_BrownRabbit_3));
        public static string Costume_WhiteWolf_2 => Tools.GetLocalization(nameof(Costume_WhiteWolf_2));
        public static string Costume_WhiteWolf_3 => Tools.GetLocalization(nameof(Costume_WhiteWolf_3));
        public static string Costume_Skeleton_1 => Tools.GetLocalization(nameof(Costume_Skeleton_1));
        public static string Costume_Skeleton_3 => Tools.GetLocalization(nameof(Costume_Skeleton_3));
        public static string Costume_FireCat_2 => Tools.GetLocalization(nameof(Costume_FireCat_2));
        public static string Costume_FireCat_3 => Tools.GetLocalization(nameof(Costume_FireCat_3));
        public static string Costume_Frog_2 => Tools.GetLocalization(nameof(Costume_Frog_2));
        public static string Costume_Frog_3 => Tools.GetLocalization(nameof(Costume_Frog_3));
        public static string Costume_WizardRabbit_2 => Tools.GetLocalization(nameof(Costume_WizardRabbit_2));
        public static string Costume_WizardRabbit_3 => Tools.GetLocalization(nameof(Costume_WizardRabbit_3));
        public static string Costume_Bat_2 => Tools.GetLocalization(nameof(Costume_Bat_2));
        public static string Costume_Bat_3 => Tools.GetLocalization(nameof(Costume_Bat_3));
        public static string Costume_Bird_2 => Tools.GetLocalization(nameof(Costume_Bird_2));
        public static string Costume_Bird_3 => Tools.GetLocalization(nameof(Costume_Bird_3));
        public static string Costume_Fox_2 => Tools.GetLocalization(nameof(Costume_Fox_2));
        public static string Costume_Fox_3 => Tools.GetLocalization(nameof(Costume_Fox_3));
        public static string Costume_Luck_2 => Tools.GetLocalization(nameof(Costume_Luck_2));
        public static string Costume_Luck_3 => Tools.GetLocalization(nameof(Costume_Luck_3));
        public static string Costume_Mole_2 => Tools.GetLocalization(nameof(Costume_Mole_2));
        public static string Costume_Mole_3 => Tools.GetLocalization(nameof(Costume_Mole_3));
        public static string Costume_HolyRabbit_2 => Tools.GetLocalization(nameof(Costume_HolyRabbit_2));
        public static string Costume_HolyRabbit_3 => Tools.GetLocalization(nameof(Costume_HolyRabbit_3));
        public static string Costume_Ghost_2 => Tools.GetLocalization(nameof(Costume_Ghost_2));
        public static string Costume_Ghost_3 => Tools.GetLocalization(nameof(Costume_Ghost_3));
        public static string Costume_Seren_2 => Tools.GetLocalization(nameof(Costume_Seren_2));
        public static string Costume_Seren_3 => Tools.GetLocalization(nameof(Costume_Seren_3));
        public static string Costume_Crocodile_2 => Tools.GetLocalization(nameof(Costume_Crocodile_2));
        public static string Costume_Crocodile_3 => Tools.GetLocalization(nameof(Costume_Crocodile_3));
        public static string Costume_WhiteRabbit_2 => Tools.GetLocalization(nameof(Costume_WhiteRabbit_2));
        public static string Costume_WhiteRabbit_3 => Tools.GetLocalization(nameof(Costume_WhiteRabbit_3));
        public static string Costume_RedHolyRabbit_2 => Tools.GetLocalization(nameof(Costume_RedHolyRabbit_2));
        public static string Costume_RedHolyRabbit_3 => Tools.GetLocalization(nameof(Costume_RedHolyRabbit_3));
        public static string Costume_DarkBrownRabbit_2 => Tools.GetLocalization(nameof(Costume_DarkBrownRabbit_2));
        public static string Costume_DarkBrownRabbit_3 => Tools.GetLocalization(nameof(Costume_DarkBrownRabbit_3));
        public static string Costume_Deer_1 => Tools.GetLocalization(nameof(Costume_Deer_1));
        public static string Costume_Deer_2 => Tools.GetLocalization(nameof(Costume_Deer_2));
        public static string Costume_Deer_3 => Tools.GetLocalization(nameof(Costume_Deer_3));
        public static string Costume_Squirrel_2 => Tools.GetLocalization(nameof(Costume_Squirrel_2));
        public static string Costume_Squirrel_3 => Tools.GetLocalization(nameof(Costume_Squirrel_3));
        public static string Costume_Otter_2 => Tools.GetLocalization(nameof(Costume_Otter_2));
        public static string Costume_Otter_3 => Tools.GetLocalization(nameof(Costume_Otter_3));
        public static string Costume_ArmoredRabbit_2 => Tools.GetLocalization(nameof(Costume_ArmoredRabbit_2));
        public static string Costume_ArmoredRabbit_3 => Tools.GetLocalization(nameof(Costume_ArmoredRabbit_3));
        public static string Costume_Turtle_2 => Tools.GetLocalization(nameof(Costume_Turtle_2));
        public static string Costume_Turtle_3 => Tools.GetLocalization(nameof(Costume_Turtle_3));
        public static string Costume_ForestCat_2 => Tools.GetLocalization(nameof(Costume_ForestCat_2));
        public static string Costume_ForestCat_3 => Tools.GetLocalization(nameof(Costume_ForestCat_3));
        #endregion
        #region 剑盾文本
        public static string Superconductor => Tools.GetLocalization(nameof(Superconductor));
        public static string BlindingSilence => Tools.GetLocalization(nameof(BlindingSilence));
        public static string BallistaSword => Tools.GetLocalization(nameof(BallistaSword));
        public static string CarrotSword => Tools.GetLocalization(nameof(CarrotSword));
        public static string CarrotSword2 => Tools.GetLocalization(nameof(CarrotSword2));
        public static string Excallion => Tools.GetLocalization(nameof(Excallion));
        public static string Rapier => Tools.GetLocalization(nameof(Rapier));
        public static string Rapier2 => Tools.GetLocalization(nameof(Rapier2));
        public static string StarTwinkle => Tools.GetLocalization(nameof(StarTwinkle));
        public static string ColdAnger => Tools.GetLocalization(nameof(ColdAnger));
        public static string StoneSword => Tools.GetLocalization(nameof(StoneSword));
        public static string TheIndomitable => Tools.GetLocalization(nameof(TheIndomitable));
        public static string WanderingAshes => Tools.GetLocalization(nameof(WanderingAshes));
        public static string MagicWand => Tools.GetLocalization(nameof(MagicWand));
        public static string PrismaticMagic => Tools.GetLocalization(nameof(PrismaticMagic));
        public static string GlacialBlade => Tools.GetLocalization(nameof(GlacialBlade));
        public static string BlizzardsEve => Tools.GetLocalization(nameof(BlizzardsEve));
        public static string GardenofNeedleIce => Tools.GetLocalization(nameof(GardenofNeedleIce));
        public static string GrippingFear => Tools.GetLocalization(nameof(GrippingFear));
        public static string GuardiansReturn => Tools.GetLocalization(nameof(GuardiansReturn));
        public static string FlameEaterHaetae => Tools.GetLocalization(nameof(FlameEaterHaetae));
        public static string SolisMissio => Tools.GetLocalization(nameof(SolisMissio));
        #endregion
        #region 大剑文本
        public static string BoneGearblade => Tools.GetLocalization(nameof(BoneGearblade));
        public static string CreakingSpine => Tools.GetLocalization(nameof(CreakingSpine));
        public static string FrostboundGreatsword => Tools.GetLocalization(nameof(FrostboundGreatsword));
        public static string EternalFrostGreatsword => Tools.GetLocalization(nameof(EternalFrostGreatsword));
        public static string IcicleSword => Tools.GetLocalization(nameof(IcicleSword));
        public static string GreatswordOfExorcism => Tools.GetLocalization(nameof(GreatswordOfExorcism));
        public static string RedSnakeCrush => Tools.GetLocalization(nameof(RedSnakeCrush));
        public static string SolisCineris => Tools.GetLocalization(nameof(SolisCineris));
        public static string TroublingDelight => Tools.GetLocalization(nameof(TroublingDelight));
        public static string PristineForm => Tools.GetLocalization(nameof(PristineForm));
        public static string LegacyoftheIllusionist => Tools.GetLocalization(nameof(LegacyoftheIllusionist));
        public static string Lazarion => Tools.GetLocalization(nameof(Lazarion));
        public static string CrimsonVortex => Tools.GetLocalization(nameof(CrimsonVortex));
        public static string TheInquisitor => Tools.GetLocalization(nameof(TheInquisitor));
        public static string WoodenClub => Tools.GetLocalization(nameof(WoodenClub));
        public static string MischievousPrank => Tools.GetLocalization(nameof(MischievousPrank));
        public static string SpikeClub => Tools.GetLocalization(nameof(SpikeClub));
        public static string LightningGreatSwordE3G => Tools.GetLocalization(nameof(LightningGreatSwordE3G));
        public static string LightningGreatSwordS3G => Tools.GetLocalization(nameof(LightningGreatSwordS3G));
        public static string SwarmingShadow => Tools.GetLocalization(nameof(SwarmingShadow));
        public static string SwiftRegret => Tools.GetLocalization(nameof(SwiftRegret));
        public static string Compliance => Tools.GetLocalization(nameof(Compliance));
        public static string Minimalism => Tools.GetLocalization(nameof(Minimalism));
        public static string TheSecond => Tools.GetLocalization(nameof(TheSecond));
        public static string BlackIronHalberd => Tools.GetLocalization(nameof(BlackIronHalberd));
        public static string Negligence => Tools.GetLocalization(nameof(Negligence));
        public static string LovingGaze => Tools.GetLocalization(nameof(LovingGaze));
        #endregion
        #region 匕首文本
        public static string SafeStart => Tools.GetLocalization(nameof(SafeStart));
        public static string LightlessBlade => Tools.GetLocalization(nameof(LightlessBlade));
        public static string PaleSmile => Tools.GetLocalization(nameof(PaleSmile));
        public static string LightningDagger => Tools.GetLocalization(nameof(LightningDagger));
        public static string OverhangingStormCloud => Tools.GetLocalization(nameof(OverhangingStormCloud));
        public static string LightningsWrath => Tools.GetLocalization(nameof(LightningsWrath));
        public static string TomeofMimicry => Tools.GetLocalization(nameof(TomeofMimicry));
        public static string Adonis => Tools.GetLocalization(nameof(Adonis));
        public static string HeatedBlade => Tools.GetLocalization(nameof(HeatedBlade));
        public static string SolisImber => Tools.GetLocalization(nameof(SolisImber));
        public static string JadeVestige => Tools.GetLocalization(nameof(JadeVestige));
        public static string UnboundImmersion => Tools.GetLocalization(nameof(UnboundImmersion));
        public static string CreepingDespair => Tools.GetLocalization(nameof(CreepingDespair));
        public static string DifficultTrial => Tools.GetLocalization(nameof(DifficultTrial));
        public static string ShadowofHope => Tools.GetLocalization(nameof(ShadowofHope));
        public static string AbsoluteEnvy => Tools.GetLocalization(nameof(AbsoluteEnvy));
        public static string AcademyMagicSwordLiberation => Tools.GetLocalization(nameof(AcademyMagicSwordLiberation));
        public static string DarkVeil => Tools.GetLocalization(nameof(DarkVeil));
        public static string FrozenSmelt => Tools.GetLocalization(nameof(FrozenSmelt));
        public static string Drífa => Tools.GetLocalization(nameof(Drífa));
        #endregion
        #region 弩文本
        public static string ExplosiveDevice => Tools.GetLocalization(nameof(ExplosiveDevice));
        public static string TheBarnacle => Tools.GetLocalization(nameof(TheBarnacle));
        public static string Twin => Tools.GetLocalization(nameof(Twin));
        public static string BountyHunter => Tools.GetLocalization(nameof(BountyHunter));
        public static string GunpowderSynthesis => Tools.GetLocalization(nameof(GunpowderSynthesis));
        public static string PurpleJacket => Tools.GetLocalization(nameof(PurpleJacket));
        public static string KalimsForm => Tools.GetLocalization(nameof(KalimsForm));
        public static string MarkStabilization => Tools.GetLocalization(nameof(MarkStabilization));
        public static string CloudGatherer => Tools.GetLocalization(nameof(CloudGatherer));
        public static string Minigun => Tools.GetLocalization(nameof(Minigun));
        public static string AccelerationCore => Tools.GetLocalization(nameof(AccelerationCore));
        public static string M9199 => Tools.GetLocalization(nameof(M9199));
        public static string XRA9 => Tools.GetLocalization(nameof(XRA9));
        public static string M9200 => Tools.GetLocalization(nameof(M9200));
        public static string MiniCannon => Tools.GetLocalization(nameof(MiniCannon));
        public static string SolisIubar => Tools.GetLocalization(nameof(SolisIubar));
        public static string RapidFreezeCrystal => Tools.GetLocalization(nameof(RapidFreezeCrystal));
        public static string ExpansionDevice => Tools.GetLocalization(nameof(ExpansionDevice));
        public static string RapidFireDevice => Tools.GetLocalization(nameof(RapidFireDevice));
        public static string EquippedwithGuidanceModule => Tools.GetLocalization(nameof(EquippedwithGuidanceModule));
        #endregion
        #region 太刀文本
        public static string Heidi => Tools.GetLocalization(nameof(Heidi));
        public static string SolisEkli => Tools.GetLocalization(nameof(SolisEkli));
        public static string BlazingHelbanus => Tools.GetLocalization(nameof(BlazingHelbanus));
        public static string LibrarySpectralSealingSociety => Tools.GetLocalization(nameof(LibrarySpectralSealingSociety));
        public static string LibrarySpectralSealingSociety2 => Tools.GetLocalization(nameof(LibrarySpectralSealingSociety2));
        public static string AdamahOath => Tools.GetLocalization(nameof(AdamahOath));
        public static string YuisShortDagger => Tools.GetLocalization(nameof(YuisShortDagger));
        public static string RendbladeHeavenrend => Tools.GetLocalization(nameof(RendbladeHeavenrend));
        public static string RendbladeRuinDance => Tools.GetLocalization(nameof(RendbladeRuinDance));
        public static string RendbladeDriftingWillow => Tools.GetLocalization(nameof(RendbladeDriftingWillow));
        public static string LongBladeOnohara => Tools.GetLocalization(nameof(LongBladeOnohara));
        public static string BeastMarksSwordHou => Tools.GetLocalization(nameof(BeastMarksSwordHou));
        public static string FamedSwordMuramasa => Tools.GetLocalization(nameof(FamedSwordMuramasa));
        public static string WinterSmithsDraftblade => Tools.GetLocalization(nameof(WinterSmithsDraftblade));
        public static string SilenceofEverfrost => Tools.GetLocalization(nameof(SilenceofEverfrost));
        public static string FragmentofNastrond => Tools.GetLocalization(nameof(FragmentofNastrond));
        public static string LightningFlash => Tools.GetLocalization(nameof(LightningFlash));
        public static string CeruleanCloudSwordArges => Tools.GetLocalization(nameof(CeruleanCloudSwordArges));
        public static string BreakplateBlade => Tools.GetLocalization(nameof(BreakplateBlade));
        #endregion
        #region 长棍文本
        public static string StaffOfGreatMight => Tools.GetLocalization(nameof(StaffOfGreatMight));
        public static string SolisBraca => Tools.GetLocalization(nameof(SolisBraca));
        public static string Viduka => Tools.GetLocalization(nameof(Viduka));
        public static string Nephorhax => Tools.GetLocalization(nameof(Nephorhax));
        public static string RuadhsleaSpear => Tools.GetLocalization(nameof(RuadhsleaSpear));
        public static string IlonicasSpiritStaff => Tools.GetLocalization(nameof(IlonicasSpiritStaff));
        public static string FuarscaSpear => Tools.GetLocalization(nameof(FuarscaSpear));
        public static string ScaleOfTheAstralSea => Tools.GetLocalization(nameof(ScaleOfTheAstralSea));
        public static string RinglessStaff => Tools.GetLocalization(nameof(RinglessStaff));
        public static string WeightOfTheSea => Tools.GetLocalization(nameof(WeightOfTheSea));
        public static string Malaspina => Tools.GetLocalization(nameof(Malaspina));
        public static string OraclesStaff => Tools.GetLocalization(nameof(OraclesStaff));
        public static string FiaolasJudgmentStaff => Tools.GetLocalization(nameof(FiaolasJudgmentStaff));
        public static string SacredJar => Tools.GetLocalization(nameof(SacredJar));
        public static string FormlessDance => Tools.GetLocalization(nameof(FormlessDance));
        public static string Successor => Tools.GetLocalization(nameof(Successor));
        #endregion

        public static bool InitializationCostumeText = false;
        public static Dictionary<string, string> CostumeTextMap = new Dictionary<string, string> { };
        public static bool InitializationWeaponText = false;
        public static Dictionary<int, (string, int)> WeaponTextMap = new Dictionary<int, (string, int)> { };
        public static void LoadText()
        {
            if (!InitializationCostumeText && ModConfig.Instance.EnableCostumeModifier)
            {
                InitializationCostumeText = true;
                CostumeTextMap = new Dictionary<string, string>
                {
                    {"PinkRabbit_2", GetCostumeTooltip(Costume_PinkRabbit_2)},
                    {"PinkRabbit_3", GetCostumeTooltip(Costume_PinkRabbit_3)},
                    {"MandarinRabbit_2", GetCostumeTooltip(Costume_MandarinRabbit_2)},
                    {"MandarinRabbit_3", GetCostumeTooltip(Costume_MandarinRabbit_3)},
                    {"BrownRabbit_2", GetCostumeTooltip(Costume_BrownRabbit_2)},
                    {"BrownRabbit_3", GetCostumeTooltip(Costume_BrownRabbit_3)},
                    {"WhiteWolf_2", GetCostumeTooltip(Costume_WhiteWolf_2)},
                    {"WhiteWolf_3", GetCostumeTooltip(Costume_WhiteWolf_3)},
                    {"Skeleton_1", GetCostumeTooltip(Costume_Skeleton_1)},
                    {"Skeleton_3", GetCostumeTooltip(Costume_Skeleton_3)},
                    {"FireCat_2", GetCostumeTooltip(Costume_FireCat_2)},
                    {"FireCat_3", GetCostumeTooltip(Costume_FireCat_3)},
                    {"Frog_2", GetCostumeTooltip(Costume_Frog_2)},
                    {"Frog_3", GetCostumeTooltip(Costume_Frog_3)},
                    {"WizardRabbit_2", GetCostumeTooltip(Costume_WizardRabbit_2)},
                    {"WizardRabbit_3", GetCostumeTooltip(Costume_WizardRabbit_3)},
                    {"Bat_2", GetCostumeTooltip(Costume_Bat_2)},
                    {"Bat_3", GetCostumeTooltip(Costume_Bat_3)},
                    {"Bird_2", GetCostumeTooltip(Costume_Bird_2)},
                    {"Bird_3", GetCostumeTooltip(Costume_Bird_3)},
                    {"Fox_2", GetCostumeTooltip(Costume_Fox_2)},
                    {"Fox_3", GetCostumeTooltip(Costume_Fox_3)},
                    {"Luck_2", GetCostumeTooltip(Costume_Luck_2)},
                    {"Luck_3", GetCostumeTooltip(Costume_Luck_3)},
                    {"Mole_2", GetCostumeTooltip(Costume_Mole_2)},
                    {"Mole_3", GetCostumeTooltip(Costume_Mole_3)},
                    {"HolyRabbit_2", GetCostumeTooltip(Costume_HolyRabbit_2)},
                    {"HolyRabbit_3", GetCostumeTooltip(Costume_HolyRabbit_3)},
                    {"Ghost_2", GetCostumeTooltip(Costume_Ghost_2)},
                    {"Ghost_3", GetCostumeTooltip(Costume_Ghost_3)},
                    {"Seren_2", GetCostumeTooltip(Costume_Seren_2)},
                    {"Seren_3", GetCostumeTooltip(Costume_Seren_3)},
                    {"Crocodile_2", GetCostumeTooltip(Costume_Crocodile_2)},
                    {"Crocodile_3", GetCostumeTooltip(Costume_Crocodile_3)},
                    {"WhiteRabbit_2", GetCostumeTooltip(Costume_WhiteRabbit_2)},
                    {"WhiteRabbit_3", GetCostumeTooltip(Costume_WhiteRabbit_3)},
                    {"RedHolyRabbit_2", GetCostumeTooltip(Costume_RedHolyRabbit_2)},
                    {"RedHolyRabbit_3", GetCostumeTooltip(Costume_RedHolyRabbit_3)},
                    {"DarkBrownRabbit_2", GetCostumeTooltip(Costume_DarkBrownRabbit_2)},
                    {"DarkBrownRabbit_3", GetCostumeTooltip(Costume_DarkBrownRabbit_3)},
                    {"Deer_1", GetCostumeTooltip(Costume_Deer_1)},
                    {"Deer_2", GetCostumeTooltip(Costume_Deer_2)},
                    {"Deer_3", GetCostumeTooltip(Costume_Deer_3)},
                    {"Squirrel_2", GetCostumeTooltip(Costume_Squirrel_2)},
                    {"Squirrel_3", GetCostumeTooltip(Costume_Squirrel_3)},
                    {"Otter_2", GetCostumeTooltip(Costume_Otter_2)},
                    {"Otter_3", GetCostumeTooltip(Costume_Otter_3)},
                    {"ArmoredRabbit_2", GetCostumeTooltip(Costume_ArmoredRabbit_2)},
                    {"ArmoredRabbit_3", GetCostumeTooltip(Costume_ArmoredRabbit_3)},
                    {"Turtle_2", GetCostumeTooltip(Costume_Turtle_2)},
                    {"Turtle_3", GetCostumeTooltip(Costume_Turtle_3)},
                    {"ForestCat_2", GetCostumeTooltip(Costume_ForestCat_2)},
                    {"ForestCat_3", GetCostumeTooltip(Costume_ForestCat_3)},
                };
            }
            if (!InitializationWeaponText && ModConfig.Instance.EnableWeaponModifier)
            {
                InitializationWeaponText = true;
                WeaponTextMap = new Dictionary<int, (string, int)>
                {
                    #region 剑盾
                    {WeaponIDs.Superconductor, (GetWeaponTooltip(Superconductor), WeaponIDs.Conductor)},
                    {WeaponIDs.BallistaSword, (GetWeaponTooltip(BallistaSword), WeaponIDs.Conductor)},
                    {WeaponIDs.BlindingSilence, (GetWeaponTooltip(BlindingSilence), WeaponIDs.Conductor)},
                    {WeaponIDs.CarrotSword, (GetWeaponTooltip(CarrotSword), WeaponIDs.DefaultSwordandShield)},
                    {WeaponIDs.CarrotSword2, (GetWeaponTooltip(CarrotSword2), WeaponIDs.CarrotSword)},
                    {WeaponIDs.Excallion, (GetWeaponTooltip(Excallion), WeaponIDs.CarrotSword)},
                    {WeaponIDs.Rapier, (GetWeaponTooltip(Rapier), WeaponIDs.DefaultSwordandShield)},
                    {WeaponIDs.Rapier2, (GetWeaponTooltip(Rapier2), WeaponIDs.Rapier)},
                    {WeaponIDs.StarTwinkle, (GetWeaponTooltip(StarTwinkle), WeaponIDs.ProtectionoftheFuzz)},
                    {WeaponIDs.ColdAnger, (GetWeaponTooltip(ColdAnger), WeaponIDs.ProtectionoftheFuzz)},
                    {WeaponIDs.StoneSword, (GetWeaponTooltip(StoneSword), WeaponIDs.ProtectionoftheFuzz)},
                    {WeaponIDs.TheIndomitable, (GetWeaponTooltip(TheIndomitable), WeaponIDs.UnityofOffenseandDefense)},
                    {WeaponIDs.WanderingAshes, (GetWeaponTooltip(WanderingAshes), WeaponIDs.UnityofOffenseandDefense)},
                    {WeaponIDs.MagicWand, (GetWeaponTooltip(MagicWand), WeaponIDs.DefaultSwordandShield)},
                    {WeaponIDs.PrismaticMagic, (GetWeaponTooltip(PrismaticMagic), WeaponIDs.MagicWand)},
                    {WeaponIDs.GlacialBlade, (GetWeaponTooltip(GlacialBlade), WeaponIDs.FrostShard)},
                    {WeaponIDs.BlizzardsEve, (GetWeaponTooltip(BlizzardsEve), WeaponIDs.FrostShard)},
                    {WeaponIDs.GardenofNeedleIce, (GetWeaponTooltip(GardenofNeedleIce), WeaponIDs.FrostShard)},
                    {WeaponIDs.GrippingFear, (GetWeaponTooltip(GrippingFear), WeaponIDs.TinyDestruction)},
                    {WeaponIDs.GuardiansReturn, (GetWeaponTooltip(GuardiansReturn), WeaponIDs.TinyDestruction)},
                    {WeaponIDs.FlameEaterHaetae, (GetWeaponTooltip(FlameEaterHaetae), WeaponIDs.Incendium)},
                    {WeaponIDs.SolisMissio, (GetWeaponTooltip(SolisMissio), WeaponIDs.Incendium)},
                    #endregion
                    #region 大剑
                    {WeaponIDs.BoneGearblade, (GetWeaponTooltip(BoneGearblade), WeaponIDs.SteelGreatsword)},
                    {WeaponIDs.CreakingSpine, (GetWeaponTooltip(CreakingSpine), WeaponIDs.BoneGearblade)},
                    {WeaponIDs.BloodlettingGearblade, ("", WeaponIDs.BoneGearblade)},
                    {WeaponIDs.FrostboundGreatsword, (GetWeaponTooltip(FrostboundGreatsword), WeaponIDs.SteelGreatsword)},
                    {WeaponIDs.EternalFrostGreatsword, (GetWeaponTooltip(EternalFrostGreatsword), WeaponIDs.FrostboundGreatsword)},
                    {WeaponIDs.IcicleSword, (GetWeaponTooltip(IcicleSword), WeaponIDs.FrostboundGreatsword)},
                    {WeaponIDs.GreatswordOfExorcism, (GetWeaponTooltip(GreatswordOfExorcism), WeaponIDs.FrostboundGreatsword)},
                    {WeaponIDs.RedSnakeCrush, (GetWeaponTooltip(RedSnakeCrush), WeaponIDs.TheEmberforged)},
                    {WeaponIDs.SolisCineris, (GetWeaponTooltip(SolisCineris), WeaponIDs.TheEmberforged)},
                    {WeaponIDs.TroublingDelight, (GetWeaponTooltip(TroublingDelight), WeaponIDs.SteelGreatsword)},
                    {WeaponIDs.LegacyofTheIllusionist, (GetWeaponTooltip(LegacyoftheIllusionist), WeaponIDs.TroublingDelight)},
                    {WeaponIDs.PristineForm, (GetWeaponTooltip(PristineForm), WeaponIDs.TroublingDelight)},
                    {WeaponIDs.Lazarion, (GetWeaponTooltip(Lazarion), WeaponIDs.TroublingDelight)},
                    {WeaponIDs.CrimsonVortex, (GetWeaponTooltip(CrimsonVortex), WeaponIDs.CrimsonThorn)},
                    {WeaponIDs.TheInquisitor, (GetWeaponTooltip(TheInquisitor), WeaponIDs.CrimsonThorn)},
                    {WeaponIDs.WoodenClub, (GetWeaponTooltip(WoodenClub), WeaponIDs.SteelGreatsword)},
                    {WeaponIDs.MischievousPrank, (GetWeaponTooltip(MischievousPrank), WeaponIDs.WoodenClub)},
                    {WeaponIDs.SpikeClub, (GetWeaponTooltip(SpikeClub), WeaponIDs.WoodenClub)},
                    {WeaponIDs.Negligence, (GetWeaponTooltip(Negligence), WeaponIDs.Hypersensitivity)},
                    {WeaponIDs.LovingGaze, (GetWeaponTooltip(LovingGaze), WeaponIDs.Hypersensitivity)},
                    {WeaponIDs.LightningGreatSwordE3G, (GetWeaponTooltip(LightningGreatSwordE3G), WeaponIDs.LightningGreatSwordE2G)},
                    {WeaponIDs.LightningGreatSwordS3G, (GetWeaponTooltip(LightningGreatSwordS3G), WeaponIDs.LightningGreatSwordE2G)},
                    {WeaponIDs.SwarmingShadow, (GetWeaponTooltip(SwarmingShadow), WeaponIDs.SteelGreatsword)},
                    {WeaponIDs.SwiftRegret, (GetWeaponTooltip(SwiftRegret), WeaponIDs.SwarmingShadow)},
                    {WeaponIDs.Compliance, (GetWeaponTooltip(Compliance), WeaponIDs.SwarmingShadow)},
                    {WeaponIDs.Minimalism, (GetWeaponTooltip(Minimalism), WeaponIDs.SteelGreatsword)},
                    {WeaponIDs.TheSecond, (GetWeaponTooltip(TheSecond), WeaponIDs.Minimalism)},
                    {WeaponIDs.BlackIronHalberd, (GetWeaponTooltip(BlackIronHalberd), WeaponIDs.Minimalism)},
                    #endregion
                    #region 匕首
                    {WeaponIDs.SafeStart, (GetWeaponTooltip(SafeStart), WeaponIDs.Dagger)},
                    {WeaponIDs.LightlessBlade, (GetWeaponTooltip(LightlessBlade), WeaponIDs.SafeStart)},
                    {WeaponIDs.PaleSmile, (GetWeaponTooltip(PaleSmile), WeaponIDs.SafeStart)},
                    {WeaponIDs.LightningDagger, (GetWeaponTooltip(LightningDagger), WeaponIDs.Dagger)},
                    {WeaponIDs.LightningsWrath, (GetWeaponTooltip(LightningsWrath), WeaponIDs.LightningDagger)},
                    {WeaponIDs.OverhangingStormCloud, (GetWeaponTooltip(OverhangingStormCloud), WeaponIDs.LightningDagger)},
                    {WeaponIDs.TomeOfMimicry, (GetWeaponTooltip(TomeofMimicry), WeaponIDs.AcademyEnchantedSword)},
                    {WeaponIDs.AcademyMagicSwordLiberation, (GetWeaponTooltip(AcademyMagicSwordLiberation), WeaponIDs.AcademyEnchantedSword)},
                    {WeaponIDs.DarkVeil, (GetWeaponTooltip(DarkVeil), WeaponIDs.AlpineEdelweiss)},
                    {WeaponIDs.Adonis, (GetWeaponTooltip(Adonis), WeaponIDs.AlpineEdelweiss)},
                    {WeaponIDs.HeatedBlade, (GetWeaponTooltip(HeatedBlade), WeaponIDs.FireDagger)},
                    {WeaponIDs.SolisImber, (GetWeaponTooltip(SolisImber), WeaponIDs.FireDagger)},
                    {WeaponIDs.JadeVestige, (GetWeaponTooltip(JadeVestige), WeaponIDs.Dagger)},
                    {WeaponIDs.UnboundImmersion, (GetWeaponTooltip(UnboundImmersion), WeaponIDs.JadeVestige)},
                    {WeaponIDs.CreepingDespair, (GetWeaponTooltip(CreepingDespair), WeaponIDs.AridBreeze)},
                    {WeaponIDs.DifficultTrial, (GetWeaponTooltip(DifficultTrial), WeaponIDs.AridBreeze)},
                    {WeaponIDs.ShadowofHope, (GetWeaponTooltip(ShadowofHope), WeaponIDs.OverTheDoorstep)},
                    {WeaponIDs.AbsoluteEnvy, (GetWeaponTooltip(AbsoluteEnvy), WeaponIDs.OverTheDoorstep)},
                    {WeaponIDs.FrozenSmelt, (GetWeaponTooltip(FrozenSmelt), WeaponIDs.DormantFrost)},
                    {WeaponIDs.Drífa, (GetWeaponTooltip(Drífa), WeaponIDs.DormantFrost)},
                    #endregion
                    #region 弩
                    {WeaponIDs.ExplosiveDevice, (GetWeaponTooltip(ExplosiveDevice), WeaponIDs.ScattershotShooter)},
                    {WeaponIDs.GunpowderSynthesis, (GetWeaponTooltip(GunpowderSynthesis), WeaponIDs.ScattershotShooter)},
                    {WeaponIDs.TheBarnacle, (GetWeaponTooltip(TheBarnacle), WeaponIDs.ScattershotShooter)},
                    {WeaponIDs.KalimsForm, (GetWeaponTooltip(KalimsForm), WeaponIDs.Kagumede)},
                    {WeaponIDs.PurpleJacket, (GetWeaponTooltip(PurpleJacket), WeaponIDs.Kagumede)},
                    {WeaponIDs.MarkStabilization, (GetWeaponTooltip(MarkStabilization), WeaponIDs.UnstableLightningMark)},
                    {WeaponIDs.CloudGatherer, (GetWeaponTooltip(CloudGatherer), WeaponIDs.UnstableLightningMark)},
                    {WeaponIDs.Minigun, (GetWeaponTooltip(Minigun), WeaponIDs.UnstableLightningMark)},
                    {WeaponIDs.AccelerationCore, (GetWeaponTooltip(AccelerationCore), WeaponIDs.ReloadDevice)},
                    {WeaponIDs.Twin, (GetWeaponTooltip(Twin), WeaponIDs.ReloadDevice)},
                    {WeaponIDs.BountyHunter, (GetWeaponTooltip(BountyHunter), WeaponIDs.ReloadDevice)},
                    {WeaponIDs.M9199, (GetWeaponTooltip(M9199), WeaponIDs.ColossalCrossbow)},
                    {WeaponIDs.XRA9, (GetWeaponTooltip(XRA9), WeaponIDs.M9199)},
                    {WeaponIDs.M9200, (GetWeaponTooltip(M9200), WeaponIDs.M9199)},
                    {WeaponIDs.MiniCannon, (GetWeaponTooltip(MiniCannon), WeaponIDs.FireMine)},
                    {WeaponIDs.SolisIubar, (GetWeaponTooltip(SolisIubar), WeaponIDs.FireMine)},
                    {WeaponIDs.EquippedwithGuidanceModule, (GetWeaponTooltip(EquippedwithGuidanceModule), WeaponIDs.FireMine)},
                    {WeaponIDs.RapidFreezeCrystal, (GetWeaponTooltip(RapidFreezeCrystal), WeaponIDs.FrostVeil)},
                    {WeaponIDs.ExpansionDevice, (GetWeaponTooltip(ExpansionDevice), WeaponIDs.FrostVeil)},
                    {WeaponIDs.RapidFireDevice, (GetWeaponTooltip(RapidFireDevice), WeaponIDs.EnhancedSpecialBolts)},
                    #endregion
                    #region 太刀
                    {WeaponIDs.Heidi, (GetWeaponTooltip(Heidi), WeaponIDs.Helbanus)},
                    {WeaponIDs.SolisEkli, (GetWeaponTooltip(SolisEkli), WeaponIDs.Helbanus)},
                    {WeaponIDs.BlazingHelbanus, (GetWeaponTooltip(BlazingHelbanus), WeaponIDs.Helbanus)},
                    {WeaponIDs.LibrarySpectralSealingSociety, (GetWeaponTooltip(LibrarySpectralSealingSociety), WeaponIDs.Blade)},
                    {WeaponIDs.LibrarySpectralSealingSociety2,(GetWeaponTooltip(LibrarySpectralSealingSociety2), WeaponIDs.LibrarySpectralSealingSociety)},
                    {WeaponIDs.AdamahOath, (GetWeaponTooltip(AdamahOath), WeaponIDs.TheAwaitingAzureblade)},
                    {WeaponIDs.YuisShortDagger, (GetWeaponTooltip(YuisShortDagger), WeaponIDs.TheAwaitingAzureblade)},
                    {WeaponIDs.RendbladeHeavenrend, (GetWeaponTooltip(RendbladeHeavenrend), WeaponIDs.Blade)},
                    {WeaponIDs.RendbladeRuinDance, (GetWeaponTooltip(RendbladeRuinDance), WeaponIDs.RendbladeHeavenrend)},
                    {WeaponIDs.RendbladeDriftingWillow, (GetWeaponTooltip(RendbladeDriftingWillow), WeaponIDs.RendbladeHeavenrend)},
                    {WeaponIDs.LongBladeOnohara, (GetWeaponTooltip(LongBladeOnohara), WeaponIDs.Blade)},
                    {WeaponIDs.BeastMarksSwordHou, (GetWeaponTooltip(BeastMarksSwordHou), WeaponIDs.LongBladeOnohara)},
                    {WeaponIDs.FamedSwordMuramasa, (GetWeaponTooltip(FamedSwordMuramasa), WeaponIDs.LongBladeOnohara)},
                    {WeaponIDs.WinterSmithsDraftblade, (GetWeaponTooltip(WinterSmithsDraftblade), WeaponIDs.Blade)},
                    {WeaponIDs.SilenceofEverfrost, (GetWeaponTooltip(SilenceofEverfrost), WeaponIDs.WinterSmithsDraftblade)},
                    {WeaponIDs.FragmentofNastrond, (GetWeaponTooltip(FragmentofNastrond), WeaponIDs.WinterSmithsDraftblade)},
                    {WeaponIDs.LightningFlash, (GetWeaponTooltip(LightningFlash), WeaponIDs.ThunderSwordBronte)},
                    {WeaponIDs.CeruleanCloudSwordArges, (GetWeaponTooltip(CeruleanCloudSwordArges), WeaponIDs.ThunderSwordBronte)},
                    {WeaponIDs.BreakplateBlade, (GetWeaponTooltip(BreakplateBlade), WeaponIDs.ArmorBender)},
                    #endregion
                    #region 长棍
                    {WeaponIDs.StaffOfGreatMight, (GetWeaponTooltip(StaffOfGreatMight), WeaponIDs.FlourishStaff)},
                    {WeaponIDs.IlonicasSpiritStaff, (GetWeaponTooltip(IlonicasSpiritStaff), WeaponIDs.FlourishStaff)},
                    {WeaponIDs.SolisBraca, (GetWeaponTooltip(SolisBraca), WeaponIDs.StaffOfEmbers)},
                    {WeaponIDs.FiaolasJudgmentStaff, (GetWeaponTooltip(FiaolasJudgmentStaff), WeaponIDs.StaffOfEmbers)},
                    {WeaponIDs.Viduka, (GetWeaponTooltip(Viduka), WeaponIDs.StaffOfStorms)},
                    {WeaponIDs.Nephorhax, (GetWeaponTooltip(Nephorhax), WeaponIDs.StaffOfStorms)},
                    {WeaponIDs.RuadhsleaSpear, (GetWeaponTooltip(RuadhsleaSpear), WeaponIDs.GeoroidSpear)},
                    {WeaponIDs.FuarscaSpear, (GetWeaponTooltip(FuarscaSpear), WeaponIDs.GeoroidSpear)},
                    {WeaponIDs.ScaleOfTheAstralSea, (GetWeaponTooltip(ScaleOfTheAstralSea), WeaponIDs.DistantStarlight)},
                    {WeaponIDs.RinglessStaff, (GetWeaponTooltip(RinglessStaff), WeaponIDs.DivineIronStaff)},
                    {WeaponIDs.WeightOfTheSea, (GetWeaponTooltip(WeightOfTheSea), WeaponIDs.DivineIronStaff)},
                    {WeaponIDs.Malaspina, (GetWeaponTooltip(Malaspina), WeaponIDs.StaffOfRime)},
                    {WeaponIDs.OraclesStaff, (GetWeaponTooltip(OraclesStaff), WeaponIDs.StaffOfRime)},
                    {WeaponIDs.SacredJar, (GetWeaponTooltip(SacredJar), WeaponIDs.Javelin)},
                    {WeaponIDs.FormlessDance, (GetWeaponTooltip(FormlessDance), WeaponIDs.Javelin)},
                    {WeaponIDs.Successor, (GetWeaponTooltip(Successor), WeaponIDs.Javelin)},
                    #endregion
                };
                KeywordDatabase.Modify("HighestElementalDamage", modifier =>
                {
                    modifier.description = GetModTooltip("ModKeyword_Stat_ElementalDamage_Effect");
                    modifier.displayDetails = true;
                });
                KeywordDatabase.Modify("DashInvincibleTimeBonus", modifier =>
                {
                    modifier.description = GetModTooltip("ModKeyword_Stat_DashInvincibleTime_Effect");
                    modifier.displayDetails = true;
                });
                KeywordDatabase.Modify("DashRecovery", modifier =>
                {
                    modifier.description = GetModTooltip("ModKeyword_Stat_DashRecovery_Effect");
                    modifier.displayDetails = true;
                });
                KeywordDatabase.Modify("WeaponAction_LightningSpear", modifier => modifier.description = GetModTooltip("Superconductor_Effect"));
                KeywordDatabase.Modify("WeaponAction_CarrotAttack", modifier => modifier.description = GetModTooltip("CarrotSword2_Effect"));
                KeywordDatabase.Modify("WeaponAction_ShieldBash", modifier => modifier.description = GetModTooltip("StoneSword_Effect"));
                KeywordDatabase.Modify("Buff_RisingDread", modifier => modifier.description = GetModTooltip("GrippingFear_Effect"));
                KeywordDatabase.Modify("WeaponAction_ShieldThrow", modifier => modifier.description = GetModTooltip("GuardiansReturn_Effect"));
                KeywordDatabase.Modify("WeaponAction_Strike", modifier => modifier.description = GetModTooltip("FlameEaterHaetae_Effect"));
                KeywordDatabase.Modify("WeaponAction_FlameEater", modifier => modifier.description = GetModTooltip("FlameEaterHaetae_Effect2"));
                KeywordDatabase.Modify("WeaponAction_SolisMissio", modifier => modifier.description = GetModTooltip("SolisMissio_Effect"));
                KeywordDatabase.Modify("WeaponAction_Greatsword_Reassemble", modifier => modifier.description = GetModTooltip("BoneGearblade_Effect"));
                KeywordDatabase.Modify("WeaponAction_AmethystConjuration", modifier => modifier.description = GetModTooltip("Lazarion_Effect"));
                KeywordDatabase.Modify("WeaponAction_Greatsword_WoundExplosion", modifier => modifier.description = GetModTooltip("TheInquisitor_Effect"));
                KeywordDatabase.Modify("WeaponAction_ThrowDagger", modifier => modifier.description = GetModTooltip("HeatedBlade_Effect"));
                KeywordDatabase.Modify("WeaponAction_Dagger_BladeZone", modifier => modifier.description = GetModTooltip("ShadowofHope_Effect"));
                KeywordDatabase.Modify("WeaponAction_FuryMP", modifier => modifier.description = GetModTooltip("AcademyMagicSwordLiberation_Effect"));
                KeywordDatabase.Modify("Buff_ThrowComp", modifier => modifier.description = GetModTooltip("DarkVeil_Effect"));
                KeywordDatabase.Modify("WeaponAction_AmmoCompression", modifier => modifier.description = GetModTooltip("M9199_Effect"));
                KeywordDatabase.Modify("WeaponAction_Overheat", modifier => modifier.description = GetModTooltip("Heidi_Effect"));
                KeywordDatabase.Modify("WeaponAction_Eclipse", modifier => modifier.description = GetModTooltip("SolisEkli_Effect"));
                KeywordDatabase.Modify("WeaponAction_SheathAttackFast", modifier => modifier.description = GetModTooltip("FamedSwordMuramasa_Effect"));
                KeywordDatabase.Modify("FrostRelic_Nastrond", modifier => modifier.description = GetModTooltip("FragmentofNastrond_Effect"));
                KeywordDatabase.Modify("WeaponAction_Issen", modifier => modifier.description = GetModTooltip("LightningFlash_Effect"));
                KeywordDatabase.Modify("WeaponAction_FlameSpear", modifier => modifier.description = GetModTooltip("SolisBraca_Effect"));
                KeywordDatabase.Modify("WeaponAction_ThrowCloudBottle", modifier => modifier.description = GetModTooltip("Nephorhax_Effect"));
                KeywordDatabase.Modify("WeaponAction_StaffBolt", modifier => modifier.description = GetModTooltip("IlonicasSpiritStaff_Effect"));
                KeywordDatabase.Modify("WeaponAction_StaffCrystalExplosion", modifier => modifier.description = GetModTooltip("ScaleOfTheAstralSea_Effect"));
                KeywordDatabase.Modify("Buff_StaffGuardExtend", modifier => modifier.description = GetModTooltip("WeightOfTheSea_Effect"));
                KeywordDatabase.Modify("WeaponAction_DeflectingIce", modifier => modifier.description = GetModTooltip("SilenceofEverfrost_Effect"));
                KeywordDatabase.Modify("Buff_Hardening", modifier => modifier.description = GetModTooltip("BreakplateBlade_Effect"));
                KeywordDatabase.Modify("WeaponAction_MoneyWhirlwind", modifier => modifier.description = GetModTooltip("MischievousPrank_Effect"));
                KeywordDatabase.Modify("WeaponAction_C4BombExplosion", modifier => modifier.description = GetModTooltip("TheBarnacle_Effect"));
                KeywordDatabase.Modify("Buff_EnhancedReassemble", modifier => modifier.description = GetModTooltip("BloodlettingGearblade"));
                KeywordDatabase.Modify("WeaponAction_CloudSlash", modifier => modifier.description = GetModTooltip("CeruleanCloudSwordArges_Effect1"));
                KeywordDatabase.Modify("ResidualLightning", modifier => modifier.description = GetModTooltip("CeruleanCloudSwordArges_Effect2"));
                KeywordDatabase.Modify("Buff_Katana_Nanmu", modifier => modifier.description = GetModTooltip("RendbladeRuinDance_Effect"));
                KeywordDatabase.Modify("DebuffDamage", modifier => modifier.visualText = new LocalizedString("ModKeyword_Stat_DeBuffDamage"));
                KeywordDatabase.Modify("DebuffDuration", modifier => modifier.visualText = new LocalizedString("ModKeyword_Stat_DeBuffDuration"));
            }
        }

        // 颜色标签（不变）
        private const string GreenColorTag = "<color=#00FF00>";
        private const string RedColorTag = "<color=#FF0000>";
        private const string ColorEndTag = "</color>";

        // 所有本地化文本
        private static string GreenTag => Tools.GetLocalization("Tooltip_GreenTag");
        private static string RedTag => Tools.GetLocalization("Tooltip_RedTag");
        public static void InitTooltipLocalization()
        {
            InitializationCostumeText = false;
            InitializationWeaponText = false;
            foreach (ModKeyWord Data in LoadModContent.ModKeywordEntityData)
            {
                KeywordDatabase.Modify(Data.ID, modifier =>
                {
                    modifier.description = GetModTooltip($"ModKeyword_{Data.ID}_Effect");
                });
            }
        }

        public static string GetCostumeTooltip(string OrigString)
        {
            if (string.IsNullOrEmpty(OrigString))
            {
                return string.Empty;
            }
            try
            {
                StringBuilder stringBuilder = new StringBuilder();
                string[] lines = OrigString.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string tempLine = line.Trim();
                    if (string.IsNullOrEmpty(tempLine))
                    {
                        continue;
                    }
                    string lineColor = "";
                    if (tempLine.Contains(GreenTag))
                    {
                        lineColor = GreenColorTag;
                        tempLine = tempLine.Replace(GreenTag, "").Trim();
                    }
                    else if (tempLine.Contains(RedTag))
                    {
                        lineColor = RedColorTag;
                        tempLine = tempLine.Replace(RedTag, "").Trim();
                    }
                    string formattedLine = FormatLine(tempLine) ?? string.Empty;
                    formattedLine = KeywordDatabase.Convert(KeywordDatabase.Convert(formattedLine)) ?? string.Empty;
                    formattedLine = AddStringIcon(formattedLine) ?? string.Empty;
                    if (!string.IsNullOrEmpty(lineColor) && !string.IsNullOrEmpty(formattedLine))
                    {
                        formattedLine = $"{lineColor}{formattedLine}{ColorEndTag}";
                    }
                    if (!string.IsNullOrWhiteSpace(formattedLine))
                    {
                        stringBuilder.AppendLine($"- {formattedLine}");
                    }
                }
                return stringBuilder.ToString().TrimEnd('\n');
            }
            catch(Exception ex)
            {
                StarSephiriaMod.LogError($"服装提示加载失败{ex}");
                return "加载失败";
            }
        }
        public static string GetWeaponTooltip(string OrigString)
        {
            if (string.IsNullOrEmpty(OrigString))
            {
                return string.Empty;
            }
            try
            {
                StringBuilder stringBuilder = new StringBuilder();
                {
                    string NewString = "";
                    string[] lines = OrigString.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    bool AddLineStack = false;
                    foreach (string line in lines)
                    {
                        string tempLine = line.Trim();
                        if (string.IsNullOrEmpty(tempLine))
                        {
                            continue;
                        }
                        string formattedLine = FormatLine(tempLine) ?? string.Empty;
                        formattedLine = KeywordDatabase.Convert(KeywordDatabase.Convert(formattedLine)) ?? string.Empty;
                        formattedLine = AddStringIcon(formattedLine) ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(formattedLine))
                        {
                            if (!AddLineStack)
                            {
                                NewString = $"{formattedLine}";
                                AddLineStack = true;
                            }
                            else NewString += $"\n{formattedLine}";
                        }
                    }
                    stringBuilder.AppendLine($"{NewString}");
                }
                return stringBuilder.ToString().TrimEnd('\n');
            }
            catch (Exception ex)
            {
                StarSephiriaMod.LogError($"武器提示加载失败{ex}");
                return "加载失败";
            }
        }
        public static LocalizedString GetModTooltip(string Name)
        {
            string Text = Tools.GetLocalization(Name);
            Text = FormatLine(Text);
            Text = KeywordDatabase.Convert(KeywordDatabase.Convert(Text));
            Text = AddStringIcon(Text);
            return new LocalizedString(Text);
        }
        private static string AddStringIcon(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return string.Empty;
            }
            try
            {
                line = line.Replace("{VALUE}", "");
                line = line.Replace(Tools.GetLocalization("AddIcon_Source_AttackCount"), Tools.GetLocalization("AddIcon_Target_AttackCount"));
                line = line.Replace(Tools.GetLocalization("AddIcon_Source_ExtraTrigger"), Tools.GetLocalization("AddIcon_Target_ExtraTrigger"));
                line = line.Replace(Tools.GetLocalization("AddIcon_Source_ExtraPickup"), Tools.GetLocalization("AddIcon_Target_ExtraPickup"));
                line = line.Replace(Tools.GetLocalization("AddIcon_Source_SwordEnergy"), $"{Tools.GetLocalization("AddIcon_Target_SwordEnergy")}{KeywordDatabase.GetEntity("PhysicalDamage").GetIcon()}");
                line = line.Replace(Tools.GetLocalization("AddIcon_Source_ExtraDamage"), Tools.GetLocalization("AddIcon_Target_DamageDealt"));
                return line;
            }
            catch (Exception ex)
            {
                StarSephiriaMod.LogError($"自定义文本替换失败{ex}");
                return line;
            }
        }

        private static readonly Regex tokenRegex = new Regex(
            @"\{(?<sign>[+\-])(?<value>\d+(?:\.\d+)?%?)\}|\{(?<mul>[×xX])(?<percent>\d+(?:\.\d+)?)%\}",
            RegexOptions.Compiled);
        private static string FormatLine(string line)
        {
            try
            {
                return tokenRegex.Replace(line, match =>
                {
                    if (match.Groups["mul"].Success)
                    {
                        int percent = int.Parse(match.Groups["percent"].Value);
                        string Mtext = $"×{percent}%";

                        return percent >= 100
                            ? $" {GreenColorTag}{Mtext}{ColorEndTag} "
                            : $" {RedColorTag}{Mtext}{ColorEndTag} ";
                    }

                    string value = match.Groups["value"].Value;
                    bool isAdd = match.Groups["sign"].Value == "+";
                    string text = isAdd ? $"+{value}" : $"-{value}";

                    return isAdd
                        ? $" {GreenColorTag}{text}{ColorEndTag} "
                        : $" {RedColorTag}{text}{ColorEndTag} ";
                });
            }
            catch (Exception ex)
            {
                StarSephiriaMod.LogError($"符号文本替换失败{ex}");
                return line;
            }
        }
    }
}