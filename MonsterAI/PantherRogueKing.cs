using FMODUnity;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SPMod.MonsterAI
{
    [HarmonyPatch]
    public static class PantherRogueKing
    {
        public static int Hard
        {
            get { return ModConfig.HardMode; }
        }
        // UnitAI_PantherRogueKing 反射字段
        private static readonly FieldInfo? pantherRogueKingField;
        private static readonly FieldInfo? pantherStateField;
        private static readonly FieldInfo? patternRoundRobbinField;
        private static readonly FieldInfo? patternCoroutineField;
        private static readonly FieldInfo? pantherEnvironmentField;
        private static readonly FieldInfo? isEndEnterCutSceneField;
        private static readonly MethodInfo? rushPatternCoroutineMethod;
        private static readonly MethodInfo? cloneRandomDaggerPatternCoroutineMethod;
        private static readonly MethodInfo? cloneDaggerPatternCoroutineMethod;
        private static readonly MethodInfo? cloneDaggerSpreadPatternCoroutineMethod;
        private static readonly MethodInfo? basicAttackPatternCoroutineMethod;
        // Unit_PantherRogueKing 反射字段
        private static readonly FieldInfo? sharedCBField;

        static PantherRogueKing()
        {
            try
            {
                // UnitAvatar 反射
                Type type = typeof(UnitAI_PantherRogueKing);
                pantherRogueKingField = AccessTools.Field(type, "king");
                pantherStateField = AccessTools.Field(type, "state");
                patternRoundRobbinField = AccessTools.Field(type, "patternRoundRobbin");
                patternCoroutineField = AccessTools.Field(type, "pattern");
                pantherEnvironmentField = AccessTools.Field(type, "pantherEnvironment");
                isEndEnterCutSceneField = AccessTools.Field(type, "isEndEnterCutScene");
                rushPatternCoroutineMethod = AccessTools.Method(type, "RushPatternCoroutine");
                cloneRandomDaggerPatternCoroutineMethod = AccessTools.Method(type, "CloneRandomDaggerPatternCoroutine");
                cloneDaggerPatternCoroutineMethod = AccessTools.Method(type, "CloneDaggerPatternCoroutine");
                cloneDaggerSpreadPatternCoroutineMethod = AccessTools.Method(type, "CloneDaggerSpreadPatternCoroutine");
                basicAttackPatternCoroutineMethod = AccessTools.Method(type, "BasicAttackPatternCoroutine");
                StarSephiriaMod.LogInfoBoss("[UnitAI_PantherRogueKing]初始化反射成功");

                // Unit_PantherRogueKing 反射
                type = typeof(Unit_PantherRogueKing);
                sharedCBField = AccessTools.Field(type, "sharedCB");
                StarSephiriaMod.LogInfoBoss("[Unit_PantherRogueKing]初始化反射成功");
            }
            catch (Exception ex)
            {
                StarSephiriaMod.LogError($"[PantherRogueKing] 反射初始化失败: {ex}");
            }
        }

        [HarmonyPatch(typeof(Unit_PantherRogueKing))]
        public static class Unit_PantherRogueKing_Patch
        {
            [HarmonyPatch("FireSlowShurikenAnimation")]
            [HarmonyPrefix]
            static bool FireSlowShurikenAnimation_Prefix(Unit_PantherRogueKing __instance)
            {
                if (Hard <= 2) return true;
                try
                {
                    if (sharedCBField == null)
                    {
                        StarSephiriaMod.LogError($"[Unit_PantherRogueKing][FireSlowShurikenAnimation]反射失败");
                        return true;
                    }
                    FireSlowShuriken(__instance);
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[Unit_PantherRogueKing][FireSlowShurikenAnimation]出现错误{ex}");
                    return true;
                }
            }

            [HarmonyPatch("FireSlowShuriken2Animation")]
            [HarmonyPrefix]
            static bool FireSlowShuriken2Animation_Prefix(Unit_PantherRogueKing __instance)
            {
                if (Hard <= 2) return true;
                try
                {
                    if (sharedCBField == null)
                    {
                        StarSephiriaMod.LogError($"[Unit_PantherRogueKing][FireSlowShuriken2Animation]反射失败");
                        return true;
                    }
                    FireSlowShuriken(__instance);
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[Unit_PantherRogueKing][FireSlowShuriken2Animation]出现错误{ex}");
                    return true;
                }
            }

            static void FireSlowShuriken(Unit_PantherRogueKing __instance)
            {
                if (sharedCBField == null) return;

                if (!__instance.slowShurikenThrowSound.IsNull)
                {
                    RuntimeManager.PlayOneShot(__instance.slowShurikenThrowSound, __instance.transform.position);
                }
                if (__instance.isServer)
                {
                    float angle = HorayUtility.GetAngle(__instance.CurrentLookingPosition, __instance.transform.position);
                    angle += Random.Range(-45f, 45f);
                    List<CombatBehaviour> sharedCB = (List<CombatBehaviour>)sharedCBField.GetValue(__instance);
                    for (float r = 0f; r <= 360f; r += 24f)
                    {
                        Vector2 vector = HorayUtility.GetVector2FromAngle(angle + r) * 3f;
                        Bullet.Pool.Spawn(__instance.slowShurikenBulletPrefab, __instance.transform.position, false, EDamageFromType.DirectAttack, "BasicAttack",
                            __instance.RequestStandardDamage(), 25, 3f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0.5f,
                            __instance.transform.position, (Vector2)__instance.transform.position + vector, Hard >= 4 ? null : sharedCB, null, 0f,
                            EDamageElementalType.Physical, true).speedScale = Random.Range(0f, 0.1f) + 0.9f;
                    }
                }
            }

            [HarmonyPatch("FireFuryAnimation")]
            [HarmonyPrefix]
            static bool FireFuryAnimation_Prefix(Unit_PantherRogueKing __instance)
            {
                if (Hard <= 0) return true;
                try
                {
                    if (sharedCBField == null)
                    {
                        StarSephiriaMod.LogError($"[Unit_PantherRogueKing][FireFuryAnimation]反射失败");
                        return true;
                    }
                    if (__instance.isServer)
                    {
                        List<CombatBehaviour> sharedCB = (List<CombatBehaviour>)sharedCBField.GetValue(__instance);
                        float Mult = 0.2f * Hard;
                        if (Hard >= 4) Mult += 0.2f;
                        __instance.furyFireData.CreateAttack(EDamageFromType.DirectAttack, __instance.RequestStandardDamage(), "BasicAttack", false, __instance, __instance.transform.position, __instance.transform.position + __instance.furyDirection, 0.2f, null, sharedCB, 1f, null, false, Mult);
                    }
                    GameCamera.Instance.targetTracker.CreateCameraShaking(__instance.transform.position, EShakeCameraType.Continous, Vector2.right * 0.0625f, 0.125f, 1f);
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[Unit_PantherRogueKing][FireFuryAnimation]出现错误{ex}");
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(UnitAI_PantherRogueKing))]
        public static class UnitAI_PantherRogueKing_Patch
        {
            private static int Oldpattern = -1;
            private static Unit_PantherRogueKing? king = null;
            private static UnitAI_PantherRogueKing.State state = UnitAI_PantherRogueKing.State.None;
            private static bool isEndEnterCutScene = false;

            [HarmonyPatch("OnAIUpdate_FoundEnemy")]
            [HarmonyPrefix]
            static bool OnAIUpdate_FoundEnemy_Prefix(UnitAI_PantherRogueKing __instance)
            {
                if (Hard <= 0) return true;
                try
                {
                    if (pantherRogueKingField == null ||
                        pantherStateField == null ||
                        patternRoundRobbinField == null ||
                        patternCoroutineField == null ||
                        rushPatternCoroutineMethod == null ||
                        cloneRandomDaggerPatternCoroutineMethod == null ||
                        cloneDaggerPatternCoroutineMethod == null ||
                        cloneDaggerSpreadPatternCoroutineMethod == null ||
                        basicAttackPatternCoroutineMethod == null ||
                        isEndEnterCutSceneField == null)
                    {
                        StarSephiriaMod.LogError("[UnitAI_PantherRogueKing][Update]获取反射失败");
                        return true;
                    }

                    king = (Unit_PantherRogueKing)pantherRogueKingField.GetValue(__instance);
                    state = (UnitAI_PantherRogueKing.State)pantherStateField.GetValue(__instance);
                    isEndEnterCutScene = (bool)isEndEnterCutSceneField.GetValue(__instance);
                    if (king.patternState == Unit_PantherRogueKing.EPatternState.Fury && (__instance.CurrentTarget.transform.position - __instance.transform.position).magnitude < 4f)
                    {
                        king.FurySwing();
                    }
                    float AttackSpeed = 1.2f + 0.1f * Hard;
                    if (Hard >= 4) AttackSpeed *= 1.2f;
                    if (king.patternState == Unit_PantherRogueKing.EPatternState.Idle && state == UnitAI_PantherRogueKing.State.None
                        && __instance.majorPatternTimer.Update(Time.deltaTime * AttackSpeed))
                    {
                        List<int> patternRoundRobbin = (List<int>)patternRoundRobbinField.GetValue(__instance);
                        if (patternRoundRobbin.Count <= 0)
                        {
                            patternRoundRobbin.Add(0);
                            patternRoundRobbin.Add(1);
                            patternRoundRobbin.Add(2);
                            patternRoundRobbin.Add(3);
                            patternRoundRobbin.Add(5);
                            patternRoundRobbin.Shuffle();
                            patternRoundRobbin.Add(4);
                            if (Oldpattern >= 0)
                            {
                                patternRoundRobbin.Remove(Oldpattern);
                                patternRoundRobbin.Insert(2, Oldpattern);
                            }
                        }

                        int UsePattern = patternRoundRobbin[0];
                        if (UsePattern != 4) Oldpattern = UsePattern;
                        StarSephiriaMod.LogInfoBoss($"耄耋当前技能：{UsePattern}");
                        switch (UsePattern)
                        {
                            case 1:
                                king.SlowShuriken();
                                __instance.majorPatternTimer.Ratio = 0.45f;
                                break;

                            case 2:
                                state = UnitAI_PantherRogueKing.State.Rush;
                                IEnumerator rushCoroutine = (IEnumerator)rushPatternCoroutineMethod.Invoke(__instance, null);
                                Coroutine patternCoroutine = __instance.StartCoroutine(rushCoroutine);
                                patternCoroutineField.SetValue(__instance, patternCoroutine);
                                break;

                            case 3:
                                state = UnitAI_PantherRogueKing.State.CloneRandomDagger;
                                IEnumerator cloneRandomCoroutine = (IEnumerator)cloneRandomDaggerPatternCoroutineMethod.Invoke(__instance, null);
                                patternCoroutineField.SetValue(__instance, __instance.StartCoroutine(cloneRandomCoroutine));
                                break;

                            case 4:
                                state = UnitAI_PantherRogueKing.State.CloneDagger;
                                IEnumerator cloneDaggerCoroutine = (IEnumerator)cloneDaggerPatternCoroutineMethod.Invoke(__instance, new object[] { false });
                                patternCoroutineField.SetValue(__instance, __instance.StartCoroutine(cloneDaggerCoroutine));
                                break;

                            case 5:
                                state = UnitAI_PantherRogueKing.State.CloneDaggerSpread;
                                IEnumerator spreadCoroutine = (IEnumerator)cloneDaggerSpreadPatternCoroutineMethod.Invoke(__instance, null);
                                patternCoroutineField.SetValue(__instance, __instance.StartCoroutine(spreadCoroutine));
                                break;

                            default:
                                state = UnitAI_PantherRogueKing.State.BasicAttack;
                                IEnumerator basicCoroutine = (IEnumerator)basicAttackPatternCoroutineMethod.Invoke(__instance, null);
                                patternCoroutineField.SetValue(__instance, __instance.StartCoroutine(basicCoroutine));
                                __instance.majorPatternTimer.Ratio = 0.5f;
                                break;
                        }
                        pantherStateField.SetValue(__instance, state);
                        patternRoundRobbin.RemoveAt(0);
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[UnitAI_PantherRogueKing][OnAIUpdate_FoundEnemy]出现错误: {ex}");
                    return true;
                }
            }

            [HarmonyPatch("CloneDaggerPatternCoroutine")]
            [HarmonyPrefix]
            static bool CloneDaggerPatternCoroutine_Prefix(UnitAI_PantherRogueKing __instance, bool isCutscene, ref IEnumerator __result)
            {
                if (Hard <= 0) return true;
                try
                {
                    __result = CloneDaggerPatternCoroutine(__instance, isCutscene);
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[UnitAI_PantherRogueKing][CloneDaggerPatternCoroutine]出现错误{ex}");
                    return true;
                }
            }

            static IEnumerator CloneDaggerPatternCoroutine(UnitAI_PantherRogueKing __instance, bool isCutscene)
            {
                if (pantherRogueKingField == null || pantherEnvironmentField == null || patternCoroutineField == null || pantherStateField == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_PantherRogueKing][CloneDaggerPatternCoroutine]获取反射失败");
                    yield break;
                }
                Unit_PantherRogueKing king = (Unit_PantherRogueKing)pantherRogueKingField.GetValue(__instance);
                BossEnvironment_Panther pantherEnvironment = (BossEnvironment_Panther)pantherEnvironmentField.GetValue(__instance);
                if (!isCutscene)
                {
                    king.SetStealth(true);
                }
                if (pantherEnvironment)
                {
                    PantherRogueKingShadow shadow;
                    PantherRogueKingShadow shadow2;
                    PantherRogueKingShadow shadow3;
                    PantherRogueKingShadow? shadow4 = null;
                    PantherRogueKingShadow? shadow5 = null;
                    if (!isCutscene && Hard >= 2)
                    {
                        float Rand = 45f;
                        float RotR = 90f;
                        if (Hard >= 4) RotR = 72f;
                        Vector2 Pos = (Vector2)pantherEnvironment.transform.position + pantherEnvironment.cloneShurikenCloneAreaOffset;
                        shadow = king.CreateShadow(Pos + HorayUtility.GetVector2FromAngle(Rand) * pantherEnvironment.cloneShurikenCloneAreaRadius);
                        shadow2 = king.CreateShadow(Pos + HorayUtility.GetVector2FromAngle(Rand + RotR) * pantherEnvironment.cloneShurikenCloneAreaRadius);
                        shadow3 = king.CreateShadow(Pos + HorayUtility.GetVector2FromAngle(Rand + RotR * 2f) * pantherEnvironment.cloneShurikenCloneAreaRadius);
                        if (Hard >= 3) shadow4 = king.CreateShadow(Pos + HorayUtility.GetVector2FromAngle(Rand + RotR * 3f) * pantherEnvironment.cloneShurikenCloneAreaRadius);
                        if (Hard >= 4) shadow5 = king.CreateShadow(Pos + HorayUtility.GetVector2FromAngle(Rand + RotR * 4f) * pantherEnvironment.cloneShurikenCloneAreaRadius);
                    }
                    else
                    {
                        Vector2 Pos1 = (Vector2)pantherEnvironment.transform.position + pantherEnvironment.cloneShurikenCloneAreaOffset + Random.insideUnitCircle.normalized * pantherEnvironment.cloneShurikenCloneAreaRadius;
                        shadow = king.CreateShadow(Pos1);

                        Vector2 Pos2 = (Vector2)pantherEnvironment.transform.position + pantherEnvironment.cloneShurikenCloneAreaOffset + Random.insideUnitCircle.normalized * pantherEnvironment.cloneShurikenCloneAreaRadius;
                        shadow2 = king.CreateShadow(Pos2);

                        shadow3 = king.CreateShadow(__instance.transform.position);
                    }
                    if (isCutscene)
                    {
                        while (!isEndEnterCutScene)
                        {
                            yield return null;
                        }
                        yield return new WaitForSeconds(2.4f);
                    }
                    else
                    {
                        yield return new WaitForSeconds(1f);
                    }
                    shadow.ThrowDagger(__instance.Avatar, __instance.CurrentTarget);
                    shadow2.ThrowDagger(__instance.Avatar, __instance.CurrentTarget);
                    shadow3.ThrowDagger(__instance.Avatar, __instance.CurrentTarget);
                    shadow4?.ThrowDagger(__instance.Avatar, __instance.CurrentTarget);
                    shadow5?.ThrowDagger(__instance.Avatar, __instance.CurrentTarget);
                    yield return new WaitForSeconds(2f);
                    king.DestroyShadow(shadow);
                    king.DestroyShadow(shadow2);
                    king.DestroyShadow(shadow3);
                    if (shadow4 != null) king.DestroyShadow(shadow4);
                    if (shadow5 != null) king.DestroyShadow(shadow5);
                }
                if (Hard < 4)
                {
                    yield return new WaitForSeconds(1f);
                    king.SetStealth(false);
                }
                else
                {
                    king.SetStealth(false);
                    yield return new WaitForSeconds(0.25f);
                    king.BeginDaggerSpreadAnimation();
                    yield return new WaitForSeconds(0.5f);
                    sharedCBField?.SetValue(king, new List<CombatBehaviour>());
                    king.FireDaggerSpreadAnimation();
                    yield return new WaitForSeconds(0.25f);
                }
                pantherStateField.SetValue(__instance, UnitAI_PantherRogueKing.State.None);
                patternCoroutineField.SetValue(__instance, null);
                yield break;
            }

            [HarmonyPatch("CloneDaggerSpreadPatternCoroutine")]
            [HarmonyPrefix]
            static bool CloneDaggerSpreadPatternCoroutine_Prefix(UnitAI_PantherRogueKing __instance, ref IEnumerator __result)
            {
                if (Hard <= 1) return true;
                try
                {
                    __result = CloneDaggerSpreadPatternCoroutine(__instance);
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[UnitAI_PantherRogueKing][CloneDaggerSpreadPatternCoroutine]出现错误{ex}");
                    return true;
                }
            }

            static IEnumerator CloneDaggerSpreadPatternCoroutine(UnitAI_PantherRogueKing __instance)
            {
                if (pantherRogueKingField == null || pantherEnvironmentField == null || patternCoroutineField == null || pantherStateField == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_PantherRogueKing][CloneDaggerPatternCoroutine]获取反射失败");
                    yield break;
                }
                Unit_PantherRogueKing king = (Unit_PantherRogueKing)pantherRogueKingField.GetValue(__instance);
                BossEnvironment_Panther pantherEnvironment = (BossEnvironment_Panther)pantherEnvironmentField.GetValue(__instance);
                Vector2 vector = (Vector2)pantherEnvironment.transform.position + pantherEnvironment.cloneShurikenCloneAreaOffset + Random.insideUnitCircle.normalized * pantherEnvironment.cloneShurikenCloneAreaRadius;
                PantherRogueKingShadow shadow = king.CreateShadow(vector);
                vector = (Vector2)pantherEnvironment.transform.position + pantherEnvironment.cloneShurikenCloneAreaOffset + Random.insideUnitCircle.normalized * pantherEnvironment.cloneShurikenCloneAreaRadius;
                PantherRogueKingShadow shadow2 = king.CreateShadow(vector);
                king.DaggerSpread();
                yield return new WaitForSeconds(0.5f);
                shadow.DaggerSpread(__instance.Avatar);
                yield return new WaitForSeconds(0.5f);
                shadow2.DaggerSpread(__instance.Avatar);
                if (Hard >= 3) king.DaggerSpread();
                pantherStateField.SetValue(__instance, UnitAI_PantherRogueKing.State.None);
                patternCoroutineField.SetValue(__instance, null);
                yield return new WaitForSeconds(1.5f);
                king.DestroyShadow(shadow);
                king.DestroyShadow(shadow2);
                yield break;
            }

            [HarmonyPatch("BasicAttackPatternCoroutine")]
            [HarmonyPrefix]
            static bool BasicAttackPatternCoroutine_Prefix(UnitAI_PantherRogueKing __instance, ref IEnumerator __result)
            {
                if (Hard <= 0) return true;
                try
                {
                    __result = BasicAttackPatternCoroutine(__instance);
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[UnitAI_PantherRogueKing][BasicAttackPatternCoroutine]出现错误{ex}");
                    return true;
                }
            }

            static IEnumerator BasicAttackPatternCoroutine(UnitAI_PantherRogueKing __instance)
            {
                if (pantherRogueKingField == null || patternCoroutineField == null || pantherStateField == null || sharedCBField == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_PantherRogueKing][BasicAttackPatternCoroutine]获取反射失败");
                    yield break;
                }
                Unit_PantherRogueKing king = (Unit_PantherRogueKing)pantherRogueKingField.GetValue(__instance);

                for(int AttackStack = 0; AttackStack < Math.Min(1 + Hard, 3); AttackStack++)
                {
                    king.SetStealth(true);
                    yield return new WaitForSeconds(1.2f);
                    if (__instance.CurrentTarget != null)
                    {
                        Vector3 prevTargetPos = __instance.CurrentTarget.transform.position;
                        bool blinkPosFound = false;
                        Vector2 blinkPos;
                        do
                        {
                            if (__instance.CurrentTarget != null)
                            {
                                prevTargetPos = __instance.CurrentTarget.transform.position;
                            }
                            blinkPos = (Vector2)prevTargetPos + Random.insideUnitCircle.normalized * 3f;
                            if (__instance.Environment)
                            {
                                blinkPos.x = Mathf.Clamp(blinkPos.x, __instance.Environment.transform.position.x + __instance.Environment.bossZoneLeftSide + 3f, __instance.Environment.transform.position.x + __instance.Environment.bossZoneRightSide - 3f);
                                blinkPos.y = Mathf.Clamp(blinkPos.y, __instance.Environment.transform.position.y + __instance.Environment.bossZoneBottomSide + 3f, __instance.Environment.transform.position.y + __instance.Environment.bossZoneTopSide - 3f);
                            }
                            if (!Physics2D.CircleCast(blinkPos, 1.5f, default, 0f, CombatManager.BlockCharacterLayerMask))
                            {
                                blinkPosFound = true;
                            }
                            yield return null;
                        }
                        while (!blinkPosFound);
                        king.ReqSetPosition(blinkPos, true);
                        yield return new WaitForSeconds(0.1f);
                    }
                    king.SetStealth(false);
                    yield return new WaitForSeconds(0.1f);
                    if (__instance.CurrentTarget != null)
                    {
                        king.ForceAimToPosition(__instance.CurrentTarget.transform.position);
                    }
                    if (AttackStack == 2 && Hard >= 3)
                    {
                        yield return new WaitForSeconds(0.1f);
                        king.FireBasicAttackAnimation();
                        yield return new WaitForSeconds(0.5f);
                        king.BeginDaggerSpreadAnimation();
                        yield return new WaitForSeconds(0.25f);
                        sharedCBField.SetValue(king, new List<CombatBehaviour>());
                        king.FireDaggerSpreadAnimation();
                    }
                    else king.BasicAttack();
                    yield return new WaitForSeconds(0.75f);
                }
                yield return new WaitForSeconds(0.25f);
                pantherStateField.SetValue(__instance, UnitAI_PantherRogueKing.State.None);
                patternCoroutineField.SetValue(__instance, null);
                yield break;
            }

            [HarmonyPatch("CloneRandomDaggerPatternCoroutine")]
            [HarmonyPrefix]
            static bool CloneRandomDaggerPatternCoroutine_Prefix(UnitAI_PantherRogueKing __instance, ref IEnumerator __result)
            {
                if (Hard <= 0) return true;
                try
                {
                    __result = CloneRandomDaggerPatternCoroutine(__instance);
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[UnitAI_PantherRogueKing][CloneRandomDaggerPatternCoroutine]出现错误{ex}");
                    return true;
                }
            }

            static IEnumerator CloneRandomDaggerPatternCoroutine(UnitAI_PantherRogueKing __instance)
            {
                if (pantherRogueKingField == null || pantherEnvironmentField == null || patternCoroutineField == null || pantherStateField == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_PantherRogueKing][BasicAttackPatternCoroutine]获取反射失败");
                    yield break;
                }
                Unit_PantherRogueKing king = (Unit_PantherRogueKing)pantherRogueKingField.GetValue(__instance);
                BossEnvironment_Panther pantherEnvironment = (BossEnvironment_Panther)pantherEnvironmentField.GetValue(__instance);

                if (pantherEnvironment)
                {
                    Vector3 prevTargetPos = __instance.CurrentTarget.transform.position;
                    Vector2 vector = (Vector2)pantherEnvironment.transform.position + pantherEnvironment.cloneShurikenCloneAreaOffset + Random.insideUnitCircle.normalized * pantherEnvironment.cloneShurikenCloneAreaRadius;
                    PantherRogueKingShadow shadow = king.CreateShadow(vector);
                    Vector2 vector2 = (Vector2)pantherEnvironment.transform.position + pantherEnvironment.cloneShurikenCloneAreaOffset + Random.insideUnitCircle.normalized * pantherEnvironment.cloneShurikenCloneAreaRadius;
                    PantherRogueKingShadow shadow2 = king.CreateShadow(vector2);
                    yield return new WaitForSeconds(1f);

                    int Stack = 6;
                    float Wait = 0.5f;
                    if (Hard >= 3)
                    {
                        Stack = 8;
                        Wait = 0.3f;
                    }
                    for (int i = 0; i < Stack; i++)
                    {
                        if (__instance.CurrentTarget != null)
                        {
                            prevTargetPos = __instance.CurrentTarget.transform.position;
                        }
                        if (i % 3 == 0)
                            king.RandomDagger(prevTargetPos);
                        else if (i % 2 == 0)
                            shadow.RandomDagger(__instance.Avatar, prevTargetPos);
                        else
                            shadow2.RandomDagger(__instance.Avatar, prevTargetPos);
                        yield return new WaitForSeconds(Wait);
                    }
                    yield return new WaitForSeconds(1f);
                    king.DestroyShadow(shadow);
                    king.DestroyShadow(shadow2);
                }
                yield return new WaitForSeconds(1f);
                pantherStateField.SetValue(__instance, UnitAI_PantherRogueKing.State.None);
                patternCoroutineField.SetValue(__instance, null);
                yield break;
            }
        }

    }
}