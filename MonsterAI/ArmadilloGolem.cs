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
        public static int Hard => ModConfig.HardMode;

        // ===== 反射缓存（FieldRef / MethodInvoker，避免每帧 GetValue/SetValue 的装箱与反射开销） =====
        // UnitAI_ArmadilloGolem
        private static readonly AccessTools.FieldRef<UnitAI_ArmadilloGolem, bool>? MissileLaunchRef;
        private static readonly AccessTools.FieldRef<UnitAI_ArmadilloGolem, Unit_ArmadilloGolem>? GolemRef;
        private static readonly AccessTools.FieldRef<UnitAI_ArmadilloGolem, Vector2>? HandAreaBottomLeftRef;
        private static readonly AccessTools.FieldRef<UnitAI_ArmadilloGolem, Vector2>? HandAreaTopRightRef;
        private static readonly AccessTools.FieldRef<UnitAI_ArmadilloGolem, List<int>>? RandomPatternForRoundRobinRef;
        private static readonly AccessTools.FieldRef<UnitAI_ArmadilloGolem, Timer>? HandPatternTimerRef;

        // Unit_ArmadilloGolem
        private static readonly AccessTools.FieldRef<Unit_ArmadilloGolem, Unit_ArmadilloGolem.EHeadState>? HeadStateRef;
        private static readonly Func<Unit_ArmadilloGolem, Unit_ArmadilloGolem.EHandState>? HandStateGetter;
        private static readonly Action<Unit_ArmadilloGolem, Unit_ArmadilloGolem.EHandState>? HandStateSetter;
        private static readonly AccessTools.FieldRef<Unit_ArmadilloGolem, int>? MissileFxZoneDirRef;
        private static readonly AccessTools.FieldRef<Unit_ArmadilloGolem, int>? SpreadBulletCounterRef;
        private static readonly AccessTools.FieldRef<Unit_ArmadilloGolem, bool>? SubshotFiringRef;
        private static readonly AccessTools.FieldRef<Unit_ArmadilloGolem, int>? SubshotFireCounterRef;
        private static readonly AccessTools.FieldRef<Unit_ArmadilloGolem, Transform>? SubshotFireTargetRef;
        private static readonly AccessTools.FieldRef<Unit_ArmadilloGolem, Vector3>? SubshotFirePositionRef;
        private static readonly AccessTools.FieldRef<Unit_ArmadilloGolem, bool>? SubshotStopRequestRef;
        private static readonly AccessTools.FieldRef<Unit_ArmadilloGolem, Unit_ArmadilloGolem_Hand[]>? HandsRef;
        private static readonly AccessTools.FieldRef<Unit_ArmadilloGolem, bool>? LaserMaxFlipRef;
        private static readonly AccessTools.FieldRef<Unit_ArmadilloGolem, float>? LaserMinYRef;
        private static readonly AccessTools.FieldRef<Unit_ArmadilloGolem, float>? LaserMaxYRef;
        private static readonly AccessTools.FieldRef<Unit_ArmadilloGolem, bool[]>? HandLaserFiredRef;
        private static readonly AccessTools.FieldRef<Unit_ArmadilloGolem, int[]>? HandLaserMoveDirRef;
        private static readonly MethodInfo? UpdateTotemStateMethod;
        private static readonly MethodInfo? CreateTotemMethod;

        // MissileLaunchData 为内部私有类型，无法用强类型 FieldRef，保留反射 + 复用参数数组
        private static readonly FieldInfo? MissilesField;
        private static readonly FieldInfo? TargetPositionField;
        private static readonly FastInvokeHandler? LaunchMissileWarningInvoker;
        private static readonly FastInvokeHandler? LaunchMissileFxInvoker;
        private static readonly FastInvokeHandler? LaunchMissileInvoker;
        private static readonly object[] MissileArgs = new object[1];

        private static readonly bool GolemUpdateReady;
        private static readonly bool GolemAIReady;

        static ArmadilloGolem()
        {
            try
            {
                // UnitAI_ArmadilloGolem 反射
                Type AIType = typeof(UnitAI_ArmadilloGolem);
                MissileLaunchRef = ReflectionCache.FieldRef<UnitAI_ArmadilloGolem, bool>(AIType, "missileLaunch");
                GolemRef = ReflectionCache.FieldRef<UnitAI_ArmadilloGolem, Unit_ArmadilloGolem>(AIType, "golem");
                HandAreaBottomLeftRef = ReflectionCache.FieldRef<UnitAI_ArmadilloGolem, Vector2>(AIType, "handAreaBottomLeft");
                HandAreaTopRightRef = ReflectionCache.FieldRef<UnitAI_ArmadilloGolem, Vector2>(AIType, "handAreaTopRight");
                RandomPatternForRoundRobinRef = ReflectionCache.FieldRef<UnitAI_ArmadilloGolem, List<int>>(AIType, "randomPatternForRoundRobin");
                HandPatternTimerRef = ReflectionCache.FieldRef<UnitAI_ArmadilloGolem, Timer>(AIType, "handPatternTimer");
                GolemAIReady = MissileLaunchRef != null && GolemRef != null && HandAreaBottomLeftRef != null
                    && HandAreaTopRightRef != null && RandomPatternForRoundRobinRef != null && HandPatternTimerRef != null;
                StarSephiriaMod.LogInfoBoss("[UnitAI_ArmadilloGolem]初始化反射完成");

                // Unit_ArmadilloGolem 反射
                Type GolemType = typeof(Unit_ArmadilloGolem);
                HeadStateRef = ReflectionCache.FieldRef<Unit_ArmadilloGolem, Unit_ArmadilloGolem.EHeadState>(GolemType, "headState");
                MissileFxZoneDirRef = ReflectionCache.FieldRef<Unit_ArmadilloGolem, int>(GolemType, "missileFxZoneDir");
                SpreadBulletCounterRef = ReflectionCache.FieldRef<Unit_ArmadilloGolem, int>(GolemType, "spreadBulletCounter");
                SubshotFiringRef = ReflectionCache.FieldRef<Unit_ArmadilloGolem, bool>(GolemType, "subshotFiring");
                SubshotFireCounterRef = ReflectionCache.FieldRef<Unit_ArmadilloGolem, int>(GolemType, "subshotFireCounter");
                SubshotFireTargetRef = ReflectionCache.FieldRef<Unit_ArmadilloGolem, Transform>(GolemType, "subshotFireTarget");
                SubshotFirePositionRef = ReflectionCache.FieldRef<Unit_ArmadilloGolem, Vector3>(GolemType, "subshotFirePosition");
                SubshotStopRequestRef = ReflectionCache.FieldRef<Unit_ArmadilloGolem, bool>(GolemType, "subshotStopRequest");
                HandsRef = ReflectionCache.FieldRef<Unit_ArmadilloGolem, Unit_ArmadilloGolem_Hand[]>(GolemType, "hands");
                LaserMaxFlipRef = ReflectionCache.FieldRef<Unit_ArmadilloGolem, bool>(GolemType, "laserMaxFlip");
                LaserMinYRef = ReflectionCache.FieldRef<Unit_ArmadilloGolem, float>(GolemType, "laserMinY");
                LaserMaxYRef = ReflectionCache.FieldRef<Unit_ArmadilloGolem, float>(GolemType, "laserMaxY");
                HandLaserFiredRef = ReflectionCache.FieldRef<Unit_ArmadilloGolem, bool[]>(GolemType, "handLaserFired");
                HandLaserMoveDirRef = ReflectionCache.FieldRef<Unit_ArmadilloGolem, int[]>(GolemType, "handLaserMoveDir");
                UpdateTotemStateMethod = AccessTools.Method(GolemType, "UpdateTotemState");
                CreateTotemMethod = AccessTools.Method(GolemType, "CreateTotem");

                try
                {
                    PropertyInfo? HandStateProperty = AccessTools.Property(GolemType, "HandState");
                    MethodInfo? HandStateGet = HandStateProperty?.GetGetMethod(true);
                    MethodInfo? HandStateSet = HandStateProperty?.GetSetMethod(true);
                    HandStateGetter = HandStateGet == null ? null
                        : AccessTools.MethodDelegate<Func<Unit_ArmadilloGolem, Unit_ArmadilloGolem.EHandState>>(HandStateGet);
                    HandStateSetter = HandStateSet == null ? null
                        : AccessTools.MethodDelegate<Action<Unit_ArmadilloGolem, Unit_ArmadilloGolem.EHandState>>(HandStateSet);
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[Unit_ArmadilloGolem] HandState委托创建失败: {ex}");
                }

                MissilesField = AccessTools.Field(GolemType, "missiles");
                Type? MissileLaunchDataType = AccessTools.FirstInner(GolemType, T => T.Name.Contains("MissileLaunchData"));
                if (MissileLaunchDataType != null)
                {
                    TargetPositionField = AccessTools.Field(MissileLaunchDataType, "targetPosition");
                    MethodInfo? WarningMethod = AccessTools.Method(MissileLaunchDataType, "LaunchMissileWarning");
                    MethodInfo? FxMethod = AccessTools.Method(MissileLaunchDataType, "LaunchMissileFx");
                    MethodInfo? LaunchMethod = AccessTools.Method(MissileLaunchDataType, "LaunchMissile");
                    LaunchMissileWarningInvoker = WarningMethod == null ? null : MethodInvoker.GetHandler(WarningMethod);
                    LaunchMissileFxInvoker = FxMethod == null ? null : MethodInvoker.GetHandler(FxMethod);
                    LaunchMissileInvoker = LaunchMethod == null ? null : MethodInvoker.GetHandler(LaunchMethod);
                    if (TargetPositionField != null && LaunchMissileWarningInvoker != null && LaunchMissileFxInvoker != null && LaunchMissileInvoker != null)
                    {
                        StarSephiriaMod.LogInfoBoss("[Unit_ArmadilloGolem] MissileLaunchData反射初始化成功");
                    }
                    else
                    {
                        StarSephiriaMod.LogError("[Unit_ArmadilloGolem] MissileLaunchData反射初始化失败");
                    }
                }
                else
                {
                    StarSephiriaMod.LogError("[Unit_ArmadilloGolem] 无法找到MissileLaunchData类型");
                }

                GolemUpdateReady = HeadStateRef != null && HandStateGetter != null && HandStateSetter != null
                    && MissileFxZoneDirRef != null && SpreadBulletCounterRef != null && SubshotFiringRef != null
                    && SubshotFireCounterRef != null && SubshotFireTargetRef != null && SubshotFirePositionRef != null
                    && SubshotStopRequestRef != null && HandsRef != null && LaserMaxFlipRef != null && LaserMinYRef != null
                    && LaserMaxYRef != null && HandLaserFiredRef != null && HandLaserMoveDirRef != null
                    && UpdateTotemStateMethod != null && CreateTotemMethod != null && MissilesField != null
                    && TargetPositionField != null && LaunchMissileWarningInvoker != null && LaunchMissileFxInvoker != null
                    && LaunchMissileInvoker != null;
                StarSephiriaMod.LogInfoBoss("[Unit_ArmadilloGolem]初始化反射");
            }
            catch (Exception ex)
            {
                StarSephiriaMod.LogError($"[LibraryGuard]反射初始化失败: {ex}");
            }
        }

        public static void InstanceUpdate(UnitAvatar __instance)
        {
            BossCommon.UpdateUnitAvatarTimers(__instance);
        }

        private static Bullet SpawnBullet(Unit_ArmadilloGolem Golem, GameObject Prefab, Vector3 FirePos, float Damage, long Layers,
            Vector3 Origin, Vector3 TargetPos, float SpeedScale = 1f)
        {
            Bullet Spawned = Bullet.Pool.Spawn(Prefab, FirePos, false, EDamageFromType.DirectAttack, "BasicAttack", Damage, 0, 0f,
                Golem, Layers, 0.3f, Origin, TargetPos, null, null);
            if (SpeedScale != 1f) Spawned.speedScale *= SpeedScale;
            return Spawned;
        }

        private static void SpawnFan(Unit_ArmadilloGolem Golem, GameObject Prefab, Vector3 FirePos, float Damage, long Layers,
            Vector3 Origin, float Step)
        {
            for (float Angle = 90f; Angle <= 270f; Angle += Step)
            {
                Vector2 Offset = HorayUtility.GetVector2FromAngle(Angle);
                Vector3 Pos = Origin + (Vector3)Offset * 2f;
                SpawnBullet(Golem, Prefab, FirePos, Damage, Layers, Origin, Pos);
            }
        }

        private static void ServerUpdate(Unit_ArmadilloGolem __instance)
        {
            // 导弹攻击
            IList? Missiles = (IList?)MissilesField!.GetValue(__instance);
            if (Missiles != null)
            {
                int MissileFxZoneDir = MissileFxZoneDirRef!(__instance);
                for (int i = Missiles.Count - 1; i >= 0; i--)
                {
                    object Missile = Missiles[i]!;

                    MissileArgs[0] = Time.deltaTime;
                    if ((bool)LaunchMissileWarningInvoker!.Invoke(Missile, MissileArgs)!)
                    {
                        Vector3 TargetPos = (Vector3)TargetPositionField!.GetValue(Missile)!;
                        __instance.RpcCreateLaunchMissileeWarning(TargetPos);
                    }

                    MissileArgs[0] = Time.deltaTime;
                    if ((bool)LaunchMissileFxInvoker!.Invoke(Missile, MissileArgs)!)
                    {
                        Vector2 Vector = MissileFxZoneDir == 0
                            ? Vector2.Lerp(__instance.missileFxZone_LB, new Vector2(__instance.missileFxZone_LB.x, __instance.missileFxZone_RT.y), Random.value)
                            : Vector2.Lerp(new Vector2(__instance.missileFxZone_RT.x, __instance.missileFxZone_LB.y), __instance.missileFxZone_RT, Random.value);
                        __instance.RpcCreateLaunchMissileFx(Vector + Random.insideUnitCircle * Random.Range(0f, 1f));
                    }

                    MissileArgs[0] = Time.deltaTime;
                    if ((bool)LaunchMissileInvoker!.Invoke(Missile, MissileArgs)!)
                    {
                        Vector3 TargetPos = (Vector3)TargetPositionField!.GetValue(Missile)!;
                        Bullet.Pool.Spawn(__instance.missileBulletPrefab, TargetPos, false, EDamageFromType.DirectAttack, "BasicAttack",
                            __instance.RequestStandardDamage(), 25, 2f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack),
                            20f, TargetPos, TargetPos, null, null, 0f, EDamageElementalType.Physical, true);
                        Missiles.RemoveAt(i);
                    }
                }
            }
            // 散弹攻击
            Unit_ArmadilloGolem.EHeadState HeadState = HeadStateRef!(__instance);
            if (HeadState == Unit_ArmadilloGolem.EHeadState.SpreadBullet_Loop)
            {
                if (__instance.spreadBulletTimer.Update(Time.deltaTime))
                {
                    int Counter = ++SpreadBulletCounterRef!(__instance);
                    __instance.RpcCreateBulletFireFx();

                    int Stack = __instance.spreadBulletCount;
                    Vector3 Origin = __instance.transform.position;
                    Vector3 FirePos = __instance.spreadBulletFirePosition.position;
                    float BulletDamage = __instance.RequestStandardDamage() * 0.5f;
                    long Layers = __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack);
                    GameObject Prefab = __instance.subshotBulletPrefab;

                    if (Hard >= 4)
                    {
                        int Count = 0;
                        foreach (PlayerSpawner PlayerSpawner in PlayerSpawner.MultiplayerList)
                        {
                            if (!PlayerSpawner) continue;
                            if (Count <= 0)
                            {
                                for (int i = -1; i <= 1; i += 2)
                                {
                                    float Rot = HorayUtility.GetAngleFromVector(PlayerSpawner.transform.position - Origin);
                                    Vector2 Offset = HorayUtility.GetVector2FromAngle(Rot + i * 60f);
                                    Vector3 Pos = Origin + (Vector3)Offset * 2f;
                                    SpawnBullet(__instance, Prefab, FirePos, BulletDamage, Layers, Origin, Pos, 0.8f);
                                }
                                Count += 1;
                            }
                            SpawnBullet(__instance, Prefab, FirePos, BulletDamage, Layers, Origin, PlayerSpawner.transform.position);
                        }
                        if (Counter % 5 == 0)
                        {
                            SpawnFan(__instance, Prefab, FirePos, BulletDamage, Layers, Origin, 5f);
                        }
                        Stack = 18;
                    }
                    else if (Hard >= 2)
                    {
                        int Type = UnitAI_ArmadilloGolem_Patch.SpreadBulletType.Count > 0 ? UnitAI_ArmadilloGolem_Patch.SpreadBulletType[0] : 0;
                        switch (Type)
                        {
                            case 0:
                                SpawnFan(__instance, Prefab, FirePos, BulletDamage, Layers, Origin, 5f);
                                break;
                            case 1:
                                {
                                    float Rot = 180f + 90f * Mathf.Sin(Counter * Mathf.PI / 30f);
                                    Vector2 Offset = HorayUtility.GetVector2FromAngle(Rot);
                                    Vector3 Pos = Origin + (Vector3)Offset * 2f;
                                    SpawnBullet(__instance, Prefab, FirePos, BulletDamage, Layers, Origin, Pos);
                                    if (Counter % 18 == 0)
                                    {
                                        foreach (PlayerSpawner PlayerSpawner in PlayerSpawner.MultiplayerList)
                                        {
                                            if (PlayerSpawner)
                                            {
                                                SpawnBullet(__instance, Prefab, FirePos, BulletDamage, Layers, Origin, PlayerSpawner.transform.position);
                                            }
                                        }
                                    }
                                    Stack = 180;
                                }
                                break;
                            case 2:
                                {
                                    float Rot = 180f + 45f * Mathf.Sin(Counter * Mathf.PI / 30f);
                                    for (int i = -1; i <= 1; i += 1)
                                    {
                                        Vector2 Offset = HorayUtility.GetVector2FromAngle(Rot + i * 75f);
                                        Vector3 Pos = Origin + (Vector3)Offset * 2f;
                                        SpawnBullet(__instance, Prefab, FirePos, BulletDamage, Layers, Origin, Pos, 0.8f);
                                    }
                                    Stack = 120;
                                }
                                break;
                        }
                    }
                    else
                    {
                        SpawnFan(__instance, Prefab, FirePos, BulletDamage, Layers, Origin, 12f);
                    }
                    if (Counter >= Stack)
                    {
                        if (Hard >= 2 && UnitAI_ArmadilloGolem_Patch.SpreadBulletType.Count > 0)
                            UnitAI_ArmadilloGolem_Patch.SpreadBulletType.RemoveAt(0);
                        __instance.NetworkheadState = Unit_ArmadilloGolem.EHeadState.SpreadBullet_End;
                    }
                }
            }
            // 射击攻击
            else if (HeadState == Unit_ArmadilloGolem.EHeadState.Subshot)
            {
                if (!SubshotFiringRef!(__instance))
                {
                    if (__instance.subShotTimer.Update(Time.deltaTime))
                    {
                        SubshotFiringRef!(__instance) = true;
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
                        SubshotFireCounterRef!(__instance) = Stack;
                        Transform Target = SubshotFireTargetRef!(__instance);
                        if (Target != null)
                        {
                            SubshotFirePositionRef!(__instance) = Target.position;
                        }
                    }
                    else if (SubshotStopRequestRef!(__instance))
                    {
                        SubshotStopRequestRef!(__instance) = false;
                        __instance.NetworkheadState = Unit_ArmadilloGolem.EHeadState.Idle;
                    }
                }
                else if (__instance.subShotIntervalTimer.Update(Time.deltaTime))
                {
                    __instance.RpcCreateBulletFireFx();
                    Vector3 FirePos = __instance.spreadBulletFirePosition.position;
                    Bullet.Pool.Spawn(__instance.subshotBulletPrefab, FirePos, false, EDamageFromType.DirectAttack, "BasicAttack",
                        __instance.RequestStandardDamage() * 0.5f, 0, 0f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack),
                        0.3f, FirePos, SubshotFirePositionRef!(__instance), null, null);
                    int FireCounter = ++SubshotFireCounterRef!(__instance);
                    if (FireCounter >= __instance.subShotFireCount)
                    {
                        SubshotFiringRef!(__instance) = false;
                        if (SubshotStopRequestRef!(__instance))
                        {
                            SubshotStopRequestRef!(__instance) = false;
                            __instance.NetworkheadState = Unit_ArmadilloGolem.EHeadState.Idle;
                        }
                    }
                }
            }
            // 手部状态
            Unit_ArmadilloGolem.EHandState HandState = HandStateGetter!(__instance);
            Unit_ArmadilloGolem_Hand[] Hands = HandsRef!(__instance);
            bool[] HandLaserFired = HandLaserFiredRef!(__instance);
            int[] HandLaserMoveDir = HandLaserMoveDirRef!(__instance);
            float LaserMinY = LaserMinYRef!(__instance);
            float LaserMaxY = LaserMaxYRef!(__instance);
            // 激光准备
            if (HandState == Unit_ArmadilloGolem.EHandState.LaserReady)
            {
                bool LaserMaxFlip = LaserMaxFlipRef!(__instance);
                bool AllReady = true;
                for (int i = 0; i < Hands.Length; i++)
                {
                    Unit_ArmadilloGolem_Hand Hand = Hands[i];
                    if (Hand == null) continue;
                    Vector3 ToPos;
                    if (Hand.handSide == Unit_ArmadilloGolem_Hand.EHandSide.Left)
                    {
                        ToPos = LaserMaxFlip
                            ? new Vector3(__instance.laserLeftXPosition, LaserMinY, 0f)
                            : new Vector3(__instance.laserLeftXPosition, LaserMaxY, 0f);
                    }
                    else
                    {
                        ToPos = LaserMaxFlip
                            ? new Vector3(__instance.laserRightXPosition, LaserMaxY, 0f)
                            : new Vector3(__instance.laserRightXPosition, LaserMinY, 0f);
                    }
                    Vector3 ToPosVec = ToPos - Hand.transform.position;
                    Hand.SetMove(ToPos, 1f);
                    if (ToPosVec.sqrMagnitude > 0.8f || HandLaserFired[i])
                    {
                        AllReady = false;
                    }
                }
                if (AllReady)
                {
                    for (int i = 0; i < Hands.Length; i++)
                    {
                        HandLaserFired[i] = true;
                        Unit_ArmadilloGolem_Hand Hand = Hands[i];
                        if (Hand == null) continue;
                        Hand.LaserFire();
                        Hand.StopMove();
                    }
                    HandStateSetter!(__instance, Unit_ArmadilloGolem.EHandState.LaserFire);
                    Unit_ArmadilloGolem_Patch.LaseSpeed = Random.Range(0.75f, 1.25f);
                    return;
                }
            }
            // 激光发射
            else if (HandState == Unit_ArmadilloGolem.EHandState.LaserFire)
            {
                for (int i = 0; i < Hands.Length; i++)
                {
                    Unit_ArmadilloGolem_Hand Hand = Hands[i];
                    if (Hand == null) continue;
                    Vector3 Position = Hand.transform.position;
                    if (Hand.handSide == Unit_ArmadilloGolem_Hand.EHandSide.Left)
                    {
                        float Speed = __instance.laseSpeed_left * Unit_ArmadilloGolem_Patch.LaseSpeed;
                        if (Hard >= 3) Speed *= 0.6f + 0.2f * Hard;
                        Position.y += Speed * HandLaserMoveDir[i] * Time.deltaTime;
                    }
                    else
                    {
                        float Speed = __instance.laseSpeed_right * (2f - Unit_ArmadilloGolem_Patch.LaseSpeed);
                        if (Hard >= 3) Speed *= 0.9f + 0.1f * Hard;
                        Position.y += Speed * HandLaserMoveDir[i] * Time.deltaTime;
                    }
                    Hand.transform.position = Position;
                    if (HandLaserMoveDir[i] == 1 && Position.y > LaserMaxY)
                    {
                        HandLaserMoveDir[i] *= -1;
                    }
                    else if (HandLaserMoveDir[i] == -1 && Position.y < LaserMinY)
                    {
                        HandLaserMoveDir[i] *= -1;
                    }
                }
                if (__instance.laserFireTimer.Update(Time.deltaTime))
                {
                    __instance.HeadPattern_StopSubshot();
                    HandStateSetter!(__instance, Unit_ArmadilloGolem.EHandState.LaserEnd);
                    foreach (Unit_ArmadilloGolem_Hand Hand in Hands)
                    {
                        if (Hand != null) Hand.LaserEnd();
                    }
                    return;
                }
            }
            // 激光结束
            else if (HandState == Unit_ArmadilloGolem.EHandState.LaserEnd)
            {
                float MidY = (LaserMinY + LaserMaxY) / 2f;
                foreach (Unit_ArmadilloGolem_Hand Hand in Hands)
                {
                    if (Hand == null) continue;
                    Vector3 TargetPos = Hand.handSide == Unit_ArmadilloGolem_Hand.EHandSide.Left
                        ? new Vector3(__instance.laserLeftXPosition + 2f, MidY, 0f)
                        : new Vector3(__instance.laserRightXPosition - 2f, MidY, 0f);
                    Hand.SetMove(TargetPos, 1f);
                }
                if (__instance.laserEndTimer.Update(Time.deltaTime))
                {
                    HandStateSetter!(__instance, Unit_ArmadilloGolem.EHandState.Idle);
                    UpdateTotemStateMethod!.Invoke(__instance, null);
                    CreateTotemMethod!.Invoke(__instance, null);
                }
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
                    List<Vector3> MissilePositions = [__instance.transform.position];
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
                                MissilePositions.Add(__instance.transform.position + Vec * i);
                        }
                    }
                    for (int i = 0; i < MissilePositions.Count; i++)
                    {
                        float Delay = HeadAI.missileWarning + HeadAI.missileFireInterval * i * 1.25f;
                        Head.LaunchMissile(MissilePositions[i] + (Vector3)Random.insideUnitCircle * HeadAI.missileError,
                            Delay, Delay - HeadAI.missileWarning, HeadAI.missileWarning);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Unit_ArmadilloGolem))]
        public static class Unit_ArmadilloGolem_Patch
        {
            public static Unit_ArmadilloGolem? Golem;
            public static float LaseSpeed = 0f;

            [HarmonyPatch("Update")]
            [HarmonyPrefix]
            static bool Update_Prefix(Unit_ArmadilloGolem __instance)
            {
                if (Hard <= 0) return true;
                if (!GolemUpdateReady)
                {
                    StarSephiriaMod.LogError("[Unit_ArmadilloGolem][Update]获取反射失败");
                    return true;
                }
                try
                {
                    InstanceUpdate(__instance);
                    UnitAvatar_Patches.Update_Postfix(__instance);
                    Golem = __instance;

                    if (__instance.isServer)
                    {
                        ServerUpdate(__instance);
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
                    __instance.spreadBulletTimer.time = 0.2f;
                }
                else
                {
                    int Type = UnitAI_ArmadilloGolem_Patch.SpreadBulletType.Count > 0 ? UnitAI_ArmadilloGolem_Patch.SpreadBulletType[0] : 0;
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
            public static List<int> SpreadBulletType = [];
            public static int Stomp = 0;
            public static UnitAI_ArmadilloGolem? GolemAI;

            [HarmonyPatch("OnAIUpdate_FoundEnemy")]
            [HarmonyPrefix]
            static bool OnAIUpdate_FoundEnemy_Prefix(UnitAI_ArmadilloGolem __instance)
            {
                if (Hard <= 0) return true;
                if (!GolemAIReady)
                {
                    StarSephiriaMod.LogError("[UnitAI_ArmadilloGolem][Update]获取反射失败");
                    return true;
                }
                try
                {
                    GolemAI = __instance;
                    Unit_ArmadilloGolem Golem = GolemRef!(__instance);

                    if (!MissileLaunchRef!(__instance))
                    {
                        float TimeMult = 0.5f;
                        int PlayerStack = Mathf.Max(1, NetworkServer.connections.Count);
                        TimeMult += 1f / PlayerStack;
                        if (Hard >= 2) TimeMult *= 1.5f;
                        if (__instance.missileTimer.Update(Time.deltaTime * TimeMult))
                        {
                            MissileLaunchRef!(__instance) = true;
                            Golem.SetMissileZoneDir(Random.Range(0, 2));
                            Golem.PlayMissileStartSound();
                        }
                    }
                    else
                    {
                        List<Vector3> MissilePositions = [];
                        foreach (PlayerSpawner PlayerSpawner in PlayerSpawner.MultiplayerList)
                        {
                            if (!PlayerSpawner) continue;
                            Vector3 PlayerPosition = PlayerSpawner.transform.position;
                            MissilePositions.Add(PlayerPosition);
                            if (Hard >= 2)
                            {
                                MissilePositions.Add(PlayerPosition + new Vector3(4f, 0f, 0f));
                                MissilePositions.Add(PlayerPosition + new Vector3(-4f, 0f, 0f));
                                MissilePositions.Add(PlayerPosition + new Vector3(0f, 2f, 0f));
                                MissilePositions.Add(PlayerPosition + new Vector3(0f, -2f, 0f));
                            }
                        }
                        for (int i = 0; i < MissilePositions.Count; i++)
                        {
                            float Delay = __instance.missileWarning + __instance.missileFireInterval * i;
                            Golem.LaunchMissile(MissilePositions[i] + (Vector3)Random.insideUnitCircle * __instance.missileError,
                                Delay, Delay - __instance.missileWarning, __instance.missileWarning);
                        }
                        MissileLaunchRef!(__instance) = false;
                    }

                    if (Golem.HeadState == Unit_ArmadilloGolem.EHeadState.Idle &&
                        Golem.HandState == Unit_ArmadilloGolem.EHandState.Idle &&
                        HandPatternTimerRef!(__instance).Update(Time.deltaTime))
                    {
                        List<int> RandomPatternForRoundRobin = RandomPatternForRoundRobinRef!(__instance);
                        if (RandomPatternForRoundRobin.Count <= 1)
                        {
                            List<int> Candidates = [0, 1, 2, 3];
                            if (Oldpattern != -1) Candidates.Remove(Oldpattern);
                            Candidates.Shuffle();
                            if (Oldpattern != -1) Candidates.Add(Oldpattern);

                            if (Hard >= 3) Candidates.Remove(2);

                            RandomPatternForRoundRobin.Clear();
                            while (Candidates.Count > 0)
                            {
                                RandomPatternForRoundRobin.Add(Candidates[0]);
                                if (Hard >= 3) RandomPatternForRoundRobin.Add(2);
                                Candidates.RemoveAt(0);
                            }
                            RandomPatternForRoundRobin.Add(2);
                        }
                        int Pattern = RandomPatternForRoundRobin[0];
                        switch (Pattern)
                        {
                            case 1:
                                Stomp = 0;
                                Golem.HeadPattern_Subshot(__instance.CurrentTarget.transform);
                                Golem.HandPattern_Laser(HandAreaBottomLeftRef!(__instance).x, HandAreaTopRightRef!(__instance).x,
                                    HandAreaBottomLeftRef!(__instance).y - 0.75f, HandAreaTopRightRef!(__instance).y - 1f);
                                break;
                            case 2:
                                if (Hard >= 2 && SpreadBulletType.Count <= 0)
                                {
                                    SpreadBulletType = [0, 1, 2];
                                    SpreadBulletType.Shuffle();
                                }
                                Stomp = 0;
                                Golem.HeadPattern_SpreadBullet();
                                break;
                            case 3:
                                Stomp = 1;
                                Golem.HandPattern_ShockwaveSmash(__instance.CurrentTarget.transform);
                                break;
                            default:
                                Stomp = 1;
                                Golem.HandPattern_Stomp(4, __instance.CurrentTarget.transform);
                                break;
                        }
                        if ((Hard >= 3 && Pattern != 2) || Hard < 3)
                            Oldpattern = Pattern;
                        StarSephiriaMod.LogInfoBoss($"牛头当前攻击：{Pattern}");
                        RandomPatternForRoundRobin.RemoveAt(0);
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
                if (!GolemAIReady)
                {
                    StarSephiriaMod.LogError("[UnitAI_ArmadilloGolem][Awake]获取反射失败");
                    return;
                }
                try
                {
                    RandomPatternForRoundRobinRef!(__instance).Clear();
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[UnitAI_ArmadilloGolem][Awake]出现错误: {ex}");
                }
            }
        }
    }
}
