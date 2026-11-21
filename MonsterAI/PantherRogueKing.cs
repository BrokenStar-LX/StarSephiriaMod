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

        private static readonly AccessTools.FieldRef<UnitAI_PantherRogueKing, Unit_PantherRogueKing>? KingRef;
        private static readonly AccessTools.FieldRef<UnitAI_PantherRogueKing, UnitAI_PantherRogueKing.State>? StateRef;
        private static readonly AccessTools.FieldRef<UnitAI_PantherRogueKing, List<int>>? PatternRoundRobbinRef;
        private static readonly AccessTools.FieldRef<UnitAI_PantherRogueKing, Coroutine?>? PatternCoroutineRef;
        private static readonly AccessTools.FieldRef<UnitAI_PantherRogueKing, BossEnvironment_Panther>? PantherEnvironmentRef;
        private static readonly AccessTools.FieldRef<UnitAI_PantherRogueKing, bool>? IsEndEnterCutSceneRef;
        private static readonly AccessTools.FieldRef<Unit_PantherRogueKing, List<CombatBehaviour>>? SharedCBRef;
        private static readonly Func<UnitAI_PantherRogueKing, IEnumerator>? RushPatternCoroutineDelegate;
        private static readonly Func<UnitAI_PantherRogueKing, IEnumerator>? CloneRandomDaggerPatternCoroutineDelegate;
        private static readonly Func<UnitAI_PantherRogueKing, bool, IEnumerator>? CloneDaggerPatternCoroutineDelegate;
        private static readonly Func<UnitAI_PantherRogueKing, IEnumerator>? CloneDaggerSpreadPatternCoroutineDelegate;
        private static readonly Func<UnitAI_PantherRogueKing, IEnumerator>? BasicAttackPatternCoroutineDelegate;
        private static readonly float[] StraightAngles = [0f, 90f, 180f, 270f];
        private static readonly float[] DiagonalAngles = [45f, 135f, 225f, 315f];

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
                KingRef = ReflectionCache.FieldRef<UnitAI_PantherRogueKing, Unit_PantherRogueKing>(type, "king");
                StateRef = ReflectionCache.FieldRef<UnitAI_PantherRogueKing, UnitAI_PantherRogueKing.State>(type, "state");
                PatternRoundRobbinRef = ReflectionCache.FieldRef<UnitAI_PantherRogueKing, List<int>>(type, "patternRoundRobbin");
                PatternCoroutineRef = ReflectionCache.FieldRef<UnitAI_PantherRogueKing, Coroutine?>(type, "pattern");
                PantherEnvironmentRef = ReflectionCache.FieldRef<UnitAI_PantherRogueKing, BossEnvironment_Panther>(type, "pantherEnvironment");
                IsEndEnterCutSceneRef = ReflectionCache.FieldRef<UnitAI_PantherRogueKing, bool>(type, "isEndEnterCutScene");
                RushPatternCoroutineDelegate = ReflectionCache.Delegate<Func<UnitAI_PantherRogueKing, IEnumerator>>(rushPatternCoroutineMethod, "RushPatternCoroutine");
                CloneRandomDaggerPatternCoroutineDelegate = ReflectionCache.Delegate<Func<UnitAI_PantherRogueKing, IEnumerator>>(cloneRandomDaggerPatternCoroutineMethod, "CloneRandomDaggerPatternCoroutine");
                CloneDaggerPatternCoroutineDelegate = ReflectionCache.Delegate<Func<UnitAI_PantherRogueKing, bool, IEnumerator>>(cloneDaggerPatternCoroutineMethod, "CloneDaggerPatternCoroutine");
                CloneDaggerSpreadPatternCoroutineDelegate = ReflectionCache.Delegate<Func<UnitAI_PantherRogueKing, IEnumerator>>(cloneDaggerSpreadPatternCoroutineMethod, "CloneDaggerSpreadPatternCoroutine");
                BasicAttackPatternCoroutineDelegate = ReflectionCache.Delegate<Func<UnitAI_PantherRogueKing, IEnumerator>>(basicAttackPatternCoroutineMethod, "BasicAttackPatternCoroutine");
                StarSephiriaMod.LogInfoBoss("[UnitAI_PantherRogueKing]初始化反射成功");

                // Unit_PantherRogueKing 反射
                type = typeof(Unit_PantherRogueKing);
                sharedCBField = AccessTools.Field(type, "sharedCB");
                SharedCBRef = ReflectionCache.FieldRef<Unit_PantherRogueKing, List<CombatBehaviour>>(type, "sharedCB");
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
                    List<CombatBehaviour> sharedCB = SharedCBRef!(__instance);
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
                        List<CombatBehaviour> sharedCB = SharedCBRef!(__instance);
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

                    king = KingRef!(__instance);
                    state = StateRef!(__instance);
                    isEndEnterCutScene = IsEndEnterCutSceneRef!(__instance);
                    if (king.patternState == Unit_PantherRogueKing.EPatternState.Fury && (__instance.CurrentTarget.transform.position - __instance.transform.position).magnitude < 4f)
                    {
                        king.FurySwing();
                    }

                    float AttackSpeed = 1.2f + 0.1f * Hard;
                    if (king.patternState == Unit_PantherRogueKing.EPatternState.Idle && state == UnitAI_PantherRogueKing.State.None
                        && __instance.majorPatternTimer.Update(Time.deltaTime * AttackSpeed))
                    {
                        List<int> patternRoundRobbin = PatternRoundRobbinRef!(__instance);
                        if (patternRoundRobbin.Count <= 0) RefillPatternRoundRobbin(patternRoundRobbin);

                        int UsePattern = patternRoundRobbin[0];
                        if (UsePattern != 4) Oldpattern = UsePattern;
                        StarSephiriaMod.LogInfoBoss($"耄耋当前技能：{UsePattern}");
                        LaunchPattern(__instance, king, UsePattern);
                        StateRef!(__instance) = state;
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

            /// <summary>下一轮技能池：4 固定排在最后，避免连续同招。</summary>
            private static void RefillPatternRoundRobbin(List<int> PatternList)
            {
                PatternList.Add(0);
                PatternList.Add(1);
                PatternList.Add(2);
                PatternList.Add(3);
                PatternList.Add(5);
                PatternList.Shuffle();
                PatternList.Add(4);
                if (Oldpattern >= 0)
                {
                    PatternList.Remove(Oldpattern);
                    PatternList.Insert(2, Oldpattern);
                }
            }

            private static void LaunchPattern(UnitAI_PantherRogueKing __instance, Unit_PantherRogueKing king, int UsePattern)
            {
                switch (UsePattern)
                {
                    case 1:
                        king.SlowShuriken();
                        __instance.majorPatternTimer.Ratio = 0.45f;
                        break;
                    case 2:
                        state = UnitAI_PantherRogueKing.State.Rush;
                        PatternCoroutineRef!(__instance) = __instance.StartCoroutine(RushPatternCoroutineDelegate!(__instance));
                        break;
                    case 3:
                        state = UnitAI_PantherRogueKing.State.CloneRandomDagger;
                        PatternCoroutineRef!(__instance) = __instance.StartCoroutine(CloneRandomDaggerPatternCoroutineDelegate!(__instance));
                        break;
                    case 4:
                        state = UnitAI_PantherRogueKing.State.CloneDagger;
                        PatternCoroutineRef!(__instance) = __instance.StartCoroutine(CloneDaggerPatternCoroutineDelegate!(__instance, false));
                        break;
                    case 5:
                        state = UnitAI_PantherRogueKing.State.CloneDaggerSpread;
                        PatternCoroutineRef!(__instance) = __instance.StartCoroutine(CloneDaggerSpreadPatternCoroutineDelegate!(__instance));
                        break;
                    default:
                        state = UnitAI_PantherRogueKing.State.BasicAttack;
                        PatternCoroutineRef!(__instance) = __instance.StartCoroutine(BasicAttackPatternCoroutineDelegate!(__instance));
                        __instance.majorPatternTimer.Ratio = 0.5f;
                        break;
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
                Unit_PantherRogueKing king = KingRef!(__instance);
                BossEnvironment_Panther pantherEnvironment = PantherEnvironmentRef!(__instance);
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
                    if (!isCutscene && Hard >= 2)
                    {
                        float Rand = 45f;
                        if (Hard >= 4) Rand = Random.Range(0f, 360f);

                        float RotR = 90f;
                        if (Hard >= 4) RotR = 72f;
                        Vector2 Pos = (Vector2)pantherEnvironment.transform.position + pantherEnvironment.cloneShurikenCloneAreaOffset;
                        shadow = king.CreateShadow(Pos + HorayUtility.GetVector2FromAngle(Rand) * pantherEnvironment.cloneShurikenCloneAreaRadius);
                        shadow2 = king.CreateShadow(Pos + HorayUtility.GetVector2FromAngle(Rand + RotR) * pantherEnvironment.cloneShurikenCloneAreaRadius);
                        shadow3 = king.CreateShadow(Pos + HorayUtility.GetVector2FromAngle(Rand + RotR * 2f) * pantherEnvironment.cloneShurikenCloneAreaRadius);
                        if (Hard >= 3) shadow4 = king.CreateShadow(Pos + HorayUtility.GetVector2FromAngle(Rand + RotR * 3f) * pantherEnvironment.cloneShurikenCloneAreaRadius);
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
                    yield return new WaitForSeconds(2f);
                    king.DestroyShadow(shadow);
                    king.DestroyShadow(shadow2);
                    king.DestroyShadow(shadow3);
                    if (shadow4 != null) king.DestroyShadow(shadow4);
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
                    SharedCBRef!(king) = new List<CombatBehaviour>();
                    king.FireDaggerSpreadAnimation();
                    yield return new WaitForSeconds(0.25f);
                }
                StateRef!(__instance) = UnitAI_PantherRogueKing.State.None;
                PatternCoroutineRef!(__instance) = null;
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

            /// <summary>
            /// 从 BossPos 沿 Dir 方向与圆（Center/Radius）求交，取较远的交点；方向背离圆时返回 false。
            /// </summary>
            private static bool TryGetCircleHit(Vector2 BossPos, Vector2 Center, float Radius, Vector2 Dir, out float Dist)
            {
                Vector2 Offset = BossPos - Center;
                float B = Vector2.Dot(Dir, Offset);
                float Disc = B * B - (Offset.sqrMagnitude - Radius * Radius);
                if (Disc < 0f)
                {
                    Dist = 0f;
                    return false;
                }
                Dist = -B + Mathf.Sqrt(Disc);
                return Dist > 0f;
            }

            static IEnumerator CloneDaggerSpreadPatternCoroutine(UnitAI_PantherRogueKing __instance)
            {
                if (pantherRogueKingField == null || pantherEnvironmentField == null || patternCoroutineField == null || pantherStateField == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_PantherRogueKing][CloneDaggerPatternCoroutine]获取反射失败");
                    yield break;
                }
                Unit_PantherRogueKing king = KingRef!(__instance);
                BossEnvironment_Panther pantherEnvironment = PantherEnvironmentRef!(__instance);
                Vector2 Center = (Vector2)pantherEnvironment.transform.position + pantherEnvironment.cloneShurikenCloneAreaOffset;
                Vector2 BossPos = (Vector2)__instance.transform.position;
                float Radius = pantherEnvironment.cloneShurikenCloneAreaRadius;
                List<(float Angle, float Dist)> Hits = [];

                // 第1个：本体四个直方向与圆的交点里，取距离本体最远的
                foreach (float A in StraightAngles)
                {
                    if (TryGetCircleHit(BossPos, Center, Radius, HorayUtility.GetVector2FromAngle(A), out float D))
                        Hits.Add((A, D));
                }
                Hits.Sort((X, Y) => Y.Dist.CompareTo(X.Dist));
                Vector2 vector = Hits.Count > 0
                    ? BossPos + HorayUtility.GetVector2FromAngle(Hits[0].Angle) * Hits[0].Dist
                    : Center + Random.insideUnitCircle.normalized * Radius;
                PantherRogueKingShadow shadow = king.CreateShadow(vector);

                // 第2个：本体四个斜方向与圆的交点里，去掉最远和最近，剩下的随机一个
                Hits.Clear();
                foreach (float A in DiagonalAngles)
                {
                    if (TryGetCircleHit(BossPos, Center, Radius, HorayUtility.GetVector2FromAngle(A), out float D))
                        Hits.Add((A, D));
                }
                Hits.Sort((X, Y) => Y.Dist.CompareTo(X.Dist));
                int Index = Hits.Count >= 3 ? Random.Range(1, Hits.Count - 1) : Hits.Count > 0 ? Random.Range(0, Hits.Count) : -1;
                vector = Index >= 0 ? BossPos + HorayUtility.GetVector2FromAngle(Hits[Index].Angle) * Hits[Index].Dist : Center + Random.insideUnitCircle.normalized * Radius;
                PantherRogueKingShadow shadow2 = king.CreateShadow(vector);

                yield return new WaitForSeconds(0.5f);
                king.DaggerSpread();
                yield return new WaitForSeconds(0.1f);
                shadow.DaggerSpread(__instance.Avatar);
                yield return new WaitForSeconds(0.1f);
                shadow2.DaggerSpread(__instance.Avatar);
                if (Hard >= 3)
                {
                    yield return new WaitForSeconds(1.25f);
                    if (__instance.CurrentTarget != null)
                    {
                        king.RandomDagger(__instance.CurrentTarget.transform.position);
                    }
                }
                StateRef!(__instance) = UnitAI_PantherRogueKing.State.None;
                PatternCoroutineRef!(__instance) = null;
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
                Unit_PantherRogueKing king = KingRef!(__instance);

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
                        SharedCBRef!(king) = new List<CombatBehaviour>();
                        king.FireDaggerSpreadAnimation();
                    }
                    else king.BasicAttack();
                    yield return new WaitForSeconds(0.75f);
                }
                yield return new WaitForSeconds(0.25f);
                StateRef!(__instance) = UnitAI_PantherRogueKing.State.None;
                PatternCoroutineRef!(__instance) = null;
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
                Unit_PantherRogueKing king = KingRef!(__instance);
                BossEnvironment_Panther pantherEnvironment = PantherEnvironmentRef!(__instance);

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
                StateRef!(__instance) = UnitAI_PantherRogueKing.State.None;
                PatternCoroutineRef!(__instance) = null;
                yield break;
            }
        }

    }
}