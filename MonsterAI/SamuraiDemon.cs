using HarmonyLib;

namespace SPMod.MonsterAI
{
    [HarmonyPatch]
    public static class SamuraiDemon
    {
        [HarmonyPatch(typeof(Unit_SamuraiDemon))]
        public static class Unit_SamuraiDemon_Patch
        {
            [HarmonyPatch("CheckGrabDistance")]
            [HarmonyPrefix]
            static void CheckGrabDistance_Prefix(Unit_SamuraiDemon __instance)
            {
                if (__instance.Leader is PlayerAvatar)
                    __instance.tryGrabDistance = 999f;
            }
        }
    }
}