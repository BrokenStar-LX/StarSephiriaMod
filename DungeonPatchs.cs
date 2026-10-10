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
        private static Coroutine? AllTeleport = null;
        private static readonly List<PlayerAvatar> TeleportPlayerList = new List<PlayerAvatar>();

        public static float Seed = 0;
        public static Dictionary<string, List<string>> NoAddExp = new Dictionary<string, List<string>>();

        public static Dictionary<string, bool> DefeatMoleElite = new Dictionary<string, bool>();
        public static bool UseSpawnerMoleElite = false;

        const string HomeFloor = "Grasstown_Chapter4_Dark";
        const string MultiZoneFloor = "MultiZone";

        const string FirstFloor = "Moleland_EntranceC6";
        const string MolelandRestFloor = "Moleland_Station";
        const string MolelandBOSS1Floor = "Moleland_Boss2";
        const string MolelandBOSS2Floor = "Moleland_Boss_Bomb";
        const string MolelandBOSS3Floor = "Moleland_Boss_RootDemon";
        private static readonly List<string> DefeatMolelandBossList = new List<string>()
        {
            MolelandBOSS1Floor, MolelandBOSS2Floor, MolelandBOSS3Floor
        };
        public static Dictionary<string, List<string>> DefeatMolelandBossDictionary = new Dictionary<string, List<string>>();

        const string GrasslandRestFloor = "Grassland_LibraryEntrance"; 
        const string GrasslandBoss1Floor = "GrasslandBoss"; 
        const string GrasslandBoss2Floor = "GrasslandBoss_Panther";
        const string GrasslandMiniBossToBossFloor = "Grasstown_Chapter6_ForReward_Day";
        const string GrasslandMiniBossToBossFloor2 = "Grasstown_Chapter6_ForReward";
        private static readonly List<string> DefeatGrasslandBossList = new List<string>()
        {
            GrasslandBoss1Floor, GrasslandBoss2Floor
        };
        public static Dictionary<string, List<string>> DefeatGrasslandBossDictionary = new Dictionary<string, List<string>>();

        const string LibraryRestFloor = "Library_Elevator_Chapter2";
        const string LibraryBoss1Floor = "LibraryBoss"; 
        const string LibraryBoss2Floor = "LibraryBoss_Factory";
        private static readonly List<string> DefeatLibraryBossList = new List<string>()
        {
            LibraryBoss1Floor, LibraryBoss2Floor
        };
        public static Dictionary<string, List<string>> DefeatLibraryBossDictionary = new Dictionary<string, List<string>>();

        const string DeepCaveRestFloor = "DeepCaveDark_Boss_Q_Chapter6";
        const string DeepCaveLizardFloor = "DeepCave_Boss_Lizard_Chapter6";
        const string DeepCaveBirdFloor = "DeepCave_Boss_Bird_Chapter6";
        private static readonly List<string> DefeatDeepCaveBossList = new List<string>()
        {
            DeepCaveBirdFloor, DeepCaveLizardFloor
        };
        public static Dictionary<string, List<string>> DefeatDeepCaveBossDictionary = new Dictionary<string, List<string>>();

        public static bool NoInitialization = false;
        public static bool LastFloor = false;
        public static float HardModAddLife = 0f;
        public static event Action? OnLoadNewNumberofReincarnations = null;
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
                StarSephiriaPatches.DefeatBoss.Clear();
                foreach (PlayerSpawner PlayerSp in PlayerSpawner.MultiplayerList)
                {
                    PlayerAvatar Player = PlayerSp.PlayerAvatar;
                    DungeonManager.Instance.StartCoroutine(StarSephiriaPatches.DeleteUnintentionalItems(Player));
                }
            }
            else
            {
                HardModAddLife = 0f;
                StarSephiriaPatches.DefeatBoss.Clear();
                OnLoadNewNumberofReincarnations?.Invoke();
            }
            StarSephiriaMod.LogInfoDungeon($"—初始化模式数据—");
            if (SaveManager.Current.GetInt("HardModeShard_[Star's Sephiria Mod]BossRushMode_Level", 0) == 1)
            {
                Seed = (float)new Random().NextDouble();
                DefeatMoleElite = new Dictionary<string, bool>();
                DefeatMolelandBossDictionary = new Dictionary<string, List<string>>();
                DefeatGrasslandBossDictionary = new Dictionary<string, List<string>>();
                DefeatLibraryBossDictionary = new Dictionary<string, List<string>>();
                DefeatDeepCaveBossDictionary = new Dictionary<string, List<string>>();
                NoAddExp = new Dictionary<string, List<string>>();
                foreach (PlayerSpawner PlayerSp in PlayerSpawner.MultiplayerList)
                {
                    string Player = PlayerSp.PlayerAvatar.Name;
                    DefeatMoleElite.Add(Player, false);
                    DefeatMolelandBossDictionary.Add(Player, DefeatMolelandBossList.ToList());
                    DefeatGrasslandBossDictionary.Add(Player, DefeatGrasslandBossList.ToList());
                    DefeatLibraryBossDictionary.Add(Player, DefeatLibraryBossList.ToList());
                    DefeatDeepCaveBossDictionary.Add(Player, DefeatDeepCaveBossList.ToList());
                    if (UseName == "LocalMoveFloor")
                    {
                        PlayerSp.PlayerAvatar.AddOrphanedStatusInstance(StatusDatabase.CreateStatusEntity("INVENTORY_SLOT/10"));
                    }
                    StarSephiriaMod.LogInfoDungeon($"初始化{Player}的数据");
                }
                UseSpawnerMoleElite = false;
                LastFloor = false;
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

                    DefeatMoleElite = new Dictionary<string, bool>();
                    DefeatMolelandBossDictionary = new Dictionary<string, List<string>>();
                    DefeatGrasslandBossDictionary = new Dictionary<string, List<string>>();
                    DefeatLibraryBossDictionary = new Dictionary<string, List<string>>();
                    DefeatDeepCaveBossDictionary = new Dictionary<string, List<string>>();

                    string Player = SaveManager.CurrentRun.GetString("[Star's Sephiria Mod]BossRush_Player", "");
                    UseSpawnerMoleElite = SaveManager.CurrentRun.GetBool("[Star's Sephiria Mod]BossRush_MoleList_MoleElite", false);
                    StarSephiriaMod.LogInfoDungeon($"是否已生成精英鼹鼠：{UseSpawnerMoleElite}");

                    bool falg = SaveManager.CurrentRun.GetBool("[Star's Sephiria Mod]BossRush_DefeatMoleElite", false);
                    DefeatMoleElite.Add(Player, falg);
                    StarSephiriaMod.LogInfoDungeon($"是否击败精英鼹鼠：{falg}");

                    bool Last = SaveManager.CurrentRun.GetBool("[Star's Sephiria Mod]BossRush_LastFloor", false);
                    LastFloor = Last;
                    StarSephiriaMod.LogInfoDungeon($"是否最后一层：{Last}");

                    List<string> BossList = new List<string>();
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
                        DefeatMolelandBossDictionary.Add(Player, new List<string>());
                        StarSephiriaMod.LogInfoDungeon($"鼹鼠层剩余Boss列表：无");
                    }

                    BossList = new List<string>();
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
                        DefeatGrasslandBossDictionary.Add(Player, new List<string>());
                        StarSephiriaMod.LogInfoDungeon($"草原层剩余Boss列表：无");
                    }

                    BossList = new List<string>();
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
                        DefeatLibraryBossDictionary.Add(Player, new List<string>());
                        StarSephiriaMod.LogInfoDungeon($"图书馆层剩余Boss列表：无");
                    }

                    BossList = new List<string>();
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
                        DefeatDeepCaveBossDictionary.Add(Player, new List<string>());
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
            foreach (string Player in StarSephiriaMod.CostumeType.Keys)
            {
                if (!DefeatMoleElite.ContainsKey(Player))
                {
                    DefeatMoleElite.Add(Player, false);
                    DefeatMolelandBossDictionary.Add(Player, DefeatMolelandBossList.ToList());
                    DefeatGrasslandBossDictionary.Add(Player, DefeatGrasslandBossList.ToList());
                    DefeatLibraryBossDictionary.Add(Player,DefeatLibraryBossList.ToList());
                    DefeatDeepCaveBossDictionary.Add(Player, DefeatDeepCaveBossList.ToList());
                    StarSephiriaMod.LogInfoDungeon($"初始化{Player}的数据");
                }
            }
        }
        public static void PlayerReduceInBossRushMod()
        {
            StarSephiriaMod.LogInfoDungeon($"检测到有玩家退出");
            foreach (string Player in DefeatMoleElite.Keys)
            {
                if (!StarSephiriaMod.CostumeType.ContainsKey(Player))
                {
                    DefeatMoleElite.SafeRemove(Player);
                    DefeatMolelandBossDictionary.SafeRemove(Player);
                    DefeatGrasslandBossDictionary.SafeRemove(Player);
                    DefeatLibraryBossDictionary.SafeRemove(Player);
                    DefeatDeepCaveBossDictionary.SafeRemove(Player);
                    StarSephiriaMod.LogInfoDungeon($"删除{Player}的数据");
                }
            }
        }
        public static Dictionary<string, GameObject> AllBossList = new Dictionary<string, GameObject>();

        [HarmonyPatch(typeof(DungeonManager))]
        public static class DungeonManager_Patch
        {
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
                if (!__instance.isServer) return;
                if (!StarSephiriaMod.PartyStats.Update) return;

                StarSephiriaMod.PartyStats.Value = Tools.GetMaxPlayerStatus();
                foreach (PlayerSpawner Spawner in PlayerSpawner.MultiplayerList.ToList())
                {
                    if (Spawner.PlayerAvatar == null) continue;
                    UnitAvatar_Patches.UpdateHighestElementalBonus_Prefix(Spawner.PlayerAvatar);
                }
                StarSephiriaMod.PartyStats.Update = false;
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

                Dictionary<string, string> FloorKey = new Dictionary<string, string>();
                Dictionary<string, string> FloorNameToKey = new Dictionary<string, string>();
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
                    HashSet<string> UseCheckFloor = new HashSet<string>()
                    {
                        MolelandRestFloor,
                        GrasslandRestFloor,
                        GrasslandMiniBossToBossFloor,
                        GrasslandMiniBossToBossFloor2,
                        LibraryRestFloor,
                        DeepCaveRestFloor
                    };
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
                foreach (var spawner in PlayerSpawner.MultiplayerList)
                {
                    var Player = spawner.PlayerAvatar;
                    if (Player == null || Player.IsDead) continue;

                    TeleportPlayerList.Add(Player);
                    Dungeon.MoveFloor(Player, floorGuid, spawnPoint, 0);
                }
                AllTeleport = null;
                yield break;
            }

            private static bool AddBossToFirstFloorTeleport(DungeonManager __instance, PlayerAvatar avatar, ref string floorGuid, ref string spawnPoint)
            {
                try
                {
                    if (!__instance.isServer) return true;

                    Dictionary<string, string> FloorKey = new Dictionary<string, string>();
                    Dictionary<string, string> FloorNameToKey = new Dictionary<string, string>();
                    // 简化楼层查找逻辑
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

                    // Boss关传送逻辑
                    if(FloorKey.TryGetValue(floorGuid, out string FloorName) && avatar != null)
                    {
                        if (ModConfig.BossRushMode)
                        {
                            if (PlayerSpawner.MultiplayerList.Count != DefeatMoleElite.Count)
                            {
                                if (PlayerSpawner.MultiplayerList.Count > DefeatMoleElite.Count)
                                    PlayerAddInBossRushMod();
                                else
                                    PlayerReduceInBossRushMod();
                            }

                            var MoleList = DefeatMolelandBossDictionary;
                            var GrassList = DefeatGrasslandBossDictionary;
                            var Library = DefeatLibraryBossDictionary;
                            var DeepCave = DefeatDeepCaveBossDictionary;
                            if (MoleList.Count + GrassList.Count + Library.Count + DeepCave.Count <= 0)
                            {
                                StarSephiriaMod.LogInfoDungeon($"Boss列表数据异常，跳过BossRush传送逻辑");
                                return true;
                            }

                            if (DefeatMoleElite.TryGetValue(avatar.Name, out bool falg) && !falg)
                            {
                                if (DefeatMolelandBossList.Contains(FloorName))
                                {
                                    DefeatMoleElite[avatar.Name] = true;
                                    if (NetworkServer.connections.Count <= 1)
                                        SaveManager.CurrentRun.SetBool("[Star's Sephiria Mod]BossRush_DefeatMoleElite", true);
                                    if (FloorNameToKey.TryGetValue(MolelandBOSS2Floor, out var targetGuid)) floorGuid = targetGuid;
                                }
                            }
                            else if (MoleList.TryGetValue(avatar.Name, out var MoleBossList) && MoleBossList.Contains(FloorName))
                            {
                                MoleBossList.Remove(FloorName);
                                if (NetworkServer.connections.Count <= 1)
                                    SaveManager.CurrentRun.SetBool($"[Star's Sephiria Mod]BossRush_DefeatMole_{FloorName}", true);
                            }

                            if (FloorName == MolelandRestFloor)
                            {
                                if (UseSpawnerMoleElite != true)
                                {
                                    UseSpawnerMoleElite = true;
                                    if (NetworkServer.connections.Count <= 1)
                                        SaveManager.CurrentRun.SetBool("[Star's Sephiria Mod]BossRush_MoleList_MoleElite", true);
                                }
                                if (MoleList.TryGetValue(avatar.Name, out var MoleBossList) && MoleBossList.Count > 0)
                                {
                                    string To = MoleList[avatar.Name][0];
                                    MoleList[avatar.Name].Remove(To);
                                    if (FloorNameToKey.TryGetValue(To, out var targetGuid)) floorGuid = targetGuid;
                                    if (NetworkServer.connections.Count <= 1)
                                        SaveManager.CurrentRun.SetBool($"[Star's Sephiria Mod]BossRush_DefeatMole_{To}", true);
                                    if (avatar.isServer) avatar.HealPercent(60f);
                                }
                            }

                            if(GrassList.TryGetValue(avatar.Name, out var GrassBossList) && GrassBossList.Contains(FloorName))
                            {
                                GrassBossList.Remove(FloorName);
                                if (NetworkServer.connections.Count <= 1)
                                    SaveManager.CurrentRun.SetBool($"[Star's Sephiria Mod]BossRush_DefeatGrass_{FloorName}", true);
                            }
                            bool BossFloor = (FloorName == GrasslandRestFloor && GrassList.TryGetValue(avatar.Name, out var GrassBossList2) && GrassBossList2.Count > 0);
                            if (FloorName == GrasslandMiniBossToBossFloor || FloorName == GrasslandMiniBossToBossFloor2 || BossFloor)
                            {
                                if (!GrassList.TryGetValue(avatar.Name, out var GrassBossList3) || GrassBossList3.Count == 0)
                                    return true;

                                int idx = Mathf.FloorToInt(Seed * GrassBossList3.Count);
                                if (idx >= GrassBossList3.Count) idx = GrassBossList3.Count - 1;

                                string To = GrassBossList3[idx];
                                GrassBossList3.Remove(To);
                                if (FloorNameToKey.TryGetValue(To, out var targetGuid)) floorGuid = targetGuid;
                                if (NetworkServer.connections.Count <= 1)
                                    SaveManager.CurrentRun.SetBool($"[Star's Sephiria Mod]BossRush_DefeatGrass_{To}", true);
                            }

                            if (Library.TryGetValue(avatar.Name, out var LibBossList) && LibBossList.Contains(FloorName))
                            {
                                LibBossList.Remove(FloorName);
                                if (NetworkServer.connections.Count <= 1)
                                    SaveManager.CurrentRun.SetBool($"[Star's Sephiria Mod]BossRush_DefeatLibrary_{FloorName}", true);
                            }
                            if (FloorName == LibraryRestFloor && Library.TryGetValue(avatar.Name, out var LibBossList2) && LibBossList2.Count > 0)
                            {
                                int idx = Mathf.FloorToInt(Seed * LibBossList2.Count);
                                if (idx >= LibBossList2.Count) idx = LibBossList2.Count - 1;
                                string To = LibBossList2[idx];
                                LibBossList2.Remove(To);
                                if (FloorNameToKey.TryGetValue(To, out var targetGuid)) floorGuid = targetGuid;
                                if (NetworkServer.connections.Count <= 1)
                                    SaveManager.CurrentRun.SetBool($"[Star's Sephiria Mod]BossRush_DefeatLibrary_{To}", true);
                                if (To == LibraryBoss1Floor && spawnPoint == "FLOORSTARTING2") spawnPoint = "FLOORSTARTING";
                            }

                            if (DeepCave.TryGetValue(avatar.Name, out var CaveBossList) && CaveBossList.Contains(FloorName))
                            {
                                LastFloor = true;
                                if (NetworkServer.connections.Count <= 1)
                                    SaveManager.CurrentRun.SetBool("[Star's Sephiria Mod]BossRush_LastFloor", true);

                                CaveBossList.Remove(FloorName);
                                if (NetworkServer.connections.Count <= 1)
                                    SaveManager.CurrentRun.SetBool($"[Star's Sephiria Mod]BossRush_DefeatDeepCave_{FloorName}", true);
                            }
                            if (FloorName == DeepCaveRestFloor && DeepCave.TryGetValue(avatar.Name, out var CaveBossList2) && CaveBossList2.Count > 0)
                            {
                                string To = CaveBossList2[0];
                                CaveBossList2.Remove(To);
                                if (FloorNameToKey.TryGetValue(To, out var targetGuid)) floorGuid = targetGuid;
                                if (NetworkServer.connections.Count <= 1)
                                    SaveManager.CurrentRun.SetBool($"[Star's Sephiria Mod]BossRush_DefeatDeepCave_{To}", true);
                            }
                            if (NoAddExp.TryGetValue(avatar.Name, out List<string> Floor))
                            {
                                FloorKey.TryGetValue(floorGuid, out FloorName);
                                if (!Floor.Contains(FloorName))
                                {
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
                                    NoAddExp[avatar.Name].Add(FloorName);
                                }
                            }
                            else NoAddExp.Add(avatar.Name, new List<string>() { FloorName });
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
                StarSephiriaMod.CostumeSkinDate.SafeRemove(name);
                PlayerAvatar? RemovePlayer = null;
                foreach(PlayerAvatar Player in StarSephiriaPatches.DPSData.Keys)
                {
                    if (Player.Name == name)
                    {
                        RemovePlayer = Player;
                        break;
                    }
                }
                if(RemovePlayer != null)
                    StarSephiriaPatches.DPSData.SafeRemove(RemovePlayer);
                StarSephiriaMod.InitializeModStat(name);
                StarSephiriaMod.LogInfo($"删除{name}的各项数据记录");
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

                    Dictionary<EFloorThreatType, List<FloorGenerator>> dictionary = new Dictionary<EFloorThreatType, List<FloorGenerator>>();
                    Dictionary<EFloorThreatType, Dictionary<EFloorMainEventType, List<FloorGenerator>>> dictionary2 = new Dictionary<EFloorThreatType, Dictionary<EFloorMainEventType, List<FloorGenerator>>>();

                    foreach (object obj in Enum.GetValues(typeof(EFloorThreatType)))
                    {
                        EFloorThreatType efloorThreatType = (EFloorThreatType)obj;
                        dictionary[efloorThreatType] = new List<FloorGenerator>();
                        dictionary2[efloorThreatType] = new Dictionary<EFloorMainEventType, List<FloorGenerator>>();
                        foreach (object obj2 in Enum.GetValues(typeof(EFloorMainEventType)))
                        {
                            EFloorMainEventType efloorMainEventType = (EFloorMainEventType)obj2;
                            dictionary2[efloorThreatType][efloorMainEventType] = new List<FloorGenerator>();
                        }
                    }

                    // BOSS楼层生成器专用列表
                    List<FloorGenerator> BossList = new List<FloorGenerator>();

                    // 分类随机事件楼层预置体
                    GameObject[] array = __instance.randomEventFloorPrefabs;
                    int i;
                    for (i = 0; i < array.Length; i++)
                    {
                        FloorGenerator component = array[i].GetComponent<FloorGenerator>();
                        dictionary[component.floorThreatType].Add(component);
                        //StarSephiriaMod.LogInfoDungeon($"—输出随机层列表：{component.floorThreatType}");
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
                        //StarSephiriaMod.LogInfoDungeon($"—输出固定层列表：{Cpmponent.floorThreatType} ： {Cpmponent.floorMainEventType}");
                    }

                    int num = 0;
                    Random rngChoice = new Random(seed);// 用于事件/节点选择
                    Random rngSeed = new Random(seed + 1);// 用于生成楼层 seed
                    Random rngPos = new Random(seed + 10);// 用于位置偏移
                    Random rngDisturb = new Random(seed + 20);// 用于干扰/扰动
                    Random rngMerchant = new Random(seed + 99);// 用于商店
                    Random rngHidden = new Random(seed + 1000);// 用于隐藏房
                    Random rngHardBoss = new Random(seed + 20);// 你原来的 Boss 硬化随机
                    // 创建第一个楼层（起始点）
                    FloorData floorData = new FloorData
                    {
                        guid = Guid.NewGuid().ToString(),
                        name = __instance.firstFloor.name,
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

                    List<FloorData> 生成列表 = new List<FloorData> { floorData };

                    // 创建标准主要事件类型列表
                    List<EFloorMainEventType> list5 = new List<EFloorMainEventType>();
                    EFloorMainEventType[] possibleMainEvents = __instance.unknownNormalEvents;
                    for (i = 0; i < possibleMainEvents.Length; i++)
                    {
                        list5.Add(possibleMainEvents[i]);
                    }

                    // 初始化唯一楼层生成器字典
                    Dictionary<EFloorMainEventType, List<FloorGenerator>> dictionary3 = new Dictionary<EFloorMainEventType, List<FloorGenerator>>();
                    foreach (FloorGenerator floorGenerator in __instance.uniqueFloorPrefabs)
                    {
                        if (!dictionary3.ContainsKey(floorGenerator.floorMainEventType))
                        {
                            dictionary3[floorGenerator.floorMainEventType] = new List<FloorGenerator>();
                        }
                        dictionary3[floorGenerator.floorMainEventType].Add(floorGenerator);
                        //StarSephiriaMod.LogInfoDungeon($"—输出唯一楼层列表：{floorGenerator.floorMainEventType}");
                    }

                    // 步骤数组
                    List<FloorData>[] array3 = new List<FloorData>[__instance.steps.Length + 1];
                    for (int j = 0; j < array3.Length; j++)
                    {
                        array3[j] = new List<FloorData>();
                    }
                    array3[0].Add(floorData);

                    // 遍历每个步骤生成楼层
                    int stepsLength = __instance.steps.Length;
                    int ActualLevels = 0;
                    List<int> LayerMapping = new List<int>();
                    List<FloorData> hiddenCandidates = new List<FloorData>();
                    for (int Stack = 0; Stack < stepsLength; Stack++)
                    {
                        int Last = stepsLength - 1;
                        if (Last <= 0)
                        {
                            Last = 1;
                        }
                        if (Stack == 0 || Stack == 1)
                        {
                            if (!createAnvil)
                                continue;
                        }
                        if (Stack == 2)
                        {
                            if (LastFloor)
                            {
                                LastFloor = false;
                                if (PlayerSpawner.MultiplayerList.Count <= 1) SaveManager.CurrentRun.SetBool("[Star's Sephiria Mod]BossRush_LastFloor", false);
                                continue;
                            }
                        }

                        LayerMapping.Add(ActualLevels);

                        // 计算当前步骤的难度
                        float num3 = Stack / (float)Last;
                        int num4 = Mathf.CeilToInt((__instance.lastDifficulty - 1 - __instance.firstDifficulty) * num3 + __instance.firstDifficulty) + 1;

                        int UseStack = 1;
                        int BossStack = BossList.Count;

                        // 过滤当前步骤可能的主要事件类型
                        List<EFloorMainEventType> MiniBossALLEvents = new List<EFloorMainEventType>();
                        possibleMainEvents = __instance.steps[Stack].possibleMainEvents;

                        for (i = 0; i < possibleMainEvents.Length; i++)
                        {
                            if (possibleMainEvents[i] == EFloorMainEventType.Anvil)
                                continue;

                            MiniBossALLEvents.Add(possibleMainEvents[i]);
                        }
                        // 创建节点类型列表
                        List<StageEntity_Choice.EFloorNodeType> EfloorList = new List<StageEntity_Choice.EFloorNodeType>();
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
                        List<StageEntity_Choice.EFloorNodeType> NecessaryStage = new List<StageEntity_Choice.EFloorNodeType>();
                        List<StageEntity_Choice.EFloorNodeType> OptionalNodes = new List<StageEntity_Choice.EFloorNodeType>();
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

                        List<float> Offset = new List<float>();
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
                                if (Stack == 0 || Stack == 1)
                                {
                                    if (createAnvil)
                                    {
                                        floorGenerator2 = dictionary3[EFloorMainEventType.Anvil].GetRandom(rngChoice);
                                        FloorEventType = EFloorMainEventType.Anvil;
                                    }
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
                                FloorData Data = new FloorData
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
                        Random hiddenRand = new Random(seed + 1000);
                        List<FloorData> pool = hiddenCandidates.ToList();
                        for (int t = 0; t < 1; t++)
                        {
                            int index = hiddenRand.Next(pool.Count);
                            FloorData Target = pool[index];
                            Target.hiddenRoomCount = 1;
                            pool.RemoveAt(index); // 防止重复选到同一个
                        }
                        pool = hiddenCandidates.ToList();
                        for (int t = 0; t < 2; t++)
                        {
                            int index = hiddenRand.Next(pool.Count);
                            FloorData Target = pool[index];
                            if ((bool)DungeonManager.Instance && DungeonManager.Instance.dungeonEnvironment.TryGetValue("RedMerchant", out var value) && value > 0)
                            {
                                Target.generateMerchantType = 2;
                            }
                            else
                            {
                                Target.generateMerchantType = 1;
                            }
                            if (t == 1) Target.hasTraveler = true;
                            pool.RemoveAt(index); // 防止重复选到同一个
                        }
                        pool = hiddenCandidates.ToList();
                        for (int t = 0; t < 2; t++)
                        {
                            int index = hiddenRand.Next(pool.Count);
                            FloorData Target = pool[index];
                            Target.randomRoomCount += t + 1;
                        }
                    }
                    // 建立楼层之间的连接关系
                    for (int a = 0; a < array3.Length - 1; a++)
                    {
                        if (a >= LayerMapping.Count) continue;
                        for (int b = 0; b < array3[a].Count; b++)
                        {
                            List<string> LineList = new List<string>();
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
                        FloorData FloorData = new FloorData
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
            [HarmonyPatch("GenerateStage")]
            [HarmonyPrefix]
            static bool GenerateStage_Prefix(StageEntity_GrasslandTown __instance, DungeonManager dungeon, int seed, int stageOrder, bool createMiracle, bool createAnvil, bool enhancedDisturbance, string bossGenerateParameter, int hardModeAllBoss, ref FloorData[] __result)
            {
                try
                {
                    if (!ModConfig.BossRushMode) return true;

                    int num = 0;
                    int num2 = 0;
                    Random random = new Random(seed);
                    Random random2 = new Random(seed);
                    Random random3 = new Random(seed);
                    Random random4 = new Random(seed);
                    FloorData floorData = new FloorData
                    {
                        guid = Guid.NewGuid().ToString(),
                        name = __instance.firstFloor.name,
                        stageName = __instance.name,
                        seed = random.Next(),
                        globalY = stageOrder,
                        globalX = num2++,
                        nodeProgress = num,
                        prefabAssetId = __instance.firstFloor.GetComponent<NetworkIdentity>().assetId,
                        hasTraveler = false,
                        difficulty = __instance.firstDifficulty,
                        mainEventType = __instance.firstFloor.floorMainEventType,
                        threatType = __instance.firstFloor.floorThreatType,
                        disturbance = ""
                    };
                    num++;
                    int num3 = 1;
                    List<FloorData> list = new List<FloorData> { floorData };
                    List<FloorData> list2 = new List<FloorData>();
                    List<FloorData> list3 = new List<FloorData>();
                    List<FloorData> list4 = new List<FloorData>();
                    List<EFloorMainEventType> list5 = new List<EFloorMainEventType>(__instance.mainEventTypes);
                    List<EFloorMainEventType> list6 = new List<EFloorMainEventType>(__instance.subEventTypes);
                    int num4 = __instance.eventCount - list5.Count;
                    for (int i = 0; i < num4; i++)
                    {
                        int num5 = random.Next(0, list6.Count);
                        list5.Add(list6[num5]);
                        list6.RemoveAt(num5);
                    }
                    int num6 = __instance.eventCount;
                    List<GrasslandTownEventEntity> list7 = new List<GrasslandTownEventEntity>();
                    List<GrasslandTownEventEntity> list8 = new List<GrasslandTownEventEntity>();
                    List<GrasslandTownEventEntity> list9 = new List<GrasslandTownEventEntity>();
                    foreach (GrasslandTownEventEntity grasslandTownEventEntity in __instance.nodeEvents)
                    {
                        if (grasslandTownEventEntity.isAnvilFirstReward)
                        {
                            list9.Add(grasslandTownEventEntity);
                        }
                        if (grasslandTownEventEntity.isEssential)
                        {
                            list7.Add(grasslandTownEventEntity);
                        }
                        else
                        {
                            list8.Add(grasslandTownEventEntity);
                        }
                    }
                    List<FloorData> list10 = new List<FloorData>();
                    List<FloorData> list11 = new List<FloorData>();
                    int num7 = 0;
                    for (int k = list5.Count - 1; k >= 0; k--)
                    {
                        EFloorMainEventType efloorMainEventType = list5[k];
                        if (efloorMainEventType == EFloorMainEventType.Anvil)
                        {
                            if (list9.Count == 0)
                            {
                                list5.RemoveAt(k);
                            }
                            else
                            {
                                int num8 = random.Next(0, list9.Count);
                                GrasslandTownEventEntity grasslandTownEventEntity2 = list9[num8];
                                int num9 = random.Next();
                                FloorGenerator floorPrefab = grasslandTownEventEntity2.floorPrefab;
                                FloorData floorData2 = new FloorData
                                {
                                    guid = Guid.NewGuid().ToString(),
                                    name = floorPrefab.name,
                                    stageName = __instance.name,
                                    seed = num9,
                                    globalX = num2++,
                                    globalY = stageOrder,
                                    nodeProgress = num3,
                                    prefabAssetId = floorPrefab.GetComponent<NetworkIdentity>().assetId,
                                    mainEventType = efloorMainEventType,
                                    threatType = grasslandTownEventEntity2.threatType,
                                    difficulty = random.Next(__instance.firstDifficulty, __instance.lastDifficulty + 1),
                                    nodeAnchoredPositionX = random3.Next(-15, 15),
                                    nodeAnchoredPositionY = num3 * -__instance.distanceBetweenNodes + random3.Next(-15, 15),
                                    disturbance = ((grasslandTownEventEntity2.threatType == EFloorThreatType.HardBattle) ? __instance.disturbanceList.GetRandom(random4) : ""),
                                    questBoardEventId = grasslandTownEventEntity2.id,
                                    isAnvilFirstReward = grasslandTownEventEntity2.isAnvilFirstReward
                                };
                                list10.Add(floorData2);
                                if (floorPrefab.ExplorationActivated)
                                {
                                    list2.Add(floorData2);
                                    list3.Add(floorData2);
                                    list4.Add(floorData2);
                                }
                                FloorGenerator random5 = __instance.townRewardFloors.GetRandom(random);
                                FloorData floorData3 = new FloorData
                                {
                                    guid = Guid.NewGuid().ToString(),
                                    name = random5.name,
                                    stageName = __instance.name,
                                    seed = num9,
                                    globalX = num2++,
                                    globalY = stageOrder,
                                    nodeProgress = num3,
                                    prefabAssetId = random5.GetComponent<NetworkIdentity>().assetId,
                                    mainEventType = efloorMainEventType,
                                    threatType = grasslandTownEventEntity2.threatType,
                                    difficulty = random.Next(__instance.firstDifficulty, __instance.lastDifficulty + 1),
                                    nodeAnchoredPositionX = random3.Next(-15, 15),
                                    nodeAnchoredPositionY = num3 * -__instance.distanceBetweenNodes + random3.Next(-15, 15),
                                    disturbance = "",
                                    isHidden = true,
                                    isAnvilFirstReward = grasslandTownEventEntity2.isAnvilFirstReward
                                };
                                list11.Add(floorData3);
                                floorData2.connectionToOtherFloors = new string[] { floorData3.guid };
                                num3++;
                                list7.Remove(grasslandTownEventEntity2);
                                list8.Remove(grasslandTownEventEntity2);
                                list5.RemoveAt(k);
                                num7++;
                            }
                        }
                    }
                    num6 -= num7;
                    foreach (GrasslandTownEventEntity grasslandTownEventEntity3 in list7)
                    {
                        int num10 = random.Next(0, list5.Count);
                        EFloorMainEventType efloorMainEventType2 = list5[num10];
                        list5.RemoveAt(num10);
                        int num11 = random.Next();
                        FloorGenerator floorPrefab2 = grasslandTownEventEntity3.floorPrefab;
                        FloorData floorData4 = new FloorData
                        {
                            guid = Guid.NewGuid().ToString(),
                            name = floorPrefab2.name,
                            stageName = __instance.name,
                            seed = num11,
                            globalX = num2++,
                            globalY = stageOrder,
                            nodeProgress = num3,
                            prefabAssetId = floorPrefab2.GetComponent<NetworkIdentity>().assetId,
                            mainEventType = efloorMainEventType2,
                            threatType = grasslandTownEventEntity3.threatType,
                            difficulty = random.Next(__instance.firstDifficulty, __instance.lastDifficulty + 1),
                            nodeAnchoredPositionX = random3.Next(-15, 15),
                            nodeAnchoredPositionY = num3 * -__instance.distanceBetweenNodes + random3.Next(-15, 15),
                            disturbance = ((grasslandTownEventEntity3.threatType == EFloorThreatType.HardBattle) ? __instance.disturbanceList.GetRandom(random4) : ""),
                            questBoardEventId = grasslandTownEventEntity3.id,
                            isAnvilFirstReward = grasslandTownEventEntity3.isAnvilFirstReward
                        };
                        list10.Add(floorData4);
                        if (floorPrefab2.ExplorationActivated)
                        {
                            list2.Add(floorData4);
                            list3.Add(floorData4);
                            list4.Add(floorData4);
                        }
                        FloorGenerator random6 = __instance.townRewardFloors.GetRandom(random);
                        FloorData floorData5 = new FloorData
                        {
                            guid = Guid.NewGuid().ToString(),
                            name = random6.name,
                            stageName = __instance.name,
                            seed = num11,
                            globalX = num2++,
                            globalY = stageOrder,
                            nodeProgress = num3,
                            prefabAssetId = random6.GetComponent<NetworkIdentity>().assetId,
                            mainEventType = efloorMainEventType2,
                            threatType = grasslandTownEventEntity3.threatType,
                            difficulty = random.Next(__instance.firstDifficulty, __instance.lastDifficulty + 1),
                            nodeAnchoredPositionX = random3.Next(-15, 15),
                            nodeAnchoredPositionY = num3 * -__instance.distanceBetweenNodes + random3.Next(-15, 15),
                            disturbance = "",
                            isHidden = true,
                            isAnvilFirstReward = grasslandTownEventEntity3.isAnvilFirstReward
                        };
                        list11.Add(floorData5);
                        floorData4.connectionToOtherFloors = new string[] { floorData5.guid };
                        num3++;
                    }

                    num6 -= list7.Count;
                    for (int l = 0; l < num6; l++)
                    {
                        int num12 = random.Next(0, list5.Count);
                        EFloorMainEventType efloorMainEventType3 = list5[num12];
                        list5.RemoveAt(num12);
                        int num13 = random.Next(0, list8.Count);
                        GrasslandTownEventEntity grasslandTownEventEntity4 = list8[num13];
                        list8.RemoveAt(num13);
                        int num14 = random.Next();
                        FloorGenerator floorPrefab3 = grasslandTownEventEntity4.floorPrefab;
                        FloorData floorData6 = new FloorData
                        {
                            guid = Guid.NewGuid().ToString(),
                            name = floorPrefab3.name,
                            stageName = __instance.name,
                            seed = num14,
                            globalX = num2++,
                            globalY = stageOrder,
                            nodeProgress = num3,
                            prefabAssetId = floorPrefab3.GetComponent<NetworkIdentity>().assetId,
                            mainEventType = efloorMainEventType3,
                            threatType = grasslandTownEventEntity4.threatType,
                            difficulty = random.Next(__instance.firstDifficulty, __instance.lastDifficulty + 1),
                            nodeAnchoredPositionX = random3.Next(-15, 15),
                            nodeAnchoredPositionY = num3 * -__instance.distanceBetweenNodes + random3.Next(-15, 15),
                            disturbance = ((grasslandTownEventEntity4.threatType == EFloorThreatType.HardBattle) ? __instance.disturbanceList.GetRandom(random4) : ""),
                            questBoardEventId = grasslandTownEventEntity4.id,
                            isAnvilFirstReward = grasslandTownEventEntity4.isAnvilFirstReward
                        };
                        list10.Add(floorData6);
                        if (floorPrefab3.ExplorationActivated)
                        {
                            list2.Add(floorData6);
                            list3.Add(floorData6);
                            list4.Add(floorData6);
                        }
                        FloorGenerator random7 = __instance.townRewardFloors.GetRandom(random);
                        FloorData floorData7 = new FloorData
                        {
                            guid = Guid.NewGuid().ToString(),
                            name = random7.name,
                            stageName = __instance.name,
                            seed = num14,
                            globalX = num2++,
                            globalY = stageOrder,
                            nodeProgress = num3,
                            prefabAssetId = random7.GetComponent<NetworkIdentity>().assetId,
                            mainEventType = efloorMainEventType3,
                            threatType = grasslandTownEventEntity4.threatType,
                            difficulty = random.Next(__instance.firstDifficulty, __instance.lastDifficulty + 1),
                            nodeAnchoredPositionX = random3.Next(-15, 15),
                            nodeAnchoredPositionY = num3 * -__instance.distanceBetweenNodes + random3.Next(-15, 15),
                            disturbance = "",
                            isHidden = true,
                            isAnvilFirstReward = grasslandTownEventEntity4.isAnvilFirstReward
                        };
                        list11.Add(floorData7);
                        floorData6.connectionToOtherFloors = new string[] { floorData7.guid };
                        num3++;
                    }
                    Random random8 = new Random(seed + 97);
                    list10.Shuffle(random8);
                    list.AddRange(list10);
                    list.AddRange(list11);

                    List<FloorData> filteredList = new List<FloorData> { list[0] };
                    foreach (FloorData FData in list)
                    {
                        if (FData.threatType == EFloorThreatType.MiniBoss)
                        {
                            FData.mainEventType = EFloorMainEventType.EXP;
                            if ((bool)DungeonManager.Instance && DungeonManager.Instance.dungeonEnvironment.TryGetValue("RedMerchant", out var value) && value > 0)
                            {
                                FData.generateMerchantType = 2;
                            }
                            else
                            {
                                FData.generateMerchantType = 1;
                            }
                            filteredList.Add(FData);
                        }
                    }
                    list = filteredList;
                    foreach (FloorGenerator floorGenerator in __instance.essentialFloorPrefabs)
                    {
                        FloorData floorData8 = new FloorData
                        {
                            guid = Guid.NewGuid().ToString(),
                            name = floorGenerator.name,
                            stageName = __instance.name,
                            seed = random.Next(),
                            prefabAssetId = floorGenerator.GetComponent<NetworkIdentity>().assetId,
                            hasTraveler = false,
                            isHidden = true,
                            globalY = stageOrder,
                            globalX = num2++,
                            nodeProgress = -1,
                            nodeAnchoredPositionX = -1280f,
                            nodeAnchoredPositionY = 0f,
                            disturbance = ""
                        };
                        list.Add(floorData8);
                    }
                    int Stack = 0;
                    foreach (FloorGenerator BossFloors in __instance.bossFloors)
                    {
                        StarSephiriaMod.LogInfoDungeon(BossFloors.name);
                        FloorData floorData9 = new FloorData
                        {
                            guid = Guid.NewGuid().ToString(),
                            name = BossFloors.name,
                            stageName = __instance.name,
                            seed = random.Next(),
                            globalY = stageOrder,
                            globalX = num2++,
                            nodeProgress = num,
                            prefabAssetId = BossFloors.GetComponent<NetworkIdentity>().assetId,
                            hasTraveler = false,
                            difficulty = __instance.lastDifficulty,
                            mainEventType = BossFloors.floorMainEventType,
                            threatType = BossFloors.floorThreatType,
                            nodeAnchoredPositionX = 0,
                            nodeAnchoredPositionY = num * -__instance.distanceBetweenNodes + random3.Next(-15, 15),
                            disturbance = ""
                        };
                        list.Add(floorData9);
                        Stack += 1;
                    }
                    __result = list.ToArray();
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[StageEntity_Choice]自定义生成失败: {ex}");
                    return true;
                }
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
                    StarSephiriaPatches.UseDPS = true;
                    if (PlayerSpawner.MultiplayerList.Count <= 1)
                    {
                        PlayerAvatar Player = PlayerSpawner.MultiplayerList[0].PlayerAvatar;
                        StarSephiriaPatches.NoHitCount.Add(Player, new NoHitRecord(Player.GetCustomStatUnsafe(TextFile.CurrentRosion)));
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