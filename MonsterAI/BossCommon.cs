using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SPMod.MonsterAI
{
    /// <summary>
    /// 各 Boss 文件共用的逻辑，避免每个 Boss 各写一份。
    /// </summary>
    internal static class BossCommon
    {
        private static readonly AccessTools.FieldRef<UnitAvatar, bool>? IsHitInvincibleEnabledRef;
        private static readonly AccessTools.FieldRef<UnitAvatar, float>? InvincibleTimerRef;
        private static readonly AccessTools.FieldRef<UnitAvatar, bool>? IsReviveInvulnerableRef;
        private static readonly AccessTools.FieldRef<UnitAvatar, bool>? IsTempInvulnerableRef;
        private static readonly AccessTools.FieldRef<UnitAvatar, Timer>? TempInvulnerableTimerRef;
        private static readonly AccessTools.FieldRef<UnitAvatar, Timer>? StunCooldownTimerRef;
        private static readonly bool Ready;
        private static readonly Timer StunTimer = new(1f);
        private static readonly Timer HitRestoreTimer = new(0.75f);

        static BossCommon()
        {
            Type type = typeof(UnitAvatar);
            IsHitInvincibleEnabledRef = ReflectionCache.FieldRef<UnitAvatar, bool>(type, "isHitInvincibleEnabled");
            InvincibleTimerRef = ReflectionCache.FieldRef<UnitAvatar, float>(type, "invincibleTimer");
            IsReviveInvulnerableRef = ReflectionCache.FieldRef<UnitAvatar, bool>(type, "isReviveInvulnerable");
            IsTempInvulnerableRef = ReflectionCache.FieldRef<UnitAvatar, bool>(type, "isTempInvulnerable");
            TempInvulnerableTimerRef = ReflectionCache.FieldRef<UnitAvatar, Timer>(type, "tempInvulnerableTimer");
            StunCooldownTimerRef = ReflectionCache.FieldRef<UnitAvatar, Timer>(type, "stunCooldownTimer");
            Ready = IsHitInvincibleEnabledRef != null && InvincibleTimerRef != null && IsReviveInvulnerableRef != null
                && IsTempInvulnerableRef != null && TempInvulnerableTimerRef != null && StunCooldownTimerRef != null;
        }

        /// <summary>
        /// 还原 UnitAvatar.Update 里的击晕/受击卡滞/无敌计时器逻辑。
        /// </summary>
        public static void UpdateUnitAvatarTimers(UnitAvatar __instance)
        {
            if (!__instance.isServer) return;

            if (__instance.isInStun && StunTimer.Update(Time.deltaTime))
            {
                __instance.StopStun();
            }

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

            if (!Ready)
            {
                StarSephiriaMod.LogError("[UnitAvatar][InstanceUpdate]获取反射失败");
                return;
            }

            if (IsHitInvincibleEnabledRef!(__instance))
            {
                ref float InvincibleTimer = ref InvincibleTimerRef!(__instance);
                InvincibleTimer += Time.deltaTime;
                if (InvincibleTimer >= __instance.invincibleTime)
                {
                    InvincibleTimer = 0f;
                    IsHitInvincibleEnabledRef!(__instance) = false;
                }
            }

            if (IsReviveInvulnerableRef!(__instance) && __instance.reviveInvulnerableTimer.Update(Time.deltaTime))
            {
                IsReviveInvulnerableRef!(__instance) = false;
            }

            if (IsTempInvulnerableRef!(__instance) && TempInvulnerableTimerRef!(__instance).Update(Time.deltaTime))
            {
                IsTempInvulnerableRef!(__instance) = false;
            }

            if (__instance.stunCooldown && StunCooldownTimerRef!(__instance).Update(Time.deltaTime))
            {
                __instance.stunCooldown = false;
            }
        }

        /// <summary>
        /// 随机取一个未死亡的玩家。
        /// </summary>
        public static bool TryGetRandomAlivePlayer(out PlayerAvatar? player)
        {
            List<PlayerAvatar> list = [];
            foreach (PlayerSpawner spawner in PlayerSpawner.MultiplayerList)
            {
                if (spawner && spawner.PlayerAvatar && !spawner.PlayerAvatar.IsDead)
                {
                    list.Add(spawner.PlayerAvatar);
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
    }
}
