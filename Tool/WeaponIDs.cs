using Mirror;
using System;
using System.Linq;
using Unity.VisualScripting;

namespace SPMod.Tool
{
    public static class WeaponIDs
    {
        // 剑盾
        public const int DefaultSwordandShield = 0;                 // 标准剑盾
        public const int ProtectionoftheFuzz = 1;                   // 绒毛的守护
        public const int UnityofOffenseandDefense = 2;              // 攻防一体
        public const int TinyDestruction = 3;                       // 小小破坏
        public const int StarTwinkle = 4;                           // 星光闪烁
        public const int ColdAnger = 5;                             // 冰冷愤怒
        public const int WanderingAshes = 6;                        // 灰烬飘扬
        public const int TheIndomitable = 7;                        // 无垠原野
        public const int GrippingFear = 9;                          // 收紧恐惧
        public const int CarrotSword = 1000;                        // 胡萝卜剑
        public const int CarrotSword2 = 1001;                       // 胡萝卜剑2
        public const int WarHammer = 1002;                          // 古代锤
        public const int AncientHammer = 1003;                      // 战争锤
        public const int MagicWand = 1004;                          // 法杖
        public const int Rapier = 1006;                             // 刺剑
        public const int Rapier2 = 1007;                            // 闪光的刺剑
        public const int GazeofFlames = 1008;                       // 火焰凝视
        public const int BreathofIce = 1009;                        // 冰之呼吸
        public const int WingsofLightning = 1010;                   // 雷电之翼
        public const int FrostShard = 1013;                         // 霜之碎片
        public const int GlacialBlade = 1014;                       // 冰川之刃
        public const int BlizzardsEve = 1015;                       // 暴风雪的前奏
        public const int Conductor = 1016;                          // 导体
        public const int BallistaSword = 1017;                      // 弩炮盾
        public const int Superconductor = 1018;                     // 超导体
        public const int Excallion = 1019;                          // 大葱剑
        public const int StoneSword = 1020;                         // 岩石剑
        public const int GuardiansReturn = 1023;                    // 归来守护
        public const int Incendium = 1024;                          // 焚火
        public const int SolisMissio = 1025;                        // 索利斯使命
        public const int FlameEaterHaetae = 1026;                   // 食火獬豸
        public const int BlindingSilence = 1028;                    // 耀目的寂静 
        public const int GardenofNeedleIce = 1029;                  // 霜针花园  
        public const int PrismaticMagic = 1030;                     // 棱镜法杖

        // 大剑系列  
        public const int SteelGreatsword = 10;                      // 钢铁巨剑
        public const int Hypersensitivity = 11;                     // 过敏
        public const int SwarmingShadow = 12;                       // 聚集的阴影
        public const int Minimalism = 13;                           // 极简主义
        public const int Negligence = 14;                           // 默杀
        public const int LovingGaze = 15;                           // 爱的眼神
        public const int SwiftRegret = 16;                          // 短暂自责
        public const int TroublingDelight = 17;                     // 不便的喜悦
        public const int TheSecond = 18;                            // 第二次
        public const int Compliance = 19;                           // 顺从的姿态
        public const int WoodenClub = 1100;                         // 木制棍棒
        public const int SpikeClub = 1101;                          // 荆棘棍
        public const int BoneGearblade = 1102;                      // 骨之巨剑
        public const int CreakingSpine = 1103;                      // 吱嘎的脊椎
        public const int FrostboundGreatsword = 1104;               // 寒气缠绕的大剑
        public const int EternalFrostGreatsword = 1105;             // 万年寒霜巨剑
        public const int IcicleSword = 1106;                        // 冰柱剑
        public const int CrimsonThorn = 1107;                       // 深红之脊
        public const int CrimsonVortex = 1108;                      // 深红漩涡
        public const int LightningGreatSwordE3G = 1109;             // 电击大剑「E3G」
        public const int BlackIronHalberd = 1111;                   // 黑铁戟
        public const int LightningGreatSwordE2G = 1112;             // 电击大剑「E2G」
        public const int LightningGreatSwordS3G = 1113;             // 电击大剑「S3G」
        public const int GreatswordOfExorcism = 1114;               // 驱魔的重剑
        public const int TheInquisitor = 1115;                      // 裁判官
        public const int LegacyofTheIllusionist = 1117;             // 幻术师的遗产
        public const int PristineForm = 1118;                       // 太初形态
        public const int MischievousPrank = 1119;                   // 顽皮恶作剧
        public const int Lazarion = 1120;                           // 拉扎里恩
        public const int BloodlettingGearblade = 1121;              // 放血机关剑
        public const int TheEmberforged = 1122;                     // 余烬巨剑
        public const int RedSnakeCrush = 1123;                      // 红蛇碎击
        public const int SolisCineris = 1124;                       // 索利斯·西涅里斯

        // 匕首系列
        public const int Dagger = 20;                               // 短剑
        public const int SafeStart = 21;                            // 安全的开始
        public const int OverTheDoorstep = 22;                      // 跨越门槛
        public const int AridBreeze = 23;                           // 干燥的风
        public const int LightlessBlade = 24;                       // 希望的影子
        public const int PaleSmile = 25;                            // 白色微笑
        public const int ShadowofHope = 26;                         // 无光之刃
        public const int AbsoluteEnvy = 27;                         // 绝对向往
        public const int CreepingDespair = 28;                      // 爬行绝望
        public const int DifficultTrial = 29;                       // 艰难实验
        public const int FireDagger = 1200;                         // 火焰匕首
        public const int HeatedBlade = 1201;                        // 炙热之刃
        public const int BurningFang = 1202;                        // 燃烧之牙
        public const int LightningDagger = 1203;                    // 闪电匕首
        public const int OverhangingStormCloud = 1204;              // 笼罩的阴云
        public const int LightningsWrath = 1205;                    // 雷电之怒
        public const int AlpineEdelweiss = 1206;                    // 高山雪绒花
        public const int Adonis = 1207;                             // 福寿草
        public const int JadeVestige = 1208;                        // 碧玉残影
        public const int UnboundImmersion = 1209;                   // 无尽沉浸
        public const int DarkVeil = 1210;                           // 黑幕
        public const int TomeOfMimicry = 1211;                      // 模仿之书
        public const int AcademyEnchantedSword = 1212;              // 学术魔法剑
        public const int AcademyMagicSwordLiberation = 1213;        // 学院魔法剑-解放
        public const int SolisImber = 1214;                         // 索利斯·因贝尔
        public const int DormantFrost = 1215;                       // 沉眠之霜
        public const int FrozenSmelt = 1216;                        // 冷冻公鱼
        public const int Drífa = 1217;                              // 德里法

        //弩系列
        public const int ColossalCrossbow = 100;                    // 重型弩
        public const int ReloadDevice = 101;                        // 重型弩：重装装置
        public const int AccelerationCore = 102;                    // 重型弩：加速核心
        public const int Twin = 103;                                // 弩：双子
        public const int ScattershotShooter = 104;                  // 重型弩：散射发射器
        public const int GunpowderSynthesis = 105;                  // 重型弩：火药合成
        public const int EnhancedSpecialBolts = 106;                // 重型弩：特殊箭强化
        public const int DualRails = 107;                           // 重型弩：双轨
        public const int FireMine = 108;                            // 重型弩：火焰地雷
        public const int EquippedwithGuidanceModule = 109;          // 重型弩：配备制导模块
        public const int UnstableLightningMark = 110;               // 重型弩：不稳定的闪电印记
        public const int MarkStabilization = 111;                   // 重型弩：标记稳定化
        public const int FrostVeil = 112;                           // 重型弩：霜之帷幕
        public const int RapidFreezeCrystal = 113;                  // 重型弩：速冻结晶
        public const int ExpansionDevice = 114;                     // 重型弩：扩展装置
        public const int CloudGatherer = 115;                       // 重型弩：聚云者
        public const int ExplosiveDevice = 116;                     // 重型弩：爆炸装置
        public const int RapidFireDevice = 117;                     // 重型弩：连发装置
        public const int MiniCannon = 118;                          // 迷你加农炮
        public const int Kagumede = 119;                            // 卡古梅德
        public const int KalimsForm = 120;                          // 卡里姆的形象
        public const int PurpleJacket = 121;                        // 紫甲毒蜂
        public const int M9199 = 122;                               // M-9199
        public const int XRA9 = 123;                                // XRA-9
        public const int SolisIubar = 124;                          // 索利斯·尤巴尔
        public const int BountyHunter = 125;                        // 赏金猎人
        public const int Minigun = 126;                             // 迷你机枪
        public const int TheBarnacle = 127;                         // 藤壶
        public const int M9200 = 128;                               // M-9200

        // 太刀系列
        public const int Blade = 400;                               // 刀
        public const int LongBladeOnohara = 401;                    // 太刀「小野原」
        public const int FamedSwordMuramasa = 402;                  // 名刀「村正」
        public const int BeastMarksSwordHou = 403;                  // 怪刀「毫羽」
        public const int ThunderSwordBronte = 404;                  // 电击刀"布朗特"
        public const int LightningFlash = 405;                      // 电光一闪
        public const int Helbanus = 406;                            // 赫尔巴尼斯
        public const int BlazingHelbanus = 407;                     // 炽焰赫尔巴努斯
        public const int Heidi = 408;                               // 海蒂
        public const int RendbladeHeavenrend = 409;                 // 天光削
        public const int RendbladeDriftingWillow = 410;             // 飞柳
        public const int RendbladeRuinDance = 411;                  // 破舞
        public const int TheAwaitingAzureblade = 412;               // 等待的碧蓝刃
        public const int YuisShortDagger = 413;                     // 优衣的短匕首
        public const int AdamahOath = 414;                          // 亚达玛的誓约
        public const int LibrarySpectralSealingSociety = 416;       // 图书馆幽灵封印会
        public const int LibrarySpectralSealingSociety2 = 417;      // 图书馆幽灵封印会2
        public const int WinterSmithsDraftblade = 419;              // 草图刀
        public const int SilenceofEverfrost = 420;                  // 永霜的沉默
        public const int FragmentofNastrond = 421;                  // 纳斯特朗的碎片
        public const int SolisEkli = 422;                           // 索利斯叶克里
        public const int ArmorBender = 423;                         // 破甲之刃
        public const int BreakplateBlade = 424;                     // 破甲刀
        public const int CeruleanCloudSwordArges = 425;             // 苍云刀“阿尔格斯”

        // 棍系列
        public const int Quarterstaff = 500;                        // 长棍
        public const int FlourishStaff = 501;                       // 花棍棒
        public const int StaffOfGreatMight = 502;                   // 大力棍
        public const int IlonicasSpiritStaff = 503;                 // 精灵融合杖伊洛妮卡
        public const int DivineIronStaff = 505;                     // 神珍铁棍
        public const int WeightOfTheSea = 506;                      // 海之锚
        public const int RinglessStaff = 507;                       // 无环锡杖
        public const int DistantStarlight = 509;                    // 远方的星光
        public const int ScaleOfTheAstralSea = 510;                 // 圣海碎片
        public const int GeoroidSpear = 513;                        // 突击枪矛基奥罗伊德
        public const int RuadhsleaSpear = 514;                      // 突击枪卢阿施蕾娅
        public const int FuarscaSpear = 515;                        // 突击枪普阿尔斯卡
        public const int StaffOfEmbers = 517;                       // 余烬之棒
        public const int FiaolasJudgmentStaff = 518;                // 皮亚奥拉议事槌
        public const int SolisBraca = 519;                          // 索利斯·布拉卡
        public const int StaffOfRime = 521;                         // 白装之杖
        public const int OraclesStaff = 522;                        // 巫女的预言
        public const int Malaspina = 523;                           // 马拉斯皮纳
        public const int StaffOfStorms = 525;                       // 雷霆之杖
        public const int Viduka = 526;                              // 比杜卡
        public const int Nephorhax = 528;                           // 内波拉克斯
        public const int Javelin = 529;                             // 投枪
        public const int SacredJar = 530;                           // 神圣之壶
        public const int FormlessDance = 531;                       // 无形之舞
        public const int Successor = 532;                           // 继承者

        public enum WeaponCategory
        {
            None = 0,
            SwordAndShield,
            GreatSword,
            Dagger,
            Crossbow,
            Katana,
            QuartterStaff
        }
        public static WeaponCategory GetUseWeaponType(int Weapon)
        {
            if (Weapon >= 0 && Weapon < 10 || Weapon >= 1000 && Weapon < 1100)
                return WeaponCategory.SwordAndShield;

            if (Weapon >= 10 && Weapon < 20 || Weapon >= 1100 && Weapon < 1200)
                return WeaponCategory.GreatSword;

            if (Weapon >= 20 && Weapon < 30 || Weapon >= 1200 && Weapon < 1300)
                return WeaponCategory.Dagger;

            if (Weapon >= 100 && Weapon < 200)
                return WeaponCategory.Crossbow;

            if (Weapon >= 400 && Weapon < 500)
                return WeaponCategory.Katana;

            if (Weapon >= 500 && Weapon < 600)
                return WeaponCategory.QuartterStaff;

            return WeaponCategory.None;
        }


        public static int GetPlayerWeapon(this UnitAvatar Player)
        {
            if(StarSephiriaMod.WeaponType.TryGetValue(Player.Name, out int ID))
                return ID;
            return -1;
        }

        public static bool IsWeaponUseNet(this UnitAvatar Player, int Weapon)
        {
            if (!ModConfig.Instance.EnableWeaponModifier)
                return false;

            if (!StarSephiriaMod.WeaponType.TryGetValue(Player.Name, out int PlayerWeaponId))
                return false;

            if (PlayerWeaponId != Weapon)
                return false;

            return true;
        }

        public static bool IsWeaponUseNet(this UnitAvatar Player, params int[] Weapon)
        {
            if (!ModConfig.Instance.EnableWeaponModifier)
                return false;

            if (!StarSephiriaMod.WeaponType.TryGetValue(Player.Name, out int PlayerWeaponId))
                return false;

            if (!Weapon.Contains(PlayerWeaponId))
                return false;

            return true;
        }

        public static bool IsWeapon(this UnitAvatar Player, int Weapon)
        {
            if (!ModConfig.Instance.EnableWeaponModifier || !NetworkServer.active)
                return false;

            if (!StarSephiriaMod.WeaponType.TryGetValue(Player.Name, out int PlayerWeaponId))
                return false;

            if (PlayerWeaponId != Weapon)
                return false;

            return true;
        }

        public static bool IsWeapon(this UnitAvatar Player, params int[] Weapon)
        {
            if (!ModConfig.Instance.EnableWeaponModifier || !NetworkServer.active)
                return false;

            if (!StarSephiriaMod.WeaponType.TryGetValue(Player.Name, out int PlayerWeaponId))
                return false;

            if (!Weapon.Contains(PlayerWeaponId))
                return false;

            return true;
        }
    }
}