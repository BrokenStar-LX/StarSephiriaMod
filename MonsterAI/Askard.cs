using HarmonyLib;
using Mirror;
using SPMod.Tool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static SPMod.Tool.ModuleComponents;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace SPMod.MonsterAI
{
    [HarmonyPatch]
    public static class Askard
    {
        public static int Hard
        {
            get { return ModConfig.HardMode; }
        }
        // Unit_Askard_Chapter2 反射字段
        private static readonly FieldInfo? bloodyRainCoroutineField;
        private static readonly FieldInfo? curGlitchTentacleField;
        private static readonly FieldInfo? darknessPlayerPreventField;
        private static readonly FieldInfo? tentacleListField;
        private static readonly FieldInfo? isHoveringField;
        private static readonly PropertyInfo? currentActionProperty;
        private static readonly MethodInfo? forceEndFastMoveMethod;
        // UnitAI_Askard_Chapter2 反射字段
        private static readonly FieldInfo? patternsField;
        private static readonly FieldInfo? patternCounterField;
        private static readonly FieldInfo? curPatternIndexField;
        private static readonly FieldInfo? centerPointField;
        private static readonly FieldInfo? askardEnvironmentField;
        private static readonly FieldInfo? askardField;
        private static readonly MethodInfo? startFastMoveFarawayMethod;
        static Askard()
        {
            try
            {
                // Unit_Askard_Chapter2 反射
                Type type = typeof(Unit_Askard_Chapter2);
                bloodyRainCoroutineField = AccessTools.Field(type, "bloodyRainCoroutine");
                curGlitchTentacleField = AccessTools.Field(type, "curGlitchTentacle");
                darknessPlayerPreventField = AccessTools.Field(type, "darknessPlayerPrevent");
                tentacleListField = AccessTools.Field(type, "tentacleList");
                isHoveringField = AccessTools.Field(type, "isHovering");
                currentActionProperty = AccessTools.Property(type, "CurrentAction");
                forceEndFastMoveMethod = AccessTools.Method(type, "ForceEndFastMove");
                StarSephiriaMod.LogInfoBoss("[Unit_Askard_Chapter2]初始化反射");

                // UnitAI_Askard_Chapter2 反射
                type = typeof(UnitAI_Askard_Chapter2);
                patternsField = AccessTools.Field(type, "patterns");
                patternCounterField = AccessTools.Field(type, "patternCounter");
                curPatternIndexField = AccessTools.Field(type, "curPatternIndex");
                centerPointField = AccessTools.Field(type, "centerPoint");
                askardEnvironmentField = AccessTools.Field(type, "askardEnvironment");
                askardField = AccessTools.Field(type, "askard");
                startFastMoveFarawayMethod = AccessTools.Method(type, "StartFastMove_Faraway", new Type[] { typeof(Vector3), typeof(Action) });
                StarSephiriaMod.LogInfoBoss("[UnitAI_Askard_Chapter2]初始化反射");
            }
            catch (Exception ex)
            {
                StarSephiriaMod.LogError($"[Askard]反射初始化失败: {ex}");
            }
        }

        [HarmonyPatch(typeof(Unit_Askard_Chapter2))]
        public static class Unit_Askard_Chapter2_Patch
        {
            [HarmonyPatch("Darkness")]
            [HarmonyPrefix]
            static bool Darkness_Prefix(Unit_Askard_Chapter2 __instance, bool phaseStartMode, ref IEnumerator __result)
            {
                if (Hard <= 1) return true;
                try
                {
                    __result = CustomDarkness(__instance, phaseStartMode);
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[Unit_Askard_Chapter2][Darkness_Prefix]出现错误{ex}");
                    return true;
                }
            }

            private static int RShoot = 0;
            private static readonly List<int> NoShoot = new List<int>() { };
            static IEnumerator CustomDarkness(Unit_Askard_Chapter2 instance, bool phaseStartMode)
            {
                if (bloodyRainCoroutineField == null || curGlitchTentacleField == null || darknessPlayerPreventField == null || tentacleListField == null || isHoveringField == null || currentActionProperty == null || forceEndFastMoveMethod == null)
                {
                    StarSephiriaMod.LogError("[Unit_Askard_Chapter2][Darkness_Prefix]反射获取失败");
                    yield return null;
                }

                currentActionProperty?.SetValue(instance, Unit_Askard.EAction.Darkness);
                if (bloodyRainCoroutineField?.GetValue(instance) is Coroutine bloodyRainCoroutine)
                {
                    instance.StopCoroutine(bloodyRainCoroutine);
                    bloodyRainCoroutineField?.SetValue(instance, null);
                }

                AskardTentacle_Glitch? curGlitchTentacle = curGlitchTentacleField?.GetValue(instance) as AskardTentacle_Glitch;
                curGlitchTentacle?.Hide();
                if (!phaseStartMode)
                {
                    instance.StartFastMove(instance.darknessTeleportPoint, null);
                    float timer = 0f;
                    while (instance.IsMoving)
                    {
                        timer += Time.deltaTime;
                        if (timer > 0.75f)
                        {
                            forceEndFastMoveMethod?.Invoke(instance, null);
                            break;
                        }
                        yield return null;
                    }
                }

                instance.NetworkcanBeTarget = (sbyte)(instance.canBeTarget - 1);
                Vector2 vector = instance.CurrentLookingPosition - (Vector2)instance.transform.position;
                instance.RpcSetDir(Mathf.Sign(vector.x));

                float top = instance.darknessTeleportPoint.y + 3.625f;
                float bottom = instance.darknessTeleportPoint.y - 7.625f;
                float right = instance.darknessTeleportPoint.x + 10f;
                float left = instance.darknessTeleportPoint.x - 10f;
                bool darknessPlayerPrevent = darknessPlayerPreventField?.GetValue(instance) as bool? ?? false;
                if (!darknessPlayerPrevent)
                {
                    darknessPlayerPreventField?.SetValue(instance, true);
                    foreach (PlayerSpawner playerSpawner in PlayerSpawner.MultiplayerList)
                    {
                        if (playerSpawner && playerSpawner.PlayerAvatar.TopdownRigidbody.preventPositionServer)
                        {
                            playerSpawner.PlayerAvatar.TopdownRigidbody.preventPositionServer.PreventPositionServerAuthority(
                                new Vector3(left, bottom), new Vector3(right, top));
                        }
                    }
                }

                instance.StartInvulnerable();
                instance.RpcStartDarkness();
                yield return new WaitForSeconds(0.5f);

                List<AskardTentacle> tentacleList = tentacleListField?.GetValue(instance) as List<AskardTentacle> ?? new List<AskardTentacle>();
                foreach (AskardTentacle askardTentacle in tentacleList)
                {
                    askardTentacle?.BreakForce();
                }
                tentacleList.Clear();
                tentacleListField?.SetValue(instance, tentacleList);

                instance.DespawnAllBullet();
                instance.NetworkisRuningDarknessAnimation = true;

                isHoveringField?.SetValue(instance, false);
                instance.RpcSetShadow(false);
                if (!phaseStartMode)
                {
                    yield return new WaitForSeconds(1.5f);
                }
                else
                {
                    yield return new WaitForSeconds(3f);
                }

                int patternCount = 0;
                while (patternCount < 1)
                {
                    float timer = 0f;
                    while (timer < instance.darknessLineBulletTime)
                    {
                        int num = Random.Range(0, 4);
                        if (Hard >= 3)
                        {
                            num = Random.Range(0, 8);
                            while (NoShoot.Contains(num))
                            {
                                num = Random.Range(0, 8);
                            }
                        }
                        foreach (PlayerSpawner playerSpawner2 in PlayerSpawner.MultiplayerList)
                        {
                            if (playerSpawner2)
                            {
                                Vector3 playerPos = playerSpawner2.transform.position;
                                Vector3 ShootPos = playerPos + (Vector3)Random.insideUnitCircle * Random.Range(0f, 1f);
                                ShootPos.x = Mathf.Clamp(ShootPos.x, left, right);
                                ShootPos.y = Mathf.Clamp(ShootPos.y, bottom, top);
                                Vector3 TrueShootPos = ShootPos;
                                float Rot = 0f;
                                float x = playerPos.x;
                                float y = playerPos.y;
                                float relativeX = x;
                                float relativeY = y;
                                switch (num)
                                {
                                    case 0:
                                        TrueShootPos.y = bottom;
                                        Rot = 0f;  // 朝上
                                        break;
                                    case 1:
                                        TrueShootPos.x = left;
                                        Rot = -90f;  // 朝右
                                        break;
                                    case 2:
                                        TrueShootPos.y = top;
                                        Rot = 180f;  // 朝下
                                        break;
                                    case 3:
                                        TrueShootPos.x = right;
                                        Rot = 90f;  // 朝左
                                        break;
                                    case 4:  // 左下方向
                                        NoShoot.Add(4);
                                        CalculateLaserSpawnPoint(left, bottom, new Vector3(-1f, -1f, 0f), ref TrueShootPos, playerPos, ref Rot);
                                        break;
                                    case 5:  // 左上方向
                                        NoShoot.Add(5);
                                        CalculateLaserSpawnPoint(left, top, new Vector3(-1f, 1f, 0f), ref TrueShootPos, playerPos, ref Rot);
                                        break;
                                    case 6:  // 右上方向
                                        NoShoot.Add(6);
                                        CalculateLaserSpawnPoint(right, top, new Vector3(1f, 1f, 0f), ref TrueShootPos, playerPos, ref Rot);
                                        break;
                                    case 7:  // 右下方向
                                        NoShoot.Add(7);
                                        CalculateLaserSpawnPoint(right, bottom, new Vector3(1f, -1f, 0f), ref TrueShootPos, playerPos, ref Rot);
                                        break;
                                }
                                TrueShootPos.x = Mathf.Clamp(TrueShootPos.x, left, right);
                                TrueShootPos.y = Mathf.Clamp(TrueShootPos.y, bottom, top);

                                AskardDarknessLaser askardDarknessLaser = Object.Instantiate(instance.darknessLaserPrefab, TrueShootPos, Quaternion.identity);
                                askardDarknessLaser.Initialize(instance, instance.darknessLaserDamage, Rot, 0.4f);
                                NetworkServer.Spawn(askardDarknessLaser.gameObject, playerSpawner2.gameObject);

                                RShoot += 1;
                                if (RShoot >= 3 && NoShoot.Count > 0)
                                {
                                    NoShoot.RemoveAt(0);
                                    RShoot = 0;
                                }
                            }
                        }
                        float ShootTime = timer / instance.darknessLineBulletTime;
                        float Mult = 0.75f;
                        if (Hard == 3) Mult = 0.66f;
                        float TimeCD = Mathf.Lerp(1.25f, 0.45f, Mathf.Clamp(ShootTime / 0.85f, 0f, 1f)) * Mult;
                        timer += TimeCD;
                        yield return new WaitForSeconds(TimeCD);
                    }
                    yield return new WaitForSeconds(1f);

                    int lastDir = Random.Range(0, 4);
                    Vector3 lineCenterPos = Vector3.zero;
                    Vector3 lineCenterPos2 = Vector3.zero;
                    switch (lastDir)
                    {
                        case 0:
                            lineCenterPos = new Vector3(instance.darknessTeleportPoint.x, bottom);
                            lineCenterPos2 = new Vector3(left, instance.darknessTeleportPoint.y - 2f);
                            break;
                        case 1:
                            lineCenterPos = new Vector3(left, instance.darknessTeleportPoint.y - 2f);
                            lineCenterPos2 = new Vector3(instance.darknessTeleportPoint.x, top);
                            break;
                        case 2:
                            lineCenterPos = new Vector3(instance.darknessTeleportPoint.x, top);
                            lineCenterPos2 = new Vector3(right, instance.darknessTeleportPoint.y - 2f);
                            break;
                        case 3:
                            lineCenterPos = new Vector3(right, instance.darknessTeleportPoint.y - 2f);
                            lineCenterPos2 = new Vector3(instance.darknessTeleportPoint.x, bottom);
                            break;
                    }
                    int AttackStack = 4;
                    int TC;
                    for (int i = AttackStack - 1; i >= 0; i = TC)
                    {
                        for (int j = 1; j >= -1; j -= 2)
                        {
                            Vector3 vector4 = Vector3.zero;
                            float num6 = 0f;
                            switch (lastDir)
                            {
                                case 0:
                                    vector4 = Vector3.right;
                                    num6 = 0f;
                                    break;
                                case 1:
                                    vector4 = Vector3.up;
                                    num6 = -90f;
                                    break;
                                case 2:
                                    vector4 = Vector3.right;
                                    num6 = 180f;
                                    break;
                                case 3:
                                    vector4 = Vector3.up;
                                    num6 = 90f;
                                    break;
                            }
                            Vector3 vector5 = lineCenterPos + (vector4 * i * instance.lastLineDistance + vector4 * instance.lastLineCenterArea) * j;
                            AskardDarknessLaser askardDarknessLaser2 = Object.Instantiate(instance.darknessLaserPrefab_AllPlayer, vector5, Quaternion.identity);
                            askardDarknessLaser2.Initialize(instance, instance.darknessLaserDamage, num6, instance.lastLineDelay * i + 1f);
                            NetworkServer.Spawn(askardDarknessLaser2.gameObject);

                            if (i > 0 && Hard >= 3)
                            {
                                vector4 = Vector3.zero;
                                num6 = 0f;
                                switch (lastDir)
                                {
                                    case 0:
                                        vector4 = Vector3.up;
                                        num6 = -90f;
                                        break;
                                    case 1:
                                        vector4 = Vector3.right;
                                        num6 = 180f;
                                        break;
                                    case 2:
                                        vector4 = Vector3.up;
                                        num6 = 90f;
                                        break;
                                    case 3:
                                        vector4 = Vector3.right;
                                        num6 = 0f;
                                        break;
                                }
                                vector5 = lineCenterPos2 + (vector4 * i * instance.lastLineDistance + vector4 * instance.lastLineCenterArea) * j;
                                askardDarknessLaser2 = Object.Instantiate(instance.darknessLaserPrefab_AllPlayer, vector5, Quaternion.identity);
                                askardDarknessLaser2.Initialize(instance, instance.darknessLaserDamage, num6, instance.lastLineDelay * i + 1f);
                                NetworkServer.Spawn(askardDarknessLaser2.gameObject);
                            }
                        }
                        yield return new WaitForSeconds(instance.lastLineDelay);
                        TC = i - 1;
                    }

                    float Damage = instance.RequestStandardDamage() * instance.darknessCenterBulletDamage;
                    int Stack = 1;
                    if (Hard >= 2)
                    {
                        Damage *= 0.75f;
                        Stack = 2;
                    }
                    for (int A = 0; A < Stack; A++)
                    {
                        switch (lastDir)
                        {
                            case 0:
                                lineCenterPos += Vector3.down;
                                break;
                            case 1:
                                lineCenterPos += Vector3.left;
                                break;
                            case 2:
                                lineCenterPos += Vector3.up;
                                break;
                            case 3:
                                lineCenterPos += Vector3.right;
                                break;
                        }

                        Bullet.Pool.Spawn(instance.darknessCenterBulletPrefab, lineCenterPos, false, EDamageFromType.DirectAttack, "BasicAttack",
                            Damage, 75, 8.5f, instance,
                            instance.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0f, lineCenterPos,
                            lineCenterPos + HorayUtility.GetVector3FromAngle(-90f * lastDir) * 5f, null, null, 0f, EDamageElementalType.Physical, true);

                        yield return new WaitForSeconds(0.75f);
                    }

                    TC = patternCount + 1;
                    patternCount = TC;
                }

                instance.ReqSetPosition(instance.darknessTeleportPoint, true);
                yield return new WaitForSeconds(3f);
                instance.RpcOnEndDarkness();

                darknessPlayerPrevent = darknessPlayerPreventField?.GetValue(instance) as bool? ?? false;
                if (darknessPlayerPrevent)
                {
                    foreach (PlayerSpawner playerSpawner3 in PlayerSpawner.MultiplayerList)
                    {
                        if (playerSpawner3 && playerSpawner3.PlayerAvatar.TopdownRigidbody.preventPositionServer)
                        {
                            playerSpawner3.PlayerAvatar.TopdownRigidbody.preventPositionServer.ResetPreventPositionServerAuthority();
                        }
                    }
                    darknessPlayerPreventField?.SetValue(instance, false);
                }

                yield return new WaitForSeconds(2f);
                if (Hard >= 4)
                {
                    int Value = instance.GetCustomStatUnsafe(TextFile.ReceivedDamageReduction);
                    if (Value > 0) instance.AddCustomStatUnsafe(TextFile.ReceivedDamageReduction, -Value);
                    instance.AddCustomStatUnsafe(TextFile.ReceivedDamageReduction, 60);
                    StarSephiriaPatches.EvilGuardianEffect[instance] = new CountingTimer(30, 1f);
                }
                instance.StopInvulnerable();
                instance.NetworkcanBeTarget = (sbyte)(instance.canBeTarget + 1);
                instance.RpcSetShadow(true);

                curGlitchTentacle?.Show();
                instance.NetworkisRuningDarknessAnimation = false;
                isHoveringField?.SetValue(instance, true);
                currentActionProperty?.SetValue(instance, Unit_Askard.EAction.None);
                yield break;
            }

            private static void CalculateLaserSpawnPoint(float x, float y, Vector3 Direction, ref Vector3 ShootPos, Vector3 PlayerPos, ref float Rot)
            {
                ShootPos = new Vector3(x, y, 0);
                Vector3 dirToPlayer = PlayerPos - ShootPos;
                Rot = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg - 90f;
            }
        }

        [HarmonyPatch(typeof(UnitAI_Askard_Chapter2))]
        public static class UnitAI_Askard_Chapter2_Patch
        {
            private static int Oldpattern = -1;

            [HarmonyPatch("OnAIUpdate_FoundEnemy")]
            [HarmonyPrefix]
            static bool OnAIUpdate_FoundEnemy_Prefix(UnitAI_Askard_Chapter2 __instance)
            {
                if (Hard <= 1) return true;
                try
                {
                    if (patternsField == null || patternCounterField == null || curPatternIndexField == null || centerPointField == null ||
                        askardEnvironmentField == null || askardField == null || startFastMoveFarawayMethod == null)
                    {
                        StarSephiriaMod.LogError("[OnAIUpdate_FoundEnemy]反射字段失败");
                        return true;
                    }

                    // 通过反射获取私有字段的值
                    List<int> patterns = (List<int>)patternsField.GetValue(__instance);
                    int patternCounter = (int)patternCounterField.GetValue(__instance);
                    int curPatternIndex = (int)curPatternIndexField.GetValue(__instance);
                    Transform centerPoint = (Transform)centerPointField.GetValue(__instance);
                    BossEnvironment_Askard askardEnvironment = (BossEnvironment_Askard)askardEnvironmentField.GetValue(__instance);
                    Unit_Askard_Chapter2 askard = (Unit_Askard_Chapter2)askardField.GetValue(__instance);

                    if (!askard.isBattlePhase || __instance.IsLifeDecreased || __instance.IsDramaticDying)
                    {
                        return false;
                    }

                    float RainAttackSpeed = 1f + Hard * 0.5f;
                    __instance.bloodyRainPatternTimer.AddTimer(Time.deltaTime * RainAttackSpeed);
                    if (!askard.IsMoving && askard.CurrentAction == Unit_Askard.EAction.None)
                    {
                        float AttackSpeed = 1.25f + Hard * 0.25f;
                        if (__instance.tentaclePatternDelay.Update(Time.deltaTime * AttackSpeed))
                        {
                            int currentPhase = askard.currentPhase;
                            if (patterns.Count == 0)
                            {
                                patterns.Add(0);
                                patterns.Add(1);
                                patterns.Add(2);
                                patterns.Add(3);
                                patterns.Add(4);
                                patterns.Add(5);
                                patterns.Add(6);
                                patterns.Shuffle();
                                if (Oldpattern >= 0)
                                {
                                    patterns.Remove(Oldpattern);
                                    patterns.Insert(patterns.Count / 2, Oldpattern);
                                }
                                if (askard.currentPhase >= 1)
                                    patterns.Add(7);
                            }
                            curPatternIndex = patterns[0];
                            patterns.RemoveAt(0);
                            if (curPatternIndex != 7)
                                Oldpattern = curPatternIndex;
                            StarSephiriaMod.LogInfoBoss($"主教当前技能：{curPatternIndex}");
                            switch (curPatternIndex)
                            {
                                case 0:
                                    askard.StartTentacle_Point(__instance.CurrentTarget.transform);
                                    break;
                                case 1:
                                    startFastMoveFarawayMethod.Invoke(__instance, new object[]
                                    {
                                        __instance.CurrentTarget.transform.position,
                                        (Action)delegate { askard.StartTentacle_Line(__instance.CurrentTarget.transform); }
                                    });
                                    break;
                                case 2:
                                    askard.StartTentacle_Fill(__instance.CurrentTarget.transform);
                                    break;
                                case 3:
                                    askard.StartTentacle_Wave(__instance.CurrentTarget.transform);
                                    break;
                                case 4:
                                    askard.StartHomingBullet(__instance.CurrentTarget.transform, centerPoint.position, askard.currentPhase >= 1);
                                    break;
                                case 5:
                                    askard.StartLaser(__instance.CurrentTarget.transform, askard.currentPhase >= 1);
                                    break;
                                case 6:
                                    askard.StartStomp(__instance.CurrentTarget.transform, centerPoint.position, askard.currentPhase >= 1);
                                    break;
                                case 7:
                                    askard.StartDarkness(__instance.CurrentTarget.transform, false);
                                    break;
                                case 8:
                                    askard.StartSimpleBullet();
                                    break;
                            }

                            curPatternIndex = -1;
                            patternCounter++;
                            curPatternIndexField.SetValue(__instance, curPatternIndex);
                            patternCounterField.SetValue(__instance, patternCounter);
                            patternsField.SetValue(__instance, patterns);
                            return false;
                        }
                        else if (__instance.hardPatternActivated && askardEnvironment != null && __instance.bloodyRainPatternTimer.Update(0f))
                        {
                            if (Random.Range(0, 2) == 0)
                            {
                                askard.StartBloodyRain(askardEnvironment.bloodRainLeftsidePoint.position, 1f);
                                return false;
                            }
                            askard.StartBloodyRain(askardEnvironment.bloodRainRightsidePoint.position, -1f);
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[UnitAI_Askard_Chapter2][OnAIUpdate_FoundEnemy]出现错误{ex}");
                    return true;
                }
            }
        }
    }
}