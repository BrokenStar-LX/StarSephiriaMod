using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SPMod.MonsterAI
{
    [HarmonyPatch]
    internal static class SummonedSamurai
    {
        private static readonly FieldInfo CurrentUnitField = AccessTools.Field(typeof(Charm_SummonUnit), "currentUnit");
        private static readonly FieldInfo TargetField = AccessTools.Field(typeof(UI_UnitHPBar), "target");
        [ThreadStatic] private static Charm_SummonUnit? CurrentSummon;

        [HarmonyPatch(typeof(Charm_SummonUnit), "Summon")]
        [HarmonyPrefix]
        private static void Summon_Prefix(Charm_SummonUnit __instance, out Charm_SummonUnit? __state)
        {
            __state = CurrentSummon;
            CurrentSummon = __instance;
        }

        [HarmonyPatch(typeof(Charm_SummonUnit), "Summon")]
        [HarmonyFinalizer]
        private static Exception? Summon_Finalizer(Exception? __exception, Charm_SummonUnit? __state)
        {
            CurrentSummon = __state;
            return __exception;
        }

        [HarmonyPatch(typeof(UnitAvatar), "OnStartServer")]
        [HarmonyPostfix]
        private static void OnStartServer_Postfix(UnitAvatar __instance)
        {
            var Summon = CurrentSummon;
            if (!Summon || __instance is not Unit_SamuraiDemon ||
                CurrentUnitField.GetValue(Summon) is not UnitAI_NewBasic Current ||
                !ReferenceEquals(Current.Avatar, __instance) ||
                Summon!.NetworkAvatar is not PlayerAvatar Player || !Player) return;

            // 在 Mirror 发送初始生成数据前设置召唤者关系，主机与客机可使用同一判定。
            __instance.ChangeFaction(Player.faction);
            __instance.SetLeader(Player, true);
        }

        [HarmonyPatch(typeof(UI_MiniBossHPBar), "LateUpdate")]
        [HarmonyPrefix]
        private static bool LateUpdate_Prefix(UI_MiniBossHPBar __instance, CanvasGroup ___canvas)
        {
            if (TargetField.GetValue(__instance) is not Unit_SamuraiDemon Unit || !Unit ||
                Unit.NetworkLeader is not PlayerAvatar Player || !Player || !___canvas) return true;

            ___canvas.alpha = 0;
            return false;
        }
    }
}
