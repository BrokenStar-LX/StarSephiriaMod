using HarmonyLib;
using Mirror;
using SPMod.Tool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static SPMod.StarSephiriaMod;
using static SPMod.StarSephiriaPatches;
using static SPMod.Tool.ModuleComponents;
using static SPMod.Tool.WeaponIDs;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UI.GridLayoutGroup;
using Color = UnityEngine.Color;
using Random = System.Random;

namespace SPMod
{
    [HarmonyPatch]
    public static class WeaponPatch
    {
        public static Dictionary<(string, int), (string, int)> DamageIdx = new Dictionary<(string, int), (string, int)>();
        public static Dictionary<string, float> GrippingFearDamageAdd = new Dictionary<string, float>();
        public static Dictionary<string, bool> UsePlanetShoot = new Dictionary<string, bool>();
        public static Dictionary<string, bool> IceBowAttack = new Dictionary<string, bool>();
        public static Dictionary<UnitAvatar, Timer> DraftbladeAttack = new Dictionary<UnitAvatar, Timer>();
        public static Dictionary<UnitAvatar, int> SilenceofEverfrostAttack = new Dictionary<UnitAvatar, int>();
        public static Dictionary<UnitAvatar, bool> SilenceofEverfrostCharm = new Dictionary<UnitAvatar, bool>();
        public static Dictionary<UnitAvatar, float> SilenceofEverfrostCharmDamage = new Dictionary<UnitAvatar, float>();
        public static Dictionary<string, int> MinimalismDashAttack = new Dictionary<string, int>();
        public static Dictionary<string, bool> TheSecondDashAttack = new Dictionary<string, bool>();
        public static Dictionary<string, int> LightningFlashBuff = new Dictionary<string, int>();
        public static Dictionary<UnitAvatar, CharacterBuff> DarkVeilBuff = new Dictionary<UnitAvatar, CharacterBuff>();
        public static Dictionary<string, int> HeidiDashAttack = new Dictionary<string, int>();
        public static Dictionary<UnitAvatar, int> LightningsWrathEffect = new Dictionary<UnitAvatar, int>();
        public static Dictionary<UnitAvatar, Timer> LightningsWrathEffect2 = new Dictionary<UnitAvatar, Timer>();
        public static Dictionary<string, int> RuadhsleaSpearBuff = new Dictionary<string, int>();
        public static Dictionary<string, Timer> RuadhsleaSpearTime = new Dictionary<string, Timer>();
        public static Dictionary<string, int> WeightOfTheSeaBuff = new Dictionary<string, int>();
        public static Dictionary<string, bool> BlazingHelbanusSpeed = new Dictionary<string, bool>();
        public static Dictionary<string, int> YuisShortDaggerMagic = new Dictionary<string, int>();
        public static Dictionary<string, float> YuisShortDaggerTime = new Dictionary<string, float>();
        public static Dictionary<string, int> SolisMissioBuff = new Dictionary<string, int>();
        public static Dictionary<UnitAvatar, Timer> FiaolasJudgmentStaffBuff = new Dictionary<UnitAvatar, Timer>();
        public static Dictionary<string, int> PristineFormAttackCount = new Dictionary<string, int>();
        public static Dictionary<string, Timer> TomeOfMimicryEffectTime = new Dictionary<string, Timer>();
        public static Dictionary<UnitAvatar, Timer> TomeOfMimicryAddTime = new Dictionary<UnitAvatar, Timer>();
        public static Dictionary<string, LegacyofTheIllusionistAttackData> LegacyofTheIllusionistAttack = new Dictionary<string, LegacyofTheIllusionistAttackData>();
        public static Dictionary<UnitAvatar, Timer> InvincibleSpecialAttackCD = new Dictionary<UnitAvatar, Timer>();
        public static Dictionary<UnitAvatar, int> FrozenSmeltEffect = new Dictionary<UnitAvatar, int>();
        public static Dictionary<string, bool> CloudGathererEffect = new Dictionary<string, bool>();
        public static Dictionary<UnitAvatar, int> TheBarnacleEffect = new Dictionary<UnitAvatar, int>();
        public static Dictionary<UnitAvatar, SuccessorBoltData> SuccessorEffect = new Dictionary<UnitAvatar, SuccessorBoltData>();
        public static Dictionary<UnitAvatar, Timer> GlacialBladeEffect = new Dictionary<UnitAvatar, Timer>();
        public static Dictionary<UnitAvatar, Timer> GlobalTimer = new Dictionary<UnitAvatar, Timer>();
        public static Dictionary<UnitAvatar, int> AmmoStack = new Dictionary<UnitAvatar, int>();
        public static Dictionary<UnitAvatar, float> ThrowSwordTime = new Dictionary<UnitAvatar, float>();
        public static Dictionary<UnitAvatar, Timer> GardenofNeedleIceEffect = new Dictionary<UnitAvatar, Timer>();
        public static Dictionary<UnitAvatar, Timer> LightningsWrathTime = new Dictionary<UnitAvatar, Timer>();
        public static Dictionary<UnitAvatar, int> XRA9Effect = new Dictionary<UnitAvatar, int>();
        public static Dictionary<UnitAvatar, int> MarkStabilizationEffect = new Dictionary<UnitAvatar, int>();
        public static Dictionary<UnitAvatar, float> OverhangingStormCloudEffect = new Dictionary<UnitAvatar, float>();

        public static event Action? OnCrossbowUseReLoad = null;
        private static void InitializeModEffect(WeaponSimple __instance)
        {
            UnitAvatar Player = __instance.Networkowner.unitAvatar;
            LightningsWrathEffect.SafeRemove(Player);
            LightningsWrathEffect2.SafeRemove(Player);
            DarkVeilBuff.SafeRemove(Player);
            FrozenSmeltEffect.SafeRemove(Player);
            TheBarnacleEffect.SafeRemove(Player);
            SuccessorEffect.SafeRemove(Player);
            GlacialBladeEffect.SafeRemove(Player);
            ThrowSwordTime.SafeRemove(Player);
            SilenceofEverfrostAttack.SafeRemove(Player);
            SilenceofEverfrostCharm.SafeRemove(Player);
            SilenceofEverfrostCharmDamage.SafeRemove(Player);
            if (WeaponPatch.MarkStabilizationEffect.ContainsKey(Player))
            {
                WeaponPatch.MarkStabilizationEffect.SafeRemove(Player);
                Player.DestroyEffectHUD("MarkStabilizationCharge");
            }

            if (!ModConfig.Instance.EnableWeaponModifier) return;
            if (__instance.entityId == WeaponIDs.GrippingFear)
            {
                if (!GrippingFearBuff.Add(Player))
                    Player.additionalPerfectGuardTime += Player.perfectGuardTime * 0.5f;
            }
            else
            {
                if (GrippingFearBuff.Remove(Player))
                    Player.additionalPerfectGuardTime -= Player.perfectGuardTime * 0.5f;
            }
        }

        [HarmonyPatch(typeof(WeaponControllerSimple))]
        public static class WeaponControllerSimple_Patches
        {
            [HarmonyPatch("EquipWeapon")]
            [HarmonyPostfix]
            static void EquipWeapon_Postfix(WeaponControllerSimple __instance, bool fromTownObject, int weaponID, bool showFx = false)
            {
                if (__instance.unitAvatar)
                    WeaponType[__instance.unitAvatar.Name] = weaponID;
            }
        }

        [HarmonyPatch(typeof(WeaponSimple))]
        public static class WeaponSimple_Patches
        {
            [HarmonyPatch("OnStartClient")]
            [HarmonyPostfix]
            static void OnStartClient_Postfix(WeaponSimple __instance)
            {
                __instance.StartCoroutine(DelayInit(__instance));
            }

            private static IEnumerator DelayInit(WeaponSimple __instance)
            {
                if (__instance == null || __instance.Networkowner == null || __instance.Networkowner.unitAvatar == null || __instance.Networkowner.currentWeapon == null)
                {
                    yield break;
                }
                try
                {
                    UnitAvatar Player = __instance.Networkowner.unitAvatar;
                    int Id = __instance.Networkowner.currentWeapon.NetworkentityId;
                    string Name = "未获取名字";
                    var weaponEntity = WeaponDatabase.GetAll();
                    foreach (WeaponEntity weapon in weaponEntity)
                    {
                        if (weapon.id == Id)
                        {
                            Name = weapon.aName.ToString();
                            break;
                        }
                    }
                    LogInfoWeapon($"{Name} 的概率数据：{__instance.AttackWeightPerSwing}");

                    WeaponType[Player.Name] = Id;
                    __instance.Networkowner.animator.speed = 1f;

                    InitializeModEffect(__instance);
                    LogInfoWeapon($"玩家{Player.Name} ID:{Player.netId} __持有武器:{Name} ID:{Id}");
                    foreach (var addon in __instance.addons)
                    {
                        if (ModConfig.Instance.EnableWeaponModifier) ModifiedWeaponCommon(addon, __instance, Id);
                        LogInfoWeapon($"—武器附加组件: {addon.GetType().Name}");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[WeaponSimple][OnStartClient]错误: {ex}");
                }
            }

            [HarmonyPatch("GetBasicAttack")]
            [HarmonyPostfix]
            static void GetBasicAttack_Postfix(WeaponSimple __instance, int idx, ref NewWeaponFireData __result)
            {
                DamageIdx[(__instance.Networkowner.unitAvatar.Name, __instance.entityId)] = ("Basic", idx);
                LogInfoProjectile($"GetBasicAttack 被调用 攻击索引:{idx}");

                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player.IsWeapon(WeaponIDs.Twin) && __instance.entityId == WeaponIDs.Twin)
                {
                    __result.attackDashSpeed = 0f;
                    __result.attackDashTime = 0f;
                }//双子
            }

            [HarmonyPatch("GetSpecialAttack")]
            [HarmonyPostfix]
            static void GetSpecialAttack_Postfix(WeaponSimple __instance, int idx, ref NewWeaponFireData __result)
            {
                DamageIdx[(__instance.Networkowner.unitAvatar.Name, __instance.entityId)] = ("Special", idx);
                LogInfoProjectile($"GetSpecialAttack 被调用 攻击索引:{idx}");
            }

            [HarmonyPatch("GetDashAttack")]
            [HarmonyPostfix]
            static void GetDashAttack_Postfix(WeaponSimple __instance, int idx, ref NewWeaponFireData __result)
            {
                DamageIdx[(__instance.Networkowner.unitAvatar.Name, __instance.entityId)] = ("Dash", idx);
                LogInfoProjectile($"GetSpecialAttack 被调用 攻击索引:{idx}");
            }

            [HarmonyPatch("OnAttack_Basic")]
            [HarmonyPostfix]
            static void OnAttack_Basic_Postfix(WeaponSimple __instance, CombatBehaviour victim, DamageInstance damage, ProjectileBase projectile)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player == null) return;

                if (Player.IsWeapon(WeaponIDs.Compliance) && __instance.entityId == WeaponIDs.Compliance)
                {
                    if (projectile.forceFinalCombo)
                        __instance.owner?.OnSpecialAttack?.Invoke(victim, damage, projectile);
                }
                if (Player.IsWeapon(WeaponIDs.FrostboundGreatsword, WeaponIDs.EternalFrostGreatsword, WeaponIDs.GreatswordOfExorcism))
                {
                    if (victim is UnitAvatar Target)
                        Target.ApplyDebuff(CombatManager.Instance.frostbiteDebuffPrefab, Player);
                }
            }

            [HarmonyPatch("OnAttack_Dash")]
            [HarmonyPostfix]
            static void OnAttack_Dash_Postfix(WeaponSimple __instance, CombatBehaviour victim, DamageInstance damage, ProjectileBase projectile)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player == null) return;

                if (Player.IsWeapon(WeaponIDs.WanderingAshes) && __instance.entityId == WeaponIDs.WanderingAshes)
                {
                    __instance.owner?.OnBasicAttack?.Invoke(victim, damage, projectile);
                }
                if (Player.IsWeapon(WeaponIDs.Rapier2) && __instance.entityId == WeaponIDs.Rapier2)
                {
                    __instance.owner?.OnBasicAttack?.Invoke(victim, damage, projectile);
                }//刺剑
            }

            [HarmonyPatch("OnAttack_Special")]
            [HarmonyPostfix]
            static void OnAttack_Special_Postfix(WeaponSimple __instance, CombatBehaviour victim, DamageInstance damage, ProjectileBase projectile)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player == null) return;

                if (Player.IsWeapon(WeaponIDs.FamedSwordMuramasa) && __instance.entityId == WeaponIDs.FamedSwordMuramasa)
                {
                    __instance.owner?.OnBasicAttack?.Invoke(victim, damage, projectile);
                }//妖刀村正
                if (Player.IsWeapon(WeaponIDs.StoneSword) && __instance.entityId == WeaponIDs.StoneSword)
                {
                    if (victim is UnitAvatar Target)
                    {
                        ShootStoneSwordPro(Player, Target);
                    }
                }//岩石之剑
                if (Player.IsWeapon(WeaponIDs.BlazingHelbanus) && __instance.entityId == WeaponIDs.BlazingHelbanus)
                {
                    if (victim is UnitAvatar Target)
                    {
                        bool falg = true;
                        bool BurnisTrue = false;
                        if (Player.GetCustomStatUnsafe(TextFile.PlasmaActive) > 0)
                        {
                            foreach (CharacterDebuff_Plasma Debuff in Target.Debuffs.OfType<CharacterDebuff_Plasma>())
                            {
                                if (Debuff.NetworkAttacker == Player)
                                {
                                    BurnisTrue = true;
                                    if (Debuff.CurrentStack < Debuff.MaxStackCount)
                                    {
                                        falg = false;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (CharacterDebuff_Burn Debuff in Target.Debuffs.OfType<CharacterDebuff_Burn>())
                            {
                                if (Debuff.NetworkAttacker == Player)
                                {
                                    BurnisTrue = true;
                                    if (Debuff.CurrentStack < Debuff.MaxStackCount)
                                    {
                                        falg = false;
                                        break;
                                    }
                                }
                            }
                        }
                        if (!falg || !BurnisTrue)
                        {
                            Target.ApplyDebuff(CombatManager.Instance.burnDebuffPrefab, Player);
                            BlazingHelbanusSpeed[Player.Name] = true;
                        }
                    }
                }//赫尔巴努斯
                if (Player.IsWeapon(WeaponIDs.LightningGreatSwordE3G) && __instance.entityId == WeaponIDs.LightningGreatSwordE3G)
                {
                    if (victim is UnitAvatar Target)
                    {
                        float Mult = 1f + Player.GetCustomStatUnsafe(TextFile.SpecialAttackDamageBonus) * 0.01f;
                        Mult *= 1f + Player.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                        int Count = Math.Max(1, Player.GetCustomStatUnsafe(TextFile.DarkCloudMultishot));
                        ModCloudAttack.Add(new CloudAttackData(Player, Target, 0.8f * Mult, 0.24f / Count, 4 * Count));
                    }
                }//乌云大剑
                if (Player.IsWeaponUseNet(WeaponIDs.TheBarnacle) && __instance.entityId == WeaponIDs.TheBarnacle)
                {
                    if (!TheBarnacleEffect.ContainsKey(Player))
                        TheBarnacleEffect[Player] = 1;
                    else
                    {
                        TheBarnacleEffect[Player] += 1;
                    }
                }//藤壶
                if (Player.IsWeaponUseNet(WeaponIDs.ScaleOfTheAstralSea) && __instance.entityId == WeaponIDs.ScaleOfTheAstralSea)
                {
                    if(Player.GetCustomStatUnsafe(TextFile.ScaleOfTheAstralSeaEffect) < 10)
                        Player.AddCustomStatUnsafe(TextFile.ScaleOfTheAstralSeaEffect, 1);
                }//圣海碎片
                if (Player.IsWeaponUseNet(WeaponIDs.BlindingSilence) && __instance.entityId == WeaponIDs.BlindingSilence)
                {
                    if (victim is UnitAvatar Target)
                        Target.ApplyDebuff(CombatManager.Instance.electricDebuffPrefab, Player);
                }//耀目的寂静
            }

            [HarmonyPatch("BasicAttackCreated")]
            [HarmonyPrefix]
            static void BasicAttackCreated_Prefix(WeaponSimple __instance, ref int idx, ProjectileBase projectile)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player == null) return;

                if (Player.IsWeapon(WeaponIDs.SwiftRegret, WeaponIDs.ExplosiveDevice))
                {
                    idx = __instance.finalComboIdx;
                    projectile.forceFinalCombo = true;
                }//短暂自责 爆炸装置
                if (Player.IsWeapon(WeaponIDs.BoneGearblade, WeaponIDs.CreakingSpine, WeaponIDs.BloodlettingGearblade))
                {
                    if (idx == 2 || idx == 4)
                    {
                        idx = __instance.finalComboIdx;
                        projectile.forceFinalCombo = true;
                    }
                }//骨剑
                if (Player.IsWeapon(WeaponIDs.BreakplateBlade) && __instance.entityId == WeaponIDs.BreakplateBlade)
                {
                    if (idx == 2 && __instance.finalComboIdx == 3)
                    {
                        idx = __instance.finalComboIdx;
                        projectile.forceFinalCombo = true;
                    }
                }//破甲刀
                if (Player.IsWeapon(WeaponIDs.Compliance) && __instance.entityId == WeaponIDs.Compliance)
                {
                    if (idx == __instance.finalComboIdx)
                    {
                        if (Player.GetCustomStatUnsafe(TextFile.InvincinleSpiceialAttack) > 0)
                        {
                            if (!InvincibleSpecialAttackCD.ContainsKey(Player))
                            {
                                __instance.Networkowner.StartParryInvincible(0.6f);
                                InvincibleSpecialAttackCD.Add(Player, new Timer(1.2f));
                            }
                        }
                    }
                }//顺从的姿态
                if (Player.IsWeapon(WeaponIDs.Malaspina) && __instance.entityId == WeaponIDs.Malaspina)
                {
                    __instance.Networkowner.OnSpecialAttackSwing?.Invoke(idx);
                }//马拉斯皮纳
                if (Player.IsWeapon(WeaponIDs.AbsoluteEnvy) && __instance.entityId == WeaponIDs.AbsoluteEnvy)
                {
                    if (idx >= 2)
                    {
                        idx = __instance.finalComboIdx;
                        projectile.forceFinalCombo = true;
                    }
                }//绝对向往
            }

            [HarmonyPatch("OnDashAttackCreated")]
            [HarmonyPostfix]
            static void OnDashAttackCreated_Postfix(WeaponSimple __instance, int idx, ProjectileBase projectile)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player == null) return;

                if (Player.IsWeapon(WeaponIDs.Rapier2) && __instance.entityId == WeaponIDs.Rapier2)
                {
                    __instance.owner?.OnBeginAttackAnimation?.Invoke(0);
                }//刺剑
            }

            [HarmonyPatch("CreateSpecialSwingFx")]
            [HarmonyPrefix]
            static bool CreateSpecialSwingFx_Prefix(WeaponSimple __instance, int idx, ref Vector3 position, Vector3 eulerAngles)
            {
                if (!NetworkClient.active) return true;
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player == null) return true;

                if (Player.IsWeapon(WeaponIDs.MarkStabilization) && __instance.entityId == WeaponIDs.MarkStabilization)
                {
                    float Mult = 1f + Player.GetCustomStatUnsafe(TextFile.WeaponRange) * 0.01f;
                    position += __instance.Networkowner.attackDirection.normalized * 3f * Mult;
                }
                return true;
            }

            [HarmonyPatch("CreateDashSwingFx")]
            [HarmonyPrefix]
            static bool CreateDashSwingFx_Prefix(WeaponSimple __instance, int idx, Vector3 position, Vector3 eulerAngles)
            {
                if (!NetworkClient.active) return true;
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player == null) return true;

                if (Player.IsWeapon(WeaponIDs.Heidi) && __instance.entityId == WeaponIDs.Heidi)
                {
                    if (HeidiDashAttack.TryGetValue(Player.Name, out int Count) && Count > 6)
                    {
                        float Scale = 1f + Player.GetCustomStat(TextFile.WeaponRange) * 0.01f;
                        NewWeaponFireData dashAttack = __instance.GetDashAttack(idx);
                        position += new Vector3(0f, __instance.Networkowner.shoulder.Position.y, 0f);
                        dashAttack.CreateSwingFx(true, __instance.Networkowner.transform, position, eulerAngles, Scale + 0.5f, __instance.Networkowner.isOwned ? 1 : 0, 0f);
                        return false;
                    }
                }
                return true;
            }
        }

        public static void ShootStoneSwordPro(UnitAvatar Player, UnitAvatar Target, float Mult = 1f)
        {
            if (Tools.GetPro("OinkShamanStoningBullet", out var Pro))
            {
                float Damage = 40 + Player.GetCustomStatUnsafe(TextFile.DamageReduction) * 1.5f;
                Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.MPSkillDamage) * 0.01f;
                Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.SpecialAttackDamageBonus) * 0.01f;
                Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                Damage *= Mult;
                Vector3 Pos = Target.transform.position;
                Bullet.Pool.Spawn(Pro, Target.transform.position, true, EDamageFromType.DirectAttack, "[Star's Sephiria Mod]落石", Damage, 0, 0f, Player, Player.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0.3f, Target.transform.position, Pos, null, null, 0f, EDamageElementalType.Physical);
            }
        }

        [HarmonyPatch(typeof(WeaponSimple_Dagger))]
        public static class WeaponSimple_Dagger_Patches
        {
            public static FieldInfo? daggerBarField = AccessTools.Field(typeof(WeaponSimple_Dagger), "daggerBar");
            public static MethodInfo? AddFuryMethod = AccessTools.Method(typeof(WeaponSimple_Dagger), "AddFury");
            public static MethodInfo? CreateFuryChargedParticleMethod = AccessTools.Method(typeof(WeaponSimple_Dagger), "CreateFuryChargedParticle");
            public static MethodInfo? TargetFuryChargedMessage_EvasionMethod = AccessTools.Method(typeof(WeaponSimple_Dagger), "TargetFuryChargedMessage_Evasion");
            public static Dictionary<int, Color> DaggerFuryColor = new Dictionary<int, Color>();

            static void ModAddFurey(WeaponSimple_Dagger Dagger, int Stack, Color color)
            {
                DaggerFuryColor[Dagger.entityId] = color;
                if (AddFuryMethod != null && CreateFuryChargedParticleMethod != null && TargetFuryChargedMessage_EvasionMethod != null)
                {
                    AddFuryMethod.Invoke(Dagger, new object[] { 1 });
                    Dagger.furyDisableTimer.SetTimer(0f);
                    Dagger.Networkowner.unitAvatar.CreateEffectHUD("IMMERSION", "DaggerImmersion");
                    Dagger.NetworkevasionFuryCounting = false;
                    if (Dagger.connectionToClient == null)
                    {
                        CreateFuryChargedParticleMethod.Invoke(Dagger, new object[] { color });
                    }
                    else
                    {
                        TargetFuryChargedMessage_EvasionMethod.Invoke(Dagger, new object[] { Dagger.connectionToClient });
                    }
                }
            }

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            static void Update_Postfix(WeaponSimple_Dagger __instance)
            {
                if (!NetworkServer.active) return;

                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player.IsWeaponUseNet(WeaponIDs.TomeOfMimicry) && __instance.entityId == WeaponIDs.TomeOfMimicry)
                {
                    __instance.furyDisableTimer.time = 999999f;
                    if (TomeOfMimicryEffectTime.TryGetValue(Player.Name, out Timer Value))
                    {
                        if (Value.Update(Time.deltaTime))
                        {
                            TomeOfMimicryEffectTime.SafeRemove(Player.Name);
                        }
                    }
                    else if (__instance.currentFury <= 0)
                    {
                        if (!TomeOfMimicryAddTime.ContainsKey(Player))
                        {
                            ModAddFurey(__instance, 1, EffectConstant.MagicColor);
                            Player.HealMp(10);
                            TomeOfMimicryAddTime[Player] = new Timer(3f);
                        }
                    }
                }//模仿之书
                if (Player.IsWeapon(WeaponIDs.SafeStart, WeaponIDs.LightlessBlade, WeaponIDs.PaleSmile))
                {
                    __instance.wideParry = true;
                    __instance.easyParry = true;
                }//安全的开始
                if (Player.IsWeapon(WeaponIDs.SolisImber) && __instance.entityId == WeaponIDs.SolisImber)
                {
                    if (__instance.currentFury <= 0)
                    {
                        if (Player.Inventory && !SolisImberEffect.ContainsKey(Player))
                        {
                            ComboEffectBase Combo = Player.Inventory.FindComboEffect("FLAMESWORD");
                            if (Combo != null && Combo is ComboEffect_FlameSword FlameSword)
                            {
                                int Max = FlameSword.maxSword + Player.GetCustomStatUnsafe(TextFile.FlameSwordMax);
                                if (FlameSword.NetworkcurrentSword >= Max)
                                {
                                    __instance.furyDisableTimer.time = 999999f;
                                    ModAddFurey(__instance, 1, EffectConstant.FireColor);
                                    SolisImberEffect[Player] = new Timer(3f);
                                }
                            }
                        }
                    }
                }//索利斯·因贝尔
                if (Player.IsWeaponUseNet(WeaponIDs.LightningsWrath) && __instance.entityId == WeaponIDs.LightningsWrath)
                {
                    if (LightningsWrathEffect.TryGetValue(Player, out int Value))
                    {
                        if (Value >= 40)
                        {
                            if (__instance.currentFury <= 0 && !LightningsWrathTime.ContainsKey(Player))
                            {
                                LightningsWrathTime[Player] = new Timer(5f);
                                ModAddFurey(__instance, 1, EffectConstant.LightningColor);
                            }
                        }
                    }
                }//雷电之怒
                if (Player.IsWeaponUseNet(WeaponIDs.OverhangingStormCloud) && __instance.entityId == WeaponIDs.OverhangingStormCloud)
                {
                    if (OverhangingStormCloudEffect.ContainsKey(Player))
                    {
                        float MaxTime = 3.2f;
                        if (__instance.currentFury <= 0)
                        {
                            if (OverhangingStormCloudEffect[Player] < MaxTime)
                            {
                                float Mult = 1f + Player.GetCustomStatUnsafe(TextFile.DarkCloudSpeed) * 0.01f;
                                float AtkMult = Player.GetCustomStatUnsafe(TextFile.DarkCloudAtkSpeedBonus) * 0.01f;
                                Mult += Player.GetCustomStatUnsafe(TextFile.AttackSpeed) * 0.01f * AtkMult;
                                OverhangingStormCloudEffect[Player] += Time.deltaTime * Mult;
                            }
                            else
                            {
                                ModAddFurey(__instance, 1, EffectConstant.LightningColor);
                                OverhangingStormCloudEffect[Player] = 0f;
                            }
                        }
                        if (__instance.isClient && daggerBarField != null)
                        {
                            UI_DaggerBar daggerBar = (UI_DaggerBar)daggerBarField.GetValue(__instance);
                            if (daggerBar && __instance.Networkowner && __instance.Networkowner.isOwned)
                            {
                                if (__instance.currentFury > 0)
                                {
                                    daggerBar.immersionRatio = 1f;
                                }
                                else
                                {
                                    float Ratio = Mathf.Clamp01(OverhangingStormCloudEffect[Player] / MaxTime);
                                    daggerBar.immersionRatio = Ratio;
                                }
                                daggerBar.transform.position = __instance.Networkowner.transform.position;
                            }
                        }
                    }
                    else OverhangingStormCloudEffect[Player] = 0f;
                }
            }

            [HarmonyPatch("HandleParry")]
            [HarmonyPrefix]
            static bool HandleParry_Prefix(WeaponSimple_Dagger __instance, DamageInstance damage)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player.IsWeapon(WeaponIDs.UnboundImmersion, WeaponIDs.TomeOfMimicry, WeaponIDs.HeatedBlade, WeaponIDs.Drífa, WeaponIDs.OverhangingStormCloud))
                {
                    return false;
                }
                if (Player.IsWeapon(WeaponIDs.PaleSmile))
                {
                    Player.AddShield(10, "PaleSmileShield");
                }
                if (Player.IsWeapon(WeaponIDs.LightlessBlade))
                {
                    if (damage.origin is UnitAvatar Target)
                    {
                        if (Target == Player) return false;

                        string Id = "[Star's Sephiria Mod]影荆棘";
                        float EffectDamage = Player.GetCustomStatUnsafe(TextFile.DamageReduction) * Player.GetCustomStatUnsafe(TextFile.Thorns) * 0.01f;
                        EffectDamage *= Player.GetCustomStatUnsafe(TextFile.PhysicalDamage) * 0.01f;
                        EffectDamage *= 1f + Player.GetCustomStatUnsafe(TextFile.SpecialAttackDamageBonus) * 0.01f;
                        EffectDamage *= 1f + Player.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                        EffectDamage *= 1f + Player.GetCustomStatUnsafe(TextFile.DamageReduction) * 0.01f / 2f;
                        if (EffectDamage > 0)
                        {
                            var Dam = DamageInstance.GetDamage(Player, Id, Target.transform.position, 4294967295L, EffectDamage, EDamageType.Slice, EDamageFromType.DirectAttack, Vector2.zero, 0, 0f);
                            Target.ApplyDamage(Dam);
                        }
                    }
                    return false;
                }
                return true;
            }

            [HarmonyPatch("HandleEvade")]
            [HarmonyPostfix]
            static void HandleEvade_Postfix(WeaponSimple_Dagger __instance, DamageInstance damage)
            {
                if (AddFuryMethod == null || CreateFuryChargedParticleMethod == null || TargetFuryChargedMessage_EvasionMethod == null)
                {
                    LogError($"[WeaponSimple_Dagger][HandleEvade]获取反射失败");
                    return;
                }

                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player.IsWeapon(WeaponIDs.JadeVestige, WeaponIDs.UnboundImmersion))
                {
                    AddFuryMethod.Invoke(__instance, new object[] { 1 });
                    __instance.furyDisableTimer.SetTimer(0f);
                    __instance.Networkowner.unitAvatar.CreateEffectHUD("IMMERSION", "DaggerImmersion");
                    __instance.NetworkevasionFuryCounting = false;
                    if (__instance.connectionToClient == null)
                    {
                        CreateFuryChargedParticleMethod.Invoke(__instance, new object[] { new Color(0.176f, 1f, 0.616f, 1f) });
                    }
                    else
                    {
                        TargetFuryChargedMessage_EvasionMethod.Invoke(__instance, new object[] { __instance.connectionToClient });
                    }
                }
            }

            [HarmonyPatch("AddFury")]
            [HarmonyPrefix]
            static bool AddFury_Prefix(WeaponSimple_Dagger __instance, int f)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                int AddStack = (f + Player.GetCustomStatUnsafe("DAGGERTRANCEBONUS"));
                int MaxStack = (__instance.maxFury + Player.GetCustomStatUnsafe("DAGGERTRANCEBONUS"));
                __instance.NetworkcurrentFury = __instance.currentFury + AddStack;
                if (__instance.entityId == WeaponIDs.UnboundImmersion)
                {
                    MaxStack += 1;
                }
                if (__instance.critFury)
                {
                    if (__instance.entityId != WeaponIDs.TomeOfMimicry)
                        MaxStack += 3;
                }
                if (__instance.currentFury > MaxStack)
                {
                    __instance.NetworkcurrentFury = MaxStack;
                }
                return false;
            }

            [HarmonyPatch("HandleSpecialAttack")]
            [HarmonyPostfix]
            static void HandleSpecialAttack_Postfix(WeaponSimple_Dagger __instance, CombatBehaviour arg1, DamageInstance arg2, ProjectileBase arg3)
            {
                if (!__instance.Networkowner) return;
                if (!(arg1 is UnitAvatar Target)) return;

                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (arg3.swingId == "FURY")
                {
                    if (Player.IsWeaponUseNet(WeaponIDs.LightningsWrath))
                    {
                        if (LightningsWrathEffect.TryGetValue(Player, out int Value) && Value >= 40)
                        {
                            float UseTime = (1f + Player.GetCustomStatUnsafe(TextFile.DebuffDuration) * 0.01f);
                            LightningsWrathEffect2[Player] = new Timer(UseTime);
                            __instance.Networkowner.StartParryInvincible(UseTime);
                            Player.CreateEffectHUD("ThunderBeast", "LightningsWrath_ThunderBeast");
                            LightningsWrathEffect[Player] = 0;
                        }
                    }
                    if (Player.IsWeaponUseNet(WeaponIDs.OverhangingStormCloud))
                    {
                        int Count = 3;
                        float Mult = 1f + Player.GetCustomStatUnsafe(TextFile.DarkCloudSpeed) * 0.01f;
                        float AtkMult = Player.GetCustomStatUnsafe(TextFile.DarkCloudAtkSpeedBonus) * 0.01f;
                        Mult += Player.GetCustomStatUnsafe(TextFile.AttackSpeed) * 0.01f * AtkMult;
                        while (Mult >= 30f)
                        {
                            Count += 1;
                            Mult -= 30;
                        }
                        Count *= Mathf.Max(1, Player.GetCustomStatUnsafe(TextFile.DarkCloudMultishot));
                        StarSephiriaPatches.ModCloudAttack.Add(new CloudAttackData(Player, Target, 1f, 0.2f, Count));
                    }
                }
            }

            [HarmonyPatch("CreateFuryChargedParticle")]
            [HarmonyPrefix]
            static void CreateFuryChargedParticle_Prefix(WeaponSimple_Dagger __instance, ref Color color)
            {
                if (DaggerFuryColor.TryGetValue(__instance.entityId, out Color Value))
                {
                    color = Value;
                }
            }

            [HarmonyPatch("get_ParryCost")]
            [HarmonyPostfix]
            static void GetParryCost_Postfix(WeaponSimple_Dagger __instance, ref int __result)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player.IsWeapon(WeaponIDs.HeatedBlade))
                {
                    int OrigMana = 3;
                    float Mana = OrigMana * (1f - (Player.GetCustomStatUnsafe(TextFile.SpecialAttackCostReduction) / 100f));
                    if (Mana < 0f) Mana = 0f;
                    __result = (int)Mana;
                }
            }
        }

        [HarmonyPatch(typeof(WeaponSimple_Katana))]
        public static class WeaponSimple_Katana_Patches
        {
            public static FieldInfo? sheathAttackTimerField = AccessTools.Field(typeof(WeaponSimple_Katana), "sheathAttackTimer");
            public static FieldInfo? currentKatanaGaugeField = AccessTools.Field(typeof(WeaponSimple_Katana), "currentKatanaGauge");
            public static FieldInfo? isAttackButtonDownField = AccessTools.Field(typeof(WeaponSimple_Katana), "isAttackButtonDown");
            public static FieldInfo? isFilledGaugeCurrentFrameField = AccessTools.Field(typeof(WeaponSimple_Katana), "isFilledGaugeCurrentFrame");
            public static FieldInfo? isNanmuActiveField = AccessTools.Field(typeof(WeaponSimple_Katana), "isNanmuActive");
            public static FieldInfo? autoFillDelayTimerField = AccessTools.Field(typeof(WeaponSimple_Katana), "autoFillDelayTimer");
            public static FieldInfo? isMagicDamageEnhancementActiveField = AccessTools.Field(typeof(WeaponSimple_Katana), "isMagicDamageEnhancementActive");
            public static FieldInfo? hardeningTimerField = AccessTools.Field(typeof(WeaponSimple_Katana), "hardeningTimer");
            public static FieldInfo? hardeningCountField;
            public static MethodInfo? SetSheathHardeningCounterMethod;
            public static MethodInfo? ActivateNanmuMethod = AccessTools.Method(typeof(WeaponSimple_Katana), "ActivateNanmu");
            public static MethodInfo? RpcCreateQuickDrawAttackFxMethod = AccessTools.Method(typeof(WeaponSimple_Katana), "RpcCreateQuickDrawAttackFx");
            public static MethodInfo? RpcCreateGreatDrawAttackFxMethod = AccessTools.Method(typeof(WeaponSimple_Katana), "RpcCreateGreatDrawAttackFx");
            public static MethodInfo? SetResidualLightningStackMethod = AccessTools.Method(typeof(WeaponSimple_Katana), "SetResidualLightningStack");

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            static void Update_Postfix(WeaponSimple_Katana __instance)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player != null)
                {
                    if (Player.IsWeapon(WeaponIDs.Heidi) && __instance.entityId == WeaponIDs.Heidi)
                    {
                        __instance.dashStackFixedAttackSpeed = 100;
                    }//海蒂
                    if (Player.IsWeapon(WeaponIDs.LongBladeOnohara) && __instance.entityId == WeaponIDs.LongBladeOnohara)
                    {
                        __instance.sheathAttackTime = 0.75f / 1.1f;
                    }//小野原
                    if (Player.IsWeapon(WeaponIDs.BeastMarksSwordHou) && __instance.entityId == WeaponIDs.BeastMarksSwordHou)
                    {
                        __instance.sheathAttackTime = 0.75f / 1.1f;
                        __instance.specialAttackCost = 0;
                    }//豪羽
                    if (Player.IsWeapon(WeaponIDs.FamedSwordMuramasa) && __instance.entityId == WeaponIDs.FamedSwordMuramasa)
                    {
                        float Orig = 0.75f;
                        float AttackSpeed = Player.GetCustomStatUnsafe(TextFile.AttackSpeed);
                        float Mult = (100 + AttackSpeed) / (100 + AttackSpeed * 1.8f);
                        float FinalSheathTime = Orig / Mult;
                        __instance.sheathAttackTime = FinalSheathTime / 1.7f;
                    }//村正
                    if (Player.IsWeapon(WeaponIDs.LightningFlash) && __instance.entityId == WeaponIDs.LightningFlash)
                    {
                        if (LightningFlashBuff.TryGetValue(Player.Name, out int Value) && Value > 0)
                        {
                            LightningFlashBuff[Player.Name] -= 1;
                            if (__instance.NetworkelectricChargeStackCount <= 0)
                            {
                                Player.CreateEffectHUD("KATANAELECTRICCHARGE", "KatanaElectricCharge");
                            }
                            __instance.NetworkelectricChargeStackCount = __instance.electricChargeStackCount + 1;
                            if (__instance.NetworkelectricChargeStackCount > __instance.electricChargeStackCountMax)
                                __instance.NetworkelectricChargeStackCount = __instance.electricChargeStackCountMax;
                            Player.SetEffectHUDValue("KatanaElectricCharge", __instance.electricChargeStackCount.ToString());
                        }
                    }//电光一闪
                    if (Player.IsWeapon(WeaponIDs.RendbladeDriftingWillow) && __instance.entityId == WeaponIDs.RendbladeDriftingWillow)
                    {
                        __instance.attackToFillGaugeValue = 0.17f;
                    }//飞柳
                    if (Player.IsWeapon(WeaponIDs.BlazingHelbanus) && __instance.entityId == WeaponIDs.BlazingHelbanus)
                    {
                        if (sheathAttackTimerField == null)
                        {
                            LogError($"[WeaponSimple_Katana][Update]反射获取失败");
                            return;
                        }
                        __instance.specialAttackCost = 2;
                        if (!BlazingHelbanusSpeed.TryGetValue(Player.Name, out bool falg))
                        {
                            return;
                        }

                        Timer Timer = (Timer)sheathAttackTimerField.GetValue(__instance);
                        if (Timer.Ratio < 0.75f && falg)
                        {
                            Timer.Ratio = 0.75f;
                            BlazingHelbanusSpeed[Player.Name] = false;
                            sheathAttackTimerField.SetValue(__instance, Timer);
                        }
                    }//赫尔巴努斯
                    if (Player.IsWeaponUseNet(WeaponIDs.YuisShortDagger) && __instance.entityId == WeaponIDs.YuisShortDagger)
                    {
                        if (!YuisShortDaggerMagic.ContainsKey(Player.Name))
                        {
                            YuisShortDaggerMagic.Add(Player.Name, 0);
                            YuisShortDaggerTime.Add(Player.Name, 0);
                        }
                        else if (YuisShortDaggerMagic[Player.Name] < 75)
                        {
                            float Mult = 1f + Player.GetCustomStatUnsafe(TextFile.CooldownRecoverySpeed) * 0.01f;
                            if (Player.TryGetComponent<SkillController>(out SkillController skillController))
                            {
                                Charm_Magic? lastUsedMagic = skillController.GetLastUsedMagic();
                                if (lastUsedMagic != null) Mult += lastUsedMagic.additionalcooldownRecoverySpeedSynced * 0.01f;
                            }
                            YuisShortDaggerTime[Player.Name] += Time.deltaTime * Mult;
                            float TimeCd = 0.33f;
                            TimeCd *= 1f - Player.GetCustomStatUnsafe(TextFile.MagicCooldownReduction) * 0.01f;
                            if (YuisShortDaggerTime[Player.Name] > TimeCd)
                            {
                                YuisShortDaggerMagic[Player.Name] += 1;
                                YuisShortDaggerTime[Player.Name] -= TimeCd;
                            }
                        }
                    }//优衣的短匕首
                    if (Player.IsWeapon(WeaponIDs.BreakplateBlade) && __instance.entityId == WeaponIDs.BreakplateBlade)
                    {
                        if (hardeningTimerField == null)
                        {
                            LogError($"[WeaponSimple_Katana][Update]反射获取失败");
                            return;
                        }
                        float hardeningTimer = (float)hardeningTimerField.GetValue(__instance);
                        if (!__instance.isBladeSheathed)
                        {
                            hardeningTimer += Time.deltaTime * Player.GetCustomStatUnsafe(TextFile.AttackSpeed) * 0.01f;
                            hardeningTimerField.SetValue(__instance, hardeningTimer);
                            __instance.Networkowner.animator.speed = 1f;
                        }
                        else
                        {
                            __instance.Networkowner.animator.speed = 1f + Player.GetCustomStatUnsafe(TextFile.AttackSpeed) * 0.01f;
                        }
                    }//破甲刀
                    if (Player.IsWeapon(WeaponIDs.CeruleanCloudSwordArges) && __instance.entityId == WeaponIDs.CeruleanCloudSwordArges)
                    {
                        __instance.residualLightningStackMax = 50;
                    }//苍云刀
                }
            }

            [HarmonyPatch("LateUpdate")]
            [HarmonyPostfix]
            static void LateUpdate_Postfix(WeaponSimple_Katana __instance)
            {
                if (!ModConfig.Instance.EnableWeaponModifier || __instance.useNanmu || !__instance.isServer) return;
                try
                {
                    if (currentKatanaGaugeField == null || isAttackButtonDownField == null)
                    {
                        LogError($"[WeaponSimple_Katana][LateUpdate]获取反射失败");
                        return;
                    }

                    float currentKatanaGauge = (float)currentKatanaGaugeField.GetValue(__instance);
                    bool isAttackButtonDown = (bool)isAttackButtonDownField.GetValue(__instance);

                    if (isAttackButtonDown && !__instance.NetworkisBladeSheathed && __instance.useQuickDraw && currentKatanaGauge >= 1f)
                    {
                        ShootGreatAttack(__instance);
                        currentKatanaGaugeField.SetValue(__instance, 0f);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[WeaponSimple_Katana][LateUpdate]错误: {ex}");
                }
            }

            public static void ShootGreatAttack(WeaponSimple_Katana __instance, bool Damage = true)
            {
                if (RpcCreateQuickDrawAttackFxMethod == null || RpcCreateGreatDrawAttackFxMethod == null)
                {
                    LogError("[WeaponSimple_Katana][ShootGreatAttack]获取反射失败");
                    return;
                }
                Vector3 attackDirection = __instance.Networkowner.attackDirection;
                if (__instance.greatDrawFireData == null)
                {
                    WeaponEntity Weapon = WeaponDatabase.FindWeaponById(WeaponIDs.RendbladeHeavenrend);
                    WeaponSimple_Katana Katana = Weapon.mainWeaponPrefab.GetComponent<WeaponSimple_Katana>();
                    __instance.greatDrawFireData = Katana.greatDrawFireData;
                }
                int Type = 0;
                if (__instance.greatDrawFireData == null) Type = 1;
                else
                {
                    __instance.greatDrawFireData.attackDashSpeed = 0f;
                    __instance.greatDrawFireData.attackDashTime = 0f;
                }
                if (Type == 1)
                {
                    if (Damage) __instance.Networkowner.CreateBasicAttackSwingFullyManual(__instance.quickDrawFireData, __instance.finalComboIdx, attackDirection, 0);
                    RpcCreateQuickDrawAttackFxMethod.Invoke(__instance, null);
                    return;
                }
                // 大拔刀斬
                if (Damage) __instance.Networkowner.CreateBasicAttackSwingFullyManual(__instance.greatDrawFireData, __instance.finalComboIdx, attackDirection, 0);
                RpcCreateGreatDrawAttackFxMethod.Invoke(__instance, null);
            }

            [HarmonyPatch("OnBasicAttack")]
            [HarmonyPrefix]
            static bool OnBasicAttack_Prefix(WeaponSimple_Katana __instance, CombatBehaviour victim, DamageInstance damage, ProjectileBase projectile)
            {
                if (!ModConfig.Instance.EnableWeaponModifier) return true;
                try
                {
                    if (currentKatanaGaugeField == null || isFilledGaugeCurrentFrameField == null || isNanmuActiveField == null || ActivateNanmuMethod == null)
                    {
                        LogError("[WeaponSimple_Katana][OnBasicAttack]获取反射失败");
                        return true;
                    }

                    float currentKatanaGauge = (float)currentKatanaGaugeField.GetValue(__instance);
                    bool isFilledGaugeCurrentFrame = (bool)isFilledGaugeCurrentFrameField.GetValue(__instance);
                    bool isNanmuActive = (bool)isNanmuActiveField.GetValue(__instance);
                    if (__instance.hasKatanaGauge && __instance.useAttackToFillGauge && !isFilledGaugeCurrentFrame)
                    {
                        isFilledGaugeCurrentFrameField.SetValue(__instance, true);
                        currentKatanaGaugeField.SetValue(__instance, currentKatanaGauge + __instance.attackToFillGaugeValue);
                        if (currentKatanaGauge > 1f)
                        {
                            currentKatanaGaugeField.SetValue(__instance, 1f);
                            if (__instance.useNanmu)
                            {
                                ActivateNanmuMethod.Invoke(__instance, null);
                            }
                        }
                        __instance.katanaGaugeDisappearDelayTimer.Ratio = 0f;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    LogError($"OnBasicAttack_Postfix 错误: {ex}");
                    return true;
                }
            }

            [HarmonyPatch("OnSpecialAttackCreated")]
            [HarmonyPrefix]
            static void OnSpecialAttackCreated_Prefix(WeaponSimple_Katana __instance, int idx, ProjectileBase projectile)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player != null)
                {
                    if (Player.IsWeapon(WeaponIDs.FamedSwordMuramasa) && __instance.entityId == WeaponIDs.FamedSwordMuramasa)
                    {
                        __instance.owner?.OnBeginAttackAnimation?.Invoke(0);
                    }//村正
                    if (Player.IsWeapon(WeaponIDs.CeruleanCloudSwordArges) && __instance.entityId == WeaponIDs.CeruleanCloudSwordArges)
                    {
                        float Mult = 5;
                        float Atk = Player.GetCustomStatUnsafe(TextFile.DarkCloudAtkSpeedBonus) * 0.01f;
                        Mult *= 1f + Player.GetCustomStatUnsafe(TextFile.DarkCloudSpeed) * 0.01f;
                        Mult *= 1f + Player.GetCustomStatUnsafe(TextFile.AttackSpeed) * Atk * 0.01f;
                        __instance.cloudSlashDamagePercentPerStack = (int)Mult;
                    }//苍云刀
                }
            }

            [HarmonyPatch("StartSpecialAttackAnimation")]
            [HarmonyPrefix]
            static void StartSpecialAttackAnimation_Prefix(WeaponSimple_Katana __instance, int idx)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player != null)
                {
                    if (Player.IsWeapon(WeaponIDs.CeruleanCloudSwordArges) && __instance.entityId == WeaponIDs.CeruleanCloudSwordArges)
                    {
                        int Stack = Mathf.Clamp(__instance.residualLightningStack, 0, __instance.residualLightningStackMax);
                        __instance.Networkowner.StartFuryInvincible(0.175f + Stack * 0.0125f);
                    }//苍云刀
                }
            }

            [HarmonyPatch("StartSpecialAttackAnimation")]
            [HarmonyPostfix]
            static void StartSpecialAttackAnimation_Postfix(WeaponSimple_Katana __instance, int idx)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player != null)
                {
                    if (Player.IsWeapon(WeaponIDs.CeruleanCloudSwordArges) && __instance.entityId == WeaponIDs.CeruleanCloudSwordArges)
                    {
                        SetResidualLightningStackMethod?.Invoke(__instance, new object[] { 0 });
                    }//苍云刀
                }
            }

            [HarmonyPatch("ActivateNanmu")]
            [HarmonyPostfix]
            static void ActivateNanmu_Postfix(WeaponSimple_Katana __instance)
            {
                if (!ModConfig.Instance.EnableWeaponModifier) return;
                ShootGreatAttack(__instance);
            }

            [HarmonyPatch("OnBeginDashServerside")]
            [HarmonyPrefix]
            static bool OnBeginDashServerside_Prefix(WeaponSimple_Katana __instance, Vector2 direction, bool isDashCostPaid, ref int ___dashStackCounter)
            {
                if (!__instance.isServer || !ModConfig.Instance.EnableWeaponModifier) return true;

                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player.GetCustomStatUnsafe("KATANADASHSTACK") > 0 && isDashCostPaid)
                {
                    bool falg = __instance.dashStackCounter == 0;
                    __instance.NetworkdashStackCounter = Math.Min(12, __instance.dashStackCounter + 3);
                    if (falg)
                    {
                        Player.CreateEffectHUD("KATANADASHSTACK", "KatanaDashStack");
                        Player.SetEffectHUDValue("KatanaDashStack", __instance.dashStackCounter.ToString());
                        return false;
                    }
                    Player.SetEffectHUDFlash("KatanaDashStack");
                    Player.SetEffectHUDValue("KatanaDashStack", __instance.dashStackCounter.ToString());
                    return false;
                }
                return true;
            }

            [HarmonyPatch("OnEndCastMagicServerside")]
            [HarmonyPostfix]
            static void OnEndCastMagicServerside_Postfix(WeaponSimple_Katana __instance)
            {
                if (!ModConfig.Instance.EnableWeaponModifier) return;
                if (YuisShortDaggerMagic.ContainsKey(__instance.Networkowner.unitAvatar.Name))
                {
                    YuisShortDaggerMagic[__instance.Networkowner.unitAvatar.Name] = 0;
                }
            }

            [HarmonyPatch("OnCreateMagicServerside")]
            [HarmonyPrefix]
            static bool OnCreateMagicServerside_Prefix(WeaponSimple_Katana __instance)
            {
                if (!ModConfig.Instance.EnableWeaponModifier) return true;
                if (__instance.useMagicBlade)
                {
                    ShootMagicBlade(__instance);
                }
                return false;
            }

            private static NewWeaponFireData? HeidiExDashData = null;
            [HarmonyPatch("GetDashAttack")]
            [HarmonyPrefix]
            static bool GetDashAttack_Prefix(WeaponSimple_Katana __instance, int idx, ref NewWeaponFireData __result)
            {
                if (!ModConfig.Instance.EnableWeaponModifier) return true;

                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player.IsWeapon(WeaponIDs.Heidi) && __instance.entityId == WeaponIDs.Heidi)
                {
                    if (HeidiDashAttack.TryGetValue(Player.Name, out int Count) && Count > 6)
                    {
                        if (HeidiExDashData == null)
                        {
                            WeaponEntity Entity = WeaponDatabase.FindWeaponById(WeaponIDs.SolisEkli);
                            WeaponSimple_Katana component = Entity.mainWeaponPrefab.GetComponent<WeaponSimple_Katana>();
                            HeidiExDashData = component.eclipseBasicAttackFireDatas[4];
                            __result = HeidiExDashData;
                        }
                        else __result = HeidiExDashData;
                        return false;
                    }
                }
                return true;
            }

            public static void ShootMagicBlade(WeaponSimple_Katana __instance, float DamageMult = 1f)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                float Damage = GetMagicBladeDamage(Player);
                Vector3 Pos = CalculateBulletSpawnPosition(__instance);

                Vector3 Vec = (Vector3)Player.CurrentLookingPosition - CalculateBulletSpawnPosition(__instance);
                Bullet Pro = Bullet.Pool.Spawn(__instance.magicBladePrefab, Pos, true, EDamageFromType.Magic, __instance.magicBladeDamageId, Damage * DamageMult, 1, 1f, Player,
                    Player.GetHostileFactionLayers(EDamageFromType.None), Player.TopdownActor.CenterYPos, Pos, Pos + Vec.normalized * 10f, null, null, 0f, Tools.BaseStatTypeToMax(Player), true);
                Pro.additionalCriticalChancePercent += Player.GetCustomStatUnsafe(TextFile.MagicCritical) * 0.01f;
                Pro.additionalCriticalDamageRate += Player.GetCustomStatUnsafe(TextFile.MagicCriticalDamageBonus);

                Pro.SetDamage(Damage);
                UnitAvatar Target = PlayerInputController.SearchTargetNearestPoint(Player, Player.CurrentLookingPosition, 10f);
                if (Target != null) Pro.SetHomingTarget(Target);
            }

            private static float GetMagicBladeDamage(UnitAvatar Player)
            {
                int BaseDamage = Tools.BaseStatToAverage(Player);
                float ManaDamageMult = 1f + Player.MaxMp * 0.01f;
                float MagicMult = 1f + Player.GetCustomStat(ECustomStat.MagicDamageBonus) * 0.01f;
                float MPMult = 1f + Player.GetCustomStatUnsafe(TextFile.MPSkillDamage) * 0.01f;
                float totalDamage = BaseDamage * ManaDamageMult * MagicMult * MPMult;
                return totalDamage;
            }

            private static Vector3 CalculateBulletSpawnPosition(WeaponSimple_Katana instance)
            {
                Vector3 ownerPosition = instance.Networkowner.transform.position;
                Vector3 attackDirection = instance.Networkowner.attackDirection.normalized;
                Vector3 basePosition = ownerPosition - attackDirection * 1.5f;
                float angle = attackDirection.GetAngle() + 90f * Mathf.Sign(instance.Networkowner.attackDirection.x);
                Vector3 sideOffset = HorayUtility.GetVector3FromAngle(angle) * 0.3f;
                Vector3 randomOffset = (Vector3)UnityEngine.Random.insideUnitCircle * 0.5f;
                return basePosition + (sideOffset + randomOffset);
            }
        }

        [HarmonyPatch(typeof(WeaponSimple_SwordAndShield))]
        public static class WeaponSimple_SwordAndShield_Patches
        {
            private static GameObject? GardenofNeedleIceBullet = null;

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            static void Update_Postfix(WeaponSimple_SwordAndShield __instance)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player.IsWeapon(WeaponIDs.SolisMissio) && __instance.entityId == WeaponIDs.SolisMissio)
                {
                    __instance.isSolisMissioEnabled = false;
                    if (Player.isGuardEnabled)
                    {
                        if (!ThrowSwordTime.ContainsKey(Player))
                            ThrowSwordTime[Player] = 0f;
                        else
                        {
                            ThrowSwordTime[Player] += Time.deltaTime;
                            if (ThrowSwordTime[Player] >= 0.3f)
                            {
                                if (Player.Inventory)
                                {
                                    ComboEffectBase Combo = Player.Inventory.FindComboEffect("FLAMESWORD");
                                    if (Combo != null && Combo is ComboEffect_FlameSword FlameSword)
                                    {
                                        Vector3 TargetPos = Player.CurrentLookingPosition;
                                        int Count = 1 + Player.GetCustomStatUnsafe(TextFile.FlameSwordAdditionalAttack);
                                        for (int i = 0; i < Count; i++)
                                        {
                                            Vector3 ShootPos = TargetPos + (Vector3)UnityEngine.Random.insideUnitCircle.normalized * 2.5f;
                                            FlameSword.FireSword(ShootPos, TargetPos);
                                        }
                                    }
                                }
                                float Value = 0.3f / (1f + Player.GetCustomStatUnsafe(TextFile.AttackSpeed) * 0.01f);
                                ThrowSwordTime[Player] -= Value;
                            }
                        }
                    }
                }
                if (Player.IsWeaponUseNet(WeaponIDs.GuardiansReturn) && __instance.entityId == WeaponIDs.GuardiansReturn)
                {
                    __instance.isSweepAvailable = false;
                }
                if (Player.IsWeapon(WeaponIDs.GardenofNeedleIce) && __instance.entityId == WeaponIDs.GardenofNeedleIce)
                {
                    if (GardenofNeedleIceBullet == null)
                    {
                        ItemEntity Item = ItemDatabase.FindItemById(1139);
                        Charm_CreateBulletOnSweep Effect = Item.resourcePrefab.GetComponent<Charm_CreateBulletOnSweep>();
                        GardenofNeedleIceBullet = Effect.orbitBulletPrefab;
                    }
                    else
                    {
                        float NewTime = 6f;
                        NewTime /= 1f + Player.GetCustomStatUnsafe(TextFile.CooldownRecoverySpeed) * 0.01f;
                        NewTime *= 1f - Player.GetCustomStatUnsafe(TextFile.MagicCooldownReduction) * 0.01f;
                        if (GardenofNeedleIceEffect.TryGetValue(Player, out Timer EffectTime))
                        {
                            if (!Player.IsInBattle) EffectTime.AddTimer(Time.deltaTime);
                            else if (EffectTime.Update(Time.deltaTime))
                            {
                                EffectTime.time = NewTime;
                                Vector3 Pos = Player.transform.position;
                                Vector3 Vec = __instance.owner.aimedPositionClientside - Player.transform.position;
                                string DamageId = "[Star's Sephiria Mod]霜针花园";
                                float Damage = Player.GetCustomStatUnsafe(TextFile.IceDamage) * 2f;
                                Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.MagicDamageBonus) * 0.01f;
                                Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.MPSkillDamage) * 0.01f;
                                int ShootStack = 4;
                                if (Player.TryGetComponent<SkillController>(out SkillController Skill))
                                {
                                    ShootStack *= Skill.GetMultipleCastCount();
                                }
                                for (int i = 0; i < ShootStack; i++)
                                {
                                    Bullet bullet = Bullet.Pool.Spawn(GardenofNeedleIceBullet, Pos, true, EDamageFromType.Magic, DamageId, Damage, 25, 5f, Player, Player.GetHostileFactionLayers(EDamageFromType.Magic), 0f, Pos, Pos + Vec, null, null, 0f, EDamageElementalType.Ice, true);
                                    if (bullet.MoveModule != null && bullet.MoveModule is BulletMoveModule_OrbitOwner MoveModule)
                                        MoveModule.SetSlotOffset(360f / ShootStack * i);
                                }
                            }
                        }
                        else GardenofNeedleIceEffect[Player] = new Timer(NewTime);
                    }
                }
                if (Player.IsWeapon(WeaponIDs.GrippingFear) && __instance.entityId == WeaponIDs.GrippingFear)
                {
                    if (!Player.IsInBattle)
                    {
                        if (__instance.NetworkchargedSweep_New_Stack > 10)
                        {
                            __instance.NetworkchargedSweep_New_Stack = 10;
                            string HudName = string.Format("Weapon_{0}", __instance.entityId);
                            __instance.Networkowner.unitAvatar.SetEffectHUDFlash(HudName + "_CHARGEDSWEEP");
                            __instance.Networkowner.unitAvatar.SetEffectHUDValue(HudName + "_CHARGEDSWEEP", __instance.chargedSweep_New_Stack.ToString());
                        }
                    }
                }
            }

            [HarmonyPatch("LateUpdate")]
            [HarmonyPostfix]
            static void LateUpdate_Postfix(WeaponSimple_SwordAndShield __instance)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player.IsWeapon(WeaponIDs.TheIndomitable) && __instance.entityId == WeaponIDs.TheIndomitable)
                {
                    __instance.Networkowner.animator.speed = 1.25f;
                }
            }

            [HarmonyPatch("OnBasicAttack")]
            [HarmonyPostfix]
            static void OnBasicAttack_Postfix(WeaponSimple_SwordAndShield __instance, CombatBehaviour victim, DamageInstance damage, ProjectileBase projectile)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (__instance.isSolisMissioEnabled && Player.IsWeapon(WeaponIDs.SolisMissio))
                {
                    __instance.NetworksolisMissioStack = __instance.solisMissioStack + 1;
                    int Max = 40;
                    if (__instance.solisMissioStack > Max)
                        __instance.NetworksolisMissioStack = Max;
                }
            }

            [HarmonyPatch("OnSpecialAttackCreated")]
            [HarmonyPostfix]
            static void OnSpecialAttackCreated_Postfix(WeaponSimple_SwordAndShield __instance, int idx, ProjectileBase projectile)
            {
                UnitAvatar Player = __instance.owner.unitAvatar;
                if (Player != null)
                {
                    if (Player.IsWeapon(WeaponIDs.GrippingFear) && __instance.entityId == WeaponIDs.GrippingFear)
                    {
                        if (__instance.chargedSweep_New_Stack >= 10)
                            projectile.additionalDamagePercent += 100;
                    }
                }
            }

            [HarmonyPatch("HandleGuard")]
            [HarmonyPostfix]
            static void HandleGuard_Postfix(WeaponSimple_SwordAndShield __instance, DamageInstance obj, bool perfectGuard)
            {
                UnitAvatar Player = __instance.owner.unitAvatar;
                if (Player != null)
                {
                    if (Player.IsWeapon(WeaponIDs.GrippingFear) && __instance.entityId == WeaponIDs.GrippingFear)
                    {
                        if (perfectGuard)
                        {
                            string HudName = string.Format("Weapon_{0}", __instance.entityId);
                            __instance.NetworkchargedSweep_New_Stack = __instance.chargedSweep_New_Stack + 10;
                            if (__instance.NetworkchargedSweep_New_Stack > 30) __instance.NetworkchargedSweep_New_Stack = 30;
                            __instance.Networkowner.unitAvatar.SetEffectHUDFlash(HudName + "_CHARGEDSWEEP");
                            __instance.Networkowner.unitAvatar.SetEffectHUDValue(HudName + "_CHARGEDSWEEP", __instance.chargedSweep_New_Stack.ToString());
                        }
                    }
                    if (Player.IsWeapon(WeaponIDs.FlameEaterHaetae) && __instance.entityId == WeaponIDs.FlameEaterHaetae)
                    {
                        if (obj.origin is UnitAvatar Target)
                        {
                            Target.ApplyDebuff(CombatManager.Instance.burnDebuffPrefab, Player);
                            if (perfectGuard)
                                Target.ApplyDebuff(CombatManager.Instance.burnDebuffPrefab, Player);
                        }
                    }
                    if (Player.IsWeapon(WeaponIDs.StoneSword) && __instance.entityId == WeaponIDs.StoneSword)
                    {
                        if (obj.origin is UnitAvatar Target)
                        {
                            ShootStoneSwordPro(Player, Target, perfectGuard ? 3f : 1f);
                        }
                    }
                }
            }

            [HarmonyPatch("OnAttackUnit_FlameEater")]
            [HarmonyPostfix]
            static void OnAttackUnit_FlameEater_Postfix(WeaponSimple_SwordAndShield __instance, CombatBehaviour victim, DamageInstance damage, ProjectileBase projectile)
            {
                if (__instance.Networkowner == null || __instance.Networkowner.unitAvatar == null || !ModConfig.Instance.EnableWeaponModifier)
                {
                    return;
                }
                if (__instance.flameEaterStack <= 0)
                {
                    return;
                }
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                Vector3 damagePoint = damage.damagePoint;
                Vector3 vector = damagePoint - Player.transform.position;
                float direction = Player.TopdownRigidbody.Direction;
                float DamageMult = 72f;
                if (Player.GetCustomStatUnsafe(TextFile.BurnEvo) > 0)
                {
                    DamageMult *= 28f / 18f;
                }
                DamageMult *= 1f + Player.GetCustomStatUnsafe(TextFile.BurnDamage) * 0.01f;
                DamageMult *= 1f + Player.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.01f;
                DamageMult *= 1f + Player.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 0.01f;
                float Damage = DamageMult * __instance.flameEaterStack * 0.01f;
                float Offset = vector.GetAngle() - (90f + UnityEngine.Random.Range(-10f, 10f)) * direction;
                __instance.RpcCreateHeataeStrikeFx(damagePoint, (short)(Offset * 70f));
                __instance.CreateBasicAttackProjectileFullyManual(__instance.haetaeStrikeAttackExplosionFireData, 0, damagePoint, HorayUtility.GetVector3FromAngle(Offset), null, 1f, 0, Damage);
                __instance.NetworkflameEaterStack = 0;
            }

            [HarmonyPatch("AttackButtonDown")]
            [HarmonyPrefix]
            static bool AttackButtonDown_Prefix(WeaponSimple_SwordAndShield __instance, int dashAttack)
            {
                if (!ModConfig.Instance.EnableWeaponModifier) return true;
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player.IsWeapon(WeaponIDs.BlindingSilence) && __instance.entityId == WeaponIDs.BlindingSilence)
                {
                    WeaponControllerSimple Controller = __instance.Networkowner;
                    if (Controller.animator.GetBool(AnimHashContainer.Instance.GuardHash))
                    {
                        if((Controller.currentWeaponSwing == -1 ||
                            Controller.allowedInputBufferName.Equals("SWEEP", StringComparison.OrdinalIgnoreCase))
                            && __instance.isSweepAvailable)
                        {
                            Controller.SetAnimationTrigger(AnimHashContainer.Instance.SweepHash);
                            Controller.SetAnimationInt(AnimHashContainer.Instance.SweepNumHash, 11);
                            return false;
                        }
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(WeaponSimple_Crossbow))]
        public static class WeaponSimple_Crossbow_Patches
        {
            public static FieldInfo? compressedAmmoCountField = AccessTools.Field(typeof(WeaponSimple_Crossbow), "compressedAmmoCount");
            public static MethodInfo? RpcCreateAmmoCompressionShootFxMethod = AccessTools.Method(typeof(WeaponSimple_Crossbow), "RpcCreateAmmoCompressionShootFx");
            public static MethodInfo? ReloadOnServerMethod = AccessTools.Method(typeof(WeaponSimple_Crossbow), "ReloadOnServer");
            public static MethodInfo? RpcCreateIceBuffFxMethod = AccessTools.Method(typeof(WeaponSimple_Crossbow), "RpcCreateIceBuffFx");

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            static void Update_Postfix(WeaponSimple_Crossbow __instance)
            {
                if (!NetworkServer.active) return;

                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player != null)
                {
                    if (Player.IsWeapon(WeaponIDs.EquippedwithGuidanceModule) && __instance.entityId == WeaponIDs.EquippedwithGuidanceModule)
                    {
                        __instance.miniDroneAddDamagePercentPerMP = 1f;
                    }//配备制导模块
                    if (Player.IsWeapon(WeaponIDs.Twin) && __instance.entityId == WeaponIDs.Twin)
                    {
                        __instance.decreaseSpeed = 0f;
                    }//双子
                    if (Player.IsWeapon(WeaponIDs.RapidFireDevice) && __instance.entityId == WeaponIDs.RapidFireDevice)
                    {
                        __instance.Networkowner.animator.speed = 1.2f;
                    }//连发装置
                    if (Player.IsWeapon(WeaponIDs.RapidFreezeCrystal) && __instance.entityId == WeaponIDs.RapidFreezeCrystal)
                    {
                        __instance.infiniteAmmoDuringBuff = true;
                    }//速冻结晶
                    if (Player.IsWeapon(WeaponIDs.SolisIubar))
                    {
                        if (SolisIubarEffect.TryGetValue(Player, out int Value) && Value > 0)
                        {
                            __instance.AddAmmo(1);
                            SolisIubarEffect[Player] -= 1;
                        }
                    }//索利斯·尤巴尔
                    if (Player.IsWeapon(WeaponIDs.Minigun) && __instance.entityId == WeaponIDs.Minigun)
                    {
                        __instance.Networkowner.animator.speed = 1.1f;
                    }//迷你机枪
                }
            }

            [HarmonyPatch("LateUpdate")]
            [HarmonyPostfix]
            static void LateUpdate_Postfix(WeaponSimple_Crossbow __instance)
            {
                if (!NetworkServer.active) return;

                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player != null)
                {
                    if (Tools.CostumeSkinUse(Player.Name, "Skeleton", "_3"))
                    {
                        if (__instance.isReloading) __instance.NetworkisReloading = false;
                        __instance.NetworkammoInCurrentMagazine = __instance.currentMagazineCapacity;
                        __instance.NetworkremainingMagazineCount = __instance.defaultMagazineCount + Player.GetCustomStatUnsafe(TextFile.CrossbowAdditionalMagazine);
                        if (Player is PlayerAvatar T)
                            if (T.isInDungeon > 0)
                                __instance.decreaseSpeed = 0.1f;
                    }
                    if (Player.IsWeapon(WeaponIDs.CloudGatherer) && __instance.entityId == WeaponIDs.CloudGatherer)
                    {
                        CloudGathererEffect[Player.Name] = false;
                        if (__instance.ammoInCurrentMagazine == 2 || __instance.ammoInCurrentMagazine == 4)
                        {
                            CloudGathererEffect[Player.Name] = true;
                        }
                    }
                    AmmoStack[Player] = __instance.ammoInCurrentMagazine;
                }
            }

            [HarmonyPatch("OnAttack_Basic")]
            [HarmonyPostfix]
            static void OnAttack_Basic_Postfix(WeaponSimple_Crossbow __instance, CombatBehaviour victim, DamageInstance damage, ProjectileBase projectile)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player != null && projectile)
                {
                    Bullet? Bullet = projectile as Bullet;
                    if (Bullet != null)
                    {
                        if (Player.IsWeapon(WeaponIDs.CloudGatherer) && Bullet.turnOnChainLightning)
                        {
                            if (Player.Inventory)
                            {
                                ComboEffectBase Combo = Player.Inventory.FindComboEffect("DARKCLOUD");
                                if (Combo is ComboEffect_DarkCloud Dark)
                                {
                                    if (victim is UnitAvatar Target)
                                    {
                                        float Mult = 1f + Player.GetCustomStatUnsafe(TextFile.BasicAttackDamageBonus) * 0.01f;
                                        Mult *= 1f + Player.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                                        int Count = Math.Max(1, Player.GetCustomStatUnsafe(TextFile.DarkCloudMultishot));
                                        Dark.AddCloud(Count);
                                        ModCloudAttack.Add(new CloudAttackData(Player, Target, 0.8f * Mult, 0.2f, Count));
                                    }
                                }
                            }
                        }//聚云者
                    }
                }
            }

            [HarmonyPatch("OnSpecialAttackCreated")]
            [HarmonyPostfix]
            static void OnSpecialAttackCreated_Postfix(WeaponSimple_Crossbow __instance, int idx, ProjectileBase projectile)
            {
                if (!ModConfig.Instance.EnableWeaponModifier) return;

                if (ReloadOnServerMethod == null)
                {
                    LogError($"[WeaponSimple_Crossbow][OnSpecialAttackCreated]反射获取失败");
                    return;
                }
                if (__instance.continueBonus && projectile)
                {
                    __instance.continueBonusCount = 0;
                    int MaxAmmoStack = __instance.NetworkcurrentMagazineCapacity + __instance.Networkowner.unitAvatar.GetCustomStatUnsafe("CROSSBOWADDITIONALMAGAZINE");
                    int AmmoStack = __instance.NetworkammoInCurrentMagazine;
                    projectile.additionalDamagePercent += (MaxAmmoStack - AmmoStack) * 15f;

                    if (__instance.NetworkammoInCurrentMagazine <= 0)
                    {
                        if (__instance.remainingMagazineCount > 1)
                        {
                            __instance.NetworkremainingMagazineCount = __instance.remainingMagazineCount - 1;
                            __instance.NetworkammoInCurrentMagazine = __instance.currentMagazineCapacity;
                        }
                        else
                        {
                            ReloadOnServerMethod.Invoke(__instance, null);
                        }
                    }
                    else
                    {
                        __instance.NetworkammoInCurrentMagazine = __instance.ammoInCurrentMagazine - 1;
                    }
                }
            }

            [HarmonyPatch("OnBasicAttackCreated")]
            [HarmonyPostfix]
            static void OnBasicAttackCreated_Postfix(WeaponSimple_Crossbow __instance, int idx, ProjectileBase projectile)
            {
                if (!ModConfig.Instance.EnableWeaponModifier) return;

                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player.GetCustomStatUnsafe("GRENADEATTACK") > 0)
                {
                    if (projectile.TryGetComponent<BulletDestroyModule_Explode>(out var component))
                    {
                        float Mult = 100f + Player.GetCustomStatUnsafe(TextFile.BurnDamage);
                        if (Player.GetCustomStatUnsafe(TextFile.BurnEvo) > 0) Mult *= 0.28f / 0.18f;
                        component.explodeRadiusBonusPercent += Mult * 0.5f;
                    }
                }
            }

            [HarmonyPatch("get_SpecialAttackCost")]
            [HarmonyPostfix]
            static void GetSpecialAttackCost_Postfix(WeaponSimple_Crossbow __instance, ref float __result)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player.IsWeapon(WeaponIDs.RapidFireDevice) && __instance.entityId == WeaponIDs.RapidFireDevice)
                {
                    if (__instance.NetworkammoInCurrentMagazine > 0) __result = 1;
                }
                if (Player.IsWeapon(WeaponIDs.M9199, WeaponIDs.XRA9, WeaponIDs.M9200))
                {
                    float OrigMana = 3f;
                    float Mana = OrigMana * (1f - (Player.GetCustomStatUnsafe(TextFile.SpecialAttackCostReduction) / 100f));
                    if (Mana < 0f) Mana = 0f;
                    __result = (int)Mana;
                }
            }

            [HarmonyPatch("SubAttackButtonDown")]
            [HarmonyPrefix]
            static bool SubAttackButtonDown_Prefix(WeaponSimple_Crossbow __instance)
            {
                if (!ModConfig.Instance.EnableWeaponModifier) return true;
                if (__instance.Networkowner.currentWeaponSwing != -1) return true;
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player.IsWeapon(WeaponIDs.M9200) && __instance.entityId == WeaponIDs.M9200)
                {
                    if (__instance.specialAttackType == WeaponSimple_Crossbow.ESpecialAttackType.AmmoCompression)
                    {
                        if (__instance.isReloading) return true;
                        if (!__instance.NetworkhasCompressedAmmo || __instance.ammoInCurrentMagazine > 1) return true;
                        if (compressedAmmoCountField == null) return true;
                        int compressedAmmoCount = (int)compressedAmmoCountField.GetValue(__instance);
                        if (__instance.NetworkremainingMagazineCount > 1)
                        {
                            __instance.Networkowner.unitAvatar.UseMp((int)__instance.SpecialAttackCost);
                            __instance.NetworkhasCompressedAmmo = true;
                            compressedAmmoCount += __instance.defaultMagazineCapacity + 3;
                            compressedAmmoCountField.SetValue(__instance, compressedAmmoCount);
                            __instance.Networkowner.unitAvatar.CreateEffectHUD("AMMO_COMPRESSED", "AmmoCompressed");
                            __instance.Networkowner.unitAvatar.SetEffectHUDValue("AmmoCompressed", compressedAmmoCount.ToString());
                            __instance.NetworkremainingMagazineCount -= 1;
                            Vector3 Pos = Player.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle;
                            UIManager.Pool.Spawn<UI_DamageParticle>(Pos).SetDamage(compressedAmmoCount.ToString() + "!", EffectConstant.WeaponColor2);
                            if (compressedAmmoCount > 10)
                                __instance.Networkowner.unitAvatar.CreateEffectHUD("AMMO_COMPRESSED_ENHANCED", "AmmoCompressedEnhanced");
                            return false;
                        }
                    }
                }
                if (Player.IsWeapon(WeaponIDs.MarkStabilization))
                {
                    if (MarkStabilizationEffect.TryGetValue(Player, out int Stack) && Stack >= 5)
                    {
                        return true;
                    }
                    Vector3 Pos = Player.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle;
                    UIManager.Pool.Spawn<UI_DamageParticle>(Pos).SetDamage("No Charge!", EffectConstant.LightningColor);
                    return false;
                }
                return true;
            }

            [HarmonyPatch("SubAttackButtonDown")]
            [HarmonyPostfix]
            static void SubAttackButtonDown_Postfix(WeaponSimple_Crossbow __instance)
            {
                if (!ModConfig.Instance.EnableWeaponModifier) return;
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player.IsWeapon(WeaponIDs.XRA9) && __instance.entityId == WeaponIDs.XRA9)
                {
                    if (compressedAmmoCountField == null) return;
                    int compressedAmmoCount = (int)compressedAmmoCountField.GetValue(__instance);
                    XRA9Effect[Player] = compressedAmmoCount;
                }
            }

            [HarmonyPatch("ReloadOnServer")]
            [HarmonyPrefix]
            static void ReloadOnServer_Prefix(WeaponSimple_Crossbow __instance)
            {
                if (!__instance.NetworkisReloading)
                    OnCrossbowUseReLoad?.Invoke();
            }
         }

        [HarmonyPatch(typeof(WeaponSimple_GreatSword))]
        public static class WeaponSimple_GreatSword_Patches
        {
            public static FieldInfo? sweepSwingField = AccessTools.Field(typeof(WeaponSimple_GreatSword), "sweepSwing");
            public static FieldInfo? sweepRequestField = AccessTools.Field(typeof(WeaponSimple_GreatSword), "sweepRequest");
            public static FieldInfo? sweepTimerField = AccessTools.Field(typeof(WeaponSimple_GreatSword), "sweepTimer");
            public static MethodInfo? RpcCreateCompleteChargeFxMethod = AccessTools.Method(typeof(WeaponSimple_GreatSword), "RpcCreateCompleteChargeFx");

            [HarmonyPatch("Update")]
            [HarmonyPrefix]
            static bool Update_Prefix(WeaponSimple_GreatSword __instance)
            {
                if (!NetworkServer.active || !__instance.Networkowner)
                {
                    return true;
                }

                if (sweepSwingField == null || sweepRequestField == null || sweepTimerField == null || RpcCreateCompleteChargeFxMethod == null)
                {
                    LogError("[WeaponSimple_GreatSword][Update]反射获取失败");
                    return true;
                }

                bool sweepSwing = (bool)sweepSwingField.GetValue(__instance);
                bool sweepRequest = (bool)sweepRequestField.GetValue(__instance);
                Timer sweepTimer = (Timer)sweepTimerField.GetValue(__instance);
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (InvincibleSpecialAttackCD.ContainsKey(Player))
                {
                    if (InvincibleSpecialAttackCD[Player].Update(Time.deltaTime))
                        InvincibleSpecialAttackCD.Remove(Player);
                }
                if (sweepSwing && !Player.IsWeapon(WeaponIDs.Compliance))
                {
                    float SpAtSpeed = Time.deltaTime;

                    // 添加蓄力速度自定义属性
                    int chargeSpeedStat = Player.GetCustomStatUnsafe("SPECIALATTACKCHARGESPEED");
                    if (chargeSpeedStat != 0)
                    {
                        SpAtSpeed *= (1f + chargeSpeedStat / 100f);
                    }

                    if (__instance.longCharge && !ModConfig.Instance.EnableWeaponModifier)
                    {
                        SpAtSpeed *= 0.6f;
                    }
                    if (__instance.superQuickSweep)
                    {
                        SpAtSpeed *= 100f;
                    }
                    if (__instance.quickSweep)
                    {
                        SpAtSpeed *= 1.5f;
                    }

                    if (!sweepRequest && sweepTimer.Update(SpAtSpeed) && Player.MP >= __instance.SweepCost)
                    {
                        RpcCreateCompleteChargeFxMethod.Invoke(__instance, null);
                        sweepRequestField.SetValue(__instance, true);
                    }
                    __instance.Networkowner.NetworkchargingRatioForUI = sweepTimer.Ratio;
                }
                else
                {
                    sweepRequestField.SetValue(__instance, false);
                    sweepTimer.SetTimer(0f);
                    __instance.Networkowner.NetworkchargingRatioForUI = 0f;
                }
                if (__instance.isTransformed)
                {
                    Player.SetEffectHUDFillAmount("GreatswordTransformed", __instance.transformResetTimer.Ratio);
                    float deltaTime = Time.deltaTime;
                    float num2 = 1f + Player.GetCustomStat(ECustomStat.BuffDuration) / 100f;
                    if (__instance.transformResetTimer.Update(deltaTime / num2))
                    {
                        __instance.OnTransformChangedServerside?.Invoke(obj: false);
                        __instance.NetworkisTransformed = false;
                        Player.DestroyEffectHUD("GreatswordTransformed");
                    }
                }
                if (Player.IsWeapon(WeaponIDs.Minimalism, WeaponIDs.TheSecond, WeaponIDs.BlackIronHalberd))
                {
                    __instance.outsideBonus = false;
                    __instance.doubleWhirlwind = false;
                }

                if (Player.IsWeapon(WeaponIDs.TroublingDelight, WeaponIDs.LegacyofTheIllusionist, WeaponIDs.Lazarion, WeaponIDs.PristineForm))
                {
                    if (__instance.entityId == WeaponIDs.TroublingDelight ||
                        __instance.entityId == WeaponIDs.LegacyofTheIllusionist ||
                        __instance.entityId == WeaponIDs.Lazarion ||
                        __instance.entityId == WeaponIDs.PristineForm)
                        __instance.attackWeightPerSwing = 0.22f;
                }
                if (Player.IsWeapon(WeaponIDs.LegacyofTheIllusionist) && __instance.entityId == WeaponIDs.LegacyofTheIllusionist)
                {
                    if (LegacyofTheIllusionistAttack.TryGetValue(Player.Name, out LegacyofTheIllusionistAttackData Attack))
                        if (Player.IsInBattle)
                            Attack.Update(__instance, Player);
                }
                if (Player.IsWeapon(WeaponIDs.BloodlettingGearblade) && __instance.entityId == WeaponIDs.BloodlettingGearblade)
                {
                    if (!__instance.NetworkisTransformed) __instance.Networkowner.animator.speed = 1f;
                }
                return false;
            }

            [HarmonyPatch("AttackButtonDown")]
            [HarmonyPrefix]
            static bool AttackButtonDown_Prefix(WeaponSimple_GreatSword __instance, int dashAttack)
            {
                if (dashAttack > 0)
                {
                    UnitAvatar Player = __instance.Networkowner.unitAvatar;
                    if (Player.IsWeapon(WeaponIDs.Minimalism, WeaponIDs.TheSecond, WeaponIDs.BlackIronHalberd))
                    {
                        if (Player.parryInvincibleApplied <= 0)
                        {
                            __instance.Networkowner.CancelAction();
                            __instance.Networkowner.SetAnimationTrigger(AnimHashContainer.Instance.DashAttackHash);
                            int UseStack = 1;
                            if (MinimalismDashAttack.TryGetValue(Player.Name, out int Stack) && Stack > 0)
                                UseStack += Stack;
                            __instance.Networkowner.StartParryInvincible(0.1f * UseStack);
                            return false;
                        }
                    }
                }
                return true;
            }

            [HarmonyPatch("get_SweepCost")]
            [HarmonyPostfix]
            static void SweepCost_Postfix(WeaponSimple_GreatSword __instance, ref int __result)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player.IsWeapon(WeaponIDs.LightningGreatSwordE3G) && __instance.entityId == WeaponIDs.LightningGreatSwordE3G)
                {
                    __result = 0;
                }
                if (Player.IsWeapon(WeaponIDs.Lazarion) && __instance.entityId == WeaponIDs.Lazarion)
                {
                    __result = __result.IntMult(10f / 24f);
                }
            }

            [HarmonyPatch("SubAttackButtonUp")]
            [HarmonyPostfix]
            static void SubAttackButtonUp_Postfix(WeaponSimple_GreatSword __instance)
            {
                if (!NetworkServer.active || !__instance.Networkowner)
                {
                    return;
                }

                if (sweepRequestField == null)
                {
                    LogError("[WeaponSimple_GreatSword][SubAttackButtonUp]反射获取失败");
                    return;
                }

                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player.GetCustomStatUnsafe(TextFile.InvincinleSpiceialAttack) > 0)
                {
                    if ((bool)sweepRequestField.GetValue(__instance))
                    {
                        if (!InvincibleSpecialAttackCD.ContainsKey(Player))
                        {
                            __instance.Networkowner.StartParryInvincible(0.6f);
                            InvincibleSpecialAttackCD.Add(Player, new Timer(1.2f));
                        }
                    }
                }
            }

            [HarmonyPatch("OnSpecialAttackCreated")]
            [HarmonyPostfix]
            static void OnSpecialAttackCreated_Postfix(WeaponSimple_GreatSword __instance, int idx, ProjectileBase projectile)
            {
                if (!NetworkServer.active)
                {
                    return;
                }

                if (__instance.rapidWhirlwind && (bool)projectile)
                {
                    UnitAvatar Player = __instance.Networkowner.unitAvatar;
                    projectile.additionalCriticalChancePercent -= 50f;
                    projectile.additionalCriticalDamageRate += Player.GetCustomStatUnsafe(TextFile.CriticalDamageBonus).IntMult(1f);
                }
            }
        }

        [HarmonyPatch(typeof(WeaponSimple_QuartterStaff))]
        public static class WeaponSimple_QuartterStaff_Patches
        {
            public static FieldInfo? isAttackButtonDownField2 = AccessTools.Field(typeof(WeaponSimple_QuartterStaff), "isAttackButtonDown");
            public static FieldInfo? isRollingProtectionCooldownField = AccessTools.Field(typeof(WeaponSimple_QuartterStaff), "isRollingProtectionCooldown");
            public static FieldInfo? currentRollingProtectionTimeField = AccessTools.Field(typeof(WeaponSimple_QuartterStaff), "currentRollingProtectionTime");
            public static FieldInfo? remainingRollingProtectionShieldValueField = AccessTools.Field(typeof(WeaponSimple_QuartterStaff), "remainingRollingProtectionShieldValue");
            public static FieldInfo? isRollingProtectionActiveField = AccessTools.Field(typeof(WeaponSimple_QuartterStaff), "isRollingProtectionActive");
            public static FieldInfo? normalAttackRollingTimeServersideField = AccessTools.Field(typeof(WeaponSimple_QuartterStaff), "normalAttackRollingTimeServerside");
            public static FieldInfo? currentRollingRotationSpeedEachClientField = AccessTools.Field(typeof(WeaponSimple_QuartterStaff), "currentRollingRotationSpeedEachClient");

            [HarmonyPatch("LateUpdate")]
            [HarmonyPrefix]
            static bool LateUpdate_Prefix(WeaponSimple_QuartterStaff __instance)
            {
                // 初始化反射
                if (isAttackButtonDownField2 == null || isRollingProtectionCooldownField == null ||
                    currentRollingProtectionTimeField == null || remainingRollingProtectionShieldValueField == null ||
                    isRollingProtectionActiveField == null || normalAttackRollingTimeServersideField == null ||
                    currentRollingRotationSpeedEachClientField == null)
                {
                    LogError("[WeaponSimple_QuartterStaff][LateUpdate]反射获取失败");
                    return true;
                }

                // 通过反射获取字段值
                bool isAttackButtonDown = (bool)(isAttackButtonDownField2.GetValue(__instance) ?? false);
                bool isRollingProtectionCooldown = (bool)(isRollingProtectionCooldownField.GetValue(__instance) ?? false);
                float currentRollingProtectionTime = (float)(currentRollingProtectionTimeField.GetValue(__instance) ?? 0f);
                float remainingRollingProtectionShieldValue = (float)(remainingRollingProtectionShieldValueField.GetValue(__instance) ?? 0f);
                bool isRollingProtectionActive = (bool)(isRollingProtectionActiveField.GetValue(__instance) ?? false);
                float normalAttackRollingTimeServerside = (float)(normalAttackRollingTimeServersideField.GetValue(__instance) ?? 0f);
                float currentRollingRotationSpeedEachClient = (float)(currentRollingRotationSpeedEachClientField.GetValue(__instance) ?? 0f);

                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                WeaponControllerSimple Contorller = __instance.Networkowner;

                if (Player.IsWeapon(WeaponIDs.ScaleOfTheAstralSea) && __instance.entityId == WeaponIDs.ScaleOfTheAstralSea)
                {
                    if (Player.GetCustomStatUnsafe(TextFile.ScaleOfTheAstralSeaEffect) > 0)
                        __instance.NetworkcanCrystalExplosion = true;
                    else
                        __instance.NetworkcanCrystalExplosion = false;
                }
                if (__instance.isServer)
                {

                    if (!__instance.isNormalAttackRolling && isAttackButtonDown &&
                        Contorller.currentWeaponSwing == -1 && !Contorller.allowedInputBufferName.Equals("DASHATTACK", StringComparison.OrdinalIgnoreCase) &&
                        Player.CanMove && Player.blockMoveByInput <= 0)
                    {
                        Contorller.NetworkattackDirection = Contorller.aimedPositionClientside - Contorller.transform.position;
                        if (Player.GetCustomStatUnsafe("STAFFGUARDEXTEND") > 0)
                        {
                            int constValue = KeywordDatabase.GetConstValue("staffGuardExtendCombo", true);
                            Contorller.SetAnimationInt(AnimHashContainer.Instance.FuryNumHash, constValue);
                        }
                        else if (__instance.canCrystalExplosion)
                        {
                            int constValue2 = KeywordDatabase.GetConstValue("staffCrystalExplosionFuryNum", true);
                            Contorller.SetAnimationInt(AnimHashContainer.Instance.FuryNumHash, constValue2);
                        }
                        else
                        {
                            Contorller.SetAnimationInt(AnimHashContainer.Instance.FuryNumHash, 0);
                        }
                        Contorller.SetAnimationTrigger(AnimHashContainer.Instance.AttackHash);
                    }
                    Contorller.SetAnimationBool(AnimHashContainer.Instance.RollingAttackHash, __instance.isNormalAttackRollingTurnedOn);

                    if (Player.IsWeapon(WeaponIDs.StaffOfGreatMight) && __instance.entityId == WeaponIDs.StaffOfGreatMight)
                    {
                        if (!isRollingProtectionCooldown)
                        {
                            isRollingProtectionCooldown = true;

                            float Time = KeywordDatabase.GetConstValue("staffRollingProtectionTime", true);
                            float ShieldValue = Player.GetCustomStatUnsafe("DAMAGEREDUCTION") * Player.GetCustomStatUnsafe("STAFFROLLINGPROTECTION") / 100f;
                            if (ModConfig.Instance.EnableWeaponModifier)
                            {
                                float TimeMult = 1f / (1f + Player.GetCustomStatUnsafe("DAMAGEREDUCTION") * 0.01f);
                                Time = 8f * TimeMult;
                                ShieldValue = 5f;
                            }
                            currentRollingProtectionTime = Time;
                            remainingRollingProtectionShieldValue = ShieldValue;
                            if (isRollingProtectionActive)
                            {
                                Player.RemoveShield("STAFF_ROLLING_PROTECTION");
                                isRollingProtectionActive = false;
                            }
                        }
                        else
                        {
                            currentRollingProtectionTime -= Time.deltaTime;
                            if (currentRollingProtectionTime <= 0f)
                            {
                                isRollingProtectionCooldown = false;
                            }
                        }
                    }

                    if (__instance.isNormalAttackRollingTurnedOn)
                    {
                        if (remainingRollingProtectionShieldValue > 0f)
                        {
                            if (!isRollingProtectionActive)
                            {
                                Player.AddShield(remainingRollingProtectionShieldValue, "STAFF_ROLLING_PROTECTION");
                                isRollingProtectionActive = true;
                            }
                            remainingRollingProtectionShieldValue = Mathf.Min(remainingRollingProtectionShieldValue, Player.Shield);
                        }
                    }
                    else if (isRollingProtectionActive)
                    {
                        Player.RemoveShield("STAFF_ROLLING_PROTECTION");
                        isRollingProtectionActive = false;
                    }

                    // 将修改后的值设置回实例
                    isRollingProtectionCooldownField.SetValue(__instance, isRollingProtectionCooldown);
                    currentRollingProtectionTimeField.SetValue(__instance, currentRollingProtectionTime);
                    remainingRollingProtectionShieldValueField.SetValue(__instance, remainingRollingProtectionShieldValue);
                    isRollingProtectionActiveField.SetValue(__instance, isRollingProtectionActive);
                }
                if (__instance.isNormalAttackRollingTurnedOn)
                {
                    float num = Time.deltaTime / __instance.attackWeightPerSwing;
                    num += num * Player.GetCustomStatUnsafe("ATTACKSPEED") / 100f;
                    normalAttackRollingTimeServerside -= num;
                    if (normalAttackRollingTimeServerside <= 0f)
                    {
                        normalAttackRollingTimeServerside = 1f;
                        if (__instance.isServer)
                        {
                            Vector3 vector;
                            if (__instance.rollingAttackRotationDirection)
                            {
                                Contorller.SetAttackDirection(Contorller.aimedPositionClientside);
                                vector = Contorller.attackDirection;
                            }
                            else
                            {
                                vector = Vector3.up;
                            }
                            Contorller.BeginAttackAnimationFullyManual(0);
                            __instance.CreateBasicAttackProjectileFullyManual(__instance.rollingFireData, 0, Contorller.shoulder.swingPoint.position, vector, null, 0f, 0, 1f);
                            Contorller.EndAttackAnimationFullyManual();
                        }
                        if (__instance.isClient)
                        {
                            __instance.CreateSwingFx(__instance.rollingFireData, true, new Vector3(0f, 0f, UnityEngine.Random.Range(-180f, 180f)));
                        }
                    }
                    // 更新服务器端时间
                    normalAttackRollingTimeServersideField.SetValue(__instance, normalAttackRollingTimeServerside);
                }
                if (__instance.isClient)
                {
                    if (__instance.isNormalAttackRollingTurnedOn)
                    {
                        currentRollingRotationSpeedEachClient = Mathf.MoveTowards(currentRollingRotationSpeedEachClient, __instance.rollingRotationSpeed, Time.deltaTime * __instance.rollingRotationSpeed * 3f);
                        if (__instance.mainWeaponRenderer)
                        {
                            __instance.mainWeaponRenderer.transform.Rotate(0f, 0f, -currentRollingRotationSpeedEachClient * Time.deltaTime);
                        }
                    }
                    else
                    {
                        currentRollingRotationSpeedEachClient = 0f;
                        float num2 = __instance.mainWeaponRenderer.transform.localEulerAngles.z;
                        num2 = Mathf.MoveTowards(num2, 0f, Time.deltaTime * 720f);
                        __instance.mainWeaponRenderer.transform.localEulerAngles = new Vector3(0f, 0f, num2);
                    }
                    // 更新客户端旋转速度
                    currentRollingRotationSpeedEachClientField.SetValue(__instance, currentRollingRotationSpeedEachClient);

                    if (__instance.mainWeaponBody && __instance.mainWeaponBody.weaponStencilRenderer && __instance.mainWeaponRenderer)
                    {
                        __instance.mainWeaponBody.weaponStencilRenderer.transform.localEulerAngles = __instance.mainWeaponRenderer.transform.localEulerAngles;
                    }
                    if (Contorller && Player && __instance.mainWeaponBody && __instance.mainWeaponBody.bladeArea)
                    {
                        float num3 = Player.GetCustomStatUnsafe("STAFFEXTEND") * __instance.extendPixelPerUnit;
                        __instance.currentStaffExtend = Mathf.MoveTowards(__instance.currentStaffExtend, num3, Time.deltaTime * 10f);
                        Vector2 size = __instance.mainWeaponBody.bladeArea.size;
                        if (__instance.mainWeaponBody.weaponSpriteRenderer)
                        {
                            __instance.mainWeaponBody.weaponSpriteRenderer.size = new Vector3(size.x, size.y + __instance.currentStaffExtend);
                        }
                        if (__instance.mainWeaponBody.weaponStencilRenderer)
                        {
                            __instance.mainWeaponBody.weaponStencilRenderer.size = new Vector3(size.x, size.y + __instance.currentStaffExtend);
                        }
                        if (__instance.mainWeaponBody.bladeAddOnRenderer)
                        {
                            __instance.mainWeaponBody.bladeAddOnRenderer.size = new Vector3(size.x, size.y + __instance.currentStaffExtend);
                        }
                    }
                }
                return false;
            }

            [HarmonyPatch("LateUpdate")]
            [HarmonyPostfix]
            static void LateUpdate_Postfix(WeaponSimple_QuartterStaff __instance)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player != null)
                {
                    if (Player.IsWeapon(WeaponIDs.ScaleOfTheAstralSea) && __instance.entityId == WeaponIDs.ScaleOfTheAstralSea)
                    {
                        if (!GlobalTimer.ContainsKey(Player))
                            GlobalTimer[Player] = new Timer(1f);
                        if (GlobalTimer.TryGetValue(Player, out Timer TValue))
                        {
                            if (TValue.Update(Time.deltaTime))
                            {
                                float Base = Player.MaxMp;
                                int Use = 3;
                                int Add = 0;
                                while(Base > 0)
                                {
                                    Add += 1;
                                    Base -= Use;
                                    Use += 3;
                                }
                                __instance.Networkowner.animator.speed = 1f + Add * 0.01f;
                            }
                        }
                    }
                }
            }

            [HarmonyPatch("OnAttack_Dash")]
            [HarmonyPostfix]
            static void OnAttack_Dash_Postfix(WeaponSimple_QuartterStaff __instance, CombatBehaviour victim, DamageInstance damage, ProjectileBase projectile)
            {
                if (!ModConfig.Instance.EnableWeaponModifier) return;

                if (__instance.NetworkoverrideDashAttackAddon)
                {
                    UnitAvatar? Target = victim as UnitAvatar;
                    if (Target != null && Target && !Target.IsDead && __instance.changedDashAttackParameter == "CLOUDBOTTLE")
                    {
                        UnitAvatar Player = __instance.Networkowner.unitAvatar;
                        int Count = Player.GetCustomStatUnsafe(TextFile.DarkCloudMultishot);
                        if (Count > 1)
                            ModCloudAttack.Add(new CloudAttackData(Player, Target, 0.7f, 0.2f, 1));
                    }
                }
            }

            [HarmonyPatch("OnBeginDashServerside")]
            [HarmonyPostfix]
            static void OnBeginDashServerside_Postfix(WeaponSimple_QuartterStaff __instance, Vector2 direction, bool isDashCostPaid)
            {
                if (!isDashCostPaid) return;
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player.IsWeaponUseNet(WeaponIDs.RuadhsleaSpear) && __instance.entityId == WeaponIDs.RuadhsleaSpear)
                {
                    if (RuadhsleaSpearBuff.ContainsKey(Player.Name))
                    {
                        RuadhsleaSpearBuff[Player.Name] += 1;
                        if (RuadhsleaSpearBuff[Player.Name] > Player.CurrentDashModule.MaxDashCount)
                            RuadhsleaSpearBuff[Player.Name] = Player.CurrentDashModule.MaxDashCount;
                    }
                    else RuadhsleaSpearBuff.Add(Player.Name, 1);

                    float OnTime = 4f;
                    OnTime *= 1f + Player.GetCustomStatUnsafe(TextFile.BuffDuration) * 0.01f;
                    RuadhsleaSpearTime[Player.Name] = new Timer(OnTime, false);
                }
            }

            [HarmonyPatch("OnDashAttackCreated")]
            [HarmonyPrefix]
            static void OnDashAttackCreated_Prefix(WeaponSimple_QuartterStaff __instance, int idx, ProjectileBase projectile)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (__instance.lastDashCount > 0 && Player.IsWeapon(WeaponIDs.FuarscaSpear) && __instance.entityId == WeaponIDs.FuarscaSpear)
                {
                    int Add = __instance.lastDashCount * __instance.lastDashCount * Player.GetCustomStatUnsafe(TextFile.SpearDashAttackBonusByLastDash) / 2;
                    float Mult = 1f + Add / 100f;
                    projectile.defaultDamageRatio *= Mult;
                    __instance.NetworklastDashCount = 0;
                }
            }

            [HarmonyPatch("HandleGuard")]
            [HarmonyPostfix]
            static void HandleGuard_Postfix(WeaponSimple_QuartterStaff __instance, DamageInstance damage, bool perfectGuard)
            {
                UnitAvatar Player = __instance.Networkowner.unitAvatar;
                if (Player.IsWeaponUseNet(WeaponIDs.ScaleOfTheAstralSea) && __instance.entityId == WeaponIDs.ScaleOfTheAstralSea)
                {
                    if (Player.GetCustomStatUnsafe(TextFile.ScaleOfTheAstralSeaEffect) < 10)
                        Player.AddCustomStatUnsafe(TextFile.ScaleOfTheAstralSeaEffect, 1);
                }
                if (Player.IsWeaponUseNet(WeaponIDs.FiaolasJudgmentStaff) && __instance.entityId == WeaponIDs.FiaolasJudgmentStaff)
                {
                    float OnTime = 8f;
                    OnTime *= 1f + Player.GetCustomStatUnsafe(TextFile.BuffDuration) * 0.01f;
                    FiaolasJudgmentStaffBuff[Player] = new Timer(OnTime, false);
                }
            }
        }

        [HarmonyPatch(typeof(WeaponAddonCommon_TransformAttackBonus))]
        public static class WeaponAddonCommon_TransformAttackBonus_Patches
        {
            [HarmonyPatch("OnBasicAttackCreated")]
            [HarmonyPrefix]
            static bool OnBasicAttackCreated_Prefix(WeaponAddonCommon_TransformAttackBonus __instance, int arg1, ProjectileBase projectile)
            {
                if (ModConfig.Instance.EnableWeaponModifier)
                {
                    if (__instance.parent.GetWeaponParameter("IsTransformed") == 1)
                    {
                        MeleeCollision? meleeCollision = projectile as MeleeCollision;
                        if (meleeCollision != null)
                        {
                            if (__instance.parent.Networkowner)
                            {
                                UnitAvatar Player = __instance.parent.Networkowner.unitAvatar;
                                float Damage = Player.GetCustomStatUnsafe(__instance.relatedStatUnsafe) * 1.75f;
                                Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.BasicAttackDamageBonus) * 0.01f;
                                Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                                Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.MeleeDamage) * 0.01f;
                                Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.DirectWeaponDamage) * 0.01f;
                                Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.MPSkillDamage) * 0.01f;
                                meleeCollision.additionalDamage += Damage;
                            }
                        }
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(WeaponAddonGreatsword_BoneBlood))]
        public static class WeaponAddonGreatsword_BoneBlood_Patches
        {
            public static FieldInfo? bloodStackField = AccessTools.Field(typeof(WeaponAddonGreatsword_BoneBlood), "bloodStack");
            public static FieldInfo? isEngagedField = AccessTools.Field(typeof(WeaponAddonGreatsword_BoneBlood), "isEngaged");

            [HarmonyPatch("Engage")]
            [HarmonyPrefix]
            static void Engage_Prefix(WeaponAddonGreatsword_BoneBlood __instance)
            {
                if (!ModConfig.Instance.EnableWeaponModifier) return;

                UnitAvatar Player = __instance.parent.Networkowner.unitAvatar;
                __instance.maxHpPerBonusAmount = 0;
                __instance.damagePerBloodStack = 0;
                __instance.receivedDamageReductionPercent = 40;
                if (__instance.parent is WeaponSimple_GreatSword GreatSword)
                {
                    float Mult = Tools.GetExMP(Player) / 5f;
                    GreatSword.Networkowner.animator.speed = 1f + 0.01f * Mult;
                }
            }

            [HarmonyPatch("Engage")]
            [HarmonyPostfix]
            static void Engage_Postfix(WeaponAddonGreatsword_BoneBlood __instance)
            {
                if (!ModConfig.Instance.EnableWeaponModifier) return;
                if (bloodStackField == null) return;
                int bloodStack = (int)bloodStackField.GetValue(__instance);
                __instance.parent.Networkowner.unitAvatar.SetEffectHUDValue("GreatswordTransformed", bloodStack.ToString());
            }

            [HarmonyPatch("OnBasicAttackCreated")]
            [HarmonyPrefix]
            static void OnBasicAttackCreated_Prefix(WeaponAddonGreatsword_BoneBlood __instance, int idx, ProjectileBase projectile)
            {
                if (!ModConfig.Instance.EnableWeaponModifier) return;
                if (bloodStackField == null) return;
                if (projectile != null)
                {
                    int bloodStack = (int)bloodStackField.GetValue(__instance);
                    if (projectile is MeleeCollision meleeCollision)
                    {
                        meleeCollision.damage *= 2f / 1.75f;
                        meleeCollision.damage *= 1f + bloodStack * 0.01f;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SpecialProjectile_WoundExplosion))]
        public static class SpecialProjectile_WoundExplosion_Patch
        {
            public static FieldInfo? onAttackField = AccessTools.Field(typeof(SpecialProjectile_WoundExplosion), "onAttack");
            public static MethodInfo? RpcCollisionMethod = AccessTools.Method(typeof(SpecialProjectile_WoundExplosion), "RpcCollision");

            [HarmonyPatch("CheckCollision")]
            [HarmonyPostfix]
            static void CheckCollision_Postfix(SpecialProjectile_WoundExplosion __instance, Collider2D[] hits, ref int __result)
            {
                if (ModConfig.Instance.EnableWeaponModifier)
                {
                    __result = Mathf.Min(__result, 1);
                }
            }

            [HarmonyPatch("Attack")]
            [HarmonyPrefix]
            static bool Attack_Prefix(SpecialProjectile_WoundExplosion __instance, int type, Vector3 hitPoint, Transform hitTransform, ref bool __result)
            {
                if (ModConfig.Instance.EnableWeaponModifier)
                {
                    if (onAttackField == null || RpcCollisionMethod == null)
                    {
                        LogError("[SpecialProjectile_WoundExplosion][Attack]反射获取失败");
                        return true;
                    }
                    Action<CombatBehaviour, DamageInstance, ProjectileBase> onAttack = (Action<CombatBehaviour, DamageInstance, ProjectileBase>)onAttackField.GetValue(__instance);
                    foreach (UnitAvatar Target in CombatManager.Instance.AllCreatures.ToList())
                    {
                        if (Target == __instance.owner) continue;
                        if (Target.hp <= 0) continue;
                        if (Target.IsDead) continue;
                        if (Target.canBeTarget.IsFalse()) continue;
                        if (!Target.gameObject.activeSelf) continue;
                        if (Vector3.Distance(__instance.owner.transform.position, Target.transform.position) > 8f) continue;
                        if (!CombatManager.ContainsAttackableFaction(Target.GetHostileFactionLayers(EDamageFromType.None), __instance.owner.faction)) continue;

                        int DebuffStack = 0;
                        int WoundStack = 0;
                        foreach (CharacterDebuff Debuff in Target.Debuffs)
                        {
                            if (Debuff.NetworkAttacker == __instance.owner)
                            {
                                if (Debuff.ID != "WOUND")
                                {
                                    DebuffStack += Debuff.CurrentStack;
                                    Debuff.RequestEnd();
                                }
                                else WoundStack += Debuff.CurrentStack;
                            }
                        }
                        {
                            float Damage = __instance.owner.GetCustomStat(ECustomStat.PhysicalDamage) * 3f;
                            Damage *= 1f + 0.33f * WoundStack;

                            float Mult = 1f + __instance.owner.GetCustomStat(ECustomStat.FinalWeaponDamage) * 0.01f;
                            Mult *= 1f + __instance.owner.GetCustomStat(ECustomStat.SpecialAttackDamageBonus) * 0.01f;
                            Mult *= 1f + __instance.owner.GetCustomStatUnsafe(TextFile.DirectWeaponDamage) * 0.01f;

                            Vector3 Vec = hitTransform.position - __instance.transform.position;

                            EDamageType damageType = EDamageType.Slice;
                            for (int i = 0; i < 2; i++)
                            {
                                if (i == 1)
                                {
                                    Damage = 60 + __instance.owner.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.6f;
                                    Damage *= DebuffStack + WoundStack / 2;
                                    damageType = EDamageType.ElementalEffectDamage;
                                    if (Damage <= 0) break;
                                }

                                Damage *= Mult;
                                DamageInstance damageInstance = DamageInstance.GetDamage(__instance.owner, __instance.damageId, hitPoint, __instance.targetTeam, Damage, damageType, EDamageFromType.DirectAttack, Vec, __instance.staggeringLevel, __instance.externalForcePower);
                                damageInstance.flag = __instance.swingId;
                                damageInstance.ignoreDefense += __instance.ignoreDefense;
                                damageInstance.criticalChancePercent += __instance.additionalCriticalChancePercent;
                                damageInstance.criticalDamageRate += __instance.additionalCriticalDamageRate;
                                damageInstance.damage *= 1f + __instance.additionalDamagePercent * 0.01f;
                                damageInstance.SetCustomColor(true, __instance.woundExplosionColor);
                                damageInstance.ignoreDebuff = true;
                                if (Target.ApplyDamage(damageInstance) == EApplyDamageResult.Success)
                                {
                                    onAttack.Invoke(Target, damageInstance, __instance);
                                    RpcCollisionMethod.Invoke(__instance, new object[] { (Vector2)__instance.transform.position, (Vector2)Target.transform.position });
                                    __result = true;
                                }
                            }
                        }
                    }
                    return false;
                }
                return true; // 跳过原方法
            }
        }

        [HarmonyPatch(typeof(SpecialProjectile_DuplicateMagic))]
        public static class SpecialProjectile_DuplicateMagic_Patch
        {
            [HarmonyPatch("OnUpdateServer")]
            [HarmonyPrefix]
            static bool OnUpdateServer_Prefix(SpecialProjectile_DuplicateMagic __instance)
            {
                if (!__instance.isServer) return true;
                try
                {
                    if (__instance.Networkowner && __instance.Networkowner.TryGetComponent<SkillController>(out SkillController skillController))
                    {
                        Charm_Magic lastUsedMagic = skillController.GetLastUsedMagic();
                        if (lastUsedMagic)
                        {
                            lastUsedMagic.FireCasting(__instance.motionDataBegin, __instance.motionDataEnd, __instance.height, 0, false, false, null);
                            if (!TomeOfMimicryEffectTime.ContainsKey(__instance.Networkowner.Name))
                            {
                                float UTime = lastUsedMagic.ContainedMagic.cooldownTime;
                                UTime /= 1f + __instance.Networkowner.GetCustomStatUnsafe(TextFile.SpecialAttackDamageBonus) * 0.01f;
                                TomeOfMimicryEffectTime.Add(__instance.Networkowner.Name, new Timer(UTime));
                            }
                        }
                        if (!ModConfig.Instance.EnableWeaponModifier)
                            skillController.ClearLastUsedMagic();
                    }
                    __instance.Despawn();
                    return false;
                }
                catch (Exception ex)
                {
                    LogError($"[SpecialProjectile_DuplicateMagic][OnUpdateServer]错误: {ex}");
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(SpecialProjectile_BladeZone))]
        public static class SpecialProjectile_BladeZone_Patch
        {
            public static FieldInfo? attackedCountField = AccessTools.Field(typeof(SpecialProjectile_BladeZone), "attackedCount");

            [HarmonyPatch("Initialize")]
            [HarmonyPostfix]
            static void Initialize_Postfix(SpecialProjectile_BladeZone __instance)
            {
                if (__instance.owner.IsWeapon(WeaponIDs.ShadowofHope))
                {
                    __instance.bladeZoneAttackCount = 10;
                    __instance.NetworkrangeBonus += 0.2f;
                    __instance.damage *= 1f / 0.66f * 0.27f;
                    __instance.damage *= 1f + __instance.owner.GetCustomStatUnsafe(TextFile.AttackSpeed) * 0.01f;
                }
            }

            [HarmonyPatch("OnUpdateServer")]
            [HarmonyPostfix]
            static void OnUpdateServer_Postfix(SpecialProjectile_BladeZone __instance)
            {
                if (!__instance.isServer) return;
                if (__instance.owner.IsWeapon(WeaponIDs.ShadowofHope))
                {
                    if (attackedCountField == null)
                    {
                        LogError("[SpecialProjectile_BladeZone][OnUpdateServer]获取反射失败");
                        return;
                    }
                    attackedCountField.SetValue(__instance, 0);
                }
            }
        }

        [HarmonyPatch(typeof(SpecialProjectile_FuryMP))]
        public static class SpecialProjectile_FuryMP_Patch
        {
            [HarmonyPatch("SetUsedMP")]
            [HarmonyPrefix]
            static void SetUsedMP_Prefix(SpecialProjectile_FuryMP __instance, ref int usedMP)
            {
                if (ModConfig.Instance.EnableWeaponModifier)
                {
                    UnitAvatar Player = __instance.owner;
                    float Mult = 1f + Player.GetCustomStatUnsafe(TextFile.MPRegenMultiple) * 0.01f;
                    float MinMP = Player.MaxMp * 0.3f * Mult;
                    MinMP = Math.Max(MinMP, 56f * Mult) + 1;
                    usedMP = Math.Max(usedMP, (int)MinMP);
                }
            }
        }

        [HarmonyPatch(typeof(SpecialProjectile_ThrowCloudBottle))]
        public static class SpecialProjectile_ThrowCloudBottle_Patch
        {
            public static FieldInfo? bulletCountField = AccessTools.Field(typeof(SpecialProjectile_ThrowCloudBottle), "bulletCount");

            [HarmonyPatch("Initialize")]
            [HarmonyPostfix]
            static void Initialize_Postfix(SpecialProjectile_ThrowCloudBottle __instance)
            {
                if (!ModConfig.Instance.EnableWeaponModifier) return;

                __instance.damage *= 2f;
                if (bulletCountField == null)
                {
                    LogError("[SpecialProjectile_ThrowCloudBottle][Initialize]获取反射失败");
                    return;
                }

                int MinStack = 2;
                if (__instance.owner && __instance.owner.Inventory)
                {
                    foreach (KeyValuePair<ItemPosition, Charm_Basic> keyValuePair in __instance.owner.Inventory.charms)
                    {
                        Charm_Basic value = keyValuePair.Value;
                        if (value != null && value.IsEffectEnabled && value.Item != null)
                        {
                            if (value is Charm_LightningPouch)
                                MinStack += 1;
                        }
                    }
                }
                bulletCountField.SetValue(__instance, MinStack);
            }
        }

        [HarmonyPatch(typeof(ActiveSkill_Arrow))]
        public static class ActiveSkill_Arrow_Patch
        {
            public static FieldInfo? calculatedDamage_ArrowField = AccessTools.Field(typeof(ActiveSkill_Arrow), "calculatedDamage");

            [HarmonyPatch("OnStartMagic")]
            [HarmonyPostfix]
            static void OnStartMagic_Postfix(ActiveSkill_Arrow __instance)
            {
                if (!ModConfig.Instance.ModifyCharm) return;

                if (calculatedDamage_ArrowField == null)
                {
                    LogError("[ActiveSkill_Arrow][OnStartMagic]获取反射失败");
                    return;
                }

                float Damage = __instance.defaultDamageByLevel.SafeRandomAccess(__instance.Level);
                Damage += __instance.NetworkCaster.GetCustomStat(__instance.relatedDamage) * __instance.damagePercentByLevel.SafeRandomAccess(__instance.Level) * 0.01f;
                Damage *= 1f + __instance.NetworkCaster.GetCustomStat(ECustomStat.MagicDamageBonus) * 0.01f;
                Damage *= 1f + __instance.MpSkillDamage * 0.01f;
                Damage *= __instance.power;
                calculatedDamage_ArrowField.SetValue(__instance, Damage);
            }
        }

        [HarmonyPatch(typeof(ActiveSkill_Bolt))]
        public static class ActiveSkill_Bolt_Patch
        {
            public static FieldInfo? calculatedDamage_BoltField = AccessTools.Field(typeof(ActiveSkill_Bolt), "calculatedDamage");

            [HarmonyPatch("OnStartMagic")]
            [HarmonyPostfix]
            static void OnStartMagic_Postfix(ActiveSkill_Bolt __instance)
            {
                if (!__instance.NetworkCaster.IsWeapon(WeaponIDs.IlonicasSpiritStaff)) return;

                if (calculatedDamage_BoltField == null)
                {
                    LogError("[ActiveSkill_Bolt][OnStartMagic]获取反射失败");
                    return;
                }

                float LevelMult = __instance.damagePercentByLevel.SafeRandomAccess(__instance.Level) * 0.01f;
                float BaseDamage = __instance.defaultDamageByLevel.SafeRandomAccess(__instance.Level);
                BaseDamage += __instance.NetworkCaster.GetCustomStatUnsafe(TextFile.PhysicalDamage) * 0.36f;
                float Damage = BaseDamage + __instance.NetworkCaster.GetCustomStat(__instance.relatedDamage) * LevelMult;
                Damage *= 1f + __instance.NetworkCaster.GetCustomStat(ECustomStat.MagicDamageBonus) * 0.01f;
                Damage *= 1f + __instance.MpSkillDamage * 0.01f;
                if (__instance.Cost > 0)
                {
                    float Value = __instance.Cost / 10f;
                    Damage *= 1f + Value * __instance.NetworkCaster.GetCustomStatUnsafe("MAGICMP") * 0.01f;
                }
                Damage *= 1f + __instance.NetworkCaster.GetCustomStat(ECustomStat.BasicAttackDamageBonus) * 0.01f;
                Damage *= 1f + __instance.NetworkCaster.GetCustomStat(TextFile.FinalWeaponDamage) * 0.01f;
                Damage *= __instance.power;
                calculatedDamage_BoltField.SetValue(__instance, Damage);
            }
        }

        [HarmonyPatch(typeof(KatanaGhost))]
        public static class KatanaGhost_Patches
        {
            [HarmonyPatch("Update")]
            [HarmonyPrefix]
            static void Update_Prefix(KatanaGhost __instance)
            {
                if (!__instance.isServer) return;

                UnitAvatar Player = __instance.weapon.Networkowner.unitAvatar;
                if (Player != null)
                {
                    if (Player.IsWeapon(WeaponIDs.LibrarySpectralSealingSociety, WeaponIDs.LibrarySpectralSealingSociety2))
                    {
                        __instance.dashAttackCount = 2;
                        if (__instance.dashAttackIntervalTimer.Ratio < 0.99f)
                        {
                            __instance.dashAttackIntervalTimer.Ratio = 0.99f;
                        }
                    }
                    if (Player.IsWeapon(WeaponIDs.LibrarySpectralSealingSociety2))
                    {
                        __instance.dashAttackCount += Player.GetCustomStatUnsafe(TextFile.AttackSpeed) / 50;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GreenBat))]
        public static class GreenBat_Patches
        {
            private static readonly List<int> PlantList = new List<int>();
            private static readonly Dictionary<string, List<int>> AttackPlant = new Dictionary<string, List<int>>();
            private static GameObject? ShootBullet = null;
            private static GameObject? BigShootBullet = null;
            public static FieldInfo? charmField = AccessTools.Field(typeof(GreenBat), "charm");

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            static void Update_Postfix(GreenBat __instance)
            {
                if (!__instance.isServer) return;
                if (charmField == null)
                {
                    LogError("[GreenBat][Update_Postfix]反射获取失败");
                    return;
                }
                Charm_SummonGreenBat charm = (Charm_SummonGreenBat)charmField.GetValue(__instance);
                if (charm == null) return;
                UnitAvatar Player = charm.NetworkAvatar;
                if (Player != null)
                {
                    if (ModConfig.Instance.EnableWeaponModifier)
                    {
                        if (Player.IsWeapon(WeaponIDs.StarTwinkle))
                        {
                            if (__instance.fireIntervalTimer.Ratio < 0.2f)
                                __instance.fireIntervalTimer.Ratio = 0.2f;
                        }

                        int Id = charm.Item.EntityID;
                        if (!PlantList.Contains(Id)) PlantList.Add(Id);

                        if (UsePlanetShoot.TryGetValue(Player.Name, out bool falg) && falg)
                        {
                            AttackPlant[Player.Name] = PlantList.ToList();
                            UsePlanetShoot[Player.Name] = false;
                        }
                        if (AttackPlant.TryGetValue(Player.Name, out List<int> Value))
                        {
                            if (Value.Contains(Id))
                            {
                                __instance.Fire(1);
                                Value.Remove(Id);
                            }
                        }
                    }
                }
            }

            private static float OrigSequenceFiringTimer = -1f;

            [HarmonyPatch("Fire")]
            [HarmonyPrefix]
            static void Fire_Postfix(GreenBat __instance, ref int amplify)
            {
                if (!__instance.isServer) return;
                if (charmField == null)
                {
                    LogError("[GreenBat][Update_Postfix]反射获取失败");
                    return;
                }
                Charm_SummonGreenBat charm = (Charm_SummonGreenBat)charmField.GetValue(__instance);
                if (charm == null) return;
                if (charm.Item.EntityID != 1015) return;
                UnitAvatar Player = charm.NetworkAvatar;
                if (Player != null)
                {
                    __instance.fireCount = 3;
                    if (Player.GetCustomStatUnsafe(TextFile.EcologicalSphereEffect) > 0 && Player.mp > 1)
                    {
                        __instance.fireCount += Mathf.FloorToInt(Player.MaxMp / 50f);
                    }
                    if (OrigSequenceFiringTimer == -1f) OrigSequenceFiringTimer = __instance.sequenceFiringTimer.time;
                    else
                    {
                        __instance.sequenceFiringTimer.time = OrigSequenceFiringTimer / ((3f + __instance.fireCount) / 3f);
                    }
                }
            }

            [HarmonyPatch("CreateBullet")]
            [HarmonyPostfix]
            static void CreateBullet_Postfix(GreenBat __instance, Vector3 position, Vector3 targetPosition)
            {
                if (!__instance.isServer) return;
                if (charmField == null)
                {
                    LogError("[GreenBat][Update_Postfix]反射获取失败");
                    return;
                }

                Charm_SummonGreenBat charm = (Charm_SummonGreenBat)charmField.GetValue(__instance);
                if (charm == null) return;

                UnitAvatar Player = charm.NetworkAvatar;
                if (!Player || Player == null) return;
                if (!Player.IsWeapon(WeaponIDs.PristineForm)) return;

                float Damage;
                if (charm)
                {
                    Damage = Tools.BaseStatToMax(Player) * 1.5f;
                    Damage *= charm.GetBatDamage() * 0.01f;
                    switch (charm.Item.Entity.rarity)
                    {
                        case EItemRarity.Common:
                            Damage *= 0.6f;
                            break;
                        case EItemRarity.Uncommon:
                            Damage *= 0.9f;
                            break;
                        case EItemRarity.Rare:
                            Damage *= 1.2f;
                            break;
                        case EItemRarity.Legend:
                            Damage *= 1.5f;
                            break;
                    }
                    Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.PlanetDamage) * 0.01f;
                    Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.BasicAttackDamageBonus) * 0.01f;
                    Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                }
                else Damage = 0f;
                GameObject? UsePro = null;
                if (ShootBullet == null)
                {
                    Tools.GetPro("GreatswordLaserThin", out GameObject? Pro);
                    ShootBullet = Pro;
                }
                else UsePro = ShootBullet;
                if (BigShootBullet == null)
                {
                    Tools.GetPro("GreatswordLaser", out GameObject? Pro);
                    BigShootBullet = Pro;
                }
                else if (__instance.isEnhanced)
                {
                    Damage *= 1.5f;
                    UsePro = BigShootBullet;
                }
                if (__instance.enableRandomDir)
                {
                    targetPosition = Player.CurrentLookingPosition;
                }
                if (UsePro != null)
                {
                    Vector3 Vec = Vector3.Normalize(targetPosition - position);
                    position += new Vector3(0f, 0.3f, 0f) + Vec * 0.2f;
                    Bullet a = Bullet.Pool.Spawn(UsePro, position, true, EDamageFromType.None, __instance.damageId, Damage, 0, 0f, Player, Player.GetHostileFactionLayers(EDamageFromType.None), 0.3f, position, targetPosition, null, null, 0f, EDamageElementalType.Chaos, true);
                    a.useCustomColorType = Bullet.ECustomColorType.Chaos;
                    a.customColor = EffectConstant.ChaosColor;
                }
            }
        }

        [HarmonyPatch(typeof(WeaponAddon_Reflect))]
        public static class WeaponAddon_Reflect_Patches
        {
            [HarmonyPatch("HandleGuard")]
            [HarmonyPrefix]
            static bool HandleGuard_Prefix(WeaponAddon_Reflect __instance, DamageInstance obj, bool isPerfectGuard)
            {
                UnitAvatar? Player = __instance.parent.Networkowner.unitAvatar;
                if (Player == null) return true;

                if (Player.IsWeapon(WeaponIDs.StarTwinkle))
                {
                    return false;
                }
                return true;
            }//星光闪烁效果
        }

        [HarmonyPatch(typeof(WeaponAddonKatana_Nastrond))]
        public static class WeaponAddonKatana_Nastrond_Patches
        {
            [HarmonyPatch("OnBeginAttackAnimation")]
            [HarmonyPostfix]
            static void OnBeginAttackAnimation_Postfix(WeaponAddonKatana_Nastrond __instance)
            {
                UnitAvatar Player = __instance.parent.Networkowner.unitAvatar;
                if (Player == null) return;

                if (Player.IsWeapon(WeaponIDs.FragmentofNastrond) && !__instance.chargingCharm.isAvailable)
                {
                    __instance.defaultDamage = 0;
                    __instance.damagePercent = 400f + Player.GetCustomStatUnsafe(TextFile.FrostRelicAddMult);
                    __instance.chargingCharm.AddTimer(0.334f);
                    if (DraftbladeAttack.ContainsKey(Player))
                        __instance.chargingCharm.AddTimer(0.334f);
                }
            }//纳斯特朗的碎片效果
        }

        [HarmonyPatch(typeof(WeaponAddomCommon_BasicAttackWithFollowerDamage))]
        public static class WeaponAddomCommon_BasicAttackWithFollowerDamage_Patches
        {
            [HarmonyPatch("OnCreatedBasicAttackProjectile")]
            [HarmonyPrefix]
            static bool OnCreatedBasicAttackProjectile_Prefix(WeaponAddomCommon_BasicAttackWithFollowerDamage __instance, ProjectileBase projectile)
            {
                if(ModConfig.Instance.EnableWeaponModifier)
                {
                    if (__instance.parent?.Networkowner?.unitAvatar)
                    {
                        UnitAvatar Player = __instance.parent.Networkowner.unitAvatar;
                        if (projectile is Bullet Pro)
                        {
                            float Damage = Pro.Damage;
                            foreach (UnitAvatar FOLLOWER in Player.Followers)
                            {
                                Damage += FOLLOWER.attack * 0.4f;
                            }
                            Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.FollowerDamage) * 0.01f;
                            Pro.SetDamage(Damage);
                        }
                    }
                    return false;
                }
                return true;
            }//神圣之壶效果
        }

        [HarmonyPatch(typeof(WeaponAddonGreatsword_FireNeedleBullet))]
        public static class WeaponAddonGreatsword_FireNeedleBullet_Patches
        {
            [HarmonyPatch("OnEnableAddon")]
            [HarmonyPostfix]
            static void OnEnableAddon_Postfix(WeaponAddonGreatsword_FireNeedleBullet __instance)
            {
                UnitAvatar Player = __instance.parent.Networkowner.unitAvatar;
                if (Player == null) return;

                if (ModConfig.Instance.EnableWeaponModifier)
                {
                    __instance.needleBulletDamageRatio = 0.26f * 1.2f;
                }
            }//荆棘棒效果
        }

        [HarmonyPatch(typeof(WeaponAddonCommon_GuardBuff))]
        public static class WeaponAddonCommon_GuardBuff_Patches
        {
            [HarmonyPatch("InflictDebuff")]
            [HarmonyPrefix]
            static bool InflictDebuff_Prefix(WeaponAddonCommon_GuardBuff __instance, DamageInstance instance, bool isPerfectGuard)
            {
                try
                {
                    UnitAvatar? Player = __instance.parent.Networkowner.unitAvatar;
                    if (Player == null) return true;
                    if (Player.IsWeapon(WeaponIDs.WeightOfTheSea))
                    {
                        CharacterBuff Buff = Player.ApplyBuff<CharacterBuff>(__instance.buffPrefab, 1f, null, __instance.hudFlashWhenBuffed);
                        int BaseCount = 2;
                        int Value = Player.GetCustomStatUnsafe(TextFile.AttackSpeed);
                        while(Value >= 40)
                        {
                            BaseCount += 1;
                            Value -= 40;
                        }
                        WeightOfTheSeaBuff[Player.Name] = BaseCount;
                        GuardBuff = Buff;
                        return true;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    LogError($"[WeaponAddonCommon_GuardBuff][InflictDebuff_Prefix]出错{ex}");
                    return true;
                }
            }//海之锚效果
        }

        [HarmonyPatch(typeof(WeaponAddon_PerfectGuardBuff))]
        public static class WeaponAddon_PerfectGuardBuff_Patches
        {
            [HarmonyPatch("HandleGuard")]
            [HarmonyPrefix]
            static bool HandleGuard_Prefix(WeaponAddon_PerfectGuardBuff __instance, DamageInstance arg1, ref bool isPerfectGuard)
            {
                try
                {
                    UnitAvatar? Player = __instance.parent.Networkowner.unitAvatar;
                    if (Player == null) return true;
                    if (Player.IsWeapon(WeaponIDs.ColdAnger))
                    {
                        return false;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    LogError($"[WeaponAddon_PerfectGuardBuff][HandleGuard_Prefix]出错{ex}");
                    return true;
                }
            }//冰冷愤怒效果
        }

        [HarmonyPatch(typeof(WeaponAddon_OppositeAttackGuard))]
        public static class WeaponAddon_OppositeAttackGuard_Patches
        {
            public static FieldInfo? guardStartField = AccessTools.Field(typeof(WeaponAddon_OppositeAttackGuard), "guardStart");

            [HarmonyPatch("StartOppositeGuard")]
            [HarmonyPrefix]
            static bool StartOppositeGuard_Prefix(WeaponAddon_OppositeAttackGuard __instance)
            {
                UnitAvatar unitAvatar = __instance.parent.Networkowner.unitAvatar;
                if (unitAvatar.IsWeapon(WeaponIDs.TheIndomitable))
                {
                    if (guardStartField == null)
                    {
                        LogError("[WeaponAddon_OppositeAttackGuard][StartOppositeGuard]获取反射失败");
                        return true;
                    }
                    bool guardStart = (bool)guardStartField.GetValue(__instance);
                    if (guardStart)
                    {
                        return false;
                    }
                    guardStartField.SetValue(__instance, true);
                    __instance.parent.Networkowner.unitAvatar.ActivateGuard(270f, true, 0f);
                    return false;
                }
                return true;
            }//无垠原野效果
        }

        [HarmonyPatch(typeof(WeaponAddonCommon_AddDarkCloudOnAttack))]
        public static class WeaponAddonCommon_AddDarkCloudOnAttack_Patches
        {
            [HarmonyPatch("OnAttackUnit")]
            [HarmonyPrefix]
            static bool OnAttackUnit_Prefix(WeaponAddonCommon_AddDarkCloudOnAttack __instance)
            {
                UnitAvatar Player = __instance.parent.Networkowner.unitAvatar;
                if (Player.IsWeapon(WeaponIDs.LightningGreatSwordE3G))
                {
                    if (Player.Inventory)
                    {
                        ComboEffectBase comboEffectBase = Player.Inventory.FindComboEffect("DARKCLOUD");
                        if (comboEffectBase != null && comboEffectBase is ComboEffect_DarkCloud comboEffect_DarkCloud)
                        {
                            comboEffect_DarkCloud.AddCloud(2);
                        }
                    }
                    return false;
                }
                return true;
            }

            [HarmonyPatch("OnSpecialAttackSwing")]
            [HarmonyPrefix]
            static bool OnSpecialAttackSwing_Prefix(WeaponAddonCommon_AddDarkCloudOnAttack __instance)
            {
                UnitAvatar Player = __instance.parent.Networkowner.unitAvatar;
                if (Player.IsWeapon(WeaponIDs.LightningGreatSwordE3G))
                {
                    return false;
                }
                return true;
            }//电击大剑E3G效果
        }

        [HarmonyPatch(typeof(WeaponAddonCommon_ConditionalStat))]
        public static class WeaponAddonCommon_ConditionalStat_Patches
        {
            [HarmonyPatch("CheckDamage")]
            [HarmonyPrefix]
            static bool CheckDamage_Prefix(WeaponAddonCommon_ConditionalStat __instance, DamageInstance instance)
            {
                if (!__instance.parent.Networkowner || !__instance.parent.Networkowner.unitAvatar || __instance.parent.Networkowner.unitAvatar.IsDead) return true;

                UnitAvatar Player = __instance.parent.Networkowner.unitAvatar;
                if (!Player.IsWeapon(WeaponIDs.RinglessStaff)) return true;
                try
                {
                    float Mult = 0.25f;
                    float Value = Player.GetCustomStatUnsafe(TextFile.Critical);
                    float Value2 = Player.GetCustomStatUnsafe(TextFile.CriticalDamageBonus);
                    Mult -= Value * 0.25f / 10000f;
                    Mult += Value2 * 0.25f / 100f;
                    instance.damage *= 1f + Mult;
                    instance.ManualCritChance(50f, Player.GetCustomStatUnsafe(TextFile.Execution) > 0);
                    return false;
                }
                catch (Exception ex)
                {
                    LogError($"[WeaponAddonCommon_ConditionalStat][CheckDamage]发生错误: {ex}");
                    return true;
                }
            }//禅杖效果
        }

        [HarmonyPatch(typeof(WeaponAddonCommon_AttackBuff))]
        public static class WeaponAddonCommon_AttackBuff_Patches
        {
            [HarmonyPatch("InflictDebuff")]
            [HarmonyPrefix]
            static bool InflictDebuff_Prefix(WeaponAddonCommon_AttackBuff __instance, UnitAvatar avatar, DamageInstance instance)
            {
                UnitAvatar Player = __instance.parent.Networkowner.unitAvatar;
                if (Player.IsWeapon(WeaponIDs.DarkVeil))
                {
                    if (Player.IsInBattle && instance.fromType == EDamageFromType.DirectAttack)
                    {
                        DarkVeilBuff[Player] = Player.ApplyBuff<CharacterBuff>(__instance.buffPrefab, 1f, null, __instance.hudFlashWhenBuffed);
                        if (Player.followers.Count > 1)
                        {
                            for(int i = 1; i < Player.followers.Count; i++)
                                Player.ApplyBuff<CharacterBuff>(__instance.buffPrefab, 1f, null, __instance.hudFlashWhenBuffed);
                        }
                        if (DarkVeilBuff[Player] is CharacterBuff_StatusInstance Stat)
                        {
                            Stat.maxStackCount = 12;
                        }
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(WeaponAddonCommon_DebuffAttack))]
        public static class WeaponAddonCommon_DebuffAttack_Patches
        {
            [HarmonyPatch("InflictDebuff")]
            [HarmonyPrefix]
            static bool InflictDebuff_Prefix(WeaponAddonCommon_DebuffAttack __instance, UnitAvatar avatar, DamageInstance instance)
            {
                UnitAvatar Player = __instance.parent.Networkowner.unitAvatar;
                if (Player.IsWeapon(WeaponIDs.Successor))
                {
                    return false;
                }//继承者
                return true;
            }

            [HarmonyPatch("InflictDebuff")]
            [HarmonyPostfix]
            static void InflictDebuff_Postfix(WeaponAddonCommon_DebuffAttack __instance, UnitAvatar avatar, DamageInstance instance)
            {
                UnitAvatar Player = __instance.parent.Networkowner.unitAvatar;
                if (Player.IsWeapon(WeaponIDs.CrimsonVortex))
                {
                    if (avatar && instance.fromType == EDamageFromType.DirectAttack && !instance.ignoreDebuff)
                    {
                        CharacterDebuff_Wound? Buff = null;
                        foreach (CharacterDebuff_Wound Debuff in avatar.Debuffs.OfType<CharacterDebuff_Wound>())
                        {
                            if (Debuff.NetworkAttacker == Player && Debuff.ID == __instance.debuffPrefab.ID)
                            {
                                Buff = Debuff;
                            }
                        }
                        if (Buff != null)
                        {
                            float Dam = 10 * Buff.CurrentStack;
                            Dam *= Player.GetCustomStatUnsafe(TextFile.PhysicalDamage) * 0.01f;
                            Dam *= 1f + Player.GetCustomStatUnsafe(TextFile.WoundDamage) * 0.01f;
                            Dam *= 1f + Player.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                            DamageInstance damageInstance = DamageInstance.GetDamage(Player, "[Star's Sephiria Mod]撕裂", avatar.transform.position, 4294967295L, Dam, EDamageType.ElementalEffectDamage, EDamageFromType.None, Vector2.zero, 0, 0f);
                            damageInstance.SetCustomColor(changeElementalToChaos: true, Buff.color);
                            avatar.ApplyDamage(damageInstance);
                        }
                    }
                }//深红漩涡
            }
        }

        [HarmonyPatch(typeof(WeaponAddonCommon_BurnRing))]
        public static class WeaponAddonCommon_BurnRing_Patches
        {
            public static FieldInfo? remainingDurationField = AccessTools.Field(typeof(WeaponAddonCommon_BurnRing), "remainingDuration");

            [HarmonyPatch("OnAddedDebuffOnTarget")]
            [HarmonyPostfix]
            static void OnAddedDebuffOnTarget_Postfix(WeaponAddonCommon_BurnRing __instance, CharacterDebuff debuff, string id)
            {
                if (remainingDurationField == null) return;
                UnitAvatar Player = __instance.parent.Networkowner.unitAvatar;
                if (Player.IsWeapon(WeaponIDs.FiaolasJudgmentStaff))
                {
                    if (Player.GetCustomStatUnsafe(TextFile.ElectricIce) > 0)
                    {
                        if(debuff is CharacterDebuff_Frostbite)
                        {
                            remainingDurationField.SetValue(__instance, __instance.burnRingDuration);
                            if (!__instance.isRingEnabled) __instance.NetworkisRingEnabled = true;
                        }
                    }
                }
            }

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            static void Update_Postfix(WeaponAddonCommon_BurnRing __instance)
            {
                if (!__instance.isServer || !__instance.isRingEnabled)
                {
                    return;
                }
                UnitAvatar Player = __instance.parent.Networkowner.unitAvatar;
                if (Player.IsWeapon(WeaponIDs.FiaolasJudgmentStaff))
                {
                    float Mult = Player.GetCustomStatUnsafe(TextFile.BurnEvo) > 0 ? 28 : 18;
                    Mult *= 1f + Player.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.01f;
                    Mult *= 1f + Player.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 0.01f;
                    __instance.damagePercent = (int)Mult;
                }
            }
        }

        [HarmonyPatch(typeof(NewWeaponFireData))]
        public static class NewWeaponFireData_Patches
        {
            [HarmonyPatch("CreateAttack_Before")]
            [HarmonyPrefix]
            static bool CreateAttack_Before_Prefix(NewWeaponFireData __instance, EDamageFromType damageFromType, UnitAvatar owner, string damageId, Vector2 motionFrom, Vector2 motionTo, float height, ref float attackDashScale, bool isIgnoreAttackDash, float rangeBonus)
            {
                if (owner == null) return true;
                if (!(owner is PlayerAvatar)) return true;
                if (ModConfig.Instance.EnableWeaponModifier)
                {
                    if (GetUseWeaponType(owner.GetPlayerWeapon()) == WeaponCategory.Crossbow && Tools.CostumeSkinUse(owner.Name, "Skeleton", "_3"))
                    {
                        attackDashScale *= 0.2f;
                    }
                }
                return true;
            }
        }

        public static WeaponSimple_Katana? LightningFlashSimple = null;
        [HarmonyPatch(typeof(NewWeaponFireData_Bullet))]
        public static class NewWeaponFireData_Bullet_Patches
        {
            [HarmonyPatch("InstantiateProjectile")]
            [HarmonyPrefix]
            static bool InstantiateProjectile_Prefix(NewWeaponFireData_Bullet __instance, EDamageFromType damageFromType, ref float damage, string damageId, bool canBeTransparentOnMultiplayer, UnitAvatar owner, Vector2 motionFrom, Vector2 motionTo, float height, List<CombatBehaviour> sharedTarget, Action<CombatBehaviour, DamageInstance, ProjectileBase> onAttack, float rangeBonus, ref EDamageElementalType elementalType, ref Action<int, ProjectileBase> onCreateAttack, float swingMultiplier, float finalDamageMultiplier)
            {
                if (owner == null) return true;

                string TName = __instance.name.Replace("(Clone)", "");
                LogInfoWeapon($"[NewWeaponFireData_Bullet]被调用 ID:{TName} 攻击倍率:{__instance.damageMultiplier} 攻击伤害:{damage}");
                int DirectWeaponDamage = owner.GetCustomStatUnsafe(TextFile.DirectWeaponDamage);
                if (ModConfig.Instance.EnableWeaponModifier)
                {
                    if (TName == "FireBullet_JellyFish_Frostbite" || TName == "FireBullet_JellyFish_Fire" || TName == "FireBullet_JellyFish_Electric")
                    {
                        if (owner.Leader.IsWeapon(WeaponIDs.KalimsForm))
                        {
                            Action<int, ProjectileBase> originalCallback = onCreateAttack;
                            onCreateAttack = (index, projectile) =>
                            {
                                if (projectile is Bullet bullet)
                                {
                                    bullet.pierceCreatureCount = 99;
                                    bullet.TurnOnHoming(true, null, 0.05f);
                                }
                                originalCallback?.Invoke(index, projectile);
                            };
                        }
                    }
                    if (owner.GetWeaponTypeAndData(out int Type, out (string AttackType, int Idx) Data))
                    {
                        if (Type == WeaponIDs.PurpleJacket)
                        {
                            elementalType = EDamageElementalType.Chaos;
                            Action<int, ProjectileBase> originalCallback = onCreateAttack;
                            onCreateAttack = (index, projectile) =>
                            {
                                if (projectile is Bullet bullet)
                                {
                                    bullet.useCustomColorType = Bullet.ECustomColorType.Chaos;
                                    bullet.chaosDamageColor = new Color(0.72f, 0.44f, 0.82f, 1f);
                                    bullet.customColor = new Color(0.72f, 0.44f, 0.82f, 1f);
                                }
                                originalCallback?.Invoke(index, projectile);
                            };
                        }//紫甲毒蜂
                        if (Type == WeaponIDs.AccelerationCore)
                        {
                            int Stack = owner.GetCustomStatUnsafe(TextFile.CrossbowAdditionalMagazine);
                            damage *= 1f + Stack * 0.2f;
                        }//加速核心
                        if (Type == WeaponIDs.RapidFreezeCrystal)
                        {
                            if (owner.GetCustomStatUnsafe(TextFile.IceCrossbowBuff) > 0)
                            {
                                float AddValue = CharmPatch.FrostRelicAddMult(owner) / __instance.damageMultiplier;
                                AddValue *= 1f + owner.GetCustomStatUnsafe(TextFile.BasicAttackDamageBonus) * 0.01f;
                                AddValue *= 1f + owner.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                                damage += AddValue;
                                damage *= 0.8f;
                                IceBowAttack[owner.Name] = true;
                                if (owner.GetCustomStatUnsafe(TextFile.FrostRelicFlame) > 0)
                                {
                                    if (Tools.GetPro("CrossbowArrow_Fire", out var Pro) && Pro != null)
                                    {
                                        float NewDamage = owner.GetCustomStatUnsafe(TextFile.FireDamage);
                                        float Mult = damage / owner.GetCustomStatUnsafe(TextFile.IceDamage);
                                        NewDamage *= Mult * finalDamageMultiplier * __instance.damageMultiplier;
                                        NewDamage *= 1f + DirectWeaponDamage * 0.01f;
                                        ModShootBullet(Pro, 1, 0f, damageFromType, NewDamage, damageId, owner, motionFrom, motionTo, height, sharedTarget, onAttack, EDamageElementalType.Fire, onCreateAttack, swingMultiplier, __instance.swingID);
                                        return false;
                                    }
                                }
                            }
                        }//速冻结晶
                        if (Type == WeaponIDs.EquippedwithGuidanceModule && Data.AttackType != "Basic" && Data.AttackType != "Dash")
                        {
                            damage = damage.WeaponDamageMult(__instance.damageMultiplier, 2.4f);
                        }//配装制导装置
                        if (Type == WeaponIDs.MiniCannon && Data.AttackType == "Basic")
                        {
                            damage = damage.WeaponDamageMult(__instance.damageMultiplier, 2f * 0.8f);
                            damage *= 1f + owner.GetCustomStatUnsafe(TextFile.BurnDamage) * 0.01f;
                            if (owner.GetCustomStatUnsafe(TextFile.BurnEvo) > 0)
                            {
                                damage *= 0.28f / 0.18f;
                            }
                            damage *= 1f + owner.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.01f;
                            damage *= 1f + owner.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 0.01f;
                        }//迷你加农炮
                        if (Type == WeaponIDs.ExpansionDevice)
                        {
                            if (owner.GetCustomStatUnsafe(TextFile.IceBuffDecreaseAttackSpeed) > 0)
                            {
                                float Mult = 1f + owner.GetCustomStatUnsafe(TextFile.FreezeDamage) * 0.01f;
                                Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.01f;
                                Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 0.01f;
                                damage = damage.WeaponDamageMult(__instance.damageMultiplier, 1.1f);
                                damage *= Mult;
                                if (Tools.GetPro("CrossbowArrowIce", out var Pro))
                                {
                                    Vector3 OrigPos = (Vector3)motionFrom + new Vector3(0f, 0f, height);
                                    for (int i = -3; i <= 3; i += 2)
                                    {
                                        if (i == 0) continue;

                                        float Damage = owner.GetCustomStatUnsafe(TextFile.IceDamage) * 0.55f;
                                        Damage *= 1f + owner.GetCustomStatUnsafe(TextFile.BasicAttackDamageBonus) * 0.01f;
                                        Damage *= 1f + owner.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                                        Damage *= 1f + owner.GetCustomStatUnsafe(TextFile.DirectWeaponDamage) * 0.01f;
                                        Damage *= Mult;
                                        Vector2 Vec = (motionTo - motionFrom).normalized;
                                        Vector2 Dir = RotateVector(Vec, 10f * i);
                                        Bullet a = Bullet.Pool.Spawn(Pro, OrigPos, true, EDamageFromType.DirectAttack, "[Star's Sephiria Mod]冰晶", Damage, 0, 0f, owner, owner.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0.3f, OrigPos, OrigPos + (Vector3)Dir * 5f, null, null, 0f, EDamageElementalType.Ice);
                                    }
                                }
                            }
                        }//扩展装置
                        if (Type == WeaponIDs.SolisIubar)
                        {
                            if (Data.AttackType == "Basic")
                            {
                                damage *= 1f + owner.GetCustomStatUnsafe(TextFile.FlameSwordDamage) * 0.01f;
                                damage *= 1f + owner.GetCustomStatUnsafe(TextFile.FlameSwordLuck) * 0.33f * 0.01f;
                                damage *= 1f + owner.GetCustomStatUnsafe(TextFile.FlameSwordAdditionalAttack) * 0.33f;
                                Action<int, ProjectileBase> originalCallback = onCreateAttack;
                                onCreateAttack = (index, projectile) =>
                                {
                                    if (projectile is Bullet bullet)
                                    {
                                        bullet.additionalCriticalChancePercent += owner.GetCustomStatUnsafe(TextFile.FlameSwordCritical) * 0.01f;
                                        bullet.additionalCriticalDamageRate += owner.GetCustomStatUnsafe(TextFile.FlameSwordCriticalDamageRate);
                                        bullet.ignoreDefense += owner.GetCustomStatUnsafe(TextFile.FlameSwordIgnoreDefense);
                                    }
                                    originalCallback?.Invoke(index, projectile);
                                };
                            }
                        }//索利斯·尤贝尔
                        if (Type == WeaponIDs.BountyHunter)
                        {
                            if (Data.AttackType == "Basic")
                            {
                                float Add = owner.GetCustomStatUnsafe(TextFile.DashAttackDamageBonus) * 0.01f;
                                float Mult = owner.GetCustomStatUnsafe(TextFile.CooldownRecoverySpeed) * 0.01f;
                                damage *= 1f + Add * (0.75f - Mult * 0.5f);
                                
                                if(AmmoStack.TryGetValue(owner, out int Stack) && Stack > 0)
                                {
                                    if (Stack <= 2 + 2 * owner.GetCustomStatUnsafe(TextFile.CrossbowAdditionalMagazine))
                                    {
                                        float NewDamage = damage * __instance.damageMultiplier * finalDamageMultiplier;
                                        NewDamage *= 1f + DirectWeaponDamage * 0.01f;
                                        NewDamage *= 0.7f;
                                        ModShootBullet(__instance, 3, 12f, damageFromType, NewDamage, damageId, owner, motionFrom, motionTo, height, sharedTarget, onAttack, elementalType, onCreateAttack, swingMultiplier);
                                        return false;
                                    }
                                }
                            }
                        }//赏金猎人
                        if (Type == WeaponIDs.Minigun)
                        {
                            if (__instance.damageMultiplier == 1.6f)
                            {
                                damage *= 0.625f;
                                float Add = owner.GetCustomStatUnsafe(TextFile.PhysicalDamage) * 0.5f;
                                Add *= 1f + owner.GetCustomStatUnsafe(TextFile.SpecialAttackDamageBonus) * 0.01f;
                                Add *= 1f + owner.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                                Add *= 1f + owner.GetCustomStatUnsafe(TextFile.MPSkillDamage) * 0.01f;
                                damage += Add / __instance.damageMultiplier;
                                if (owner.GetCustomStatUnsafe(TextFile.LightningCrossbow) - 100 > UnityEngine.Random.Range(0, 100f))
                                {
                                    damage *= 2f;
                                    float NewDamage = damage;
                                    NewDamage *= 1f + DirectWeaponDamage * 0.01f;
                                    NewDamage *= finalDamageMultiplier * __instance.damageMultiplier;
                                    if (Tools.GetPro("LibraryGuardLaser", out GameObject? Pro) && Pro != null)
                                    {
                                        Vector2 AddVec = (motionTo - motionFrom).normalized * 0.2f;
                                        ModShootBullet(Pro, 1, 0f, damageFromType, NewDamage, damageId, owner, motionFrom + AddVec, motionTo, height, sharedTarget, onAttack, elementalType, onCreateAttack, swingMultiplier, __instance.swingID);
                                        return false;
                                    }
                                }
                            }
                        }//迷你机枪
                        if (Type == WeaponIDs.Successor)
                        {
                            if (SuccessorEffect.TryGetValue(owner, out var BoltData))
                            {
                                elementalType = BoltData.Elemental;
                                float NewDamage = BoltData.Base;
                                NewDamage += elementalType switch
                                {
                                    EDamageElementalType.Fire => owner.GetCustomStatUnsafe(TextFile.FireDamage) * BoltData.Mult * 0.01f,
                                    EDamageElementalType.Ice => owner.GetCustomStatUnsafe(TextFile.IceDamage) * BoltData.Mult * 0.01f,
                                    EDamageElementalType.Lightning => owner.GetCustomStatUnsafe(TextFile.LightningDamage) * BoltData.Mult * 0.01f,
                                    _ => owner.GetCustomStatUnsafe(TextFile.PhysicalDamage) * BoltData.Mult * 0.01f,
                                };
                                NewDamage *= __instance.damageMultiplier;
                                NewDamage *= BoltData.CharmDamageMult;
                                NewDamage *= finalDamageMultiplier;

                                int DartleStack = 1;
                                int ScatteringStack = BoltData.Count;
                                if (BoltData.isMultiShot)
                                {
                                    DartleStack = BoltData.MultiShotCount;
                                    NewDamage *= BoltData.MultiShotMult;
                                }
                                float Interval = 0.3f / ScatteringStack;
                                NewDamage *= 1f + owner.GetCustomStatUnsafe(TextFile.BasicAttackDamageBonus) * 0.01f;
                                NewDamage *= 1f + owner.GetCustomStatUnsafe(TextFile.WeaponDamageBonus) * 0.01f;
                                NewDamage *= 1f + owner.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                                NewDamage *= 1f + owner.GetCustomStatUnsafe(TextFile.DirectWeaponDamage) * 0.01f;
                                NewDamage *= 1f + owner.GetCustomStatUnsafe(TextFile.MagicDamageBonus) * 0.01f;

                                Action<int, ProjectileBase> originalCallback = onCreateAttack;
                                onCreateAttack = (index, projectile) =>
                                {
                                    if (projectile is Bullet bullet)
                                    {
                                        bullet.additionalCriticalChancePercent += owner.GetCustomStatUnsafe(TextFile.MagicCritical) * 0.01f;
                                        bullet.additionalCriticalDamageRate += owner.GetCustomStatUnsafe(TextFile.MagicCriticalDamageBonus);
                                        if (owner.GetCustomStatUnsafe(TextFile.BoltMagicHoming) > 0)
                                            bullet.TurnOnHoming(true, null, 0f);
                                    }
                                    originalCallback?.Invoke(index, projectile);
                                };
                                owner.StartCoroutine(Dartle(__instance, DartleStack, ScatteringStack, Interval, 12f, damageFromType, NewDamage, damageId, owner, motionFrom, motionTo, height, sharedTarget, onAttack, elementalType, onCreateAttack, swingMultiplier));
                                return false;
                            }
                        }//继承者
                        if (Type == WeaponIDs.XRA9 && Data.AttackType != "Dash")
                        {
                            if (owner.TryGetComponent<SkillController>(out SkillController skillController))
                            {
                                List<Charm_Magic> MagicList = new List<Charm_Magic>();
                                foreach (Charm_Basic Charm in skillController.GridInventory.charms.Values)
                                {
                                    if (Charm is Charm_Magic Magic)
                                    {
                                        if (Magic == null) continue;
                                        if (Magic.ContainedMagic.magicPrefab.TryGetComponent<ActiveSkill>(out var Target) && Target != null && Magic.currentAmmo > 0)
                                        {
                                            MagicList.Add(Magic);
                                        }
                                    }
                                }
                                if (MagicList.Count > 0 && XRA9Effect.TryGetValue(owner, out int Stack) && Stack > 0)
                                {
                                    float Mult = damage / owner.GetCustomStatUnsafe(TextFile.PhysicalDamage);
                                    for (int i = 0; i < Stack; i++)
                                    {
                                        int idx = UnityEngine.Random.Range(0, MagicList.Count);
                                        MagicList.SafeRandomAccess(idx).FireCasting(motionFrom, motionTo, height, 0, true, false, skillController);
                                    }
                                    XRA9Effect[owner] = 0;
                                }
                            }
                        }//XRA-9
                    }
                }
                if (DirectWeaponDamage > 0)
                {
                    damage *= 1f + DirectWeaponDamage * 0.01f;
                    swingMultiplier *= 1f + DirectWeaponDamage * 0.01f;
                }
                return true;
            }

            private static IEnumerator Dartle(NewWeaponFireData_Bullet BulletData, int DartleStack, int ScatteringStack, float Interval, float Angle, EDamageFromType EType, float damage, string damageId, UnitAvatar owner, Vector2 ShootPos, Vector2 ToPos, float height, List<CombatBehaviour> Target, Action<CombatBehaviour, DamageInstance, ProjectileBase> OnAttack, EDamageElementalType ElementalType, Action<int, ProjectileBase> onCreateAttack, float SwingMult, float Speed = 1f)
            {
                for (int i = 0; i < ScatteringStack; i++)
                {
                    ModShootBullet(BulletData, DartleStack, Angle, EType, damage, damageId, owner, ShootPos, ToPos, height, Target, OnAttack, ElementalType, onCreateAttack, SwingMult, Speed);
                    if (Interval > 0) yield return new WaitForSeconds(Interval);
                }
                yield break;
            }

            private static void ModShootBullet(NewWeaponFireData_Bullet BulletData, int DartleStack, float Angle, EDamageFromType EType, float damage, string damageId, UnitAvatar owner, Vector2 ShootPos, Vector2 ToPos, float height, List<CombatBehaviour> Target, Action<CombatBehaviour, DamageInstance, ProjectileBase> OnAttack, EDamageElementalType ElementalType, Action<int, ProjectileBase> onCreateAttack, float SwingMult, float Speed = 1f)
            {
                float BaseAngle = HorayUtility.GetAngle(ToPos, ShootPos) - Angle * (DartleStack - 1) / 2f;
                int Level = BulletData.staggeringLevel;
                float Power = BulletData.externalForcePower;
                long Layers = owner.GetHostileFactionLayers(EType);
                for (int j = 0; j < DartleStack; j++)
                {
                    Vector2 Vec = HorayUtility.GetVector2FromAngle(BaseAngle + j * Angle);
                    Vector2 Pos = (Vector3)ShootPos + new Vector3(0f, 0f, height);
                    Bullet bullet = Bullet.Pool.Spawn(BulletData.bulletPrefab, Pos, true, EType, damageId, damage, Level, Power, owner, Layers, height, ShootPos, ShootPos + Vec * 5f, Target, OnAttack, 1f, ElementalType);
                    bullet.swingMultiplier = SwingMult;
                    bullet.swingId = BulletData.swingID;
                    bullet.forceFinalCombo = BulletData.forceFinalCombo;
                    bullet.speedScale = Speed;
                    if (ElementalType == EDamageElementalType.Chaos)
                    {
                        bullet.useChaosDamageColor = true;
                        bullet.chaosDamageColor = EffectConstant.ChaosColor;
                    }
                    onCreateAttack?.Invoke(0, bullet);
                }
            }

            private static void ModShootBullet(GameObject BulletPro, int DartleStack, float Angle, EDamageFromType EType, float damage, string damageId, UnitAvatar owner, Vector2 ShootPos, Vector2 ToPos, float height, List<CombatBehaviour> Target, Action<CombatBehaviour, DamageInstance, ProjectileBase> OnAttack, EDamageElementalType ElementalType, Action<int, ProjectileBase> onCreateAttack, float SwingMult, string SwingId, float Speed = 1f)
            {
                float BaseAngle = HorayUtility.GetAngle(ToPos, ShootPos) - Angle * (DartleStack - 1) / 2f;
                long Layers = owner.GetHostileFactionLayers(EType);
                for (int j = 0; j < DartleStack; j++)
                {
                    Vector2 Vec = HorayUtility.GetVector2FromAngle(BaseAngle + j * Angle);
                    Vector2 Pos = (Vector3)ShootPos + new Vector3(0f, 0f, height);
                    Bullet bullet = Bullet.Pool.Spawn(BulletPro, Pos, true, EType, damageId, damage, 0, 0, owner, Layers, height, ShootPos, ShootPos + Vec * 5f, Target, OnAttack, 1f, ElementalType);
                    bullet.speedScale = Speed;
                    bullet.swingId = SwingId;
                    bullet.swingMultiplier = SwingMult;
                    if (ElementalType == EDamageElementalType.Chaos)
                    {
                        bullet.useChaosDamageColor = true;
                        bullet.chaosDamageColor = EffectConstant.ChaosColor;
                    }
                    onCreateAttack?.Invoke(0, bullet);
                }
            }
        }

        public static Vector2 RotateVector(Vector2 vector, float angle)
        {
            float rad = angle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos
            );
        }

        [HarmonyPatch(typeof(NewWeaponFireData_BulletBurst))]
        public static class NNewWeaponFireData_BulletBurst_Patches
        {
            [HarmonyPatch("AAA")]
            [HarmonyPrefix]
            static bool AAA_Prefix(NewWeaponFireData_BulletBurst __instance, ref int burstRound, ref int burstIntervalMiliseconds, ref float damage, string damageId, bool canBeTransparentOnMultiplayer, UnitAvatar owner, float height, List<CombatBehaviour> sharedTarget, Action<CombatBehaviour, DamageInstance, ProjectileBase> onAttack, ref EDamageElementalType elementalType, ref Action<int, ProjectileBase> onCreateAttack, float swingMultiplier, string swingID, float finalDamageMultiplier)
            {
                if (owner == null) return true;

                string TName = __instance.name.Replace("(Clone)", "");
                LogInfoWeapon($"[NewWeaponFireData_BulletBurst]被调用 ID:{TName} 攻击倍率{__instance.damageMultiplier} 攻击伤害{damage} & {__instance.arcShotIntervalAngle}");
                if (ModConfig.Instance.EnableWeaponModifier)
                {
                    if (owner.GetWeaponTypeAndData(out int Type, out (string AttackType, int Idx) Data))
                    {
                        if (Type == WeaponIDs.Adonis)
                        {
                            __instance.arcShot = true;
                            __instance.arcShotIntervalAngle = 6f;
                            int CritChanceBonus = owner.GetCustomStatUnsafe("CRITICAL");
                            int CritDamageBonus = 50 + owner.GetCustomStatUnsafe("CRITICALDAMAGEBONUS");
                            if (new Random().NextDouble() < CritChanceBonus / 10000f)
                            {
                                int Stack = 1;
                                while (CritDamageBonus >= 70)
                                {
                                    Stack += 1;
                                    CritDamageBonus -= 70;
                                }
                                burstRound = Stack;
                                float DamageMult = (Stack - 0.65f * (Stack - 1)) / Stack;
                                damage *= DamageMult;
                                burstIntervalMiliseconds = 10;
                            }
                            if (Data.AttackType == "Basic")
                            {
                                damage = damage.WeaponDamageMult(__instance.damageMultiplier, __instance.damageMultiplier + 0.2f);
                            }
                        }//福寿草
                        if (Type == WeaponIDs.HeatedBlade)
                        {
                            damage *= 0.75f;
                        }//炙热之刃
                        if (Type == WeaponIDs.MagicWand || Type == WeaponIDs.PrismaticMagic)
                        {
                            damage = damage.WeaponDamageMult(__instance.damageMultiplier, 1.4f);
                            burstRound = 1;
                            if (Type == WeaponIDs.PrismaticMagic)
                            {
                                burstRound = 4;
                                damage *= 0.2f + Tools.BaseStatToAverage(owner) * 0.01f * 0.8f;
                                burstIntervalMiliseconds /= 2;
                            }
                            Action<int, ProjectileBase> originalCallback = onCreateAttack;
                            onCreateAttack = (index, projectile) =>
                            {
                                if (projectile is Bullet bullet)
                                {
                                    bullet.TurnOnHoming(true, null);
                                }
                                originalCallback?.Invoke(index, projectile);
                            };
                        }//法杖系列
                        if (Type == WeaponIDs.TroublingDelight || Type == WeaponIDs.LegacyofTheIllusionist || Type == WeaponIDs.Lazarion || Type == WeaponIDs.PristineForm)
                        {
                            float Mult = 1.5f;
                            if (Type == WeaponIDs.PristineForm)
                            {
                                if (PristineFormAttackCount.ContainsKey(owner.Name))
                                {
                                    PristineFormAttackCount[owner.Name] += 1;
                                    if (PristineFormAttackCount[owner.Name] >= 6)
                                    {
                                        UsePlanetShoot[owner.Name] = true;
                                        PristineFormAttackCount[owner.Name] = 0;
                                    }
                                }
                                else PristineFormAttackCount.Add(owner.Name, 1);
                            }
                            if (Data.AttackType == "Basic")
                            {
                                damage = Tools.BaseStatToMax(owner);
                                damage = damage.WeaponDamageMult(__instance.damageMultiplier, Mult);
                                damage *= 1f + owner.GetCustomStatUnsafe(TextFile.BasicAttackDamageBonus) * 0.01f;
                                damage *= 1f + owner.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                            }
                            elementalType = EDamageElementalType.Chaos;
                            Action<int, ProjectileBase> originalCallback = onCreateAttack;
                            onCreateAttack = (index, projectile) =>
                            {
                                if (projectile is Bullet bullet)
                                {
                                    bullet.useCustomColorType = Bullet.ECustomColorType.Chaos;
                                    bullet.chaosDamageColor = EffectConstant.ChaosColor;
                                    bullet.customColor = EffectConstant.ChaosColor;
                                }
                                originalCallback?.Invoke(index, projectile);
                            };
                        }//激光大剑系列
                        if (Type == WeaponIDs.Superconductor)
                        {
                            if (owner.GetCustomStatUnsafe(TextFile.DarkCloudIce) > 0)
                            {
                                elementalType = EDamageElementalType.IceAndLightning;
                                string Height = TextFile.LightningDamage;
                                string Low = TextFile.IceDamage;
                                if (owner.GetCustomStatUnsafe(TextFile.IceDamage) > owner.GetCustomStatUnsafe(TextFile.LightningDamage))
                                {
                                    Height = TextFile.IceDamage;
                                    Low = TextFile.LightningDamage;
                                }
                                float Add = owner.GetCustomStatUnsafe(Height) * 1.1f + owner.GetCustomStatUnsafe(Low) * 0.15f;
                                float Mult = damage / owner.GetCustomStatUnsafe(TextFile.LightningDamage);
                                damage = Add * Mult / 1.1f;
                            }
                            if (Data.AttackType == "Special")
                            {
                                __instance.arcShot = false;
                                if (owner.GetCustomStatUnsafe("DARKCLOUDMULTISHOT") > 0)
                                    burstRound = owner.GetCustomStatUnsafe("DARKCLOUDMULTISHOT");
                                burstIntervalMiliseconds = 750 / burstRound;
                            }
                            else burstRound = 1;
                        }//超导体
                        if (Type == WeaponIDs.BreakplateBlade)
                        {
                            damage = damage.WeaponDamageMult(__instance.damageMultiplier, 3f);
                        }//破甲刀
                        if (Type == WeaponIDs.MischievousPrank && Data.AttackType == "Special")
                        {
                            damage = damage.WeaponDamageMult(__instance.damageMultiplier, 5f);
                            damage /= 1f + owner.GetCustomStatUnsafe(TextFile.BasicAttackDamageBonus) * 0.01f;
                            float Mult = owner.currentMoney / 240f;
                            damage *= 1f + Mult * 0.01f;
                            damage *= 1.2f;
                        }//顽皮恶作剧
                        if (Type == WeaponIDs.FormlessDance)
                        {
                            int AttackCount = 1;
                            float EvasionValue = owner.GetCustomStatUnsafe(TextFile.Evasion) * 0.01f;
                            while (UnityEngine.Random.Range(0f, 100f) < EvasionValue)
                            {
                                EvasionValue *= 0.5f;
                                AttackCount += 1;
                                if (AttackCount >= 10) break;
                            }
                            burstRound = AttackCount;
                            if (AttackCount > 2) burstIntervalMiliseconds /= (AttackCount - 1);
                        }//无形之舞
                    }
                }
                int Value = owner.GetCustomStatUnsafe(TextFile.DirectWeaponDamage);
                if (Value > 0)
                {
                    damage *= 1f + Value * 0.01f;
                    swingMultiplier *= 1f + Value * 0.01f;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(NewWeaponFireData_MeleeAttack))]
        public static class NewWeaponFireData_MeleeAttack_Patches
        {
            [HarmonyPatch("InstantiateProjectile")]
            [HarmonyPrefix]
            static bool InstantiateProjectile_Prefix(NewWeaponFireData_MeleeAttack __instance, ref float damage, ref float swingMultiplier, ref UnitAvatar owner, Vector2 motionFrom, Vector2 motionTo, Action<CombatBehaviour, DamageInstance, ProjectileBase> onAttack, ref float rangeBonus, ref EDamageElementalType elementalType, ref Action<int, ProjectileBase> onCreateAttack, float finalDamageMultiplier)
            {
                if (owner == null) return true;

                string TName = __instance.name.Replace("(Clone)", "");
                LogInfoWeapon($"[NewWeaponFireData_MeleeAttack]被调用 ID:{TName} 攻击倍率：{swingMultiplier} 攻击伤害：{damage}");
                int DirectWeaponDamage = owner.GetCustomStatUnsafe(TextFile.DirectWeaponDamage);
                int MeleeDamage = owner.GetCustomStatUnsafe(TextFile.MeleeDamage);
                if (ModConfig.Instance.EnableWeaponModifier)
                {
                    if (owner.GetWeaponTypeAndData(out int Type, out (string AttackType, int Idx) Data))
                    {
                        if (Type == WeaponIDs.CarrotSword2 && Data.AttackType == "Basic")
                        {
                            damage = damage.WeaponDamageMult(__instance.damageMultiplier, 1.5f);
                            elementalType = EDamageElementalType.Chaos;
                            if (owner.mp > 0)
                            {
                                int UseEffect = (int)MathF.Max(1, owner.mp * 0.02f);
                                owner.UseMp(UseEffect);
                                damage *= 1f + owner.GetCustomStatUnsafe(TextFile.MPSkillDamage) * 0.01f;
                                if (Tools.GetPro("ThrowingSpear_Carrot", out var Pro))
                                {
                                    Vector3 OrigPos = (Vector3)motionFrom;
                                    Vector2 Vec = (motionTo - motionFrom).normalized;
                                    float ShootMult = 0.2f + 0.5f * (owner.MaxHp - 50) * 0.01f;
                                    float EffectDamage = damage * __instance.damageMultiplier * finalDamageMultiplier * ShootMult;
                                    EffectDamage *= 1f + DirectWeaponDamage * 0.01f;
                                    EffectDamage *= 1f + UseEffect * 0.24f;
                                    Bullet a = Bullet.Pool.Spawn(Pro, OrigPos, true, EDamageFromType.DirectAttack, "[Star's Sephiria Mod]胡萝卜飞弹", EffectDamage, 0, 0f, owner, owner.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0.3f, OrigPos, OrigPos + (Vector3)Vec * 5f, null, onAttack, 0f, elementalType);
                                    a.forceFinalCombo = __instance.forceFinalCombo;
                                    a.useChaosDamageColor = true;
                                    a.chaosDamageColor = __instance.chaosDamageColor;
                                    onCreateAttack?.Invoke(0, a);
                                }
                            }
                        }//胡萝卜剑
                        if (Type == WeaponIDs.TheIndomitable && Data.AttackType == "Basic")
                        {
                            damage *= 1.25f;
                        }//无垠原野
                        if ((Type == WeaponIDs.Rapier || Type == WeaponIDs.Rapier2) && Data.AttackType == "Dash")
                        {
                            damage *= 1f + owner.GetCustomStat(ECustomStat.AttackSpeed) * 0.01f / 2f;
                            if (Type == WeaponIDs.Rapier2)
                                damage *= 1f + owner.GetCustomStatUnsafe(TextFile.BasicAttackDamageBonus) * 0.01f;
                        }// 刺剑
                        if (Type == WeaponIDs.CreepingDespair && Data.AttackType == "Special")
                        {
                            damage *= 2.2f / 1.35f;
                        }//爬行的绝望
                        if (Type == WeaponIDs.DifficultTrial && Data.AttackType == "Special")
                        {
                            float Mult = owner.GetCustomStatUnsafe("ATTACKSPEED") * 0.5f;
                            damage *= 1f + Mult * 0.01f;
                        }//艰难实验
                        if (Type == WeaponIDs.ExplosiveDevice && Data.AttackType == "Basic")
                        {
                            damage = damage.WeaponDamageMult(__instance.damageMultiplier, 1.8f);
                        }//爆炸装置
                        if (Type == WeaponIDs.GunpowderSynthesis && Data.AttackType == "Basic")
                        {
                            damage = damage.WeaponDamageMult(__instance.damageMultiplier, 2.4f);
                        }//火药合成
                        if (Type == WeaponIDs.Heidi)
                        {
                            string Key = elementalType == EDamageElementalType.Physical ? TextFile.PhysicalDamage : TextFile.FireDamage;
                            float Mult = damage / owner.GetCustomStatUnsafe(Key);
                            elementalType = EDamageElementalType.Fire;
                            damage = owner.GetCustomStatUnsafe(TextFile.PhysicalDamage);
                            damage *= Mult;
                            float UseDamageMult = 1f;
                            bool UseDash = false;
                            bool HeidiDash = HeidiDashAttack.ContainsKey(owner.Name);
                            if (HeidiDash && (__instance.damageMultiplier == 1.85f || Data.AttackType == "Dash" || TName.Contains("DashAttackEnhanced")))
                            {
                                if (HeidiDashAttack[owner.Name] >= 6)
                                {
                                    rangeBonus += 0.6f;
                                    UseDamageMult = 4f;
                                    damage *= 1f + owner.GetCustomStatUnsafe(TextFile.FireDamage) * 0.03f;
                                    HeidiDashAttack[owner.Name] = 0;
                                    UseDash = true;
                                }
                            }
                            else if (__instance.damageMultiplier == 1.35f)
                            {
                                damage *= 1.3f;
                                Mult *= 1.3f;
                            }
                            damage = damage.WeaponDamageMult(__instance.damageMultiplier, UseDamageMult);
                            swingMultiplier = UseDamageMult * Mult;
                            if (!UseDash)
                            {
                                if (HeidiDash)
                                {
                                    HeidiDashAttack[owner.Name] += 1;
                                    if (HeidiDashAttack[owner.Name] > 12) HeidiDashAttack[owner.Name] = 12;
                                }
                                else HeidiDashAttack[owner.Name] = 1;
                            }
                        }//海蒂
                        if (Type == WeaponIDs.Negligence && Data.AttackType == "Special")
                        {
                            float Mult = 1f + Tools.NegligenceAttackMult(owner.GetCustomStatUnsafe(TextFile.AttackSpeed), 1.5f);
                            damage *= Mult;
                        }//默杀
                        if (Type == WeaponIDs.BeastMarksSwordHou && Data.AttackType == "Special")
                        {
                            if (__instance.damageMultiplier == 1.175f)
                            {
                                damage = damage.WeaponDamageMult(__instance.damageMultiplier, 0.8f);
                                damage *= 1f + owner.GetCustomStatUnsafe(TextFile.Evasion) / 200f * 0.01f;

                                int Add = 1;
                                if (owner.GetCustomStatUnsafe(TextFile.Evasion) >= 3300)
                                {
                                    Add += 1;
                                }
                                Action<int, ProjectileBase>? originalCallback = onCreateAttack;
                                onCreateAttack = (index, projectile) =>
                                {
                                    if (projectile is MeleeCollision Melee)
                                    {
                                        Melee.multiHit += Add;
                                        Melee.multiHitIntervalTimer.time = 0.05f;
                                    }
                                    originalCallback?.Invoke(index, projectile);
                                };
                            }
                        }//怪刀
                        if (Type == WeaponIDs.LightningGreatSwordE3G && Data.AttackType == "Special")
                        {
                            elementalType = EDamageElementalType.Lightning;
                            damage = owner.GetCustomStatUnsafe(TextFile.LightningDamage);
                            if (owner.GetCustomStatUnsafe(TextFile.DarkCloudIce) > 0)
                            {
                                elementalType = EDamageElementalType.IceAndLightning;
                                string Height = TextFile.LightningDamage;
                                string Low = TextFile.IceDamage;
                                if (owner.GetCustomStatUnsafe(TextFile.IceDamage) > owner.GetCustomStatUnsafe(TextFile.LightningDamage))
                                {
                                    Height = TextFile.IceDamage;
                                    Low = TextFile.LightningDamage;
                                }
                                damage = owner.GetCustomStatUnsafe(Height) * 1.1f + owner.GetCustomStatUnsafe(Low) * 0.15f;
                                damage /= 1.1f;
                            }
                            damage = damage.WeaponDamageMult(__instance.damageMultiplier, 1f);
                            float Mult = 4f;
                            Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.DarkCloudDamage) * 0.01f;
                            Mult *= 1f + Math.Min(1f, owner.GetCustomStatUnsafe(TextFile.DarkCloudLuck) * 0.01f);
                            swingMultiplier = Mult;

                            Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.SpecialAttackDamageBonus) * 0.01f;
                            Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                            Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.MPSkillDamage) * 0.01f;
                            damage *= Mult;
                        }//乌云大剑
                        if (Type == WeaponIDs.UnboundImmersion && Data.AttackType == "Special")
                        {
                            float DamageA = owner.GetCustomStatUnsafe(TextFile.Evasion) * 0.01f / 2f;
                            DamageA *= 1f + owner.GetCustomStatUnsafe(TextFile.SpecialAttackDamageBonus) * 0.01f;
                            DamageA *= 1f + owner.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                            damage += DamageA;
                            damage *= 1f + owner.GetCustomStatUnsafe(TextFile.Evasion) * 0.01f * 0.01f;
                        }//无尽沉浸
                        if (Type == WeaponIDs.AbsoluteEnvy && Data.AttackType == "Basic")
                        {
                            if (Data.Idx >= 2)
                                damage *= 1f + owner.GetCustomStatUnsafe(TextFile.DashAttackDamageBonus) * 0.01f;
                        }//绝对向往
                        if (Type == WeaponIDs.WinterSmithsDraftblade || Type == WeaponIDs.SilenceofEverfrost || Type == WeaponIDs.FragmentofNastrond)
                        {
                            if (DraftbladeAttack.ContainsKey(owner))
                            {
                                elementalType = EDamageElementalType.Ice;
                                damage = owner.GetCustomStatUnsafe(TextFile.IceDamage);
                                damage *= 1f + owner.GetCustomStatUnsafe(TextFile.BasicAttackDamageBonus) * 0.01f;
                                damage *= 1f + owner.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                            }
                        }//草图刀
                        if (Type == WeaponIDs.SilenceofEverfrost)
                        {
                            if (__instance.damageMultiplier == 0.5f)
                            {
                                int Stack = 0;
                                if (SilenceofEverfrostAttack.TryGetValue(owner, out int Value) && Value > 0)
                                {
                                    Stack = Math.Min(Value, 3);
                                    SilenceofEverfrostAttack[owner] -= Math.Min(Value, 3);
                                }
                                damage /= __instance.damageMultiplier;
                                float Mult = 1f + 1f * Stack;
                                Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.FreezeDamage) * 0.01f;
                                Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.01f;
                                Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 0.01f;
                                if (SilenceofEverfrostCharmDamage.TryGetValue(owner, out float CharmDamage))
                                    Mult *= 1f + CharmDamage;
                                damage *= Mult;
                                swingMultiplier = Mult;
                                if (Stack >= 3) SilenceofEverfrostCharm[owner] = true;
                            }
                            else if (Data.AttackType == "Basic")
                            {
                                if (!SilenceofEverfrostAttack.ContainsKey(owner))
                                    SilenceofEverfrostAttack.Add(owner, 1);
                                else if (SilenceofEverfrostAttack[owner] < 6)
                                    SilenceofEverfrostAttack[owner] += 1;
                            }
                        }//永霜的沉默
                        if (Type == WeaponIDs.Minimalism || Type == WeaponIDs.TheSecond || Type == WeaponIDs.BlackIronHalberd)
                        {
                            int Max = 3;
                            if (Type == WeaponIDs.BlackIronHalberd)
                                Max = 5;
                            if (Data.AttackType == "Basic")
                            {
                                if (!MinimalismDashAttack.ContainsKey(owner.Name))
                                    MinimalismDashAttack.Add(owner.Name, 1);
                                else if (MinimalismDashAttack[owner.Name] < Max)
                                    MinimalismDashAttack[owner.Name] += 1;
                            }
                            if (Data.AttackType == "Dash")
                            {
                                damage = damage.WeaponDamageMult(__instance.damageMultiplier, 2f);
                                if (MinimalismDashAttack.TryGetValue(owner.Name, out int Stack) && Stack > 0)
                                {
                                    damage *= 1f + Math.Min(Stack, Max);
                                    if (Type == WeaponIDs.TheSecond)
                                    {
                                        if (TheSecondDashAttack.TryGetValue(owner.Name, out bool falg) && falg)
                                        {
                                            MinimalismDashAttack[owner.Name] = 0;
                                            TheSecondDashAttack[owner.Name] = false;
                                        }
                                        else TheSecondDashAttack[owner.Name] = true;
                                    }
                                    else MinimalismDashAttack[owner.Name] = 0;

                                    Action<int, ProjectileBase>? originalCallback = onCreateAttack;
                                    onCreateAttack = (index, projectile) =>
                                    {
                                        if (projectile is MeleeCollision Melee)
                                        {
                                            Melee.additionalCriticalChancePercent += Stack * 10f;
                                        }
                                        originalCallback?.Invoke(index, projectile);
                                    };
                                }
                            }
                        }//极简主义
                        if (Type == WeaponIDs.LightningFlash)
                        {
                            if (Data.AttackType == "Basic")
                            {
                                if (LightningFlashBuff.ContainsKey(owner.Name))
                                    LightningFlashBuff[owner.Name] += 1;
                                else
                                    LightningFlashBuff[owner.Name] = 1;
                            }
                            if (__instance.damageElementalType == EDamageElementalType.Lightning)
                            {
                                float Mult = 1f + owner.GetCustomStatUnsafe(TextFile.ElectricDamage) * 0.01f;
                                Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.01f;
                                Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 0.01f;
                                damage *= Mult;
                            }
                        }//电光一闪
                        if (Type == WeaponIDs.StaffOfGreatMight && TName.Contains("AttackRolling"))
                        {
                            damage *= 1.3f;
                            float DamageA = owner.GetCustomStatUnsafe(TextFile.DamageReduction) * 0.4f / __instance.damageMultiplier;
                            DamageA *= 1f + owner.GetCustomStatUnsafe(TextFile.BasicAttackDamageBonus) * 0.01f;
                            damage += DamageA;
                        }//大力棍
                        if (Type == WeaponIDs.RuadhsleaSpear && Data.AttackType == "Basic")
                        {
                            if (Data.Idx >= 2)
                            {
                                damage *= 1f + owner.GetCustomStatUnsafe(TextFile.DashAttackDamageBonus) / 100f;
                            }
                        }//红枪
                        if (Type == WeaponIDs.FamedSwordMuramasa)
                        {
                            damage *= 1f + owner.GetCustomStatUnsafe(TextFile.BasicAttackDamageBonus) * 0.01f;
                        }//村正
                        if (Type == WeaponIDs.RendbladeRuinDance)
                        {
                            if (__instance.damageMultiplier == 0.75f)
                                damage = damage.WeaponDamageMult(__instance.damageMultiplier, 1f);
                        }//乱舞
                        if (Type == WeaponIDs.ScaleOfTheAstralSea && __instance.damageMultiplier == 3f)
                        {
                            float Mult = 1f;
                            int EffectCount = owner.GetCustomStatUnsafe(TextFile.ScaleOfTheAstralSeaEffect);
                            if (EffectCount > 0)
                            {
                                Mult *= 1f + Tools.GetExMP(owner) * 0.08f * EffectCount * 0.01f;
                                owner.AddCustomStatUnsafe(TextFile.ScaleOfTheAstralSeaEffect, -EffectCount);
                            }
                            damage = damage.WeaponDamageMult(__instance.damageMultiplier, 4.2f);
                            damage *= 1f + owner.GetCustomStatUnsafe(TextFile.MPSkillDamage) * 0.01f;
                            damage *= Mult;
                        }//圣海碎片
                        if (Type == WeaponIDs.WeightOfTheSea && Data.AttackType == "Basic")
                        {
                            if (Data.Idx == 5)
                            {
                                if (GuardBuff != null && owner.GetBuffInstanceByPrefab(GuardBuff) != null)
                                {
                                    damage *= 2f;
                                    if (WeightOfTheSeaBuff.TryGetValue(owner.Name, out int Stack) && Stack > 0)
                                    {
                                        WeightOfTheSeaBuff[owner.Name] -= 1;
                                    }
                                    else GuardBuff.RequestEnd();
                                }
                            }
                        }//海之锚
                        if (Type == WeaponIDs.WanderingAshes && Data.AttackType == "Dash")
                        {
                            damage = damage.WeaponDamageMult(__instance.damageMultiplier, 1f);
                            damage *= 1f + owner.GetCustomStatUnsafe(TextFile.BasicAttackDamageBonus) * 0.01f;
                            if (owner.NetworkisGuardEnabled)
                            {
                                damage *= 2;
                            }
                        }//灰烬飘扬
                        if (Type == WeaponIDs.SolisCineris && Data.AttackType == "Basic")
                        {
                            if (owner.Inventory)
                            {
                                ComboEffectBase comboEffectBase = owner.Inventory.FindComboEffect("FLAMESWORD");
                                if (comboEffectBase != null && comboEffectBase is ComboEffect_FlameSword comboEffect_FlameSword)
                                {
                                    UnitAvatar Target = PlayerInputController.SearchTargetNearestPoint(owner, owner.transform.position, 50f);
                                    if (Target != null)
                                    {
                                        Vector3 TargetPos = Target.transform.position;
                                        int Count = 3 + owner.GetCustomStatUnsafe(TextFile.FlameSwordAdditionalAttack);
                                        for (int i = 0; i < Count; i++)
                                        {
                                            Vector3 ShootPos = TargetPos + (Vector3)UnityEngine.Random.insideUnitCircle.normalized * 2.5f;
                                            comboEffect_FlameSword.FireSword(ShootPos, TargetPos);
                                        }
                                    }
                                }
                            }
                        }//索利斯灰烬
                        if (Type == WeaponIDs.GuardiansReturn && Data.AttackType == "Basic")
                        {
                            if (Tools.GetPro("ThrowShield", out var Pro))
                            {
                                float Damage = owner.GetCustomStatUnsafe(TextFile.PhysicalDamage) * 0.5f * 1.4f;
                                Damage *= 1f + owner.GetCustomStatUnsafe(TextFile.SpecialAttackDamageBonus) * 0.01f;
                                Damage *= 1f + owner.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                                Damage *= 1f + DirectWeaponDamage * 0.01f;
                                int BaseCount = 2;
                                float BasicValue = owner.GetCustomStatUnsafe(TextFile.BasicAttackDamageBonus);
                                while (BasicValue >= 20)
                                {
                                    BaseCount += 1;
                                    BasicValue -= 20;
                                }
                                Bullet a = Bullet.Pool.Spawn(Pro, owner.transform.position, true, EDamageFromType.DirectAttack, "[Star's Sephiria Mod]飞盾", Damage, 0, 0f, owner, owner.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0.3f, motionFrom, motionTo, null, null, 0f, EDamageElementalType.Physical);
                                a.pierceCreatureCount = BaseCount;
                                a.TurnOnHoming(true, null, 1f);
                            }
                        }//归来守护
                        if ((Type == WeaponIDs.CreakingSpine || Type == WeaponIDs.BoneGearblade))
                        {
                            if (Data.Idx == 4)
                            {
                                Action<int, ProjectileBase>? originalCallback = onCreateAttack;
                                onCreateAttack = (index, projectile) =>
                                {
                                    if (projectile is MeleeCollision Melee)
                                    {
                                        Melee.forceFinalCombo = true;
                                    }
                                    originalCallback?.Invoke(index, projectile);
                                };
                            }
                        }//骨剑
                        if (Type == WeaponIDs.BlazingHelbanus && Data.AttackType == "Special")
                        {
                            elementalType = EDamageElementalType.Fire;
                            damage = owner.GetCustomStatUnsafe(TextFile.FireDamage) / __instance.damageMultiplier;
                            float Mult = 2f;
                            Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.BurnDamage) * 0.01f;
                            if (owner.GetCustomStatUnsafe(TextFile.BurnEvo) > 0)
                            {
                                Mult *= 0.28f / 0.18f;
                            }
                            Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.01f;
                            Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 0.01f;
                            swingMultiplier = Mult;
                            Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.SpecialAttackDamageBonus) * 0.01f;
                            Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                            damage *= Mult;
                        }//赫尔巴努斯
                        if (Type == WeaponIDs.FlameEaterHaetae && elementalType == EDamageElementalType.Fire && __instance.damageMultiplier == 1.7f)
                        {
                            damage *= 1f + owner.GetCustomStatUnsafe(TextFile.BurnDamage) * 0.01f;
                            if (owner.GetCustomStatUnsafe(TextFile.BurnEvo) > 0)
                            {
                                damage *= 2.8f / 1.8f;
                            }
                            damage *= 1f + owner.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.01f;
                            damage *= 1f + owner.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 0.01f;
                        }//食火獬豸
                        if (Type == WeaponIDs.SafeStart || Type == WeaponIDs.LightlessBlade || Type == WeaponIDs.PaleSmile)
                        {
                            if (Type == WeaponIDs.LightlessBlade)
                            {
                                float Add = owner.GetCustomStatUnsafe(TextFile.DamageReduction) * owner.GetCustomStatUnsafe(TextFile.Thorns) * 0.01f;
                                Add *= 1f + owner.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                                damage += Add * 0.1f;
                            }
                            if (Data.AttackType == "Special")
                            {
                                if (__instance.damageMultiplier == 2.6f || __instance.damageMultiplier == 4.68f)
                                {
                                    damage = damage.WeaponDamageMult(__instance.damageMultiplier, 1.3f);
                                }
                                if (Type == WeaponIDs.PaleSmile && __instance.damageMultiplier == 4.8f)
                                {
                                    float DamageA = owner.GetCustomStatUnsafe(TextFile.DamageReduction) / __instance.damageMultiplier * 2.4f;
                                    DamageA *= 1f + owner.GetCustomStatUnsafe(TextFile.SpecialAttackDamageBonus) * 0.01f;
                                    DamageA *= 1f + owner.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                                    damage += DamageA;
                                }
                                damage *= 1f + owner.GetCustomStatUnsafe(TextFile.DamageReduction) * 0.01f / 2f;
                            }
                        }
                        if (Type == WeaponIDs.Compliance && Data.AttackType == "Basic")
                        {
                            if (__instance.forceFinalCombo)
                            {
                                damage /= 1f + owner.GetCustomStatUnsafe(TextFile.BasicAttackDamageBonus) * 0.01f;
                                damage *= 1f + owner.GetCustomStatUnsafe(TextFile.SpecialAttackDamageBonus) * 0.01f;
                            }
                        }//顺从的姿态
                        if ((Type == WeaponIDs.WoodenClub || Type == WeaponIDs.SpikeClub || Type == WeaponIDs.MischievousPrank) && Data.AttackType != "Basic")
                        {
                            damage *= 1.2f;
                        }//木制棍棒
                        if (Type == WeaponIDs.FrozenSmelt && Data.AttackType == "Basic")
                        {
                            if (FrozenSmeltEffect.TryGetValue(owner, out int Stack) && Stack > 0)
                            {
                                float Mult = 1f + owner.GetCustomStatUnsafe(TextFile.FreezeDamage) * 0.01f;
                                Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.01f;
                                Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 0.01f;
                                damage *= Mult;

                                Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.BasicAttackDamageBonus) * 0.01f;
                                Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                                Mult *= 1f + DirectWeaponDamage * 0.01f;
                                float Damage = owner.GetCustomStatUnsafe(TextFile.IceDamage) * Mult;
                                if (Data.Idx == 0 || Data.Idx == 2 || Data.Idx == 4)
                                {
                                    if (Tools.GetPro("AirSlashMini", out var Pro))
                                    {
                                        Vector3 OrigPos = (Vector3)motionFrom;
                                        Vector2 Vec = (motionTo - motionFrom).normalized;
                                        Bullet a = Bullet.Pool.Spawn(Pro, OrigPos, true, EDamageFromType.DirectAttack, "[Star's Sephiria Mod]冰刃", Damage * 1.8f, 0, 0f, owner, owner.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0.3f, OrigPos, OrigPos + (Vector3)Vec * 5f, null, onAttack, 0f, EDamageElementalType.Ice);
                                        a.forceFinalCombo = __instance.forceFinalCombo;
                                        a.speedScale = 1.25f;
                                        onCreateAttack?.Invoke(0, a);
                                    }
                                }
                                if (Data.Idx == 1 || Data.Idx == 3 || Data.Idx == 4)
                                {
                                    if (Tools.GetPro("CrossbowArrowIce", out var Pro))
                                    {
                                        Vector3 OrigPos = (Vector3)motionFrom;
                                        for (int i = -3; i <= 3; i += 2)
                                        {
                                            Vector2 Vec = (motionTo - motionFrom).normalized;
                                            Vector2 Dir = RotateVector(Vec, 10f * i);
                                            Bullet a = Bullet.Pool.Spawn(Pro, OrigPos, true, EDamageFromType.DirectAttack, "[Star's Sephiria Mod]冰晶", Damage * 0.6f, 0, 0f, owner, owner.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0.3f, OrigPos, OrigPos + (Vector3)Dir * 5f, null, onAttack, 0f, EDamageElementalType.Ice);
                                            a.forceFinalCombo = __instance.forceFinalCombo;
                                            a.speedScale = 0.75f;
                                            onCreateAttack?.Invoke(0, a);
                                        }
                                    }
                                }
                                FrozenSmeltEffect[owner] -= 1;
                            }
                        }//冻鱼
                        if (Type == WeaponIDs.GlacialBlade && Data.AttackType == "Basic")
                        {
                            if (Tools.GetPro("AirSlashMini", out var Pro))
                            {
                                float Mult = 1f + owner.GetCustomStatUnsafe(TextFile.FreezeDamage) * 0.01f;
                                Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.01f;
                                Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 0.01f;
                                Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.BasicAttackDamageBonus) * 0.01f;
                                Mult *= 1f + owner.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                                Mult *= 1f + DirectWeaponDamage * 0.01f;
                                float DValue = owner.GetCustomStatUnsafe(TextFile.IceDamage) * 1.6f * Mult;

                                Vector3 OrigPos = (Vector3)motionFrom;
                                int Stack = 0;
                                if (GlacialBladeEffect.TryGetValue(owner, out Timer TValue) && TValue.Ratio < 1f) Stack = 1;
                                for (int i = -Stack; i <= Stack; i += 1)
                                {
                                    float DMult = 1f;
                                    if (i != 0) DMult = 0.5f;

                                    Vector2 Vec = (motionTo - motionFrom).normalized;
                                    Vector2 Dir = RotateVector(Vec, 24f * i);
                                    Bullet a = Bullet.Pool.Spawn(Pro, OrigPos, true, EDamageFromType.DirectAttack, "[Star's Sephiria Mod]冰川剑气", DValue * DMult, 0, 0f, owner, owner.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0.3f, OrigPos, OrigPos + (Vector3)Dir * 5f, null, onAttack, 0f, EDamageElementalType.Ice);
                                    a.forceFinalCombo = __instance.forceFinalCombo;
                                    a.speedScale = 1.5f;
                                    onCreateAttack?.Invoke(0, a);
                                }
                            }
                        }//冰川之刃
                        if (Type == WeaponIDs.SolisEkli)
                        {
                            float AddMult = 1f + owner.GetCustomStatUnsafe(TextFile.FlameSwordAdditionalAttack) * 0.33f;
                            AddMult *= 1f + MathF.Min(100, owner.GetCustomStatUnsafe(TextFile.FlameSwordLuck)) * 0.33f * 0.01f;
                            damage *= AddMult;
                            swingMultiplier *= AddMult;
                        }//索利斯·叶克里
                        if (Type == WeaponIDs.BlindingSilence && Data.AttackType == "Special")
                        {
                            if (Tools.GetPro("Tornado", out var Pro))
                            {
                                float Damage = owner.GetCustomStatUnsafe(TextFile.LightningDamage) * 1.2f;
                                Damage *= 1f + owner.GetCustomStatUnsafe(TextFile.ElectricDamage) * 0.01f;
                                float Luck = owner.GetCustomStatUnsafe(TextFile.ElectricLuck) * 0.01f;
                                Damage *= 1f + owner.GetCustomStatUnsafe(TextFile.DebuffDuration) * 0.01f * Luck;
                                Damage *= 1f + owner.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.01f;
                                Damage *= 1f + owner.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 0.01f;
                                Damage *= 1f + owner.GetCustomStatUnsafe(TextFile.SpecialAttackDamageBonus) * 0.01f;
                                Damage *= 1f + owner.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                                Damage *= 1f + DirectWeaponDamage * 0.01f;
                                Vector2 Vec = (motionTo - motionFrom).normalized;
                                Bullet a = Bullet.Pool.Spawn(Pro, motionFrom + Vec, true, EDamageFromType.DirectAttack, "[Star's Sephiria Mod]电卷云", Damage, 0, 0f, owner, owner.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0.3f, motionFrom + Vec, motionTo + Vec, null, onAttack, 0f, EDamageElementalType.Lightning);
                                a.speedScale *= 0.5f;
                                a.TurnOnHoming(true, null, 0.1f);
                                a.pierceCreatureCount = 99;
                                a.collisionStayDamageIntervalTimer.time = 0.2f;
                                onCreateAttack?.Invoke(0, a);
                                return false;
                            }
                        }//耀目的寂静
                        if (Type == WeaponIDs.CeruleanCloudSwordArges && __instance.damageMultiplier >= 2f)
                        {
                            if (owner.GetCustomStatUnsafe(TextFile.DarkCloudIce) > 0)
                            {
                                elementalType = EDamageElementalType.IceAndLightning;
                                string Height = TextFile.LightningDamage;
                                string Low = TextFile.IceDamage;
                                if (owner.GetCustomStatUnsafe(TextFile.IceDamage) > owner.GetCustomStatUnsafe(TextFile.LightningDamage))
                                {
                                    Height = TextFile.IceDamage;
                                    Low = TextFile.LightningDamage;
                                }
                                float Mult = damage / owner.GetCustomStatUnsafe(TextFile.LightningDamage);
                                float NewDamage = owner.GetCustomStatUnsafe(Height) * 1.1f + owner.GetCustomStatUnsafe(Low) * 0.15f;
                                damage = NewDamage * Mult / 1.1f;
                            }
                            damage = damage.WeaponDamageMult(__instance.damageMultiplier, 1.6f);
                            damage *= 1f + Mathf.Min(100, owner.GetCustomStatUnsafe(TextFile.DarkCloudLuck)) * 0.01f;
                            damage *= Mathf.Max(1, owner.GetCustomStatUnsafe(TextFile.DarkCloudMultishot));
                        }//苍云刀
                        if (Type == WeaponIDs.SolisImber && __instance.damageMultiplier == 4.8f)
                        {
                            float Mult = damage / owner.GetCustomStatUnsafe(TextFile.PhysicalDamage);
                            elementalType = EDamageElementalType.Fire;
                            damage = owner.GetCustomStatUnsafe(TextFile.FireDamage);
                            damage = damage.WeaponDamageMult(__instance.damageMultiplier, 5f);
                            damage *= Mult;
                            float FlameSwordAdd = 1f + owner.GetCustomStatUnsafe(TextFile.FlameSwordDamage) * 0.01f;
                            FlameSwordAdd *= 1f + owner.GetCustomStatUnsafe(TextFile.FlameSwordLuck) * 0.33f * 0.01f;
                            FlameSwordAdd *= 1f + owner.GetCustomStatUnsafe(TextFile.FlameSwordAdditionalAttack) * 0.33f;
                            damage *= FlameSwordAdd;
                            swingMultiplier = 5f * FlameSwordAdd;
                            float Crit = owner.GetCustomStatUnsafe(TextFile.FlameSwordCritical) * 0.01f;
                            int CritDamage = owner.GetCustomStatUnsafe(TextFile.FlameSwordCritical);
                            int IgnoreDefense = owner.GetCustomStatUnsafe(TextFile.FlameSwordCritical);
                            if (onCreateAttack != null)
                            {
                                Action<int, ProjectileBase> originalCallback = onCreateAttack;
                                onCreateAttack = (index, projectile) =>
                                {
                                    if (projectile is MeleeCollision Melee)
                                    {
                                        Melee.additionalCriticalChancePercent += Crit;
                                        Melee.additionalCriticalDamageRate += CritDamage;
                                        Melee.ignoreDefense += IgnoreDefense;
                                    }
                                    originalCallback?.Invoke(index, projectile);
                                };
                            }
                        }//索利斯·因贝尔
                        if (Type == WeaponIDs.MarkStabilization && Data.AttackType == "Special")
                        {
                            Vector3 OrigPos = (Vector3)motionFrom;
                            Vector2 Vec = (motionTo - motionFrom).normalized;
                            damage *= 1f + owner.GetCustomStatUnsafe(TextFile.ElectricDamage) * 0.01f;
                            damage *= 1f + owner.GetCustomStatUnsafe(TextFile.ElectricStack) * 0.33f;
                            damage *= 1f + owner.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.01f;
                            damage *= 1f + owner.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 0.01f;
                            damage *= 1f + owner.GetCustomStatUnsafe(TextFile.ElectricLuck) * 0.01f;
                            damage *= 1f + DirectWeaponDamage * 0.01f;

                            bool NoAmmo = false;
                            if (MarkStabilizationEffect.TryGetValue(owner, out int Value) && Value >= 5)
                            {
                                MarkStabilizationEffect[owner] -= 5;
                                owner.SetEffectHUDValue("MarkStabilizationCharge", MarkStabilizationEffect[owner].ToString());
                                if (MarkStabilizationEffect[owner] < 5) NoAmmo = true;
                            }
                            else NoAmmo = true;
                            if (NoAmmo)
                            {
                                if (MarkStabilizationEffect[owner] < 0)
                                    MarkStabilizationEffect[owner] = 0;
                                owner.SetEffectHUDValue("MarkStabilizationCharge", MarkStabilizationEffect[owner].ToString());
                            }
                        }//标记稳定化
                    }
                }
                if (DirectWeaponDamage != 0)
                {
                    damage *= 1f + DirectWeaponDamage * 0.01f;
                    swingMultiplier *= 1f + DirectWeaponDamage * 0.01f;
                }
                if (MeleeDamage != 0)
                {
                    damage *= 1f + MeleeDamage * 0.01f;
                    swingMultiplier *= 1f + MeleeDamage * 0.01f;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(NewWeaponFireData_SpecialProjectile))]
        public static class NewWeaponFireData_SpecialProjectile_Patches
        {
            [HarmonyPatch("InstantiateProjectile")]
            [HarmonyPrefix]
            static bool InstantiateProjectile(NewWeaponFireData_SpecialProjectile __instance, ref float damage, ref float swingMultiplier, string damageId, ref UnitAvatar owner, ref Vector2 motionFrom, ref Vector2 motionTo, ref Action<int, ProjectileBase> onCreateAttack)
            {
                if (owner == null) return true;

                string TName = __instance.name.Replace("(Clone)", "");
                LogInfoWeapon($"[NewWeaponFireData_SpecialProjectile]被调用 ID:{TName} 攻击倍率{__instance.damageMultiplier} 攻击伤害{damage}");
                if(ModConfig.Instance.EnableWeaponModifier)
                {
                    if (owner.GetWeaponTypeAndData(out int Type, out (string AttackType, int Idx) Data))
                    {
                        if (Type == WeaponIDs.SolisBraca)
                        {
                            float Dis = (motionTo - (Vector2)owner.transform.position).magnitude / 9f;
                            float Mult = 6f - 3f * Mathf.Min(1f, Dis);
                            float AdditionalDamage = 1f + owner.GetCustomStatUnsafe(TextFile.FlameSwordAdditionalAttack) * 0.33f;
                            damage = damage.WeaponDamageMult(__instance.damageMultiplier, Mult);
                            damage *= AdditionalDamage;
                            swingMultiplier = Mult * AdditionalDamage;
                        }//阳棍
                    }
                }
                int Value = owner.GetCustomStatUnsafe(TextFile.DirectWeaponDamage);
                if (Value > 0)
                {
                    damage *= 1f + Value * 0.01f;
                    swingMultiplier *= 1f + Value * 0.01f;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(NewWeaponFireData_Summon))]
        public static class NewWeaponFireData_Summon_Patch
        {
            [HarmonyPatch("InstantiateProjectile")]
            [HarmonyPrefix]
            static bool InstantiateProjectile_Prefix(NewWeaponFireData_Summon __instance, ref float damage, UnitAvatar owner)
            {
                if (owner == null) return true;
                if (owner.IsWeapon(WeaponIDs.Lazarion))
                {
                    int AddCount = 0;
                    if (owner.GetCustomStatUnsafe(TextFile.PhysicalDamage) > 60)
                    {
                        AddCount += 1;
                    }
                    if (owner.GetCustomStatUnsafe(TextFile.FireDamage) > 60)
                    {
                        AddCount += 1;
                    }
                    if (owner.GetCustomStatUnsafe(TextFile.IceDamage) > 60)
                    {
                        AddCount += 1;
                    }
                    if (owner.GetCustomStatUnsafe(TextFile.LightningDamage) > 60)
                    {
                        AddCount += 1;
                    }
                    __instance.summonLimit = 3 + Mathf.Min(3, AddCount);
                }
                return true;
            }
        }
    }
}