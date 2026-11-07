using HarmonyLib;
using Mirror;
using SPMod.Tool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static SPMod.StarSephiriaPatches;
using static SPMod.Tool.ModuleComponents;
using Random = System.Random;

namespace SPMod
{
    [HarmonyPatch]
    public static class DungeonPatchs
    {
        public static int NumberofReincarnations = 0;
        public static readonly FieldInfo eachStageHeadsField = AccessTools.Field(typeof(DungeonManager), "eachStageHeads");
        private static readonly FieldInfo? stageOrderField = AccessTools.Field(typeof(DungeonManager), "stageOrder");
        private static Coroutine? AllTeleport = null;
        private static readonly List<PlayerAvatar> TeleportPlayerList = [];

        public static float Seed = 0;
        public static Dictionary<string, List<string>> NoAddExp = [];

        public const string PartyKey = "__BossRushParty__";
        public static string CurrentBossFloor = "";
        public static string CurrentBossFloorGuid = "";
        private static bool GroupTeleportResolved = false;

        public static Dictionary<string, bool> DefeatMoleElite = [];
        public static bool UseSpawnerMoleElite = false;

        const string HomeFloor = "Grasstown_Chapter4_Dark";
        const string MultiZoneFloor = "MultiZone";

        const string FirstFloor = "Moleland_EntranceC6";
        const string MolelandRestFloor = "Moleland_Station";
        const string MolelandBOSS1Floor = "Moleland_Boss2";
        const string MolelandBOSS2Floor = "Moleland_Boss_Bomb";
        const string MolelandBOSS3Floor = "Moleland_Boss_RootDemon";
        private static readonly List<string> DefeatMolelandBossList =
        [
            MolelandBOSS1Floor, MolelandBOSS2Floor, MolelandBOSS3Floor
        ];
        public static Dictionary<string, List<string>> DefeatMolelandBossDictionary = [];

        const string GrasslandRestFloor = "Grassland_LibraryEntrance"; 
        const string GrasslandBoss1Floor = "GrasslandBoss"; 
        const string GrasslandBoss2Floor = "GrasslandBoss_Panther";
        const string GrasslandMiniBossToBossFloor = "Grasstown_Chapter6_ForReward_Day";
        const string GrasslandMiniBossToBossFloor2 = "Grasstown_Chapter6_ForReward";
        private static readonly List<string> DefeatGrasslandBossList =
        [
            GrasslandBoss1Floor, GrasslandBoss2Floor
        ];
        public static Dictionary<string, List<string>> DefeatGrasslandBossDictionary = [];

        const string LibraryRestFloor = "Library_Elevator_Chapter2";
        const string LibraryBoss1Floor = "LibraryBoss"; 
        const string LibraryBoss2Floor = "LibraryBoss_Factory";
        private static readonly List<string> DefeatLibraryBossList =
        [
            LibraryBoss1Floor, LibraryBoss2Floor
        ];
        public static Dictionary<string, List<string>> DefeatLibraryBossDictionary = [];

        const string DeepCaveRestFloor = "DeepCaveDark_Boss_Q_Chapter6";
        const string DeepCaveLizardFloor = "DeepCave_Boss_Lizard_Chapter6";
        const string DeepCaveBirdFloor = "DeepCave_Boss_Bird_Chapter6";
        private static readonly List<string> DefeatDeepCaveBossList =
        [
            DeepCaveBirdFloor, DeepCaveLizardFloor
        ];
        public static Dictionary<string, List<string>> DefeatDeepCaveBossDictionary = [];

        public static bool NoInitialization = false;
        public static bool LastFloor = false;
        public static float HardModAddLife = 0f;
        public static event Action? OnLoadNewNumberofReincarnations = null;

        public static void ResetSavedStageData()
        {
            SaveData Run = SaveManager.CurrentRun;
            if (Run == null) return;

            int OldStageCount = Run.GetInt("StagesCount", 0);
            for (int i = 0; i < OldStageCount; i++)
            {
                string StageName = Run.GetString($"StageName{i}", "");
                if (!string.IsNullOrEmpty(StageName))
                    Run.RemoveKey("StageHead_" + StageName);
                Run.RemoveKey($"StageName{i}");
            }

            int OldFloorCount = Run.GetInt("FloorCount", 0);
            for (int i = 0; i < OldFloorCount; i++)
                Run.RemoveKey($"Floor{i}");

            Run.SetInt("StagesCount", 0);
            Run.SetInt("FloorCount", 0);
            Run.RemoveKey("LastFloorGuid");
        }

        public static void ResetStageData(DungeonManager Dungeon)
        {
            if (Dungeon == null) return;

            ResetSavedStageData();
            eachStageHeadsField.SetValue(Dungeon, new Dictionary<string, FloorData>());
            stageOrderField?.SetValue(Dungeon, 0);
            Dungeon.generatedFloors.Clear();
            Dungeon.sortedStages.Clear();
            Dungeon.stageProgress.Clear();
        }

        public static void InitializationModGameData(string UseName)
        {
            if (NoInitialization)
            {
                StarSephiriaMod.LogInfoDungeon($"—已阻止{UseName}加载数据—");
                return;
            }

            StarSephiriaMod.LogInfoDungeon($"—{UseName}加载游戏数据—");
            if (UseName == "LoadDungeon")
            {
                if (SaveManager.CurrentRun.GetInt("FloorCount", 0) > 0) LoadModData();
                return;
            }
            if (UseName != "New")
            {
                HardModAddLife = 0;
                NumberofReincarnations = 0;
                DefeatBoss.Clear();
                foreach (PlayerSpawner PlayerSp in PlayerSpawner.MultiplayerList)
                {
                    PlayerAvatar Player = PlayerSp.PlayerAvatar;
                    DungeonManager.Instance.StartCoroutine(DeleteUnintentionalItems(Player));
                }
            }
            else
            {
                HardModAddLife = 0f;
                DefeatBoss.Clear();
                OnLoadNewNumberofReincarnations?.Invoke();
            }
            StarSephiriaMod.LogInfoDungeon($"—初始化模式数据—");
            if (SaveManager.Current.GetInt("HardModeShard_[Star's Sephiria Mod]BossRushMode_Level", 0) == 1)
            {
                Seed = (float)new Random().NextDouble();
                DefeatMoleElite = [];
                DefeatMolelandBossDictionary = [];
                DefeatGrasslandBossDictionary = [];
                DefeatLibraryBossDictionary = [];
                DefeatDeepCaveBossDictionary = [];
                NoAddExp = [];

                DefeatMoleElite[PartyKey] = false;
                DefeatMolelandBossDictionary[PartyKey] = DefeatMolelandBossList.ToList();
                DefeatGrasslandBossDictionary[PartyKey] = DefeatGrasslandBossList.ToList();
                DefeatLibraryBossDictionary[PartyKey] = DefeatLibraryBossList.ToList();
                DefeatDeepCaveBossDictionary[PartyKey] = DefeatDeepCaveBossList.ToList();

                foreach (PlayerSpawner PlayerSp in PlayerSpawner.MultiplayerList)
                {
                    string Player = PlayerSp.PlayerAvatar.Name;
                    NoAddExp[Player] = [];
                    if (UseName == "LocalMoveFloor")
                    {
                        PlayerSp.PlayerAvatar.AddOrphanedStatusInstance(StatusDatabase.CreateStatusEntity("INVENTORY_SLOT/10"));
                    }
                    StarSephiriaMod.LogInfoDungeon($"初始化{Player}的数据");
                }
                UseSpawnerMoleElite = false;
                LastFloor = false;
                CurrentBossFloor = "";
                CurrentBossFloorGuid = "";
                GroupTeleportResolved = false;
                AllTeleport = null;
                TeleportPlayerList.Clear();
            }
            if (PlayerSpawner.MultiplayerList.Count <= 1)
            {
                SaveManager.CurrentRun.SetFloat("[Star's Sephiria Mod]BossRush_Seed", Seed);
                foreach (string Player in StarSephiriaMod.CostumeType.Keys)
                {
                    SaveManager.CurrentRun.SetString("[Star's Sephiria Mod]BossRush_Player", Player);
                    break;
                }
                SaveManager.CurrentRun.SetBool("[Star's Sephiria Mod]BossRush_MoleList_MoleElite", false);
                SaveManager.CurrentRun.SetBool("[Star's Sephiria Mod]BossRush_DefeatMoleElite", false);
                foreach (string Mole in DefeatMolelandBossList)
                {
                    SaveManager.CurrentRun.SetBool("[Star's Sephiria Mod]BossRush_DefeatMole_" + Mole, false);
                }
                foreach (string Grass in DefeatGrasslandBossList)
                {
                    SaveManager.CurrentRun.SetBool("[Star's Sephiria Mod]BossRush_DefeatGrass_" + Grass, false);
                }
                foreach (string Library in DefeatLibraryBossList)
                {
                    SaveManager.CurrentRun.SetBool("[Star's Sephiria Mod]BossRush_DefeatLibrary_" + Library, false);
                }
                foreach (string DeepCave in DefeatDeepCaveBossList)
                {
                    SaveManager.CurrentRun.SetBool("[Star's Sephiria Mod]BossRush_DefeatDeepCave_" + DeepCave, false);
                }
                SaveManager.CurrentRun.SetBool("[Star's Sephiria Mod]BossRush_LastFloor", false);
                SaveManager.CurrentRun.SetFloat("[Star's Sephiria Mod]HardMod_AddLife", 0f);
                SaveManager.CurrentRun.SetInt("[Star's Sephiria Mod]InfiniteMode_NumberofReincarnations", NumberofReincarnations);
            }
        }
        public static void LoadModData()
        {
            try
            {

                StarSephiriaMod.LogInfoDungeon($"———————SL进入游戏，开始读取存档数据———————");

                HardModAddLife = SaveManager.CurrentRun.GetFloat("[Star's Sephiria Mod]HardMod_AddLife", 0f);
                StarSephiriaMod.LogInfoDungeon($"苦难模式怪物血量加成：{HardModAddLife}");

                NumberofReincarnations = SaveManager.CurrentRun.GetInt("[Star's Sephiria Mod]InfiniteMode_NumberofReincarnations", 0);
                StarSephiriaMod.LogInfoDungeon($"轮回次数：{NumberofReincarnations}");

                if (SaveManager.Current.GetInt("HardModeShard_[Star's Sephiria Mod]BossRushMode_Level", 0) == 1)
                {
                    Seed = SaveManager.CurrentRun.GetFloat("[Star's Sephiria Mod]BossRush_Seed", (float)new Random().NextDouble());
                    StarSephiriaMod.LogInfoDungeon($"BossRush种子：{Seed}");

                    DefeatMoleElite = [];
                    DefeatMolelandBossDictionary = [];
                    DefeatGrasslandBossDictionary = [];
                    DefeatLibraryBossDictionary = [];
                    DefeatDeepCaveBossDictionary = [];
                    NoAddExp = [];
                    CurrentBossFloor = "";
                    CurrentBossFloorGuid = "";
                    GroupTeleportResolved = false;
                    AllTeleport = null;
                    TeleportPlayerList.Clear();

                    // 旧存档的 BossRush_Player 只用于单人存档；进度现在统一存在 PartyKey 下
                    string Player = PartyKey;
                    UseSpawnerMoleElite = SaveManager.CurrentRun.GetBool("[Star's Sephiria Mod]BossRush_MoleList_MoleElite", false);
                    StarSephiriaMod.LogInfoDungeon($"是否已生成精英鼹鼠：{UseSpawnerMoleElite}");

                    bool falg = SaveManager.CurrentRun.GetBool("[Star's Sephiria Mod]BossRush_DefeatMoleElite", false);
                    DefeatMoleElite.Add(Player, falg);
                    StarSephiriaMod.LogInfoDungeon($"是否击败精英鼹鼠：{falg}");

                    bool Last = SaveManager.CurrentRun.GetBool("[Star's Sephiria Mod]BossRush_LastFloor", false);
                    LastFloor = Last;
                    StarSephiriaMod.LogInfoDungeon($"是否最后一层：{Last}");

                    List<string> BossList = [];
                    foreach (string Mole in DefeatMolelandBossList)
                    {
                        if (!SaveManager.CurrentRun.GetBool("[Star's Sephiria Mod]BossRush_DefeatMole_" + Mole, false))
                            BossList.Add(Mole);
                    }
                    if (BossList.Count > 0)
                    {
                        DefeatMolelandBossDictionary.Add(Player, BossList);
                        StarSephiriaMod.LogInfoDungeon($"鼹鼠层剩余Boss列表：{string.Join(",", BossList)}");
                    }
                    else
                    {
                        DefeatMolelandBossDictionary.Add(Player, []);
                        StarSephiriaMod.LogInfoDungeon($"鼹鼠层剩余Boss列表：无");
                    }

                    BossList = [];
                    foreach (string Grass in DefeatGrasslandBossList)
                    {
                        if (!SaveManager.CurrentRun.GetBool("[Star's Sephiria Mod]BossRush_DefeatGrass_" + Grass, false))
                            BossList.Add(Grass);
                    }
                    if (BossList.Count > 0)
                    {
                        DefeatGrasslandBossDictionary.Add(Player, BossList);
                        StarSephiriaMod.LogInfoDungeon($"草原层剩余Boss列表：{string.Join(",", BossList)}");
                    }
                    else
                    {
                        DefeatGrasslandBossDictionary.Add(Player, []);
                        StarSephiriaMod.LogInfoDungeon($"草原层剩余Boss列表：无");
                    }

                    BossList = [];
                    foreach (string Library in DefeatLibraryBossList)
                    {
                        if (!SaveManager.CurrentRun.GetBool("[Star's Sephiria Mod]BossRush_DefeatLibrary_" + Library, false))
                            BossList.Add(Library);
                    }
                    if (BossList.Count > 0)
                    {
                        DefeatLibraryBossDictionary.Add(Player, BossList);
                        StarSephiriaMod.LogInfoDungeon($"图书馆层剩余Boss列表：{string.Join(",", BossList)}");
                    }
                    else
                    {
                        DefeatLibraryBossDictionary.Add(Player, []);
                        StarSephiriaMod.LogInfoDungeon($"图书馆层剩余Boss列表：无");
                    }

                    BossList = [];
                    foreach (string DeepCave in DefeatDeepCaveBossList)
                    {
                        if (!SaveManager.CurrentRun.GetBool("[Star's Sephiria Mod]BossRush_DefeatDeepCave_" + DeepCave, false))
                            BossList.Add(DeepCave);
                    }
                    if (BossList.Count > 0)
                    {
                        DefeatDeepCaveBossDictionary.Add(Player, BossList);
                        StarSephiriaMod.LogInfoDungeon($"洞穴层剩余Boss列表：{string.Join(",", BossList)}");
                    }
                    else
                    {
                        DefeatDeepCaveBossDictionary.Add(Player, []);
                        StarSephiriaMod.LogInfoDungeon($"洞穴馆层剩余Boss列表：无");
                    }
                }
                StarSephiriaMod.LogInfoDungeon($"————————————————————————————");
            }
            catch (Exception ex)
            {
                StarSephiriaMod.LogInfoDungeon($"加载BossRush存档失败: {ex.Message}");
            }
        }
        public static void PlayerAddInBossRushMod()
        {
            StarSephiriaMod.LogInfoDungeon($"检测到有玩家加入");
            EnsurePartyState();
        }
        public static void PlayerReduceInBossRushMod()
        {
            StarSephiriaMod.LogInfoDungeon($"检测到有玩家退出");
            foreach (string Player in NoAddExp.Keys.ToList())
            {
                if (!StarSephiriaMod.CostumeType.ContainsKey(Player))
                {
                    NoAddExp.Remove(Player);
                    StarSephiriaMod.LogInfoDungeon($"删除{Player}的经验记录");
                }
            }
        }
        private static List<string> PartyList(Dictionary<string, List<string>> Dict)
        {
            if (!Dict.TryGetValue(PartyKey, out List<string>? List) || List == null)
            {
                List = [];
                Dict[PartyKey] = List;
            }
            return List;
        }

        private static void EnsurePartyState()
        {
            if (!DefeatMolelandBossDictionary.ContainsKey(PartyKey))
                DefeatMolelandBossDictionary[PartyKey] = DefeatMolelandBossList.ToList();
            if (!DefeatGrasslandBossDictionary.ContainsKey(PartyKey))
                DefeatGrasslandBossDictionary[PartyKey] = DefeatGrasslandBossList.ToList();
            if (!DefeatLibraryBossDictionary.ContainsKey(PartyKey))
                DefeatLibraryBossDictionary[PartyKey] = DefeatLibraryBossList.ToList();
            if (!DefeatDeepCaveBossDictionary.ContainsKey(PartyKey))
                DefeatDeepCaveBossDictionary[PartyKey] = DefeatDeepCaveBossList.ToList();
            if (!DefeatMoleElite.ContainsKey(PartyKey))
                DefeatMoleElite[PartyKey] = false;
        }

        private static string? PickBoss(Dictionary<string, List<string>> Dict, bool random)
        {
            List<string> List = PartyList(Dict);
            if (List.Count == 0) return null;

            int idx = 0;
            if (random)
            {
                idx = Mathf.FloorToInt(Seed * List.Count);
                if (idx >= List.Count) idx = List.Count - 1;
                if (idx < 0) idx = 0;
            }
            return List[idx];
        }

        private static bool IsBossFloor(string FloorName)
        {
            return DefeatMolelandBossList.Contains(FloorName)
                || DefeatGrasslandBossList.Contains(FloorName)
                || DefeatLibraryBossList.Contains(FloorName)
                || DefeatDeepCaveBossList.Contains(FloorName);
        }

        private static void UpdateCurrentBossFloor(string FloorName, string FloorGuid)
        {
            if (IsBossFloor(FloorName))
            {
                CurrentBossFloor = FloorName;
                CurrentBossFloorGuid = FloorGuid;
            }
            else
            {
                CurrentBossFloor = "";
                CurrentBossFloorGuid = "";
            }
        }

        /// <summary>BossSpawner 结算完成时调用：以 BossSpawner 所在楼层为准，避免分体 BOSS（如 MadArmadillo）判定不到。</summary>
        public static void NotifySpawnerBossDefeated(BossSpawner Spawner)
        {
            if (Spawner == null || Spawner.parent == null) return;
            FloorData? Data = Spawner.parent.DataOnServer;
            if (Data == null || !IsBossFloor(Data.name)) return;

            CurrentBossFloor = Data.name;
            CurrentBossFloorGuid = Data.guid;

            // 精英鼹鼠和 MolelandBOSS2Floor 是同一个房间：
            // 击败精英只记录精英，保留该楼层的BOSS进度，之后还要回来打真正的BOSS
            if (Data.name == MolelandBOSS2Floor && Spawner.bossObject is Unit_MoleElite)
            {
                MarkMoleEliteDefeated();
                return;
            }

            MarkCurrentBossFloorCleared();
        }

        private static void MarkMoleEliteDefeated()
        {
            CurrentBossFloor = "";
            CurrentBossFloorGuid = "";
            bool Changed = !UseSpawnerMoleElite;
            UseSpawnerMoleElite = true;
            if (!DefeatMoleElite.TryGetValue(PartyKey, out bool Elite) || !Elite)
            {
                DefeatMoleElite[PartyKey] = true;
                Changed = true;
            }
            if (!Changed) return;

            StarSephiriaMod.LogInfoDungeon($"BossRush：已击败精英鼹鼠，保留 {MolelandBOSS2Floor} 的BOSS进度");
            SaveBossRushState();
        }

        private static void MarkCurrentBossFloorCleared()
        {
            string Floor = CurrentBossFloor;
            CurrentBossFloor = "";
            CurrentBossFloorGuid = "";
            if (string.IsNullOrEmpty(Floor)) return;

            bool Changed = false;
            if (DefeatMolelandBossDictionary.TryGetValue(PartyKey, out List<string>? Mole) && Mole != null && Mole.Remove(Floor)) Changed = true;
            if (DefeatGrasslandBossDictionary.TryGetValue(PartyKey, out List<string>? Grass) && Grass != null && Grass.Remove(Floor)) Changed = true;
            if (DefeatLibraryBossDictionary.TryGetValue(PartyKey, out List<string>? Library) && Library != null && Library.Remove(Floor)) Changed = true;
            if (DefeatDeepCaveBossDictionary.TryGetValue(PartyKey, out List<string>? DeepCave) && DeepCave != null && DeepCave.Remove(Floor)) Changed = true;

            if (Floor == MolelandBOSS2Floor)
            {
                if (!DefeatMoleElite.ContainsKey(PartyKey))
                    DefeatMoleElite[PartyKey] = false;
                DefeatMoleElite[PartyKey] = true;
                Changed = true;
            }

            if (Changed)
            {
                StarSephiriaMod.LogInfoDungeon($"BossRush：已击败 {Floor}");
                SaveBossRushState();
            }
        }

        private static void GrantFloorEntryReward(PlayerAvatar avatar, string FloorName)
        {
            if (avatar == null || string.IsNullOrEmpty(FloorName)) return;

            if (!NoAddExp.TryGetValue(avatar.Name, out List<string> Floor))
            {
                Floor = [];
                NoAddExp[avatar.Name] = Floor;
            }
            if (Floor.Contains(FloorName)) return;

            avatar.AddMoney(100.IntMult(1f + avatar.GetCustomStatUnsafe(TextFile.MoneyDrop) * 0.01f));
            LevelController Level = avatar.GetComponent<LevelController>();
            int Add = StarSephiriaMod.CustomExpTable[Level.currentLevel - 1];
            if (Level.currentLevel - 2 > 0)
            {
                Add -= StarSephiriaMod.CustomExpTable[Level.currentLevel - 2];
            }
            float Value = HardModAddLife * (1f + HardModAddLife * 2);
            if (Value > 0f)
            {
                float Mult = 1f + avatar.GetCustomStatUnsafe(TextFile.EXPDrop) * 0.01f;
                Level?.AddExp(Add.IntMult(Value * 0.15f * Mult));
            }
            Floor.Add(FloorName);
        }

        private static void SaveBossRushState()
        {
            if (!NetworkServer.active || NetworkServer.connections.Count > 1) return;
            SaveData Run = SaveManager.CurrentRun;
            if (Run == null) return;

            Run.SetBool("[Star's Sephiria Mod]BossRush_MoleList_MoleElite", UseSpawnerMoleElite);
            Run.SetBool("[Star's Sephiria Mod]BossRush_DefeatMoleElite", DefeatMoleElite.TryGetValue(PartyKey, out bool Elite) && Elite);
            foreach (string Mole in DefeatMolelandBossList)
                Run.SetBool("[Star's Sephiria Mod]BossRush_DefeatMole_" + Mole, !PartyList(DefeatMolelandBossDictionary).Contains(Mole));
            foreach (string Grass in DefeatGrasslandBossList)
                Run.SetBool("[Star's Sephiria Mod]BossRush_DefeatGrass_" + Grass, !PartyList(DefeatGrasslandBossDictionary).Contains(Grass));
            foreach (string Library in DefeatLibraryBossList)
                Run.SetBool("[Star's Sephiria Mod]BossRush_DefeatLibrary_" + Library, !PartyList(DefeatLibraryBossDictionary).Contains(Library));
            foreach (string DeepCave in DefeatDeepCaveBossList)
                Run.SetBool("[Star's Sephiria Mod]BossRush_DefeatDeepCave_" + DeepCave, !PartyList(DefeatDeepCaveBossDictionary).Contains(DeepCave));
            Run.SetBool("[Star's Sephiria Mod]BossRush_LastFloor", LastFloor);
        }

        public static Dictionary<string, GameObject> AllBossList = [];

        [HarmonyPatch(typeof(DungeonManager))]
        public static class DungeonManager_Patch
        {
            private static float PartyStatsTimer = 0.2f;

            [HarmonyPatch("Awake")]
            [HarmonyPostfix]
            static void Awake_Postfix(DungeonManager __instance)
            {
                GameObject[] AllNPCPrefabs = Resources.FindObjectsOfTypeAll<GameObject>()
                    .Where(go => go.scene.name == null)
                    .Where(go => go.GetComponent<UnitAvatar>() != null)
                    .ToArray();

                int Stack = 0;
                foreach (GameObject prefab in AllNPCPrefabs)
                {
                    UnitAvatar BOSS = prefab.GetComponent<UnitAvatar>();
                    if (BOSS.monsterType != EMonsterType.Boss) continue;
                    StarSephiriaMod.LogInfoDungeon($" ——实体计数：{Stack} | {BOSS.gameObject.name}");
                    if(!AllBossList.ContainsKey(BOSS.gameObject.name))
                        AllBossList.Add(BOSS.gameObject.name, BOSS.gameObject);
                    Stack += 1;
                }
            }

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            static void Update_Postfix(DungeonManager __instance)
            {
                PartyStatsTimer += Time.deltaTime;
                if (PartyStatsTimer < 0.2f) return;
                PartyStatsTimer = 0f;

                AllPlayerStatus NewValue = Tools.GetMaxPlayerStatus();
                if (StarSephiriaMod.PartyStats.SameAs(NewValue)) return;

                StarSephiriaMod.PartyStats = NewValue;
                if (!__instance.isServer) return;
                foreach (PlayerSpawner Spawner in PlayerSpawner.MultiplayerList.ToList())
                {
                    if (Spawner.PlayerAvatar == null) continue;
                    UnitAvatar_Patches.RefreshElementalBonus(Spawner.PlayerAvatar);
                }
            }

            [HarmonyPatch("LoadStageAndMove")]
            [HarmonyPrefix]
            static void LoadStageAndMove_Prefix(DungeonManager __instance, string stageName)
            {
                StarSephiriaMod.LogInfoDungeon($"传送名：{stageName}");
            }

            [HarmonyPatch("LoadDungeon")]
            [HarmonyPrefix]
            static void LoadDungeon_Prefix(DungeonManager __instance)
            {
                if (!__instance.isServer) return;

                InitializationModGameData("LoadDungeon");
                NoInitialization = true;
            }

            [HarmonyPatch("LocalMoveFloor")]
            [HarmonyPrefix]
            static bool LocalMoveFloor_Prefix(DungeonManager __instance, PlayerAvatar avatar, ref string floorGuid, ref string spawnPoint)
            {
                if (!CheckAllPlayerReady(__instance, avatar, floorGuid, spawnPoint)) return false;
                if (!__instance.isServer || avatar == null) return true;

                if (NetworkServer.connections.Count <= 1)
                    SaveManager.CurrentRun.SetFloat("[Star's Sephiria Mod]HardMod_AddLife", HardModAddLife);

                string FloorName = "";
                foreach (var kvp in __instance.generatedFloors.ToList())
                {
                    if (kvp.Key == floorGuid) FloorName = kvp.Value.name;
                }
                StarSephiriaMod.LogInfoDungeon($"玩家 {avatar.Name} 目标楼层：{FloorName} UID：{floorGuid} 生成点：{spawnPoint}");
                if (FloorName == FirstFloor && NumberofReincarnations <= 0 && avatar == PlayerSpawner.MultiplayerList[0].PlayerAvatar) InitializationModGameData("LocalMoveFloor");
                NoInitialization = false;
                if (!ModConfig.BossRushMode && !ModConfig.InfiniteMode)
                {
                    return true;
                }
                if (avatar.isInDungeon <= 0) return true;
                return AddBossToFirstFloorTeleport(__instance, avatar, ref floorGuid, ref spawnPoint);
            }

            private static bool CheckAllPlayerReady(DungeonManager Dungeon, PlayerAvatar avatar, string floorGuid, string spawnPoint)
            {
                if (AllTeleport == null) GroupTeleportResolved = false;

                if (TeleportPlayerList.Contains(avatar))
                {
                    TeleportPlayerList.Remove(avatar);
                    return true;
                }

                if (!ModConfig.BossRushMode)
                    return true;

                if (PlayerSpawner.MultiplayerList.Count <= 1)
                    return true;

                if (avatar.isInDungeon <= 0)
                    return true;

                if (AllTeleport != null)
                    return false;

                Dictionary<string, string> FloorKey = [];
                Dictionary<string, string> FloorNameToKey = [];
                foreach (var kvp in Dungeon.generatedFloors.ToList())
                {
                    if (!FloorKey.ContainsKey(kvp.Key))
                    {
                        FloorKey.Add(kvp.Key, kvp.Value.name);
                    }
                    if (!FloorNameToKey.ContainsKey(kvp.Value.name))
                    {
                        FloorNameToKey.Add(kvp.Value.name, kvp.Key);
                    }
                }
                bool Falg = true;
                if (FloorKey.TryGetValue(floorGuid, out string FloorName))
                {
                    HashSet<string> UseCheckFloor =
                    [
                        MolelandRestFloor,
                        GrasslandRestFloor,
                        GrasslandMiniBossToBossFloor,
                        GrasslandMiniBossToBossFloor2,
                        LibraryRestFloor,
                        DeepCaveRestFloor
                    ];
                    if (UseCheckFloor.Contains(FloorName))
                        Falg = false;
                }
                if (Falg) return true;

                if (avatar != PlayerSpawner.MultiplayerList[0].PlayerAvatar)
                {
                    DungeonManager.Instance.Chat(avatar, "[Star's Sephiria Mod]", Tools.GetLocalization("Instruction_BossRush"));
                    return false;
                }

                Falg = false;
                Vector2 Pos = avatar.transform.position;
                foreach (var spawner in PlayerSpawner.MultiplayerList)
                {
                    var Player = spawner.PlayerAvatar;
                    if (Player == null || Player.IsDead) continue;

                    if(Vector2.Distance(Player.transform.position, Pos) > 3f)
                    {
                        Falg = true;
                    }
                }
                if (!Falg) AllTeleport = Dungeon.StartCoroutine(ReadyTeleportAllPlayer(Dungeon, Pos, floorGuid, spawnPoint));
                else DungeonManager.Instance.Chat(avatar, "[Star's Sephiria Mod]", Tools.GetLocalization("Instruction_BossRush2"));
                return false;
            }
            private static IEnumerator ReadyTeleportAllPlayer(DungeonManager Dungeon, Vector3 Pos, string floorGuid, string spawnPoint)
            {
                yield return new WaitForSeconds(0.5f);
                if (AllTeleport == null) yield break;

                PlayerAvatar? Leader = PlayerSpawner.MultiplayerList.Count > 0 ? PlayerSpawner.MultiplayerList[0].PlayerAvatar : null;
                if (Leader == null)
                {
                    AllTeleport = null;
                    yield break;
                }

                string TargetFloor = floorGuid;
                string TargetSpawn = spawnPoint;
                try
                {
                    AddBossToFirstFloorTeleport(Dungeon, Leader, ref TargetFloor, ref TargetSpawn);

                    Dungeon.generatedFloors.TryGetValue(TargetFloor, out FloorData TargetData);
                    string TargetFloorName = TargetData != null ? TargetData.name : "";

                    GroupTeleportResolved = true;
                    foreach (var spawner in PlayerSpawner.MultiplayerList)
                    {
                        var Player = spawner.PlayerAvatar;
                        if (Player == null || Player.IsDead || Player.isInDungeon <= 0) continue;

                        TeleportPlayerList.Add(Player);
                        Dungeon.MoveFloor(Player, TargetFloor, TargetSpawn, 0);
                        GrantFloorEntryReward(Player, TargetFloorName);
                    }
                }
                finally
                {
                    GroupTeleportResolved = false;
                    AllTeleport = null;
                }
            }

            private static bool AddBossToFirstFloorTeleport(DungeonManager __instance, PlayerAvatar avatar, ref string floorGuid, ref string spawnPoint)
            {
                try
                {
                    if (!__instance.isServer) return true;
                    if (GroupTeleportResolved) return true;

                    Dictionary<string, string> FloorKey = [];
                    Dictionary<string, string> FloorNameToKey = [];
                    foreach (var kvp in __instance.generatedFloors.ToList())
                    {
                        if (!FloorKey.ContainsKey(kvp.Key))
                        {
                            FloorKey.Add(kvp.Key, kvp.Value.name);
                        }
                        if (!FloorNameToKey.ContainsKey(kvp.Value.name))
                        {
                            FloorNameToKey.Add(kvp.Value.name, kvp.Key);
                        }
                    }

                    if(FloorKey.TryGetValue(floorGuid, out string FloorName) && avatar != null)
                    {
                        if (ModConfig.BossRushMode)
                        {
                            EnsurePartyState();

                            if (FloorName == MolelandRestFloor)
                            {
                                if (!UseSpawnerMoleElite)
                                {
                                    UseSpawnerMoleElite = true;
                                    SaveBossRushState();
                                }

                                string? To;
                                if (!(DefeatMoleElite.TryGetValue(PartyKey, out bool Elite) && Elite))
                                    To = MolelandBOSS2Floor;
                                else
                                    To = PickBoss(DefeatMolelandBossDictionary, random: false);

                                if (To != null && FloorNameToKey.TryGetValue(To, out var targetGuid))
                                    floorGuid = targetGuid;
                                if (avatar.isServer) avatar.HealPercent(60f);
                            }
                            else if (FloorName == GrasslandRestFloor)
                            {
                                // 城镇 → 图书馆入口：还有没打的草原 Boss 就继续打，避免跳过
                                string? To = PickBoss(DefeatGrasslandBossDictionary, random: true);
                                if (To != null && FloorNameToKey.TryGetValue(To, out var targetGuid))
                                    floorGuid = targetGuid;
                            }
                            else if (FloorName == LibraryRestFloor)
                            {
                                string? To = PickBoss(DefeatLibraryBossDictionary, random: true);
                                if (To != null && FloorNameToKey.TryGetValue(To, out var targetGuid))
                                {
                                    floorGuid = targetGuid;
                                    if (To == LibraryBoss1Floor && spawnPoint == "FLOORSTARTING2")
                                        spawnPoint = "FLOORSTARTING";
                                }
                            }
                            else if (FloorName == DeepCaveRestFloor)
                            {
                                string? To = PickBoss(DefeatDeepCaveBossDictionary, random: false);
                                if (To != null && FloorNameToKey.TryGetValue(To, out var targetGuid))
                                    floorGuid = targetGuid;
                            }

                            if (FloorKey.TryGetValue(floorGuid, out string FinalFloorName))
                            {
                                bool EliteDefeated = DefeatMoleElite.TryGetValue(PartyKey, out bool EliteFlag) && EliteFlag;
                                if (!EliteDefeated
                                    && DefeatMolelandBossList.Contains(FinalFloorName)
                                    && FinalFloorName != MolelandBOSS2Floor
                                    && FloorNameToKey.TryGetValue(MolelandBOSS2Floor, out var eliteGuid))
                                {
                                    floorGuid = eliteGuid;
                                    FinalFloorName = MolelandBOSS2Floor;
                                }

                                PlayerAvatar? Leader = PlayerSpawner.MultiplayerList.Count > 0 ? PlayerSpawner.MultiplayerList[0].PlayerAvatar : null;
                                if (IsBossFloor(FinalFloorName) || Leader == null || avatar == Leader)
                                    UpdateCurrentBossFloor(FinalFloorName, floorGuid);

                                if (FinalFloorName == DeepCaveLizardFloor || FinalFloorName == DeepCaveBirdFloor)
                                {
                                    LastFloor = true;
                                    SaveBossRushState();
                                }
                            }

                            FloorKey.TryGetValue(floorGuid, out string RewardFloorName);
                            GrantFloorEntryReward(avatar, RewardFloorName);
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[Boss传送功能出错]:{ex.Message}");
                    return true;
                }
            }

            [HarmonyPatch("RpcBroadcastPlayerLeave")]
            [HarmonyPostfix]
            static void BroadcastPlayerLeave_Postfix(DungeonManager __instance, string name, uint netId)
            {
                StarSephiriaMod.WeaponType.SafeRemove(name);
                StarSephiriaMod.CostumeType.SafeRemove(name);
                StarSephiriaMod.CostumeSkinType.SafeRemove(name);
                StarSephiriaMod.CostumeSkinKey.SafeRemove(name);
                PlayerAvatar? RemovePlayer = null;
                foreach(PlayerAvatar Player in DPSData.Keys)
                {
                    if (Player.Name == name)
                    {
                        RemovePlayer = Player;
                        break;
                    }
                }
                if(RemovePlayer != null)
                    DPSData.SafeRemove(RemovePlayer);
                StarSephiriaMod.InitializeModStat(name);
                PlayerReduceInBossRushMod();
                StarSephiriaMod.LogInfo($"删除{name}的各项数据记录");
            }
        }

        /// <summary>Boss Rush 地图节点固定成商店，RedMerchant 环境下换成红色商人。</summary>
        private static void SetMerchantType(FloorData Target)
        {
            if ((bool)DungeonManager.Instance && DungeonManager.Instance.dungeonEnvironment.TryGetValue("RedMerchant", out var value) && value > 0)
            {
                Target.generateMerchantType = 2;
            }
            else
            {
                Target.generateMerchantType = 1;
            }
        }

        [HarmonyPatch(typeof(StageEntity_Choice))]
        public static class StageEntity_Choice_Patch
        {
            [HarmonyPatch("GenerateStage")]
            [HarmonyPrefix]
            public static bool GenerateStage_Prefix(StageEntity_Choice __instance, DungeonManager dungeon, int seed, int stageOrder, bool createMiracle, bool createAnvil, bool enhancedDisturbance, string bossGenerateParameter, int hardModeAllBoss, ref FloorData[] __result)
            {
                try
                {
                    if (!ModConfig.BossRushMode) return true;

                    Dictionary<EFloorThreatType, List<FloorGenerator>> dictionary = [];
                    Dictionary<EFloorThreatType, Dictionary<EFloorMainEventType, List<FloorGenerator>>> dictionary2 = [];

                    foreach (object obj in Enum.GetValues(typeof(EFloorThreatType)))
                    {
                        EFloorThreatType efloorThreatType = (EFloorThreatType)obj;
                        dictionary[efloorThreatType] = [];
                        dictionary2[efloorThreatType] = [];
                        foreach (object obj2 in Enum.GetValues(typeof(EFloorMainEventType)))
                        {
                            EFloorMainEventType efloorMainEventType = (EFloorMainEventType)obj2;
                            dictionary2[efloorThreatType][efloorMainEventType] = [];
                        }
                    }

                    // BOSS楼层生成器专用列表
                    List<FloorGenerator> BossList = [];

                    // 分类随机事件楼层预置体
                    GameObject[] array = __instance.randomEventFloorPrefabs;
                    int i;
                    for (i = 0; i < array.Length; i++)
                    {
                        FloorGenerator component = array[i].GetComponent<FloorGenerator>();
                        dictionary[component.floorThreatType].Add(component);
                    }

                    // 分类固定事件楼层预置体，并收集BOSS类型
                    array = __instance.fixedEventFloorPrefabs;
                    for (i = 0; i < array.Length; i++)
                    {
                        FloorGenerator Cpmponent = array[i].GetComponent<FloorGenerator>();
                        dictionary2[Cpmponent.floorThreatType][Cpmponent.floorMainEventType].Add(Cpmponent);
                        if (Cpmponent.floorThreatType == EFloorThreatType.Boss)
                        {
                            BossList.Add(Cpmponent);
                        }
                    }

                    int num = 0;
                    Random rngChoice = new(seed);// 用于事件/节点选择
                    Random rngSeed = new(seed + 1);// 用于打乱节点位置
                    Random rngPos = new(seed + 10);// 用于楼层 seed
                    Random rngDisturb = new(seed + 20);// 用于位置抖动
                    Random rngMerchant = new(seed + 99);// 用于困难战斗干扰
                    Random rngHidden = new(seed + 1000);// 用于 Boss 硬化与隐藏房
                    Random rngHardBoss = new(seed + 20);// 用于战斗区楼层判定
                    // 创建第一个楼层（起始点）
                    FloorData floorData = new()
                    {
                        guid = Guid.NewGuid().ToString(),
                        name = __instance.firstFloor.FloorDataName,
                        stageName = __instance.name,
                        seed = rngPos.Next(),
                        globalY = stageOrder,
                        globalX = num++,
                        nodeProgress = 0,
                        prefabAssetId = __instance.firstFloor.GetComponent<NetworkIdentity>().assetId,
                        hasTraveler = false,
                        difficulty = __instance.firstDifficulty,
                        mainEventType = __instance.firstFloor.floorMainEventType,
                        threatType = __instance.firstFloor.floorThreatType,
                        disturbance = ""
                    };

                    List<FloorData> 生成列表 = [floorData];

                    // 初始化唯一楼层生成器字典
                    Dictionary<EFloorMainEventType, List<FloorGenerator>> dictionary3 = [];
                    foreach (FloorGenerator floorGenerator in __instance.uniqueFloorPrefabs)
                    {
                        if (!dictionary3.ContainsKey(floorGenerator.floorMainEventType))
                        {
                            dictionary3[floorGenerator.floorMainEventType] = [];
                        }
                        dictionary3[floorGenerator.floorMainEventType].Add(floorGenerator);
                    }

                    // 步骤数组
                    List<FloorData>[] array3 = new List<FloorData>[__instance.steps.Length + 1];
                    for (int j = 0; j < array3.Length; j++)
                    {
                        array3[j] = [];
                    }
                    array3[0].Add(floorData);

                    // 遍历每个步骤生成楼层
                    int stepsLength = __instance.steps.Length;
                    int ActualLevels = 0;
                    List<FloorData> hiddenCandidates = [];
                    for (int Stack = 0; Stack < stepsLength; Stack++)
                    {
                        int Last = stepsLength - 1;
                        if (Last <= 0)
                        {
                            Last = 1;
                        }
                        if ((Stack == 0 || Stack == 1) && !createAnvil)
                            continue;
                        if (Stack == 2 && LastFloor)
                        {
                            LastFloor = false;
                            if (PlayerSpawner.MultiplayerList.Count <= 1) SaveManager.CurrentRun.SetBool("[Star's Sephiria Mod]BossRush_LastFloor", false);
                            continue;
                        }

                        // 计算当前步骤的难度
                        float num3 = Stack / (float)Last;
                        int num4 = Mathf.CeilToInt((__instance.lastDifficulty - 1 - __instance.firstDifficulty) * num3 + __instance.firstDifficulty) + 1;

                        int UseStack = 1;
                        int BossStack = BossList.Count;

                        // 过滤当前步骤可能的主要事件类型
                        List<EFloorMainEventType> MiniBossALLEvents = [];
                        EFloorMainEventType[] possibleMainEvents = __instance.steps[Stack].possibleMainEvents;

                        for (i = 0; i < possibleMainEvents.Length; i++)
                        {
                            if (possibleMainEvents[i] == EFloorMainEventType.Anvil)
                                continue;

                            MiniBossALLEvents.Add(possibleMainEvents[i]);
                        }
                        // 创建节点类型列表
                        List<StageEntity_Choice.EFloorNodeType> EfloorList = [];
                        // 先把固定匹配事件加进去
                        foreach (StageEntity_Choice.EFloorNodeType nodeType in __instance.steps[Stack].matchEvents)
                        {
                            if (nodeType.threat != EFloorThreatType.Boss)
                            {
                                EfloorList.Add(nodeType);
                            }
                            else
                            {
                                // Boss 节点按 Boss 数量复制
                                for (int m = 0; m < BossStack; m++)
                                {
                                    EfloorList.Add(nodeType);
                                }
                            }
                        }
                        // 再把随机主事件加进去
                        foreach (EFloorMainEventType evType in MiniBossALLEvents)
                        {
                            EfloorList.Add(new StageEntity_Choice.EFloorNodeType
                            {
                                ev = evType,
                                threat = __instance.steps[Stack].possibleThreats.GetRandom(rngChoice),
                                fixedPool = false,
                                essential = (evType == EFloorMainEventType.Unknown)
                            });
                        }

                        // 分离必需节点和可选节点
                        List<StageEntity_Choice.EFloorNodeType> NecessaryStage = [];
                        List<StageEntity_Choice.EFloorNodeType> OptionalNodes = [];
                        foreach (var nodeType in EfloorList)
                        {
                            if (nodeType.essential)
                                NecessaryStage.Add(nodeType);
                            else
                                OptionalNodes.Add(nodeType);
                        }

                        // 计算节点位置分布
                        if (Stack == stepsLength - 2)
                            UseStack = MiniBossALLEvents.Count;

                        if (Stack == stepsLength - 1)
                            UseStack = BossStack;

                        float Middle = (UseStack - 1f) / 2f;
                        int Rand = -1;
                        if (UseStack > 0 && BossStack > 0)
                        {
                            Rand = rngHidden.Next(0, UseStack);
                        }

                        List<float> Offset = [];
                        for (int n = 0; n < UseStack; n++)
                        {
                            float NU = n - Middle;
                            if (UseStack <= 1)
                            {
                                NU = 0f;
                            }
                            Offset.Add(NU * (160f * 3f / UseStack) + rngDisturb.Next(-20, 20) * 3f / UseStack);
                        }
                        Offset.Shuffle(rngSeed);

                        // 为当前步骤生成每个节点
                        for (int k = 0; k < UseStack; k++)
                        {
                            StageEntity_Choice.EFloorNodeType efloorNodeType3;
                            if (NecessaryStage.Count > 0)
                            {
                                int num12 = rngChoice.Next(0, NecessaryStage.Count);
                                efloorNodeType3 = NecessaryStage[num12];
                                NecessaryStage.RemoveAt(num12);
                            }
                            else if (OptionalNodes.Count == 0)
                            {
                                efloorNodeType3 = new StageEntity_Choice.EFloorNodeType
                                {
                                    ev = EFloorMainEventType.EXP,
                                    threat = EFloorThreatType.Battle,
                                    fixedPool = false,
                                    essential = false
                                };
                            }
                            else
                            {
                                int num13 = rngChoice.Next(0, OptionalNodes.Count);
                                efloorNodeType3 = OptionalNodes[num13];
                                OptionalNodes.RemoveAt(num13);
                            }

                            EFloorThreatType FloorType = efloorNodeType3.threat;
                            EFloorMainEventType FloorEventType = efloorNodeType3.ev;
                            FloorGenerator floorGenerator2;

                            // 根据节点类型选择楼层生成器
                            if (efloorNodeType3.fixedPool)
                            {
                                if (FloorType == EFloorThreatType.Boss)
                                {
                                    int num14 = rngChoice.Next(0, BossList.Count);
                                    floorGenerator2 = BossList[num14];
                                    BossList.RemoveAt(num14);
                                }
                                else
                                {
                                    floorGenerator2 = dictionary2[FloorType][FloorEventType].GetRandom(rngChoice);
                                }
                            }
                            else
                            {
                                if (enhancedDisturbance)
                                {
                                    if (FloorType == EFloorThreatType.Battle)
                                    {
                                        FloorType = EFloorThreatType.HardBattle;
                                    }
                                }
                                else if (FloorType == EFloorThreatType.Battle && dictionary[EFloorThreatType.BattleFloor].Count > 0 && rngHardBoss.NextDouble() < __instance.battleZoneFloorChance)
                                {
                                    FloorType = EFloorThreatType.BattleFloor;
                                }
                                floorGenerator2 = dictionary[FloorType].GetRandom(rngChoice);
                            }

                            if (!floorGenerator2)
                            {
                                k--;
                            }
                            else
                            {
                                float 节点偏移X = Offset.SafeRandomAccess(k, ArrayExtensions.ERandomAccessType.Clamp);
                                if ((Stack == 0 || Stack == 1) && createAnvil)
                                {
                                    floorGenerator2 = dictionary3[EFloorMainEventType.Anvil].GetRandom(rngChoice);
                                    FloorEventType = EFloorMainEventType.Anvil;
                                }
                                if (FloorType == EFloorThreatType.Boss && FloorEventType != EFloorMainEventType.None)
                                {
                                    FloorEventType = EFloorMainEventType.None;
                                }

                                // 设置干扰类型
                                string text = "";
                                string text2 = "";
                                if (FloorType == EFloorThreatType.HardBattle)
                                {
                                    text = __instance.disturbanceList.GetRandom(rngMerchant);
                                }
                                else if (FloorType == EFloorThreatType.Boss && hardModeAllBoss > 0 && Rand == k)
                                {
                                    text2 = __instance.GetRandomHardBattleDisturbance(dungeon, rngHidden);
                                }

                                // 创建楼层数据
                                float Y = (ActualLevels + 1) * -__instance.distanceBetweenNodes + rngDisturb.Next(-15, 15);
                                FloorData Data = new()
                                {
                                    guid = Guid.NewGuid().ToString(),
                                    name = floorGenerator2.name,
                                    stageName = __instance.name,
                                    seed = rngPos.Next(),
                                    globalX = num++,
                                    globalY = stageOrder,
                                    nodeProgress = k + 1,
                                    prefabAssetId = floorGenerator2.GetComponent<NetworkIdentity>().assetId,
                                    mainEventType = FloorEventType,
                                    threatType = FloorType,
                                    hasTraveler = false,
                                    difficulty = num4,
                                    nodeAnchoredPositionX = 节点偏移X,
                                    nodeAnchoredPositionY = Y,
                                    disturbance = text,
                                    bossHard = text2
                                };
                                if (FloorType != EFloorThreatType.Boss && FloorEventType != EFloorMainEventType.Anvil)
                                {
                                    hiddenCandidates.Add(Data);
                                }
                                生成列表.Add(Data);
                                array3[ActualLevels + 1].Add(Data);
                            }
                        }
                        ActualLevels += 1;
                    }
                    if (hiddenCandidates.Count > 0 && !LastFloor)
                    {
                        Random hiddenRand = new(seed + 1000);
                        List<FloorData> pool = hiddenCandidates.ToList();

                        pool[hiddenRand.Next(pool.Count)].hiddenRoomCount = 1;

                        // 两个商人节点，第二个同时是旅人
                        pool = hiddenCandidates.ToList();
                        FloorData FirstMerchant = pool[hiddenRand.Next(pool.Count)];
                        SetMerchantType(FirstMerchant);
                        pool.Remove(FirstMerchant);
                        FloorData SecondMerchant = pool[hiddenRand.Next(pool.Count)];
                        SetMerchantType(SecondMerchant);
                        SecondMerchant.hasTraveler = true;
                        pool.Remove(SecondMerchant);

                        pool = hiddenCandidates.ToList();
                        pool[hiddenRand.Next(pool.Count)].randomRoomCount += 1;
                        pool[hiddenRand.Next(pool.Count)].randomRoomCount += 2;
                    }
                    // 建立楼层之间的连接关系
                    for (int a = 0; a < ActualLevels && a < array3.Length - 1; a++)
                    {
                        for (int b = 0; b < array3[a].Count; b++)
                        {
                            List<string> LineList = [];
                            if (a + 1 < array3.Length && array3[a + 1].Count > 0)
                            {
                                foreach (FloorData floorData7 in array3[a + 1])
                                {
                                    LineList.Add(floorData7.guid);
                                }
                            }
                            array3[a][b].connectionToOtherFloors = LineList.ToArray();
                        }
                    }
                    // 添加必需的特殊楼层
                    foreach (FloorGenerator Floor in __instance.essentialFloorPrefabs)
                    {
                        FloorData FloorData = new()
                        {
                            guid = Guid.NewGuid().ToString(),
                            name = Floor.name,
                            stageName = __instance.name,
                            seed = rngPos.Next(),
                            prefabAssetId = Floor.GetComponent<NetworkIdentity>().assetId,
                            hasTraveler = false,
                            isHidden = true,
                            globalY = stageOrder,
                            globalX = num++,
                            nodeProgress = -1,
                            nodeAnchoredPositionX = -1280f,
                            nodeAnchoredPositionY = 0f,
                            disturbance = ""
                        };
                        生成列表.Add(FloorData);
                    }
                    __result = 生成列表.ToArray();
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[StageEntity_Choice]自定义生成失败: {ex}");
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(StageEntity_GrasslandTown))]
        public static class StageEntity_GrasslandTown_Patch
        {
            private static readonly Dictionary<string, (StageEntity_GrasslandTown Stage, GrasslandTownEventEntity[] Events, int ClearCount)> TownBackups = [];
            private static readonly Dictionary<string, GrasslandTownEventEntity> SyntheticEvents = [];

            /// <summary>离开 Boss Rush / 卸载模组时把城镇资产改回原样。</summary>
            public static void RestoreOverrides()
            {
                foreach (var Backup in TownBackups.Values)
                {
                    if (Backup.Stage == null) continue;
                    Backup.Stage.nodeEvents = Backup.Events;
                    Backup.Stage.eventCountToClear = Backup.ClearCount;
                }
                TownBackups.Clear();
                SyntheticEvents.Clear();
            }

            private static void ApplyOverrides(StageEntity_GrasslandTown Stage)
            {
                if (!TownBackups.TryGetValue(Stage.name, out var Backup) || Backup.Stage != Stage)
                {
                    Backup = (Stage, Stage.nodeEvents, Stage.eventCountToClear);
                    TownBackups[Stage.name] = Backup;
                }
                // 打一个事件就能解锁下方 Boss 节点
                Stage.eventCountToClear = 1;
            }

            [HarmonyPatch("GenerateStage")]
            [HarmonyPrefix]
            static bool GenerateStage_Prefix(StageEntity_GrasslandTown __instance, DungeonManager dungeon, int seed, int stageOrder, bool createMiracle, bool createAnvil, bool enhancedDisturbance, string bossGenerateParameter, int hardModeAllBoss, ref FloorData[] __result)
            {
                if (!ModConfig.BossRushMode)
                {
                    RestoreOverrides();
                    return true;
                }
                try
                {
                    ApplyOverrides(__instance);

                    Random rngSeed = new(seed + 1);
                    Random rngPos = new(seed + 10);
                    int GlobalX = 0;
                    List<FloorData> Result = [];
                    List<FloorData> Options = [];

                    // 起始层
                    FloorData Head = new()
                    {
                        guid = Guid.NewGuid().ToString(),
                        name = __instance.firstFloor.FloorDataName,
                        stageName = __instance.name,
                        seed = rngPos.Next(),
                        globalY = stageOrder,
                        globalX = GlobalX++,
                        nodeProgress = 0,
                        prefabAssetId = __instance.firstFloor.GetComponent<NetworkIdentity>().assetId,
                        hasTraveler = false,
                        difficulty = __instance.firstDifficulty,
                        mainEventType = __instance.firstFloor.floorMainEventType,
                        threatType = __instance.firstFloor.floorThreatType,
                        disturbance = ""
                    };
                    Result.Add(Head);

                    int Progress = 1;

                    // 复制城镇原本保留的 MiniBoss 房，只改奖励类型
                    GrasslandTownEventEntity? MiniBossEvent = __instance.nodeEvents.FirstOrDefault(Event => Event != null
                        && Event.threatType == EFloorThreatType.MiniBoss
                        && Event.floorPrefab != null);
                    if (MiniBossEvent != null)
                    {
                        EFloorMainEventType[] RewardTypes = [EFloorMainEventType.EXP, EFloorMainEventType.Charm, EFloorMainEventType.Enchant, EFloorMainEventType.StoneTablet];
                        foreach (EFloorMainEventType Type in RewardTypes)
                        {
                            GrasslandTownEventEntity Event = GetSyntheticEvent(__instance, Type.ToString(), "UI_WorldMap_Event_" + Type, "UI_WorldMap_Event_" + Type + "_Description", MiniBossEvent.floorPrefab, EFloorThreatType.MiniBoss);
                            AddOption(__instance, Event, MiniBossEvent.floorPrefab, EFloorThreatType.MiniBoss, Type, false, stageOrder, rngSeed, rngPos, ref Progress, ref GlobalX, Options, Result);
                        }
                    }
                    else
                    {
                        StarSephiriaMod.LogError("[BossRush]城镇没有可用的 MiniBoss 事件房");
                    }

                    // 2 个商店 + 2 个隐藏房
                    if (Options.Count > 0)
                    {
                        Random rngPick = new(seed + 99);
                        List<FloorData> Pool = Options.ToList();
                        for (int i = 0; i < 2 && Pool.Count > 0; i++)
                        {
                            FloorData Merchant = Pool[rngPick.Next(Pool.Count)];
                            SetMerchantType(Merchant);
                            Pool.Remove(Merchant);
                        }
                        Pool = Options.ToList();
                        for (int i = 0; i < 2 && Pool.Count > 0; i++)
                        {
                            FloorData Hidden = Pool[rngPick.Next(Pool.Count)];
                            Hidden.hiddenRoomCount += 1;
                            Pool.Remove(Hidden);
                        }
                    }

                    // 必需楼层（隐藏）
                    foreach (FloorGenerator Floor in __instance.essentialFloorPrefabs)
                    {
                        FloorData FloorData = new()
                        {
                            guid = Guid.NewGuid().ToString(),
                            name = Floor.name,
                            stageName = __instance.name,
                            seed = rngSeed.Next(),
                            prefabAssetId = Floor.GetComponent<NetworkIdentity>().assetId,
                            hasTraveler = false,
                            isHidden = true,
                            globalY = stageOrder,
                            globalX = GlobalX++,
                            nodeProgress = -1,
                            nodeAnchoredPositionX = -1280f,
                            nodeAnchoredPositionY = 0f,
                            disturbance = ""
                        };
                        Result.Add(FloorData);
                    }

                    // Boss 楼层：除最后一个外隐藏，最后一个作为公告板下方的 Boss 节点
                    for (int BossIndex = 0; BossIndex < __instance.bossFloors.Length; BossIndex++)
                    {
                        FloorGenerator BossFloors = __instance.bossFloors[BossIndex];
                        FloorData FloorData = new()
                        {
                            guid = Guid.NewGuid().ToString(),
                            name = BossFloors.name,
                            stageName = __instance.name,
                            seed = rngSeed.Next(),
                            globalY = stageOrder,
                            globalX = GlobalX++,
                            nodeProgress = 1,
                            prefabAssetId = BossFloors.GetComponent<NetworkIdentity>().assetId,
                            hasTraveler = false,
                            isHidden = BossIndex < __instance.bossFloors.Length - 1,
                            difficulty = __instance.lastDifficulty,
                            mainEventType = BossFloors.floorMainEventType,
                            threatType = BossFloors.floorThreatType,
                            nodeAnchoredPositionX = 0,
                            nodeAnchoredPositionY = -__instance.distanceBetweenNodes + rngPos.Next(-15, 15),
                            disturbance = ""
                        };
                        Result.Add(FloorData);
                    }

                    __result = Result.ToArray();
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[StageEntity_GrasslandTown]自定义生成失败: {ex}");
                    return true;
                }
            }

            private static void AddOption(StageEntity_GrasslandTown Stage, GrasslandTownEventEntity Event, FloorGenerator Prefab, EFloorThreatType Threat, EFloorMainEventType MainEventType, bool AnvilFirst, int StageOrder, Random RngSeed, Random RngPos, ref int Progress, ref int GlobalX, List<FloorData> Options, List<FloorData> Result)
            {
                int EventSeed = RngSeed.Next();
                int NodeProgress = Progress++;
                FloorData Node = new()
                {
                    guid = Guid.NewGuid().ToString(),
                    name = Prefab.name,
                    stageName = Stage.name,
                    seed = EventSeed,
                    globalX = GlobalX++,
                    globalY = StageOrder,
                    nodeProgress = NodeProgress,
                    prefabAssetId = Prefab.GetComponent<NetworkIdentity>().assetId,
                    hasTraveler = false,
                    difficulty = RngSeed.Next(Stage.firstDifficulty, Stage.lastDifficulty + 1),
                    mainEventType = MainEventType,
                    threatType = Threat,
                    nodeAnchoredPositionX = RngPos.Next(-15, 15),
                    nodeAnchoredPositionY = NodeProgress * -Stage.distanceBetweenNodes + RngPos.Next(-15, 15),
                    disturbance = Threat == EFloorThreatType.HardBattle ? Stage.disturbanceList.GetRandom(RngPos) : "",
                    questBoardEventId = Event.id,
                    isAnvilFirstReward = AnvilFirst
                };

                FloorGenerator RewardPrefab = Stage.townRewardFloors.GetRandom(RngSeed);
                FloorData Reward = new()
                {
                    guid = Guid.NewGuid().ToString(),
                    name = RewardPrefab.name,
                    stageName = Stage.name,
                    seed = EventSeed,
                    globalX = GlobalX++,
                    globalY = StageOrder,
                    nodeProgress = NodeProgress,
                    prefabAssetId = RewardPrefab.GetComponent<NetworkIdentity>().assetId,
                    hasTraveler = false,
                    isHidden = true,
                    difficulty = Node.difficulty,
                    mainEventType = Node.mainEventType,
                    threatType = Threat,
                    nodeAnchoredPositionX = RngPos.Next(-15, 15),
                    nodeAnchoredPositionY = NodeProgress * -Stage.distanceBetweenNodes + RngPos.Next(-15, 15),
                    disturbance = "",
                    isAnvilFirstReward = AnvilFirst
                };

                Node.connectionToOtherFloors = [Reward.guid];
                Result.Add(Node);
                Result.Add(Reward);
                Options.Add(Node);
            }

            private static GrasslandTownEventEntity GetSyntheticEvent(StageEntity_GrasslandTown Stage, string Key, string NameKey, string DescriptionKey, FloorGenerator Prefab, EFloorThreatType Threat)
            {
                string CacheKey = Stage.name + ":" + Key;
                if (SyntheticEvents.TryGetValue(CacheKey, out GrasslandTownEventEntity? Cached) && Cached != null)
                    return Cached;

                GrasslandTownEventEntity Event = ScriptableObject.CreateInstance<GrasslandTownEventEntity>();
                Event.id = "BossRush_" + Key;
                Event.eventName = new LocalizedString(NameKey);
                Event.eventDescription = new LocalizedString(DescriptionKey);
                Event.threatType = Threat;
                Event.floorPrefab = Prefab;
                Event.isEssential = false;
                Event.isAnvilFirstReward = false;
                Event.eventObjectiveType = GrasslandTownEventEntity.EEventObjectiveType.ClearBattle;
                Event.targetObjectiveCount = 1;
                Event.objectiveText = new LocalizedString(DescriptionKey);

                List<GrasslandTownEventEntity> Events = new(Stage.nodeEvents);
                Events.Add(Event);
                Stage.nodeEvents = Events.ToArray();
                SyntheticEvents[CacheKey] = Event;
                return Event;
            }

        }

        [HarmonyPatch(typeof(BossSpawner))]
        public static class BossSpawner_Patch
        {
            [HarmonyPatch("OnStartServer")]
            [HarmonyPrefix]
            public static bool OnStartServer_Prefix(BossSpawner __instance)
            {
                try
                {
                    StarSephiriaMod.LogInfoDungeon($"生成的BOSS实体: {__instance.bossSocialID.avatarPrefab.name}");
                    UseDPS = true;
                    if (PlayerSpawner.MultiplayerList.Count <= 1)
                    {
                        PlayerAvatar Player = PlayerSpawner.MultiplayerList[0].PlayerAvatar;
                        NoHitCount.Add(Player, new NoHitRecord(Player.GetCustomStatUnsafe(TextFile.CurrentRosion)));
                    }
                    if (ModConfig.HardMode > 0)
                    {
                        if (__instance.bossSocialID.avatarPrefab.name == "Askard_Chapter5-3")
                        {
                            if (AllBossList.TryGetValue("Askard_Chapter2", out var Boss))
                                __instance.bossSocialID.avatarPrefab = Boss;
                        }
                    }
                    else if (__instance.bossSocialID.avatarPrefab.name == "Askard_Chapter2")
                    {
                        if (AllBossList.TryGetValue("Askard_Chapter5-3", out var Boss))
                            __instance.bossSocialID.avatarPrefab = Boss;
                    }
                    if (ModConfig.BossRushMode)
                    {
                        if (!UseSpawnerMoleElite)
                        {
                            if (__instance.bossSocialID.avatarPrefab.name == "MoleBigBomb")
                            {
                                if (AllBossList.TryGetValue("MoleElite_Boss", out var Boss))
                                    __instance.bossSocialID.avatarPrefab = Boss;
                            }
                        }
                        else
                        {
                            if (__instance.bossSocialID.avatarPrefab.name == "MoleElite_Boss")
                            {
                                if (AllBossList.TryGetValue("MoleBigBomb", out var Boss))
                                    __instance.bossSocialID.avatarPrefab = Boss;
                            }
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[BossSpawner][OnStartServer_Prefix]出错: {ex.Message}");
                    return true;
                }
            }

            [HarmonyPatch("ServerEndBossDramaticDie")]
            [HarmonyPostfix]
            static void ServerEndBossDramaticDie_Postfix(BossSpawner __instance)
            {
                if (!ModConfig.BossRushMode || !__instance.isServer) return;
                DungeonPatchs.NotifySpawnerBossDefeated(__instance);
            }

            [HarmonyPatch("ApplyClearProgressionLocal")]
            [HarmonyPostfix]
            public static void ApplyClearProgressionLocal_Postfix(BossSpawner __instance, int gameClearType)
            {
                int HardPoint = DungeonManager.Instance.CalculateCurrentHardModePoints();
                if (gameClearType > 0 && HardPoint > 0)
                {
                    PlayerAvatar? Player = (CombatManager.Instance ? CombatManager.Instance.CurrentPlayer : null);
                    if (Player != null)
                    {
                        int WeaponId = Player.GetPlayerWeapon();
                        if (WeaponId >= 0)
                        {
                            int OldValue = SaveManager.Current.GetInt("[Star's Sephiria Mod]HardModePoint_Weapon_" + WeaponId, 0);
                            if (HardPoint > OldValue)
                            {
                                SaveManager.Current.SetInt("[Star's Sephiria Mod]HardModePoint_Weapon_" + WeaponId, HardPoint);
                            }
                        }
                    }
                }
            }
        }
    }
}