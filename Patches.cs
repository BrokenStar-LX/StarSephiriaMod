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
    [HarmonyPatch]
    public static class StarSephiriaPatches
    {
        public static readonly FieldInfo OnEvadeField = AccessTools.Field(typeof(UnitAvatar), "OnEvade");
        public static readonly FieldInfo OnCounterField = AccessTools.Field(typeof(UnitAvatar), "OnCounter");
        public static readonly FieldInfo OnParryField = AccessTools.Field(typeof(UnitAvatar), "OnParry");
        public static readonly FieldInfo OnGuardSucceededField = AccessTools.Field(typeof(UnitAvatar), "OnGuardSucceeded");
        public static readonly FieldInfo OnHpChangedServersideField = AccessTools.Field(typeof(UnitAvatar), "OnHpChangedServerside");
        public static readonly FieldInfo isDodgeInvincibleAppliedField = AccessTools.Field(typeof(UnitAvatar), "isDodgeInvincibleApplied");
        public static readonly FieldInfo curShieldDatasField = AccessTools.Field(typeof(UnitAvatar), "curShieldDatas");
        public static readonly FieldInfo OnCostumeChangedClientsideField = AccessTools.Field(typeof(PlayerAvatar), "OnCostumeChangedClientside");
        public static readonly MethodInfo RegisterInternalMethod = AccessTools.Method(typeof(MiracleDatabase), "RegisterInternal");

        public static bool CustomStatsMessage = false;
        public static bool DisplayCustomStats = false;
        public static Dictionary<string, string> DisplayCustomStatsValue = new Dictionary<string, string>();
        public static bool ItemMessage = false;
        public static bool MonsterMessage = false;
        private static readonly Dictionary<UnitAvatar, float> AttackTimers = new Dictionary<UnitAvatar, float>();
        private static readonly Dictionary<string, Timer> BatTime = new Dictionary<string, Timer>();
        public static List<string> DefeatBoss = new List<string> { };
        public static List<(string, float, int, int)> Monster = new List<(string, float, int, int)> { };
        public static Dictionary<string, GameObject> Projectile = new Dictionary<string, GameObject>();
        public static Dictionary<string, bool> UseCostomEffect = new Dictionary<string, bool>();
        public static Dictionary<string, int> GoldObtained = new Dictionary<string, int>();
        public static Dictionary<string, float> ColdAngerTime = new Dictionary<string, float>();
        public static Dictionary<string, float> HeatedBladeEffect = new Dictionary<string, float>();
        public static Dictionary<string, int> HeatedBladeStack = new Dictionary<string, int>();
        public static Dictionary<UnitAvatar, Timer> SolisImberEffect = new Dictionary<UnitAvatar, Timer>();
        public static Dictionary<UnitAvatar, int> SolisIubarEffect = new Dictionary<UnitAvatar, int>();
        public static Dictionary<UnitAvatar, int> SolisIubarAttackSpeed = new Dictionary<UnitAvatar, int>();
        public static Dictionary<UnitAvatar, Timer> Br2RecoveryLife = new Dictionary<UnitAvatar, Timer>();
        public static Dictionary<UnitAvatar, float> RosionTime = new Dictionary<UnitAvatar, float>();
        public static List<PlayerAvatar> HolyRabbit2List = new List<PlayerAvatar>();
        public static HashSet<UnitAvatar> GrippingFearBuff = new HashSet<UnitAvatar>();
        public static List<string> DefenseToAllDamage = new List<string>();
        public static int UpCharmStack = 0;
        public static Dictionary<UnitAvatar, float> RecordQQBossDamage = new Dictionary<UnitAvatar, float>();
        public static Dictionary<UnitAvatar, float> UseQQBossDamage = new Dictionary<UnitAvatar, float>();
        public static Dictionary<UnitAvatar, PollutedBloodData> PollutedBloodRecovery = new Dictionary<UnitAvatar, PollutedBloodData>();
        public static Dictionary<UnitAvatar, CountingTimer> EvilGuardianEffect = new Dictionary<UnitAvatar, CountingTimer>();
        public static Dictionary<UnitAvatar, Counter> GuardDeBuff = new Dictionary<UnitAvatar, Counter>();
        public static Dictionary<UnitAvatar, NoHitRecord> NoHitCount = new Dictionary<UnitAvatar, NoHitRecord>();
        public static Dictionary<UnitAvatar, Timer> StrikeBackTime = new Dictionary<UnitAvatar, Timer>();
        public static Dictionary<UnitAvatar, int> PurpleJacketStack = new Dictionary<UnitAvatar, int>();
        public static HashSet<UnitAvatar> RosionList = new HashSet<UnitAvatar>();

        public static List<CloudAttackData> ModCloudAttack = new List<CloudAttackData>();

        public static bool UseDPS = false;
        public static Dictionary<PlayerAvatar, DPSRecord> DPSData = new Dictionary<PlayerAvatar, DPSRecord>();

        public static event Action? OnUpdateHighestElemental = null;

        [HarmonyPatch(typeof(UnitAvatar))]
        public static class UnitAvatar_Patches
        {
            public static FieldInfo invulnerableCountField = AccessTools.Field(typeof(UnitAvatar), "invulnerableCount");
            public static FieldInfo isPerfectGuardAvailableField = AccessTools.Field(typeof(UnitAvatar), "isPerfectGuardAvailable");
            public static FieldInfo guardStartTimerField = AccessTools.Field(typeof(UnitAvatar), "guardStartTimer");
            public static FieldInfo perfectGuardLatencyBonusField = AccessTools.Field(typeof(UnitAvatar), "perfectGuardLatencyBonus");

            private static readonly List<string> NoNpcER = new List<string>()
            {
                "SlimeOrange",
                "SlimeBlue"
            };

            [HarmonyPatch("get_MaxHp")]
            [HarmonyPostfix]
            static void GetMaxHp_Postfix(UnitAvatar __instance, ref float __result)
            {
                if (ModConfig.HardMode < 3) return;
                if (__result >= 50)
                {
                    if (__instance.IsWeapon(WeaponIDs.BloodlettingGearblade) && __instance.GetCustomStatUnsafe(TextFile.ReceivedDamageReduction) > 0) return;
                }

                int Value = __instance.GetRosionStack();
                if (Value > 0)
                {
                    float Mult = 1f - Value * 0.01f;
                    __result *= Mult;
                    if (__instance.hp > __result) __instance.hp = __result;
                    if (Value >= 100) __instance.Die(1, null);
                }
            }

            [HarmonyPatch("OnStartServer")]
            [HarmonyPostfix]
            public static void OnStartServer_Postfix(UnitAvatar __instance)
            {
                __instance.OnAddedDebuff += OnAddNewDebuff;
                __instance.OnAddedDebuffOnTarget += OnAddBuff;
                __instance.OnAttackUnitBeforeOperation += OnModifyAttack;
                __instance.OnAttackUnitBeforeOperation_AsLeader += OnModifyAttack_AsLeader;
                __instance.OnEvade += OnEvade;
                __instance.OnCounter += OnCounter;
                __instance.OnParry += OnParry;
                __instance.OnGuardSucceeded += OnGuard;
                if (__instance.TryGetComponent<ItemController>(out var itemController))
                {
                    itemController.OnDrinkPotionServerside += OnDrinkPotion;
                }
            }

            private static void OnAddNewDebuff(CharacterDebuff DeBuff)
            {

            }

            private static void OnAddBuff(CharacterDebuff DeBuff, string DeBuffName)
            {
                if (DeBuff.NetworkAttacker is PlayerAvatar Player)
                {
                    if (DeBuff.GetMatchDebuffType(Player, typeof(CharacterDebuff_Burn)))
                    {
                        if (Player.IsWeapon(WeaponIDs.HeatedBlade))
                        {
                            if (!HeatedBladeEffect.TryGetValue(Player.Name, out float Mult) && Mult <= 0f) return;
                            if (CharmPatch.Charm_FlamePlantRoot_Patch.FlamePlant == null) return;
                            if (HeatedBladeStack.ContainsKey(Player.Name))
                            {
                                HeatedBladeStack[Player.Name] += 1;
                                if (HeatedBladeStack[Player.Name] >= 5)
                                {
                                    GameObject Bullt = CharmPatch.Charm_FlamePlantRoot_Patch.FlamePlant;
                                    string Id = CharmPatch.Charm_FlamePlantRoot_Patch.DamageId;
                                    Vector3 Pos = DeBuff.Target.transform.position;
                                    Mult *= 1f + Player.GetCustomStatUnsafe(TextFile.BurnDamage) * 0.01f;
                                    Mult *= 1f + Player.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.01f;
                                    Mult *= 1f + Player.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 0.01f;
                                    if (Player.GetCustomStatUnsafe(TextFile.BurnEvo) > 0)
                                        Mult *= 28f / 18f;
                                    Mult *= 1f + Player.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                                    Mult *= 1f + Player.GetCustomStatUnsafe(TextFile.SpecialAttackDamageBonus) * 0.01f;

                                    Bullet.Pool.Spawn(Bullt, Pos, true, EDamageFromType.None, Id, Mult * 0.75f, 0, 0f, Player, Player.GetHostileFactionLayers(EDamageFromType.None), 10, Pos, Pos + Vector3.down, null, null, 0f, EDamageElementalType.Fire, true);
                                    HeatedBladeStack[Player.Name] -= 5;
                                    HeatedBladeEffect[Player.Name] = 0f;
                                }
                            }
                            else HeatedBladeStack[Player.Name] = 1;
                        }
                    }
                    if (DeBuff.GetMatchDebuffType(Player, typeof(CharacterDebuff_Electric)))
                    {
                        if (Player.IsWeaponUseNet(WeaponIDs.LightningsWrath))
                        {
                            if (!WeaponPatch.LightningsWrathEffect2.ContainsKey(Player))
                            {
                                if (!WeaponPatch.LightningsWrathEffect.ContainsKey(Player))
                                    WeaponPatch.LightningsWrathEffect[Player] = 1;
                                else if (WeaponPatch.LightningsWrathEffect[Player] < 40)
                                {
                                    WeaponPatch.LightningsWrathEffect[Player] += 1;
                                }
                            }
                        }
                        if (Player.IsWeaponUseNet(WeaponIDs.LightningGreatSwordS3G))
                        {
                            if (UnityEngine.Random.Range(0, 100) < Player.GetCustomStatUnsafe(TextFile.ElectricLuck) * 0.5f)
                            {
                                float BaseDamage = Player.GetCustomStatUnsafe(TextFile.LightningDamage) * 2.4f;
                                BaseDamage *= 1f + Player.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.01f;
                                BaseDamage *= 1f + Player.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 0.01f;
                                BaseDamage *= 1f + Player.GetCustomStatUnsafe(TextFile.DebuffDuration) * 0.01f;
                                BaseDamage *= 1f + Player.GetCustomStatUnsafe(TextFile.ElectricDamage) * 0.01f;
                                if (Tools.GetPro("LightningTouch", out var Pro))
                                {
                                    EDamageElementalType Elemental = EDamageElementalType.Lightning;
                                    Vector3 Pos = DeBuff.NetworkTarget.transform.position;
                                    string DamageID = "[Star's Sephiria Mod]落雷";
                                    Bullet.Pool.Spawn(Pro, Pos, true, EDamageFromType.None, DamageID, BaseDamage, 25, 2f, Player, Player.GetHostileFactionLayers(EDamageFromType.None), 0.3f, Pos, Pos, null, null, 0f, Elemental, false);
                                }
                            }
                        }
                        if (Player.IsWeaponUseNet(WeaponIDs.MarkStabilization))
                        {
                            if (WeaponPatch.MarkStabilizationEffect.ContainsKey(Player))
                            {
                                WeaponPatch.MarkStabilizationEffect[Player] += 1;
                                if (WeaponPatch.MarkStabilizationEffect[Player] >= 15)
                                    WeaponPatch.MarkStabilizationEffect[Player] = 15;
                                Player.SetEffectHUDValue("MarkStabilizationCharge", WeaponPatch.MarkStabilizationEffect[Player].ToString());
                            }
                            else
                            {
                                WeaponPatch.MarkStabilizationEffect[Player] = 1;
                                Player.CreateEffectHUD("Charge", "MarkStabilizationCharge");
                            }
                        }
                    }
                    if (DeBuff.GetMatchDebuffType(Player, typeof(CharacterDebuff_Freeze)))
                    {
                        if (Player.IsWeapon(WeaponIDs.IcicleSword))
                            ShootIceBullet(DeBuff.NetworkTarget, Player, 5);
                    }
                    if (Player.IsWeapon(WeaponIDs.PurpleJacket))
                    {
                        if (PurpleJacketStack.TryGetValue(Player, out int Value))
                        {
                            PurpleJacketStack[Player] += 1;
                            if (PurpleJacketStack[Player] >= 5)
                            {
                                PurpleJacketEffect(Player, DeBuff.NetworkTarget);
                                PurpleJacketStack[Player] -= 5;
                            }
                        }
                        else PurpleJacketStack[Player] = 1;
                    }//紫甲毒蜂
                }
            }

            private static void PurpleJacketEffect(UnitAvatar Player, UnitAvatar Target)
            {
                int Stack = 0;
                foreach (CharacterDebuff Debuff in Target.Debuffs)
                {
                    if (Debuff.NetworkAttacker == Player)
                    {
                        Stack += Math.Min((int)Debuff.CurrentStack, 10);
                    }
                }
                float Damage = 40 + Tools.BaseStatToMax(Player) * 2f;
                Damage *= 1f + Stack * 0.1f;
                Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                DamageInstance damage = DamageInstance.GetDamage(Player, "[Star's Sephiria Mod]毒爆", Target.transform.position, Player.GetHostileFactionLayers(EDamageFromType.None), Damage, EDamageType.ElementalEffectDamage, EDamageFromType.None, Vector2.zero, 0, 0f);
                damage.elementalType = EDamageElementalType.Chaos;
                damage.SetCustomColor(true, new Color(0.72f, 0.44f, 0.82f, 1f));
                Target.ApplyDamage(damage);
            }

            private static void OnDrinkPotion(WieldingPotion potion)
            {
                ItemController ICont = potion.Controller;
                WieldingPotion? wieldingPotion = ICont.NetworkcurrentWieldingItem as WieldingPotion;
                if (wieldingPotion != null)
                {
                    ItemPosition itemPosition = ICont.Avatar.Inventory.IdxToPos(ICont.quickSlotTable[ICont.SelectedQuickSlotIdx].idx);
                    NewItemOwnInstance Item = ICont.Avatar.Inventory.FindItem(itemPosition);
                    if (Item != null)
                    {
                        if (Item.EntityID == ICont.NetworkcurrentWieldingItem.entityID)
                        {
                            if (Item.EntityID == 38 && ICont.Avatar.IsInBattle)//觉醒药水
                                return;
                            if (Item.Entity.rarity == EItemRarity.Common)
                                ICont.Avatar.AddOrphanedStatusInstance(StatusDatabase.CreateStatusEntity("DRINK_POTION_STACK/1"));
                            if (Item.Entity.rarity == EItemRarity.Uncommon)
                                ICont.Avatar.AddOrphanedStatusInstance(StatusDatabase.CreateStatusEntity("DRINK_POTION_STACK/2"));
                            if (Item.Entity.rarity == EItemRarity.Rare)
                                ICont.Avatar.AddOrphanedStatusInstance(StatusDatabase.CreateStatusEntity("DRINK_POTION_STACK/4"));
                            if (Item.Entity.rarity == EItemRarity.Legend)
                                ICont.Avatar.AddOrphanedStatusInstance(StatusDatabase.CreateStatusEntity("DRINK_POTION_STACK/6"));
                        }
                    }
                }
            }

            private static void OnHit(DamageInstance Damage)
            {
                if (Damage.origin is UnitAvatar NPC)
                {
                    if (Damage.victim is PlayerAvatar Player)
                    {
                        if (NPC.monsterType == EMonsterType.Boss && Damage.damage > 0)
                        {
                            if (!RosionTime.ContainsKey(Player) || RosionTime.TryGetValue(Player, out float Time) && Time <= 0f)
                            {
                                Player.AddOrphanedStatusInstance(StatusDatabase.CreateStatusEntity("CURRENT_ROSION/1"));
                                if (RosionList.Contains(Player)) Player.SetEffectHUDValue("CurrentRosion", Player.GetRosionStack().ToString());
                                RosionTime[Player] = 0.75f;
                            }
                        }
                    }
                }
            }

            private static void OnModifyAttack(UnitAvatar Target, DamageInstance Damage)
            {
                if (ModConfig.Instance.EnableWeaponModifier && Damage.id == "Laser" && Damage.elementalType == EDamageElementalType.Normal)
                {
                    Damage.elementalType = EDamageElementalType.Chaos;
                }
                if (Damage.origin is PlayerAvatar Player)
                {
                    if (Hard >= 4) Damage.damage *= 0.8f;

                    if (Player.IsWeapon(WeaponIDs.SolisImber))
                    {
                        if (Damage.id == "Ability_FlameSword")
                        {
                            Damage.damage *= 1f + Player.GetCustomStatUnsafe(TextFile.SpecialAttackDamageBonus) * 0.01f;
                            Damage.damage *= 1f + Player.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                        }
                    }
                    if (Tools.CostumeSkinUse(Player.Name, "ForestCat_3"))
                    {
                        float Mult = Player.currentMoney / 400f;
                        Damage.damage *= 1f + Mult * 0.01f;
                    }
                    if (Tools.CostumeSkinUse(Player.Name, "MandarinRabbit_3"))
                    {
                        Damage.damage *= 0.8f;
                    }
                    if (Tools.CostumeSkinUse(Player.Name, "RedHolyRabbit_2"))
                    {
                        float Mult = (Player.MaxMp - Player.MP) / 20f;
                        Damage.damage *= 1f + Mult * 0.01f;
                    }
                    if (ModConfig.Instance.ModifyCharm)
                    {
                        if (Damage.id == "Debuff_Burn")
                        {
                            if (Player.GetCustomStatUnsafe(TextFile.BlueBurnChange) > 0)
                            {
                                if (UnityEngine.Random.Range(0, 100) <= 20)
                                    Target.ApplyDebuff(CombatManager.Instance.frostbiteDebuffPrefab, Player);
                            }
                        }
                    }
                }
                else if (Damage.origin is UnitAvatar Orig)
                {
                    if (Orig.Leader is PlayerAvatar Leader)
                    {
                        if (Hard >= 4) Damage.damage *= 0.8f;

                        if (Leader.IsWeapon(WeaponIDs.DarkVeil))
                        {
                            if (WeaponPatch.DarkVeilBuff.TryGetValue(Leader, out var Buff) && !Buff.IsEndBuff)
                            {
                                int Count = Buff.CurrentStack;
                                float AddDamage = Leader.GetCustomStatUnsafe(TextFile.PhysicalDamage) * 0.8f;
                                AddDamage *= 1f + Leader.GetCustomStatUnsafe(TextFile.BasicAttackDamageBonus) * 0.01f;
                                AddDamage *= 1f + Leader.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;

                                Damage.damage += AddDamage * Count;
                                WeaponPatch.DarkVeilBuff.Remove(Leader);
                                Buff.RequestEnd();
                            }
                        }
                    }
                }
            }

            private static void OnModifyAttack_AsLeader(UnitAvatar Follower, UnitAvatar Target, DamageInstance Damage)
            {
                if (Follower.Leader is PlayerAvatar Player)
                {
                    string TName = Follower.name.Replace("(Clone)", "");
                    if (TName == "SamuraiDemon")
                    {
                        if (Player.FollowersCount > 1)
                            Damage.damage *= 0.5f;
                    }
                    if (Tools.CostumeSkinUse(Player.Name, "Mole_3", false))
                    {
                        string DamageId = "[Star's Sephiria Mod]国王力量";
                        if (Damage.flag != DamageId && Damage.damage > 0)
                        {
                            float AddDamage = Player.GetCustomStatUnsafe(TextFile.TrueDamage) * 0.5f;
                            var Dam = DamageInstance.GetDamage(Follower, DamageId, Target.transform.position, 4294967295L, AddDamage, EDamageType.Slice, EDamageFromType.None, Vector2.zero, 0, 0f);
                            Dam.ignoreDefense = 100;
                            Dam.flag = DamageId;
                            Target.ApplyDamage(Dam);
                        }
                    }//鼹鼠3
                    if (Tools.CostumeSkinUse(Player.Name, "Mole_2", false))
                    {
                        if (Player.FollowersCount <= 1)
                        {
                            Damage.damage *= 3f;
                        }
                    }//鼹鼠2 同伴攻击伤害乘算增加（仅当同伴数小于等于1时）
                }
            }

            private static void OnGuard(DamageInstance damage, bool Perfect)
            {
                string Message = Perfect ? "PerfectGuard" : "Guard";
                if (damage.victim is PlayerAvatar Player)
                {
                    if (!GuardDeBuff.ContainsKey(Player))
                    {
                        GuardDeBuff.Add(Player, new Counter(20, 3f));
                    }
                    else
                    {
                        if (Perfect)
                            GuardDeBuff[Player].Count += 1;
                        else
                            GuardDeBuff[Player].Count += 2;
                    }
                }
                OnBlockDamage(damage, Message);
            }

            private static void OnParry(DamageInstance damage)
            {
                OnBlockDamage(damage, "Parry");
            }

            private static void OnCounter(DamageInstance damage)
            {
                OnBlockDamage(damage, "Counter");
            }

            private static void OnEvade(DamageInstance damage)
            {
                string Message = "Evade";
                if (isDodgeInvincibleAppliedField != null)
                {
                    if (damage.victim is PlayerAvatar EPlayer)
                    {
                        NestedBoolean isDodgeInvincibleApplied = (NestedBoolean)isDodgeInvincibleAppliedField.GetValue(EPlayer);
                        if (isDodgeInvincibleApplied.IsTrue()) Message = "DashEvade";
                    }
                }
                if (damage.flag == "UseSpecialEvade")
                    Message = "SpecialEvade";
                OnBlockDamage(damage, Message);

                if (damage.victim is UnitAvatar Player)
                {
                    if (damage.origin is UnitAvatar NPC)
                    {
                        float Value = Player.GetCustomStatUnsafe(TextFile.StrikeBack) * 0.01f;
                        if (Value > 0 && !StrikeBackTime.ContainsKey(Player))
                        {
                            float Damage = Value * Player.GetCustomStatUnsafe(TextFile.Evasion) * 0.01f;
                            string Id = "[Star's Sephiria Mod]反击";
                            if (Damage > 0)
                            {
                                var Dam = DamageInstance.GetDamage(Player, Id, NPC.transform.position, 4294967295L, Damage, EDamageType.Slice, EDamageFromType.DirectAttack, Vector2.zero, 0, 0f);
                                NPC.ApplyDamage(Dam);
                            }
                            StrikeBackTime[Player] = new Timer(0.5f);
                        }
                    }
                }
            }

            private static void OnBlockDamage(DamageInstance damage, string Type)
            {
                if (damage.victim is PlayerAvatar Player)
                {
                    if (damage.origin is UnitAvatar NPC)
                    {
                        bool falg = (Type == "Guard" || Type == "Evade");
                        if(ModConfig.HardMode >= 4)
                        {
                            if (Type == "SpecialEvade" && UnityEngine.Random.Range(0, 100) < 33)
                                falg = true;
                        }
                        if (falg)
                        {
                            if (NPC.monsterType == EMonsterType.Boss && damage.damage > 0)
                            {
                                if (!RosionTime.ContainsKey(Player) || RosionTime.TryGetValue(Player, out float Time) && Time <= 0f)
                                {
                                    Player.AddOrphanedStatusInstance(StatusDatabase.CreateStatusEntity("CURRENT_ROSION/1"));
                                    if (RosionList.Contains(Player)) Player.SetEffectHUDValue("CurrentRosion", Player.GetRosionStack().ToString());
                                    RosionTime[Player] = 0.75f;
                                }
                            }
                        }
                    }
                    if (Player.IsWeaponUseNet(WeaponIDs.WinterSmithsDraftblade, WeaponIDs.SilenceofEverfrost, WeaponIDs.FragmentofNastrond))
                    {
                        float Time = 6f * (1f + Player.GetCustomStatUnsafe("BUFFDURATION") * 0.01f);
                        WeaponPatch.DraftbladeAttack[Player] = new Timer(Time, false);
                    }
                    if (Tools.CostumeSkinUse(Player.Name, "Otter_3", false))
                    {
                        float Time = 6f * (1f + Player.GetCustomStatUnsafe("BUFFDURATION") * 0.01f);
                        StarSephiriaMod.Otter3_AddCrit[Player] = new Timer(Time);
                    }
                }
            }

            [HarmonyPatch("OnStopServer")]
            [HarmonyPostfix]
            public static void OnStopServer_Postfix(UnitAvatar __instance)
            {
                __instance.OnAddedDebuffOnTarget -= OnAddBuff;
                __instance.OnAddedDebuff -= OnAddNewDebuff;
                __instance.OnAttackUnitBeforeOperation -= OnModifyAttack;
                __instance.OnEvade -= OnEvade;
                __instance.OnCounter -= OnCounter;
                __instance.OnParry -= OnParry;
                __instance.OnGuardSucceeded -= OnGuard;
                if (__instance.TryGetComponent<ItemController>(out var itemController))
                {
                    itemController.OnDrinkPotionServerside -= OnDrinkPotion;
                }
            }

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            public static void Update_Postfix(UnitAvatar __instance)
            {
                if (!__instance.isServer)
                    return;

                PlayerAvatar? player = __instance as PlayerAvatar;
                if (player != null) return;

                if (EvilGuardianEffect.TryGetValue(__instance, out var Value))
                {
                    if (__instance.hp < __instance.MaxHp)
                    {
                        if (Value.Update(Time.deltaTime))
                        {
                            __instance.AddCustomStatUnsafe(TextFile.ReceivedDamageReduction, -2);
                        }
                        if (!Value.Active())
                            EvilGuardianEffect.Remove(__instance);
                    }
                }

                __instance.UpdateModEffectTime(ref StrikeBackTime);
                if (__instance.Leader is PlayerAvatar Leader)
                {
                    if (Leader.IsWeapon(WeaponIDs.ColdAnger))
                    {
                        __instance.StartTempInvulnerable(0.5f);
                    }
                }

                int Hard = ModConfig.HardMode;
                if (Hard >= 2)
                {
                    if (!Br2RecoveryLife.ContainsKey(__instance))
                        Br2RecoveryLife.Add(__instance, new Timer(0.4f));
                    else if (Br2RecoveryLife[__instance].Update(Time.deltaTime))
                    {
                        if((int)invulnerableCountField.GetValue(__instance) <= 0)
                        {
                            float Mult = 0.2f;
                            if (Hard >= 4) Mult *= 1.6f;
                            __instance.HealPercent(Mult);
                        }
                    }
                }
                if (__instance.GetCustomStatUnsafe(TextFile.ModAddCustomerIntensity) <= 0)
                {
                    string TName = __instance.name.Replace("(Clone)", "");
                    if (__instance.monsterType != EMonsterType.Dummy)
                    {
                        if (Hard > 0)
                        {
                            float AddMult = DungeonPatchs.HardModAddLife;
                            if (!ModConfig.BossRushMode) AddMult *= 1.5f;
                            if (__instance.monsterType == EMonsterType.Boss)
                            {
                                if (TName != "QQBoss" && TName != "QQQBoss")
                                {
                                    float ExtraMult = 0.15f * Hard * AddMult;
                                    if (!ModConfig.BossRushMode) ExtraMult *= 1.5f;
                                    AddMult += ExtraMult;
                                }
                            }
                            __instance.AddMaxHp(__instance.MaxHp * AddMult);
                        }
                        if (__instance.monsterType == EMonsterType.Boss)
                        {
                            if (ModConfig.EvilGuardian)
                            {
                                __instance.AddCustomStatUnsafe(TextFile.ReceivedDamageReduction, 40);
                                EvilGuardianEffect[__instance] = new CountingTimer(20, 1f);
                            }
                        }
                        if (TName == "WeaselKnight")
                        {
                            if (ModConfig.Instance.AddItem)
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
                                            __instance.Inventory.AddItem(new ItemMetadata(ItemDatabase.GenerateInstanceID(new Random(__instance.randomID)), ItemDatabase.FindItemById(ModItem.id), 1));
                                            break;
                                    }
                                }
                            }
                        }
                        if (TName == "QQQBoss" && ModConfig.Instance.EnableDamageStatistics)
                        {
                            DungeonManager.Instance.StartCoroutine(UseQQQBossDps());
                        }
                        if (Hard >= 1)
                        {
                            int Cout = 0;
                            while (__instance.protectionPoint > 0)
                            {
                                __instance.NetworkprotectionPoint -= 1;
                                Cout += 1;
                            }
                            if (Cout > 0)
                            {
                                float AddMult = DungeonPatchs.HardModAddLife;
                                if (!ModConfig.BossRushMode) AddMult *= 1.5f;
                                float Mult = 1f + AddMult;
                                __instance.AddMaxHp(250 * Cout * Mult * Hard);
                                __instance.HealPercent(100f);
                            }
                        }

                        if (DungeonPatchs.NumberofReincarnations > 0) TName += $"(轮回{DungeonPatchs.NumberofReincarnations})";
                        DungeonManager.Instance.hardModeEnvironment.TryGetValue("FEROCIOUSCLAWS", out int V);
                        (string, float, int, int) Date = (TName, __instance.NetworkmaxHp, __instance.Networkattack.IntMult(1f + V * 0.01f), __instance.GetCustomStat(ECustomStat.DamageReduction));
                        if (!Monster.Contains(Date))
                        {
                            Monster.Add(Date);
                        }
                    }
                    else __instance.AddMaxHp(float.MaxValue * 0.1f);
                    __instance.AddCustomStatUnsafe(TextFile.ModAddCustomerIntensity, 1);
                }//通用怪物属性加成
                else if (__instance.GetCustomStatUnsafe(TextFile.ModAddCustomerIntensity) <= 1)
                {
                    if (__instance.monsterType != EMonsterType.Dummy)
                    {
                        string TName = __instance.name.Replace("(Clone)", "");
                        int EternalReincarnation = DungeonPatchs.NumberofReincarnations;
                        if (EternalReincarnation > 0 && !NoNpcER.Contains(TName))
                        {
                            __instance.NetworkmaxHp = Tools.CalculateValue(__instance.MaxHp, 5f);
                            __instance.HealPercent(100f);
                            __instance.AddCustomStatUnsafe(TextFile.AllDamageBonus, 10.IntMult(Mathf.Pow(EternalReincarnation, 4)));
                            __instance.AddCustomStat(ECustomStat.DamageReduction, 78 * EternalReincarnation);
                        }
                    }
                    __instance.AddCustomStatUnsafe(TextFile.ModAddCustomerIntensity, 1);
                }//轮回属性加成
                else if (__instance.GetCustomStatUnsafe(TextFile.ModAddCustomerIntensity) <= 2)
                {
                    if (__instance.monsterType != EMonsterType.Dummy)
                    {
                        int PlayerStack = PlayerSpawner.MultiplayerList.Count;
                        int AddPlayerStack = ModConfig.Instance.ModifyConnectionsStack;
                        if (AddPlayerStack > 0)
                        {
                            string HPKey = (__instance.monsterType == EMonsterType.Miniboss || __instance.monsterType == EMonsterType.Boss)
                            ? "minibossBonusHpByPlayerNumber" : "enemyBonusHpByPlayerNumber";

                            float Orig = PlayerStack * KeywordDatabase.GetConstValue(HPKey, true) / 100f;
                            float Base = __instance.MaxHp / (1 + Orig);
                            float Mult = 1f + (PlayerStack + AddPlayerStack) * KeywordDatabase.GetConstValue(HPKey, true) / 100f;
                            __instance.NetworkmaxHp = Base * Mult;
                            __instance.HealPercent(100f);

                            Orig = ModConfig.Instance.ModifyConnectionsStack * KeywordDatabase.GetConstValue("minibossBonusDamageByPlayerNumber", true) / 100f;
                            __instance.AddCustomStat(ECustomStat.AllDamageBonus, (int)Orig);
                        }
                    }
                    __instance.AddCustomStatUnsafe(TextFile.ModAddCustomerIntensity, 1);
                }//额外计算多人血量
                else if (__instance.GetCustomStatUnsafe(TextFile.ModAddCustomerIntensity) <= 3)
                {
                    string TName = __instance.name.Replace("(Clone)", "");
                    if (__instance.monsterType != EMonsterType.Dummy)
                    {
                        if (TName == "Askard_Chapter2")
                        {
                            float Mult = 0.2f;
                            if (Hard >= 4) Mult = 0.4f;
                            __instance.AddMaxHp(__instance.MaxHp * Mult);
                            if (__instance.isServer) __instance.HealPercent(100f);
                        }
                        if (TName == "MoleElite_Boss")
                        {
                            __instance.AddMaxHp(__instance.MaxHp);
                            if (__instance.isServer) __instance.HealPercent(100f);
                        }
                        if (TName == "ArmadilloGolem" && Hard > 0)
                        {
                            float LifeMult = 1.5f;
                            if (Hard >= 4) LifeMult = 2f;
                            __instance.AddMaxHp(__instance.MaxHp * LifeMult);
                            if (__instance.isServer) __instance.HealPercent(100f);
                        }
                        if (TName == "StoneOink" && Hard > 0)
                        {
                            __instance.AddMaxHp(__instance.MaxHp * Hard);
                            if (__instance.isServer) __instance.HealPercent(100f);

                            __instance.AddCustomStatUnsafe(TextFile.DamageReduction, Hard * 20);
                        }
                        if (TName == "QQBossPowerCrystal2" && Hard >= 2)
                        {
                            float Add = 0.1f * QQBoss.CrystalStack;
                            __instance.AddMaxHp(__instance.MaxHp * Add);
                            __instance.HealPercent(100f);
                        }
                    }
                    __instance.AddCustomStatUnsafe(TextFile.ModAddCustomerIntensity, 1);
                }//独立怪物属性加成
            }

            [HarmonyPatch("GetCustomStatUnsafe")]
            [HarmonyPostfix]
            static void GetCustomStatUnsafe_Postfix(UnitAvatar __instance, string id, ref int __result)
            {
                if (__instance.Leader is PlayerAvatar Leader)
                {
                    if (id.EqualsAny(TextFile.Evasion, TextFile.StrikeBack))
                    {
                        __result += Leader.GetCustomStatUnsafe(id.ToUpperInvariant()).IntMult(Leader.GetCustomStatUnsafe(TextFile.CompanionStrikeBack) * 0.01f);
                    }
                    return;
                }

                PlayerAvatar? player = __instance as PlayerAvatar;
                if (player == null) return;

                string normalizedId = id.ToUpperInvariant();
                __result = StarSephiriaMod.ModifiedCustomStat(__instance, normalizedId, __result);
            }

            [HarmonyPatch("ApplyDamage")]
            [HarmonyPostfix]
            static void ApplyDamage_Postfix(UnitAvatar __instance, DamageInstance damage, ref EApplyDamageResult __result)
            {
                if (__result != EApplyDamageResult.Success)
                    return;

                OnHit(damage);
            }

            [HarmonyPatch("GetEvasionChancePercent")]
            [HarmonyPrefix]
            public static bool GetEvasionChancePercent_Prefix(UnitAvatar __instance, ref float __result)
            {
                if (!ModConfig.Instance.ModifyEvasionFormula)
                {
                    return true;
                }
                float Value = __instance.GetCustomStatUnsafe(TextFile.Evasion) * 0.01f;
                if (Value <= 0)
                {
                    __result = 0;
                    return false;
                }

                Value = 1f - 65f / (Value + 65f);
                Value *= 1f + __instance.GetCustomStatUnsafe(TextFile.EvasionRateReduction) * 0.01f;
                __result = Value * 100f;
                return false;
            }

            [HarmonyPatch("GetDamageReduction")]
            [HarmonyPrefix]
            public static bool GetDamageReduction_Prefix(float damage, int defense, ref float __result)
            {
                if (!ModConfig.Instance.ModifyDefenseFormula)
                {
                    return true;
                }
                __result = damage * (1 - 65f / (defense + 65f));
                return false;
            }

            [HarmonyPatch("AddShield")]
            [HarmonyPrefix]
            public static void AddShield_Prefix(UnitAvatar __instance, ref float shield, string key)
            {
                if (ModConfig.HardMode >= 3)
                {
                    shield *= 1f - __instance.GetRosionStack() * 0.01f;
                    if (shield <= 0) shield = 0f;
                }
            }

            [HarmonyPatch("ApplyDebuff")]
            [HarmonyPrefix]
            public static bool ApplyDebuff_Prefix(UnitAvatar __instance, ref CharacterDebuff debuffPrefab, UnitAvatar caster)
            {
                if (caster.GetCustomStatUnsafe(TextFile.ElectricIce) > 0)
                {
                    bool Plasma = caster.GetCustomStatUnsafe(TextFile.PlasmaActive) > 0;
                    if (debuffPrefab is CharacterDebuff_Electric || debuffPrefab is CharacterDebuff_Burn && Plasma)
                        debuffPrefab = CombatManager.Instance.frostbiteDebuffPrefab;
                }
                if ((int)invulnerableCountField.GetValue(__instance) > 0) return false;
                return true;
            }

            [HarmonyPatch("CreateHPBar")]
            [HarmonyPrefix]
            public static bool CreateHPBar_Prefix(UnitAvatar __instance)
            {
                if (__instance.Leader is PlayerAvatar Player)
                {
                    return false;
                }
                return true;
            }

            [HarmonyPatch("Heal", new Type[] { typeof(float), typeof(bool), typeof(bool) })]
            [HarmonyPrefix]
            public static bool Heal_Prefix(UnitAvatar __instance, float amount, bool allowOverHeal, bool ignorePenalty)
            {
                if (PollutedBloodRecovery.TryGetValue(__instance, out var Data))
                {
                    if (Data.NoHPUp) return false;
                }
                return true;
            }

            [HarmonyPatch("HealPercent", new Type[] { typeof(float), typeof(bool), typeof(bool) })]
            [HarmonyPrefix]
            public static bool HealPercent_Prefix(UnitAvatar __instance, float percent, bool preventOverHeal, bool ignorePenalty)
            {
                if (PollutedBloodRecovery.TryGetValue(__instance, out var Data))
                {
                    if (Data.NoHPUp) return false;
                }
                return true;
            }

            [HarmonyPatch("LocalSetHP")]
            [HarmonyPostfix]
            static void LocalSetHP_Postfix(UnitAvatar __instance, float amount)
            {
                if (!ModConfig.PollutedBlood) return;

                if (PollutedBloodRecovery.TryGetValue(__instance, out var Data))
                {
                    PollutedBloodData PBD = Data;
                    PBD.CurrentHP = amount;
                }
            }

            [HarmonyPatch("LocalStartBattle")]
            [HarmonyPostfix]
            static void LocalStartBattle_Postfix(UnitAvatar __instance)
            {
                if (__instance.IsInBattle)
                {
                    if (RosionList.Contains(__instance))
                        __instance.SetEffectHUDValue("CurrentRosion", __instance.GetRosionStack().ToString());
                }
            }

            [HarmonyPatch("Die")]
            [HarmonyPostfix]
            static void Die_Postfix(UnitAvatar __instance, int hitLevel, DamageInstance diedFrom)
            {
                if (!__instance.isServer) return;

                Br2RecoveryLife.SafeRemove(__instance);
                string TargetName = __instance.name.Replace("(Clone)", "");
                if (__instance.monsterType == EMonsterType.Boss && !DefeatBoss.Contains(TargetName))
                {
                    if(TargetName == "RootDemonPart_Point" || TargetName == "RootDemonPart_Laser" || TargetName == "RootDemonPart_Line")
                    {
                        DefeatBoss.Add(TargetName);
                        return;
                    }

                    DungeonPatchs.HardModAddLife += 0.06f + 0.02f * ModConfig.HardMode;
                    StarSephiriaMod.LogInfoDungeon($"怪物血量加成{DungeonPatchs.HardModAddLife}");

                    DefeatBoss.Add(TargetName);
                }

                if (TargetName == "QQQBoss")
                {
                    if (ModConfig.InfiniteMode)
                    {
                        int ReincarnationsCount = 0;
                        foreach (PlayerSpawner PlayerList in PlayerSpawner.MultiplayerList)
                        {
                            PlayerAvatar TargetPlayer = PlayerList.PlayerAvatar;
                            foreach (string Falg in TargetPlayer.savableParameters.ToList())
                            {
                                if (Falg.StartsWith("HaveTraded/"))
                                    TargetPlayer.savableParameters.Remove(Falg);
                            }
                            if (TargetPlayer.isServer) TargetPlayer.HealPercent(60f);
                            if (ModConfig.BossRushMode)
                            {
                                StatusInstance statusInstance = StatusDatabase.CreateStatusEntity("INVENTORY_SLOT/10");
                                TargetPlayer.AddOrphanedStatusInstance(statusInstance);
                            }
                            if (ReincarnationsCount <= 0)
                            {
                                ReincarnationsCount += 1;
                                DungeonPatchs.NumberofReincarnations += 1;
                                DungeonManager.Instance.Chat(TargetPlayer, "[Star's Sephiria Mod]", $"{Tools.GetLocalization("Instruction_InfiniteMode")}{DungeonPatchs.NumberofReincarnations}");
                                DungeonPatchs.InitializationModGameData("New");
                            }
                        }
                        DungeonManager CurrenDungeon = DungeonManager.Instance;
                        if (CurrenDungeon != null)
                        {
                            DungeonPatchs.eachStageHeadsField.SetValue(CurrenDungeon, new Dictionary<string, FloorData>());
                            CurrenDungeon.generatedFloors.Clear();
                            CurrenDungeon.sortedStages.Clear();
                            CurrenDungeon.LoadStageAndMove("Moleland");
                        }
                    }
                }
            }
            static IEnumerator UseQQQBossDps()
            {
                while (UseDPS)
                {
                    yield return null;
                }
                UseQQBossDamage = new Dictionary<UnitAvatar, float>(RecordQQBossDamage);
                UseDPS = true;
                if (PlayerSpawner.MultiplayerList.Count <= 1)
                {
                    PlayerAvatar Player = PlayerSpawner.MultiplayerList[0].PlayerAvatar;
                    NoHitCount.Add(Player, new NoHitRecord(Player.GetCustomStatUnsafe(TextFile.CurrentRosion)));
                }
                yield break;
            }

            [HarmonyPatch("HandleMoneyChanged")]
            [HarmonyPostfix]
            static void HandleMoneyChanged_Postfix(UnitAvatar __instance, int oldMoney, int newMoney)
            {
                if (oldMoney <= 0) return;
                if (oldMoney > newMoney)
                {
                    int Down = oldMoney - newMoney;
                    if (__instance.GetCustomStatUnsafe(TextFile.ReturnCoin) > 0)
                    {
                        float DownValue = Down * 0.4f;
                        DownValue *= (1f + __instance.GetCustomStatUnsafe(TextFile.MoneyDrop) * 0.01f);
                        __instance.AddMoney((int)DownValue);
                        AddGoldObtained(__instance, (int)DownValue);
                        UseCoinToExp(__instance, (int)DownValue);
                        __instance.TargetSendCustomMessage(__instance.connectionToClient, "TradeMoneyAdded/" + ((int)DownValue).ToString());
                        __instance.AddCustomStatUnsafe(TextFile.ReturnCoin, -1);
                    }
                }
                else if (newMoney > oldMoney)
                {
                    int Add = newMoney - oldMoney;
                    AddGoldObtained(__instance, Add);
                    UseCoinToExp(__instance, Add);
                }
            }

            private static void AddGoldObtained(UnitAvatar __instance, int Value)
            {
                PlayerAvatar? Player = __instance as PlayerAvatar;
                if (Player != null)
                {
                    if (Player.GetCustomStatUnsafe(TextFile.GoldObtained) <= 0)
                    {
                        Player.AddOrphanedStatusInstance(StatusDatabase.CreateStatusEntity("STAT_GOLDOBTAINED", Player.Money));
                    }
                    else
                    {
                        Player.AddOrphanedStatusInstance(StatusDatabase.CreateStatusEntity("STAT_GOLDOBTAINED", Value));
                    }
                }
            }

            private static void UseCoinToExp(UnitAvatar Player, int Value)
            {
                if (Player.GetCustomStatUnsafe(TextFile.CoinToExp) > 0)
                {
                    if (Player.TryGetComponent<LevelController>(out var Level))
                    {
                        float AddValue = Value * 0.5f * (1f + Player.GetCustomStatUnsafe(TextFile.EXPDrop) * 0.01f);
                        Level.AddExp((int)AddValue);
                    }
                }
            }

            [HarmonyPatch("ServerSellToShop")]
            [HarmonyPrefix]
            static bool ServerSellToShop_Prefix(UnitAvatar __instance, UnitAvatar shop, GridInventory safe, sbyte x, sbyte y)
            {
                if (!NetworkServer.active) return true;
                if (!ModConfig.Instance.ModifyMerchant) return true;
                NewItemOwnInstance TargetItem = __instance.Inventory.FindItem(x, y);
                if (TargetItem != null)
                {
                    int Price = ItemDatabase.GetItemSellPrice(TargetItem.Entity, shop.GetCustomStat(ECustomStat.Negotiation), __instance.GetCustomStat(ECustomStat.Negotiation));
                    shop.SubMoney(Price);
                    __instance.AddMoney(Price);
                    int instanceID = TargetItem.InstanceID;
                    __instance.Inventory.DecreaseItemQuantity(x, y, 1);
                    if (DungeonManager.Instance) DungeonManager.Instance.UnboundOnServer(instanceID);
                }
                return false;
            }

            [HarmonyPatch("ServerSellSubBagItemToShop")]
            [HarmonyPrefix]
            static bool ServerSellSubBagItemToShop_Prefix(UnitAvatar __instance, UnitAvatar shop, GridInventory safe, sbyte subBagX)
            {
                if (!NetworkServer.active) return true;
                if (!ModConfig.Instance.ModifyMerchant) return true;
                if (!__instance.Inventory.subBagMatrix.TryGetValue(subBagX, out var value)) return true;
                ItemEntity itemEntity = ItemDatabase.FindItemById(value.entityID);
                if (itemEntity != null)
                {
                    int itemSellPrice = ItemDatabase.GetItemSellPrice(itemEntity, shop.GetCustomStat(ECustomStat.Negotiation), __instance.GetCustomStat(ECustomStat.Negotiation));
                    shop.SubMoney(itemSellPrice);
                    __instance.AddMoney(itemSellPrice);
                    __instance.Inventory.ServerDecreaseSubBagItemQuantity(subBagX, 1);
                    if (DungeonManager.Instance) DungeonManager.Instance.UnboundOnServer(value.instanceID);
                }
                return false;
            }

            [HarmonyPatch("UpdateHighestElementalBonus")]
            [HarmonyPrefix]
            public static bool RefreshElementalBonus(UnitAvatar __instance)
            {
                if (ModConfig.Instance.ModifyCharm || ModConfig.Instance.AddItem)
                {
                    int AddElemental = __instance.highestElementalBonus + StarSephiriaMod.PartyStats.ElementalDamage;
                    if (AddElemental >= 0)
                    {
                        int MaxValue = Tools.BaseOrigStatToMax(__instance);
                        foreach (string Key in EffectConstant.BaseStatsName)
                        {
                            if (__instance.customStats.GetValueOrDefault(Key, 0) == MaxValue)
                            {
                                __instance.calculatedBonusStats[Key] = AddElemental;
                                continue;
                            }
                            else __instance.calculatedBonusStats[Key] = 0;
                        }
                    }
                    OnUpdateHighestElemental?.Invoke();
                    return false;
                }
                return true;
            }
        }

        public static IEnumerator DeleteUnintentionalItems(PlayerAvatar Player, List<int>? StartItemId = null)
        {
            yield return new WaitForSeconds(0.5f);
            if (ModConfig.Instance.AddItem)
            {
                foreach (PlayerSpawner Spawner in PlayerSpawner.MultiplayerList)
                {
                    if (Spawner.PlayerAvatar == Player)
                    {
                        foreach (int Item in DisableCharm) Spawner.unlockedCharms.Remove(Item);
                        break;
                    }
                }
            }
            if (ModConfig.Instance.EnableCostumeModifier)
            {
                using (new GridInventory.Permission(Player.Inventory))
                {
                    int Count = 0;
                    int MaxCount = 0;
                    if (Tools.CostumeSkinUse(Player.Name, "Fox_2", false) || Tools.CostumeSkinUse(Player.Name, "Skeleton_2", false))
                        MaxCount += 1;
                    foreach (StoneTablet Tablet in Player.Inventory.stoneTablets.Values)
                    {
                        var Item = Player.Inventory.FindItem(Tablet.xIdx, Tablet.yIdx);
                        if (Item.Entity.id == 12000 || Item.Entity.id == 2099)
                        {
                            if (Count < MaxCount)
                                Count += 1;
                            else
                                Player.Inventory.ForceRemoveItem(Tablet.xIdx, Tablet.yIdx);
                        }
                    }
                    if (MaxCount <= 0)
                    {
                        if (StartItemId != null)
                        {
                            MaxCount = StartItemId.Count;
                            foreach (NewItemOwnInstance NewItem in Player.Inventory.inventoryMatrix.Values)
                            {
                                var Item = Player.Inventory.FindItem(NewItem.Position);
                                if (StartItemId.Contains(Item.Entity.id))
                                {
                                    if (Count < MaxCount)
                                        Count += 1;
                                    else
                                        Player.Inventory.ForceRemoveItem(NewItem.Position);
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(DamageDummy))]
        public static class DamageDummy_Patches
        {
            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            static void Update_Postfix(DamageDummy __instance)
            {
                if (__instance.isServer) __instance.HealPercent(100f);
                __instance.stunImmunity = 1;

                if (!ModConfig.Instance.ModifyDummyDamage) return;
                if (!AttackTimers.ContainsKey(__instance)) AttackTimers.Add(__instance, 0f);
                else
                {
                    if (AttackTimers[__instance] <= 0f)
                    {
                        CheckCollision(__instance);
                        AttackTimers[__instance] = 1f;
                    }
                    else AttackTimers[__instance] -= Time.deltaTime;
                }
            }
            private static void CheckCollision(DamageDummy Dummy)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll((Vector2)Dummy.transform.position, 2f);

                foreach (Collider2D col in hits)
                {
                    if (col.transform == Dummy.transform || col.transform.IsChildOf(Dummy.transform))
                        continue;

                    Hitbox hitbox = col.GetComponent<Hitbox>();
                    if (hitbox == null) continue;

                    CombatBehaviour combat = hitbox.GetCombatBehaviour(0);
                    if (combat == null) continue;
                    if (combat is PlayerAvatar Player)
                    {
                        DamageInstance damage = DamageInstance.GetDamage(Dummy, "[Star's Sephiria Mod]傀儡触发", Player.transform.position, Player.GetHostileFactionLayers(EDamageFromType.None), 0, EDamageType.Slice, EDamageFromType.None, Vector2.zero, 0, 0f);
                        var Field = OnEvadeField;
                        if (Field != null)
                        {
                            var Action = (Action<DamageInstance>)Field.GetValue(Player);
                            Action?.Invoke(damage);
                        }
                        Field = OnCounterField;
                        if (Field != null)
                        {
                            var Action = (Action<DamageInstance>)Field.GetValue(Player);
                            Action?.Invoke(damage);
                        }
                        Field = OnParryField;
                        if (Field != null)
                        {
                            var Action = (Action<DamageInstance>)Field.GetValue(Player);
                            Action?.Invoke(damage);
                        }
                        Field = OnGuardSucceededField;
                        if (Field != null)
                        {
                            var Action = (Action<DamageInstance, bool>)Field.GetValue(Player);
                            Action?.Invoke(damage, true);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PlayerAvatar))]
        public static class PlayerAvatar_Patches
        {
            [HarmonyPatch("OnStartServer")]
            [HarmonyPostfix]
            static void OnStartServer_Postfix(PlayerAvatar __instance)
            {
                __instance.AddCustomStatUnsafe(TextFile.WindDamage, KeywordDatabase.GetConstValue("playerDefaultElementalDamage", true));
            }

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            static void Update_Postfix(PlayerAvatar __instance)
            {
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.f3Key.isPressed)
                    {
                        CustomStatsMessage = true;
                    }
                    if (Keyboard.current.f4Key.isPressed)
                    {
                        ItemMessage = true;
                    }
                    if (Keyboard.current.f5Key.isPressed)
                    {
                        MonsterMessage = true;
                    }
                    if (!Keyboard.current.f3Key.isPressed && CustomStatsMessage)
                    {
                        DisplayCustomStats = !DisplayCustomStats;
                        CustomStatsMessage = false;
                    }
                    if (!Keyboard.current.f4Key.isPressed && ItemMessage)
                    {
                        ItemMessage = false;
                        if (DisplayCustomStats)
                        {
                            foreach (KeywordEntity KeywordEntity in Resources.LoadAll<KeywordEntity>("Keyword"))
                            {
                                StarSephiriaMod.LogInfo($"—关键词名称：{KeywordEntity.Name} ID: {KeywordEntity.keyword}" +
                                    $"\n描述：{KeywordEntity.Context}" +
                                    $"\n颜色：({KeywordEntity.textColor.r},{KeywordEntity.textColor.g},{KeywordEntity.textColor.b})");
                            }
                            foreach (GameObject Pro in Projectile.Values)
                            {
                                if(Pro.TryGetComponent<Bullet>(out var bullet))
                                    StarSephiriaMod.LogInfo($"—弹幕名称：{bullet.name}");
                                else
                                    StarSephiriaMod.LogInfo($"—弹幕名称(本体)：{Pro.name}");
                            }
                            foreach (AvatarSpawnEntity Avatar in UnitDatabase.GetSpawnEntities())
                            {
                                StarSephiriaMod.LogInfo($"—实体名称：{Avatar.name}");
                            }
                        }
                        else
                        {
                            foreach (ItemEntity itemEntity in Resources.LoadAll<ItemEntity>("Item"))
                            {
                                string A = $"—物品名称：{itemEntity.aName} ID：{itemEntity.id}";
                                if (itemEntity.icon != null)
                                    A += $" 贴图名称：{itemEntity.icon.name}";
                                if (itemEntity.type == EItemType.Charm && itemEntity.resourcePrefab != null)
                                {
                                    foreach (Charm_Basic comps in itemEntity.resourcePrefab.GetComponents<Component>().OfType<Charm_Basic>())
                                    {
                                        if(comps is Charm_Magic Magic)
                                            A += $" 魔法名称：{Magic.ContainedMagic.magicPrefab.GetComponent<ActiveSkill>().GetType().Name}";
                                        else
                                            A += $" 效果名称：{comps.GetType().Name}";
                                    }
                                }
                                StarSephiriaMod.LogInfo(A);
                            }
                            foreach (WeaponEntity weapon in WeaponDatabase.GetAll())
                            {
                                string A = $"—武器名称：{weapon.aName} ID：{weapon.id}";
                                if (weapon.mainWeaponPrefab != null)
                                {
                                    if (weapon.mainWeaponPrefab.TryGetComponent<WeaponSimple>(out var Simple))
                                    {
                                        A += $"\n 概率数据：{Simple.AttackWeightPerSwing}";
                                        foreach (var addon in Simple.addons)
                                        {
                                            if (addon == null) continue;

                                            A += $"\n 附加组件: {addon.GetType().Name}";
                                        }
                                    }
                                }
                                StarSephiriaMod.LogInfo(A);
                            }
                            foreach (ItemCategoryEntity itemcategoryEntiy in ItemDatabase.GetAllItemCategory())
                            {
                                StarSephiriaMod.LogInfo($"—连击名称：{itemcategoryEntiy.Name} ID：{itemcategoryEntiy.id}");
                            }
                            foreach (StatusEntity statusEntity in Resources.LoadAll<StatusEntity>("Status"))
                            {
                                StarSephiriaMod.LogInfo($"—状态名称：{statusEntity.className} ID：{statusEntity.id} 关键词：{statusEntity.statKeyword}");
                            }
                            foreach (CharacterDebuff characterDebuff in Resources.LoadAll<CharacterDebuff>("Debuff"))
                            {
                                StarSephiriaMod.LogInfo($"—减益名：{characterDebuff.name}");
                            }
                            foreach (Miracle MiracleEntiy in MiracleDatabase.GetAll())
                            {
                                StarSephiriaMod.LogInfo($"—奇迹名称：{MiracleEntiy.Name} ID：{MiracleEntiy.id}");
                            }
                            foreach (SocialIDEntity socialIDEntity in Resources.LoadAll<SocialIDEntity>("SocialID"))
                            {
                                StarSephiriaMod.LogInfo($"—实体名称：{socialIDEntity.Name} ID：{socialIDEntity.name}");
                            }
                            foreach (PassiveEntity passiveEntity in Resources.LoadAll<PassiveEntity>("Passive"))
                            {
                                StarSephiriaMod.LogInfo($"—能力名称：{passiveEntity.aName} ID：{passiveEntity.id}");
                            }
                            foreach (ActiveSkillEntity activeSkillEntity in Resources.LoadAll<ActiveSkillEntity>("ActiveSkill"))
                            {
                                StarSephiriaMod.LogInfo($"—技能名称：{activeSkillEntity.aName} ID：{activeSkillEntity.id}");
                            }
                            foreach (var Dir in DungeonManager.Instance.hardModeEnvironment)
                            {
                                StarSephiriaMod.LogInfo($"—困难模式设置：{Dir.Key} 值：{Dir.Value}");
                            }
                        }
                    }
                    if (!Keyboard.current.f5Key.isPressed && MonsterMessage)
                    {
                        StarSephiriaMod.LogInfo("——————正在生成——————");
                        foreach ((string Name, float Life, int Damage, int Defense) Date in Monster)
                        {
                            StarSephiriaMod.LogInfo($"—怪物名称：{Date.Name} 生命值：{Date.Life} 攻击力：{Date.Damage} 防御力：{Date.Defense}");
                        }
                        StarSephiriaMod.LogInfo("——————生成结束——————");
                        MonsterMessage = false;
                    }
                }

                if (__instance.isInDungeon <= 0)
                {
                    DungeonPatchs.NumberofReincarnations = 0;
                    GoldObtained[__instance.Name] = __instance.currentMoney;
                }

                if (UseHolyRabbitEffect.Count > 0)
                {
                    DungeonManager.Instance.StartCoroutine(HolyRabbitEffect(UseHolyRabbitEffect[0]));
                    UseHolyRabbitEffect.RemoveAt(0);
                }

                if (!__instance.isServer) return;

                if (ModConfig.HardMode >= 3 && __instance.isInDungeon > 0)
                {
                    if(RosionList.Add(__instance))
                    {
                        __instance.CreateEffectHUD("Rosion", "CurrentRosion");
                    }
                }
                else if (RosionList.Remove(__instance))
                {
                    __instance.DestroyEffectHUD("CurrentRosion");
                }

                if (__instance.isInDungeon <= 0)
                {
                    if (UseDPS)
                    {
                        DPSData.Clear();
                        UseQQBossDamage.Clear();
                        UseDPS = false;
                    }
                    if (NoHitCount.Count > 0)
                        NoHitCount.Clear();
                }

                foreach (CloudAttackData Cloud in ModCloudAttack)
                {
                    if (Cloud.Player != __instance) continue;
                    if (!Cloud.StartAttack)
                    {
                        ComboEffectBase Combo = __instance.Inventory.FindComboEffect("DARKCLOUD");
                        if (Combo != null && Combo is ComboEffect_DarkCloud DarkCloud)
                        {
                            __instance.StartCoroutine(Cloud.UpdateCloudAttack(DarkCloud));
                            Cloud.StartAttack = true;
                        }
                    }
                }

                if (Tools.CostumeSkinUse(__instance.Name, "Bat_3"))
                {
                    if (!BatTime.ContainsKey(__instance.Name))
                        BatTime.Add(__instance.Name, new Timer(0.2f));
                    else if (BatTime[__instance.Name].Update(Time.deltaTime))
                    {
                        if (__instance.hp > __instance.MaxHp * 0.5f && !__instance.IsInBattle)
                            __instance.Networkhp = __instance.hp - 1f;
                    }
                }

                UpdateMaxDimensionPocket(__instance);

                UpdateMaxPassivePoint(__instance);

                UpdateDPS(__instance);

                UpdateNoHit(__instance);

                UpdatePollutedBlood(__instance);

                if (StarSephiriaMod.BrownRabbit2Hit.TryGetValue(__instance.Name, out Timer time))
                {
                    if (time.Update(Time.deltaTime))
                    {
                        DamageInstance damage = DamageInstance.GetDamage(null, "[Star's Sephiria Mod]被动受伤", __instance.transform.position, __instance.GetHostileFactionLayers(EDamageFromType.None), 1, EDamageType.Slice, EDamageFromType.None, Vector2.zero, 0, 0f);
                        var Field = OnEvadeField;
                        if (Field != null)
                        {
                            var Action = (Action<DamageInstance>)Field.GetValue(__instance);
                            Action?.Invoke(damage);
                        }
                        Field = OnCounterField;
                        if (Field != null)
                        {
                            var Action = (Action<DamageInstance>)Field.GetValue(__instance);
                            Action?.Invoke(damage);
                        }
                        Field = OnParryField;
                        if (Field != null)
                        {
                            var Action = (Action<DamageInstance>)Field.GetValue(__instance);
                            Action?.Invoke(damage);
                        }
                        Field = OnGuardSucceededField;
                        if (Field != null)
                        {
                            var Action = (Action<DamageInstance, bool>)Field.GetValue(__instance);
                            Action?.Invoke(damage, true);
                        }
                        StarSephiriaMod.BrownRabbit2Hit[__instance.Name] = new Timer(6f);
                    }
                }
                else if (Tools.CostumeSkinUse(__instance.Name, "BrownRabbit_2"))
                {
                    StarSephiriaMod.BrownRabbit2Hit.Add(__instance.Name, new Timer(6f));
                }

                if (WeaponPatch.RuadhsleaSpearTime.TryGetValue(__instance, out Timer tim3))
                {
                    if (tim3.Update(Time.deltaTime))
                    {
                        WeaponPatch.RuadhsleaSpearBuff[__instance] = 0;
                    }
                }

                __instance.UpdateModEffectTime(ref WeaponPatch.InvincibleSpecialAttackCD);
                __instance.UpdateModEffectTime(ref WeaponPatch.DraftbladeAttack);
                __instance.UpdateModEffectTime(ref StarSephiriaMod.Otter3_AddCrit);
                __instance.UpdateModEffectTime(ref SolisImberEffect);
                __instance.UpdateModEffectTime(ref WeaponPatch.LightningsWrathTime);
                __instance.UpdateModEffectTime(ref WeaponPatch.FiaolasJudgmentStaffBuff);
                __instance.UpdateModEffectTime(ref WeaponPatch.GlacialBladeEffect);
                __instance.UpdateModEffectTime(ref WeaponPatch.LightningsWrathEffect2, Effect =>
                {
                    float Ratio = WeaponPatch.LightningsWrathEffect2[Effect].Ratio;
                    Effect.SetEffectHUDFillAmount("LightningsWrath_ThunderBeast", 1f - Ratio);
                }, End =>
                {
                    End.DestroyEffectHUD("LightningsWrath_ThunderBeast");
                });

                if (GuardDeBuff.TryGetValue(__instance, out Counter GuardCount))
                {
                    if (GuardCount.Update(Time.deltaTime, out bool Take))
                        GuardDeBuff.Remove(__instance);
                    if (Take)
                    {
                        UpdateGuardBroken(__instance);
                        GuardDeBuff.Remove(__instance);
                    }
                }

                if (RosionTime.ContainsKey(__instance))
                {
                    RosionTime[__instance] -= Time.deltaTime;
                }
            }

            public static FieldInfo isAvailableGuardField = AccessTools.Field(typeof(UnitAvatar), "isAvailableGuard");
            public static FieldInfo guardAvailabilityRestoreTimerField = AccessTools.Field(typeof(UnitAvatar), "guardAvailabilityRestoreTimer");
            public static void UpdateGuardBroken(UnitAvatar __instance)
            {
                if (ModConfig.HardMode <= 0) return;
                if (isAvailableGuardField == null || guardAvailabilityRestoreTimerField == null) return;

                isAvailableGuardField.SetValue(__instance, false);
                Timer RestoreTime = (Timer)guardAvailabilityRestoreTimerField.GetValue(__instance);
                RestoreTime.SetTimer(0f);
                guardAvailabilityRestoreTimerField.SetValue(__instance, RestoreTime);
                __instance.CreateEffectHUD("GUARDBREAK", "GuardBreak");
                if (__instance.guardBreakFxPrefab != null)
                {
                    SpriteFx spriteFx = SpriteFx.Pool.Spawn(__instance.guardBreakFxPrefab, __instance.transform.position, null);
                    if (spriteFx)
                    {
                        spriteFx.SetBodyYPos(__instance.TopdownActor ? __instance.TopdownActor.CenterYPos : 0f);
                    }
                }
                __instance.guardBreakSoundEvent?.Play();
                __instance.StartTempInvulnerable(0.5f);
                __instance.InactivateGuard();
            }

            private static readonly List<PlayerAvatar> UseHolyRabbitEffect = new List<PlayerAvatar>();

            [HarmonyPatch("ReviveAnotherPlayer")]
            [HarmonyPostfix]
            static void ReviveAnotherPlayer_Postfix(PlayerAvatar __instance, PlayerAvatar patient)
            {
                if (Tools.CostumeSkinUse(__instance.Name, "HolyRabbit_2", false))
                {
                    if (!UseHolyRabbitEffect.Contains(__instance))
                        UseHolyRabbitEffect.Add(__instance);
                    if (!UseHolyRabbitEffect.Contains(patient))
                        UseHolyRabbitEffect.Add(patient);
                    StarSephiriaMod.LogError("触发翼耳兔2的治疗效果");
                }
            }

            [HarmonyPatch("UserCode_CmdReviveAnotherPlayer__PlayerAvatar")]
            [HarmonyPostfix]
            static void UserCode_CmdReviveAnotherPlayer__PlayerAvatar_Postfix(PlayerAvatar __instance, PlayerAvatar patient)
            {
                if (Tools.CostumeSkinUse(__instance.Name, "HolyRabbit_2", false))
                {
                    if (!UseHolyRabbitEffect.Contains(__instance))
                        UseHolyRabbitEffect.Add(__instance);
                    if (!UseHolyRabbitEffect.Contains(patient))
                        UseHolyRabbitEffect.Add(patient);
                    StarSephiriaMod.LogError("触发翼耳兔2的治疗效果");
                }
            }

            private static IEnumerator HolyRabbitEffect(PlayerAvatar Player)
            {
                if (!Player.isServer) yield break;
                for(int i = 0; i < 25; i++)
                {
                    yield return new WaitForSeconds(0.4f);
                    Player.Heal(1);
                }
                yield break;
            }

            private static readonly FieldInfo costumeStatsField = AccessTools.Field(typeof(PlayerAvatar), "costumeStats");
            private static readonly FieldInfo costumeStartingItemInstanceIDsField = AccessTools.Field(typeof(PlayerAvatar), "costumeStartingItemInstanceIDs");
            private static readonly FieldInfo weaponControllerField = AccessTools.Field(typeof(PlayerAvatar), "weaponController");

            [HarmonyPatch("UpdateCostumeData")]
            [HarmonyPrefix]
            static bool UpdateCostumeData_Prefix(PlayerAvatar __instance, string costumeID, bool fromRuntime)
            {
                if (!ModConfig.Instance.EnableCostumeModifier) return true;
                try
                {
                    if (!UseCostomEffect.TryGetValue(__instance.Name, out bool useEffect) || !useEffect)
                    {
                        return false;
                    }

                    if (costumeStatsField == null || costumeStartingItemInstanceIDsField == null || weaponControllerField == null)
                    {
                        StarSephiriaMod.LogError("[PlayerAvatar][UpdateCostumeData]字段反射失败");
                        return true; // 执行原方法
                    }
                    // 获取字段值
                    WeaponControllerSimple? weaponController = (WeaponControllerSimple)weaponControllerField.GetValue(__instance);
                    List<StatusInstance> costumeStats = (List<StatusInstance>)costumeStatsField.GetValue(__instance);
                    List<int> costumeStartingItemInstanceIDs = (List<int>)costumeStartingItemInstanceIDsField.GetValue(__instance);

                    if (__instance.isServer)
                    {
                        foreach (StatusInstance statusInstance in costumeStats)
                        {
                            if (statusInstance != null)
                            {
                                statusInstance.RemoveStatus();
                                statusInstance.ClearTarget();
                            }
                        }
                        costumeStats.Clear();
                        costumeStatsField.SetValue(__instance, costumeStats);

                        using (new GridInventory.Permission(__instance.Inventory))
                        {
                            foreach (int Item in costumeStartingItemInstanceIDs)
                            {
                                __instance.Inventory.RemoveStartingItem(Item);
                            }
                            costumeStartingItemInstanceIDs.Clear();
                            if (__instance.NetworkcurrentCostumeEquipEffect)
                            {
                                __instance.NetworkcurrentCostumeEquipEffect.Remove();
                                __instance.NetworkcurrentCostumeEquipEffect.UnequipServer();
                                Object.Destroy(__instance.NetworkcurrentCostumeEquipEffect.gameObject);
                                __instance.NetworkcurrentCostumeEquipEffect = null;
                            }
                        }
                    }

                    // 2. 查找服装
                    CostumeEntity costumeEntity = CostumeDatabase.FindCostumeByID(costumeID);
                    if (costumeEntity == null)
                    {
                        StarSephiriaMod.LogError($"找不到服装: {costumeID}");
                        return true;
                    }
                    weaponController.onlyCanUseDefaultWeapon = costumeEntity.onlyCanUseDefaultWeapon;

                    StarSephiriaMod.LogInfo($"玩家：{__instance.Name}");
                    StarSephiriaMod.LogInfo($"—穿戴服装：{costumeEntity.aName} ID:{costumeID} ");
                    if (__instance.isServer)
                    {
                        string[] stats = costumeEntity.stats;
                        ItemEntity[] item = costumeEntity.startingItems;
                        bool UseEffect = true;
                        StarSephiriaMod.ModifiedCostume(__instance.Name, ref stats, ref item, ref UseEffect);
                        foreach(string UseStats in stats)
                        {
                            StarSephiriaMod.LogInfo($"—服装属性：{UseStats}");
                            StatusInstance statusInstance = StatusDatabase.CreateStatusEntity(UseStats);
                            if (statusInstance != null)
                            {
                                statusInstance.SetTarget(__instance);
                                statusInstance.ApplyStatus(fromRuntime);
                                costumeStats.Add(statusInstance);
                            }
                        }

                        List<int> NewStartingItem = new List<int>();
                        foreach (ItemEntity itemEntity in item)
                        {
                            if (itemEntity && NewStartingItem.Count <= 0)
                            {
                                ItemMetadata itemMetadata = new ItemMetadata(-1, itemEntity.id, 1);
                                int itemInstanceId = __instance.Inventory.AddStartingItem(itemMetadata, __instance.spawner.currentPlayerIdx);
                                StarSephiriaMod.LogInfo($"—服装初始物品：{itemEntity.Name} ID:{itemEntity.id} 序列号:{itemInstanceId} ");
                                NewStartingItem.Add(itemInstanceId);
                            }
                        }
                        costumeStatsField.SetValue(__instance, costumeStats);
                        costumeStartingItemInstanceIDsField.SetValue(__instance, NewStartingItem);
                        if (costumeEntity.networkEffectPrefab && UseEffect)
                        {
                            GameObject obj = Object.Instantiate(costumeEntity.networkEffectPrefab, __instance.transform);
                            NetworkServer.Spawn(obj, __instance.gameObject);
                            if (obj.TryGetComponent<CostumeEquipEffect>(out var component))
                            {
                                __instance.NetworkcurrentCostumeEquipEffect = component;
                                __instance.NetworkcurrentCostumeEquipEffect.EquipServer(__instance);
                                __instance.NetworkcurrentCostumeEquipEffect.Apply();
                            }
                            else
                            {
                                Debug.LogError($"[CostumeEquipEffect]找不到组件: {costumeEntity.networkEffectPrefab}");
                            }
                        }
                        __instance.StartCoroutine(DeleteUnintentionalItems(__instance, NewStartingItem));
                    }
                    UseCostomEffect[__instance.Name] = false;
                    var Field = OnCostumeChangedClientsideField;
                    if (Field != null)
                    {
                        var Action = (Action<string>)Field.GetValue(__instance);
                        Action?.Invoke(costumeID);
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[PlayerAvatar][UpdateCostumeData]重写失败: {ex.Message}\n{ex.StackTrace}");
                    return true;
                }
            }

            [HarmonyPatch("EquipCostume")]
            [HarmonyPostfix]
            static void EquipCostume_Postfix(PlayerAvatar __instance, string costumeID, string skinID)
            {
                if (!__instance.isServer)
                {
                    CostumeSkinEntity costumeSkinByID = CostumeDatabase.GetCostumeSkinByID(skinID);
                    StarSephiriaMod.CostumeType[__instance.Name] = costumeID;
                    string SkinGuid = costumeSkinByID.skinID.ToSafeString();
                    StarSephiriaMod.CostumeSkinType[__instance.Name] = SkinGuid;
                    StarSephiriaMod.CostumeSkinKey[__instance.Name] =
                        StarSephiriaMod.CostumeSkinIdToName.TryGetValue(SkinGuid, out string SkinKey) ? SkinKey : "";
                }
                TextFile.LoadText();
            }

            [HarmonyPatch("LocalEquipCostume")]
            [HarmonyPostfix]
            static void LocalEquipCostume_Postfix(PlayerAvatar __instance, string costumeID, string skinID)
            {
                CostumeSkinEntity costumeSkinByID = CostumeDatabase.GetCostumeSkinByID(skinID);
                StarSephiriaMod.CostumeType[__instance.Name] = costumeID;
                string SkinGuid = costumeSkinByID.skinID.ToSafeString();
                StarSephiriaMod.CostumeSkinType[__instance.Name] = SkinGuid;
                StarSephiriaMod.CostumeSkinKey[__instance.Name] =
                    StarSephiriaMod.CostumeSkinIdToName.TryGetValue(SkinGuid, out string SkinKey) ? SkinKey : "";
                StarSephiriaMod.InitializeModStat(__instance.Name);
                StarSephiriaMod.LogInfo($"—服装皮肤：{costumeSkinByID.skinID.ToSafeString()}");
                UseCostomEffect[__instance.Name] = true;

                UpdateCostumeData_Prefix(__instance, costumeID, true);
            }

            [HarmonyPatch("AddDealStat")]
            [HarmonyPostfix]
            static void AddDealStat_Postfix(PlayerAvatar __instance, UnitAvatar victim, DamageInstance damage)
            {
                if (victim.monsterType != EMonsterType.Dummy && !string.IsNullOrWhiteSpace(damage.id))
                {
                    if (DPSData.ContainsKey(__instance))
                    {
                        AddDPS(__instance, DPSData[__instance]);
                    }
                }
            }

            [HarmonyPatch("AddDealStatAsLeader")]
            [HarmonyPostfix]
            static void AddDealStatAsLeader_Postfix(PlayerAvatar __instance, UnitAvatar victim, UnitAvatar follower, DamageInstance damage)
            {
                if (victim.monsterType != EMonsterType.Dummy && (bool)follower)
                {
                    if (DPSData.ContainsKey(__instance))
                    {
                        AddDPS(__instance, DPSData[__instance]);
                    }
                }
            }

            private static void AddDPS(PlayerAvatar __instance, DPSRecord Record)
            {
                if (!Record.InBattle) return;

                float totalDamage = 0f;
                foreach (var kvp in __instance.dealsStatistics_LastLocation)
                {
                    totalDamage += kvp.Value;
                }
                if(totalDamage > 0f)
                {
                    float Down = 0;
                    if (UseQQBossDamage.TryGetValue(__instance, out float Value))
                        Down = Value;

                    DPSData[__instance].TotalDamage = totalDamage - Down;
                    DPSData[__instance].TrueDuration = DPSData[__instance].Duration;
                    DPSData[__instance].EndBattle = false;
                }
            }

            private static void UpdateDPS(PlayerAvatar __instance)
            {
                if (!ModConfig.Instance.EnableDamageStatistics) return;

                if (UseDPS && !DPSData.ContainsKey(__instance))
                    DPSData.Add(__instance, new DPSRecord());
                if (DPSData.TryGetValue(__instance, out DPSRecord Record))
                {
                    if (__instance.IsInBattle)
                    {
                        Record.InBattle = true;
                    }
                    if (Record.TotalDamage > 0)
                    {
                        Record.Duration += Time.deltaTime;
                        if (!__instance.IsInBattle && Record.InBattle && !Record.EndBattle)
                        {
                            Record.EndBattle = true;
                        }
                        if (__instance.IsInBattle && Record.EndBattle)
                        {
                            Record.EndBattle = false;
                        }
                    }
                    if (DPSData.Count > 0 && __instance.Name == DPSData.First().Key.Name && Record.EndBattle)
                    {
                        if (DPSData.Count > 1)
                        {
                            bool falg = true;
                            float Duration = 0f;
                            foreach (var MultiRecord in DPSData)
                            {
                                DPSRecord R = MultiRecord.Value;
                                if (!R.EndBattle && R.TotalDamage > 0)
                                {
                                    falg = false;
                                }
                                else if (R.TotalDamage > 0)
                                {
                                    if (R.TrueDuration > Duration) Duration = R.TrueDuration;
                                }
                            }
                            if (falg)
                            {
                                float TotalDamage = 0f;
                                int Minutes = (int)(Duration / 60);
                                int Seconds = (int)(Duration % 60);
                                foreach (var MultiRecord in DPSData)
                                {
                                    DPSRecord R = MultiRecord.Value;
                                    TotalDamage += R.TotalDamage;
                                    RecordQQBossDamage[__instance] = R.TotalDamage;
                                    float DPS = Duration > 1f ? R.TotalDamage / Duration : R.TotalDamage;
                                    string MultText = string.Format(Tools.GetLocalization("Instruction_DPSRecord_MultiPlayer"), MultiRecord.Key.Name, R.TotalDamage.ToString("#0"), DPS.ToString("#0"));
                                    DungeonManager.Instance.Chat(MultiRecord.Key, "[Star's Sephiria Mod]", MultText);
                                }
                                string Text = string.Format(Tools.GetLocalization("Instruction_DPSRecord_Multi"), Minutes, Seconds, TotalDamage.ToString("#0")) + "\n";
                                DungeonManager.Instance.Chat(__instance, "[Star's Sephiria Mod]", Text);
                                DPSData.Clear();
                                UseQQBossDamage.Clear();
                                UseDPS = false;
                            }
                        }
                        else
                        {
                            int Minutes = (int)(Record.TrueDuration / 60);
                            int Seconds = (int)(Record.TrueDuration % 60);
                            RecordQQBossDamage[__instance] = Record.TotalDamage;
                            DungeonManager.Instance.Chat(__instance, "[Star's Sephiria Mod]", string.Format(Tools.GetLocalization("Instruction_DPSRecord_Single"),
                                __instance.Name, Minutes, Seconds, Record.TotalDamage.ToString("#0"), Record.DPS.ToString("#0")));
                            DPSData.Clear();
                            UseQQBossDamage.Clear();
                            UseDPS = false;
                        }
                    }
                }
            }

            private static void UpdateNoHit(PlayerAvatar __instance)
            {
                if (NoHitCount.Count <= 0) return;
                if (!NoHitCount.TryGetValue(__instance, out var Target)) return;

                if (__instance.IsInBattle)
                {
                    if (!Target.InBattle)
                    {
                        float totalDamage = 0f;
                        foreach (var kvp in __instance.dealsStatistics_LastLocation) totalDamage += kvp.Value;
                        if (totalDamage > 0f)
                        {
                            Target.InBattle = true;
                            Target.CurrenHit = __instance.GetCustomStatUnsafe(TextFile.CurrentRosion);
                            StarSephiriaMod.LogInfo("[UpdateNoHit]进入战斗");
                        }
                    }
                }
                else if (Target.InBattle)
                {
                    int CurrenHitCount = __instance.GetCustomStatUnsafe(TextFile.CurrentRosion) - Target.CurrenHit;
                    string Text = $"Hit Count：{CurrenHitCount}!";
                    if (CurrenHitCount == 0 &&
                        ModConfig.Instance.AddItem &&
                        ModConfig.Instance.ModifyCharm &&
                        ModConfig.Instance.EnableWeaponModifier &&
                        ModConfig.Instance.EnableCostumeModifier)
                    {
                        __instance.AddDice(1);
                        __instance.AddMoney(300);
                        Text = $"No Hit!\n+1{KeywordDatabase.GetEntity("Dice").GetIcon()}!\n+300{KeywordDatabase.GetEntity("Leaf").GetIcon()}!";
                    }
                    if (UIManager.Instance)
                    {
                        Vector2 Pos = __instance.transform.position - new Vector3(0f, 1f, 0f) + (Vector3)UnityEngine.Random.insideUnitCircle;
                        UIManager.Pool.Spawn<UI_DamageParticle>(Pos).SetDamage(Text, EffectConstant.CritColor);
                    }
                    NoHitCount.SafeRemove(__instance);
                }
            }

            private static void UpdateMaxPassivePoint(PlayerAvatar __instance)
            {
                int AddPassivePoint = 0;
                if (Tools.CostumeSkinUse(__instance.Name, "PinkRabbit_3"))
                {
                    AddPassivePoint += 5;
                }
                if (Tools.CostumeSkinUse(__instance.Name, "Deer_2"))
                {
                    AddPassivePoint += 30;
                }
                int Hard = ModConfig.HardMode;
                if(Hard > 0)
                {
                    AddPassivePoint += Hard * 5;
                }
                if (StarSephiriaMod.AddPassivePoint.TryGetValue(__instance.Name, out int OldAddPoint))
                {
                    if (OldAddPoint != AddPassivePoint)
                    {
                        StarSephiriaMod.AddPassivePoint[__instance.Name] = AddPassivePoint;
                        ResetPassiveStat(__instance);
                        if (NetworkServer.connections.Count <= 1)
                        {
                            SaveManager.Current.SetInt("ModAddPassivePoint", AddPassivePoint);
                        }
                    }
                }
                else
                {
                    StarSephiriaMod.AddPassivePoint.Add(__instance.Name, SaveManager.Current.GetInt("ModAddPassivePoint", 0));
                }
                __instance.NetworkmaxPassivePoint = 50 + AddPassivePoint;

                int CurrenPoint = 0;
                foreach (KeyValuePair<ulong, int> keyValuePair in __instance.passiveStats)
                {
                    CurrenPoint += keyValuePair.Value;
                }
                if (CurrenPoint > __instance.NetworkmaxPassivePoint) ResetPassiveStat(__instance);
            }

            private static void UpdatePollutedBlood(PlayerAvatar __instance)
            {
                if (!ModConfig.PollutedBlood) return;

                if (!PollutedBloodRecovery.ContainsKey(__instance))
                    PollutedBloodRecovery.Add(__instance, new PollutedBloodData(100));

                PollutedBloodData PBD = PollutedBloodRecovery[__instance];
                if (__instance.isInDungeon <= 0 || !__instance.IsInBattle)
                {
                    PBD.CurrentHP = __instance.hp;
                    if (PBD.CurrentHP < 1) PBD.CurrentHP = 1;
                    PBD.RecoveryHP = 0;
                    PBD.NoHPUp = false;
                }
                else
                {
                    if (__instance.hp > PBD.CurrentHP)
                    {
                        float Max = __instance.MaxHp * 0.5f;
                        if (Tools.CostumeSkinUse(__instance.Name, "Bat_3"))
                        {
                            Max = (__instance.maxHp + __instance.finalMaxHp * __instance.maxHp / 100f) * 0.5f;
                        }
                        if (PBD.RecoveryHP < Max)
                        {
                            PBD.RecoveryHP += __instance.hp - PBD.CurrentHP;
                            if (PBD.RecoveryHP > Max)
                            {
                                float Down = PBD.RecoveryHP - Max;
                                PBD.CurrentHP = __instance.hp - Down;
                                if (PBD.CurrentHP < 1) PBD.CurrentHP = 1;
                                PBD.RecoveryHP = Max;
                                PBD.NoHPUp = true;
                                __instance.SetHp(PBD.CurrentHP);
                            }
                            else
                            {
                                PBD.CurrentHP = __instance.hp;
                                if (PBD.CurrentHP < 1) PBD.CurrentHP = 1;
                            }
                        }
                        else PBD.NoHPUp = true;
                    }
                    else if(__instance.hp < PBD.CurrentHP)
                    {
                        PBD.CurrentHP = __instance.hp;
                        if (PBD.CurrentHP < 1) PBD.CurrentHP = 1;
                    }
                }
            }

            private static readonly List<string> DeerAddStats = new List<string>()
            {
                "DIMENSION_POCKET/3",
                "MAX_DIMENSION_POCKET/3"
            };
            private static readonly List<StatusInstance> DeerStatsList = new List<StatusInstance>();
            private static void UpdateMaxDimensionPocket(PlayerAvatar __instance)
            {
                if (__instance.isInDungeon > 0)
                {
                    return;
                }
                if (costumeStatsField == null)
                {
                    StarSephiriaMod.LogError("[PlayerAvatar][UpdateMaxDimensionPocket]字段反射失败");
                    return;
                }

                if (Tools.CostumeSkinUse(__instance.Name, "Deer_1", false))
                {
                    if(!StarSephiriaMod.Deer_Buff.Contains(__instance.Name))
                        StarSephiriaMod.Deer_Buff.Add(__instance.Name);
                }
                else if (StarSephiriaMod.Deer_Buff.Count > 0)
                {
                    if (__instance.GetCustomStatUnsafe(TextFile.MaxDimensionPocket) < 3)
                    {
                        List<StatusInstance> costumeStats = (List<StatusInstance>)costumeStatsField.GetValue(__instance);
                        foreach(string Stats in DeerAddStats)
                        {
                            StatusInstance UseStat = StatusDatabase.CreateStatusEntity(Stats);
                            if (!costumeStats.Contains(UseStat))
                            {
                                UseStat.SetTarget(__instance);
                                UseStat.ApplyStatus(true);
                                costumeStats.Add(UseStat);
                                DeerStatsList.Add(UseStat);
                            }
                        }
                        costumeStatsField.SetValue(__instance, costumeStats);
                    }
                }//鹿1 所有玩家许愿池数量增加
                else if(__instance.GetCustomStatUnsafe(TextFile.MaxDimensionPocket) > 0)
                {
                    List<StatusInstance> costumeStats = (List<StatusInstance>)costumeStatsField.GetValue(__instance);
                    foreach (StatusInstance Stats in costumeStats)
                    {
                        if (DeerStatsList.Contains(Stats))
                        {
                            Stats.RemoveStatus();
                            Stats.ClearTarget();
                            costumeStats.Remove(Stats);
                            DeerStatsList.Remove(Stats);
                        }
                    }
                    costumeStatsField.SetValue(__instance, costumeStats);
                }
            }

            private static void ResetPassiveStat(PlayerAvatar __instance)
            {
                foreach (KeyValuePair<ulong, List<StatusInstance>> keyValuePair in __instance.passiveStatInstances)
                {
                    foreach (StatusInstance statusInstance in keyValuePair.Value)
                    {
                        statusInstance.RemoveStatus();
                        statusInstance.ClearTarget();
                    }
                }
                foreach (KeyValuePair<ulong, List<PassiveObject>> keyValuePair2 in __instance.passiveLV5Perk)
                {
                    if (keyValuePair2.Value != null)
                    {
                        foreach (PassiveObject passiveObject in keyValuePair2.Value)
                        {
                            if (passiveObject != null)
                            {
                                passiveObject.Disable();
                                NetworkServer.Destroy(passiveObject.gameObject);
                            }
                        }
                    }
                }
                foreach (KeyValuePair<ulong, List<PassiveObject>> keyValuePair3 in __instance.passiveLV10Perk)
                {
                    if (keyValuePair3.Value != null)
                    {
                        foreach (PassiveObject passiveObject2 in keyValuePair3.Value)
                        {
                            if (passiveObject2 != null)
                            {
                                passiveObject2.Disable();
                                NetworkServer.Destroy(passiveObject2.gameObject);
                            }
                        }
                    }
                }
                foreach (KeyValuePair<ulong, List<PassiveObject>> keyValuePair4 in __instance.passiveLV20Perk)
                {
                    if (keyValuePair4.Value != null)
                    {
                        foreach (PassiveObject passiveObject3 in keyValuePair4.Value)
                        {
                            if (passiveObject3 != null)
                            {
                                passiveObject3.Disable();
                                NetworkServer.Destroy(passiveObject3.gameObject);
                            }
                        }
                    }
                }
                __instance.passiveStats.Clear();
                __instance.passiveStatInstances.Clear();
                __instance.passiveLV5Perk.Clear();
                __instance.passiveLV10Perk.Clear();
                __instance.passiveLV20Perk.Clear();
            }
        }

        [HarmonyPatch(typeof(UnitAI_NewBasic))]
        public static class UnitAI_NewBasic_Patches
        {
            [HarmonyPatch("SetSocialID")]
            [HarmonyPrefix]
            static void SetSocialID_Prefix(UnitAI_NewBasic __instance, string socialID, ref EProceduralMerchantType merchant, ref ItemMetadata[] startingItems)
            {
                StarSephiriaMod.LogInfoProjectile("生成单位: " + socialID);
            }

            [HarmonyPatch("AddTradingItemsToList")]
            [HarmonyPrefix]
            static void AddTradingItemsToList_Prefix(UnitAI_NewBasic __instance, Random itemSeed, ref List<ItemMetadata> targetList, ref int charms, ref int stoneTablets, double extraDamageCharmRatio)
            {
                if (ModConfig.Instance.ModifyMerchant)
                {
                    targetList.Add(new ItemMetadata(ItemDatabase.GenerateInstanceID(itemSeed), ItemDatabase.FindItemById(51), 1));
                }
            }
        }

        public static List<int> BondingItems = new List<int>();
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
                        List<RectTransform> Group = new List<RectTransform> { CloneRect };
                        foreach (UI_StatusTooltipOpener Row in __instance.statElements)
                        {
                            if (Row.transform.parent != Template.transform.parent) continue;
                            if (!ElementHooks.Contains(Row.statHookID)) continue;
                            if (!(Row.transform is RectTransform RowRect)) continue;
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

            static readonly string[] ElementHooks = { "PHYSICAL_DAMAGE", "FIRE_DAMAGE", "ICE_DAMAGE", "LIGHTNING_DAMAGE" };
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
                            __result = new Loc.KeywordValue[]
                            {
                                new Loc.KeywordValue("BASE", hooker.HookStat(TextFile.WindDamage + "_BASE", out _, out _)),
                                new Loc.KeywordValue("AMP", hooker.HookStat(TextFile.WindDamage + "_AMP", out _, out _)),
                            };
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
                    StringBuilder stringBuilder = new StringBuilder();
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
                StringBuilder stringBuilder = new StringBuilder();
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
                    string[] ModifyWeaponToopTip = new string[2] { "", "" };
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
                GameObject container = new GameObject("IconTextGroup");
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

                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(container.transform, false);
                Image icon = iconObj.AddComponent<Image>();
                icon.sprite = iconSprite;
                icon.preserveAspect = true;
                RectTransform iconRt = icon.rectTransform;
                iconRt.sizeDelta = new Vector2(iconSize, iconSize);

                GameObject textObj = new GameObject("LeftText");
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

        [HarmonyPatch(typeof(ComboEffect_Debuff))]
        public static class ComboEffect_Debuff_Patch
        {
            [HarmonyPatch("OnAttackUnit")]
            [HarmonyPrefix]
            static void OnAttackUnit_Prefix(ComboEffect_Debuff __instance, out int __state)
            {
                __state = __instance.cooldownInfos.Count;
            }

            [HarmonyPatch("OnAttackUnit")]
            [HarmonyPostfix]
            static void OnAttackUnit_Postfix(ComboEffect_Debuff __instance, UnitAvatar avatar, int __state)
            {
                if (!__instance.isServer) return;
                if (__instance.cooldownInfos.Count <= __state) return;
                if (!(__instance.debuffPrefab is CharacterDebuff_WindErosion)) return;

                UnitAvatar Player = __instance.Networkavatar;
                if (!Player.IsWeapon(WeaponIDs.Vortex)) return;
                if (!Player.IsInBattle) return;

                VortexEffect(Player, avatar.transform.position);
            }

            public static void VortexEffect(UnitAvatar Player, Vector3 Pos)
            {
                Vector2 vector = (Vector2)(Pos - Player.transform.position);
                if (Player.isServer)
                {
                    float Damage = CharacterDebuff_WindErosion.GetWindErosionDamage(Player) / 24f * 50f;
                    Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                    Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.01f;
                    Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 0.01f;
                    WindDagger_Vortex.Data.CreateAttack(EDamageFromType.DirectAttack, Damage, "[Star's Sephiria Mod]涡旋", true, Player, Pos, Pos, 0.5f, null);
                }
            }
        }

        [HarmonyPatch(typeof(ComboEffect_FlameSword))]
        public static class ComboEffect_FlameSword_Patch
        {
            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            static void Update_Postfix(ComboEffect_FlameSword __instance)
            {
                if (!__instance.isServer) return;

                __instance.minCooldownTime = 0.125f;
                UnitAvatar Player = __instance.Networkavatar;
                if (Player.IsWeapon(WeaponIDs.SolisIubar))
                {
                    __instance.minCooldownTime = 0.125f / (1f + Player.GetCustomStatUnsafe(TextFile.AttackSpeed));
                }
             }

            [HarmonyPatch("AddSwordServer")]
            [HarmonyPostfix]
            static void AddSwordServer_Postfix(ComboEffect_FlameSword __instance, int amount, bool invokeCallback)
            {
                UnitAvatar Player = __instance.Networkavatar;
                if (Player.IsWeapon(WeaponIDs.SolisIubar))
                {
                    if (!SolisIubarEffect.ContainsKey(Player))
                        SolisIubarEffect.Add(Player, 1);
                    else
                        SolisIubarEffect[Player] += 1;
                }
            }
        }

        [HarmonyPatch(typeof(ComboEffect_DarkCloud))]
        public static class ComboEffect_DarkCloud_Patch
        {
            [HarmonyPatch("UseCloudCoroutine")]
            [HarmonyPrefix]
            static void UseCloudCoroutine_Postfix(ComboEffect_DarkCloud __instance, bool needCloudValue, ref int multishot, bool isAutoDischarge)
            {
                if (!isAutoDischarge) return;
                if (__instance.avatar != null)
                {
                    if (__instance.avatar is PlayerAvatar Player)
                    {
                        if (Player.IsWeapon(WeaponIDs.CeruleanCloudSwordArges))
                        {
                            int AddStack = Mathf.Min(3, Mathf.FloorToInt(Player.GetCustomStatUnsafe(TextFile.DarkCloudKeep) / 30f));
                            multishot += AddStack;
                        }//苍云刀
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SkillController))]
        public static class SkillController_Patch
        {
            [HarmonyPatch("GetMultipleCastCount")]
            [HarmonyPostfix]
            static void GetMultipleCastCount_Postfix(SkillController __instance, ref int __result)
            {
                if (Tools.CostumeSkinUse(__instance.UnitAvatar.Name, "Ghost_2"))
                    __result += 1;
            }

            [HarmonyPatch("OnSkillCreated")]
            [HarmonyPostfix]
            static void OnSkillCreated_Postfix(SkillController __instance, ActiveSkill skill)
            {
                if (__instance.UnitAvatar is PlayerAvatar Player)
                {
                    if (Player.IsWeapon(WeaponIDs.LegacyofTheIllusionist))
                    {
                        int CastCount = __instance.GetMultipleCastCount();
                        float Speed = 1f + (Player.GetCustomStatUnsafe(TextFile.CooldownRecoverySpeed) * 0.01f);
                        Speed += skill.Charm.additionalcooldownRecoverySpeedSynced * 0.01f;
                        Speed *= 1f / (1f - Player.GetCustomStatUnsafe(TextFile.MagicCooldownReduction) * 0.01f);
                        float CharmDamage = skill.Charm.RequestCharmDamageBonusOnRoot() * 0.01f;
                        if (WeaponPatch.LegacyofTheIllusionistAttack.TryGetValue(Player, out var Attack))
                        {
                            if (Attack.Count <= 0)
                            {
                                Attack.Count = 10;
                                Attack.AttackStack = CastCount;
                                Attack.AttackSpeed = Speed;
                                Attack.CharmDamageMult = CharmDamage;
                            }
                        }
                        else
                        {
                            LegacyofTheIllusionistAttackData NewAttack = new LegacyofTheIllusionistAttackData(10, 0.8f)
                            {
                                AttackStack = CastCount,
                                AttackSpeed = Speed,
                                CharmDamageMult = CharmDamage
                            };
                            WeaponPatch.LegacyofTheIllusionistAttack.Add(Player, NewAttack);
                        }
                    }//幻术师的遗产
                    if (Player.IsWeapon(WeaponIDs.Successor))
                    {
                        if(skill is ActiveSkill_Bolt Bolt)
                        {
                            float CharmDamage = 1f + skill.Charm.RequestCharmDamageBonusOnRoot() * 0.01f;
                            WeaponPatch.SuccessorEffect[Player] = new SuccessorBoltData(Bolt, CharmDamage, __instance.GetMultipleCastCount());
                        }
                    }//继承者
                    if (Tools.CostumeSkinUse(Player.Name, "Seren_2", false))
                    {
                        bool Add = true;
                        float Mana = skill.Charm.GetCost(Player, skill.Charm.CurrentLevelToIdx());
                        List<ShieldData> curShieldDatas = ((SyncList<ShieldData>)curShieldDatasField.GetValue(Player)).ToList();
                        foreach(ShieldData ShieldDatas in curShieldDatas)
                        {
                            if(ShieldDatas.key == "Seren2Shield")
                            {
                                float Value = 5 + Player.MaxMp * 0.05f;
                                if (ShieldDatas.shield < Value)
                                {
                                    int Orig = Player.GetRosionStack();
                                    Mana += ShieldDatas.shield / (1f - Orig * 0.01f);
                                    Mana = Math.Min(Mana, Value);
                                }
                                else Add = false;
                                break;
                            }
                        }
                        if (Add && Mana > 0) Player.AddShield(Mana, "Seren2Shield");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(AvatarStatsHooker))]
        public static class AvatarStatsHooker_Patch
        {
            [HarmonyPatch("HasStat")]
            [HarmonyPrefix]
            static bool HasStat_Prefix(AvatarStatsHooker __instance, string id, ref bool __result)
            {
                if(__instance.TryGetComponent<UnitAvatar>(out UnitAvatar Player))
                {
                    if (id == TextFile.WindDamage)
                    {
                        __result = Player.GetCustomStatUnsafe(TextFile.WindDamage) != 0;
                        return false;
                    }
                    if (id == TextFile.WindDamage + "_BASE")
                    {
                        __result = Player.GetCustomBaseStatUnsafe(TextFile.WindDamage) != 0;
                        return false;
                    }
                    if (id == TextFile.WindDamage + "_AMP")
                    {
                        __result = Player.GetCustomStatAmpUnsafe(TextFile.WindDamage) != 0;
                        return false;
                    }
                }
                return true;
            }

            [HarmonyPatch("HookStat")]
            [HarmonyPrefix]
            static bool HookStat_Prefix(AvatarStatsHooker __instance, string id, ref EStatValueSign sign, ref bool hasStat, ref string __result)
            {
                if (__instance.TryGetComponent<UnitAvatar>(out UnitAvatar Player))
                {
                    if (id == TextFile.WindDamage)
                    {
                        int Value = Player.GetCustomStatUnsafe(TextFile.WindDamage);
                        sign = Tools.GetStandardSign(Value);
                        hasStat = Value != 0;
                        __result = Value.ToString();
                        return false;
                    }
                    if (id == TextFile.WindDamage + "_BASE")
                    {
                        int Value = Player.GetCustomBaseStatUnsafe(TextFile.WindDamage);
                        sign = Tools.GetStandardSign(Value);
                        hasStat = Value != 0;
                        __result = Value.ToString();
                        return false;
                    }
                    if (id == TextFile.WindDamage + "_AMP")
                    {
                        int Value = Player.GetCustomStatAmpUnsafe(TextFile.WindDamage);
                        sign = Tools.GetStandardSign(Value);
                        hasStat = Value != 0;
                        __result = Value.ToString();
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(KeywordDatabase))]
        public static class KeywordDatabase_Patch
        {
            private static readonly FieldInfo constField = AccessTools.Field(typeof(KeywordDatabase), "constValueDictionary");
            private static readonly FieldInfo floatField = AccessTools.Field(typeof(KeywordDatabase), "constFloatValueDictionary");

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

        [HarmonyPatch(typeof(LevelController))]
        public static class LevelController_Patch
        {
            [HarmonyPatch("LevelUpOnServer")]
            [HarmonyPostfix]
            static void LevelUpOnServer_Postfix(LevelController __instance)
            {
                if (!__instance.isServer) return;

                if (Tools.CostumeSkinUse(__instance.Avatar.Name, "PinkRabbit_3"))
                {
                    if (__instance.currentLevel % 5 == 0)
                        __instance.GenerateItem(__instance.Avatar.RandomID + __instance.currentLevel + 500);
                }

                if (!ModConfig.InfiniteMode) return;

                int Stack = 3;
                if (__instance.currentLevel % Stack == 0)
                {
                    StatusInstance statusInstance = StatusDatabase.CreateStatusEntity("INVENTORY_SLOT/1");
                    __instance.Avatar.AddOrphanedStatusInstance(statusInstance);
                }
            }
        }

        public static Sephirite.Type CurrentSephiriteType { get; set; } = Sephirite.Type.NORMAL;
        public static event Action<Sephirite, UnitAvatar>? OnGenerateItemsList;

        [HarmonyPatch(typeof(Sephirite))]
        public static class Sephirite_Patch
        {
            [HarmonyPatch("GenerateItems")]
            [HarmonyPrefix]
            static void GenerateItems_Prefix(Sephirite __instance, GameObject actor)
            {
                if (!__instance.isServer) return;

                __instance.appearLimit = 10;
                if (actor.TryGetComponent<UnitAvatar>(out UnitAvatar Player))
                {
                    OnGenerateItemsList?.Invoke(__instance, Player);
                }
                if (ModConfig.BossRushMode)
                {
                    if (__instance.type == Sephirite.Type.TABLET_BOSS)
                        __instance.type = Sephirite.Type.TABLET;
                }
                CurrentSephiriteType = __instance.type;
            }

            [HarmonyPatch("GenerateItems")]
            [HarmonyPostfix]
            static void GenerateItems_Postfix(Sephirite __instance, GameObject actor)
            {
                if (!__instance.isServer) return;

                if (actor.TryGetComponent<UnitAvatar>(out UnitAvatar Player))
                {
                    if (__instance.rewards.Count > 0)
                    {
                        if (__instance.type != Sephirite.Type.TABLET && __instance.type != Sephirite.Type.TABLET_BOSS)
                        {
                            if (ModConfig.Instance.AddItem)
                            {
                                if (Tools.CostumeSkinUse(Player.Name, "Squirrel_3", false))
                                {
                                    if (GetModItem.TryGetValue("MysteryBox_Random", out ItemEntity ModItem))
                                    {
                                        Random random = new System.Random(__instance.CurrentSeed + 1000);
                                        __instance.rewards.Add(new SephiriteRewardMetadata(ItemDatabase.GenerateInstanceID(random), ModItem.id));
                                    }
                                }
                            }
                        }

                        if (actor.TryGetComponent<PlayerSpawner>(out PlayerSpawner Spawner))
                        {
                            if (__instance.type == Sephirite.Type.NORMAL)
                            {
                                if (Tools.CostumeSkinUse(Player.Name, "Crocodile_2", false))
                                {
                                    Dictionary<EItemRarity, WeightedItemSelector> StoneList = new Dictionary<EItemRarity, WeightedItemSelector>();
                                    foreach (int All in Spawner.unlockedStoneTablets)
                                    {
                                        ItemEntity Item = ItemDatabase.FindItemById(All);
                                        if (!Item) continue;
                                        if (__instance.appearedItems.Contains(Item.id)) continue;

                                        if (!StoneList.ContainsKey(Item.rarity))
                                        {
                                            StoneList.Add(Item.rarity, new WeightedItemSelector());
                                        }

                                        StoneList[Item.rarity].AddItem(new WeightedItem(Item, KeywordDatabase.GetConstValue("defaultItemDropWeight")));
                                    }
                                    if (StoneList.Count > 0)
                                    {
                                        Random random = new Random(__instance.CurrentSeed + 1999);
                                        EItemRarity key = Sephirite.RarityDice(__instance.type, random, Player.GetCustomStatUnsafe(TextFile.Luck) * 0.01f);
                                        WeightedItem TargetItem = StoneList[key].GetRandom(random);
                                        __instance.rewards.Add(new SephiriteRewardMetadata(ItemDatabase.GenerateInstanceID(random), TargetItem.item.id));
                                        __instance.appearedItems.Enqueue(TargetItem.item.id);
                                    }
                                }
                            }
                        }
                    }
                }

                int i = 0;
                while(__instance.appearedItems.Count > __instance.appearLimit)
                {
                    __instance.appearedItems.Dequeue();

                    i += 1;
                    if (i > 10) break;
                }
            }
        }

        [HarmonyPatch(typeof(GridInventory))]
        public static class GridInventory_Patch
        {
            [HarmonyPatch("AddStorage")]
            [HarmonyPrefix]
            static bool AddStorage_Prefix(GridInventory __instance, ref short add)
            {
                if(__instance.UnitAvatar is PlayerAvatar Player)
                {
                    if (Tools.CostumeSkinUse(Player.Name, "Fox_3", false))
                    {
                        if (add > 0)
                        {
                            int Max = 32;
                            if (__instance.CurrentInventoryStorage + add > Max)
                            {
                                add = (short)(Max - __instance.CurrentInventoryStorage);
                            }
                            if (__instance.CurrentInventoryStorage > Max)
                                return false;
                        }
                    }
                }
                return true;
            }

            [HarmonyPatch("GetItemDropWeight")]
            [HarmonyPostfix]
            static void GetItemDropWeight_Postfix(GridInventory __instance, ItemEntity entity, ref int __result)
            {
                if (entity.categories.Contains("PARTY"))
                {
                    if (NetworkServer.connections.Count <= 1)
                        __result = 0;
                }
            }

            [HarmonyPatch("get_CurrentStoneTabletsCount")]
            [HarmonyPostfix]
            static void CurrentStoneTabletsCount_Postfix(GridInventory __instance, ref int __result)
            {
                int Stack = __instance.stoneTablets.Count;
                foreach(StoneTablet tablet in __instance.stoneTablets.Values)
                {
                    if (tablet.entityID == 2101)
                        Stack += 1;
                }
                __result = Stack;
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
                    if (ModConfig.Instance.AddCategory)
                    {
                        foreach (ItemCategoryEntity Data in ModItemCategoryData)
                        {
                            itemCategories[Data.id] = Data;
                        }
                        foreach (int DisableId in DisableCharm)
                        {
                            itemDictionary[DisableId].activeType = EItemActiveType.Hidden;
                        }
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
                                    DarkCloud.addStatByCombo = new ComboEffectBase.ComboStat[]
                                    {
                                        new ComboEffectBase.ComboStat(){ comboCount = 4, status = new string[] { "MIN_DARK_CLOUD/25" } },
                                        new ComboEffectBase.ComboStat(){ comboCount = 6, status = new string[] { "MIN_DARK_CLOUD/25" } },
                                        new ComboEffectBase.ComboStat(){ comboCount = 8, status = new string[] { "MIN_DARK_CLOUD/25" } },
                                        new ComboEffectBase.ComboStat(){ comboCount = 10, status = new string[] { "DARK_CLOUD_MULTISHOT/2" } },
                                    };
                                }
                            }
                            else if (itemCategoryEntity.id == "LAKE")
                            {
                                if (itemCategoryEntity.comboEffectPrefab.GetComponent<ComboEffectBase>() is ComboEffectBase Lake)
                                {
                                    Lake.addStatByCombo = new ComboEffectBase.ComboStat[]
                                    {
                                        new ComboEffectBase.ComboStat(){ comboCount = 2, status = new string[] { "MAX_MP/10", "MP_REGEN/10" } },
                                        new ComboEffectBase.ComboStat(){ comboCount = 4, status = new string[] { "MAX_MP/15", "MP_REGEN/20" } },
                                        new ComboEffectBase.ComboStat(){ comboCount = 6, status = new string[] { "MAX_MP/20", "MP_SKILL_DAMAGE/5" } },
                                        new ComboEffectBase.ComboStat(){ comboCount = 8, status = new string[] { "MAX_MP/25", "MP_SKILL_DAMAGE/10" } },
                                        new ComboEffectBase.ComboStat(){ comboCount = 10, status = new string[] { "MAX_MP/30", "MP_SKILL_DAMAGE/20" } },
                                    };
                                }
                            }
                            else if (itemCategoryEntity.id == "PARTY")
                            {
                                if (itemCategoryEntity.comboEffectPrefab.GetComponent<ComboEffectBase>() is ComboEffectBase PARTY)
                                {
                                    PARTY.addStatByCombo = new ComboEffectBase.ComboStat[]
                                    {
                                        new ComboEffectBase.ComboStat(){ comboCount = 2, status = new string[] { "ALL_PLAYER_DEFENSE/8" } },
                                        new ComboEffectBase.ComboStat(){ comboCount = 4, status = new string[] { "ALL_PLAYER_ALL_DAMAGE/8" } },
                                        new ComboEffectBase.ComboStat(){ comboCount = 6, status = new string[] { "ALL_PLAYER_ELEMENTAL_DAMAGE/8" } }
                                    };
                                }
                            }
                            else if (itemCategoryEntity.id == "SHADOW")
                            {
                                if (itemCategoryEntity.comboEffectPrefab.GetComponent<ComboEffectBase>() is ComboEffectBase SHADOW)
                                {
                                    SHADOW.addStatByCombo = new ComboEffectBase.ComboStat[]
                                    {
                                        new ComboEffectBase.ComboStat(){ comboCount = 2, status = new string[] { "EVASION/600" } },
                                        new ComboEffectBase.ComboStat(){ comboCount = 4, status = new string[] { "EVASION/700" } },
                                        new ComboEffectBase.ComboStat(){ comboCount = 6, status = new string[] { "EVASION/800" } },
                                        new ComboEffectBase.ComboStat(){ comboCount = 8, status = new string[] { "EVASION/1000" } },
                                        new ComboEffectBase.ComboStat(){ comboCount = 10, status = new string[] { "EVASION/1200", "IGNORE_DEFENSE/5" } },
                                    };
                                }
                            }
                            else if (itemCategoryEntity.id == "ELEMENTAL")
                            {
                                if (itemCategoryEntity.comboEffectPrefab.GetComponent<ComboEffectBase>() is ComboEffectBase ELEMENTAL)
                                {
                                    foreach(ComboEffectBase.ComboStat stat in ELEMENTAL.addStatByCombo)
                                    {
                                        if (stat.replaceEffectText)
                                        {
                                            stat.status = new string[]
                                            {
                                                "PHYSICAL_DAMAGE_AMP/10",
                                                "FIRE_DAMAGE_AMP/10",
                                                "ICE_DAMAGE_AMP/10",
                                                "LIGHTNING_DAMAGE_AMP/10",
                                                "WIND_DAMAGE_AMP/10",
                                            };
                                        }
                                    }
                                }
                            }
                        }
                    }

                    List<int> DisplayItems = new List<int>()
                    {
                        1305
                    };
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
                        if (itemIDList.Contains(itemEntity.id) || ModConfig.Instance.AddCategory && DisableCharm.Contains(itemEntity.id))
                        {
                            continue;
                        }
                        if (ModConfig.Instance.DisplayDisabledItems || ModConfig.Instance.AddItem && DisplayItems.Contains(itemEntity.id))
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
                    if (ModConfig.Instance.AddItem)
                    {
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
                if (ModConfig.Instance.AddItem)
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
                    if (ModConfig.Instance.Ultimate)
                    {
                        if (shard.shardKey == "[Star's Sephiria Mod]HardMode")
                        {
                            shard.shardMaxLevel = 4;
                            shard.effectPerLevel = new int[] { 1, 2, 3, 4 };
                            shard.stepIncreasePerLevel = new int[] { 10, 10, 10, 1 };
                        }
                    }
                    HardModeDatebase.data[shard.shardKey] = shard;
                }
                if (HardModeDatebase.data.TryGetValue("DIZZINESS", out HardModeShardEntity Dizziness))
                {
                    Dizziness.shardMaxLevel = 3;
                    Dizziness.effectPerLevel = new int[] { 1, 2, 3 };
                    Dizziness.stepIncreasePerLevel = new int[] { 2, 2, 2 };
                }
            }
        }

        [HarmonyPatch(typeof(HardModeManager))]
        public static class HardModeManager_Patch
        {
            [HarmonyPatch("ActivateHardModeOnServer")]
            [HarmonyPrefix]
            public static void ActivateHardModeOnServer_Prefix(HardModeManager __instance)
            {
                foreach (HardModeShardEntity hardModeShardEntity in HardModeDatebase.GetAll())
                {
                    if (hardModeShardEntity.isActive)
                    {
                        int HardLevel = SaveManager.Current.GetInt("HardModeShard_" + hardModeShardEntity.shardKey + "_Level", 0);
                        if (HardLevel > hardModeShardEntity.effectPerLevel.Length)
                        {
                            SaveManager.Current.SetInt("HardModeShard_" + hardModeShardEntity.shardKey + "_Level", hardModeShardEntity.effectPerLevel.Length);
                        }
                    }
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
                                RegisterInternalMethod.Invoke(null, new object[] { component });
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

        [HarmonyPatch(typeof(StatusDatabase))]
        public static class StatusDatabase_Patch
        {
            private static readonly FieldInfo statTypesField = AccessTools.Field(typeof(StatusDatabase), "statTypes");
            private static readonly FieldInfo entityDictionaryField = AccessTools.Field(typeof(StatusDatabase), "entityDictionary");
            private static readonly FieldInfo isCustomStatField = AccessTools.Field(typeof(StatusDatabase), "isCustomStat");

            [HarmonyPatch("Initialize")]
            [HarmonyPostfix]
            public static void Initialize_Postfix()
            {
                Dictionary<string, StatusEntity> entityDictionary = (Dictionary<string, StatusEntity>)entityDictionaryField.GetValue(null);
                Dictionary<string, Type> statTypes = (Dictionary<string, Type>)statTypesField.GetValue(null);
                Dictionary<string, bool> isCustomStat = (Dictionary<string, bool>)isCustomStatField.GetValue(null);
                foreach (ModKeyWord Data in ModKeywordEntityData)
                {
                    if (Data.Status != null)
                    {
                        isCustomStat[Data.Status.id] = true;
                        entityDictionary.Add(Data.Status.id, Data.Status);
                        statTypes.Add(Data.Status.id, typeof(StatusInstance_Custom));
                    }
                }
                entityDictionaryField.SetValue(null, entityDictionary);
                statTypesField.SetValue(null, statTypes);
                isCustomStatField.SetValue(null, isCustomStat);
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
                    List<WeaponEntity> UseRandmon = new List<WeaponEntity>();
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

        [HarmonyPatch(typeof(PlayerSpawner))]
        public static class PlayerSpawner_Patch
        {
            [HarmonyPatch("OnStartServer")]
            [HarmonyPostfix]
            public static void OnStartServer_Postfix(PlayerSpawner __instance)
            {
                if (ModConfig.Instance.AddCategory)
                {
                    foreach (int DisableId in DisableCharm)
                    {
                        __instance.unlockedCharms.Remove(DisableId);
                    }
                }
                if (ModConfig.Instance.AddItem)
                {
                    foreach (ModItem Data in ModItemEntityData)
                    {
                        ItemEntity? ModItem = Data.Item;
                        if (ModItem == null) continue;
                        if (ModItem.activeType == EItemActiveType.Hidden) continue;
                        switch (ModItem.type)
                        {
                            case EItemType.Potion:
                                if (!__instance.unlockedPotions.Contains(ModItem.id))
                                    __instance.unlockedPotions.Add(ModItem.id);
                                break;
                            case EItemType.StoneTablet:
                                if (!__instance.unlockedStoneTablets.Contains(ModItem.id))
                                    __instance.unlockedStoneTablets.Add(ModItem.id);
                                break;
                            default:
                                if (!__instance.unlockedCharms.Contains(ModItem.id))
                                    __instance.unlockedCharms.Add(ModItem.id);
                                break;
                        }
                    }
                }
                if (ModConfig.Instance.ModifyCharm)
                {
                    foreach (int ID in BondingItems)
                    {
                        if (ItemDatabase.FindItemById(ID).isDual) continue;
                        if (!__instance.unlockedCharms.Contains(ID))
                            __instance.unlockedCharms.Add(ID);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CombatManager))]
        public static class CombatManager_Patch
        {
            [HarmonyPatch("Start")]
            [HarmonyPostfix]
            public static void Start_Postfix(CombatManager __instance)
            {
                __instance.elementalColors[EffectConstant.Wind] = EffectConstant.WindColor;
            }
        }

        [HarmonyPatch(typeof(CharacterDebuff))]
        public static class CharacterDebuff_Patch
        {
            private static GameObject? OrigFx = null;
            private static GameObject? PlasmaFx = null;

            [HarmonyPatch("CreateLoopFx")]
            [HarmonyPrefix]
            static void CreateLoopFx_Prefix(CharacterDebuff __instance)
            {
                if (__instance is CharacterDebuff_Frostbite)
                {
                    if (OrigFx == null) OrigFx = __instance.loopFxPrefab;
                    if (PlasmaFx == null) PlasmaFx = UnitDatabase.GetDebuff("PLASMA").loopFxPrefab;
                    if (OrigFx != null && PlasmaFx != null)
                    {
                        bool Plasma = __instance.NetworkAttacker.GetCustomStatUnsafe(TextFile.PlasmaActive) > 0;
                        bool ElectricIce = __instance.NetworkAttacker.GetCustomStatUnsafe(TextFile.ElectricIce) > 0;
                        if (!Plasma) __instance.loopFxPrefab = OrigFx;
                        else if (Plasma && ElectricIce) __instance.loopFxPrefab = PlasmaFx;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CharacterDebuff_Electric))]
        public static class CharacterDebuff_Electric_Patch
        {
            [HarmonyPatch("Attack")]
            [HarmonyPostfix]
            static void Attack_Postfix(CharacterDebuff_Electric __instance, UnitAvatar target, int stack, float damageRatio)
            {
                if (!__instance.NetworkAttacker) return;

                UnitAvatar Player = __instance.NetworkAttacker;
                if (Player.IsWeaponUseNet(WeaponIDs.LightningsWrath))
                {
                    if (!WeaponPatch.LightningsWrathEffect2.ContainsKey(Player))
                    {
                        if (!WeaponPatch.LightningsWrathEffect.ContainsKey(Player))
                            WeaponPatch.LightningsWrathEffect[Player] = 1;
                        else if (WeaponPatch.LightningsWrathEffect[Player] < 40)
                        {
                            WeaponPatch.LightningsWrathEffect[Player] += 1;
                        }
                    }
                }
                if (Player.IsWeaponUseNet(WeaponIDs.LightningFlash))
                {
                    if (stack > 1)
                    {
                        int Add = stack - 1;
                        if (WeaponPatch.LightningFlashBuff.ContainsKey(Player))
                            WeaponPatch.LightningFlashBuff[Player] += Add;
                        else
                            WeaponPatch.LightningFlashBuff[Player] = Add;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CharacterDebuff_Frostbite))]
        public static class CharacterDebuff_Frostbite_Patch
        {
            [HarmonyPatch("get_MaxStackCount")]
            [HarmonyPostfix]
            static void GetMaxStackCount_Postfix(CharacterDebuff_Frostbite __instance, ref int __result)
            {
                __result = 99;
            }

            [HarmonyPatch("AddStack")]
            [HarmonyPostfix]
            static void AddStack_Postfix(CharacterDebuff_Frostbite __instance)
            {
                UnitAvatar Player = __instance.NetworkAttacker;
                int FreezeCount = 5 - Player.GetCustomStatUnsafe(TextFile.FreezeThreshold);
                bool Plasma = Player.GetCustomStatUnsafe(TextFile.PlasmaActive) > 0;
                bool ElectricIce = Player.GetCustomStatUnsafe(TextFile.ElectricIce) > 0;
                if (ElectricIce)
                {
                    if (UnityEngine.Random.Range(0, 100) < Player.GetCustomStatUnsafe(TextFile.ElectricLuck))
                    {
                        __instance.NetworkCurrentStack = (sbyte)(__instance.CurrentStack + 1);
                    }
                    int CountMult = 2 + Player.GetCustomStatUnsafe(TextFile.ElectricStack);
                    if (Plasma)
                    {
                        CountMult += Player.GetCustomStatUnsafe(TextFile.BurnStack);
                        FreezeCount -= Player.GetCustomStatUnsafe(TextFile.BurnAdd);
                    }
                    if (__instance.NetworkTarget.monsterType != EMonsterType.Boss &&
                        __instance.NetworkTarget.monsterType != EMonsterType.Dummy)
                    {
                        FreezeCount = 1;
                    }
                    FreezeCount *= CountMult;
                }
                if (__instance.CurrentStack >= FreezeCount && __instance.NetworkTarget && Player)
                {
                    __instance.NetworkTarget.ApplyDebuff(__instance.freezeDebuffPrefab, Player);
                    __instance.NetworkCurrentStack -= (sbyte)FreezeCount;
                    UseFreezeEffect(__instance);
                    if (__instance.NetworkCurrentStack <= 0) __instance.RequestEnd();
                }
            }
        }

        [HarmonyPatch(typeof(CharacterDebuff_Freeze))]
        public static class CharacterDebuff_Freeze_Patch
        {
            public static readonly MethodInfo RpcChimFxMethod_Electric = AccessTools.Method(typeof(CharacterDebuff_Electric), "UserCode_RpcChimFx__UInt32__Vector2");
            public static readonly MethodInfo RpcChimFxMethod_Plasma = AccessTools.Method(typeof(CharacterDebuff_Plasma), "UserCode_RpcChimFx__UInt32__Vector2");

            private static float GetElectricIceDamage(CharacterDebuff_Freeze __instance)
            {
                UnitAvatar Player = __instance.NetworkAttacker;
                bool Plasma = Player.GetCustomStatUnsafe(TextFile.PlasmaActive) > 0;
                int FreezeCount = 5 - Player.GetCustomStatUnsafe(TextFile.FreezeThreshold);
                int CountMult = 2 + Player.GetCustomStatUnsafe(TextFile.ElectricStack);
                if (Plasma)
                {
                    CountMult += Player.GetCustomStatUnsafe(TextFile.BurnStack);
                    FreezeCount -= Player.GetCustomStatUnsafe(TextFile.BurnAdd);
                }
                if (__instance.NetworkTarget.monsterType != EMonsterType.Boss &&
                    __instance.NetworkTarget.monsterType != EMonsterType.Dummy)
                {
                    FreezeCount = 1;
                }
                int MaxFreezeCount = FreezeCount * CountMult;
                float Damage = Player.GetCustomStatUnsafe(TextFile.IceDamage) + Player.GetCustomStatUnsafe(TextFile.LightningDamage);
                if (Plasma)
                {
                    Damage += Player.GetCustomStatUnsafe(TextFile.FireDamage);
                    Damage /= 2f;
                    Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.BurnDamage) * 0.01f;
                    Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.PlasmaDamage) * 0.01f;
                    if (Player.GetCustomStatUnsafe(TextFile.BurnEvo) > 0)
                        Damage *= 28f / 18f;
                }
                Damage *= 0.4f * MaxFreezeCount;
                Damage *= 1f + MaxFreezeCount / FreezeCount * 0.1f;
                Damage *= 1f + 0.25f * Player.GetCustomStatUnsafe(TextFile.FreezeThreshold);
                Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.FreezeDamage) * 0.01f;
                Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.ElectricDamage) * 0.01f;
                Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.DebuffDuration) * 0.01f * 0.5f;
                return Damage;
            }

            [HarmonyPatch("ApplyDamage")]
            [HarmonyPrefix]
            static bool ApplyDamage_Prefix(CharacterDebuff_Freeze __instance)
            {
                if (!__instance.NetworkAttacker) return true;

                UnitAvatar Player = __instance.NetworkAttacker;
                if (Player.GetCustomStatUnsafe(TextFile.ElectricIce) > 0)
                {
                    bool Plasma = Player.GetCustomStatUnsafe(TextFile.PlasmaActive) > 0;
                    string DamageId = Plasma ? "[Star's Sephiria Mod]等离子结晶" : "[Star's Sephiria Mod]电冰体";
                    float Damage = GetElectricIceDamage(__instance);
                    DamageInstance damage = DamageInstance.GetDamage(Player, DamageId, __instance.NetworkTarget.transform.position, 4294967295L, Damage, EDamageType.ElementalEffectDamage, EDamageFromType.None, Vector2.zero, 0, 0f);
                    damage.elementalType = EDamageElementalType.IceAndLightning;

                    Vector3 vector = __instance.NetworkTarget.transform.position + new Vector3(0f, __instance.NetworkTarget.TopdownActor.size.y * 0.5f);
                    if (Plasma)
                    {
                        damage.elementalType = EDamageElementalType.Chaos;
                        damage.SetCustomColor(true, EffectConstant.PlasmaCrystalColor);
                        RpcChimFxMethod_Plasma?.Invoke(UnitDatabase.GetDebuff("PLASMA"), new object[] { Player.netId, (Vector2)vector });
                    }
                    else RpcChimFxMethod_Electric?.Invoke(CombatManager.Instance.electricDebuffPrefab, new object[] { Player.netId, (Vector2)vector });

                    __instance.NetworkTarget.ApplyDamage(damage);
                    if (__instance.NetworkTarget.StartStun(__instance.BuffDuration))
                    {
                        __instance.NetworkstunApplied = true;
                    }
                    return false;
                }
                return true;
            }
        }

        static void UseFreezeEffect(CharacterDebuff_Frostbite DeBuff)
        {
            UnitAvatar Player = DeBuff.NetworkAttacker;
            if (Player.IsWeaponUseNet(WeaponIDs.FrozenSmelt))
            {
                if (!WeaponPatch.FrozenSmeltEffect.ContainsKey(Player))
                    WeaponPatch.FrozenSmeltEffect[Player] = 10;
                else
                {
                    WeaponPatch.FrozenSmeltEffect[Player] += 10;
                    if (WeaponPatch.FrozenSmeltEffect[Player] > 20)
                        WeaponPatch.FrozenSmeltEffect[Player] = 20;
                }
            }
            if (Player.IsWeaponUseNet(WeaponIDs.GlacialBlade)) 
            {
                float Time = 3f * (1f + Player.GetCustomStatUnsafe("BUFFDURATION") * 0.01f);
                WeaponPatch.GlacialBladeEffect[Player] = new Timer(Time, true, true);
            }
        }

        static void ShootIceBullet(UnitAvatar Target, UnitAvatar Player, int Stack)
        {
            if (Tools.GetPro("CrossbowArrowIce", out var Pro))
            {
                float Rand = UnityEngine.Random.Range(0f, 360f);
                float Damage = Player.GetCustomStatUnsafe(TextFile.IceDamage) * 0.5f;
                Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.FreezeDamage) * 0.01f;
                Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.01f;
                Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 0.01f;
                for (float i = 0f; i < 360f; i += 360f / Stack)
                {
                    Vector2 Offset = HorayUtility.GetVector2FromAngle(i + Rand);
                    Vector3 Pos = Target.transform.position + (Vector3)Offset * 2f;
                    Bullet.Pool.Spawn(Pro, Target.transform.position, true, EDamageFromType.None, "[Star's Sephiria Mod]冰锥", Damage, 0, 0f, Player, Player.GetHostileFactionLayers(EDamageFromType.None), 0.3f, Target.transform.position, Pos, null, null, 0f, EDamageElementalType.Ice);
                }
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

                GameObject textObj = new GameObject("CustomText");
                GameObject textObj3 = new GameObject("CustomText3");
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

        [HarmonyPatch(typeof(NetworkClient))]
        public static class NetworkClient_Patch
        {
            [HarmonyPatch("GetPrefab")]
            [HarmonyPostfix]
            static void GetPrefab_Prefix(uint assetId, ref GameObject prefab, ref bool __result)
            {
                if (StarSephiriaMod.registered.TryGetValue(assetId, out var modPrefab) && modPrefab != null)
                {
                    if (modPrefab is GameObject Target)
                    {
                        prefab = Target;
                        __result = true;
                    }
                }
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

            [HarmonyPatch("OnOpened")]
            [HarmonyPostfix]
            private static void OnOpened_Postfix(UI_BossHPBar __instance)
            {
                if (__instance == null || __instance.barImage == null) return;

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

                UnitAI_BossBasic bossAI = (UnitAI_BossBasic)bossAIField.GetValue(__instance);
                if (bossAI != null)
                {
                    __instance.nameText.text = KeywordDatabase.Convert(bossAI.Avatar.Name, false, false);
                    if (bossAI.CurrentTarget != null)
                    {
                        __instance.nameText.text += " -> " + bossAI.CurrentTarget.Name;
                    }
                }

                UnitAvatar bossAvatar = (UnitAvatar)bossAvatarField.GetValue(__instance);
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

        [HarmonyPatch(typeof(Bullet))]
        public static class Bullet_Patch
        {
            [HarmonyPatch("Initialize")]
            [HarmonyPostfix]
            static void Initialize_Postfix(Bullet __instance, float damage, UnitAvatar owner)
            {
                if (__instance.gameObject != null && DisplayCustomStats)
                {
                    string prefabName = __instance.gameObject.name.Replace("(Clone)", "");
                    StarSephiriaMod.LogInfo($"子弹生成: {prefabName}, 伤害：{damage}, 来源：{owner?.name}");
                    StarSephiriaMod.LogInfo($"子弹移动类型：{__instance.MoveModule.GetType()}");
                    StarSephiriaMod.LogInfo($"子弹死亡类型：{__instance.DestroyModule.GetType()}");
                    if (!Projectile.ContainsKey(prefabName))
                    {
                        Projectile.Add(prefabName, __instance.gameObject);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MeleeCollision))]
        public static class MeleeCollision_Patch
        {
            [HarmonyPatch("Initialize")]
            [HarmonyPostfix]
            static void Initialize_Postfix(ProjectileBase __instance, float damage, UnitAvatar owner)
            {
                if (__instance.gameObject != null && DisplayCustomStats)
                {
                    string prefabName = __instance.gameObject.name.Replace("(Clone)", "");
                    StarSephiriaMod.LogInfoBoss($"近战生成: {prefabName}, 伤害：{damage}, 来源：{owner?.name}");
                    if (!Projectile.ContainsKey(prefabName))
                    {
                        Projectile.Add(prefabName, __instance.gameObject);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(LocalizedString))]
        public static class LocalizedString_Patch
        {
            public static string GetMod(string Key)
            {
                string Text = Tools.GetLocalization(Key);
                if (Text == null) return "";

                Text = KeywordDatabase.Convert(KeywordDatabase.Convert(Text));
                return Text;
            }

            [HarmonyPatch("ToString")]
            [HarmonyPostfix]
            static void ToString_Postfix(LocalizedString __instance, ref string __result)
            {
                string ModText = GetMod(__result);
                if (ModText != "")
                    __result = ModText;
            }
        }

        [HarmonyPatch(typeof(ScreenFader))]
        public static class ScreenFader_Patch
        {
            private static readonly PropertyInfo? TestField = AccessTools.Property(typeof(ScreenFader), "IsTestMode");

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            static void Update_Postfix(ScreenFader __instance)
            {
                TestField?.SetValue(__instance, ModConfig.Instance.IsTestMode);
            }
        }

        [HarmonyPatch(typeof(Object))]
        public static class Object_Patch
        {
            [HarmonyPatch("Internal_InstantiateSingle")]
            [HarmonyPostfix]
            static void Internal_InstantiateSingle_Postfix(ref Object __result, Object data, Vector3 pos, Quaternion rot)
            {
                __result = TryPatch(__result);
            }

            [HarmonyPatch("Internal_CloneSingle", new Type[] { typeof(Object) })]
            [HarmonyPostfix]
            static void Internal_CloneSingle_Postfix(ref Object __result, Object data)
            {
                __result = TryPatch(__result);
            }

            [HarmonyPatch("Instantiate", new Type[] { typeof(Object), typeof(Vector3), typeof(Quaternion), typeof(Transform) })]
            [HarmonyPostfix]
            static void Instantiate_Postfix(ref Object __result, Object original, Vector3 position, Quaternion rotation, Transform parent)
            {
                __result = TryPatch(__result);
            }

            [HarmonyPatch("Instantiate", new Type[] { typeof(Object), typeof(Vector3), typeof(Quaternion) })]
            [HarmonyPostfix]
            static void Instantiate_Postfix(ref Object __result, Object original, Vector3 position, Quaternion rotation)
            {
                __result = TryPatch(__result);
            }

            [HarmonyPatch("Instantiate", new Type[] { typeof(Object), typeof(Transform), typeof(bool) })]
            [HarmonyPostfix]
            static void Instantiate_Postfix(ref Object __result, Object original, Transform parent, bool instantiateInWorldSpace)
            {
                __result = TryPatch(__result);
            }

            [HarmonyPatch("Instantiate", new Type[] { typeof(Object), typeof(Transform) })]
            [HarmonyPostfix]
            static void Instantiate_Postfix(ref Object __result, Object original, Transform parent)
            {
                __result = TryPatch(__result);
            }

            public static Object TryPatch(Object clone)
            {
                GameObject? Target;
                if (clone is GameObject go)
                {
                    Target = go;
                }
                else if (clone is Component comp)
                {
                    Target = comp.gameObject;
                }
                else
                {
                    return clone;
                }

                ModAssetId AssetId = Target.GetComponent<ModAssetId>();
                if (Target.GetComponent<NetworkIdentity>() == null && AssetId != null)
                {
                    NetworkIdentity netId = Target.AddComponent<NetworkIdentity>();
                    SetModAssetId(netId, AssetId.AssetId);

                    StarSephiriaMod.LogInfo($"已经为复制体 {Target.name} 挂上NetworkIdentity");
                    if (Target.GetComponent<ComboEffectBase>() is ComboEffectBase combo)
                    {
                        combo.enabled = true;
                    }
                    if (Target.GetComponent<Charm_Basic>() is Charm_Basic charm)
                    {
                        charm.enabled = true;
                    }
                    if (Target.GetComponent<ChargingCharm>() is ChargingCharm Charging)
                    {
                        Charging.enabled = true;
                    }
                    if (Target.GetComponent<CharacterDebuff>() is CharacterDebuff DeBuff)
                    {
                        DeBuff.enabled = true;
                    }
                }
                if (Target.GetComponent<ModSpawnTemplate>() != null && !Target.activeSelf)
                {
                    Target.SetActive(true);
                }
                return clone;
            }
        }

        [HarmonyPatch(typeof(PotionEffect_ElementalDamageBoost))]
        public static class PotionEffect_ElementalDamageBoost_Patch
        {
            [HarmonyPatch("SetStatusEffect")]
            [HarmonyPrefix]
            static bool SetStatusEffect_Prefix(PotionEffect_ElementalDamageBoost __instance, UnitAvatar avatar)
            {
                Tools.GetHighestDamageElemental(avatar, out string BaseName);
                if (!Tools.GetKeyWordToStatus(BaseName, out string StatusId)) return true;
                int Type = EffectConstant.BaseStatsName.IndexOf(BaseName);

                avatar.AddOrphanedStatusInstance(StatusDatabase.CreateStatusEntity(StatusId, __instance.potionEffectValue));
                if (avatar.connectionToClient != null)
                    __instance.TargetShowEffect(avatar.connectionToClient, avatar.transform.position, Type);
                return false;
            }

            [HarmonyPatch("UserCode_TargetShowEffect__NetworkConnectionToClient__Vector2__Int32")]
            [HarmonyPrefix]
            static bool UserCode_TargetShowEffect_Prefix(PotionEffect_ElementalDamageBoost __instance, Vector2 pos, int type)
            {
                if (type != 4) return true;

                StatusInstance Status = StatusDatabase.CreateStatusEntity("WIND_DAMAGE", __instance.potionEffectValue);
                Loc.KeywordValue[] Keywords =
                {
                    new Loc.KeywordValue("STAT", Status.ToNameString(color: false, sprite: false)),
                    new Loc.KeywordValue("VALUE", Status.ToValueString())
                };
                GameLogWriter.Instance.WriteLog(Loc.Convert(__instance.statLogString.ToString(), Keywords).Trim(), Color.white);
                UIManager.Pool.Spawn<UI_DamageParticle>(pos + UnityEngine.Random.insideUnitCircle)
                    .SetDamage(Status.ToSimpleString(reverse: true).Trim(), Color.white);
                return false;
            }
        }

        [HarmonyPatch(typeof(SpriteFx_Simple))]
        public static class SpriteFx_Simple_Patch
        {
            [HarmonyPatch("SetColor")]
            [HarmonyPostfix]
            static void SetColor_Postfix(SpriteFx_Simple __instance, Color color)
            {
                if (__instance == null) return;
                ModFxVisibility? Vis = __instance.GetComponent<ModFxVisibility>();
                Vis?.SetBaseColor(color);
            }
        }
    }
}