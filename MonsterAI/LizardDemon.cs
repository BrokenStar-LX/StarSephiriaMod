using HarmonyLib;
using Mirror;
using SPMod.Tool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace SPMod.MonsterAI
{
    [HarmonyPatch]
    public static class LizardDemon
    {
        public static int Hard
        {
            get { return ModConfig.HardMode; }
        }
        // UnitAI_LizardDemon 反射字段
        private static readonly FieldInfo? inAndOutCoroutineField;
        private static readonly FieldInfo? inAndOutTargetPositionField;
        private static readonly FieldInfo? dashCoroutineField;
        private static readonly MethodInfo? dashCoroutineMethod;
        private static readonly MethodInfo? dashToCenterCoroutineMethod;
        // Unit_LizardDemon 反射字段
        private static readonly FieldInfo? currentTwisterSwordSphereCountField;
        private static readonly FieldInfo? isRunningHeadSmashAnimationField;
        private static readonly FieldInfo? spawnedSwordSphereListField;
        private static readonly MethodInfo? rpcHeadSmashMethod;

        private static readonly AccessTools.FieldRef<UnitAI_LizardDemon, Coroutine?>? InAndOutCoroutineRef;
        private static readonly AccessTools.FieldRef<UnitAI_LizardDemon, Vector2>? InAndOutTargetPositionRef;
        private static readonly AccessTools.FieldRef<UnitAI_LizardDemon, Coroutine?>? DashCoroutineRef;
        private static readonly AccessTools.FieldRef<Unit_LizardDemon, int>? CurrentTwisterSwordSphereCountRef;
        private static readonly AccessTools.FieldRef<Unit_LizardDemon, bool>? IsRunningHeadSmashAnimationRef;
        private static readonly AccessTools.FieldRef<Unit_LizardDemon, List<SwordSphere>>? SpawnedSwordSphereListRef;
        private static readonly Func<UnitAI_LizardDemon, IEnumerator>? DashCoroutineDelegate;
        private static readonly Func<UnitAI_LizardDemon, IEnumerator>? DashToCenterCoroutineDelegate;
        private static readonly Action<Unit_LizardDemon, bool, Vector3>? RpcHeadSmashDelegate;
        //变种攻击判定
        private static bool MeleeAttack2 = false;
        private static bool TwisterAttack2 = false;
        private static readonly List<Vector3> TwisterPos = [];
        static LizardDemon()
        {
            try
            {
                MeleeAttack2 = Random.Range(0, 2) == 0;
                TwisterAttack2 = false;
                TwisterPos.Clear();

                // UnitAI_LizardDemon 反射
                Type type = typeof(UnitAI_LizardDemon);
                inAndOutCoroutineField = AccessTools.Field(type, "inAndOutCoroutine");
                inAndOutTargetPositionField = AccessTools.Field(type, "inAndOutTargetPosition");
                dashCoroutineField = AccessTools.Field(type, "dashCoroutine");
                dashCoroutineMethod = AccessTools.Method(type, "DashCoroutine");
                dashToCenterCoroutineMethod = AccessTools.Method(type, "DashToCenterCoroutine");
                InAndOutCoroutineRef = ReflectionCache.FieldRef<UnitAI_LizardDemon, Coroutine?>(type, "inAndOutCoroutine");
                InAndOutTargetPositionRef = ReflectionCache.FieldRef<UnitAI_LizardDemon, Vector2>(type, "inAndOutTargetPosition");
                DashCoroutineRef = ReflectionCache.FieldRef<UnitAI_LizardDemon, Coroutine?>(type, "dashCoroutine");
                DashCoroutineDelegate = ReflectionCache.Delegate<Func<UnitAI_LizardDemon, IEnumerator>>(dashCoroutineMethod, "DashCoroutine");
                DashToCenterCoroutineDelegate = ReflectionCache.Delegate<Func<UnitAI_LizardDemon, IEnumerator>>(dashToCenterCoroutineMethod, "DashToCenterCoroutine");
                StarSephiriaMod.LogInfoBoss("[UnitAI_LizardDemon]初始化反射");

                // Unit_LizardDemon 反射
                type = typeof(Unit_LizardDemon);
                currentTwisterSwordSphereCountField = AccessTools.Field(type, "currentTwisterSwordSphereCount");
                isRunningHeadSmashAnimationField = AccessTools.Field(type, "isRunningHeadSmashAnimation");
                spawnedSwordSphereListField = AccessTools.Field(type, "spawnedSwordSphereList");
                rpcHeadSmashMethod = AccessTools.Method(type, "RpcHeadSmash", [typeof(bool), typeof(Vector3)]);
                CurrentTwisterSwordSphereCountRef = ReflectionCache.FieldRef<Unit_LizardDemon, int>(type, "currentTwisterSwordSphereCount");
                IsRunningHeadSmashAnimationRef = ReflectionCache.FieldRef<Unit_LizardDemon, bool>(type, "isRunningHeadSmashAnimation");
                SpawnedSwordSphereListRef = ReflectionCache.FieldRef<Unit_LizardDemon, List<SwordSphere>>(type, "spawnedSwordSphereList");
                RpcHeadSmashDelegate = ReflectionCache.Delegate<Action<Unit_LizardDemon, bool, Vector3>>(rpcHeadSmashMethod, "RpcHeadSmash");
                StarSephiriaMod.LogInfoBoss("[Unit_LizardDemon]初始化反射");
            }
            catch (Exception ex)
            {
                StarSephiriaMod.LogError($"[LizardDemon]反射初始化失败: {ex}");
            }
        }

        [HarmonyPatch(typeof(Unit_LizardDemon))]
        public static class Unit_LizardDemon_Patch
        {
            private static float TSSTime = -1f;
            private static int SwordCount = -1;
            public static Timer ShootShockwave = new(1f);

            /// <summary>高难度额外加 10 点冲刺速度。</summary>
            private static float DashSpeed(float BaseSpeed) => BaseSpeed + (Hard >= 4 ? 10f : 0f);

            /// <summary>
            /// 基础攻击的冲刺速度。First = 第一套 FireData，否则第二套。
            /// 对应 Anim_BasicAttack_Fire / Anim_BasicAttack_FireSecond。
            /// </summary>
            private static float GetBasicAttackDashSpeed(bool First, bool DoubleAttack)
            {
                if (First) return DashSpeed(35f);
                if (Hard < 2) return 20f;
                if (!DoubleAttack) return DashSpeed(10f);
                return Hard >= 3 ? DashSpeed(25f) : 20f;
            }

            /// <summary>是否走第一套攻击数据的变体（Double + Hard3+ + MeleeAttack2）。</summary>
            private static bool UsesFirstAttackData(Unit_LizardDemon Demon)
                => Demon.currentAction == Unit_LizardDemon.EAnimation.BasicAttackDouble && Hard >= 3 && MeleeAttack2;

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            static void Update_Postfix(Unit_LizardDemon __instance)
            {
                if (TSSTime == -1f)
                {
                    TSSTime = __instance.twisterSwordSphereTimer.time;
                    ShootShockwave.time = __instance.inAndOutDistortionTimer.time;
                    SwordCount = __instance.twisterSwordSphereCount;
                }

                __instance.inAndOutSecondAttackDelayTimer.time = 0.5f - 0.18f / 3f * Hard;
                if (Hard >= 3)
                {
                    int PlayerStack = PlayerSpawner.MultiplayerList.Count;
                    __instance.twisterSwordSphereTimer.time = TSSTime * 0.25f + TSSTime * 0.5f * PlayerStack;
                    __instance.twisterSwordSphereCount = Math.Max(2, SwordCount + 3 - PlayerStack);
                }
                else if (Hard >= 1)
                {
                    __instance.twisterSwordSphereTimer.time = TSSTime * 0.75f;
                }
                else
                {
                    __instance.twisterSwordSphereTimer.time = TSSTime;
                    __instance.twisterSwordSphereCount = SwordCount;
                }

                if (Hard > 0 && __instance.isInAndOutAnimationRunning && ShootShockwave.Update(Time.deltaTime))
                {
                    if (Tools.GetPro("LizardDemonShockwave", out var Pro) && __instance.isServer)
                    {
                        float Rand = Random.Range(0f, 360f);
                        float Stack = 4f + Hard * 4f;
                        for (float i = 0f; i < 360f; i += 360f / Stack)
                        {
                            Vector2 Offset = HorayUtility.GetVector2FromAngle(i + Rand);
                            Vector3 Pos = __instance.transform.position + (Vector3)Offset * 2f;
                            Bullet.Pool.Spawn(Pro, __instance.transform.position, false, EDamageFromType.DirectAttack, "BasicAttack", __instance.RequestStandardDamage() * 0.75f, 0, 0f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0.3f, __instance.transform.position, Pos, null, null, 0f, EDamageElementalType.Physical, true);
                        }
                    }
                }
            }

            [HarmonyPatch("DashStart")]
            [HarmonyPostfix]
            static void DashStart_Postfix(Unit_LizardDemon __instance, Vector2 targetPosition)
            {
                if (Hard <= 0)
                {
                    __instance.dashAttackBulletTimer.time = 0.1f;
                    return;
                }

                float SpeedMult = 0.85f + 0.15f * Hard;
                if (Hard >= 2) __instance.dashAttackBulletTimer.time = 0.075f;
                __instance.NetworkdashVelocity = (targetPosition - (Vector2)__instance.transform.position).normalized * __instance.dashAttackSpeed * SpeedMult;
            }

            [HarmonyPatch("headSmashEndDashCoroutine")]
            [HarmonyPrefix]
            static bool HeadSmashEndDashCoroutine_Prefix(Unit_LizardDemon __instance, ref IEnumerator __result)
            {
                if (Hard < 0) return true;
                try
                {
                    __result = CustomHeadSmashEndDashCoroutine(__instance);
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[Unit_LizardDemon][StompCoroutine]出现错误{ex}");
                    return true;
                }
            }
            static IEnumerator CustomHeadSmashEndDashCoroutine(Unit_LizardDemon __instance)
            {
                if (rpcHeadSmashMethod == null || isRunningHeadSmashAnimationField == null || spawnedSwordSphereListField == null)
                {
                    StarSephiriaMod.LogError("[Unit_LizardDemon][HeadSmashEndDashCoroutine]获取反射失败");
                    yield break;
                }
                if (SpawnedSwordSphereListRef!(__instance) is not List<SwordSphere> spawnedSwordSphereList)
                {
                    StarSephiriaMod.LogError("[Unit_LizardDemon][headSmashEndDashCoroutine]反射类型错误");
                    yield break;
                }

                int attackCount = 3;
                __instance.NetworkheadSmashTargetPosition = __instance.CurrentLookingPosition;
                RpcHeadSmashDelegate!(__instance, __instance.currentAction == Unit_LizardDemon.EAnimation.HeadSmash_Double, (Vector3)__instance.headSmashTargetPosition);
                bool isRunningHeadSmashAnimation = IsRunningHeadSmashAnimationRef!(__instance);
                while (!isRunningHeadSmashAnimation)
                {
                    yield return null;
                    isRunningHeadSmashAnimation = IsRunningHeadSmashAnimationRef!(__instance);
                }
                while (isRunningHeadSmashAnimation)
                {
                    yield return null;
                    isRunningHeadSmashAnimation = IsRunningHeadSmashAnimationRef!(__instance);
                }
                while (attackCount > 0)
                {
                    __instance.DashStart(__instance.CurrentLookingPosition, false, true);
                    while (!__instance.isDashing)
                    {
                        yield return null;
                    }
                    while (__instance.isDashing)
                    {
                        yield return new WaitForSeconds(0.05f);
                        if(__instance.hideInvulnerable && __instance.isServer)
                        {
                            if (Tools.GetPro("LizardDemonShockwave", out var Pro))
                            {
                                if (Hard >= 4)
                                {
                                    Vector3 Offset = (__instance.CurrentLookingPosition - (Vector2)__instance.transform.position).normalized;
                                    Vector3 Pos = __instance.transform.position + Offset * 5f;
                                    Bullet.Pool.Spawn(Pro, __instance.transform.position, false, EDamageFromType.DirectAttack, "BasicAttack", __instance.RequestStandardDamage() * 0.75f, 0, 0f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0.3f, __instance.transform.position, Pos, null, null, 0f, EDamageElementalType.Physical, true);
                                }
                                else if (Hard >= 3)
                                {
                                    for (float i = -1f; i <= 1f; i += 2)
                                    {
                                        Vector2 Offset = HorayUtility.GetVector2FromAngle(HorayUtility.GetAngleFromVector(__instance.NetworkdashVelocity) + 90f * i);
                                        Vector3 Pos = __instance.transform.position + (Vector3)Offset * 2f;
                                        Bullet a = Bullet.Pool.Spawn(Pro, __instance.transform.position, false, EDamageFromType.DirectAttack, "BasicAttack", __instance.RequestStandardDamage() * 0.75f, 0, 0f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0.3f, __instance.transform.position, Pos, null, null, 0f, EDamageElementalType.Physical, true);
                                        a.speedScale *= 0.75f;
                                    }
                                }
                            }
                        }
                        yield return null;
                    }
                    __instance.ForceAimToPosition(__instance.AimedPosition);
                    yield return new WaitForSeconds(0.3f);
                    __instance.NetworkheadSmashTargetPosition = __instance.CurrentLookingPosition;
                    RpcHeadSmashDelegate!(__instance, __instance.currentAction == Unit_LizardDemon.EAnimation.HeadSmash_Double, (Vector3)__instance.headSmashTargetPosition);
                    attackCount--;
                    isRunningHeadSmashAnimation = IsRunningHeadSmashAnimationRef!(__instance);
                    bool falg = false;
                    while (!isRunningHeadSmashAnimation)
                    {
                        yield return null;
                        if (Tools.GetPro("LizardDemonShockwave", out var Pro) && Hard > 2 && !falg && __instance.isServer)
                        {
                            float Rand = Random.Range(0f, 360f);
                            float Stack = 4f * Hard;
                            for (float i = 0f; i < 360f; i += 360f / Stack)
                            {
                                Vector2 Offset = HorayUtility.GetVector2FromAngle(i + Rand);
                                Vector3 Pos = __instance.transform.position + (Vector3)Offset * 2f;
                                Bullet.Pool.Spawn(Pro, __instance.transform.position, false, EDamageFromType.DirectAttack, "BasicAttack", __instance.RequestStandardDamage() * 0.75f, 0, 0f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0.3f, __instance.transform.position, Pos, null, null, 0f, EDamageElementalType.Physical, true);
                            }
                            falg = true;
                        }
                        isRunningHeadSmashAnimation = IsRunningHeadSmashAnimationRef!(__instance);
                    }
                    while (isRunningHeadSmashAnimation)
                    {
                        yield return null;
                        isRunningHeadSmashAnimation = IsRunningHeadSmashAnimationRef!(__instance);
                    }
                }
                __instance.currentAction = Unit_LizardDemon.EAnimation.None;
                yield break;
            }

            [HarmonyPatch("Anim_BasicAttack_Fire")]
            [HarmonyPrefix]
            static bool Anim_BasicAttack_Fire_Prefix(Unit_LizardDemon __instance)
            {
                if (Hard < 2) return true;
                try
                {
                    if (UsesFirstAttackData(__instance)) return true;
                    __instance.Anim_BasicAttack_FireSecond();
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[Unit_Askard_Chapter2][Anim_BasicAttack_Fire]出现错误{ex}");
                    return true;
                }
            }

            [HarmonyPatch("Anim_BasicAttack_Warning")]
            [HarmonyPrefix]
            static bool Anim_BasicAttack_Warning_Prefix(Unit_LizardDemon __instance)
            {
                if (Hard < 2) return true;
                try
                {
                    if (UsesFirstAttackData(__instance))
                    {
                        __instance.basicAttackFireData.attackDashSpeed = GetBasicAttackDashSpeed(true, true);
                        return true;
                    }
                    __instance.Anim_BasicAttack_WarningSecond();
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[Unit_Askard_Chapter2][Anim_BasicAttack_Fire]出现错误{ex}");
                    return true;
                }
            }

            [HarmonyPatch("Anim_BasicAttack_FireSecond")]
            [HarmonyPrefix]
            static bool Anim_BasicAttack_FireSecond_Prefix(Unit_LizardDemon __instance)
            {
                bool DoubleAttack = __instance.currentAction == Unit_LizardDemon.EAnimation.BasicAttackDouble;
                if (Hard < 2)
                {
                    __instance.basicAttackSecondFireData.attackDashSpeed = GetBasicAttackDashSpeed(false, DoubleAttack);
                    return true;
                }
                try
                {
                    if (UsesFirstAttackData(__instance))
                    {
                        __instance.Anim_BasicAttack_Fire();
                        return false;
                    }
                    __instance.basicAttackSecondFireData.attackDashSpeed = GetBasicAttackDashSpeed(false, DoubleAttack);

                    Vector2 vector = __instance.CurrentLookingPosition - (Vector2)__instance.transform.position;
                    if (__instance.isServer)
                    {
                        __instance.basicAttackSecondFireData.CreateAttack(EDamageFromType.DirectAttack, __instance.RequestStandardDamage(), "BasicAttack", false, __instance, __instance.transform.position, __instance.CurrentLookingPosition, 0.5f, null, null, 1f, null, false, 0f, 1f, 0, null);
                    }
                    if (__instance.isClient)
                    {
                        if (GameCamera.Instance)
                        {
                            GameCamera.Instance.targetTracker.CreateCameraShaking(__instance.transform.position, EShakeCameraType.Impact, Vector2.up * 0.125f, 0.12f, 1f);
                        }
                        __instance.basicAttackSecondFireData.CreateSwingFx(false, __instance.transform, __instance.transform.position, new Vector3(0f, 0f, HorayUtility.GetAngleFromVector(vector)), 1f, -1, __instance.TopdownActor.CenterYPos);
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[Unit_Askard_Chapter2][Anim_BasicAttack_Fire]出现错误{ex}");
                    return true;
                }
            }

            [HarmonyPatch("Anim_BasicAttack_WarningSecond")]
            [HarmonyPrefix]
            static bool Anim_BasicAttack_WarningSecond_Prefix(Unit_LizardDemon __instance)
            {
                if (Hard < 3) return true;
                try
                {
                    if (UsesFirstAttackData(__instance))
                    {
                        __instance.Anim_BasicAttack_Warning();
                        return false;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[Unit_Askard_Chapter2][Anim_BasicAttack_Fire]出现错误{ex}");
                    return true;
                }
            }

            [HarmonyPatch("CreateSwordSphere")]
            [HarmonyPrefix]
            static bool CreateSwordSphere_Prefix(Unit_LizardDemon __instance, Vector3 targetPosition)
            {
                if (Hard <= 2) return true;
                try
                {
                    if (spawnedSwordSphereListField == null)
                    {
                        StarSephiriaMod.LogError("[Unit_LizardDemon][CreateSwordSphere]获取反射失败");
                        return true;
                    }

                    if (SpawnedSwordSphereListRef!(__instance) is not List<SwordSphere> spawnedSwordSphereList)
                    {
                        StarSephiriaMod.LogError("[Unit_LizardDemon][CreateSwordSphere]获取反射失败");
                        return true;
                    }

                    if (__instance.isServer)
                    {
                        if (TwisterAttack2 && currentTwisterSwordSphereCountField != null)
                        {
                            foreach(Vector3 Pos in TwisterPos)
                            {
                                GameObject gameObject = Object.Instantiate(__instance.swordSpherePrefab, __instance.transform.position, Quaternion.identity);
                                SwordSphere component = gameObject.GetComponent<SwordSphere>();
                                component.Initialize(__instance, Pos);
                                NetworkServer.Spawn(gameObject, ownerConnection: null);
                                spawnedSwordSphereList.Add(component);
                            }
                            TwisterPos.Clear();
                            TwisterAttack2 = false;
                            CurrentTwisterSwordSphereCountRef!(__instance) = 9;
                            return false;
                        }
                        foreach (PlayerSpawner PlayerSpawner in PlayerSpawner.MultiplayerList)
                        {
                            if (PlayerSpawner && PlayerSpawner.PlayerAvatar.hp > 0)
                            {
                                GameObject gameObject = Object.Instantiate(__instance.swordSpherePrefab, __instance.transform.position, Quaternion.identity);
                                SwordSphere component = gameObject.GetComponent<SwordSphere>();
                                component.Initialize(__instance, PlayerSpawner.transform.position);
                                NetworkServer.Spawn(gameObject, ownerConnection: null);
                                spawnedSwordSphereList.Add(component);
                            }
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[Unit_LizardDemon][CreateSwordSphere_Prefix]出现错误{ex}");
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(UnitAI_LizardDemon))]
        public static class UnitAI_LizardDemon_Patch
        {
            private static int Oldpattern = -1;
            [HarmonyPatch("OnAIUpdate_FoundEnemy")]
            [HarmonyPrefix]
            static bool OnAIUpdate_FoundEnemy_Prefix(UnitAI_LizardDemon __instance)
            {
                if (Hard <= 0) return true;
                try
                {
                    if (inAndOutCoroutineField == null || inAndOutTargetPositionField == null || dashToCenterCoroutineMethod == null || dashCoroutineField == null || dashCoroutineMethod == null)
                    {
                        StarSephiriaMod.LogError("[UnitAI_LizardDemon][OnAIUpdate_FoundEnemy]反射获取失败");
                        return true;
                    }

                    float Dis = Vector3.Distance(__instance.CurrentTarget.transform.position, __instance.transform.position);
                    __instance.unit.currentLookingLocalPosition_Server = (__instance.CurrentTarget.transform.position - __instance.transform.position).normalized * Dis;

                    Vector2 inAndOutTargetPosition = InAndOutTargetPositionRef!(__instance);
                    if (!__instance.unit.IsAppeared || __instance.unit.currentAction != Unit_LizardDemon.EAnimation.None) return false;
                    if (InAndOutCoroutineRef!(__instance) is Coroutine) return false;

                    if (__instance.inAndOutPatternDelayTimer.Update(Time.deltaTime) && __instance.patternTimer.Update(Time.deltaTime))
                    {
                        if (__instance.patternList.Count == 0)
                        {
                            RefillPatternList(__instance, ref inAndOutTargetPosition);
                        }

                        int pattern = __instance.patternList[0];
                        if (pattern != 1 && pattern != 6) Oldpattern = pattern;
                        __instance.patternList.RemoveAt(0);
                        StarSephiriaMod.LogInfoBoss($"蜥蜴当前攻击{pattern}");
                        LaunchPattern(__instance, pattern, inAndOutTargetPosition);
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[UnitAI_LizardDemon][OnAIUpdate_FoundEnemy]出现错误{ex}");
                    return true;
                }
            }

            /// <summary>下一轮技能池；环境存在时同时刷新 InAndOut 目标点。</summary>
            private static void RefillPatternList(UnitAI_LizardDemon __instance, ref Vector2 inAndOutTargetPosition)
            {
                MeleeAttack2 = !MeleeAttack2;
                __instance.patternList.Add(0);
                __instance.patternList.Add(2);
                __instance.patternList.Add(3);
                __instance.patternList.Add(4);
                __instance.patternList.Add(5);
                __instance.patternList.Shuffle();
                if (__instance.Environment && __instance.Environment is BossEnvironment_LizardDemon)
                {
                    if (Hard >= 2) __instance.patternList.Add(6);
                    __instance.patternList.Add(1);
                    __instance.patternList.Add(1);
                }
                if (Oldpattern >= 0)
                {
                    __instance.patternList.Remove(Oldpattern);
                    __instance.patternList.Insert(__instance.patternList.Count / 2, Oldpattern);
                }
                if (__instance.Environment)
                {
                    inAndOutTargetPosition = __instance.Environment.BossZoneCenter + (Vector3)Random.insideUnitCircle * 3f;
                    InAndOutTargetPositionRef!(__instance) = inAndOutTargetPosition;
                }
            }

            /// <summary>按编号释放技能。</summary>
            private static void LaunchPattern(UnitAI_LizardDemon __instance, int pattern, Vector2 inAndOutTargetPosition)
            {
                switch (pattern)
                {
                    case 0:
                        __instance.unit.HeadSmash();
                        __instance.patternTimer.Ratio = 0.5f;
                        break;
                    case 1:
                        SunkenAttack(__instance);
                        break;
                    case 2:
                        DashCoroutineRef!(__instance) = __instance.StartCoroutine(DashCoroutineDelegate!(__instance));
                        break;
                    case 3:
                        __instance.unit.Twister(__instance.CurrentTarget);
                        break;
                    case 4:
                        if ((__instance.transform.position - (Vector3)inAndOutTargetPosition).magnitude > __instance.dashToCenterDistance)
                        {
                            InAndOutCoroutineRef!(__instance) = __instance.StartCoroutine(DashToCenterCoroutineDelegate!(__instance));
                            __instance.inAndOutPatternDelayTimer.Ratio = 0f;
                        }
                        else if (__instance.Environment)
                        {
                            __instance.unit.InAndOut(__instance.Environment.BossZoneBottomLeft, __instance.Environment.BossZoneTopRight);
                            __instance.inAndOutPatternDelayTimer.Ratio = 0f;
                        }
                        Unit_LizardDemon_Patch.ShootShockwave.Ratio = 0f;
                        break;
                    case 6:
                        TwisterAttack(__instance);
                        break;
                    default:
                        __instance.unit.BasicAttack(true);
                        break;
                }
            }

            /// <summary>技能 1：根据玩家位置挑选硬直/沉没区域组合。</summary>
            private static void SunkenAttack(UnitAI_LizardDemon __instance)
            {
                if (__instance.Environment is not BossEnvironment_LizardDemon bossEnvironment_LizardDemon) return;
                Vector3 bossZoneBottomLeft = __instance.Environment.BossZoneBottomLeft;
                Vector3 bossZoneTopRight = __instance.Environment.BossZoneTopRight;
                Vector3 bossZoneCenter = __instance.Environment.BossZoneCenter;
                List<int> list = [];
                {
                    Vector2 vector = __instance.CurrentTarget.transform.position;
                    bool flag = vector.y > bossZoneCenter.y;
                    bool flag2 = vector.x > bossZoneCenter.x;

                    if (flag) list.Add(4);
                    else list.Add(5);

                    if (flag2) list.Add(2);
                    else list.Add(3);
                }
                list.Add(1);
                list.Add(0);

                int V = list[Random.Range(0, list.Count)];
                List<BoxCollider2D> list2 = [];
                if (V > 1) list2.AddRange(bossEnvironment_LizardDemon.areaAttackPatterns_Hard[V - 2].GetComponentsInChildren<BoxCollider2D>());
                else list2.AddRange(bossEnvironment_LizardDemon.areaAttackPatterns[V].GetComponentsInChildren<BoxCollider2D>());
                list.Remove(V);

                if (Hard >= 3)
                {
                    V = list[Random.Range(0, list.Count)];
                    if (V > 1) list2.AddRange(bossEnvironment_LizardDemon.areaAttackPatterns_Hard[V - 2].GetComponentsInChildren<BoxCollider2D>());
                    else list2.AddRange(bossEnvironment_LizardDemon.areaAttackPatterns[V].GetComponentsInChildren<BoxCollider2D>());
                }
                int num3 = -1;
                if (V == 0) num3 = 0;
                else if (V == 1) num3 = 1;
                __instance.unit.Sunken(bossZoneBottomLeft, bossZoneTopRight, num3, list2.ToArray(), __instance.CurrentTarget);
                if (Hard >= 2) __instance.patternTimer.Ratio = 0.5f;
            }

            /// <summary>技能 6：多点龙卷风。</summary>
            private static void TwisterAttack(UnitAI_LizardDemon __instance)
            {
                if (__instance.Environment is not BossEnvironment_LizardDemon) return;
                TwisterAttack2 = true;
                Vector3 ZoneBottomLeft = __instance.Environment.BossZoneBottomLeft;
                Vector3 ZoneTopRight = __instance.Environment.BossZoneTopRight;
                Vector3 ZoneCenter = __instance.Environment.BossZoneCenter;
                int Count = Hard * 3;
                for (int i = 0; i < Count; i++)
                {
                    TwisterPos.Add(GetRandomPositionInBossZone(ZoneCenter, ZoneBottomLeft, ZoneTopRight));
                }
                __instance.unit.Twister(__instance.CurrentTarget);
            }

            public static Vector3 GetRandomPositionInBossZone(Vector3 center, Vector3 bottomLeft, Vector3 topRight)
            {
                float randomX = Random.Range(bottomLeft.x, topRight.x);
                float randomY = Random.Range(bottomLeft.y, topRight.y);
                float offsetX = randomX - center.x;
                float offsetY = randomY - center.y;

                offsetX *= 0.9f;
                offsetY *= 0.8f;
                return center + new Vector3(offsetX, offsetY, 0);
            }

            [HarmonyPatch("DashCoroutine")]
            [HarmonyPrefix]
            static bool DashCoroutine_Prefix(UnitAI_LizardDemon __instance, ref IEnumerator __result)
            {
                if (Hard < 0) return true;
                try
                {
                    __result = DashCoroutine(__instance);
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[UnitAI_LizardDemon][DashCoroutine]出现错误{ex}");
                    return true;
                }
            }
            static IEnumerator DashCoroutine(UnitAI_LizardDemon __instance)
            {
                if (dashCoroutineField == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_LizardDemon][OnAIUpdate_FoundEnemy]反射获取失败");
                    yield break;
                }

                int dash = 3;
                for (int i = 0; i < dash; i++)
                {
                    if (!__instance.CurrentTarget)
                    {
                        break;
                    }

                    bool falg = false;
                    __instance.unit.Dash(__instance.CurrentTarget.transform.position, true);
                    while (__instance.unit.currentAction != 0)
                    {
                        if (__instance.unit.dashTimer.Ratio > 0.85f && !falg)
                        {
                            if (Hard >= 3)
                                __instance.unit.Anim_BasicAttack_WarningSecond();
                            else
                                __instance.unit.Anim_BasicAttack_Warning();
                            falg = true;
                        }
                        yield return null;
                    }
                    if (Hard >= 3)
                        __instance.unit.Anim_BasicAttack_FireSecond();
                    else
                        __instance.unit.Anim_BasicAttack_Fire();
                    yield return new WaitForSeconds(0.1f);
                }
                DashCoroutineRef!(__instance) = null;
            }
        }
    }
}