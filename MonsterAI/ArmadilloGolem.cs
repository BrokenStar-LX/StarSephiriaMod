using HarmonyLib;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static SPMod.StarSephiriaPatches;
using Random = UnityEngine.Random;

namespace SPMod.MonsterAI
{
    [HarmonyPatch]
    public static class ArmadilloGolem
    {
        public static int Hard
        {
            get { return ModConfig.HardMode; }
        }
        // UnitAI_ArmadilloGolem 反射字段
        private static readonly FieldInfo? missileLaunchField;
        private static readonly FieldInfo? golemField;
        private static readonly FieldInfo? handAreaBottomLeftField;
        private static readonly FieldInfo? handAreaTopRightField;
        private static readonly FieldInfo? randomPatternForRoundRobinField;
        private static readonly FieldInfo? handPatternTimerField;
        // Unit_ArmadilloGolem 反射字段
        private static readonly PropertyInfo? handStateProperty;
        private static readonly FieldInfo? missilesField;
        private static readonly FieldInfo? missileFxZoneDirField;
        private static readonly FieldInfo? spreadBulletCounterField;
        private static readonly FieldInfo? subshotFiringField;
        private static readonly FieldInfo? subshotFireCounterField;
        private static readonly FieldInfo? subshotFireTargetField;
        private static readonly FieldInfo? subshotFirePositionField;
        private static readonly FieldInfo? subshotStopRequestField;
        private static readonly FieldInfo? handsField;
        private static readonly FieldInfo? laserMaxFlipField;
        private static readonly FieldInfo? laserMinYField;
        private static readonly FieldInfo? laserMaxYField;
        private static readonly FieldInfo? handLaserFiredField;
        private static readonly FieldInfo? handLaserMoveDirField;
        private static readonly MethodInfo? updateTotemStateMethod;
        private static readonly MethodInfo? createTotemMethod;
        private static readonly Type? missileLaunchDataType;
        private static readonly FieldInfo? targetPositionField;
        private static readonly MethodInfo? launchMissileWarningMethod;
        private static readonly MethodInfo? launchMissileFxMethod;
        private static readonly MethodInfo? launchMissileMethod;
        private static readonly FieldInfo? headStateField;
        // UnitAvatar 反射字段
        private static readonly FieldInfo? isHitInvincibleEnabledField;
        private static readonly FieldInfo? invincibleTimerField;
        private static readonly FieldInfo? isReviveInvulnerableField;
        private static readonly FieldInfo? isTempInvulnerableField;
        private static readonly FieldInfo? tempInvulnerableTimerField;
        private static readonly FieldInfo? stunCooldownTimerField;
        static ArmadilloGolem()
        {
            try
            {
                // UnitAvatar 反射
                Type type = typeof(UnitAvatar);
                isHitInvincibleEnabledField = AccessTools.Field(type, "isHitInvincibleEnabled");
                invincibleTimerField = AccessTools.Field(type, "invincibleTimer");
                isReviveInvulnerableField = AccessTools.Field(type, "isReviveInvulnerable");
                isTempInvulnerableField = AccessTools.Field(type, "isTempInvulnerable");
                tempInvulnerableTimerField = AccessTools.Field(type, "tempInvulnerableTimer");
                stunCooldownTimerField = AccessTools.Field(type, "stunCooldownTimer");
                StarSephiriaMod.LogInfoBoss("[UnitAvatar]初始化反射");

                // UnitAI_ArmadilloGolem 反射
                type = typeof(UnitAI_ArmadilloGolem);
                missileLaunchField = AccessTools.Field(type, "missileLaunch");
                golemField = AccessTools.Field(type, "golem");
                handAreaBottomLeftField = AccessTools.Field(type, "handAreaBottomLeft");
                handAreaTopRightField = AccessTools.Field(type, "handAreaTopRight");
                randomPatternForRoundRobinField = AccessTools.Field(type, "randomPatternForRoundRobin");
                handPatternTimerField = AccessTools.Field(type, "handPatternTimer");
                StarSephiriaMod.LogInfoBoss("[UnitAI_ArmadilloGolem]初始化反射完成");

                // Unit_ArmadilloGolem 反射
                type = typeof(Unit_ArmadilloGolem);
                handStateProperty = AccessTools.Property(type, "HandState");
                missilesField = AccessTools.Field(type, "missiles");
                missileFxZoneDirField = AccessTools.Field(type, "missileFxZoneDir");
                spreadBulletCounterField = AccessTools.Field(type, "spreadBulletCounter");
                subshotFiringField = AccessTools.Field(type, "subshotFiring");
                subshotFireCounterField = AccessTools.Field(type, "subshotFireCounter");
                subshotFireTargetField = AccessTools.Field(type, "subshotFireTarget");
                subshotFirePositionField = AccessTools.Field(type, "subshotFirePosition");
                subshotStopRequestField = AccessTools.Field(type, "subshotStopRequest");
                handsField = AccessTools.Field(type, "hands");
                laserMaxFlipField = AccessTools.Field(type, "laserMaxFlip");
                laserMinYField = AccessTools.Field(type, "laserMinY");
                laserMaxYField = AccessTools.Field(type, "laserMaxY");
                handLaserFiredField = AccessTools.Field(type, "handLaserFired");
                handLaserMoveDirField = AccessTools.Field(type, "handLaserMoveDir");
                updateTotemStateMethod = AccessTools.Method(type, "UpdateTotemState");
                createTotemMethod = AccessTools.Method(type, "CreateTotem");
                headStateField = AccessTools.Field(typeof(Unit_ArmadilloGolem), "headState");
                missileLaunchDataType = AccessTools.FirstInner(type, t => t.Name.Contains("MissileLaunchData"));
                if (missileLaunchDataType != null)
                {
                    targetPositionField = AccessTools.Field(missileLaunchDataType, "targetPosition");
                    launchMissileWarningMethod = AccessTools.Method(missileLaunchDataType, "LaunchMissileWarning");
                    launchMissileFxMethod = AccessTools.Method(missileLaunchDataType, "LaunchMissileFx");
                    launchMissileMethod = AccessTools.Method(missileLaunchDataType, "LaunchMissile");
                    StarSephiriaMod.LogInfoBoss("[Unit_ArmadilloGolem] MissileLaunchData反射初始化成功");
                }
                else
                {
                    StarSephiriaMod.LogError("[Unit_ArmadilloGolem] 无法找到MissileLaunchData类型");
                }
                StarSephiriaMod.LogInfoBoss("[Unit_ArmadilloGolem]初始化反射");
            }
            catch (Exception ex)
            {
                StarSephiriaMod.LogError($"[LibraryGuard]反射初始化失败: {ex}");
            }
        }
        private static readonly Timer StunTimer = new Timer(1f);
        private static readonly Timer HitRestoreTimer = new Timer(0.75f);
        public static void InstanceUpdate(UnitAvatar __instance)
        {
            // 原有的击晕逻辑
            if (!__instance.isServer) return;

            if (__instance.isInStun && StunTimer.Update(Time.deltaTime))
            {
                __instance.StopStun();
            }
            // 原有的击中卡滞逻辑
            if (__instance.hitStuck && !__instance.IsDead)
            {
                float TimeMult;
                if (__instance.currentHitStuckLevel == 1)
                {
                    TimeMult = 1f;
                }
                else if (__instance.TopdownRigidbody == null || __instance.TopdownRigidbody.IsGrounded)
                {
                    TimeMult = 0.8f;
                }
                else
                {
                    TimeMult = 0f;
                }

                if (!__instance.isInStun && HitRestoreTimer.Update(Time.deltaTime * TimeMult))
                {
                    __instance.StopHitFeedback();
                }
            }
            if (isHitInvincibleEnabledField == null || invincibleTimerField == null || isReviveInvulnerableField == null || isTempInvulnerableField == null || tempInvulnerableTimerField == null || stunCooldownTimerField == null)
            {
                StarSephiriaMod.LogError("[UnitAvatar][InstanceUpdate]获取反射失败");
                return;
            }
            // 通过反射获取私有字段的值
            bool isHitInvincibleEnabled = (bool)isHitInvincibleEnabledField.GetValue(__instance);
            float invincibleTimer = (float)invincibleTimerField.GetValue(__instance);
            bool isReviveInvulnerable = (bool)isReviveInvulnerableField.GetValue(__instance);
            bool isTempInvulnerable = (bool)isTempInvulnerableField.GetValue(__instance);
            Timer tempInvulnerableTimer = (Timer)tempInvulnerableTimerField.GetValue(__instance);
            Timer stunCooldownTimer = (Timer)stunCooldownTimerField.GetValue(__instance);
            // 处理无敌状态
            if (isHitInvincibleEnabled)
            {
                invincibleTimer += Time.deltaTime;
                if (invincibleTimer >= __instance.invincibleTime)
                {
                    invincibleTimer = 0f;
                    isHitInvincibleEnabledField.SetValue(__instance, false);
                }
                invincibleTimerField.SetValue(__instance, invincibleTimer);
            }
            // 处理复活无敌
            if (isReviveInvulnerable && __instance.reviveInvulnerableTimer.Update(Time.deltaTime))
            {
                isReviveInvulnerableField.SetValue(__instance, false);
            }
            // 处理临时无敌
            if (isTempInvulnerable && tempInvulnerableTimer.Update(Time.deltaTime))
            {
                isTempInvulnerableField.SetValue(__instance, false);
            }
            // 处理击晕免疫
            if (__instance.stunCooldown && stunCooldownTimer.Update(Time.deltaTime))
            {
                __instance.stunCooldown = false;
            }
        }

        [HarmonyPatch(typeof(UnitAI_MadArmadillo))]
        public static class UnitAI_MadArmadillo_Patch
        {
            [HarmonyPatch("get_Life")]
            [HarmonyPrefix]
            static bool GetLife_Prefix(ref int __result)
            {
                if (Hard <= 0) return true;

                __result = 0;
                return false;
            }
        }

        [HarmonyPatch(typeof(Unit_ArmadilloGolem_Hand))]
        public static class Unit_ArmadilloGolem_Hand_Patch
        {
            [HarmonyPatch("SetMove")]
            [HarmonyPrefix]
            static void SetMove_Prefix(Unit_ArmadilloGolem_Hand __instance, Vector3 targetPosition, ref float moveMultiplier)
            {
                if (Hard >= 2) moveMultiplier *= 0.75f + Hard * 0.25f;
            }

            [HarmonyPatch("StompDown")]
            [HarmonyPostfix]
            static void StompDown_Postfix(Unit_ArmadilloGolem_Hand __instance)
            {
                if (Unit_ArmadilloGolem_Patch.Golem != null && UnitAI_ArmadilloGolem_Patch.GolemAI != null && Hard >= 2)
                {
                    Unit_ArmadilloGolem Head = Unit_ArmadilloGolem_Patch.Golem;
                    UnitAI_ArmadilloGolem HeadAI = UnitAI_ArmadilloGolem_Patch.GolemAI;
                    List<Vector3> missilePositions = new List<Vector3> { __instance.transform.position };
                    if (Hard >= 3)
                    {
                        if (UnitAI_ArmadilloGolem_Patch.Stomp == 1 || Hard >= 4)
                        {
                            Vector3 Vec = Vector3.zero;
                            int Stack = 3;
                            if (Hard >= 4)
                            {
                                Stack = 4;
                                Vec = (HeadAI.CurrentTarget.transform.position - __instance.transform.position).normalized * 4f;
                                Vec.y *= 0.5f;
                            }
                            for (int i = 1; i <= Stack; i++)
                                missilePositions.Add(__instance.transform.position + Vec * i);
                        }
                    }
                    for (int i = 0; i < missilePositions.Count; i++)
                    {
                        float delay = HeadAI.missileWarning + HeadAI.missileFireInterval * i * 1.25f;
                        Head.LaunchMissile(missilePositions[i] + (Vector3)Random.insideUnitCircle * HeadAI.missileError,
                            delay, delay - HeadAI.missileWarning, HeadAI.missileWarning);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Unit_ArmadilloGolem))]
        public static class Unit_ArmadilloGolem_Patch
        {
            public static Unit_ArmadilloGolem? Golem;
            private static float LaseSpeed = 0f;
            [HarmonyPatch("Update")]
            [HarmonyPrefix]
            static bool Update_Prefix(Unit_ArmadilloGolem __instance)
            {
                if (Hard <= 0) return true;
                try
                {
                    if (handStateProperty == null || missilesField == null || missileFxZoneDirField == null || spreadBulletCounterField == null || subshotFiringField == null || subshotFireCounterField == null || subshotFireTargetField == null || subshotFirePositionField == null || subshotStopRequestField == null || handsField == null || laserMaxFlipField == null || laserMinYField == null || laserMaxYField == null || handLaserFiredField == null || handLaserMoveDirField == null || updateTotemStateMethod == null || createTotemMethod == null || targetPositionField == null || launchMissileWarningMethod == null || launchMissileFxMethod == null || launchMissileMethod == null || headStateField == null)
                    {
                        StarSephiriaMod.LogError("[Unit_ArmadilloGolem][Update]获取反射失败");
                        return true;
                    }

                    InstanceUpdate(__instance);
                    UnitAvatar_Patches.Update_Postfix(__instance);
                    Golem = __instance;

                    IList missiles = (IList)missilesField.GetValue(__instance);
                    int missileFxZoneDir = (int)missileFxZoneDirField.GetValue(__instance);
                    int spreadBulletCounter = (int)spreadBulletCounterField.GetValue(__instance);
                    bool subshotFiring = (bool)subshotFiringField.GetValue(__instance);
                    int subshotFireCounter = (int)subshotFireCounterField.GetValue(__instance);
                    Transform subshotFireTarget = (Transform)subshotFireTargetField.GetValue(__instance);
                    Vector3 subshotFirePosition = (Vector3)subshotFirePositionField.GetValue(__instance);
                    bool subshotStopRequest = (bool)subshotStopRequestField.GetValue(__instance);
                    Unit_ArmadilloGolem_Hand[] hands = (Unit_ArmadilloGolem_Hand[])handsField.GetValue(__instance);
                    bool laserMaxFlip = (bool)laserMaxFlipField.GetValue(__instance);
                    float laserMinY = (float)laserMinYField.GetValue(__instance);
                    float laserMaxY = (float)laserMaxYField.GetValue(__instance);
                    bool[] handLaserFired = (bool[])handLaserFiredField.GetValue(__instance);
                    int[] handLaserMoveDir = (int[])handLaserMoveDirField.GetValue(__instance);
                    Unit_ArmadilloGolem.EHeadState headState = (Unit_ArmadilloGolem.EHeadState)headStateField.GetValue(__instance);
                    if (__instance.isServer)
                    {
                        // 导弹攻击
                        if (missiles != null)
                        {
                            for (int i = missiles.Count - 1; i >= 0; i--)
                            {
                                var missile = missiles[i];
                                bool warningResult = (bool)launchMissileWarningMethod.Invoke(missile, new object[] { Time.deltaTime });
                                if (warningResult)
                                {
                                    Vector3 targetPos = (Vector3)targetPositionField.GetValue(missile);
                                    __instance.RpcCreateLaunchMissileeWarning(targetPos);
                                }
                                bool fxResult = (bool)launchMissileFxMethod.Invoke(missile, new object[] { Time.deltaTime });
                                if (fxResult)
                                {
                                    Vector2 vector = missileFxZoneDir == 0 ? Vector2.Lerp(__instance.missileFxZone_LB, new Vector2(__instance.missileFxZone_LB.x, __instance.missileFxZone_RT.y), Random.value) : Vector2.Lerp(new Vector2(__instance.missileFxZone_RT.x, __instance.missileFxZone_LB.y), __instance.missileFxZone_RT, Random.value);
                                    __instance.RpcCreateLaunchMissileFx(vector + Random.insideUnitCircle * Random.Range(0f, 1f));
                                }
                                bool launchResult = (bool)launchMissileMethod.Invoke(missile, new object[] { Time.deltaTime });
                                if (launchResult)
                                {
                                    Vector3 targetPos = (Vector3)targetPositionField.GetValue(missile);
                                    Bullet.Pool.Spawn(__instance.missileBulletPrefab, targetPos, false, EDamageFromType.DirectAttack, "BasicAttack", __instance.RequestStandardDamage(), 25, 2f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack), 20f, targetPos, targetPos, null, null, 0f, EDamageElementalType.Physical, true);
                                    missiles.RemoveAt(i);
                                }
                            }
                            missilesField.SetValue(__instance, missiles);
                        }
                        // 散弹攻击
                        if (headState == Unit_ArmadilloGolem.EHeadState.SpreadBullet_Loop)
                        {
                            if (__instance.spreadBulletTimer.Update(Time.deltaTime))
                            {
                                spreadBulletCounter++;
                                spreadBulletCounterField.SetValue(__instance, spreadBulletCounter);

                                __instance.RpcCreateBulletFireFx();
                                int Stack = __instance.spreadBulletCount;
                                if (Hard >= 4)
                                {
                                    float Rot = 180f + 90f * Mathf.Sin(spreadBulletCounter * Mathf.PI / 30f);
                                    Vector2 Offset = HorayUtility.GetVector2FromAngle(Rot);
                                    Vector3 Pos = __instance.transform.position + (Vector3)Offset * 2f;
                                    Bullet.Pool.Spawn(__instance.subshotBulletPrefab, __instance.spreadBulletFirePosition.position, false, EDamageFromType.DirectAttack, "BasicAttack", __instance.RequestStandardDamage() * 0.5f, 0, 0f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack),
                                        0.3f, __instance.transform.position, Pos, null, null, 0f, EDamageElementalType.Physical, true);
                                    if (spreadBulletCounter % 20 == 0)
                                    {
                                        foreach (PlayerSpawner playerSpawner2 in PlayerSpawner.MultiplayerList)
                                        {
                                            if (playerSpawner2)
                                            {
                                                Bullet.Pool.Spawn(__instance.subshotBulletPrefab, __instance.spreadBulletFirePosition.position, false, EDamageFromType.DirectAttack, "BasicAttack", __instance.RequestStandardDamage() * 0.5f, 0, 0f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack),
                                                0.3f, __instance.transform.position, playerSpawner2.transform.position, null, null, 0f, EDamageElementalType.Physical, true);
                                            }
                                        }
                                    }
                                    if (spreadBulletCounter % 40 == 0)
                                    {
                                        for (float i = 90f; i <= 270f; i += 5f)
                                        {
                                            Offset = HorayUtility.GetVector2FromAngle(i);
                                            Pos = __instance.transform.position + (Vector3)Offset * 2f;
                                            Bullet.Pool.Spawn(__instance.subshotBulletPrefab, __instance.spreadBulletFirePosition.position, false, EDamageFromType.DirectAttack, "BasicAttack", __instance.RequestStandardDamage() * 0.5f, 0, 0f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack),
                                                0.3f, __instance.transform.position, Pos, null, null, 0f, EDamageElementalType.Physical, true);
                                        }
                                    }
                                    Stack = 120;
                                }
                                else if (Hard >= 2)
                                {
                                    int Type = UnitAI_ArmadilloGolem_Patch.SpreadBulletType[0];
                                    switch (Type)
                                    {
                                        case 0:
                                            {
                                                for (float i = 90f; i <= 270f; i += 5f)
                                                {
                                                    Vector2 Offset = HorayUtility.GetVector2FromAngle(i);
                                                    Vector3 Pos = __instance.transform.position + (Vector3)Offset * 2f;
                                                    Bullet.Pool.Spawn(__instance.subshotBulletPrefab, __instance.spreadBulletFirePosition.position, false, EDamageFromType.DirectAttack, "BasicAttack", __instance.RequestStandardDamage() * 0.5f, 0, 0f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack),
                                                        0.3f, __instance.transform.position, Pos, null, null, 0f, EDamageElementalType.Physical, true);
                                                }
                                            }
                                            break;
                                        case 1:
                                            {

                                                float Rot = 180f + 90f * Mathf.Sin(spreadBulletCounter * Mathf.PI / 30f);
                                                Vector2 Offset = HorayUtility.GetVector2FromAngle(Rot);
                                                Vector3 Pos = __instance.transform.position + (Vector3)Offset * 2f;
                                                Bullet.Pool.Spawn(__instance.subshotBulletPrefab, __instance.spreadBulletFirePosition.position, false, EDamageFromType.DirectAttack, "BasicAttack", __instance.RequestStandardDamage() * 0.5f, 0, 0f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack),
                                                    0.3f, __instance.transform.position, Pos, null, null, 0f, EDamageElementalType.Physical, true);
                                                if (spreadBulletCounter % 18 == 0)
                                                {
                                                    foreach (PlayerSpawner playerSpawner2 in PlayerSpawner.MultiplayerList)
                                                    {
                                                        if (playerSpawner2)
                                                        {
                                                            Bullet.Pool.Spawn(__instance.subshotBulletPrefab, __instance.spreadBulletFirePosition.position, false, EDamageFromType.DirectAttack, "BasicAttack", __instance.RequestStandardDamage() * 0.5f, 0, 0f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack),
                                                            0.3f, __instance.transform.position, playerSpawner2.transform.position, null, null, 0f, EDamageElementalType.Physical, true);
                                                        }
                                                    }
                                                }
                                                Stack = 180;
                                            }
                                            break;
                                        case 2:
                                            {
                                                float Rot = 180f + 45f * Mathf.Sin(spreadBulletCounter * Mathf.PI / 30f);
                                                for (int i = -1; i <= 1; i += 1)
                                                {
                                                    Vector2 Offset = HorayUtility.GetVector2FromAngle(Rot + i * 75f);
                                                    Vector3 Pos = __instance.transform.position + (Vector3)Offset * 2f;
                                                    Bullet a = Bullet.Pool.Spawn(__instance.subshotBulletPrefab, __instance.spreadBulletFirePosition.position, false, EDamageFromType.DirectAttack, "BasicAttack", __instance.RequestStandardDamage() * 0.5f, 0, 0f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack),
                                                        0.3f, __instance.transform.position, Pos, null, null, 0f, EDamageElementalType.Physical, true);
                                                    a.speedScale *= 0.8f;
                                                }
                                                Stack = 120;
                                            }
                                            break;
                                    }
                                }
                                else
                                {
                                    for (float i = 90f; i <= 270f; i += 12f)
                                    {
                                        Vector2 vector2FromAngle = HorayUtility.GetVector2FromAngle(i);
                                        Vector3 vector2 = __instance.transform.position + (Vector3)vector2FromAngle * 2f;
                                        Bullet.Pool.Spawn(__instance.spreadBulletPrefab, __instance.spreadBulletFirePosition.position, false, EDamageFromType.DirectAttack, "BasicAttack",
                                            __instance.RequestStandardDamage() * 0.5f, 0, 0f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack),
                                            0.3f, __instance.transform.position, vector2, null, null, 0f, EDamageElementalType.Physical, true);
                                    }
                                }
                                if (spreadBulletCounter >= Stack)
                                {
                                    if(Hard >= 2)
                                        UnitAI_ArmadilloGolem_Patch.SpreadBulletType.RemoveAt(0);
                                    __instance.NetworkheadState = Unit_ArmadilloGolem.EHeadState.SpreadBullet_End;
                                }
                            }
                        }
                        // 射击攻击
                        else if (headState == Unit_ArmadilloGolem.EHeadState.Subshot)
                        {
                            if (!subshotFiring)
                            {
                                if (__instance.subShotTimer.Update(Time.deltaTime))
                                {
                                    subshotFiring = true;
                                    subshotFiringField.SetValue(__instance, true);
                                    int Stack = 0;
                                    if (Hard >= 2)
                                    {
                                        Stack += 3;
                                        float TimeCd = Hard >= 4 ? 0.1f : Hard >= 3 ? 0.2f : 0.5f;
                                        __instance.subShotTimer.time = TimeCd;
                                    }
                                    else
                                    {
                                        __instance.subShotTimer.time = 2f;
                                    }
                                    subshotFireCounter = Stack;
                                    subshotFireCounterField.SetValue(__instance, Stack);
                                    if (subshotFireTarget != null)
                                    {
                                        subshotFirePosition = subshotFireTarget.position;
                                        subshotFirePositionField.SetValue(__instance, subshotFirePosition);
                                    }
                                }
                                else if (subshotStopRequest)
                                {
                                    subshotStopRequest = false;
                                    subshotStopRequestField.SetValue(__instance, false);
                                    __instance.NetworkheadState = Unit_ArmadilloGolem.EHeadState.Idle;
                                }
                            }
                            else if (__instance.subShotIntervalTimer.Update(Time.deltaTime))
                            {
                                __instance.RpcCreateBulletFireFx();
                                Bullet.Pool.Spawn(__instance.subshotBulletPrefab, __instance.spreadBulletFirePosition.position, false, EDamageFromType.DirectAttack, "BasicAttack",
                                    __instance.RequestStandardDamage() * 0.5f, 0, 0f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0.3f, __instance.spreadBulletFirePosition.position, subshotFirePosition, null, null, 0f, EDamageElementalType.Physical, true);
                                subshotFireCounter += 1;
                                subshotFireCounterField.SetValue(__instance, subshotFireCounter);
                                if (subshotFireCounter >= __instance.subShotFireCount)
                                {
                                    subshotFiring = false;
                                    subshotFiringField.SetValue(__instance, false);
                                    if (subshotStopRequest)
                                    {
                                        subshotStopRequest = false;
                                        subshotStopRequestField.SetValue(__instance, false);
                                        __instance.NetworkheadState = Unit_ArmadilloGolem.EHeadState.Idle;
                                    }
                                }
                            }
                        }
                        // 手部状态
                        Unit_ArmadilloGolem.EHandState HandState = (Unit_ArmadilloGolem.EHandState)handStateProperty.GetValue(__instance);
                        // 激光准备
                        if (HandState == Unit_ArmadilloGolem.EHandState.LaserReady)
                        {
                            bool flag = true;
                            for (int k = 0; k < hands.Length; k++)
                            {
                                if (!(hands[k] == null))
                                {
                                    Vector3 ToPos;
                                    if (hands[k].handSide == Unit_ArmadilloGolem_Hand.EHandSide.Left)
                                    {
                                        if (laserMaxFlip)
                                        {
                                            ToPos = new Vector3(__instance.laserLeftXPosition, laserMinY, 0f);
                                        }
                                        else
                                        {
                                            ToPos = new Vector3(__instance.laserLeftXPosition, laserMaxY, 0f);
                                        }
                                    }
                                    else if (laserMaxFlip)
                                    {
                                        ToPos = new Vector3(__instance.laserRightXPosition, laserMaxY, 0f);
                                    }
                                    else
                                    {
                                        ToPos = new Vector3(__instance.laserRightXPosition, laserMinY, 0f);
                                    }
                                    Vector3 ToPosVec = ToPos - hands[k].transform.position;
                                    hands[k].SetMove(ToPos, 1f);
                                    if (ToPosVec.sqrMagnitude > 0.8f || handLaserFired[k])
                                    {
                                        flag = false;
                                    }
                                }
                            }
                            if (flag)
                            {
                                for (int l = 0; l < hands.Length; l++)
                                {
                                    handLaserFired[l] = true;
                                    hands[l].LaserFire();
                                    hands[l].StopMove();
                                }
                                handLaserFiredField.SetValue(__instance, handLaserFired);
                                handStateProperty.SetValue(__instance, Unit_ArmadilloGolem.EHandState.LaserFire);
                                LaseSpeed = Random.Range(0.75f, 1.25f);
                                return false;
                            }
                        }
                        // 激光发射
                        else if (HandState == Unit_ArmadilloGolem.EHandState.LaserFire)
                        {
                            for (int m = 0; m < hands.Length; m++)
                            {
                                if (!(hands[m] == null))
                                {
                                    Vector3 position = hands[m].transform.position;
                                    if (hands[m].handSide == Unit_ArmadilloGolem_Hand.EHandSide.Left)
                                    {
                                        float Speed = __instance.laseSpeed_left;
                                        Speed *= LaseSpeed;
                                        if (Hard >= 3) Speed *= 0.6f + 0.2f * Hard;
                                        position.y += Speed * handLaserMoveDir[m] * Time.deltaTime;
                                    }
                                    else
                                    {
                                        float Speed = __instance.laseSpeed_right;
                                        Speed *= 2f - LaseSpeed;
                                        if (Hard >= 3) Speed *= 0.9f + 0.1f * Hard;
                                        position.y += Speed * handLaserMoveDir[m] * Time.deltaTime;
                                    }
                                    hands[m].transform.position = position;
                                    if (handLaserMoveDir[m] == 1 && position.y > laserMaxY)
                                    {
                                        handLaserMoveDir[m] *= -1;
                                    }
                                    else if (handLaserMoveDir[m] == -1 && position.y < laserMinY)
                                    {
                                        handLaserMoveDir[m] *= -1;
                                    }
                                }
                            }
                            handLaserMoveDirField.SetValue(__instance, handLaserMoveDir);
                            if (__instance.laserFireTimer.Update(Time.deltaTime))
                            {
                                __instance.HeadPattern_StopSubshot();
                                handStateProperty.SetValue(__instance, Unit_ArmadilloGolem.EHandState.LaserEnd);
                                Unit_ArmadilloGolem_Hand[] array = hands;
                                for (int j = 0; j < array.Length; j++)
                                {
                                    array[j].LaserEnd();
                                }
                                return false;
                            }
                        }
                        // 激光结束
                        else if (HandState == Unit_ArmadilloGolem.EHandState.LaserEnd)
                        {
                            foreach (Unit_ArmadilloGolem_Hand unit_ArmadilloGolem_Hand in hands)
                            {
                                Vector3 vector5;
                                if (unit_ArmadilloGolem_Hand.handSide == Unit_ArmadilloGolem_Hand.EHandSide.Left)
                                {
                                    vector5 = new Vector3(__instance.laserLeftXPosition + 2f, (laserMinY + laserMaxY) / 2f, 0f);
                                }
                                else
                                {
                                    vector5 = new Vector3(__instance.laserRightXPosition - 2f, (laserMinY + laserMaxY) / 2f, 0f);
                                }
                                unit_ArmadilloGolem_Hand.SetMove(vector5, 1f);
                            }
                            if (__instance.laserEndTimer.Update(Time.deltaTime))
                            {
                                handStateProperty.SetValue(__instance, Unit_ArmadilloGolem.EHandState.Idle);
                                updateTotemStateMethod.Invoke(__instance, null);
                                createTotemMethod.Invoke(__instance, null);
                            }
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[Unit_ArmadilloGolem][Update]出现错误{ex}");
                    return true;
                }
            }

            [HarmonyPatch("LoopSpreadBulletAnimation")]
            [HarmonyPostfix]
            static void LoopSpreadBulletAnimation_Postfix(Unit_ArmadilloGolem __instance)
            {
                if (Hard <= 0)
                {
                    __instance.spreadBulletTimer.time = 0.66f;
                }
                else if (Hard >= 4)
                {
                    __instance.spreadBulletTimer.time = 0.03f;
                }
                else
                {
                    int Type = UnitAI_ArmadilloGolem_Patch.SpreadBulletType[0];
                    switch (Type)
                    {
                        case 0:
                            __instance.spreadBulletTimer.time = 0.99f;
                            break;
                        case 1:
                            __instance.spreadBulletTimer.time = 0.02f;
                            break;
                        case 2:
                            __instance.spreadBulletTimer.time = 0.04f;
                            break;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(UnitAI_ArmadilloGolem))]
        public static class UnitAI_ArmadilloGolem_Patch
        {
            private static int Oldpattern = -1;
            public static List<int> SpreadBulletType = new List<int>() { };
            public static int Stomp = 0;
            public static UnitAI_ArmadilloGolem? GolemAI;

            [HarmonyPatch("OnAIUpdate_FoundEnemy")]
            [HarmonyPrefix]
            static bool OnAIUpdate_FoundEnemy_Prefix(UnitAI_ArmadilloGolem __instance)
            {
                if (Hard <= 0) return true;
                try
                {
                    if (missileLaunchField == null || golemField == null || handAreaBottomLeftField == null || handAreaTopRightField == null || randomPatternForRoundRobinField == null || handPatternTimerField == null)
                    {
                        StarSephiriaMod.LogError("[UnitAI_ArmadilloGolem][Update]获取反射失败");
                        return true;
                    }

                    GolemAI = __instance;
                    // 通过反射获取私有字段
                    bool missileLaunch = (bool)missileLaunchField.GetValue(__instance);
                    Unit_ArmadilloGolem golem = (Unit_ArmadilloGolem)golemField.GetValue(__instance);
                    Vector2 handAreaBottomLeft = (Vector2)handAreaBottomLeftField.GetValue(__instance);
                    Vector2 handAreaTopRight = (Vector2)handAreaTopRightField.GetValue(__instance);
                    List<int> randomPatternForRoundRobin = (List<int>)randomPatternForRoundRobinField.GetValue(__instance);
                    Timer handPatternTimer = (Timer)handPatternTimerField.GetValue(__instance);
                    if (!missileLaunch)
                    {
                        float TimeMult = 0.5f;
                        int PlayerStack = NetworkServer.connections.Count;
                        TimeMult += 1f / PlayerStack;
                        if (Hard >= 2) TimeMult *= 1.5f;
                        if (__instance.missileTimer.Update(Time.deltaTime * TimeMult))
                        {
                            missileLaunchField.SetValue(__instance, true);
                            golem.SetMissileZoneDir(Random.Range(0, 2));
                            golem.PlayMissileStartSound();
                        }
                    }
                    else
                    {
                        List<Vector3> missilePositions = new List<Vector3>() { };
                        foreach (PlayerSpawner playerSpawner2 in PlayerSpawner.MultiplayerList)
                        {
                            if (playerSpawner2)
                            {
                                Vector3 playerPosition = playerSpawner2.transform.position;
                                missilePositions.Add(playerPosition);
                                if (Hard >= 2)
                                {
                                    missilePositions.Add(playerPosition + new Vector3(4f, 0f, 0f));
                                    missilePositions.Add(playerPosition + new Vector3(-4f, 0f, 0f));
                                    missilePositions.Add(playerPosition + new Vector3(0f, 2f, 0f));
                                    missilePositions.Add(playerPosition + new Vector3(0f, -2f, 0f));
                                }
                            }
                        }
                        for (int i = 0; i < missilePositions.Count; i++)
                        {
                            float delay = __instance.missileWarning + __instance.missileFireInterval * i;
                            golem.LaunchMissile(missilePositions[i] + (Vector3)Random.insideUnitCircle * __instance.missileError,
                                delay, delay - __instance.missileWarning, __instance.missileWarning);
                        }
                        missileLaunchField.SetValue(__instance, false);
                    }

                    if (golem.HeadState == Unit_ArmadilloGolem.EHeadState.Idle &&
                        golem.HandState == Unit_ArmadilloGolem.EHandState.Idle &&
                        handPatternTimer.Update(Time.deltaTime))
                    {
                        if (randomPatternForRoundRobin.Count <= 1)
                        {
                            List<int> random = new List<int> { 0, 1, 2, 3 };
                            if (Oldpattern != -1) random.Remove(Oldpattern);
                            random.Shuffle();
                            if (Oldpattern != -1) random.Add(Oldpattern);

                            if (Hard >= 3) random.Remove(2);

                            randomPatternForRoundRobin.Clear();
                            while (random.Count > 0)
                            {
                                randomPatternForRoundRobin.Add(random[0]);
                                if (Hard >= 3) randomPatternForRoundRobin.Add(2);
                                random.RemoveAt(0);
                            }
                            randomPatternForRoundRobin.Add(2);
                        }
                        switch (randomPatternForRoundRobin[0])
                        {
                            case 1:
                                Stomp = 0;
                                golem.HeadPattern_Subshot(__instance.CurrentTarget.transform);
                                golem.HandPattern_Laser(handAreaBottomLeft.x, handAreaTopRight.x, handAreaBottomLeft.y - 0.75f, handAreaTopRight.y - 1f);
                                break;
                            case 2:
                                if (Hard >= 2 && SpreadBulletType.Count <= 0)
                                {
                                    SpreadBulletType = new List<int> { 0, 1, 2 };
                                    SpreadBulletType.Shuffle();
                                }
                                Stomp = 0;
                                golem.HeadPattern_SpreadBullet();
                                break;
                            case 3:
                                Stomp = 1;
                                golem.HandPattern_ShockwaveSmash(__instance.CurrentTarget.transform);
                                break;
                            default:
                                Stomp = 1;
                                golem.HandPattern_Stomp(4, __instance.CurrentTarget.transform);
                                break;
                        }
                        if (Hard >= 3 && randomPatternForRoundRobin[0] != 2 || Hard < 3)
                            Oldpattern = randomPatternForRoundRobin[0];
                        StarSephiriaMod.LogInfoBoss($"牛头当前攻击：{randomPatternForRoundRobin[0]}");
                        randomPatternForRoundRobin.RemoveAt(0);
                        randomPatternForRoundRobinField.SetValue(__instance, randomPatternForRoundRobin);
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[UnitAI_ArmadilloGolem][OnAIUpdate_FoundEnemy]出现错误: {ex}");
                    return true;
                }
            }

            [HarmonyPatch("Awake")]
            [HarmonyPostfix]
            static void Awake_Postfix(UnitAI_ArmadilloGolem __instance)
            {
                if (Hard <= 0) return;
                try
                {
                    if (randomPatternForRoundRobinField == null)
                    {
                        StarSephiriaMod.LogError("[UnitAI_ArmadilloGolem][Awake]获取反射失败");
                        return;
                    }

                    List<int> randomPatternForRoundRobin = (List<int>)randomPatternForRoundRobinField.GetValue(__instance);
                    randomPatternForRoundRobin.Clear();
                    randomPatternForRoundRobinField.SetValue(__instance, randomPatternForRoundRobin);
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[UnitAI_ArmadilloGolem][Awake]出现错误: {ex}");
                }
            }
        }
    }
}