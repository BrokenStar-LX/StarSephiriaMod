using HarmonyLib;
using Mirror;
using Newtonsoft.Json;
using SPMod.Tool;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static Charm_StatusInstance;
using static Miracle;
using static SPMod.ModContent.LoadModContent;
using static SPMod.StarSephiriaPatches;
using static SPMod.Tool.EffectConstant;
using static SPMod.Tool.ModuleComponents;
using static UnityEngine.GraphicsBuffer;
using Component = UnityEngine.Component;
using Object = UnityEngine.Object;

namespace SPMod
{
    public class StarSephiriaMod : HorayModBase
    {
        public static Dictionary<string, int> AddPassivePoint = new Dictionary<string, int>();
        public static Dictionary<string, int> WeaponType = new Dictionary<string, int>();

        public static Dictionary<string, string> CostumeType = new Dictionary<string, string>();
        public static Dictionary<string, string> CostumeSkinType = new Dictionary<string, string>();
        public static Dictionary<string, string> CostumeSkinKey = new Dictionary<string, string>();
        public static Dictionary<string, string> CostumeSkinDate = new Dictionary<string, string>();
        public static Dictionary<string, string> CostumeSkinIdToName = new Dictionary<string, string>();
        public static Dictionary<string, Dictionary<string, string>> _modTranslations = new Dictionary<string, Dictionary<string, string>>();

        public static Dictionary<string, string> _displayNames = new Dictionary<string, string>();
        public static List<string> StarTwinkle_AddDamage = new List<string>();
        public static CharacterBuff? GuardBuff = null;
        public static Dictionary<UnitAvatar, Timer> Otter3_AddCrit = new Dictionary<UnitAvatar, Timer>();
        public static Dictionary<string, Timer> BrownRabbit2Hit = new Dictionary<string, Timer>();
        public static Dictionary<string, float> PlayerLife = new Dictionary<string, float>();
        public static List<string> Deer_Buff = new List<string>();
        public static AllPlayerStatus PartyStats = new AllPlayerStatus(0, 0, 0, 0);
        public static Dictionary<uint, Object> registered = new Dictionary<uint, Object>();
        // 经验表
        public static List<int> CustomExpTable = new List<int>
        {
            0, 800, 2600, 4000, 5700, 7400, 9400, 11600, 13800, 16000,
            18400, 20600, 23600, 26600, 29600, 32600, 35600, 38600, 42000, 45600,
            49000, 52600, 56000, 59600, 63800, 68000, 72200, 76600, 80800, 85200
        };

        protected override void OnModLoaded()
        {
            if (!_isPatched)
            {
                LoadAllDependenciesInDirectory();
                InitializeHarmonyAndMod();
                InitializeCostumeSkinDate();
                AddStatsName();
                ModifyExpTable();
                LoadProData();
                ModConfig.Initialize();
                _isPatched = true;
            }
            HorayModAPI.OnLoadWeaponDatabase += OnLoadWeaponDatabase;
            HorayModAPI.OnLoadMiracleDatabase += OnLoadMiracleDatabase;
            HorayModAPI.OnLoadKeywordDatabase += OnLoadKeywordDatabase;
            HorayModAPI.OnLoadItemDatabase += OnLoadItemDatabase;
            HorayModAPI.OnLocalizationReady += OnLocalizationInit;
            HorayModAPI.OnLoadPassiveDatabase += OnLoadPassiveDatabase;
            HorayModAPI.OnAllDatabasesReady += OnAllDatabasesReady;
        }

        protected override void OnModUnloaded()
        {
            HorayModAPI.OnLoadWeaponDatabase -= OnLoadWeaponDatabase;
            HorayModAPI.OnLoadMiracleDatabase -= OnLoadMiracleDatabase;
            HorayModAPI.OnLoadKeywordDatabase -= OnLoadKeywordDatabase;
            HorayModAPI.OnLoadItemDatabase -= OnLoadItemDatabase;
            HorayModAPI.OnLocalizationReady -= OnLocalizationInit;
            HorayModAPI.OnLoadPassiveDatabase -= OnLoadPassiveDatabase;
            HorayModAPI.OnAllDatabasesReady -= OnAllDatabasesReady;
            WeaponType.Clear();
            CostumeType.Clear();
            CostumeSkinType.Clear();
            CostumeSkinKey.Clear();
            CostumeSkinDate.Clear();
            CostumeSkinIdToName.Clear();
            Projectile.Clear();
            DPSData.Clear();
            Otter3_AddCrit.Clear();
            BrownRabbit2Hit.Clear();
            StarTwinkle_AddDamage.Clear();
            Deer_Buff.Clear();
            ColdAngerTime.Clear();
            HeatedBladeEffect.Clear();
            HeatedBladeStack.Clear();
            RosionList.Clear();
        }

        private void LoadAllDependenciesInDirectory()
        {
            string modDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string[] dllFiles = Directory.GetFiles(modDirectory, "*.dll");
            Assembly[] domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            HashSet<string> loadedAssemblySimpleNames = new HashSet<string>(domainAssemblies.Select(a => a.GetName().Name));
            HashSet<string> loadedAssemblyFullNames = new HashSet<string>(domainAssemblies.Select(a => a.FullName));
            foreach (string dllFile in dllFiles)
            {
                string fileName = Path.GetFileName(dllFile);
                Assembly currentExec = Assembly.GetExecutingAssembly();
                if (fileName.Equals(Path.GetFileName(currentExec.Location), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                try
                {
                    AssemblyName dllAssemblyName = AssemblyName.GetAssemblyName(dllFile);
                    string simpleName = dllAssemblyName.Name;
                    string fullName = dllAssemblyName.FullName;
                    if (loadedAssemblyFullNames.Contains(fullName))
                    {
                        LogInfo($"[依赖跳过] {fileName} FullName已在域中，无需加载");
                        continue;
                    }
                    if (loadedAssemblySimpleNames.Contains(simpleName))
                    {
                        LogInfo($"[依赖跳过] {fileName} 域内已存在 {simpleName}，跳过磁盘dll");
                        continue;
                    }
                    Assembly loaded = Assembly.LoadFrom(dllFile);
                    LogInfo($"[依赖加载成功] {fileName} -> {loaded.FullName}");
                }
                catch (BadImageFormatException)
                {
                    LogInfo($"[依赖跳过] {fileName} 不是有效的.NET程序集");
                }
                catch (FileNotFoundException)
                {
                    LogError($"[依赖失败] {fileName} 文件不存在");
                }
                catch (Exception ex)
                {
                    LogError($"[依赖失败]{fileName}: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        private static bool _isPatched = false;
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InitializeHarmonyAndMod()
        {
            var harmony = new Harmony("com.Star.SephiriaMod");
            harmony.PatchAll();
            PatchPlayerInputController(harmony);
        }

        private void OnLocalizationInit(HorayModLocalizationContext ctx)
        {
            LoadModTranslations(ctx);
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            TextFile.InitTooltipLocalization();
            ModBullet.RegisterAllToPool();
            ModFireData.RegisterAllFxToPool();
            LogInfo("模组本地化系统初始化完成");
        }

        public static void LoadModTranslations(HorayModLocalizationContext ctx)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] allResources = assembly.GetManifestResourceNames();
            foreach (string resourceFullName in allResources)
            {
                if (!resourceFullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    continue;
                try
                {
                    string[] nameParts = resourceFullName.Split('.');
                    string languageCode = nameParts[^2];
                    using Stream stream = assembly.GetManifestResourceStream(resourceFullName);
                    using StreamReader reader = new StreamReader(stream);
                    string jsonContent = reader.ReadToEnd();
                    var translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
                    if (translations != null && translations.Count > 0)
                    {
                        _modTranslations[languageCode] = translations;
                        foreach (var (key, value) in translations)
                        {
                            ctx.AddText(languageCode, key, value);
                        }
                        LogInfo($"已加载内嵌翻译: {languageCode}.json ({translations.Count} 条)");
                    }
                    else
                    {
                        LogError($"翻译文件 {languageCode}.json 为空或格式错误");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"加载内嵌翻译失败 {resourceFullName}: {ex.Message}");
                }
            }
        }

        private static void OnLanguageChanged(string newLanguage)
        {
            TextFile.InitTooltipLocalization();
            TextFile.InitializationCostumeText = false;
            TextFile.InitializationWeaponText = false;
        }

        private void LoadProData()
        {
            GameObject[] Bullet = Resources.LoadAll<GameObject>("Bullet");
            GameObject[] Melee = Resources.LoadAll<GameObject>("MeleeCollision");
            List<GameObject> NewPro = new List<GameObject>();
            NewPro.AddRange(Bullet);
            NewPro.AddRange(Melee);
            foreach (GameObject Pro in NewPro)
            {
                Projectile.Add(Pro.name.Replace("(Clone)", ""), Pro);
            }
        }

        private void InitializeCostumeSkinDate()
        {
            // 粉红兔
            CostumeSkinDate.Add("PinkRabbit_1", "c3a1568b-ead1-4aeb-9f5b-832d6e9d19d7");
            CostumeSkinDate.Add("PinkRabbit_2", "cd6fd202-3b9b-4d17-a092-3b14ddbcb873");
            CostumeSkinDate.Add("PinkRabbit_3", "45f46af6-46fa-48e6-ad6a-f6c75b0ee225");

            // 盔甲兔
            CostumeSkinDate.Add("ArmoredRabbit_1", "e0f66eeb-d1fe-4f62-a9e4-bdca10781d99");
            CostumeSkinDate.Add("ArmoredRabbit_2", "cf013e07-e084-4d93-aceb-96237a53f259");
            CostumeSkinDate.Add("ArmoredRabbit_3", "3355c7f1-0649-4426-9020-1dbf3b0a7843");

            // 棕兔
            CostumeSkinDate.Add("BrownRabbit_1", "723cb1ed-22de-4e51-9802-ae1530474004");
            CostumeSkinDate.Add("BrownRabbit_2", "95cebcc9-ee70-485f-b230-025db11fbe39");
            CostumeSkinDate.Add("BrownRabbit_3", "1f33b6a7-cec8-44d8-9483-0c3f84137ad3");

            // 辫发兔 DarkBrownRabbit
            CostumeSkinDate.Add("DarkBrownRabbit_1", "b699989a-6095-4bb1-b7b2-542108f735f4");
            CostumeSkinDate.Add("DarkBrownRabbit_2", "55072adc-6de9-4beb-856e-f3c058e4704b");
            CostumeSkinDate.Add("DarkBrownRabbit_3", "4330177f-210a-4e4f-8c7c-3613a7628478");

            // 橙兔 MandarinRabbit
            CostumeSkinDate.Add("MandarinRabbit_1", "ca0c6f38-a37d-4703-b8c6-23286e2a8454");
            CostumeSkinDate.Add("MandarinRabbit_2", "bbb60a5d-3034-4359-9968-3fa4001508cd");
            CostumeSkinDate.Add("MandarinRabbit_3", "121df93e-b53d-4922-93b5-73e3ff53aead");

            // 红衣兔 RedHolyRabbit
            CostumeSkinDate.Add("RedHolyRabbit_1", "ce9214ed-ef4c-43e3-942e-0dc3c9ab70b0");
            CostumeSkinDate.Add("RedHolyRabbit_2", "0af43604-00fb-4d47-b807-979f9453ec5c");
            CostumeSkinDate.Add("RedHolyRabbit_3", "5599730b-a9dc-42aa-9f82-87cc27b6bb63");

            // 白兔 WhiteRabbit
            CostumeSkinDate.Add("WhiteRabbit_1", "04600976-14e6-48d3-9e91-f5edb8abf04c");
            CostumeSkinDate.Add("WhiteRabbit_2", "a74a2c68-e11f-4f62-adc3-47e29bc463c8");
            CostumeSkinDate.Add("WhiteRabbit_3", "cdab303b-a90c-4047-a20e-f0149a864900");

            // 翼耳兔 HolyRabbit
            CostumeSkinDate.Add("HolyRabbit_1", "0d5855c6-1304-43a4-a4ef-b0e331fbbdb5");
            CostumeSkinDate.Add("HolyRabbit_2", "3c9580e8-5477-4f18-a069-98023b866df4");
            CostumeSkinDate.Add("HolyRabbit_3", "cbda3cf0-212a-4ade-861a-6d609611bc4a");

            // 火猫 FireCat
            CostumeSkinDate.Add("FireCat_1", "c8b464d6-8e38-4524-9da4-143e3e63084d");
            CostumeSkinDate.Add("FireCat_2", "1b4f07a5-0802-4a54-ae0f-d88a650d4050");
            CostumeSkinDate.Add("FireCat_3", "0f41fe45-1eb8-499f-99c6-293b033115b5");

            // 狐狸 Fox
            CostumeSkinDate.Add("Fox_1", "7906db2b-f2b1-4c8c-ada9-bba4ff2ee8ca");
            CostumeSkinDate.Add("Fox_2", "412c516c-1540-48a4-97d7-bdbe4b525fed");
            CostumeSkinDate.Add("Fox_3", "851a47ff-cea8-431f-8e66-4079587cf183");

            // 青蛙 Frog
            CostumeSkinDate.Add("Frog_1", "60f43f28-1922-4516-88a0-67635c21cdc9");
            CostumeSkinDate.Add("Frog_2", "eb9ba662-5206-4a43-acc3-ca67c619be6a");
            CostumeSkinDate.Add("Frog_3", "7325e939-9da2-42eb-87b1-7317f83dbc95");

            // 鼹鼠 Mole
            CostumeSkinDate.Add("Mole_1", "a12909d1-7700-4ccf-bd52-2b1277a6d99c");
            CostumeSkinDate.Add("Mole_2", "55b9c5c1-a607-4f88-9a8a-fa3d10f5d50d");
            CostumeSkinDate.Add("Mole_3", "21619904-31f6-4efa-8836-de00ba276f1b");

            // 水獭 Otter
            CostumeSkinDate.Add("Otter_1", "50f73ef0-9869-43b3-b0b4-ae3cd0c60806");
            CostumeSkinDate.Add("Otter_2", "84cf85b7-32ca-4633-85fa-629e22a6d853");
            CostumeSkinDate.Add("Otter_3", "fe5091e1-909b-4f34-8a9e-d71a9ce6a406");

            // 白狼 WhiteWolf
            CostumeSkinDate.Add("WhiteWolf_1", "6e311e37-63ad-473e-8f27-d56e0d9a8517");
            CostumeSkinDate.Add("WhiteWolf_2", "ea126e4d-a900-4250-94ff-9eddf758f357");
            CostumeSkinDate.Add("WhiteWolf_3", "b91a3227-14e7-4812-aece-5db9736f6eff");

            // 巫师兔子 WizardRabbit
            CostumeSkinDate.Add("WizardRabbit_1", "7cc54a8d-afde-4e1f-8dd0-2eadde171ecd");
            CostumeSkinDate.Add("WizardRabbit_2", "fdfda8b9-ebc2-4103-8cd8-b9df2ea7a55a");
            CostumeSkinDate.Add("WizardRabbit_3", "9f934136-4cae-41d3-8bfb-490c0d2bb5f0");

            // 老鹰 Bird
            CostumeSkinDate.Add("Bird_1", "434b70ab-da3e-4e4a-a600-18222134604f");
            CostumeSkinDate.Add("Bird_2", "0514cabf-44ce-4b11-93fb-6a684468cfdd");
            CostumeSkinDate.Add("Bird_3", "39f83800-8449-40c6-b79f-6d253e746a5c");

            // 鳄鱼 Crocodile
            CostumeSkinDate.Add("Crocodile_1", "58b41830-b04e-4310-ba22-99f1106be973");
            CostumeSkinDate.Add("Crocodile_2", "17379100-2e13-40ae-af70-431c257f6c38");
            CostumeSkinDate.Add("Crocodile_3", "0d790443-c110-4e0b-aabe-a89731a21a3d");

            // 鹿 Deer
            CostumeSkinDate.Add("Deer_1", "f7562045-ec92-45d0-9b3d-2b10581dd191");
            CostumeSkinDate.Add("Deer_2", "7ddaea18-e3bc-4c4c-a5a0-b4fb68be466a"); 
            CostumeSkinDate.Add("Deer_3", "842c0706-3777-4dc5-8608-495ca7894888");

            // 幸运仙子 Luck
            CostumeSkinDate.Add("Luck_1", "d0b48c5c-fa55-431b-88f6-86ce82ba6f19");
            CostumeSkinDate.Add("Luck_2", "9568c777-4666-4f76-ad9e-b1b442dcec84");
            CostumeSkinDate.Add("Luck_3", "e103a732-39e3-4fd8-accf-d1abf83c5cd0");

            // 农夫松鼠 Squirrel
            CostumeSkinDate.Add("Squirrel_1", "f239ecad-6781-4703-ae04-793b78516c4e");
            CostumeSkinDate.Add("Squirrel_2", "477aff25-86ca-49cb-b608-aefbe2a41d55");
            CostumeSkinDate.Add("Squirrel_3", "582fd8ad-8d6d-4596-a8b3-93221dbe5251");

            // 鬼魂 Ghost
            CostumeSkinDate.Add("Ghost_1", "ea8d7300-eaab-429f-beb8-09feefb84826");
            CostumeSkinDate.Add("Ghost_2", "1e58b852-5d04-4c1c-ad71-8f762f1d7787");
            CostumeSkinDate.Add("Ghost_3", "52537107-4a0f-4509-a896-500a55a50c77");

            // 学者蜥蜴 Seren
            CostumeSkinDate.Add("Seren_1", "1336dfc6-5e16-4b90-bc14-5419a82f75cc");
            CostumeSkinDate.Add("Seren_2", "efd1b711-a014-43c1-82ed-6e7418a00d5c");
            CostumeSkinDate.Add("Seren_3", "5fa99515-b39d-4482-a3d6-9806eee3e302");

            // 失去翅膀的蝙蝠 Bat
            CostumeSkinDate.Add("Bat_1", "ecaceae8-f5d6-4a40-85a3-22c4959e8696");
            CostumeSkinDate.Add("Bat_2", "b7130f46-ecc6-476b-87c4-233791eb1c96");
            CostumeSkinDate.Add("Bat_3", "1d1cbc87-2d05-4a67-af2e-d82cd9926f36");

            // 骷髅 Skeleton
            CostumeSkinDate.Add("Skeleton_1", "2d43983e-0d40-44fa-8cd6-cb4493f6a82f");
            CostumeSkinDate.Add("Skeleton_2", "15615e60-8dc5-49fb-b329-5566193ac442");
            CostumeSkinDate.Add("Skeleton_3", "376d981c-9034-4956-834a-a1da49e539d7");

            // 乌龟 Turtle
            CostumeSkinDate.Add("Turtle_1", "860cc326-bed5-460f-a92a-652f82f6f1bc");
            CostumeSkinDate.Add("Turtle_2", "7a3cbf22-a2c3-4744-be3b-6f9460927634");
            CostumeSkinDate.Add("Turtle_3", "a52fcc6d-d050-4e43-a1c2-a5725e23ec08");

            // 森林猫 ForestCat
            CostumeSkinDate.Add("ForestCat_1", "11ec244e-b4b9-483f-9abb-296c18c7f4da");
            CostumeSkinDate.Add("ForestCat_2", "b766e49e-98c3-41a1-82a8-e8ed3e1d5432");
            CostumeSkinDate.Add("ForestCat_3", "fa867788-8c11-448e-978c-1dcba8f1a80c");

            CostumeSkinIdToName.Clear();
            foreach (KeyValuePair<string, string> Pair in CostumeSkinDate)
                CostumeSkinIdToName[Pair.Value] = Pair.Key;
        }

        private void PatchPlayerInputController(Harmony harmony)
        {
            Type playerInputType = typeof(PlayerInputController);
            if (playerInputType == null)
            {
                LogError("找不到PlayerInputController类型");
                return;
            }

            MethodInfo updateMethod = playerInputType.GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            if (updateMethod == null)
            {
                LogError("找不到PlayerInputController.Update方法");
                return;
            }

            MethodInfo patchMethod = typeof(StarSephiriaMod).GetMethod("PlayerInputUpdatePostfix");
            harmony.Patch(updateMethod, postfix: new HarmonyMethod(patchMethod));
        }

        private void ModifyExpTable()
        {
            try
            {
                int baseIncrement = CustomExpTable[29] - CustomExpTable[28];
                double growthRate = 1.0196;
                int currentLevel = CustomExpTable.Count - 1;
                int currentExp = CustomExpTable.Last();
                for (int level = currentLevel + 1; level <= 999; level++)
                {
                    int levelsFromBase = level - 30;
                    double incrementMultiplier = Math.Pow(growthRate, levelsFromBase);
                    int currentIncrement = (int)Math.Round(baseIncrement * incrementMultiplier);
                    if (currentExp + currentIncrement < 0 || currentExp + currentIncrement > int.MaxValue || level >= 500)
                    {
                        LogError($"经验值溢出，等级{level}停止扩展，需要经验{currentExp}");
                        break;
                    }

                    currentExp += currentIncrement;
                    CustomExpTable.Add(currentExp);
                }
                int[] ExpTable = CustomExpTable.ToArray();

                FieldInfo ModifyField = AccessTools.Field(typeof(LevelController), "ExpTableByLevel");
                if (ModifyField != null && ModifyField.IsInitOnly)
                {
                    ModifyField.SetValue(null, ExpTable);
                    LogInfo("修改经验表成功!");
                }
            }
            catch (Exception ex)
            {
                LogError($"修改经验表失败: {ex.Message}");
            }
        }

        private void AddStatsName()
        {
            Dictionary<string, string> stats = new Dictionary<string, string>
            {
                {"MODADDCUSTOMERINTENSITY","模组自定义属性增加"},
                {"TABLET","启用石板"},
                {"REPLENISHMENT","启用补给"},
                {"HARDMODEUNLOCK","困难模式解锁"},
                {"MINDARKCLOUD","迷你黑暗乌云"},
                {"COUNTMINIBOSSKILL","红茶袋计数"},
                {"COMBOBONUSDAMAGE","启用机智20增伤"},
                {"IGNOREDEFENSE","无视防御"},
                {"GUARDRESIST","防御消耗减少"},
                {"DASHATTACKICEHAMMER","启用冰锤效果"},
                {"ENHANCEDMINIBALLISTA","启用弩炮剑效果"},
                {"DARKCLOUDSWEEP","启用超导体效果"},
                {"REDSNAKEEYEATKCOOLDOWNBONUS","普攻减少红蛇之眼冷却"},
                {"WOUNDSTACK","伤口恶化堆叠"},
                {"WOUNDEXPLOSION","启用裁判官效果"},
                {"EVASIONFURY","启用碧玉残影效果"},
                {"EVASIONFURY_NEXT","启用无尽沉浸效果"},
                {"CROSSBOWMG","启用加速核心效果"},
                {"MOVESPEEDTOATTACKSPEED","启用双子效果"},
                {"ADDAMMOWHENCRITICAL","启用火药合成效果"},
                {"LIGHTNINGCROSSBOW","弩发射闪电箭概率"},
                {"LIGHTNINGCROSSBOW_CLOUD","闪电箭命中恢复乌云"},
                {"LIGHTNINGCROSSBOWELECTRIC","闪电箭命中施加触电"},
                {"POISONSTACK","中毒叠加"},
                {"JELLYFISHDOUBLEATTACK","水母双重攻击"},
                {"JELLYFISHBASICDAMAGE","水母伤害增加"},
                {"SPEEDSHEATH","太刀特攻受攻速加成"},
                {"FASTSHEATH","启用村正效果"},
                {"RANGESHEATH","启用怪刀伤害"},
                {"EVASIONSHEATH","太刀特攻受闪避加成"},
                {"KATANADASHSTACK","启用海蒂效果"},
                {"KATANAGHOSTCOUNTER","启用图书馆效果"},
                {"ECHOOFTHEGLACIERUPGRADE","冰川的回声伤害倍率"},
                {"ECHOOFTHEGLACIERPARRY","启用永霜的沉默效果"},
                {"DEFLECTINGICE","启用冰上滑行"},
                {"KATANAELECTRICCHARGE","启用电光一闪效果"},
                {"FIXEDATTACKSPEED","最终攻击速度"},
                {"DASHEVASION","冲刺无敌判定为闪避"},
                {"ABSOLUTEEVASION","必定闪避值"},
                {"BLUEBURNCHANGE","启用封印的加斯特效果"},
                {"DARKCLOUDICE","启用雷霜蝶效果"},
                {"FLAMESWORDRETURN","启用陨铁耳环效果"},
                {"FLAMESWORDCALLBACKFROST","启用永恒蚀效果：冰霜武具触发太阳剑"},
                {"FLAMESWORDFROST","启用永恒蚀效果：太阳剑转为冰属性"},
                {"FROSTRELICFLAME","启用永恒蚀效果：冰霜武具转为火属性"},
                {"MAGICQUICKCAST","启用冥想书效果"},
                {"RAYETSTARFRAGMENT","启用雷伊星碎片效果"},
                {"BOLTMAGICHOMING","启用鼠法师的吊坠效果"},
                {"EXECUTION","启用贝鲁特之镰效果"},
                {"ADDITIONALELEMENTALDAMAGEBONUS","武器附加伤害增加"},
                {"SUPERPLANET","启用乐谱银河效果"},
                {"ICECROSSBOWBUFF","启用霜之帷幕效果"},
                {"POISONDEBUFFDAMAGEBONUS","毒减益伤害"},
                {"FIXEDDASH","最终冲刺次数"},
                {"STAFFROLLINGPROTECTION","使用棍获得防御力%护盾"},
                {"BLOCKCASTMAGIC","禁止主动释放魔法"},
                {"STAFFEXTEND","棍长度延伸"},
                {"DEFENSEKATANA","破甲刃防御力增加普攻伤害"},
                {"STAFFALLGUARD","棍全方向防御"},
                {"SPEARDASHATTACKBONUSBYLASTDASH","消耗冲刺获得枪冲刺伤害增加"},
                {"ICESPEARWITHWEAPONATTACK", "武器攻击触发沃尔斯帕"},
                {"ENHANCEDCHAKRAM", "查克拉姆造成的伤害"},
                {"HOMINGCHAKRAM", "鼠标控制查克拉姆位置"},
                {"HEALINGPENALTY", "治疗效果衰减"},
                {"PARTYBUFF", "翼耳兔共享增益效果"},
                {"REVIVEPLAYERHASTE", "复活队友的速度"},
                {"FURYINVINCIBLETIME", "狂怒无敌时间"},
                {"REDSNAKEEYEDAMAGEBONUS", "红蛇之眼造成的伤害"},
                {"CROSSBOWAMMO", "弹匣容量"},
                {"ICEBUFF_DECREASEATTACKSPEED", "扩展装置攻击速度减少"},
                {"PLASMAACTIVE", "启用等离子效果"},
                {"PLASMADAMAGE", "等离子伤害"},
                {"BURNEVO", "余烬10效果"},
                {"PHYSICALDAMAGE","物理伤害"},
                {"FIREDAMAGE","火焰伤害"},
                {"ICEDAMAGE","冰霜伤害"},
                {"LIGHTNINGDAMAGE","闪电伤害"},
                {"TRUEDAMAGE","无视防御伤害"},
                {"ALLDAMAGEBONUS","所有伤害加成"},
                {"HPSTEAL","生命偷取"},
                {"HPREGEN","生命恢复"},
                {"GIVEUPREWARDDICE","放弃奖励骰子"},
                {"BOSSREWARDDICE","Boss奖励骰子"},
                {"FREEITEMREROLL","抽卡免费刷新"},
                {"EXTRAITEMCHOICES","额外物品选择"},
                {"EXTRAMIRACLECHOICES","额外奇迹选择"},
                {"EXTRAWEAPONCHOICES","额外武器选择"},
                {"LUCK","幸运值"},
                {"FRUITCOUNT","水果串数量上限"},
                {"ELITEDAMAGE","对Boss伤害加成"},
                {"SAPPHIREBONUS","蓝宝石掉落增加"},
                {"FINALWEAPONDAMAGE","武器伤害"},
                {"SPECIALATTACKDAMAGEBONUS","特殊攻击伤害"},
                {"BASICATTACKDAMAGEBONUS","普通攻击伤害"},
                {"LASTBASICATTACKDAMAGE","最后一击造成的伤害"},
                {"CROSSBOWADDITIONALMAGAZINE","十字弩额外弹夹"},
                {"SPECIALATTACKCOSTREDUCTION","特殊攻击消耗减少"},
                {"CROSSBOWRELOADSPEED","十字弩装填速度"},
                {"SPECIALATTACKRANGE","特殊攻击范围"},
                {"SPECIALATTACKSPEED","特殊攻击速度"},
                {"SPECIALATTACKCHARGESPEED","旋风斩蓄力速度"},
                {"WEAPONDAMAGEBONUSBYDASHCOUNT","冲刺次数转化为武器造成的伤害"},
                {"ATTACKSPEED","攻击速度"},
                {"WEAPONRANGE","武器范围"},
                {"DASHATTACKDAMAGEBONUS","冲刺攻击伤害"},
                {"DAGGERTRANCEBONUS","专注获取次数"},
                {"DASHCOUNT","冲刺次数"},
                {"DASHRECOVERY","冲刺恢复速度"},
                {"DASHINVINCIBLETIMEBONUS","冲刺无敌时间"},
                {"DASHSPEEDBONUSPERCENT","冲刺速度加成"},
                {"INFINITYMP","无限魔力"},
                {"FINALMP","最大魔法加成"},
                {"MPREGEN","MP回复速度"},
                {"MPSTEAL","吸取MP"},
                {"MPSKILLDAMAGE","消耗MP伤害"},
                {"MPREGENMULTIPLE","MP恢复效率"},
                {"MAGICDAMAGEBONUS","魔法书伤害"},
                {"MAGICCRITICAL","魔法暴击率"},
                {"MAGICCRITICALDAMAGEBONUS","魔法暴击伤害"},
                {"COOLDOWNRECOVERYSPEED","冷却回复速度"},
                {"EVASION","闪避率"},
                {"DEFENSETOATTACK","防御转伤害"},
                {"DAMAGEREDUCTION","防御力"},
                {"THORNS","荆棘"},
                {"TOUGHNESS","韧性"},
                {"FLAMESWORDADDITIONALATTACK","太阳剑额外触发次数"},
                {"FLAMESWORDADDITIONALATTACKFROMWEAPON","太阳剑额外触发次数-武器"},
                {"FLAMESWORDADDITIONALATTACKFROMMAGIC","太阳剑额外触发次数-魔法"},
                {"FLAMESWORDCRITICAL","太阳剑暴击率"},
                {"FLAMESWORDIGNOREDEFENSE","太阳剑无视防御"},
                {"FLAMESWORDLUCK","太阳剑概率增强"},
                {"FLAMESWORDFASTFALL","太阳剑下落速度"},
                {"FLAMESWORDMAX","太阳剑最大数量"},
                {"FLAMESWORDCRITICALDAMAGERATE","太阳剑暴击伤害"},
                {"FLAMESWORDDAMAGE","太阳剑伤害"},
                {"FLAMESWORDPICKBONUS","太阳剑额外获得"},
                {"CRITICAL","暴击率"},
                {"CRITICALDAMAGEBONUS","暴击伤害"},
                {"WEAPONCRITICAL","武器暴击率"},
                {"WEAPONCRITICALDAMAGE","武器暴击伤害"},
                {"IGNORESUPERARMOR","超级护甲穿透"},
                {"DARKCLOUDSPEED","乌云消耗速度"},
                {"DARKCLOUDRESTOREDURINGBATTLE","战斗中乌云恢复速度"},
                {"DARKCLOUDDAMAGE","乌云伤害"},
                {"DARKCLOUDKEEP","乌云消耗概率"},
                {"DARKCLOUDMULTISHOT","乌云多重射击"},
                {"DARKCLOUDATKSPEEDBONUS","乌云攻击速度加成"},
                {"DARKCLOUDLUCK","乌云概率强化"},
                {"FOLLOWERDAMAGE","同伴伤害"},
                {"FOLLOWERATTACKSPEED","同伴攻击速度"},
                {"FOLLOWERREVIVE","同伴复活速度"},
                {"FOLLOWERDEFENSE","同伴防御"},
                {"FOLLOWERCRITICALCONTRIBUTE","同伴暴击效率"},
                {"BURNSPEED","燃烧速度"},
                {"BURNDAMAGE","燃烧伤害"},
                {"BURNADD","燃烧施加次数"},
                {"BURNSTACK","燃烧叠加层数"},
                {"PLANETATTACKSPEED","行星攻击速度"},
                {"PLANETDAMAGE","行星伤害"},
                {"CHARGINGCHARMBONUS","冰霜武具充能速度"},
                {"AIRSLASHHASTE","祈祷剑鞘充能速度"},
                {"VOLUSPAHASTE","沃尔斯帕充能速度"},
                {"FROSTRELICDAMAGE","冰霜武具伤害"},
                {"CHARGINGCHARMAMPLIFY","冰霜武具触发次数"},
                {"FROSTRELICMPDAMAGE","冰霜武具MP伤害加成"},
                {"ELECTRICLUCK","触电施加时额外触电概率"},
                {"ELECTRICQUICKNESS","触电触发速度"},
                {"ELECTRICSTACK","触电叠加层数"},
                {"ELECTRICDAMAGE","触电伤害"},
                {"HPPOTIONBONUS","生命药水加成"},
                {"FREEZETHRESHOLD","冰冻所需次数减少"},
                {"FREEZEDAMAGE","冰冻伤害"},
                {"MONEYDROP","叶子掉落"},
                {"EXPDROP","经验掉落"},
                {"NEGOTIATION","谈判力"},
                {"DEBUFFDAMAGE","减益伤害"},
                {"BUFFDURATION","增益持续时间"},
                {"DEBUFFDURATION","减益持续时间"},
                {"STAT_DIRECTWEAPONDAMAGE","武器造成的直接伤害"},
                {"STAT_MELEEDAMAGE","近战伤害"},
                {"STAT_COINTODAMAGE","迈达斯手镯加成"},
                {"STAT_RETURNCOIN","消耗叶子返还"},
                {"STAT_SCORECOUNT","乐谱数量"},
                {"STAT_SCORECOUNTADD","乐谱数量加成属性"},
                {"HARDMODEHEALINGPENALTY", "困难模式治疗衰减"},
                {"PLANETIGNITEDAMAGE", "行星点燃伤害"},
                {"STAT_FROSTRELICADDMULT", "霜寒铁砧加成"},
                {"FROSTBITEDAMAGE", "冻伤效果倍率"},
                {"STAT_COINTOEXP", "成长基金效果"},
                {"MAGICCOSTREDUCE", "魔法消耗减少"},
                {"EVASIONCOOLDOWN", "闪避减少魔法冷却"},
                {"BELTUI", "多用途腰带效果"},
                {"STAT_ELECTRICDARKCLOUD", "瓶装闪电效果"},
                {"STAT_ALLPLAYERDEFENSE", "全体防御力"},
                {"STAT_ALLPLAYERELEMENTALDAMAGE", "全体最高元素伤害"},
                {"STAT_ALLPLAYERALLDAMAGE", "全体所有伤害放大"},
                {"STAT_MPREGENTODAMAGE", "纯白核心效果"},
                {"GOLDHAND", "启用谈判4效果"},
                {"AIRSLASHMINI", "迷你剑鞘"},
                {"FLAMESWORDMAGICDAMAGE", "启用书库之阳效果"},
                {"FLAMESWORDFURY", "启用索利斯·因贝尔效果"},
                {"WEAPONCRITICALDAMAGEAMPLIFY", "武器暴击伤害倍率"},
                {"WEAPONDAMAGEBONUS", "武器倍率加成"},
                {"STAFFMULTITHROWINGSPEARDOUBLE", "启用无形之舞效果"},
                {"STAT_ROSION", "侵蚀"},
                {"STAT_CURRENTROSION", "侵蚀保存"},
                {"RIPOSTELASER", "强化还击剑碎片"},
                {"ICICLEVINEORBIT", "启用霜针花园效果"},
                {"CHARMDAMAGEBONUS", "神器伤害"},
                {"WINDDAMAGE", "风属性伤害"},
                {"STAT_WINDEROSIONDAMAGE", "风蚀伤害"},
                {"STAT_WINDEROSIONMULT", "风蚀基础倍率"},
                {"STAT_WINDEROSIONCOUNT", "风蚀可触发次数"},
                {"STAT_WINDEROSIONLUCK", "风蚀额外触发概率"},
                {"STAT_WINDEROSIONOVERFLOW", "风帆倍率"},
                {"STAT_WINDEROSIONOVERFLOWADD", "风帆额外倍率"},
                {"STAT_GOLDOBTAINED", "总获取叶子数量"},
                {"WOUNDDAMAGE", "伤口恶化伤害"},
                {"ATKSPDCHAKRAM", "查克拉姆受攻速影响"},
                {"ICEHAMMERSCYTHE", "冰锤变成冰镰刀"},
                {"DASHCOUNTDAMAGE", "冲刺计数伤害"},
                {"RECEIVEDDAMAGEINCREASE", "通用易伤"},
                {"RECEIVEDDAMAGEREDUCTION", "通用免伤"},
                {"PHYSICALDEFENSE", "物理抗性"},
                {"FIREDEFENSE", "火焰抗性"},
                {"ICEDEFENSE", "冰抗性"},
                {"LIGHTNINGDEFENSE", "闪电抗性"},
                {"ELECTRICEARRINGDAMAGE", "桑德耳环伤害"},
                {"ELECTRICEARRINGCOUNT", "桑德耳环攻击次数"},
                {"ELECTRICEARRINGSWEEP", "耀目的寂静效果"},
                {"STAT_MAXDIMENSIONPOCKET", "许愿池额外容量计算"},
                {"STAT_BRINVENTORYSLOT", "背包额外槽位"},
                {"STAT_MAGICCOOLDOWNREDUCTION", "魔法书冷却减少"},
                {"STAT_DEFENSETOALLDAMAGE", "防御力转所有伤害"},
                {"STAT_ALLPLAYERCRIT", "全体玩家暴击几率"},
                {"STAT_INVINCIBLESPECIALATTACK", "绝对戒指效果"},
                {"STAT_DRINKPOTIONSTACK", "喝下高级药水数量"},
                {"STAT_TELESCOPECOUNT", "望远镜数量"},
                {"STAT_SCALEOFTHEASTRALSEAEFFECT", "圣海碎片效果计数器"},
                {"STAT_ECOLOGICALSPHEREEFFECT", "生态球效果"},
                {"STAT_STRIKEBACK", "反击"},
                {"STAT_ELECTRICICE", "电冰体效果"},
                {"STAT_COMPANIONSTRIKEBACK", "同伴继承反击"},
                {"STAT_WINDEROSIONCRITDAMAGE", "风蚀暴击伤害"},
                {"STAT_WINDEROSIONDOT", "空气钟倍率"},
                {"STAT_CYANORBUP", "强化青绿色宝珠"}
            };
            _displayNames.AddRange(stats);
        }

        public static void LogInfo(string message)
        {
            if (ModConfig.Instance.DebugLogging)
            {
                HorayDebug.Log(message);
            }
        }

        public static void LogInfoProjectile(string message)
        {
            if (ModConfig.Instance.DebugLoggingProjectile)
            {
                HorayDebug.Log(message);
            }
        }

        public static void LogInfoWeapon(string message)
        {
            if (ModConfig.Instance.DebugLoggingWeapon)
            {
                HorayDebug.Log(message);
            }
        }

        public static void LogInfoDungeon(string message)
        {
            if (ModConfig.Instance.DebugLoggingDungeon)
            {
                HorayDebug.Log(message);
            }
        }

        public static void LogInfoBoss(string message)
        {
            if (ModConfig.Instance.DebugLoggingBoss)
            {
                HorayDebug.Log(message);
            }
        }

        public static void LogError(string message)
        {
            HorayDebug.Log(message);
        }

        public static void PlayerInputUpdatePostfix(PlayerInputController __instance)
        {
            try
            {
                if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
                {
                    UI_DevCommandlinePanel? panel = UIManager.Instance?.GetElement<UI_DevCommandlinePanel>();
                    if (panel != null && !panel.IsOpened) panel.Open();
                }
            }
            catch (Exception ex)
            {
                LogError($"补丁执行出错: {ex}");
            }
        }

        public static Dictionary<int, float> CrossBowFireTime = new Dictionary<int, float>();
        public static Dictionary<int, float> CrossBowRoldTime = new Dictionary<int, float>();
        public static Dictionary<int, int> CrossBowMagazineCount = new Dictionary<int, int>();
        public static Dictionary<int, int> CrossBowMagazineCapacity = new Dictionary<int, int>();
        private void AddBaseStats(WeaponEntity Weapon, int Delta)
        {
            foreach (string Stat in BaseStatsName)
            {
                if (Stat == TextFile.WindDamage) continue;
                Weapon.SetAddonStat(Stat, Delta);
            }
        }
        private void OnLoadWeaponDatabase()
        {
            if (ModConfig.Instance.EnableWeaponModifier)
            {
                //—— 剑盾 ——
                WeaponDatabase.Modify(WeaponIDs.WanderingAshes, Weapon => Weapon.ChangeAddon<WeaponAddon_DashAttack>(Addon => Addon.dashAttackDamageBonus = 0));//灰烬飘扬
                WeaponDatabase.Modify(WeaponIDs.TheIndomitable, Weapon => Weapon.ChangeAddon<WeaponAddon_BasicAttack>(Addon => Addon.basicAttackDamageBonus = 25));//无垠原野
                WeaponDatabase.Modify(WeaponIDs.CarrotSword, Weapon => AddBaseStats(Weapon, 5));//胡萝卜剑
                WeaponDatabase.Modify(WeaponIDs.CarrotSword2, Weapon => AddBaseStats(Weapon, 5));//胡萝卜剑2
                WeaponDatabase.Modify(WeaponIDs.Rapier2, Weapon =>
                {
                    Weapon.SetAddonStat(TextFile.AttackSpeed, 0);
                    Weapon.SetAddonStat(TextFile.BasicAttackDamageBonus, 20);
                });//闪光的细剑
                WeaponDatabase.Modify(WeaponIDs.GlacialBlade, Weapon => Weapon.SetAddonStat(TextFile.AirSlashHaste, 0));//冰川之刃
                WeaponDatabase.Modify(WeaponIDs.Superconductor, Weapon => Weapon.SetAddonStat("SWEEPCOSTREDUCTION", 50));//超导体
                WeaponDatabase.Modify(WeaponIDs.Excallion, Weapon => Weapon.SetAddonStat(TextFile.MagicDamageBonus, 0));//大葱剑
                WeaponDatabase.Modify(WeaponIDs.BlindingSilence, Weapon =>
                {
                    Weapon.SetAddonStat(TextFile.ElectricEarringDamage, 0);
                    Weapon.SetAddonStat(TextFile.ElectricEarringCount, 0);
                    Weapon.SetAddonStat(TextFile.ElectricEarringSweep, 0);
                    Weapon.SetSimple(Action =>
                    {
                        NewWeaponFireData Attack = Action.specialAttacks[1];
                        Attack.attackDashSpeed *= 0.5f;
                        Attack.attackDashTime *= 0.5f;
                        Attack.swingFxPrefab = null;
                    });
                });//耀目的寂静

                //—— 大剑 ——
                WeaponDatabase.Modify(WeaponIDs.Compliance, Weapon =>
                {
                    Weapon.enhanceFromId = WeaponIDs.SwarmingShadow;
                    WeaponAddon Addon = WeaponDatabase.FindWeaponById(12).mainWeaponPrefab.GetComponent<WeaponSimple>().addons[0];
                    WeaponAddon Addon2 = WeaponDatabase.FindWeaponById(13).mainWeaponPrefab.GetComponent<WeaponSimple>().addons[0];
                    WeaponSimple component = Weapon.mainWeaponPrefab.GetComponent<WeaponSimple>();
                    component.addons = new WeaponAddon[] { Addon, Addon2 };
                    NewWeaponFireData Attack = component.specialAttacks[0];
                    Attack.forceFinalCombo = true;
                    Attack.attackDashAngle = component.basicComboAttacks[0].attackDashAngle;
                    Attack.attackDashCurveType = component.basicComboAttacks[0].attackDashCurveType;
                    Attack.attackDashDirectionType = component.basicComboAttacks[0].attackDashDirectionType;
                    Attack.attackDashSpeed = component.basicComboAttacks[0].attackDashSpeed;
                    Attack.attackDashTime = component.basicComboAttacks[0].attackDashTime;
                    component.basicComboAttacks[2] = Attack;
                });// 顺从的姿态
                WeaponDatabase.Modify(WeaponIDs.LightningGreatSwordE3G, Weapon =>
                {
                    Weapon.mainWeaponPrefab.GetComponent<WeaponSimple_GreatSword>().sweepCost = 0;
                });// 电击大剑[E3G]
                WeaponDatabase.Modify(WeaponIDs.Lazarion, Weapon =>
                {
                    Weapon.mainWeaponPrefab.GetComponent<WeaponSimple_GreatSword>().sweepCost = 10;
                });// 拉扎里恩
                WeaponDatabase.Modify(WeaponIDs.Negligence, Weapon =>
                {
                    WeaponSimple_GreatSword Target = Weapon.mainWeaponPrefab.GetComponent<WeaponSimple_GreatSword>();
                    Target.sweepCost = 3;
                    NewWeaponFireData_MeleeAttack Special = (NewWeaponFireData_MeleeAttack)Object.Instantiate(Target.specialAttacks[0]);
                    Special.attackDashCurveType = EAttackDashCurveType.Deceleration;
                    Special.attackDashDirectionType = EAttackDashDirectionType.InputDirection;
                    Special.attackDashSpeed = 0f;
                    Special.attackDashTime = 0f;
                    Special.damageMultiplier = 3f;
                    Target.specialAttacks[0] = Special;
                });// 无视
                WeaponDatabase.Modify(WeaponIDs.Minimalism, Weapon =>
                {
                    WeaponSimple Target = WeaponDatabase.FindWeaponById(513).mainWeaponPrefab.GetComponent<WeaponSimple>();
                    NewWeaponFireData_MeleeAttack Dash = (NewWeaponFireData_MeleeAttack)Object.Instantiate(Target.dashAttacks[0]);
                    Dash.attackDashSpeed = 32f;
                    Weapon.mainWeaponPrefab.GetComponent<WeaponSimple>().dashAttacks[0] = Dash;
                });// 极简主义
                WeaponDatabase.Modify(WeaponIDs.TheSecond, Weapon =>
                {
                    WeaponSimple Target = WeaponDatabase.FindWeaponById(513).mainWeaponPrefab.GetComponent<WeaponSimple>();
                    NewWeaponFireData_MeleeAttack Dash = (NewWeaponFireData_MeleeAttack)Object.Instantiate(Target.dashAttacks[0]);
                    Dash.attackDashSpeed = 32f;
                    Weapon.mainWeaponPrefab.GetComponent<WeaponSimple>().dashAttacks[0] = Dash;
                });// 第二次
                WeaponDatabase.Modify(WeaponIDs.BlackIronHalberd, Weapon =>
                {
                    WeaponSimple Target = WeaponDatabase.FindWeaponById(513).mainWeaponPrefab.GetComponent<WeaponSimple>();
                    NewWeaponFireData_MeleeAttack Dash = (NewWeaponFireData_MeleeAttack)Object.Instantiate(Target.dashAttacks[0]);
                    Dash.attackDashSpeed = 32f;
                    Weapon.mainWeaponPrefab.GetComponent<WeaponSimple>().dashAttacks[0] = Dash;
                });// 黑铁戟
                WeaponDatabase.Modify(WeaponIDs.SwarmingShadow, Weapon =>
                {
                    Weapon.ChangeAddon<WeaponAddonGreatsword_BasicAttack>(Addon => Addon.basicAttackDamage = 0);
                    Weapon.ChangeAddon<WeaponAddonGreatsword_BasicAttack>(Addon => Addon.specialAttackDamage = 25);
                });//聚集的阴影
                WeaponDatabase.Modify(WeaponIDs.Negligence, Weapon => Weapon.ChangeAddon<WeaponAddonGreatsword_SuperQuick>(Addon =>
                {
                    Addon.specialAttackDamage = 0;
                    Addon.specialAttackSpeed = 15;
                }));//无视
                WeaponDatabase.Modify(WeaponIDs.SwiftRegret, Weapon =>
                {
                    Weapon.ChangeAddon<WeaponAddonGreatsword_BasicAttack>(Addon => Addon.basicAttackDamage = 0);
                    Weapon.ChangeAddon<WeaponAddonGreatsword_BasicAttack>(Addon => Addon.specialAttackDamage = 25);
                });//短暂自责
                WeaponDatabase.Modify(WeaponIDs.FrostboundGreatsword, Weapon =>
                {
                    Weapon.SetAddonStat(TextFile.FreezeDamage, -40);
                    Weapon.ChangeAddon<WeaponAddonCommon_AdditionalElementalDamage>(Addon => Addon.additionalDamagePercent = 10);
                });//寒气缠绕的大剑
                WeaponDatabase.Modify(WeaponIDs.EternalFrostGreatsword, Weapon =>
                {
                    Weapon.SetAddonStat(TextFile.FreezeDamage, 0);
                    Weapon.ChangeAddon<WeaponAddonCommon_AdditionalElementalDamage>(Addon => Addon.additionalDamagePercent = 0);
                });//万年寒霜巨剑
                WeaponDatabase.Modify(WeaponIDs.IcicleSword, Weapon =>
                {
                    Weapon.SetAddonStat(TextFile.FreezeDamage, 20);
                    Weapon.ChangeAddon<WeaponAddonCommon_AdditionalElementalDamage>(Addon => Addon.additionalDamagePercent = 0);
                    Weapon.ChangeAddon<WeaponAddonCommon_DebuffAttack>(Addon => Addon.debuffPercent = 100);
                });//冰柱剑
                WeaponDatabase.Modify(WeaponIDs.CrimsonVortex, Weapon => Weapon.SetAddonStat(TextFile.WoundStack, 0));//深红漩涡
                WeaponDatabase.Modify(WeaponIDs.LightningGreatSwordS3G, Weapon => Weapon.SetAddonStat(TextFile.ElectricDamage, 0));//电击大剑S3G
                WeaponDatabase.Modify(WeaponIDs.GreatswordOfExorcism, Weapon =>
                {
                    Weapon.SetAddonStat(TextFile.FreezeDamage, 0);
                    Weapon.SetAddonStat(TextFile.AirSlashHaste, 9949);
                    Weapon.ChangeAddon<WeaponAddonCommon_AdditionalElementalDamage>(Addon => Addon.additionalDamagePercent = 0);
                });//驱魔的重剑
                WeaponDatabase.Modify(WeaponIDs.LegacyofTheIllusionist, Weapon => Weapon.SetAddonStat(TextFile.CooldownRecoverySpeed, 0));//幻术师的遗产
                WeaponDatabase.Modify(WeaponIDs.RedSnakeCrush, Weapon => Weapon.SetAddonStat(TextFile.RedSnakeEyeDamageBonus, 0));//红蛇碎击
                WeaponDatabase.Modify(WeaponIDs.SolisCineris, Weapon => Weapon.SetAddonStat(TextFile.FlameSwordAdditionalAttackFromWeapon, 0));//索利斯·西涅里斯

                //—— 匕首 ——
                WeaponDatabase.Modify(WeaponIDs.LightningDagger, Weapon => Weapon.ChangeAddon<WeaponAddonCommon_AdditionalElementalDamage>(Addon => Addon.additionalDamagePercent = 40));//闪电匕首
                WeaponDatabase.Modify(WeaponIDs.OverhangingStormCloud, Weapon =>
                {
                    Weapon.SetAddonStat(TextFile.LightningDamage, 0);
                    Weapon.SetAddonStat(TextFile.DarkCloudDamage, 0);
                    Weapon.ChangeAddon<WeaponAddonCommon_AdditionalElementalDamage>(Addon => Addon.additionalDamagePercent = 40);
                    Weapon.SetSimple(Action =>
                    {
                        WeaponSimple_Dagger Target = WeaponDatabase.FindWeaponById(WeaponIDs.UnboundImmersion).mainWeaponPrefab.GetComponent<WeaponSimple_Dagger>();
                        if (Action is WeaponSimple_Dagger Dagger)
                        {
                            Dagger.daggerBarPrefab = Target.daggerBarPrefab;
                            Dagger.furyDisableTimer.time = 999999f;
                        }
                    });
                });//笼罩的阴云
                WeaponDatabase.Modify(WeaponIDs.LightningsWrath, Weapon =>
                {
                    Weapon.SetAddonStat(TextFile.ElectricStack, 0);
                    Weapon.ChangeAddon<WeaponAddonCommon_AdditionalElementalDamage>(Addon => Addon.additionalDamagePercent = 40);
                    Weapon.ChangeAddon<WeaponAddonCommon_DebuffAttack>(Addon => Addon.debuffPercent = 100);
                });//雷电之怒
                WeaponDatabase.Modify(WeaponIDs.Adonis, Weapon => Weapon.SetAddonStat(TextFile.WeaponCriticalDamageAmplify, 0));//福寿草
                WeaponDatabase.Modify(WeaponIDs.FrozenSmelt, Weapon => Weapon.SetAddonStat(TextFile.IceDamage, 0));//冰冻公鱼
                WeaponDatabase.Modify(WeaponIDs.SafeStart, Weapon =>
                {
                    Weapon.SetSimple(Action =>
                    {
                        if (Action is WeaponSimple_Dagger Dagger)
                        {
                            Dagger.wideParry = true;
                            Dagger.easyParry = true;
                        }
                    });
                });//安全的开始
                WeaponDatabase.Modify(WeaponIDs.LightlessBlade, Weapon =>
                {
                    Weapon.SetSimple(Action =>
                    {
                        if (Action is WeaponSimple_Dagger Dagger)
                        {
                            Dagger.wideParry = true;
                            Dagger.easyParry = true;
                        }
                    });
                });//希望的影子
                WeaponDatabase.Modify(WeaponIDs.PaleSmile, Weapon =>
                {
                    Weapon.SetSimple(Action =>
                    {
                        if (Action is WeaponSimple_Dagger Dagger)
                        {
                            Dagger.wideParry = true;
                            Dagger.easyParry = true;
                        }
                    });
                });//白色微笑
                WeaponDatabase.Modify(WeaponIDs.TomeOfMimicry, Weapon =>
                {
                    Weapon.SetSimple(Action =>
                    {
                        WeaponSimple_Dagger Target = WeaponDatabase.FindWeaponById(WeaponIDs.UnboundImmersion).mainWeaponPrefab.GetComponent<WeaponSimple_Dagger>();
                        if (Action is WeaponSimple_Dagger Dagger)
                        {
                            Dagger.daggerBarPrefab = Target.daggerBarPrefab;
                            Dagger.furyDisableTimer.time = 999999f;
                        }
                    });
                });//模仿之书
                WeaponDatabase.Modify(WeaponIDs.SolisImber, Weapon =>
                {
                    Weapon.SetSimple(Action =>
                    {
                        if (Action is WeaponSimple_Dagger Dagger)
                            Dagger.furyDisableTimer.time = 999999f;
                    });
                });//索利斯·因贝尔

                //—— 弩 ——
                foreach (WeaponEntity Weapon in WeaponDatabase.GetAll().ToList())
                {
                    if (Weapon.mainWeaponPrefab == null) continue;
                    if (Weapon.mainWeaponPrefab.TryGetComponent<WeaponSimple_Crossbow>(out WeaponSimple_Crossbow Target))
                    {
                        if (!CrossBowFireTime.ContainsKey(Weapon.id)) CrossBowFireTime.Add(Weapon.id, Target.fireIntervalTimer.time);//发射速度
                        if (!CrossBowRoldTime.ContainsKey(Weapon.id)) CrossBowRoldTime.Add(Weapon.id, Target.reloadTime);//装填速度
                        if (!CrossBowMagazineCount.ContainsKey(Weapon.id)) CrossBowMagazineCount.Add(Weapon.id, Target.defaultMagazineCount);//弹匣数量
                        if (!CrossBowMagazineCapacity.ContainsKey(Weapon.id)) CrossBowMagazineCapacity.Add(Weapon.id, Target.defaultMagazineCapacity);//弹匣容量
                    }
                }
                WeaponDatabase.Modify(WeaponIDs.ExplosiveDevice, Weapon => Weapon.SetCrossBowValue(true, 0.75f));//爆炸装置
                WeaponDatabase.Modify(WeaponIDs.MiniCannon, Weapon => Weapon.SetCrossBowValue(true, 0.75f, 1.5f));//榴弹炮
                WeaponDatabase.Modify(WeaponIDs.M9199, Weapon =>
                {
                    Weapon.SetCrossBowValue(true, 1f, 1f);
                    Weapon.SetSimple(Action =>
                    {
                        if (Action is WeaponSimple_Crossbow Cross)
                            Cross.specialAttackCost = 3;
                    });
                });//M9199
                WeaponDatabase.Modify(WeaponIDs.XRA9, Weapon =>
                {
                    Weapon.SetCrossBowValue(true, 1f, 1f);
                    Weapon.SetSimple(Action =>
                    {
                        if (Action is WeaponSimple_Crossbow Cross)
                            Cross.specialAttackCost = 3;
                    });
                });//XRA-9
                WeaponDatabase.Modify(WeaponIDs.M9200, Weapon =>
                {
                    Weapon.SetCrossBowValue(true, 1f, 1.5f);
                    Weapon.SetSimple(Action =>
                    {
                        if (Action is WeaponSimple_Crossbow Cross)
                            Cross.specialAttackCost = 3;
                    });
                });//M9200
                WeaponDatabase.Modify(WeaponIDs.Twin, Weapon => Weapon.SetCrossBowValue(true, 1.2f, 2f));//双子
                WeaponDatabase.Modify(WeaponIDs.RapidFireDevice, Weapon => Weapon.SetCrossBowValue(true, 1f, 4f, 1, 8));//连发装置
                WeaponDatabase.Modify(WeaponIDs.ReloadDevice, Weapon => Weapon.SetCrossBowValue(true, 1f, -1, 2));//填装装置
                WeaponDatabase.Modify(WeaponIDs.ScattershotShooter, Weapon => Weapon.SetCrossBowValue(true, 1f, 1, 2, 2));//散弹发射器
                WeaponDatabase.Modify(WeaponIDs.GunpowderSynthesis, Weapon => Weapon.SetCrossBowValue(true, 1f, -1, 2, 2));//火药合成
                WeaponDatabase.Modify(WeaponIDs.GunpowderSynthesis, Weapon => Weapon.SetAddonStat(TextFile.WeaponCritical, 0));//火药合成
                WeaponDatabase.Modify(WeaponIDs.MarkStabilization, Weapon =>
                {
                    Weapon.SetAddonStat(TextFile.ElectricDamage, 0);
                    Weapon.SetSimple(Action =>
                    {
                        WeaponSimple_Katana Target = WeaponDatabase.FindWeaponById(WeaponIDs.LightningFlash).mainWeaponPrefab.GetComponent<WeaponSimple_Katana>();
                        NewWeaponFireData FireData = Object.Instantiate(Target.electricChargeIssenFireData);
                        FireData.attackDashSpeed = 32f;
                        FireData.damageMultiplier = 7.2f;
                        FireData.attackDashAngle = Action.specialAttacks[0].attackDashAngle;
                        FireData.attackDashCurveType = Action.specialAttacks[0].attackDashCurveType;
                        FireData.attackDashDirectionType = Action.specialAttacks[0].attackDashDirectionType;
                        FireData.attackDashSpeed = Action.specialAttacks[0].attackDashSpeed;
                        FireData.attackDashTime = Action.specialAttacks[0].attackDashTime;
                        Weapon.mainWeaponPrefab.GetComponent<WeaponSimple>().specialAttacks[0] = FireData;
                    });
                });//标记稳定化
                WeaponDatabase.Modify(WeaponIDs.KalimsForm, Weapon => Weapon.SetAddonStat(TextFile.JellyfishBasicDamage, 0));//卡里姆的形象
                WeaponDatabase.Modify(WeaponIDs.SolisIubar, Weapon =>
                {
                    Weapon.SetCrossBowValue(true, 1.15f);
                    Weapon.SetAddonStat(TextFile.FlameSwordDamage, 0);
                    Weapon.SetAddonStat(TextFile.FinalWeaponDamage, 0);
                });//索利斯·尤巴尔

                //—— 太刀 ——
                WeaponDatabase.Modify(WeaponIDs.LongBladeOnohara, Weapon =>
                {
                    Weapon.SetAddonStat(TextFile.AttackSpeed, 0);
                    Weapon.SetSimple(Action =>
                    {
                        if (Action is WeaponSimple_Katana Katana)
                        {
                            Katana.sheathAttackTime = 0.75f / 1.1f;
                        }
                    });
                });//太刀小野原
                WeaponDatabase.Modify(WeaponIDs.FamedSwordMuramasa, Weapon => Weapon.SetAddonStat(TextFile.AttackSpeed, 0));//名刀村正
                WeaponDatabase.Modify(WeaponIDs.BeastMarksSwordHou, Weapon =>
                {
                    Weapon.SetAddonStat(TextFile.SpecialAttackRange, -33);
                    Weapon.SetAddonStat(TextFile.AttackSpeed, 0);
                    Weapon.SetSimple(Action =>
                    {
                        if (Action is WeaponSimple_Katana Katana)
                        {
                            Katana.sheathAttackTime = 0.75f / 1.1f;
                            Katana.specialAttackCost = 0;
                        }
                    });
                });//怪刀毫羽
                WeaponDatabase.Modify(WeaponIDs.BlazingHelbanus, Weapon => Weapon.SetAddonStat(TextFile.FireDamage, 0));//炽焰赫尔巴努斯
                WeaponDatabase.Modify(WeaponIDs.RendbladeDriftingWillow, Weapon =>
                {
                    Weapon.SetAddonStat(TextFile.Critical, 0);
                    Weapon.SetAddonStat(TextFile.LastBasicAttackDamage, 30);
                    Weapon.SetAddonStat(TextFile.WeaponRange, 30);
                    Weapon.SetSimple(Action =>
                    {
                        if (Action is WeaponSimple_Katana Katana)
                        {
                            Katana.attackToFillGaugeValue = 0.17f;
                        }
                    });
                });//飞柳
                WeaponDatabase.Modify(WeaponIDs.YuisShortDagger, Weapon =>
                {
                    Weapon.SetAddonStat(TextFile.MagicCritical, 0);
                });//优衣的短匕首
                WeaponDatabase.Modify(WeaponIDs.LibrarySpectralSealingSociety, Weapon => Weapon.SetAddonStat(TextFile.DashAttackDamageBonus, 0));//图书馆幽灵封印会
                WeaponDatabase.Modify(WeaponIDs.LibrarySpectralSealingSociety2, Weapon => Weapon.SetAddonStat(TextFile.DashAttackDamageBonus, 0));//图书馆幽灵封印会2
                WeaponDatabase.Modify(WeaponIDs.WinterSmithsDraftblade, Weapon =>
                {
                    Weapon.SetAddonStat(TextFile.IceDamage, 0);
                    Weapon.ChangeAddon<WeaponAddonCommon_AdditionalElementalDamage>(Addon => Addon.additionalDamagePercent = 20);
                });//草图刀
                WeaponDatabase.Modify(WeaponIDs.SilenceofEverfrost, Weapon =>
                {
                    Weapon.SetAddonStat(TextFile.IceDamage, 0);
                    Weapon.ChangeAddon<WeaponAddonCommon_AdditionalElementalDamage>(Addon => Addon.additionalDamagePercent = 20);
                });//永霜的沉默
                WeaponDatabase.Modify(WeaponIDs.FragmentofNastrond, Weapon =>
                {
                    Weapon.SetAddonStat(TextFile.IceDamage, 0);
                    Weapon.ChangeAddon<WeaponAddonCommon_AdditionalElementalDamage>(Addon => Addon.additionalDamagePercent = 20);
                });//纳斯特朗的碎片
                WeaponDatabase.Modify(WeaponIDs.CeruleanCloudSwordArges, Weapon =>
                {
                    Weapon.SetSimple(Action =>
                    {
                        if (Action is WeaponSimple_Katana Katana)
                        {
                            Katana.residualLightningStackMax = 50;
                        }
                    });
                });//苍云刀
                WeaponDatabase.Modify(WeaponIDs.Heidi, Weapon =>
                {
                    Weapon.SetSimple(Action =>
                    {
                        if (Action is WeaponSimple_Katana Katana)
                        {
                            Katana.dashStackFixedAttackSpeed = 100;
                        }
                    });
                });//海蒂

                //—— 棍 ——
                WeaponDatabase.Modify(WeaponIDs.StaffOfGreatMight, Weapon => Weapon.SetAddonStat(TextFile.BasicAttackDamageBonus, 30));//大力棍
                WeaponDatabase.Modify(WeaponIDs.RuadhsleaSpear, Weapon => Weapon.SetAddonStat(TextFile.AttackSpeed, 0));//突击枪卢阿施蕾娅
                WeaponDatabase.Modify(WeaponIDs.SolisBraca, Weapon => Weapon.SetAddonStat(TextFile.WeaponRange, 33));//索利斯·布拉卡
                WeaponDatabase.Modify(WeaponIDs.OraclesStaff, Weapon => Weapon.SetAddonStat(TextFile.WeaponRange, 30));//巫女的预言
                WeaponDatabase.Modify(WeaponIDs.Malaspina, Weapon =>
                {
                    Weapon.SetAddonStat(TextFile.IceDamage, 0);
                    Weapon.ChangeAddon<WeaponAddonCommon_DebuffAttack>(Addon => Addon.debuffPercent = 60);
                });//马拉斯皮纳
                WeaponDatabase.Modify(WeaponIDs.Viduka, Weapon => Weapon.SetAddonStat(TextFile.EnhancedChakram, 0));//比杜卡
            }
            if (ModConfig.Instance.AddWeaponEnhancements)
            {
                WeaponDatabase.Modify(WeaponIDs.Minimalism, Weapon => Weapon.enhanceFromId = WeaponIDs.SteelGreatsword);
                WeaponDatabase.Modify(WeaponIDs.BlackIronHalberd, Weapon => Weapon.enhanceFromId = -1);
                WeaponDatabase.Modify(WeaponIDs.SafeStart, Weapon => Weapon.enhanceFromId = WeaponIDs.Dagger);

                foreach (ModWeapon Data in ModWeaponData)
                    Data.CreateWeapon();
            }
            Skin.GetAddAllModSkin();
        }

        public static void OnLoadItemDatabase()
        {
            CreateModItems();
            if (ModConfig.Instance.ModifyMerchant)
            {
                ItemDatabase.Modify(51, item => item.cost = CommonCost);
            }
            if (ModConfig.Instance.ModifyCharm)
            {
                if (ModConfig.Instance.AddCategory)
                {
                    //彗星
                    ItemDatabase.Modify(1011, item => item.categories = Tools.SetCategories("COMET"));
                    ItemDatabase.Modify(1011, item =>
                    {
                        item.categories = Tools.SetCategories("COMET");
                        Charm_DashAttackDamage Effect = item.resourcePrefab.GetComponent<Charm_DashAttackDamage>();
                        Effect.bonusByLevel = new int[4] { 10, 20, 30, 40 };
                    }); // 风草围巾
                    ItemDatabase.Modify(1049, item => item.categories = Tools.SetCategories("COMET")); // 小太刀八云
                    ItemDatabase.Modify(1082, item => item.categories = Tools.SetCategories("COMET")); // 压迫头带
                    ItemDatabase.Modify(1093, item => item.categories = Tools.SetCategories("COMET")); // 荆棘丛
                    ItemDatabase.Modify(1149, item => item.categories = Tools.SetCategories("COMET", "WINDSONG")); // 金色斗篷
                    ItemDatabase.Modify(1279, item =>
                    {
                        item.categories = Tools.SetCategories("COMET", "SHADOW");
                        item.isDual = true;
                        Charm_StatusInstance Effect = item.resourcePrefab.GetComponent<Charm_StatusInstance>();
                        Effect.stats[1] = Tools.SetStatusGroup("EVASION", 300, 600, 900, 1200);
                    }); // 拉里德的手镯
                    //锻造
                    ItemDatabase.Modify(1010, item => item.categories = Tools.SetCategories("WEAPON")); // 盾牌术
                    ItemDatabase.Modify(1086, item => item.categories = Tools.SetCategories("WEAPON")); // 盘子
                    ItemDatabase.Modify(1147, item => item.categories = Tools.SetCategories("WEAPON")); // 彩虹羽毛
                    ItemDatabase.Modify(1156, item => item.categories = Tools.SetCategories("WEAPON")); // 玻璃锤
                    ItemDatabase.Modify(1157, item => item.categories = Tools.SetCategories("WEAPON")); // 兔子头盔
                    ItemDatabase.Modify(1218, item => item.categories = Tools.SetCategories("WEAPON")); // 香囊
                    ItemDatabase.Modify(1221, item => item.categories = Tools.SetCategories("WEAPON")); // 蓝色护腕
                    ItemDatabase.Modify(1225, item =>
                    {
                        item.categories = Tools.SetCategories("WEAPON");
                        if (item.resourcePrefab.TryGetComponent<Charm_StatusInstance>(out var Status))
                        {
                            Status.isWeaponRelatedCharm = false;
                        }
                    }); // 黄铜镜碎片
                    ItemDatabase.Modify(1262, item => item.categories = Tools.SetCategories("WEAPON")); // 神秘摆锤
                    ItemDatabase.Modify(1283, item =>
                    {
                        item.categories = Tools.SetCategories("WEAPON");
                        if (item.resourcePrefab.TryGetComponent<Charm_StatusInstance>(out var Status))
                        {
                            Status.isWeaponRelatedCharm = false;
                        }
                    }); // 勇气护身符
                    //命运
                    ItemDatabase.Modify(1126, item => item.categories = Tools.SetCategories("FORTUNE")); // 金色枫叶
                    ItemDatabase.Modify(1155, item => item.categories = Tools.SetCategories("FORTUNE")); // 四叶草
                    ItemDatabase.Modify(1172, item => item.categories = Tools.SetCategories("FORTUNE")); // 帕拉斯的卡牌
                    ItemDatabase.Modify(1195, item => item.categories = Tools.SetCategories("FORTUNE")); // 万花筒
                    ItemDatabase.Modify(1300, item => item.categories = Tools.SetCategories("ALCHEMY", "FORTUNE")); // 生气的土豆
                    //派对
                    ItemDatabase.Modify(1140, item => item.categories = Tools.SetCategories("PARTY")); // 鼓励旗帜
                    ItemDatabase.Modify(3031, item => item.categories = Tools.SetCategories("PARTY")); // 祝福
                    //飓风
                    ItemDatabase.Modify(1030, item =>
                    {
                        Charm_WoodenBox Effect = item.resourcePrefab.GetComponent<Charm_WoodenBox>();
                        Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_WoodenBox") };
                    }); // 多用途腰带
                    ItemDatabase.Modify(1053, item =>
                    {
                        Charm_AddStatByDefense Effect = item.resourcePrefab.GetComponent<Charm_AddStatByDefense>();
                        Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_AddStatByDefense") };
                    }); // 盾牌包
                }
                //连击更改
                ItemDatabase.Modify(1030, item => item.categories = Tools.SetCategories("ACADEMY")); // 多用途腰带
                ItemDatabase.Modify(1081, item => item.categories = Tools.SetCategories("STURDY", "SHADOW")); // 标本喙
                ItemDatabase.Modify(1092, item => item.categories = Tools.SetCategories("SHADOW")); // 风车
                ItemDatabase.Modify(1120, item => item.categories = Tools.SetCategories("LAKE")); // 血石戒指
                ItemDatabase.Modify(1143, item =>
                {
                    Charm_Wings Effect = item.resourcePrefab.GetComponent<Charm_Wings>();
                    Effect.attackSpeedUnitByLevel = new float[] { 10, 8, 8, 6, 6, 4 };
                    Effect.stats = new StatusGroup[] { Effect.stats[0], Tools.SetStatusGroup("MOVE_SPEED", 3, 6, 9, 12, 15, 18) };
                }); // 翅膀
                ItemDatabase.Modify(1146, item => item.categories = new List<string>()); // 如意宝珠
                ItemDatabase.Modify(1162, item => item.categories = Tools.SetCategories("ELEMENTAL")); // 诱惑的鱼饵
                ItemDatabase.Modify(1186, item => item.categories = new List<string>()); // 纯白斗篷
                ItemDatabase.Modify(1196, item => item.categories = new List<string>()); // 生命之触
                ItemDatabase.Modify(1297, item => item.categories = Tools.SetCategories("EMBER")); // 石榴石徽章
                ItemDatabase.Modify(1298, item => item.categories = Tools.SetCategories("MAGITECH")); // 黄玉徽章
                ItemDatabase.Modify(1299, item => item.categories = Tools.SetCategories("GLACIER")); // 钻石徽章
                ItemDatabase.Modify(1305, item =>
                {
                    Charm_AddStatByAnotherStat Effect = item.resourcePrefab.GetComponent<Charm_AddStatByAnotherStat>();
                    Effect.maxLevel = 3;
                    Effect.perBaseStatByLevel = new int[] { 5, 4, 3, 2 };
                    Effect.stats = new StatusGroup[] { Tools.SetStatusGroup("DEBUFF_DAMAGE", 4, 8, 12, 16) };
                    item.isDual = true;
                    item.categories = Tools.SetCategories("CURSE", "WINDSONG");
                    item.cannotBeReward = false;
                    item.activeType = EItemActiveType.Default;
                }); // 红蘑菇
                //效果更改
                ItemDatabase.Modify(1016, item =>
                {
                    Charm_Reddew Effect = item.resourcePrefab.GetComponent<Charm_Reddew>();
                    Effect.damagePercentByLevel = new float[4] { 25f, 50f, 75f, 100f };
                }); // 红色露水
                ItemDatabase.Modify(1032, item =>
                {
                    Charm_FlameGround_Meteor Effect = item.resourcePrefab.GetComponent<Charm_FlameGround_Meteor>();
                    Effect.searchTimer.time = 0.1f;
                }); // 红蛇之眼
                ItemDatabase.Modify(1071, item =>
                {
                    Charm_MiniBallista Effect = item.resourcePrefab.GetComponent<Charm_MiniBallista>();
                    Effect.maxLevel = 2;
                    Effect.bulletCountByLevel = new int[] { 1, 3, 5 };
                    Effect.addCloudPercentByLevel = new int[] { 100 };
                }); // 迷你弩炮
                ItemDatabase.Modify(1093, item =>
                {
                    Charm_DashDamage Effect = item.resourcePrefab.GetComponent<Charm_DashDamage>();
                    Effect.maxLevel = 2;
                    Effect.physicalDashDamage = new int[3] { 50, 100, 150 };
                }); // 荆棘丛
                ItemDatabase.Modify(1087, item =>
                {
                    Charm_IncreaseGoldDropRate Effect = item.resourcePrefab.GetComponent<Charm_IncreaseGoldDropRate>();
                    Effect.maxLevel = 2;
                    Effect.goldDropBonusByLevel = new int[] { 20, 30, 40 };
                }); // 克里顿的印章
                ItemDatabase.Modify(1101, item =>
                {
                    Charm_FrostiumRing Effect = item.resourcePrefab.GetComponent<Charm_FrostiumRing>();
                    Effect.maxLevel = 1;
                    Effect.damageByLevel = new int[2] { 15, 30 };
                    Effect.cooldownTimer.time = 0.2f;
                }); // 欧希阿蕾胡阿
                ItemDatabase.Modify(1117, item =>
                {
                    Charm_TuningForks Effect = item.resourcePrefab.GetComponent<Charm_TuningForks>();
                    Effect.damagePercentByLevel = new int[] { 400, 500, 600, 700, 800 };
                }); // 音叉
                ItemDatabase.Modify(1137, item =>
                {
                    Charm_IceSpear Effect = item.resourcePrefab.GetComponent<Charm_IceSpear>();
                    Effect.maxLevel = 5;
                    Effect.fireCountByLevel = new int[] { 1 };
                    Effect.damagePercentByLevel = new float[] { 100, 140, 180, 220, 260, 300 };
                    Effect.defaultDamage = 40;
                }); // 沃尔斯帕
                ItemDatabase.Modify(1139, item =>
                {
                    Charm_CreateBulletOnSweep Effect = item.resourcePrefab.GetComponent<Charm_CreateBulletOnSweep>();
                    Effect.damagePercentByLevel = new float[] { 150, 300, 450 };
                }); // 冰雪藤
                ItemDatabase.Modify(1141, item =>
                {
                    Charm_FrostiumRing Effect = item.resourcePrefab.GetComponent<Charm_FrostiumRing>();
                    Effect.maxLevel = 1;
                    Effect.damageByLevel = new int[2] { 15, 30 };
                    Effect.cooldownTimer.time = 0.2f;
                }); // 被雷击中的树枝
                ItemDatabase.Modify(1152, item =>
                {
                    Charm_Kunai Effect = item.resourcePrefab.GetComponent<Charm_Kunai>();
                    Effect.throwChanceByLevel = new float[3] { 110, 220, 330 };
                }); // 苦无
                ItemDatabase.Modify(1153, item =>
                {
                    Charm_FlamePlantRoot Effect = item.resourcePrefab.GetComponent<Charm_FlamePlantRoot>();
                    Effect.fireDamageRatioByLevel = new int[] { 100, 200, 300 };
                }); // 火焰草根
                ItemDatabase.Modify(1158, item => 
                {
                    Charm_EnhancedPotionCork Effect = item.resourcePrefab.GetComponent<Charm_EnhancedPotionCork>();
                    Effect.hpRatio = 100;
                    Effect.potionBonus = 20;
                });// 强化药水盖
                ItemDatabase.Modify(1172, item =>
                {
                    Charm_PallasCard Effect = item.resourcePrefab.GetComponent<Charm_PallasCard>();
                    Effect.bulletDamage = 60;
                }); // 帕拉斯的卡牌
                ItemDatabase.Modify(1175, item =>
                {
                    Charm_FrostiumRing Effect = item.resourcePrefab.GetComponent<Charm_FrostiumRing>();
                    Effect.maxLevel = 1;
                    Effect.damageByLevel = new int[2] { 15, 30 };
                    Effect.cooldownTimer.time = 0.2f;
                }); // 霜冻矿戒指
                ItemDatabase.Modify(1178, item =>
                {
                    Charm_EchoOfTheGlacier Effect = item.resourcePrefab.GetComponent<Charm_EchoOfTheGlacier>();
                    Effect.frostbiteTimeByLevel = new float[] { 5f, 4f, 3f, 2f };
                    Effect.effectsString = new LocalizedString[] { Effect.effectsString[0], new LocalizedString("ModCharmEffect_EchoOfTheGlacier") };
                }); // 冰川之回声
                ItemDatabase.Modify(1196, item =>
                {
                    Charm_Exploitation Effect = item.resourcePrefab.GetComponent<Charm_Exploitation>();
                    Effect.maxLevel = 2;
                    Effect.healAmountByLevel = new float[] { 1, 2, 3 };
                    Effect.bonusHealAmountByLevel = new float[] { 1, 2, 3 };
                    Effect.stats = new StatusGroup[0] { };
                }); // 生命之触
                ItemDatabase.Modify(1243, item =>
                {
                    Charm_StatusInstance Effect = item.resourcePrefab.GetComponent<Charm_StatusInstance>();
                    Effect.effectsString = new LocalizedString[] { new LocalizedString("ModCharmEffect_IceBurn") };
                }); // 封印的特加斯
                ItemDatabase.Modify(1268, item =>
                {
                    Charm_FreezeNormalSlash Effect = item.resourcePrefab.GetComponent<Charm_FreezeNormalSlash>();
                    Effect.phSlashDamageMultiplierByLevel = new float[] { 60, 120, 180 };
                    Effect.slashDamageMultiplierByLevel = new float[] { 90, 180, 270 };
                }); // 永恒的冬日
                ItemDatabase.Modify(1277, item =>
                {
                    Charm_FireBulletInRange Effect = item.resourcePrefab.GetComponent<Charm_FireBulletInRange>();
                    Effect.coolDownTimer = new Timer(3f);
                    CharmPatch.Charm_FireBulletInRange_Patch.EnsureWindBullet(Effect);
                }); // 五色糖果玻璃罐
                ItemDatabase.Modify(1285, item =>
                {
                    Charm_EvasionRestore Effect = item.resourcePrefab.GetComponent<Charm_EvasionRestore>();
                    Effect.restoreDashCountByLevel = new int[] { 0, 1, 2, 3 };
                    Effect.maxLevel = 3;
                    Effect.stats = new StatusGroup[1] { Tools.SetStatusGroup("EVASION", 300, 600, 900, 1200) };
                }); // 弹力带
                ItemDatabase.Modify(1288, item =>
                {
                    ItemEntity Item = ItemDatabase.FindItemById(1051);
                    Charm_GuardCounter OrigEffect = Item.resourcePrefab.GetComponent<Charm_GuardCounter>();
                    Charm_GuardCounter Effect = item.resourcePrefab.GetComponent<Charm_GuardCounter>();
                    Effect.ripostelaserFxPrefab = OrigEffect.ripostelaserFxPrefab;
                }); // 还击法杖碎片
                ItemDatabase.Modify(1293, item =>
                {
                    Charm_IceBat Effect = item.resourcePrefab.GetComponent<Charm_IceBat>();
                    Effect.iceDamage = 100;
                    Effect.attackSpeedBonusByLevel = new int[] { 60, 120, 180, 240, 300 };
                }); // 雪山大耳蝙蝠
                //法术更改
                ItemDatabase.Modify(3000, item =>
                {
                    item.activeType = EItemActiveType.Default;
                    item.categories = Tools.SetCategories("HURRICANE");
                    item.rarity = EItemRarity.Common;
                    item.cost = CommonCost;
                    Charm_Magic Effect = item.resourcePrefab.GetComponent<Charm_Magic>();
                    Effect.maxLevel = 1;
                    Effect.skill.rarity = EItemRarity.Common;
                    Effect.skill.cooldownTime = 0.8f;
                    ActiveSkill_Bolt Magic = Effect.skill.magicPrefab.GetComponent<ActiveSkill_Bolt>();
                    Magic.fireCount = 2;
                    Magic.damageId = "Air Bolt";
                    Magic.relatedDamage = "WindDamage";
                    Magic.elementalType = Wind;
                    Magic.defaultDamageByLevel = new float[] { 8, 12 };
                    Magic.damagePercentByLevel = new float[] { 16, 24 };
                    BondingItems.Add(3000);
                }); // 空气魔弹
                ItemDatabase.Modify(3012, item =>
                {
                    Charm_Magic Effect = item.resourcePrefab.GetComponent<Charm_Magic>();
                    Effect.maxLevel = 4;
                    Effect.ContainedMagic.cooldownTime = 5f;
                    ActiveSkill_Arrow Magic = Effect.ContainedMagic.magicPrefab.GetComponent<ActiveSkill_Arrow>();
                    Magic.damagePercentByLevel = new int[] { 200, 400, 600, 800, 1000 };
                    Magic.defaultDamageByLevel = new float[] { 10, 20, 30, 40, 50 };
                }); // 火焰箭
                ItemDatabase.Modify(3013, item =>
                {
                    Charm_Magic Effect = item.resourcePrefab.GetComponent<Charm_Magic>();
                    Effect.maxLevel = 4;
                    Effect.ContainedMagic.ammo = 3;
                    ActiveSkill_Ball Magic = Effect.ContainedMagic.magicPrefab.GetComponent<ActiveSkill_Ball>();
                    Magic.defaultDamageByLevel = new float[] { 16, 20, 24, 28, 32 };
                    Magic.damagePercentByLevel = new float[] { 60, 75, 90, 105, 120 };
                }); // 火焰马戏团
                ItemDatabase.Modify(3020, item =>
                {
                    Charm_Magic Effect = item.resourcePrefab.GetComponent<Charm_Magic>();
                    ActiveSkill_FireBullet Magic = Effect.ContainedMagic.magicPrefab.GetComponent<ActiveSkill_FireBullet>();
                    Magic.bulletCount = 6;
                }); // 霜刀
                ItemDatabase.Modify(3022, item =>
                {
                    Charm_Magic Effect = item.resourcePrefab.GetComponent<Charm_Magic>();
                    Effect.maxLevel = 4;
                    Effect.ContainedMagic.cooldownTime = 25f;
                    ActiveSkill_MeteorShower Magic = Effect.ContainedMagic.magicPrefab.GetComponent<ActiveSkill_MeteorShower>();
                    Magic.meteorFireTimer = new Timer(0.2f);
                    Magic.numberOfMeteorsByLevel = new int[] { 24, 36, 48, 60, 72 };
                    Magic.defaultDamageByLevel = new float[] { 8, 12, 16, 20, 24 };
                    Magic.damagePercentByLevel = new float[] { 40, 60, 80, 100, 120 };
                }); // 陨石雨
                ItemDatabase.Modify(3023, item =>
                {
                    Charm_Magic Effect = item.resourcePrefab.GetComponent<Charm_Magic>();
                    Effect.maxLevel = 4;
                    Effect.ContainedMagic.cooldownTime = 12f;
                    ActiveSkill_ArrowRain Magic = Effect.ContainedMagic.magicPrefab.GetComponent<ActiveSkill_ArrowRain>();
                    Magic.defaultDamageByLevel = new float[] { 8, 10, 12, 14, 16 };
                    Magic.damagePercentByLevel = new float[] { 25, 50, 75, 100, 125 };
                    Magic.debuffPercent = 100;
                }); // 箭雨
                ItemDatabase.Modify(3028, item =>
                {
                    Charm_Magic Effect = item.resourcePrefab.GetComponent<Charm_Magic>();
                    Effect.maxLevel = 4;
                    Effect.ContainedMagic.cooldownTime = 24f;
                    ActiveSkill_LightningArmor Magic = Effect.ContainedMagic.magicPrefab.GetComponent<ActiveSkill_LightningArmor>();
                    Magic.damagesByLevel = new float[] { 2, 4, 6, 8, 10 };
                    Magic.damagePercentByLevel = new float[] { 20, 40, 60, 80, 100 };
                    Magic.durationTimer = new Timer(16f);
                }); // 雷霆之甲
                ItemDatabase.Modify(3036, item =>
                {
                    Charm_Magic Effect = item.resourcePrefab.GetComponent<Charm_Magic>();
                    ActiveSkill_Projectile Magic = Effect.ContainedMagic.magicPrefab.GetComponent<ActiveSkill_Projectile>();
                    Magic.defaultDamageByLevel = new float[] { 8, 12, 16, 20, 24 };
                    Magic.damagePercentByLevel = new float[] { 120, 180, 240, 300, 360 };
                }); // 雷之裁决
                //自动加载内容
                foreach (var entry in ModfyisItemUniqueEffect)
                {
                    ItemDatabase.Modify(entry.Key, item =>
                    {
                        if (item.resourcePrefab.TryGetComponent<Charm_Basic>(out var charm))
                            charm.isUniqueEffect = entry.Value;
                    });
                }

                foreach (var entry in ModfyisItemRarity)
                {
                    ItemDatabase.Modify(entry.Key, item =>
                    {
                        item.rarity = entry.Value;
                        switch (item.rarity)
                        {
                            case EItemRarity.Common:
                                item.cost = CommonCost;
                                break;
                            case EItemRarity.Uncommon:
                                item.cost = UncommonCost;
                                break;
                            case EItemRarity.Rare:
                                item.cost = RareCost;
                                break;
                            case EItemRarity.Legend:
                                item.cost = LegendCost;
                                break;
                        }
                    });
                }

                foreach (var entry in ModfyisItemStatusInstance)
                {
                    ItemDatabase.Modify(entry.Key, item =>
                    {
                        if (item.resourcePrefab.TryGetComponent<Charm_StatusInstance>(out var status))
                        {
                            int MaxLv = 0;
                            foreach (StatusGroup Group in entry.Value)
                            {
                                if (MaxLv < Group.valuesByLevel.Length) MaxLv = Group.valuesByLevel.Length - 1;
                            }
                            status.maxLevel = MaxLv;
                            status.stats = entry.Value.ToArray();
                        }
                    });
                }
            }
            if (ModConfig.Instance.AddItem)
            {
                ItemDatabase.Modify(2058, item => item.activeType = EItemActiveType.Default);
                ItemDatabase.Modify(2059, item => item.activeType = EItemActiveType.Default);
                ItemDatabase.Modify(2060, item => item.activeType = EItemActiveType.Default);
            }
        }

        public static void OnLoadMiracleDatabase()
        {
            if (ModConfig.Instance.AddMiracle)
            {
                foreach (ModMiracle Data in ModMiraclesData)
                {
                    MiracleDatabase.Register(Data.ThisMiracle);
                }
            }
            if (ModConfig.Instance.ModifyMiracle)
            {
                foreach (var entry in ModfyisMiracleStatus)
                {
                    MiracleDatabase.Modify(entry.Key, modifier =>
                    {
                        if (modifier is Miracle_StatusInstance MiracleStatus)
                            MiracleStatus.statInfo = entry.Value.ToArray();
                    });
                }
                MiracleDatabase.Modify("Electromancer", modifier =>
                {
                    if (modifier is Miracle_StatusInstance MiracleStatus)
                        MiracleStatus.miracleImage = Tools.LoadModSprite($"Miracle_Engineer");
                });
                MiracleDatabase.Modify("Elementalist", modifier =>
                {
                    if (modifier is Miracle_StatusInstance MiracleStatus)
                        MiracleStatus.categories = new string[1] { "ELEMENTAL" };
                });
                MiracleDatabase.Modify("Berserker", modifier =>
                {
                    if (modifier is Miracle_StatusInstance MiracleStatus)
                        MiracleStatus.categories = new string[1] { "MYSTIC" };
                });
                if (ModConfig.Instance.AddCategory)
                {
                    MiracleDatabase.Modify("IntelligenceAgent", modifier =>
                    {
                        if (modifier is Miracle_StatusInstance MiracleStatus)
                            MiracleStatus.categories = new string[1] { "COMET" };
                    });
                    MiracleDatabase.Modify("Gambler", modifier =>
                    {
                        if (modifier is Miracle_Gambler Miracle)
                        {
                            Miracle.dice = 7;
                            Miracle.categories = new string[1] { "FORTUNE" };
                            Miracle.giveItem = EItemGiveType.GiveItem;
                            Effect effect = Miracle.effects[0];
                            Miracle.effects = new Effect[1] { effect };
                        }
                    });
                    MiracleDatabase.Modify("Guard", modifier =>
                    {
                        if (modifier is Miracle_StatusInstance MiracleStatus)
                        {
                            MiracleStatus.categories = new string[1] { "PARTY" };
                            MiracleStatus.statInfo = Tools.SetStatInfo("ALL_PLAYER_DEFENSE/8=Positive", "ALL_PLAYER_ALL_DAMAGE/8=Positive").ToArray();
                        }
                    });
                }
            }
        }

        public static void OnLoadKeywordDatabase()
        {
            foreach(ModKeyWord Data in ModKeywordEntityData)
            {
                KeywordEntity Entity = Data.EndCreate();
                if (Data.UseKeyWordSpriteName != "")
                {
                    Entity.keywordImage = KeywordDatabase.GetEntity(Data.UseKeyWordSpriteName).keywordImage;
                }
                KeywordDatabase.Register(Entity);
            }
            Tools.InjectKeywordSpriteAssets();
        }

        private static readonly Dictionary<ulong, GameObject> Lv5 = new Dictionary<ulong, GameObject>();
        private static readonly Dictionary<ulong, GameObject> Lv10 = new Dictionary<ulong, GameObject>();
        private static readonly Dictionary<ulong, GameObject> Lv20 = new Dictionary<ulong, GameObject>();
        public static void OnLoadPassiveDatabase()
        {
            foreach(PassiveEntity passive in PassiveDatabase.GetAll())
            {
                if (passive.lv5PerkPrefab != null && !Lv5.ContainsKey(passive.id)) Lv5.Add(passive.id, passive.lv5PerkPrefab);
                if (passive.lv10PerkPrefab != null && !Lv10.ContainsKey(passive.id)) Lv10.Add(passive.id, passive.lv10PerkPrefab);
                if (passive.lv20PerkPrefab != null && !Lv20.ContainsKey(passive.id)) Lv20.Add(passive.id, passive.lv20PerkPrefab);
            }
            if (ModConfig.Instance.ModifyPassive)
            {
                PassiveDatabase.Modify(7, modifier =>
                {
                    if (modifier.lv20PerkPrefab.TryGetComponent<PassiveObjectMetadata>(out var Data))
                    {
                        Data.effectString = new LocalizedString("ModPassive_DefenseToAllDamage");
                    }
                    if (modifier.lv20PerkPrefab.TryGetComponent<PassiveObject_StatusInstance>(out var Status))
                    {
                        Status.stats = new string[] { "DEFENSE_TO_ALLDAMAGE/1" };
                    }
                });//耐心(防御)
                PassiveDatabase.Modify(8, modifier =>
                {
                    modifier.addStats = new string[] { "MAX_MP/2" };
                });//智慧(MP)
                PassiveDatabase.Modify(11, modifier =>
                {
                    modifier.addStats = new string[] { "EXP_DROP/1" };
                });//机智
            }
        }

        public static Dictionary<string, string> KeyWordToStatusName = new Dictionary<string, string>();
        public static void OnAllDatabasesReady()
        {
            foreach (StatusEntity statusEntity in Resources.LoadAll<StatusEntity>("Status"))
            {
                string KeyWord = statusEntity.statKeyword.ToUpper();
                if (!KeyWordToStatusName.ContainsKey(KeyWord))
                    KeyWordToStatusName.Add(KeyWord, statusEntity.id);
            }
            foreach (ModKeyWord Data in ModKeywordEntityData)
            {
                if (Data.Status == null) continue;
                string KeyWord = Data.Status.statKeyword.ToUpper();
                if (!KeyWordToStatusName.ContainsKey(KeyWord))
                    KeyWordToStatusName.Add(KeyWord, Data.Status.id);
            }
            ModFxPool.Register("CyanOrb", prewarm: 4, max: 16, fps: 30, loop: false, "CyanOrb_1", "CyanOrb_2", "CyanOrb_3", "CyanOrb_4", "CyanOrb_5", "CyanOrb_6", "CyanOrb_7", "CyanOrb_8");
            ModFxPool.Register("AirValveGround", prewarm: 2, max: 8, fps: 18, loop: false,
                "AirArrow_FX_Ground00", "AirArrow_FX_Ground01", "AirArrow_FX_Ground02", "AirArrow_FX_Ground03", "AirArrow_FX_Ground04", "AirArrow_FX_Ground05", "AirArrow_FX_Ground06");
            ModFxPool.Register("WindSail_Explosion", 4, 7, 10, false, "FX", 0, "WindSail_Explosion00", "WindSail_Explosion01", "WindSail_Explosion02", "WindSail_Explosion03", "WindSail_Explosion04", "WindSail_Explosion05");
        }

        public static void InitializeModStat(string Name)
        {
            BrownRabbit2Hit.SafeRemove(Name);
            Deer_Buff.Remove(Name);
            ColdAngerTime.SafeRemove(Name);
            HeatedBladeEffect.SafeRemove(Name);
            HeatedBladeStack.SafeRemove(Name);
        }

        public static int ModifiedCustomStat(UnitAvatar __instance, string id, int result)
        {
            string ValueId = id.ToString().ToUpperInvariant();
            int OrigValue = result;

            ModifiedCustomStatAdd(__instance, ValueId, ref result);
            ModifiedCustomStatMult(__instance, ValueId, ref result);
            ModifiedCustomStatMultAdd(__instance, ValueId, ref result);
            int customStatAmp = __instance.GetCustomStatAmp(id);
            if (customStatAmp != 0)
            {
                int AddValue = result - OrigValue;
                result += AddValue.IntMult(customStatAmp * 0.01f);
            }
            ModifiedCustomStatFinally(__instance, ValueId, ref result);
            return result;
        }

        public static void ModifiedCustomStatAdd(UnitAvatar Player, string ID, ref int Result)
        {

            if (ModConfig.BossRushMode)
            {
                if (ID.EqualsAny(TextFile.ExtraWeaponChoices))
                {
                    Result += 10;
                }
            }

            if (PartyStatsGetter.TryGetValue(ID, out Func<AllPlayerStatus, int> GetPartyStats))
            {
                Result += GetPartyStats(PartyStats);
            }

            if (ID.EqualsAny(TextFile.AllDamageBonus))
            {
                int CoinMult = Player.GetCustomStatUnsafe(TextFile.CoinToDamage);
                if (CoinMult > 0)
                {
                    int Coin = Player.GetCustomStatUnsafe(TextFile.GoldObtained);
                    Result += (int)MathF.Floor(Coin / CoinMult);
                }
                int DTD = Player.GetCustomStatUnsafe(TextFile.DefenseToAllDamage);
                if (DTD > 0)
                {
                    Result += (int)MathF.Floor(Player.GetCustomStatUnsafe(TextFile.DamageReduction) / 3);
                }
            }//所有伤害加成

            if (ID.EqualsAny(TextFile.MPSkillDamage))
            {
                int Value = Player.GetCustomStatUnsafe(TextFile.MPRegenToDamage);
                if (Value > 0)
                {
                    Result += (int)MathF.Floor(Player.GetCustomStatUnsafe(TextFile.MPRegen) / Value);
                }
            }//消耗MP伤害

            if (Player.IsWeaponUseNet(WeaponIDs.CarrotSword, WeaponIDs.CarrotSword2, WeaponIDs.Excallion))
            {
                if (ID.EqualsAny(TextFile.WindDamage))
                {
                    Result += 5;
                }
            }//胡萝卜剑系列 风属性

            if (Player.IsWeaponUseNet(WeaponIDs.Excallion))
            {
                if (ID.EqualsAny(TextFile.MagicDamageBonus))
                {
                    int max = Tools.BaseStatToMax(Player);
                    Result += (int)(max * 0.8f);
                }
            }//大葱剑 增加魔法伤害

            if (Player.IsWeaponUseNet(WeaponIDs.EternalFrostGreatsword))
            {
                if (ID.EqualsAny(TextFile.AdditionalElementalDamageBonus))
                    Result += Player.GetCustomStatUnsafe(TextFile.PhysicalDamage) * 2;
            }//万年冰 武器附加伤害增加

            if (Player.IsWeaponUseNet(WeaponIDs.GreatswordOfExorcism))
            {
                if (ID.EqualsAny(TextFile.FixedAttackSpeed))
                {
                    Result += Player.GetCustomStatUnsafe(TextFile.ChargingCharmBonus);
                }
            }//驱魔的重剑 冰霜武具冷却速度加攻击速度

            if (Player.IsWeaponUseNet(WeaponIDs.RedSnakeCrush))
            {
                if (ID.EqualsAny(TextFile.FixedAttackSpeed))
                {
                    Result += Player.GetCustomStatUnsafe(TextFile.BurnSpeed).IntMult(0.5f);
                }
            }//红蛇碎击

            if (Player.IsWeaponUseNet(WeaponIDs.LightningsWrath))
            {
                if (ID.EqualsAny(TextFile.ElectricDamage))
                {
                    if (WeaponPatch.LightningsWrathEffect2.ContainsKey(Player))
                        Result += 30;
                }
                if (ID.EqualsAny(TextFile.AttackSpeed))
                {
                    if (WeaponPatch.LightningsWrathEffect2.ContainsKey(Player))
                      Result += 125;
                }
            }//雷电之怒

            if (Player.IsWeaponUseNet(WeaponIDs.CrimsonVortex))
            {
                if (ID.EqualsAny(TextFile.CrimsonVortex))
                {
                    Result += (int)(Player.GetCustomStatUnsafe(TextFile.DebuffDamage) / 10f);
                    if (Result > 96) Result = 96;
                }
            }//深红漩涡 伤口恶化伤害 伤口恶化上限

            if (Player.IsWeaponUseNet(WeaponIDs.TheIndomitable))
            {
                if (ID.EqualsAny(TextFile.WeaponRange))
                {
                    Result += 25;
                }
            }//无垠原野 攻击范围增加

            if (Player.IsWeaponUseNet(WeaponIDs.Negligence))
            {
                if (ID.EqualsAny(TextFile.SpecialAttackChargeSpeed))
                {
                    Result += Player.GetCustomStatUnsafe(TextFile.AttackSpeed);
                }
            }//默杀 特殊攻击速度/特殊攻击蓄力速度

            if (Player.IsWeaponUseNet(WeaponIDs.Heidi))
            {
                if (ID.EqualsAny(TextFile.DashAttackDamageBonus))
                {
                    if (WeaponPatch.HeidiDashAttack.TryGetValue(Player, out int Stack))
                    {
                        Result += Stack * 10;
                    }
                }
            }//海蒂 最终攻击速度 冲刺攻击伤害

            if (Player.IsWeaponUseNet(WeaponIDs.CloudGatherer))
            {
                if (ID.EqualsAny(TextFile.LightningCrossbow))
                {
                    if (WeaponPatch.CloudGathererEffect.TryGetValue(Player, out bool Falg) && Falg)
                        Result = 100;
                }
            }//聚云者

            if (Player.IsWeaponUseNet(WeaponIDs.Minigun))
            {
                if (ID.EqualsAny(TextFile.LightningCrossbow))
                {
                    Result = 40 + Player.GetCustomStatUnsafe(TextFile.PhysicalDamage);
                }
            }//迷你机枪 闪电箭概率增加

            if (Player.IsWeaponUseNet(WeaponIDs.LightningGreatSwordE3G))
            {
                if (ID.EqualsAny(TextFile.SpecialAttackChargeSpeed))
                {
                    Result += Player.GetCustomStatUnsafe(TextFile.DarkCloudSpeed);
                    float Value = Player.GetCustomStatUnsafe(TextFile.DarkCloudAtkSpeedBonus) * 0.01f;
                    if (Value > 0)
                        Result += Player.GetCustomStatUnsafe(TextFile.AttackSpeed).IntMult(Value);
                }
            }//雷剑E3G

            if (Player.IsWeaponUseNet(WeaponIDs.BlizzardsEve))
            {
                if (ID.EqualsAny(TextFile.DashRecovery))
                {
                    Result += Player.GetCustomStatUnsafe(TextFile.ChargingCharmBonus) / 2;
                }
            }//暴风雪之锤 冰霜武具充能效率增加冲刺恢复速度

            if (Player.IsWeaponUseNet(WeaponIDs.LightningGreatSwordS3G))
            {
                if (ID.EqualsAny(TextFile.ElectricLuck))
                {
                    Result += 40;
                }
            }//雷剑S3G 额外触电概率增加

            if (Player.IsWeaponUseNet(WeaponIDs.SpikeClub))
            {
                if (ID.EqualsAny(TextFile.WeaponCritical))
                {
                    Result += 5000;
                }
            }//荆棘棍

            if (Player.IsWeaponUseNet(WeaponIDs.SwarmingShadow, WeaponIDs.SwiftRegret, WeaponIDs.Compliance))
            {
                if (ID.EqualsAny(TextFile.SpecialAttackChargeSpeed))
                {
                    Result += 66;
                }
                if (ID.EqualsAny(TextFile.LastBasicAttackDamage))
                {
                    Result += Player.GetCustomStatUnsafe(TextFile.SpecialAttackDamageBonus);
                }
            }//聚集的阴影

            if (Player.IsWeaponUseNet(WeaponIDs.Compliance))
            {
                if (ID.EqualsAny(TextFile.SpecialAttackDamageBonus))
                {
                    Result += 25;
                }
                if (ID.EqualsAny(TextFile.WeaponRange))
                {
                    Result += Player.GetCustomStatUnsafe(TextFile.SpecialAttackRange) / 2;
                }
            }//顺从的姿态

            if (Player.IsWeaponUseNet(WeaponIDs.Minimalism, WeaponIDs.TheSecond, WeaponIDs.BlackIronHalberd))
            {
                int Add = 40;
                if (Player.GetWeaponType(WeaponIDs.BlackIronHalberd))
                    Add += 40;
                if (ID.EqualsAny("ATTACKSPEED"))
                {
                    Result += Add;
                }
            }//极简主义 第二次 黑铁戟 攻击速度

            if (Player.IsWeaponUseNet(WeaponIDs.DarkVeil))
            {
                if (WeaponPatch.DarkVeilBuff.TryGetValue(Player, out CharacterBuff Buff) && !Buff.IsEndBuff)
                {
                    int Stack = Buff.CurrentStack;
                    if (ID.EqualsAny(TextFile.FollowerDamage))
                        Result -= Stack * 5;
                    if (ID.EqualsAny(TextFile.FollowerAttackSpeed))
                        Result -= Stack * 6;
                }
            }//黑幕 支配效果增强

            if (Player.IsWeaponUseNet(WeaponIDs.KalimsForm))
            {
                if (ID.EqualsAny(TextFile.JellyfishDoubleAttack))
                {
                    int Value = Player.GetCustomStatUnsafe(TextFile.DebuffDuration);
                    while (Value >= 100)
                    {
                        Result += 1;
                        Value -= 100;
                    }
                }
                if (ID.EqualsAny(TextFile.JellyfishBasicDamage))
                {
                    Result += Player.GetCustomStatUnsafe(TextFile.DebuffDamage) * 2;
                    Result += Player.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 2;
                }
            }//卡里姆的形象 减益伤害增加 基础水母伤害减少 受到减益伤害和毒减益伤害加成

            if (Player.IsWeaponUseNet(WeaponIDs.SolisEkli))
            {
                if (ID.EqualsAny(TextFile.WeaponRange))
                {
                    Result += Player.GetCustomStatUnsafe(TextFile.FlameSwordLuck).IntMult(0.33f);
                }
            }//叶克里 太阳剑增强影响攻击速度和范围

            if (Player.IsWeaponUseNet(WeaponIDs.Viduka))
            {
                if (ID.EqualsAny(TextFile.EnhancedChakram))
                {
                    Result += Player.GetCustomStatUnsafe(TextFile.AttackSpeed);
                }
            }//比杜卡 飞轮受攻击速度加成

            if (Player.IsWeaponUseNet(WeaponIDs.CreepingDespair))
            {
                if (ID.EqualsAny(TextFile.FuryInvincibilitTime))
                {
                    Result += 2;
                }
            }//爬行的绝望 狂怒无敌时间增加

            if (Player.IsWeaponUseNet(WeaponIDs.RuadhsleaSpear))
            {
                if (ID.EqualsAny(TextFile.DashAttackDamageBonus))
                {
                    if (WeaponPatch.RuadhsleaSpearBuff.ContainsKey(Player))
                    {
                        Result += WeaponPatch.RuadhsleaSpearBuff[Player] * 10;
                    }
                }
            }//突击枪普阿尔斯卡 层数增加冲刺攻击伤害

            if (Player.IsWeaponUseNet(WeaponIDs.ColdAnger))
            {
                if (ID.EqualsAny(TextFile.FollowerDamage, TextFile.FollowerAttackSpeed))
                {
                    Result += 30;
                }
            }//冰冷愤怒 增加同伴伤害和攻击速度

            if (Player.IsWeaponUseNet(WeaponIDs.ExpansionDevice))
            {
                if (ID.EqualsAny(TextFile.CrossbowAmmo))
                {
                    if (Result < 0)
                        Result += 3;
                }
            }//扩展装置 弹药数量

            if (Player.IsWeaponUseNet(WeaponIDs.YuisShortDagger))
            {
                if (ID.EqualsAny(TextFile.CooldownRecoverySpeed))
                {
                    if(!Player.IsInBattle)
                    {
                        Result += 100;
                    }
                }
                if (ID.EqualsAny(TextFile.MPSkillDamage))
                {
                    if (WeaponPatch.YuisShortDaggerMagic.TryGetValue(Player, out int Value))
                    {
                        Result += Value;
                    }
                }
            }//优衣的短匕首

            if (Player.IsWeaponUseNet(WeaponIDs.WeightOfTheSea))
            {
                if (ID.EqualsAny(TextFile.LastBasicAttackDamage))
                {
                    Result += Player.GetCustomStatUnsafe(TextFile.AttackSpeed) / 2;
                }
            }//海猫 攻击速度转最后一击伤害

            if (Player.IsWeaponUseNet(WeaponIDs.OraclesStaff))
            {
                if (ID.EqualsAny(TextFile.FrostRelicDamage))
                {
                    Result += Player.GetCustomStatUnsafe(TextFile.AttackSpeed).IntMult(0.25f);
                }
            }//巫女的预言

            if (Player.IsWeaponUseNet(WeaponIDs.GuardiansReturn))
            {
                if (ID.EqualsAny(TextFile.MeleeDamage))
                {
                    Result -= 25;
                }
            }//归来守护

            if (Player.IsWeaponUseNet(WeaponIDs.LightlessBlade))
            {
                if (ID.EqualsAny(TextFile.Thorns))
                {
                    Result += 200;
                }
            }//希望的影子

            if (StarTwinkle_AddDamage.Count > 0)
            {
                if (ID.EqualsAny("PLANETDAMAGE", "PLANETATTACKSPEED"))
                {
                    Result += 10 * StarTwinkle_AddDamage.Count;
                }
            }//星光闪烁 所有玩家的行星伤害和攻速增加10%

            if (Tools.CostumeSkinUse(Player.Name, "Skeleton_1", false))
            {
                if (ID.EqualsAny(TextFile.DamageReduction))
                {
                    float ExHP = Tools.GetMaxLife(Player);
                    Result += 8 + (int)((ExHP - 50) * 0.4f);
                }
                if (ID.EqualsAny(TextFile.MPSkillDamage))
                {
                    float ExMP = Player.MaxMp;
                    Result += (int)((ExMP - 50) * 0.1f);
                }
            }//骷髅1 防御力增加基于生命值

            if (Tools.CostumeSkinUse(Player.Name, "BrownRabbit_3", false))
            {
                if (ID.EqualsAny("PHYSICALDAMAGE"))
                {
                    float EvToPD = Player.GetCustomStat(ECustomStat.Evasion) * 0.01f;
                    Result += (int)(EvToPD * 0.4f);
                }
            }//棕兔3 物理伤害增加基于闪避率

            if (Tools.CostumeSkinUse(Player.Name, "FireCat_2", false))
            {
                if (ID.EqualsAny("FLAMESWORDADDITIONALATTACK"))
                {
                    Result += 1;
                }
            }//火猫2 太阳剑额外触发次数增加1

            if (Tools.CostumeSkinUse(Player.Name, "Luck_2", false))
            {
                if (ID.EqualsAny(TextFile.TrueDamage))
                {
                    int average = Tools.BaseStatToAverage(Player).IntMult(0.75f);
                    Result += average;
                }
            }//幸运2 真实伤害减少 真实伤害增加基于平均攻击力

            if (Tools.CostumeSkinUse(Player.Name, "Mole_2", false))
            {
                if (Player.followers.Count == 1)
                {
                    if (ID.EqualsAny(TextFile.FollowerAttackSpeed))
                    {
                        Result += 50;
                    }
                    if (ID.EqualsAny(TextFile.FollowerDefense))
                    {
                        Result += 100;
                    }
                }
            }//鼹鼠2 同伴防御+100（仅当同伴数小于等于1时）

            if (Tools.CostumeSkinUse(Player.Name, "Mole_3", false))
            {
                if (ID.EqualsAny(TextFile.FollowerDefense))
                {
                    Result += Player.GetCustomStatUnsafe(TextFile.DamageReduction).IntMult(0.6f);
                }
                if (ID.EqualsAny(TextFile.TrueDamage))
                {
                    Result += Player.followers.Count * 5;
                }
            }//鼹鼠3 根据同伴数量增加无视防御伤害 同伴防御增加基于防御力

            if (Tools.CostumeSkinUse(Player.Name, "HolyRabbit_3", false))
            {
                if (ID.EqualsAny(TextFile.AllDamageBonus))
                {
                    Result += PlayerSpawner.MultiplayerList.Count * 6;
                }
            }//圣兔3 每有1个玩家增加所有伤害

            if (Tools.CostumeSkinUse(Player.Name, "ArmoredRabbit_2", false))
            {
                if (ID.EqualsAny(TextFile.DashRecovery))
                {
                    Result += (int)MathF.Floor(Player.GetCustomStatUnsafe(TextFile.DamageReduction) / 5);
                }
                if (ID.EqualsAny(TextFile.DashAttackDamageBonus))
                {
                    Result += (int)MathF.Floor(Player.GetCustomStatUnsafe(TextFile.DamageReduction) / 5) * 6;
                }
            }//盔甲兔2

            if (Tools.CostumeSkinUse(Player.Name, "ArmoredRabbit_3", false))
            {
                if (ID.EqualsAny(TextFile.PhysicalDamage))
                {
                    Result += Player.GetCustomStatUnsafe(TextFile.DamageReduction).IntMult(0.3f);
                }
            }//盔甲兔3 防御力增加物理属性

            if (Tools.CostumeSkinUse(Player.Name, "Otter_3", false))
            {
                if (ID.EqualsAny("CRITICAL"))
                {
                    if (Otter3_AddCrit.ContainsKey(Player))
                    {
                        Result += 2000;
                    }
                }
            }//水獭3 有被动时增加20%暴击率

            if (Tools.CostumeSkinUse(Player.Name, "Bat_2", false))
            {
                if (ID.EqualsAny(TextFile.AllDamageBonus))
                {
                    float ExHP = Tools.GetMaxLife(Player);
                    Result += (int)(75 + ExHP * 0.5f);
                }
            }//蝙蝠2 增加所有伤害加成

            if (Tools.CostumeSkinUse(Player.Name, "Seren_2", false))
            {
                if (ID.EqualsAny(TextFile.MagicDamageBonus))
                {
                    Result += (int)Player.Shield * 2;
                }
            }//蜥蜴2 根据护盾量增加魔法书伤害

            if (Tools.CostumeSkinUse(Player.Name, "RedHolyRabbit_2", false))
            {
                if (ID.EqualsAny("MPREGENMULTIPLE") && Player.IsInBattle)
                {
                    Result -= 90;
                }
            }//红圣兔2 战斗中MP恢复减少

            if (Tools.CostumeSkinUse(Player.Name, "Luck_3", false))
            {
                if (ID.EqualsAny(TextFile.Luck))
                {
                    Result += Player.GetCustomStatUnsafe(TextFile.Critical) / 200;
                }
            }//幸运草3 幸运值随暴击率增加 
        }

        public static void ModifiedCustomStatMult(UnitAvatar Player, string ID, ref int Result)
        {

            if (Player.IsWeaponUseNet(WeaponIDs.GunpowderSynthesis))
            {
                if (ID.EqualsAny("WEAPONRANGE"))
                {
                    Result += Result.CustomStatMult(0.25f, 100);
                }
            }//火药合成 乘算增加攻击范围

            if (Player.IsWeaponUseNet(WeaponIDs.StarTwinkle))
            {
                if (ID.EqualsAny("PLANETDAMAGE", "PLANETATTACKSPEED"))
                {
                    Result += Result.CustomStatMult(0.1f, 100);
                }
                if (!StarTwinkle_AddDamage.Contains(Player.Name))
                    StarTwinkle_AddDamage.Add(Player.Name);
            }//星光闪烁 行星伤害和攻速乘算增加

            if (Player.IsWeaponUseNet(WeaponIDs.LightningGreatSwordE3G))
            {
                if (ID.EqualsAny(TextFile.SpecialAttackRange))
                {
                    float Mult = Math.Max(Player.GetCustomStatUnsafe(TextFile.DarkCloudLuck), 100) / 200f;
                    Result += Result.CustomStatMult(Mult, 100);
                }
            }//E3G 根据箭头概率特殊攻击范围乘算增加%
            
            if (Player.IsWeaponUseNet(WeaponIDs.FiaolasJudgmentStaff))
            {
                if (ID.EqualsAny(TextFile.BurnSpeed))
                {
                    if (WeaponPatch.FiaolasJudgmentStaffBuff.TryGetValue(Player, out Timer time) && time.Ratio < 1f)
                    {
                        Result += Result.CustomStatMult(0.5f, 100);
                    }
                }
            }//烧火棍

            if (Tools.CostumeSkinUse(Player.Name, "Frog_2", false))
            {
                if (ID.EqualsAny(TextFile.DarkCloudAtkSpeedBonus, TextFile.DarkCloudSpeed))
                {
                    Result += Result.CustomStatMult(0.25f, 100);
                }
            }//青蛙2 乌云消耗速度增加

            if (Tools.CostumeSkinUse(Player.Name, "FireCat_3", false))
            {
                if (ID.EqualsAny(TextFile.BurnDamage))
                {
                    Result += Result.CustomStatMult(0.15f, 100);
                }
            }//火猫3 灼烧伤害乘算增加

            if (Tools.CostumeSkinUse(Player.Name, "WizardRabbit_3", false))
            {
                if (ID.EqualsAny(TextFile.FreezeDamage))
                {
                    Result += Result.CustomStatMult(0.15f, 100);
                }
            }//冰兔3 冰冻伤害乘算增加

            if (Tools.CostumeSkinUse(Player.Name, "Luck_2", false))
            {
                if (ID.EqualsAny(TextFile.AllDamageBonus))
                {
                    Result -= Result.CustomStatMult(0.3f, 100);
                }
            }//幸运草2 减少所有伤害

            if (Tools.CostumeSkinUse(Player.Name, "Skeleton_3", false))
            {
                if (ID.EqualsAny(TextFile.MeleeDamage))
                {
                    Result -= Result.CustomStatMult(0.2f, 100);
                }
            }//骷髅3 近战伤害乘算减少

            if (Tools.CostumeSkinUse(Player.Name, "Seren_3", false))
            {
                if (ID.EqualsAny(TextFile.CrossbowReloadSpeed))
                {
                    Result -= Result.CustomStatMult(0.7f, 100);
                }
            }//蜥蜴3 填装速度乘算减少

            if (Tools.CostumeSkinUse(Player.Name, "MandarinRabbit_2", false))
            {
                if (ID.EqualsAny("DEBUFFDAMAGE"))
                {
                    Result += Result.CustomStatMult(0.25f, 100);
                }
            }//橙兔2 减益伤害乘算增加

            if (Tools.CostumeSkinUse(Player.Name, "RedHolyRabbit_3", false))
            {
                if (ID.EqualsAny(TextFile.BuffDuration))
                {
                    Result += Result.CustomStatMult(2f, 100);
                }
                if (ID.EqualsAny("FINALMP"))
                {
                    Result -= Result.CustomStatMult(0.4f, 100);
                }
            }//红圣兔3 持续时间大幅增加 最终MP乘算减少

            if (Tools.CostumeSkinUse(Player.Name, "DarkBrownRabbit_2", false))
            {
                if (ID.EqualsAny("BASICATTACKDAMAGEBONUS"))
                {
                    Result -= Result.CustomStatMult(0.1f, 100);
                }
                if (ID.EqualsAny("ATTACKSPEED"))
                {
                    Result += Result.CustomStatMult(0.25f, 100);
                }
            }//深棕兔2 普通攻击伤害乘算减少 攻击速度乘算增加

            if (Tools.CostumeSkinUse(Player.Name, "Otter_3", false))
            {
                if (ID.EqualsAny("ALLDAMAGEBONUS"))
                {
                    Result -= Result.CustomStatMult(0.15f, 100);
                }
                if (ID.EqualsAny("CRITICALDAMAGEBONUS"))
                {
                    Result += Result.CustomStatMult(0.3f, 50);
                }
            }//水獭3 所有伤害乘算减少 暴击伤害乘算增加

            if (Tools.CostumeSkinUse(Player.Name, "BrownRabbit_2", false))
            {
                if (ID.EqualsAny(TextFile.BuffDuration))
                {
                    Result -= Result.CustomStatMult(0.7f, 100);
                }
            }//棕兔2 乘算减少增益持续时间
        }

        public static void ModifiedCustomStatMultAdd(UnitAvatar Player, string ID, ref int Result)
        {
            if (Tools.CostumeSkinUse(Player.Name, "ArmoredRabbit_3", false))
            {
                if (ID.EqualsAny(TextFile.PhysicalDamage))
                {
                    Result = 20 + (Result - 20).IntMult(0.8f);
                }
            }//盔甲3 物理属性获取减少

            if (Tools.CostumeSkinUse(Player.Name, "Luck_3", false))
            {
                if (ID.EqualsAny(TextFile.Critical))
                {
                    Result = Result.IntMult(0.7f);
                }
                if (ID.EqualsAny(TextFile.CriticalDamageBonus))
                {
                    Result = Result.IntMult(0.7f);
                }
            }//幸运草3 暴击几率, 暴击伤害获取减少

            if (Tools.CostumeSkinUse(Player.Name, "MandarinRabbit_3", false))
            {
                if (Result > 0)
                {
                    Result = (int)MathF.Floor(Result * 1.1f);
                }
            }//橙兔3 全属性增加

            if (Tools.CostumeSkinUse(Player.Name, "Skeleton_3", false))
            {
                if (ID.EqualsAny(TextFile.AttackSpeed))
                {
                    Result = Result.IntMult(0.8f);
                }
            }//骷髅3 攻击速度获取减少
        }

        public static void ModifiedCustomStatFinally(UnitAvatar Player, string ID, ref int Result)
        {
            if (ModConfig.Instance.ModifyCharm)
            {
                if (ID.EqualsAny(TextFile.DashRecovery))
                {
                    if (Result > 100)
                    {
                        Result = 100 + 100 * (Result - 100) / Result;
                    }
                }

                if (ID.EqualsAny(TextFile.DashInvincibleTimeBonus))
                {
                    if (Result > 100) Result = 100;
                }

                if (ID.EqualsAny(TextFile.FollowerCriticalContribute))
                {
                    Result += 50;
                }

                if (ID.EqualsAny(TextFile.EchoOfTheGlacierUpgrade))
                {
                    Result = 1;
                }
            }

            if (ModConfig.BossRushMode)
            {
                if (ID.EqualsAny(TextFile.HealingPenalty))
                {
                    Result = Math.Max(50, Result);
                }//治疗量
                if (ID.EqualsAny(TextFile.BossRewardDice))
                {
                    Result -= 1;
                }//BOSS骰子掉落
            }

            if (DungeonPatchs.NumberofReincarnations > 0)
            {
                int Count = DungeonPatchs.NumberofReincarnations;
                if (ID.EqualsAny(TextFile.HealingPenalty))
                {
                    Result = 100 - (100 - Result).IntMult(Mathf.Pow(0.8f, Count));
                }
                if (ID.EqualsAny(TextFile.EXPDrop, TextFile.MoneyDrop))
                {
                    Result = Result.IntMult(Mathf.Pow(0.8f, Count));
                }
            }

            if (ModConfig.Instance.EnableWeaponModifier && ID.EqualsAny(TextFile.FixedAttackSpeed) && Result > 0)
            {
                Result += 100 + Player.GetCustomStatUnsafe(TextFile.AttackSpeed);
            }

            if (ID.EqualsAny(TextFile.RiPostElaser))
            {
                if (ModConfig.Instance.ModifyCharm)
                {
                    Result = 1;
                }
                else if (ModConfig.Instance.EnableWeaponModifier)
                {
                    Result = 0;
                }
            }//强化还击剑碎片

            if (Player.IsWeaponUseNet(WeaponIDs.CloudGatherer))
            {
                if (ID.EqualsAny("LIGHTNINGCROSSBOW_CLOUD"))
                {
                    Result = 0;
                }
            }//聚云者 不再启用原本的效果

            if (Player.IsWeaponUseNet(WeaponIDs.BeastMarksSwordHou))
            {
                if (ID.EqualsAny("EVASIONSHEATH"))
                {
                    Result = 0;
                }
            }//怪刀毫羽 不再启用原本的效果

            if (Player.IsWeaponUseNet(WeaponIDs.SilenceofEverfrost))
            {
                if (ID.EqualsAny("ECHOOFTHEGLACIERPARRY"))
                {
                    Result = 0;
                }
            }//冰刀 不再启用原本的效果

            if (Player.IsWeaponUseNet(WeaponIDs.RuadhsleaSpear))
            {
                if (ID.EqualsAny(TextFile.WeaponDamageBonusByDashCount))
                {
                    Result = 0;
                }
            }//红枪 不再启用原本的效果

            if (Player.IsWeaponUseNet(WeaponIDs.UnboundImmersion))
            {
                if (ID.EqualsAny(TextFile.EvasionFuryNext))
                {
                    Result = 0;
                }
            }//无尽沉浸 不再启用原本的效果

            if (Player.IsWeaponUseNet(WeaponIDs.BountyHunter))
            {
                if (ID.EqualsAny(TextFile.DashCountDamage))
                {
                    Result = 0;
                }
            }//赏金猎人 不再启用原本的效果

            if (Player.IsWeaponUseNet(WeaponIDs.GardenofNeedleIce))
            {
                if (ID.EqualsAny(TextFile.IcicleVineOrbit))
                {
                    Result = 0;
                }
            }//霜针花园 不再启用原本的效果

            if (Tools.CostumeSkinUse(Player.Name, "WhiteRabbit_3", false))
            {
                if (ID.EqualsAny(TextFile.BasicAttackDamageBonus, TextFile.SpecialAttackDamageBonus, TextFile.DashAttackDamageBonus))
                {
                    int WeaponDamage = Player.GetCustomStatUnsafe(TextFile.FinalWeaponDamage);
                    if (Result < WeaponDamage)
                        Result += Math.Min(30, WeaponDamage - Result);
                }
            }//白兔3 普攻加成、特攻加成、冲刺攻击不会低于武器伤害

            if (Tools.CostumeSkinUse(Player.Name, "DarkBrownRabbit_3", false))
            {
                if (ID.EqualsAny(TextFile.AttackSpeed))
                {
                    Result = (int)MathF.Min(Result, 50);
                }
            }//编发3 攻击速度上限50点
        }

        public static void ModifiedCostume(string PlayerName, ref string[] Stats, ref ItemEntity[] Item, ref bool UseEffect)
        {
            if (!ModConfig.Instance.EnableCostumeModifier) return;
            if (!CostumeSkinType.TryGetValue(PlayerName, out string skin)) return;
            if (!CostumeSkinIdToName.TryGetValue(skin, out string Name)) return;
            LoadModCostumeEffect(Name, ref Stats, ref Item, ref UseEffect);
        }

        public static void LoadModCostumeEffect(string SkinName, ref string[] Stats, ref ItemEntity[] Item, ref bool UseEffect)
        {
            switch (SkinName)
            {
                case "PinkRabbit_2":
                {
                    Stats = new string[] { "EXTRA_ITEM_CHOICES/1", "EXP_DROP/-30" };
                    break;
                }
                case "PinkRabbit_3":
                {
                    Stats = Array.Empty<string>();
                    break;
                }
                case "BrownRabbit_2":
                {
                    Stats = Array.Empty<string>();
                    break;
                }
                case "BrownRabbit_3":
                {
                    Stats = Array.Empty<string>();
                    Item = new ItemEntity[] { ItemDatabase.FindItemById(1160) };
                    break;
                }
                case "MandarinRabbit_2":
                {
                    Stats = new string[] { "CRITICAL/-2000" };
                    Item = new ItemEntity[] { ItemDatabase.FindItemById(1183) };
                    break;
                }
                case "MandarinRabbit_3":
                {
                    Stats = Array.Empty<string>();
                    break;
                }
                case "WhiteWolf_2":
                {
                    Stats = new string[] { "MOVE_SPEED/100", "DASH_RECOVERY_SPEED/-40" };
                    break;
                }
                case "WhiteWolf_3":
                {
                    Stats = new string[] { "FIXED_DASH/1", "DASH_RECOVERY_SPEED/200" };
                    break;
                }
                case "Skeleton_1":
                {
                    Stats = new string[] { "HP_CURSE/50" };
                    Item = Array.Empty<ItemEntity>();
                    UseEffect = false;
                    break;
                }
                case "Skeleton_3":
                {
                    Stats = new string[] { "DEBUFF_DAMAGE/-20" };
                    Item = Array.Empty<ItemEntity>();
                    UseEffect = false;
                    break;
                }
                case "FireCat_2":
                {
                    Stats = new string[] { "FLAME_SWORD_DAMAGE/-50", "FLAME_SWORD_FAST_FALL/25" };
                    Item = Array.Empty<ItemEntity>();
                    break;
                }
                case "FireCat_3":
                {
                    Stats = new string[] { "FINAL_WEAPONDAMAGE/15" };
                    Item = Array.Empty<ItemEntity>();
                    break;
                }
                case "Frog_2":
                {
                    Stats = Array.Empty<string>();
                    Item = new ItemEntity[] { ItemDatabase.FindItemById(1104) };
                    break;
                }
                case "Frog_3":
                {
                    Stats = new string[] { "ELECTRIC_DAMAGE/12", "ELECTRIC_QUICKNESS/5" };
                    Item = Array.Empty<ItemEntity>();
                    break;
                }
                case "WizardRabbit_2":
                {
                    Stats = new string[] { "CHARM_DAMAGE_BONUS/-35" };
                    Item = new ItemEntity[] { ItemDatabase.FindItemById(1179) };
                    break;
                }
                case "WizardRabbit_3":
                {
                    Stats = new string[] { "FINAL_WEAPONDAMAGE/15" };
                    Item = Array.Empty<ItemEntity>();
                    break;
                }
                case "Bat_2":
                {
                    Stats = new string[] { "HEALING_PENALTY/100" };
                    Item = new ItemEntity[] { ItemDatabase.FindItemById(1077) };
                    break;
                }
                case "Bat_3":
                {
                    Stats = new string[] { "HP_STEAL/5" };
                    break;
                }
                case "Bird_2":
                {
                    Stats = new string[] { "DASH_RECOVERY_SPEED/-20" };
                    if (ModConfig.Instance.AddItem && GetModItem.TryGetValue("WindSpriteHairWax", out ItemEntity ModItem))
                        Item = new ItemEntity[] { ModItem };
                    break;
                }
                case "Bird_3":
                {
                    Stats = Array.Empty<string>();
                    break;
                }
                case "Fox_2":
                {
                    Stats = new string[] { "INVENTORY_SLOT/-23" };
                    Item = new ItemEntity[] { ItemDatabase.FindItemById(2099) };
                    break;
                }
                case "Fox_3":
                {
                    Stats = new string[] { "TABLET_ENGRAVING/1" };
                    break;
                }
                case "Luck_2":
                {
                    Stats = new string[] { "LUCK/10", "FINAL_WEAPONDAMAGE/-30" };
                    break;
                }
                case "Luck_3":
                {
                    Stats = Array.Empty<string>();
                    if (ModConfig.Instance.AddItem && GetModItem.TryGetValue("LuckyCoin", out ItemEntity ModItem))
                        Item = new ItemEntity[] { ModItem };
                    break;
                }
                case "Mole_2":
                {
                    Stats = new string[] { "FINAL_WEAPONDAMAGE/-70" };
                    Item = Array.Empty<ItemEntity>();
                    break;
                }
                case "Mole_3":
                {
                    Stats = new string[] { "CHARM_DAMAGE_BONUS/-50" };
                    Item = Array.Empty<ItemEntity>();
                    break;
                }
                case "HolyRabbit_2":
                {
                    Stats = new string[] { "FINAL_DAMAGE/-20", "REVIVE_PLAYER_HASTE/40" };
                    Item = Array.Empty<ItemEntity>();
                    break;
                }
                case "HolyRabbit_3":
                {
                    Stats = new string[] { "HP_CURSE/50", "ALL_PLAYER_DEFENSE/20" };
                    Item = Array.Empty<ItemEntity>();
                    break;
                }
                case "Ghost_2":
                {
                    Stats = new string[] { "MAGIC_DAMAGE_BONUS/-50" };
                    Item = Array.Empty<ItemEntity>();
                    break;
                }
                case "Ghost_3":
                {
                    Stats = new string[] { "MAGIC_DAMAGE_BONUS/-50", "MAGIC_COOLDOWN_REDUCTION/40" };
                    Item = new ItemEntity[] { ItemDatabase.FindItemById(1040) };
                    break;
                }
                case "Seren_2":
                {
                    Stats = new string[] { "MAX_HP/-40", "FINAL_HP/-40", "MP_STEAL/5" };
                    Item = new ItemEntity[] { ItemDatabase.FindItemById(3020) };
                    break;
                }
                case "Seren_3":
                {
                    Stats = new string[] { "PLANET_ATTACK_SPEED/20", "ATTACK_SPEED/-30" };
                    Item = Array.Empty<ItemEntity>();
                    if (ModConfig.Instance.AddItem && GetModItem.TryGetValue("DeepSpaceObserver", out ItemEntity ModItem))
                        Item = new ItemEntity[] { ModItem };
                    break;
                }
                case "Crocodile_2":
                {
                    Stats = Array.Empty<string>();
                    if (ModConfig.Instance.AddItem && GetModItem.TryGetValue("AlchemistsFlask", out ItemEntity ModItem))
                        Item = new ItemEntity[] { ModItem };
                    break;
                }
                case "Crocodile_3":
                {
                    Stats = new string[] { "MELEE_DAMAGE/8", "BASIC_ATTACK_DAMAGE/20", "ATTACK_SPEED/20", "WEAPON_RANGE/20" };
                    break;
                }
                case "WhiteRabbit_2":
                {
                    Stats = new string[] { "HIGHEST_ELEMENTAL_DAMAGE/4" };
                    if (ModConfig.Instance.AddItem && GetModItem.TryGetValue("HeartofElements", out ItemEntity ModItem))
                        Item = new ItemEntity[] { ModItem };
                    break;
                }
                case "WhiteRabbit_3":
                {
                    Stats = new string[] { "FINAL_WEAPONDAMAGE/8" };
                    break;
                }
                case "RedHolyRabbit_2":
                {
                    Stats = new string[] { "HIGHEST_ELEMENTAL_DAMAGE/4", "MAX_MP/20", "FINAL_MP/20" };
                    break;
                }
                case "RedHolyRabbit_3":
                {
                    Stats = Array.Empty<string>();
                    break;
                }
                case "DarkBrownRabbit_2":
                {
                    Stats = Array.Empty<string>();
                    break;
                }
                case "DarkBrownRabbit_3":
                {
                    Stats = new string[] { "MELEE_DAMAGE/10", "LAST_BASIC_ATTACK_DAMAGE/20", "WEAPON_RANGE/30" };
                    break;
                }
                case "Deer_1":
                {
                    Stats = new string[] { "DIMENSION_POCKET/3", "CRITICAL/-10000" };
                    break;
                }
                case "Deer_2":
                {
                    Stats = new string[] { "DEFENSE/-40", "EVASION/-4000" };
                    break;
                }
                case "Deer_3":
                {
                    Stats = Array.Empty<string>();
                    break;
                }
                case "Squirrel_2":
                {
                    Stats = new string[] { "LUCK/5", "FRUIT_COUNT/-5" };
                    Item = new ItemEntity[] { ItemDatabase.FindItemById(1195) };
                    break;
                }
                case "Squirrel_3":
                {
                    Stats = Array.Empty<string>();
                    if (ModConfig.Instance.AddItem && GetModItem.TryGetValue("MysteryBox", out ItemEntity ModItem))
                        Item = new ItemEntity[] { ModItem };
                    break;
                }
                case "Otter_2":
                {
                    Stats = new string[] { "MP_STEAL/5", "MP_SKILL_DAMAGE/12", "FINAL_WEAPONDAMAGE/-18", "CRITICAL_DAMAGE_RATE/30" };
                    break;
                }
                case "Otter_3":
                {
                    Stats = Array.Empty<string>();
                    break;
                }
                case "ArmoredRabbit_2":
                {
                    Stats = Array.Empty<string>();
                    break;
                }
                case "ArmoredRabbit_3":
                {
                    Stats = Array.Empty<string>();
                    break;
                }
                case "Turtle_2":
                {
                    Stats = Array.Empty<string>();
                    break;
                }
                case "Turtle_3":
                {
                    Stats = Array.Empty<string>();
                    break;
                }
                case "ForestCat_2":
                {
                    Stats = new string[] { "LEAF_DROP/20", "FINAL_DAMAGE/-10" };
                    Item = new ItemEntity[] { ItemDatabase.FindItemById(1271) };
                    break;
                }
                case "ForestCat_3":
                {
                    Stats = new string[] { "NEGOTIATION/-20" };
                    if (ModConfig.Instance.AddItem && GetModItem.TryGetValue("MidasRing", out ItemEntity ModItem))
                        Item = new ItemEntity[] { ModItem };
                    break;
                }
            }
        }

    }
}