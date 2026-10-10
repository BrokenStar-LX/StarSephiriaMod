using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static SPMod.StarSephiriaPatches;
using Color = UnityEngine.Color;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace SPMod.MonsterAI
{
    [HarmonyPatch]
    public static class LibraryGuard
    {
        public static int Hard
        {
            get { return ModConfig.HardMode; }
        }
        // UnitAI_LibraryGuard 反射字段
        private static readonly FieldInfo? patternQueueField;
        private static readonly FieldInfo? unitField;
        // Unit_LibraryGuard 反射字段
        private static readonly FieldInfo? jumpAttackWarningField;
        private static readonly FieldInfo? laserWarningsField;
        private static readonly FieldInfo? crosshairInstanceField;
        private static readonly FieldInfo? laserTargetField;
        private static readonly FieldInfo? chakramsField;
        private static readonly FieldInfo? zoomOutClientsideField;
        private static readonly FieldInfo? jumpTargetPositionField;
        private static readonly FieldInfo? currentJumpAttackCountField;
        private static readonly FieldInfo? laserAttackedListField;
        private static readonly FieldInfo? laserAttackCountField;
        private static readonly FieldInfo? currentLaserAimTimerField;
        private static readonly FieldInfo? spinDashPhaseField;
        private static readonly FieldInfo? spinDashCollidersField;
        private static readonly FieldInfo? chakramCollidersField;
        private static readonly FieldInfo? fireBallPositionField;
        // CombatBehaviour 反射字段
        private static readonly FieldInfo? transformAuthorityField;
        // UnitAvatar 反射字段
        private static readonly FieldInfo? isHitInvincibleEnabledField;
        private static readonly FieldInfo? invincibleTimerField;
        private static readonly FieldInfo? isReviveInvulnerableField;
        private static readonly FieldInfo? isTempInvulnerableField;
        private static readonly FieldInfo? tempInvulnerableTimerField;
        private static readonly FieldInfo? stunCooldownTimerField;
        static LibraryGuard()
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

                // UnitAI_LibraryGuard 反射
                type = typeof(UnitAI_LibraryGuard);
                patternQueueField = AccessTools.Field(type, "patternQueue");
                unitField = AccessTools.Field(type, "unit");
                StarSephiriaMod.LogInfoBoss("[UnitAI_LibraryGuard]初始化反射");

                // Unit_LibraryGuard 反射
                type = typeof(Unit_LibraryGuard);
                jumpAttackWarningField = AccessTools.Field(type, "jumpAttackWarning");
                laserWarningsField = AccessTools.Field(type, "laserWarnings");
                crosshairInstanceField = AccessTools.Field(type, "crosshairInstance");
                laserTargetField = AccessTools.Field(type, "laserTarget");
                chakramsField = AccessTools.Field(type, "chakrams");
                zoomOutClientsideField = AccessTools.Field(type, "zoomOutClientside");
                jumpTargetPositionField = AccessTools.Field(type, "jumpTargetPosition");
                currentJumpAttackCountField = AccessTools.Field(type, "currentJumpAttackCount");
                laserAttackedListField = AccessTools.Field(type, "laserAttackedList");
                laserAttackCountField = AccessTools.Field(type, "laserAttackCount");
                currentLaserAimTimerField = AccessTools.Field(type, "currentLaserAimTimer");
                spinDashPhaseField = AccessTools.Field(type, "spinDashPhase");
                spinDashCollidersField = AccessTools.Field(type, "spinDashColliders");
                chakramCollidersField = AccessTools.Field(type, "chakramColliders");
                fireBallPositionField = AccessTools.Field(type, "fireBallPosition");
                StarSephiriaMod.LogInfoBoss("[Unit_LibraryGuard]初始化反射");

                // CombatBehaviour 反射
                type = typeof(CombatBehaviour);
                transformAuthorityField = AccessTools.Field(type, "transformAuthority");
                StarSephiriaMod.LogInfoBoss("[CombatBehaviour]初始化反射");
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

        [HarmonyPatch(typeof(LibraryGuardZigzagGun))]
        public static class LibraryGuardZigzagGun_Patch
        {
            [HarmonyPatch("Fire")]
            [HarmonyPrefix]
            static void Fire_Prefix(LibraryGuardZigzagGun __instance, ref int fireCount)
            {
                if (Hard >= 2) fireCount += Hard - 1;
            }
        }

        [HarmonyPatch(typeof(Unit_LibraryGuard))]
        public static class Unit_LibraryGuard_Patch
        {
            [HarmonyPatch("Update")]
            [HarmonyPrefix]
            static bool Update_Prefix(Unit_LibraryGuard __instance)
            {
                if (Hard <= 0) return true;
                try
                {
                    if (jumpAttackWarningField == null || laserWarningsField == null || crosshairInstanceField == null || laserTargetField == null || chakramsField == null || zoomOutClientsideField == null || currentJumpAttackCountField == null || laserAttackedListField == null || laserAttackCountField == null || currentLaserAimTimerField == null || spinDashPhaseField == null)
                    {
                        StarSephiriaMod.LogError("[Unit_LibraryGuard][Update]获取反射失败");
                        return true;
                    }

                    // 使用反射獲取字段
                    var jumpAttackWarning = jumpAttackWarningField.GetValue(__instance) as UI_AOEWarning_MeleeAttackLine_Windmill;
                    var crosshairInstance = crosshairInstanceField.GetValue(__instance) as LibraryGuardCrosshair;
                    var laserTarget = laserTargetField.GetValue(__instance) as UnitAvatar;
                    var chakrams = chakramsField.GetValue(__instance) as GameObject[];
                    var zoomOutClientside = (bool)zoomOutClientsideField.GetValue(__instance);
                    var currentJumpAttackCount = (int)currentJumpAttackCountField.GetValue(__instance);
                    var laserAttackedList = laserAttackedListField.GetValue(__instance) as List<CombatBehaviour>;
                    var laserAttackCount = (int)laserAttackCountField.GetValue(__instance);
                    var currentLaserAimTimer = (float)currentLaserAimTimerField.GetValue(__instance);
                    var spinDashPhase = (int)spinDashPhaseField.GetValue(__instance);
                    InstanceUpdate(__instance);
                    UnitAvatar_Patches.Update_Postfix(__instance);
                    if (__instance.isServer)
                    {
                        if (__instance.currentState == Unit_LibraryGuard.EState.JumpAttack)
                        {
                            JumpAttack(__instance);
                        }
                        else if (__instance.currentState == Unit_LibraryGuard.EState.Laser)
                        {
                            Laser(__instance);
                        }
                        else if (__instance.currentState == Unit_LibraryGuard.EState.SpinDash)
                        {
                            SpinDash(__instance);
                        }
                        else if (__instance.currentState == Unit_LibraryGuard.EState.Chakram)
                        {
                            Chakram(__instance);
                            if (Hard >= 3)
                                Laser(__instance);
                        }
                        for (int n = __instance.attackedList.Count - 1; n >= 0; n--)
                        {
                            __instance.attackedList[n].timer += Time.deltaTime;
                            if (__instance.attackedList[n].timer > 1f)
                            {
                                __instance.attackedList.RemoveAt(n);
                            }
                        }
                    }
                    if (__instance.isClient)
                    {
                        if (jumpAttackWarning != null && jumpAttackWarning.IsSpawned)
                        {
                            Vector3 vector7 = __instance.transform.position + __instance.jumpAttackOffset;
                            jumpAttackWarning.transform.position = vector7;
                        }

                        if (__instance.trueDiePhase == 1 && __instance.trueDieFxTimer.Update(Time.deltaTime))
                        {
                            __instance.trueDieFxTimer.time -= 0.015f;
                            if (__instance.trueDieFxTimer.time < 0.12f)
                            {
                                __instance.trueDieFxTimer.time = 0.12f;
                            }
                            GameCamera.Instance.targetTracker.CreateCameraShaking(__instance.transform.position, EShakeCameraType.Continous, new Vector2(0.03f, 0.03f), 0.06f, 1f);
                            if (__instance.dieExplosionSoundEvent)
                            {
                                __instance.dieExplosionSoundEvent.Play();
                            }
                            SpriteFx.Pool.Spawn(__instance.trueDieFxPrefab, __instance.transform.position + new Vector3(0f, 1f) + (Vector3)Random.insideUnitCircle * 1.5f, null);
                            SpriteFx.Pool.Spawn(__instance.trueDieFxPrefab, __instance.transform.position + new Vector3(0f, 1f) + (Vector3)Random.insideUnitCircle * 1.5f, null);
                            SpriteFx.Pool.Spawn(__instance.trueDieFxPrefab, __instance.transform.position + new Vector3(0f, 1f) + (Vector3)Random.insideUnitCircle * 1.5f, null);
                        }

                        if (laserWarningsField.GetValue(__instance) is List<UI_AOEWarning_RangeAttackLine> laserWarnings && laserWarnings.Count > 0)
                        {
                            for (int i = 0; i < laserWarnings.Count; i++)
                            {
                                if (laserWarnings[i] != null && laserWarnings[i].IsSpawned)
                                {
                                    float num5 = 360f / laserWarnings.Count * i;
                                    laserWarnings[i].SetFromTo(__instance.laserStartPoint.position, __instance.laserStartPoint.position + HorayUtility.GetVector3FromAngle(__instance.laserTargetAngle + num5) * 10f);
                                }
                            }
                        }

                        if (__instance.currentState == Unit_LibraryGuard.EState.SpinDash && __instance.spinDashSpeed > 5f && __instance.spinDashSilhouetteTimer.Update(Time.deltaTime) && __instance.TopdownActor && __instance.TopdownActor.bodyRenderer)
                        {
                            SilhouetteFx.Pool.Spawn(__instance.transform.position).SetSprite(__instance.TopdownActor.bodyRenderer.sprite, Color.white, __instance.transform.localScale, 0.4f);
                        }

                        if (__instance.currentState == Unit_LibraryGuard.EState.Laser || __instance.currentState == Unit_LibraryGuard.EState.Chakram && Hard >= 3)
                        {
                            if (crosshairInstance == null)
                            {
                                crosshairInstance = Object.Instantiate(__instance.crosshairPrefab, new Vector3(-1000f, -1000f), Quaternion.identity);
                                crosshairInstanceField.SetValue(__instance, crosshairInstance);
                            }
                            if (laserTarget != null)
                            {
                                if (laserTarget.TopdownActor)
                                {
                                    crosshairInstance.transform.position = laserTarget.TopdownActor.Position_Center;
                                }
                                else
                                {
                                    crosshairInstance.transform.position = laserTarget.transform.position;
                                }
                            }
                            else
                            {
                                crosshairInstance.transform.position = new Vector3(-1000f, -1000f);
                            }
                        }
                        else if (crosshairInstance != null)
                        {
                            Object.Destroy(crosshairInstance.gameObject);
                            crosshairInstanceField.SetValue(__instance, null);
                        }

                        if (__instance.currentState == Unit_LibraryGuard.EState.Chakram && __instance.chakramState == 0)
                        {
                            if (__instance.chakramFxTimer.Update(Time.deltaTime) && __instance.chakramFxPrefab && __instance.TopdownActor && __instance.TopdownActor.bodyRenderer)
                            {
                                Vector3 size = __instance.TopdownActor.bodyRenderer.sprite.bounds.size;
                                Vector3 vector8 = new Vector3(Random.Range(-size.x / 2f, size.x / 2f), Random.Range(-size.y / 2f, size.y / 2f)) * 0.5f;
                                SpriteFx.Pool.Spawn(__instance.chakramFxPrefab, __instance.TopdownActor.Position_Center + vector8, null);
                            }
                            if (__instance.chakramSilhouetteTimer.Update(Time.deltaTime) && __instance.TopdownActor && __instance.TopdownActor.bodyRenderer)
                            {
                                SilhouetteFx.Pool.Spawn(__instance.transform.position).SetSprite(__instance.TopdownActor.bodyRenderer.sprite, Color.white, __instance.transform.localScale, 0.4f);
                            }
                        }
                        if (__instance.currentState == Unit_LibraryGuard.EState.Chakram && __instance.chakramState >= 1)
                        {
                            if (chakrams == null)
                            {
                                chakrams = new GameObject[3];
                                for (int i = 0; i < 3; i++)
                                {
                                    chakrams[i] = Object.Instantiate(__instance.chakramPrefab, __instance.chakramCenterPoint.position, Quaternion.identity);
                                    chakrams[i].SetActive(true);
                                }
                                chakramsField.SetValue(__instance, chakrams);
                            }
                            else
                            {
                                for (int i = 0; i < 3; i++)
                                {
                                    Vector2 vector2FromAngle2 = HorayUtility.GetVector2FromAngle(__instance.currentChakramAngle + i * 120f);
                                    vector2FromAngle2.y *= 0.625f;
                                    chakrams[i].transform.position = __instance.chakramCenterPoint.position + (Vector3)vector2FromAngle2 * __instance.currentChakramRadius;
                                }
                            }
                        }
                        else if (chakrams != null)
                        {
                            GameObject[] array = chakrams;
                            for (int num8 = 0; num8 < array.Length; num8++)
                            {
                                if (array[num8] != null)
                                {
                                    Object.Destroy(array[num8].gameObject);
                                }
                            }
                            chakramsField.SetValue(__instance, null);
                        }

                        if (__instance.zoomOut)
                        {
                            if (!zoomOutClientside)
                            {
                                zoomOutClientsideField.SetValue(__instance, true);
                                if (GameCamera.Instance)
                                {
                                    GameCamera.Instance.targetTracker.SetZoom(7f, 2f);
                                    return false;
                                }
                            }
                        }
                        else if (zoomOutClientside)
                        {
                            zoomOutClientsideField.SetValue(__instance, false);
                            if (GameCamera.Instance)
                            {
                                GameCamera.Instance.targetTracker.ResetZoom(0.3f);
                            }
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[Unit_LibraryGuard][Update]出现错误{ex}");
                    return true;
                }
            }

            static void JumpAttack(Unit_LibraryGuard __instance)
            {
                if (currentJumpAttackCountField == null || jumpTargetPositionField == null)
                {
                    StarSephiriaMod.LogError("[Unit_LibraryGuard][JumpAttack]获取反射失败");
                    return;
                }

                var currentJumpAttackCount = (int)currentJumpAttackCountField.GetValue(__instance);
                var jumpTargetPosition = (Vector3)jumpTargetPositionField.GetValue(__instance);

                if (__instance.jumpAttackPhase == 1)
                {
                    Vector3 Vec = jumpTargetPosition - __instance.transform.position;
                    __instance.TopdownRigidbody.RequestMove(Vec * 2f * Time.deltaTime);
                    if (__instance.jumpUpSpeedTimer.Update(Time.deltaTime))
                    {
                        __instance.TopdownRigidbody.YPosition = __instance.jumpUpYPos;
                        __instance.NetworkjumpAttackPhase = 2;
                        __instance.TopdownRigidbody.SetGravityVelocityAndScale(0.1f, 0.33f);
                    }
                    else
                    {
                        float ratio = __instance.jumpUpSpeedTimer.Ratio;
                        float num = 1f - (1f - ratio) * (1f - ratio);
                        __instance.TopdownRigidbody.YPosition = Mathf.Lerp(0f, __instance.jumpUpYPos, num);
                    }
                }
                else if (__instance.jumpAttackPhase == 2)
                {
                    Vector3 Vec = jumpTargetPosition - __instance.transform.position;
                    __instance.TopdownRigidbody.RequestMove(Vec * 2f * Time.deltaTime);
                    if (__instance.jumpUpSpeedTimer.Update(Time.deltaTime * 3f))
                    {
                        __instance.NetworkjumpAttackPhase = 3;
                        __instance.RpcCreateJumpAttackWarning(__instance.jumpAttackFollowTargetTimer.time);
                        __instance.TopdownRigidbody.SetGravityVelocity(1f);
                        if (__instance.jumpAttackTarget)
                        {
                            jumpTargetPositionField.SetValue(__instance, __instance.jumpAttackTarget.transform.position);
                        }
                    }
                }
                else if (__instance.jumpAttackPhase == 3)
                {
                    Vector3 Vec = jumpTargetPosition - __instance.transform.position;
                    __instance.TopdownRigidbody.RequestMove(Vec * 5f * Time.deltaTime);
                    if (__instance.jumpAttackFollowTargetTimer.Update(Time.deltaTime))
                    {
                        __instance.NetworkjumpAttackPhase = 4;
                        __instance.TopdownRigidbody.SetGravityVelocityAndScale(-30f, 1f);
                    }
                }
                else if (__instance.jumpAttackPhase == 4)
                {
                    if (__instance.TopdownRigidbody.IsGrounded)
                    {
                        __instance.TopdownRigidbody.YPosition = 0f;
                        __instance.TopdownRigidbody.SetGravityVelocity(0f);
                        __instance.NetworkjumpAttackPhase = 5;
                        __instance.RpcJumpAttackSwingFx();
                        Vector3 Pos = __instance.transform.position + __instance.jumpAttackOffset;
                        __instance.jumpAttackFireData.CreateAttack(EDamageFromType.DirectAttack, __instance.RequestStandardDamage(), "BasicAttack", false,
                            __instance, Pos, Pos, 0f, null, null, 1f, null, false, 0f, 1f, 0, null);
                        int MaxStack = 6;
                        if (Hard >= 2)
                            MaxStack = 12;
                        for (int i = 0; i < MaxStack; i++)
                        {
                            Vector3 Rot = HorayUtility.GetVector3FromAngle(360f / MaxStack * i);
                            Bullet.Pool.Spawn(__instance.jumpAttackShockwaveBulletPrefab, Pos, false, EDamageFromType.DirectAttack, "BasicAttack",
                                __instance.RequestStandardDamage() * 0.5f, 0, 0f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0f,
                                Pos, Pos + Rot.normalized * 10f, null, null, 0f, EDamageElementalType.Physical, true);
                        }
                        if (Hard >= 3)
                        {
                            Bullet a = Bullet.Pool.Spawn(__instance.fireBallPrefab, __instance.transform.position, false, EDamageFromType.DirectAttack, "BasicAttack",
                                __instance.RequestStandardDamage(), 25, 1f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0.5f,
                                __instance.transform.position, __instance.transform.position, null, null, 0f, EDamageElementalType.Physical, true);
                            a.Destroy();
                        }
                    }
                }
                else if (__instance.jumpAttackPhase == 5 && __instance.jumpAttackEndTimer.Update(Time.deltaTime))
                {
                    int newCount = currentJumpAttackCount + 1;
                    currentJumpAttackCountField.SetValue(__instance, newCount);
                    if (newCount >= __instance.jumpAttackCount)
                    {
                        __instance.NetworkjumpAttackPhase = -1;
                        __instance.NetworkcurrentState = Unit_LibraryGuard.EState.Idle;
                    }
                    else
                    {
                        __instance.NetworkjumpAttackPhase = 0;
                        __instance.TopdownRigidbody.SetGravityScale(0f);
                        if (__instance.jumpAttackTarget)
                        {
                            jumpTargetPositionField.SetValue(__instance, __instance.jumpAttackTarget.transform.position);
                        }
                    }
                }
            }

            static void Laser(Unit_LibraryGuard __instance)
            {
                if (currentLaserAimTimerField == null || laserAttackedListField == null || laserAttackCountField == null)
                {
                    StarSephiriaMod.LogError("[Unit_LibraryGuard][Laser]获取反射失败");
                    return;
                }

                var currentLaserAimTimer = (float)currentLaserAimTimerField.GetValue(__instance);
                var laserAttackedList = laserAttackedListField.GetValue(__instance) as List<CombatBehaviour>;
                var laserAttackCount = (int)laserAttackCountField.GetValue(__instance);

                if (__instance.laserPhase == 0)
                {
                    Vector3 Vec = __instance.laserMoveTargetPosition - __instance.transform.position;
                    __instance.TopdownRigidbody.RequestMove(Vec * __instance.laserPhaseTimer.time * 2f * Time.deltaTime);
                    if (__instance.laserPhaseTimer.Update(Time.deltaTime))
                    {
                        __instance.laserPhase = 1;
                    }
                }
                else if (__instance.laserAttackIntervalTimer.Update(Time.deltaTime))
                {
                    for (int j = 0; j < 3; j++)
                    {
                        Vector3 vector3FromAngle2 = HorayUtility.GetVector3FromAngle(__instance.laserTargetAngle + j * 360f / 3f);
                        Bullet.Pool.Spawn(__instance.laserPrefab, __instance.laserStartPoint.position, false, EDamageFromType.DirectAttack, "BasicAttack",
                            __instance.RequestStandardDamage(), 0, 0f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0f,
                            __instance.laserStartPoint.position, __instance.laserStartPoint.position + vector3FromAngle2.normalized * 10f, Hard >= 4 ? null : laserAttackedList, null, 0f, EDamageElementalType.Physical, true);
                    }
                    int newLaserAttackCount = laserAttackCount + 1;
                    laserAttackCountField?.SetValue(__instance, newLaserAttackCount);

                    if (newLaserAttackCount >= __instance.laserAttackMaxCount)
                    {
                        __instance.NetworkcurrentState = Unit_LibraryGuard.EState.Idle;
                    }
                }
                if (__instance.laserPhase == 1 && Hard < 3)
                {
                    Vector3 Vec = __instance.laserMoveTargetPosition - __instance.transform.position;
                    __instance.TopdownRigidbody.RequestMove(Vec * __instance.laserPhaseTimer.time * 0.33f * Time.deltaTime);
                }
                currentLaserAimTimer += Time.deltaTime;
                currentLaserAimTimerField?.SetValue(__instance, currentLaserAimTimer);

                var laserTarget = laserTargetField?.GetValue(__instance) as UnitAvatar;
                if (laserTarget != null)
                {
                    float angle = HorayUtility.GetAngle(laserTarget.transform.position, __instance.laserStartPoint.position);
                    __instance.currentLaserAimSpeed = Mathf.Lerp(__instance.laserAimRotateSpeed, __instance.laserAimRotateSpeedEnd, currentLaserAimTimer / __instance.currentLaserAimTime);
                    float AimSpeedMult = Time.deltaTime;
                    if (Hard >= 2) AimSpeedMult *= 1.1f;
                    __instance.NetworklaserTargetAngle = Mathf.MoveTowardsAngle(__instance.laserTargetAngle, angle, __instance.currentLaserAimSpeed * AimSpeedMult);
                }
            }

            static void SpinDash(Unit_LibraryGuard __instance)
            {
                if (spinDashPhaseField == null || transformAuthorityField == null || spinDashCollidersField == null)
                {
                    StarSephiriaMod.LogError("[Unit_LibraryGuard][SpinDash]获取反射失败");
                    return;
                }

                var spinDashPhase = (int)spinDashPhaseField.GetValue(__instance);
                var transformAuthority = (bool)transformAuthorityField.GetValue(__instance);
                var spinDashColliders = (Collider2D[])spinDashCollidersField.GetValue(null);

                if (spinDashPhase == 1)
                {
                    float MaxSpeed = __instance.spinDashMaxSpeed;
                    MaxSpeed *= 0.95f + Hard * 0.1f;
                    __instance.NetworkspinDashSpeed = Mathf.MoveTowards(__instance.spinDashSpeed, MaxSpeed, 5f * Time.deltaTime);
                    if (__instance.spinDashSpeed > 6f)
                    {
                        int MaxColliders = HorayPhysics2D.OverlapBox((Vector2)__instance.transform.position + __instance.spinDashCollisionOffset, __instance.spinDashCollisionSize, 0f, spinDashColliders, CombatManager.Topdown1FLayerMask);
                        for (int k = 0; k < MaxColliders; k++)
                        {
                            if (spinDashColliders[k].TryGetComponent(out Hitbox hitbox))
                            {
                                CombatBehaviour combatBehaviour = hitbox.GetCombatBehaviour(0);
                                if (combatBehaviour)
                                {
                                    bool flag = false;
                                    using (List<Unit_LibraryGuard.Attacked>.Enumerator enumerator = __instance.attackedList.GetEnumerator())
                                    {
                                        while (enumerator.MoveNext())
                                        {
                                            if (enumerator.Current.combatBehaviour == combatBehaviour)
                                            {
                                                flag = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (!flag)
                                    {
                                        DamageInstance damage = DamageInstance.GetDamage(
                                            __instance, "SpinDash", __instance.transform.position, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack),
                                            __instance.RequestStandardDamage(), EDamageType.Slice, EDamageFromType.DirectAttack, __instance.spinDashDirection, 25, 1f);
                                        combatBehaviour.ApplyDamage(damage);
                                        __instance.attackedList.Add(new Unit_LibraryGuard.Attacked
                                        {
                                            timer = 0f,
                                            combatBehaviour = combatBehaviour
                                        });
                                    }
                                }
                            }
                        }
                    }
                    if (__instance.spinDashTimer.Update(Time.deltaTime))
                    {
                        spinDashPhaseField?.SetValue(__instance, 2);
                    }
                }
                else if (spinDashPhase == 2)
                {
                    __instance.NetworkspinDashSpeed = Mathf.MoveTowards(__instance.spinDashSpeed, 0f, 20f * Time.deltaTime);
                    if (__instance.spinDashSpeed <= 0.01f)
                    {
                        spinDashPhaseField?.SetValue(__instance, 0);
                        __instance.NetworkzoomOut = false;
                        __instance.NetworkcurrentState = Unit_LibraryGuard.EState.Idle;
                    }
                }
                if (transformAuthority)
                {
                    if (__instance.TopdownRigidbody.collisions.left && __instance.spinDashDirection.x < 0f)
                    {
                        __instance.spinDashDirection.x = Mathf.Abs(__instance.spinDashStartDirection.x);
                        __instance.SpinDashWallCollide();
                        WallCollideShoot(__instance, "left");
                    }
                    else if (__instance.TopdownRigidbody.collisions.right && __instance.spinDashDirection.x > 0f)
                    {
                        __instance.spinDashDirection.x = -Mathf.Abs(__instance.spinDashStartDirection.x);
                        __instance.SpinDashWallCollide();
                        WallCollideShoot(__instance, "right");
                    }
                    if (__instance.TopdownRigidbody.collisions.below && __instance.spinDashDirection.y < 0f)
                    {
                        __instance.spinDashDirection.y = Mathf.Abs(__instance.spinDashStartDirection.y);
                        __instance.SpinDashWallCollide();
                        WallCollideShoot(__instance, "below");
                    }
                    else if (__instance.TopdownRigidbody.collisions.above && __instance.spinDashDirection.y > 0f)
                    {
                        __instance.spinDashDirection.y = -Mathf.Abs(__instance.spinDashStartDirection.y);
                        __instance.SpinDashWallCollide();
                        WallCollideShoot(__instance, "above");
                    }
                }
                if (transformAuthority && __instance.TopdownRigidbody != null)
                {
                    __instance.TopdownRigidbody.RequestMove(__instance.spinDashSpeed * __instance.spinDashDirection * Time.deltaTime);
                }
            }

            static void WallCollideShoot(Unit_LibraryGuard __instance, string wallDirection)
            {
                if (Hard < 3) return;

                float __instanceAngle = 0f;
                switch (wallDirection)
                {
                    case "left":
                        __instanceAngle = 0f;
                        break;
                    case "right":
                        __instanceAngle = 180f;
                        break;
                    case "below":
                        __instanceAngle = 90f;
                        break;
                    case "above":
                        __instanceAngle = 270f;
                        break;
                }
                float totalAngle = 90f;
                float startAngle = __instanceAngle - totalAngle / 2f;
                Vector2 pos = (Vector2)__instance.transform.position;
                int Stack = 3;
                if (Hard >= 4) Stack = 5;
                float angleStep = totalAngle / (Stack - 1);
                for (int i = 0; i < Stack; i++)
                {
                    float currentAngle = startAngle + angleStep * i;
                    Vector2 attackDirection = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad)).normalized;
                    Vector2 targetPos = pos + attackDirection * 10f;
                    Bullet.Pool.Spawn(__instance.zigzagBulletPrefab, pos, false, EDamageFromType.DirectAttack, "ZigzagBullet", __instance.RequestStandardDamage() * 0.5f, 0, 0f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0f, pos, targetPos, null, null);
                }
            }

            static void Chakram(Unit_LibraryGuard __instance)
            {
                if (chakramCollidersField == null)
                {
                    StarSephiriaMod.LogError("[Unit_LibraryGuard][Chakram]获取反射失败");
                    return;
                }

                var chakramColliders = (Collider2D[])chakramCollidersField.GetValue(null);
                __instance.NetworkcurrentChakramAngle = __instance.currentChakramAngle + Time.deltaTime * 1080f;
                if (__instance.currentChakramAngle > 360f)
                {
                    __instance.NetworkcurrentChakramAngle = __instance.currentChakramAngle - 360f;
                }
                if (__instance.chakramState == 0)
                {
                    if (Hard < 3)
                    {
                        Vector3 Vec = __instance.chakramTargetPosition - __instance.transform.position;
                        __instance.TopdownRigidbody.RequestMove(Vec * __instance.chakramMoveSpeed * Time.deltaTime);
                    }
                    if (__instance.chakramMoveTimer.Update(Time.deltaTime))
                    {
                        __instance.NetworkchakramState = 1;
                    }
                }
                else if (__instance.chakramState == 1)
                {
                    Vector3 Vec = __instance.chakramTargetPosition - __instance.transform.position;
                    float SpeedMult = 0.25f;
                    if (Hard >= 3) SpeedMult = 0.1f;
                    if (Hard >= 4) SpeedMult += 0.05f;
                    __instance.TopdownRigidbody.RequestMove(Vec * __instance.chakramMoveSpeed * SpeedMult * Time.deltaTime);

                    __instance.NetworkcurrentChakramRadius = __instance.currentChakramRadius + Time.deltaTime * 2f;
                    if (__instance.currentChakramRadius > 3f)
                    {
                        __instance.NetworkcurrentChakramRadius = 3f;
                    }
                    for (int l = 0; l < 3; l++)
                    {
                        Vector2 vector2FromAngle = HorayUtility.GetVector2FromAngle(__instance.currentChakramAngle + l * 120f);
                        vector2FromAngle.y *= 0.625f;
                        int MaxColliders = HorayPhysics2D.OverlapCircle((Vector2)__instance.chakramCenterPoint.position + vector2FromAngle * __instance.currentChakramRadius, 0.5f, chakramColliders, CombatManager.Topdown1FLayerMask);
                        for (int m = 0; m < MaxColliders; m++)
                        {
                            if (chakramColliders[m].TryGetComponent(out Hitbox hitbox2))
                            {
                                CombatBehaviour combatBehaviour2 = hitbox2.GetCombatBehaviour(0);
                                bool flag2 = false;
                                using (List<Unit_LibraryGuard.Attacked>.Enumerator enumerator = __instance.attackedList.GetEnumerator())
                                {
                                    while (enumerator.MoveNext())
                                    {
                                        if (enumerator.Current.combatBehaviour == combatBehaviour2)
                                        {
                                            flag2 = true;
                                            break;
                                        }
                                    }
                                }
                                if (!flag2)
                                {
                                    DamageInstance damage2 = DamageInstance.GetDamage(
                                        __instance, "Chakram", __instance.transform.position, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack),
                                        __instance.RequestStandardDamage(), EDamageType.Slice, EDamageFromType.DirectAttack, __instance.spinDashDirection, 25, 1f);
                                    combatBehaviour2.ApplyDamage(damage2);
                                    __instance.attackedList.Add(new Unit_LibraryGuard.Attacked
                                    {
                                        timer = 0f,
                                        combatBehaviour = combatBehaviour2
                                    });
                                }
                            }
                        }
                    }
                    if (__instance.chakramEndTimer.Update(Time.deltaTime))
                    {
                        __instance.NetworkchakramState = 2;
                    }
                }
                else if (__instance.chakramState == 2)
                {
                    __instance.NetworkcurrentChakramRadius = __instance.currentChakramRadius - Time.deltaTime * 5f;
                    if (__instance.currentChakramRadius < 0.01f)
                    {
                        __instance.NetworkcurrentChakramRadius = 0.01f;
                        __instance.NetworkcurrentState = Unit_LibraryGuard.EState.Idle;
                    }
                }
            }

            [HarmonyPatch("FireBallAnimation")]
            [HarmonyPrefix]
            static bool FireBallAnimation_Prefix(Unit_LibraryGuard __instance)
            {
                if (Hard <= 0) return true;
                try
                {
                    if (fireBallPositionField == null)
                    {
                        StarSephiriaMod.LogError("[Unit_LibraryGuard][FireBallAnimation]获取反射失败");
                        return true;
                    }
                    Vector3 fireBallPosition = (Vector3)fireBallPositionField.GetValue(__instance);
                    if (__instance.isServer)
                    {
                        int Stack = 0;
                        if (Hard >= 1) Stack += 1;
                        if (Hard >= 2) Stack += 2;
                        Vector3 Rot = HorayUtility.GetVector3FromAngle(45f);
                        if (Random.value <= 0.5f)
                        {
                            Rot = HorayUtility.GetVector3FromAngle(135f);
                        }
                        if (Hard >= 4) Rot = HorayUtility.GetVector3FromAngle(90f);
                        for (int i = -Stack; i <= Stack; i++)
                        {
                            Vector3 EndPos = fireBallPosition;
                            EndPos += i * Rot * 6f;
                            Bullet.Pool.Spawn(__instance.fireBallPrefab, __instance.transform.position, false, EDamageFromType.DirectAttack, "BasicAttack",
                                __instance.RequestStandardDamage(), 25, 1f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0.5f,
                                __instance.transform.position, EndPos, null, null, 0f, EDamageElementalType.Physical, true);
                            Object.Instantiate(__instance.fireBallAreaPrefab, EndPos, Quaternion.identity);
                        }
                    }
                    if (__instance.isClient) __instance.GetComponent<BoingToHit>().ClientBoing();
                    __instance.EndFireBallAnimation();
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[Unit_LibraryGuard][FireBallAnimation]出现错误{ex}");
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(UnitAI_LibraryGuard))]
        public static class UnitAI_LibraryGuard_Patch
        {
            [HarmonyPatch("Awake")]
            [HarmonyPostfix]
            static void Awake_Postfix(UnitAI_LibraryGuard __instance)
            {
                if (Hard <= 0) return;
                if (patternQueueField == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_LibraryGuard][Awake]反射获取失败");
                    return;
                }
                patternQueueField.SetValue(__instance, new List<int>());
            }

            private static int Oldpattern = -1;
            [HarmonyPatch("OnAIUpdate_FoundEnemy")]
            [HarmonyPrefix]
            static bool OnAIUpdate_FoundEnemy_Prefix(UnitAI_LibraryGuard __instance)
            {
                if (Hard <= 0) return true;
                if (patternQueueField == null || unitField == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_LibraryGuard][OnAIUpdate_FoundEnemy]反射获取失败");
                    return true;
                }

                Unit_LibraryGuard? unit = unitField.GetValue(__instance) as Unit_LibraryGuard;
                if (unit == null || !(patternQueueField.GetValue(__instance) is List<int> patternQueue))
                {
                    StarSephiriaMod.LogError("[UnitAI_LibraryGuard][OnAIUpdate_FoundEnemy]反射获取错误");
                    return true;
                }

                float SpeedMult = 0.85f + 0.15f * Hard;
                if (__instance.orderedExplosionTimer.Update(Time.deltaTime * SpeedMult) && __instance.Environment && __instance.hardPatternActivated)
                {
                    unit.PatternOrderedExplosion(__instance.Environment.BossZoneBottomLeft, __instance.Environment.BossZoneTopRight);
                }
                if (unit.currentState == Unit_LibraryGuard.EState.Idle)
                {
                    unit.Stop();
                    if (__instance.patternQueueTimer.Update(Time.deltaTime))
                    {
                        if (patternQueue.Count == 0)
                        {
                            patternQueue.Add(0);
                            patternQueue.Add(1);
                            if (Hard < 3) patternQueue.Add(2);
                            patternQueue.Add(3);
                            patternQueue.Add(4);
                            patternQueue.Add(5);
                            patternQueue.Shuffle();
                            if (Oldpattern >= 0)
                            {
                                patternQueue.Remove(Oldpattern);
                                patternQueue.Insert(patternQueue.Count / 2, Oldpattern);
                            }
                        }
                        int pattern = patternQueue[0];
                        patternQueue.RemoveAt(0);
                        Oldpattern = pattern;
                        BossEnvironment_LibraryGuard? bossEnvironment_LibraryGuard = null;
                        if (__instance.Environment != null) bossEnvironment_LibraryGuard = __instance.Environment as BossEnvironment_LibraryGuard;
                        StarSephiriaMod.LogInfoBoss($"故障机器人当前攻击：{pattern}");
                        switch (pattern)
                        {
                            case 1:
                                {
                                    var vector = Random.Range(0, 4) switch
                                    {
                                        0 => new Vector2(Random.Range(0.7f, 1f), Random.Range(0.7f, 1f)),
                                        1 => new Vector2(Random.Range(-1f, -0.7f), Random.Range(0.7f, 1f)),
                                        2 => new Vector2(Random.Range(-1f, -0.7f), Random.Range(-1f, -0.7f)),
                                        _ => new Vector2(Random.Range(0.7f, 1f), Random.Range(-1f, -0.7f)),
                                    };
                                    Vector2 vector2 = vector;
                                    unit.PatternSpinDash(vector2, __instance.CurrentTarget);
                                    return false;
                                }
                            case 2:
                                if (__instance.Environment != null)
                                {
                                    unit.PatternLaser(__instance.CurrentTarget, __instance.Environment.BossZoneCenter + (Vector3)Random.insideUnitCircle * 3f);
                                    return false;
                                }
                                unit.PatternLaser(__instance.CurrentTarget, __instance.transform.position);
                                return false;
                            case 3:
                                unit.PatternJumpAttack(3, __instance.CurrentTarget);
                                return false;
                            case 4:
                                {
                                    Vector3 vector3 = __instance.transform.position;
                                    for (int i = 0; i < 100; i++)
                                    {
                                        vector3 = __instance.CurrentTarget.transform.position + (Vector3)Random.insideUnitCircle.normalized * 3f;
                                        if (Physics2D.OverlapBox(vector3, new Vector2(2.5f, 2f), 0f) == null)
                                        {
                                            break;
                                        }
                                    }
                                    if (Hard >= 3)
                                    {
                                        if (__instance.Environment != null)
                                            unit.PatternLaser(__instance.CurrentTarget, __instance.Environment.BossZoneCenter + (Vector3)Random.insideUnitCircle * 3f);
                                        else
                                            unit.PatternLaser(__instance.CurrentTarget, __instance.transform.position);
                                        unit.NetworkcurrentState = unit.currentState = Unit_LibraryGuard.EState.Idle;
                                    }
                                    unit.PatternChakram(vector3);
                                    return false;
                                }
                            case 5:
                                unit.PatternFireBall(__instance.CurrentTarget.transform.position);
                                return false;
                            default:
                                if (bossEnvironment_LibraryGuard != null)
                                {
                                    unit.PatternZigzagBullet(bossEnvironment_LibraryGuard.zigzagLeft, bossEnvironment_LibraryGuard.zigzagRight);
                                    return false;
                                }
                                unit.PatternFireBall(__instance.CurrentTarget.transform.position);
                                break;
                        }
                    }
                    return false;
                }
                if (unit.currentState == Unit_LibraryGuard.EState.Chakram && unit.chakramState == 1)
                {
                    unit.chakramTargetPosition = __instance.CurrentTarget.transform.position;
                    return false;
                }
                if (unit.currentState == Unit_LibraryGuard.EState.Laser && unit.laserPhase == 1)
                {
                    unit.laserMoveTargetPosition = __instance.CurrentTarget.transform.position;
                    return false;
                }
                unit.Stop();
                return false;
            }
        }
    }
}