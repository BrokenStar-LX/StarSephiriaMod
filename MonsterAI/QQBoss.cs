using HarmonyLib;
using SPMod.Tool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SPMod.MonsterAI
{
    [HarmonyPatch]
    public static class QQBoss
    {
        public static int Hard
        {
            get { return ModConfig.HardMode; }
        }
        private static readonly FieldInfo? currentPatternCoroutineField;
        private static readonly FieldInfo? qqBossField;

        private static readonly MethodInfo? UpdateTentacleSummonPatternMethod;
        private static readonly MethodInfo? UpdateMouseOrbPatternMethod;
        private static readonly MethodInfo? UpdateDiamondExplosionPatternMethod;
        private static readonly MethodInfo? TryGetRandomAlivePlayerMethod;
        private static readonly MethodInfo? BlinkToTargetCoroutineMethod;
        private static readonly MethodInfo? FoxAttackCoroutineMethod;
        private static readonly MethodInfo? BlinkToTargetAndCrystalCoroutineMethod;

        private static readonly MethodInfo? StartPerimeterBulletGimmickMethod;
        private static readonly MethodInfo? ClearPerimeterBulletsMethod;
        static QQBoss()
        {
            // QQBoss 反射
            Type type = typeof(UnitAI_QQBoss);
            currentPatternCoroutineField = AccessTools.Field(type, "currentPatternCoroutine");
            qqBossField = AccessTools.Field(type, "qqBoss");

            UpdateTentacleSummonPatternMethod = AccessTools.Method(type, "UpdateTentacleSummonPattern");
            UpdateMouseOrbPatternMethod = AccessTools.Method(type, "UpdateMouseOrbPattern");
            UpdateDiamondExplosionPatternMethod = AccessTools.Method(type, "UpdateDiamondExplosionPattern");
            TryGetRandomAlivePlayerMethod = AccessTools.Method(type, "TryGetRandomAlivePlayer");
            BlinkToTargetCoroutineMethod = AccessTools.Method(type, "BlinkToTargetCoroutine");
            FoxAttackCoroutineMethod = AccessTools.Method(type, "FoxAttackCoroutine");
            BlinkToTargetAndCrystalCoroutineMethod = AccessTools.Method(type, "BlinkToTargetAndCrystalCoroutine");

            type = typeof(Unit_QQBoss);
            StartPerimeterBulletGimmickMethod = AccessTools.Method(type, "StartPerimeterBulletGimmick");
            ClearPerimeterBulletsMethod = AccessTools.Method(type, "ClearPerimeterBullets");
        }

        private static bool TryGetRandomAlivePlayer(out PlayerAvatar? player)
        {
            List<PlayerAvatar> list = new List<PlayerAvatar>();
            foreach (PlayerSpawner playerSpawner in PlayerSpawner.MultiplayerList)
            {
                if (playerSpawner && playerSpawner.PlayerAvatar && !playerSpawner.PlayerAvatar.IsDead)
                {
                    list.Add(playerSpawner.PlayerAvatar);
                }
            }
            if (list.Count == 0)
            {
                player = null;
                return false;
            }
            player = list.GetRandom();
            return true;
        }

        public static GameObject? windmillBulletHellPrefab = null;
        public static int CrystalStack = 0;

        [HarmonyPatch(typeof(UnitAI_QQBoss))]
        public static class UnitAI_QQBoss_Patch
        {
            private static int Oldpattern = -1;
            private static List<int> Pattern = new List<int>();
            private static int AttackFlag = 0;

            [HarmonyPatch("Awake")]
            [HarmonyPostfix]
            static void Awake_Postfix(UnitAI_QQBoss __instance)
            {
                Pattern = new List<int>();
                AttackFlag = 0;
                CrystalStack = 0;
                __instance.crystalPatternTimer.Ratio = 0.5f;
            }

            [HarmonyPatch("OnAIUpdate_FoundEnemy")]
            [HarmonyPrefix]
            static bool OnAIUpdate_FoundEnemy_Prefix(UnitAI_QQBoss __instance)
            {
                if (Hard <= 0) return true;
                try
                {

                    if (currentPatternCoroutineField == null ||
                        qqBossField == null ||
                        UpdateTentacleSummonPatternMethod == null ||
                        UpdateMouseOrbPatternMethod == null ||
                        UpdateDiamondExplosionPatternMethod == null ||
                        TryGetRandomAlivePlayerMethod == null ||
                        BlinkToTargetCoroutineMethod == null ||
                        FoxAttackCoroutineMethod == null ||
                        BlinkToTargetAndCrystalCoroutineMethod == null)
                    {
                        StarSephiriaMod.LogError("[UnitAI_QQBoss][OnAIUpdate_FoundEnemy]获取反射失败");
                        return true;
                    }

                    UpdateTentacleSummonPatternMethod?.Invoke(__instance, null);
                    UpdateMouseOrbPatternMethod?.Invoke(__instance, null);
                    UpdateDiamondExplosionPatternMethod?.Invoke(__instance, null);
                    if (currentPatternCoroutineField.GetValue(__instance) is Coroutine curCoroutine)
                    {
                        return false;
                    }

                    Unit_QQBoss? qqBoss = qqBossField.GetValue(__instance) as Unit_QQBoss;
                    if (qqBoss == null) return true;

                    if (qqBoss.isInStun)
                    {
                        return true;
                    }
                    if (qqBoss.powerCrystalState == 3)
                    {
                        return false;
                    }
                    if (qqBoss.currentAction == Unit_QQBoss.EAnimation.None && __instance.patternTimer.Update(Time.deltaTime))
                    {
                        if (Pattern.Count == 0)
                        {
                            Pattern.Add(0);
                            Pattern.Add(1);
                            Pattern.Add(2);
                            Pattern.Add(3);
                            Pattern.Shuffle();
                            if (Oldpattern >= 0)
                            {
                                Pattern.Remove(Oldpattern);
                                Pattern.Insert(2, Oldpattern);
                            }
                            if (AttackFlag % 2 == 0 || Hard >= 4)
                            {
                                Pattern.Add(4);
                            }
                            else
                            {
                                Pattern.Add(5);
                            }
                            AttackFlag += 1;
                        }
                        int UsePattern = -1;
                        UsePattern = Pattern[0];
                        if (UsePattern < 4)
                            Oldpattern = UsePattern;
                        Pattern.RemoveAt(0);
                        StarSephiriaMod.LogInfoBoss($"终克1阶段当前技能：{UsePattern}");
                        switch (UsePattern)
                        {
                            case 0:
                                {
                                    if (TryGetRandomAlivePlayer(out var playerAvatar))
                                        qqBoss.SatelliteAttack(playerAvatar);
                                    break;
                                }
                            case 1:
                                if (__instance.hardPatternActivated)
                                {
                                    float spd = Mathf.Sign(Random.Range(-1f, 1f)) * __instance.windmillBulletHellHardSpeed;
                                    qqBoss.WindmillBulletHell(spd);
                                }
                                else
                                {
                                    qqBoss.WindmillBulletHell(1f);
                                }
                                break;
                            case 2:
                                {
                                    if (BlinkToTargetCoroutineMethod.Invoke(__instance, null) is IEnumerator ie)
                                    {
                                        Coroutine co = __instance.StartCoroutine(ie);
                                        currentPatternCoroutineField.SetValue(__instance, co);
                                    }
                                    break;
                                }
                            case 3:
                                {
                                    if (TryGetRandomAlivePlayer(out var playerAvatar) && playerAvatar != null)
                                    {
                                        qqBoss.LineGroundAreaAttack(playerAvatar.transform.position);
                                    }
                                    break;
                                }
                            case 4:
                                {
                                    if (FoxAttackCoroutineMethod.Invoke(__instance, null) is IEnumerator ie)
                                    {
                                        Coroutine co = __instance.StartCoroutine(ie);
                                        currentPatternCoroutineField.SetValue(__instance, co);
                                        if (Hard >= 2)
                                        {
                                            __instance.StartCoroutine(FireBulletAndDestroy(qqBoss));
                                            __instance.StartCoroutine(FireBulletInvincible(qqBoss));
                                        }
                                    }
                                    break;
                                }
                            case 5:
                                {
                                    if (BlinkToTargetAndCrystalCoroutineMethod.Invoke(__instance, null) is IEnumerator ie)
                                    {
                                        Coroutine co = __instance.StartCoroutine(ie);
                                        currentPatternCoroutineField.SetValue(__instance, co);
                                        CrystalStack += 1;
                                    }
                                    break;
                                }
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[UnitAI_QQBoss][OnAIUpdate_FoundEnemy]出现错误: {ex}");
                    return true;
                }
            }

            [HarmonyPatch("CheckDeadlyDamage")]
            [HarmonyPrefix]
            static void CheckDeadlyDamage_Prefix(UnitAI_QQBoss __instance, DamageInstance damage)
            {
                if (__instance.Avatar.hp <= 0f)
                {
                    foreach (PlayerSpawner Spawner in PlayerSpawner.MultiplayerList)
                    {
                        PlayerAvatar Player = Spawner.PlayerAvatar;
                        Player.StartTempInvulnerable(16f);
                    }
                    if (qqBossField != null)
                    {
                        Unit_QQBoss? qqBoss = qqBossField.GetValue(__instance) as Unit_QQBoss;
                        if (qqBoss == null) return;

                        if (__instance.isServer && ClearPerimeterBulletsMethod != null)
                            ClearPerimeterBulletsMethod.Invoke(qqBoss, null);
                    }
                }
            }
        }

        private static IEnumerator FireBulletAndDestroy(Unit_QQBoss __instance)
        {
            yield return new WaitForSeconds(2.5f);

            int Stack = 72;
            float Time = 3f;
            if (Hard >= 3)
            {
                Stack = 108;
                Time = 2f;
            }
            float angleStep = 360f / Stack;
            float randomOffset = Random.Range(0f, 360f);
            while(!__instance.NetworkisFoxAttackSafePositionObjectSpawned)
            {
                FireBulletRing(__instance, randomOffset, angleStep, Stack);
                yield return new WaitForSeconds(Time);
            }
        }
        private static IEnumerator FireBulletInvincible(Unit_QQBoss __instance)
        {
            yield return new WaitForSeconds(2.5f);
            while (!__instance.NetworkisFoxAttackSafePositionObjectSpawned)
            {
                yield return null;
            }
            while (!__instance.NetworkcreateFoxAttack)
            {
                foreach (PlayerSpawner Spawner in PlayerSpawner.MultiplayerList)
                {
                    PlayerAvatar Player = Spawner.PlayerAvatar;
                    if ((Player.transform.position - __instance.NetworkfoxAttackSafePosition).sqrMagnitude < 4f)
                        Player.StartTempInvulnerable(5f);
                }
                yield return new WaitForSeconds(0.5f);
            }
        }
        private static void FireBulletRing(Unit_QQBoss __instance, float offsetAngle, float angleStep, int Stack)
        {
            if (Tools.GetPro("QBossRootExplosionBullet", out var Pro) && Pro != null)
            {
                Vector2 vector = __instance.transform.position;
                long hostileFactionLayers = __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack);
                for (int i = 0; i < Stack; i++)
                {
                    Vector2 vector2FromAngle = HorayUtility.GetVector2FromAngle(offsetAngle + angleStep * i);
                    Bullet.Pool.Spawn(Pro, vector, false, EDamageFromType.DirectAttack, "RootExplosion", __instance.RequestStandardDamage() * 0.33f, 0, 0f,
                        __instance, hostileFactionLayers, 0f, vector, vector + vector2FromAngle * 20f, null, null, 0f, EDamageElementalType.Physical, true);
                }
            }
        }

        [HarmonyPatch(typeof(Unit_QQBoss))]
        public static class Unit_QQBoss_Patch
        {
            private static int CrystalAttackCount = -1;

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            static void Update_Postfix(Unit_QQBoss __instance)
            {
                if (windmillBulletHellPrefab == null)
                    windmillBulletHellPrefab = __instance.windmillBulletHellPrefab;
                if (CrystalAttackCount < 0)
                {
                    CrystalAttackCount = __instance.powerCrystalSuccessAttackCount;
                }
                else if(Hard >= 1)
                {
                    float Mult = 1.25f;
                    if (Hard >= 3)
                        Mult = 1.5f;
                    __instance.powerCrystalSuccessAttackCount = CrystalAttackCount.IntMult(Mult);
                }
            }

            [HarmonyPatch("BlinkShowAndTeleport")]
            [HarmonyPostfix]
            static void BlinkShowAndTeleport_Postfix(Unit_QQBoss __instance)
            {
                if (StartPerimeterBulletGimmickMethod == null)
                {
                    StarSephiriaMod.LogError("[Unit_QQBoss][BlinkShowAndTeleport]获取反射失败");
                    return;
                }
                if (Hard >= 3)
                    StartPerimeterBulletGimmickMethod.Invoke(__instance, null);
            }

            [HarmonyPatch("SatelliteAttack")]
            [HarmonyPostfix]
            static void SatelliteAttack_Postfix(Unit_QQBoss __instance, UnitAvatar target)
            {
                if (Hard >= 2)
                {
                    if (__instance.isServer)
                    {
                        __instance.StartCoroutine(FireSatelliteAttack(__instance, target));
                    }
                }
            }
            private static IEnumerator FireSatelliteAttack(Unit_QQBoss __instance, UnitAvatar Target)
            {
                yield return new WaitForSeconds(2.5f);
                __instance.satelliteTarget = Target;
                __instance.CreateSatellite();

                if (Hard >= 3)
                {
                    yield return new WaitForSeconds(2.5f);
                    __instance.satelliteTarget = Target;
                    __instance.CreateSatellite();
                }
                __instance.satelliteTarget = null;
                yield break;
            }

            [HarmonyPatch("StartReceivedDamageIncreaseServer")]
            [HarmonyPrefix]
            static void StartReceivedDamageIncreaseServer_Prefix(Unit_QQBoss __instance)
            {
                __instance.receivedDamageIncreaseInterval = 10f;
                __instance.receivedDamageIncreaseAmount = 10 - Hard * 2;
            }
        }
    }
}