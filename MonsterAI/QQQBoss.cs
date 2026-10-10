using HarmonyLib;
using Mirror;
using SPMod.Tool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static Unit_QQQBoss;
using static UnityEngine.UI.GridLayoutGroup;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace SPMod.MonsterAI
{
    [HarmonyPatch]
    public static class QQQBoss
    {
        public static int Hard
        {
            get { return ModConfig.HardMode; }
        }
        private static readonly FieldInfo? currentPatternCoroutineField;
        private static readonly FieldInfo? qqqBossField;
        private static readonly FieldInfo? obeliskSummonerTryTimerField;

        private static readonly MethodInfo? UpdateObeliskSummonerAvailabilityMethod;
        private static readonly MethodInfo? ObeliskSummonerPatternCoroutineMethod;
        private static readonly MethodInfo? CircleAttackPatternCoroutineMethod;
        private static readonly MethodInfo? LaserAttackPatternCoroutineMethod;
        private static readonly MethodInfo? SummonUnitPatternCoroutineMethod;
        private static readonly MethodInfo? InAndOutAttackPatternCoroutineMethod;
        private static readonly MethodInfo? IssenAttackPatternCoroutineMethod;
        private static readonly MethodInfo? DarknessPatternCoroutineMethod;
        private static readonly MethodInfo? FallGuysPatternCoroutineMethod;
        private static readonly MethodInfo? LastPatternCoroutineMethod;

        private static readonly FieldInfo? lastPatternOneShotLaserCoroutineField;
        private static readonly FieldInfo? patternTargetField;
        private static readonly FieldInfo? laserAttackCoroutineField;
        private static readonly FieldInfo? isLastPatternReadyField;
        private static readonly FieldInfo? issenAttackCoroutineField;
        private static readonly FieldInfo? darknessOuterDamageCoroutineField;
        private static readonly FieldInfo? circleAttackCoroutineField;
        private static readonly FieldInfo? circleAttackWarningCoroutineField;
        private static readonly FieldInfo? circleAttackWarningsField;
        private static readonly FieldInfo? bossEnvironmentOnServerField;
        private static readonly FieldInfo? currentInAndOutCircleField;
        private static readonly FieldInfo? inAndOutAttackCoroutineField;
        private static readonly FieldInfo? fallGuysShieldAppliedField;
        private static readonly FieldInfo? fallGuysCellRegionMapField;
        private static readonly FieldInfo? fallGuysUsedRegionIdsField;
        private static readonly FieldInfo? summonUnitCoroutineField;

        private static readonly MethodInfo? LastPatternOneShotLaserCoroutineMethod;
        private static readonly MethodInfo? AttackLaserMethod;
        private static readonly MethodInfo? RpcCreateLaserAttackFxMethod;
        private static readonly MethodInfo? RpcCreateLaserAttackWarningMethod;
        private static readonly MethodInfo? RpcClearIssenAttackMethod;
        private static readonly MethodInfo? StopIssenDamageAttacksMethod;
        private static readonly MethodInfo? RpcCreateIssenAttackWarningMethod;
        private static readonly MethodInfo? RpcStartIssenAttackMethod;
        private static readonly MethodInfo? RpcPlayIssenAttackStartSoundMethod;
        private static readonly MethodInfo? RpcPlayIssenAttackFxMethod;
        private static readonly MethodInfo? StartIssenDamageAttackMethod;
        private static readonly MethodInfo? IssenExtraAttackCoroutineMethod;
        private static readonly MethodInfo? DarknessOuterDamageCoroutineMethod;
        private static readonly MethodInfo? StartDarknessMoveMethod;
        private static readonly MethodInfo? EndDarknessAttackMethod;
        private static readonly MethodInfo? RpcCreateLastPatternOneShotLaserWarningMethod;
        private static readonly MethodInfo? AttackLastPatternOneShotLaserMethod;
        private static readonly MethodInfo? RpcSpawnLastPatternOneShotLaserFxMethod;
        private static readonly MethodInfo? AttackDonutMethod;
        private static readonly MethodInfo? CreateCircleAttackWarningMethod;
        private static readonly MethodInfo? SpawnObeliskSummonerMethod;
        private static readonly MethodInfo? GetInAndOutOuterWarningTimeMethod;
        private static readonly MethodInfo? RpcCreateInAndOutOuterWarningMethod;
        private static readonly MethodInfo? AttackInAndOutOuterAreaMethod;
        private static readonly MethodInfo? RpcCreateInAndOutInnerWarningMethod;
        private static readonly MethodInfo? AttackInAndOutInnerAreaMethod;
        private static readonly MethodInfo? EndFallGuysAttackMethod;
        private static readonly MethodInfo? ActivateFallGuysRegionMethod;
        private static readonly MethodInfo? ClearFallGuysDamageZonesMethod;
        private static readonly MethodInfo? StartFallGuysPerimeterBulletsMethod;
        static QQQBoss()
        {
            Type type = typeof(UnitAI_QQQBoss);
            currentPatternCoroutineField = AccessTools.Field(type, "currentPatternCoroutine");
            qqqBossField = AccessTools.Field(type, "qqqBoss");
            obeliskSummonerTryTimerField = AccessTools.Field(type, "obeliskSummonerTryTimer");

            UpdateObeliskSummonerAvailabilityMethod = AccessTools.Method(type, "UpdateObeliskSummonerAvailability");
            ObeliskSummonerPatternCoroutineMethod = AccessTools.Method(type, "ObeliskSummonerPatternCoroutine");
            CircleAttackPatternCoroutineMethod = AccessTools.Method(type, "CircleAttackPatternCoroutine");
            LaserAttackPatternCoroutineMethod = AccessTools.Method(type, "LaserAttackPatternCoroutine");
            SummonUnitPatternCoroutineMethod = AccessTools.Method(type, "SummonUnitPatternCoroutine");
            InAndOutAttackPatternCoroutineMethod = AccessTools.Method(type, "InAndOutAttackPatternCoroutine");
            IssenAttackPatternCoroutineMethod = AccessTools.Method(type, "IssenAttackPatternCoroutine");
            DarknessPatternCoroutineMethod = AccessTools.Method(type, "DarknessPatternCoroutine");
            FallGuysPatternCoroutineMethod = AccessTools.Method(type, "FallGuysPatternCoroutine");
            LastPatternCoroutineMethod = AccessTools.Method(type, "LastPatternCoroutine");

            type = typeof(Unit_QQQBoss);
            lastPatternOneShotLaserCoroutineField = AccessTools.Field(type, "lastPatternOneShotLaserCoroutine");
            patternTargetField = AccessTools.Field(type, "patternTarget");
            laserAttackCoroutineField = AccessTools.Field(type, "laserAttackCoroutine");
            isLastPatternReadyField = AccessTools.Field(type, "isLastPatternReady");
            issenAttackCoroutineField = AccessTools.Field(type, "issenAttackCoroutine");
            darknessOuterDamageCoroutineField = AccessTools.Field(type, "darknessOuterDamageCoroutine");
            circleAttackCoroutineField = AccessTools.Field(type, "circleAttackCoroutine");
            circleAttackWarningCoroutineField = AccessTools.Field(type, "circleAttackWarningCoroutine");
            circleAttackWarningsField = AccessTools.Field(type, "circleAttackWarnings");
            bossEnvironmentOnServerField = AccessTools.Field(type, "bossEnvironmentOnServer");
            currentInAndOutCircleField = AccessTools.Field(type, "currentInAndOutCircle");
            inAndOutAttackCoroutineField = AccessTools.Field(type, "inAndOutAttackCoroutine");
            fallGuysShieldAppliedField = AccessTools.Field(type, "fallGuysShieldApplied");
            fallGuysCellRegionMapField = AccessTools.Field(type, "fallGuysCellRegionMap");
            fallGuysUsedRegionIdsField = AccessTools.Field(type, "fallGuysUsedRegionIds");
            summonUnitCoroutineField = AccessTools.Field(type, "summonUnitCoroutine");

            LastPatternOneShotLaserCoroutineMethod = AccessTools.Method(type, "LastPatternOneShotLaserCoroutine");
            AttackLaserMethod = AccessTools.Method(type, "AttackLaser");
            RpcCreateLaserAttackFxMethod = AccessTools.Method(type, "RpcCreateLaserAttackFx");
            RpcCreateLaserAttackWarningMethod = AccessTools.Method(type, "RpcCreateLaserAttackWarning");
            RpcClearIssenAttackMethod = AccessTools.Method(type, "RpcClearIssenAttack");
            StopIssenDamageAttacksMethod = AccessTools.Method(type, "StopIssenDamageAttacks");
            RpcCreateIssenAttackWarningMethod = AccessTools.Method(type, "RpcCreateIssenAttackWarning");
            RpcStartIssenAttackMethod = AccessTools.Method(type, "RpcStartIssenAttack");
            RpcPlayIssenAttackStartSoundMethod = AccessTools.Method(type, "RpcPlayIssenAttackStartSound");
            RpcPlayIssenAttackFxMethod = AccessTools.Method(type, "RpcPlayIssenAttackFx");
            StartIssenDamageAttackMethod = AccessTools.Method(type, "StartIssenDamageAttack");
            IssenExtraAttackCoroutineMethod = AccessTools.Method(type, "IssenExtraAttackCoroutine");
            DarknessOuterDamageCoroutineMethod = AccessTools.Method(type, "DarknessOuterDamageCoroutine");
            StartDarknessMoveMethod = AccessTools.Method(type, "StartDarknessMove");
            EndDarknessAttackMethod = AccessTools.Method(type, "EndDarknessAttack");
            RpcCreateLastPatternOneShotLaserWarningMethod = AccessTools.Method(type, "RpcCreateLastPatternOneShotLaserWarning");
            AttackLastPatternOneShotLaserMethod = AccessTools.Method(type, "AttackLastPatternOneShotLaser");
            RpcSpawnLastPatternOneShotLaserFxMethod = AccessTools.Method(type, "RpcSpawnLastPatternOneShotLaserFx");
            AttackDonutMethod = AccessTools.Method(type, "AttackDonut");
            CreateCircleAttackWarningMethod = AccessTools.Method(type, "CreateCircleAttackWarning");
            SpawnObeliskSummonerMethod = AccessTools.Method(type, "SpawnObeliskSummoner");
            GetInAndOutOuterWarningTimeMethod = AccessTools.Method(type, "GetInAndOutOuterWarningTime");
            RpcCreateInAndOutOuterWarningMethod = AccessTools.Method(type, "RpcCreateInAndOutOuterWarning");
            AttackInAndOutOuterAreaMethod = AccessTools.Method(type, "AttackInAndOutOuterArea");
            RpcCreateInAndOutInnerWarningMethod = AccessTools.Method(type, "RpcCreateInAndOutInnerWarning");
            AttackInAndOutInnerAreaMethod = AccessTools.Method(type, "AttackInAndOutInnerArea");
            EndFallGuysAttackMethod = AccessTools.Method(type, "EndFallGuysAttack");
            ActivateFallGuysRegionMethod = AccessTools.Method(type, "ActivateFallGuysRegion");
            ClearFallGuysDamageZonesMethod = AccessTools.Method(type, "ClearFallGuysDamageZones");
            StartFallGuysPerimeterBulletsMethod = AccessTools.Method(type, "StartFallGuysPerimeterBullets");
        }

        private static int UsePatternType = 0;
        public static QQQBossObelisk? obeliskPrefab = null;
        private static float inAndOutOuterSafeRadius = 0f;
        private static float OrigLastSpeed = 0f;
        private static float LastSpeed = 0f;

        private static Vector2 GetBossZoneCenterPosition(Unit_QQQBoss Boss)
        {
            SearchablePositionProp searchablePositionProp = SearchablePositionProp.FindByGuid("BossZoneCenter");
            if (searchablePositionProp)
            {
                return searchablePositionProp.transform.position;
            }
            return Boss.transform.position;
        }

        [HarmonyPatch(typeof(UnitAI_QQQBoss))]
        public static class UnitAI_QQQBoss_Patch
        {
            private static int Oldpattern = -1;
            private static bool Start = false;
            private static Dictionary<int, List<int>> PatternType = new Dictionary<int, List<int>>();
            private static Dictionary<int, int> LastUsePatternType = new Dictionary<int, int>();

            [HarmonyPatch("Awake")]
            [HarmonyPostfix]
            static void Awake_Postfix(UnitAI_QQQBoss __instance)
            {
                Oldpattern = -1;
                Start = false;
                PatternType = ReStartPatternType();
                LastUsePatternType = new Dictionary<int, int>
                {
                    { 0, -1 },
                    { 1, -1 },
                    { 2, -1 },
                    { 3, -1 },
                    { 4, -1 },
                    { 5, -1 },
                    { 6, -1 },
                };
                UsePatternType = 0;
                LastSpeed = 0f;
                OrigLastSpeed = 0f;
            }

            [HarmonyPatch("StartLaserAttack_Opening")]
            [HarmonyPrefix]
            static bool StartLaserAttack_Opening_Postfix(UnitAI_QQQBoss __instance)
            {
                if (Hard < 1) return true;
                if (qqqBossField != null)
                {
                    Unit_QQQBoss? qqqBoss = qqqBossField.GetValue(__instance) as Unit_QQQBoss;
                    qqqBoss?.SetBodyType(EBodyType.None);
                }
                return false;
            }

            private static Dictionary<int, List<int>> ReStartPatternType()
            {
                Dictionary<int, List<int>> ReStartPattern = new Dictionary<int, List<int>>();
                for (int i = 0; i < 7; i++)
                {
                    List<int> Rand;
                    if (Hard <= 1) Rand = new List<int>() { 1 };
                    else Rand = new List<int>() { 1, 2, 3 };
                    Rand.Shuffle();
                    ReStartPattern.Add(i, Rand);
                }
                return ReStartPattern;
            }

            [HarmonyPatch("OnAIUpdate_FoundEnemy")]
            [HarmonyPrefix]
            static bool OnAIUpdate_FoundEnemy_Prefix(UnitAI_QQQBoss __instance)
            {
                if (Hard <= 0) return true;
                try
                {
                    if (currentPatternCoroutineField == null ||
                        qqqBossField == null ||
                        obeliskSummonerTryTimerField == null ||
                        UpdateObeliskSummonerAvailabilityMethod == null ||
                        ObeliskSummonerPatternCoroutineMethod == null ||
                        CircleAttackPatternCoroutineMethod == null ||
                        LaserAttackPatternCoroutineMethod == null ||
                        SummonUnitPatternCoroutineMethod == null ||
                        InAndOutAttackPatternCoroutineMethod == null ||
                        IssenAttackPatternCoroutineMethod == null ||
                        DarknessPatternCoroutineMethod == null ||
                        FallGuysPatternCoroutineMethod == null ||
                        LastPatternCoroutineMethod == null ||
                        lastPatternOneShotLaserCoroutineField == null ||
                        LastPatternOneShotLaserCoroutineMethod == null)
                    {
                        StarSephiriaMod.LogError("[UnitAI_QQQBoss][OnAIUpdate_FoundEnemy]获取反射失败");
                        return true;
                    }

                    UpdateObeliskSummonerAvailabilityMethod.Invoke(__instance, null);
                    if (currentPatternCoroutineField.GetValue(__instance) is Coroutine curCoroutine)
                    {
                        return false;
                    }

                    Unit_QQQBoss? qqqBoss = qqqBossField.GetValue(__instance) as Unit_QQQBoss;
                    if (qqqBoss == null || !(obeliskSummonerTryTimerField.GetValue(__instance) is Timer obeliskTimer)) return true;

                    if (qqqBoss.isInStun)
                    {
                        return true;
                    }
                    if (!Start && Hard >= 3 && __instance.isServer)
                    {
                        Start = true;
                        qqqBoss.AddCustomStatUnsafe(TextFile.ReceivedDamageReduction, 60);
                        IEnumerator? laserEnum = LastPatternOneShotLaserCoroutineMethod.Invoke(qqqBoss, null) as IEnumerator;
                        Coroutine laserCoro = qqqBoss.StartCoroutine(laserEnum);
                        lastPatternOneShotLaserCoroutineField.SetValue(qqqBoss, laserCoro);
                    }
                    float PatternSpeed = 0.75f + Hard * 0.25f;
                    if (qqqBoss.currentAction == EAnimation.None && __instance.patternTimer.Update(Time.deltaTime * PatternSpeed))
                    {
                        if (qqqBoss.ObeliskSummonerFreeSlotCount > 0 && obeliskTimer.Check())
                        {
                            IEnumerator? obeliskCoro = ObeliskSummonerPatternCoroutineMethod.Invoke(__instance, null) as IEnumerator;
                            Coroutine newCoro = __instance.StartCoroutine(obeliskCoro);
                            currentPatternCoroutineField.SetValue(__instance, newCoro);
                            obeliskTimer.SetTimer(0f);
                            obeliskTimer.time = 30f;
                            obeliskSummonerTryTimerField.SetValue(__instance, obeliskTimer);
                            return false;
                        }
                        if (__instance.patternList.Count == 0)
                        {
                            __instance.patternList.Add(0);
                            __instance.patternList.Add(1);
                            __instance.patternList.Add(2);
                            __instance.patternList.Add(3);
                            __instance.patternList.Add(4);
                            __instance.patternList.Add(5);
                            __instance.patternList.Add(6);
                            __instance.patternList.Shuffle();
                            if (Oldpattern >= 0)
                            {
                                __instance.patternList.Remove(Oldpattern);
                                __instance.patternList.Insert(3, Oldpattern);
                            }
                        }
                        int UsePattern = -1;
                        UsePattern = __instance.patternList[0];
                        Oldpattern = UsePattern;
                        if (qqqBoss.GetCustomStatUnsafe(TextFile.ReceivedDamageReduction) > 0)
                        {
                            int Down = Hard >= 4 ? 2 : 3;
                            qqqBoss.AddCustomStatUnsafe(TextFile.ReceivedDamageReduction, -Down);
                        }
                        StarSephiriaMod.LogInfoBoss($"终克2阶段当前技能：{UsePattern}");
                        if (UsePattern < 7)
                        {
                            if (PatternType.Count <= 0) PatternType = ReStartPatternType();
                            if (PatternType[UsePattern].Count <= 0)
                            {
                                List<int> Rand;
                                if (Hard <= 1) Rand = new List<int>() { 1 };
                                else
                                {
                                    Rand = new List<int>() { 1, 2, 3 };
                                }
                                Rand.Shuffle();
                                if (Rand.Count > 1 && LastUsePatternType[UsePattern] >= 0)
                                {
                                    Rand.Remove(LastUsePatternType[UsePattern]);
                                    Rand.Insert(1, LastUsePatternType[UsePattern]);
                                }
                                PatternType[UsePattern] = Rand;
                            }
                            if (PatternType[UsePattern].Count > 0)
                            {
                                UsePatternType = PatternType[UsePattern][0];
                                PatternType[UsePattern].RemoveAt(0);
                                LastUsePatternType[UsePattern] = UsePatternType;
                            }
                        }
                        __instance.patternList.RemoveAt(0);
                        IEnumerator? selectCoro = null;
                        switch (UsePattern)
                        {
                            case 0:
                                selectCoro = CircleAttackPatternCoroutineMethod.Invoke(__instance, null) as IEnumerator;
                                break;
                            case 1:
                                selectCoro = LaserAttackPatternCoroutineMethod.Invoke(__instance, null) as IEnumerator;
                                break;
                            case 2:
                                selectCoro = SummonUnitPatternCoroutineMethod.Invoke(__instance, null) as IEnumerator;
                                break;
                            case 3:
                                selectCoro = InAndOutAttackPatternCoroutineMethod.Invoke(__instance, null) as IEnumerator;
                                break;
                            case 4:
                                selectCoro = IssenAttackPatternCoroutineMethod.Invoke(__instance, null) as IEnumerator;
                                break;
                            case 5:
                                selectCoro = DarknessPatternCoroutineMethod.Invoke(__instance, null) as IEnumerator;
                                break;
                            case 6:
                                selectCoro = FallGuysPatternCoroutineMethod.Invoke(__instance, null) as IEnumerator;
                                if (UsePatternType == 3 && selectCoro != null) selectCoro = FallGuysPattern(__instance, selectCoro);
                                break;
                            case 7:
                                selectCoro = LastPatternCoroutineMethod.Invoke(__instance, null) as IEnumerator;
                                break;
                        }
                        if (selectCoro != null)
                        {
                            Coroutine started = __instance.StartCoroutine(selectCoro);
                            currentPatternCoroutineField.SetValue(__instance, started);
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[UnitAI_QQQBoss][OnAIUpdate_FoundEnemy]出现错误: {ex}");
                    return true;
                }
            }
            private static IEnumerator FallGuysPattern(UnitAI_QQQBoss Boss, IEnumerator Orig)
            {
                if (qqqBossField == null || currentPatternCoroutineField == null) yield break;

                Unit_QQQBoss? qqqBoss = qqqBossField.GetValue(Boss) as Unit_QQQBoss;
                if (qqqBoss == null) yield break;

                Vector2 bossZoneCenterPosition = GetBossZoneCenterPosition(qqqBoss);
                if (qqqBoss.FastMove(bossZoneCenterPosition, null, true))
                {
                    yield return new WaitWhile(() => qqqBoss.currentAction == EAnimation.FastMove);
                }
                currentPatternCoroutineField.SetValue(Boss, Boss.StartCoroutine(Orig));
            }

            [HarmonyPatch("SummonUnitPatternCoroutine")]
            [HarmonyPrefix]
            static bool SummonUnitPatternCoroutine_Prefix(UnitAI_QQQBoss __instance, ref IEnumerator __result)
            {
                if (PlayerSpawner.MultiplayerList.Count > 1) return true;
                if (!__instance.isServer) return true;
                if (Hard <= 0) return true;

                if (UsePatternType == 2)
                {
                    __result = SummonUnitPatternType2(__instance);
                    return false;
                }
                else if (UsePatternType == 3)
                {
                    __result = SummonUnitPatternType3(__instance);
                    return false;
                }
                return true;
            }
            private static IEnumerator SummonUnitPatternType2(UnitAI_QQQBoss Boss)
            {
                if (qqqBossField == null || currentPatternCoroutineField == null) yield break;

                Unit_QQQBoss? qqqBoss = qqqBossField.GetValue(Boss) as Unit_QQQBoss;
                if (qqqBoss == null) yield break;

                qqqBoss.SetBodyType(EBodyType.Green);
                yield return qqqBoss.WaitUntilBodyReady();

                float Rot = Random.Range(0f, 360f);
                float Mult = 0.4f + 0.6f * qqqBoss.hp / qqqBoss.MaxHp;
                Vector2 Cen = GetBossZoneCenterPosition(qqqBoss);
                int Max = 4;
                if (Mult < 0.8f) Max += 1;
                if (Mult < 0.6f) Max += 1;
                if (Hard >= 4) Max += 1;
                for (int i = 0; i < Max; i++)
                {
                    Vector2 Pos = Cen + HorayUtility.GetVector2FromAngle(360f / Max * i + Rot) * Random.Range(4f, 8f);
                    if (qqqBoss.FastMove(Pos, null, true))
                    {
                        yield return new WaitWhile(() => qqqBoss.currentAction == EAnimation.FastMove);
                    }
                    if (qqqBoss.currentAction == EAnimation.None)
                    {
                        qqqBoss.SummonUnit();
                        yield return new WaitWhile(() => qqqBoss.currentAction == EAnimation.SummonUnit);
                    }
                }
                yield return new WaitForSeconds(0.75f);
                qqqBoss.SetBodyType(EBodyType.None);
                currentPatternCoroutineField.SetValue(Boss, null);
            }
            private static IEnumerator SummonUnitPatternType3(UnitAI_QQQBoss Boss)
            {
                if (qqqBossField == null || currentPatternCoroutineField == null) yield break;

                Unit_QQQBoss? qqqBoss = qqqBossField.GetValue(Boss) as Unit_QQQBoss;
                if (qqqBoss == null) yield break;

                qqqBoss.SetBodyType(EBodyType.Green);
                yield return qqqBoss.WaitUntilBodyReady();

                Vector2 Cen = GetBossZoneCenterPosition(qqqBoss);
                if (qqqBoss.FastMove(Cen, null, true))
                {
                    yield return new WaitWhile(() => qqqBoss.currentAction == EAnimation.FastMove);
                }
                if (qqqBoss.currentAction == EAnimation.None)
                {
                    qqqBoss.SummonUnit();
                    yield return new WaitWhile(() => qqqBoss.currentAction == EAnimation.SummonUnit);
                }
                yield return new WaitForSeconds(0.75f);
                qqqBoss.SetBodyType(EBodyType.None);
                currentPatternCoroutineField.SetValue(Boss, null);
            }
        }

        [HarmonyPatch(typeof(Unit_QQQBoss))]
        public static class Unit_QQQBoss_Patch
        {
            private static bool TryGetRandomAlivePlayer(out PlayerAvatar? player)
            {
                List<PlayerAvatar> list = new List<PlayerAvatar>();
                foreach (PlayerSpawner playerSpawner in PlayerSpawner.MultiplayerList)
                {
                    if (playerSpawner && playerSpawner.PlayerAvatar && !playerSpawner.PlayerAvatar.IsDead)
                    {
                        list.Add(playerSpawner.PlayerAvatar);
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
            private static float LastPatternTime = 0f;

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            static void Update_Postfix(Unit_QQQBoss __instance)
            {
                if (__instance.NetworkcurrentAction == EAnimation.LastPattern && Hard >= 3)
                {
                    if (OrigLastSpeed > 0f)
                    {
                        if (LastPatternTime < 0.33f) LastPatternTime += Time.deltaTime;
                        else
                        {
                            __instance.AddCustomStatUnsafe(TextFile.ReceivedDamageReduction, -1);
                            LastPatternTime = 0f;
                        }
                        LastSpeed += Time.deltaTime;
                        float LastTime = Hard >= 4 ? 26f : 28f;
                        float Mult = MathF.Max(1f - LastSpeed / LastTime, 0.05f);
                        __instance.oneShotLaserInterval = OrigLastSpeed * Mult;
                    }
                }
                else if (OrigLastSpeed > 0f)
                    __instance.oneShotLaserInterval = OrigLastSpeed;

                if (inAndOutOuterSafeRadius <= 0f)
                    inAndOutOuterSafeRadius = __instance.inAndOutOuterSafeRadius;

                if (Hard >= 1)
                {
                    __instance.inAndOutRandomPosDistance = 5f;
                    __instance.summonUnitCount = 6;
                }
                else
                {
                    __instance.inAndOutRandomPosDistance = 4f;
                    __instance.summonUnitCount = 15;
                }
                __instance.circleAttackAdditionalWaitTime = 0.3f - Hard * 0.05f;
                if (Hard >= 1 && __instance.hp / __instance.MaxHp < 0.33f)
                {
                    __instance.circleAttackRepeatCount = UsePatternType == 3 ? 4 : 5;
                    __instance.laserAttackRepeatCount = 3;
                }
                else
                {
                    __instance.circleAttackRepeatCount = UsePatternType == 3 ? 3 : 4;
                    __instance.laserAttackRepeatCount = 2;
                }
            }

            [HarmonyPatch("SetReadyLastPattern")]
            [HarmonyPostfix]
            static void SetReadyLastPattern_Postfix(Unit_QQQBoss __instance)
            {
                if (OrigLastSpeed <= 0f && Hard >= 3)
                {
                    int Add = 99 - __instance.GetCustomStatUnsafe(TextFile.ReceivedDamageReduction);
                    __instance.AddCustomStatUnsafe(TextFile.ReceivedDamageReduction, Add);
                    OrigLastSpeed = __instance.oneShotLaserInterval;
                    LastPatternTime = 0f;
                }
            }
                
            [HarmonyPatch("StartReceivedDamageIncreaseServer")]
            [HarmonyPrefix]
            static void StartReceivedDamageIncreaseServer_Prefix(Unit_QQQBoss __instance)
            {
                __instance.receivedDamageIncreaseInterval = 10f;
                __instance.receivedDamageIncreaseAmount = 10 - Hard * 2;
            }

            [HarmonyPatch("LaserAttackCoroutine")]
            [HarmonyPrefix]
            static bool LaserAttackCoroutine_Prefix(Unit_QQQBoss __instance, ref IEnumerator __result)
            {
                if (!__instance.isServer) return true;
                if (Hard <= 0) return true;
                if (UsePatternType == 2)
                {
                    __result = LaserAttackType2(__instance);
                    return false;
                }
                else if (UsePatternType == 3)
                {
                    __result = LaserAttackType3(__instance);
                    return false;
                }
                return true;
            }
            private static IEnumerator LaserAttackType2(Unit_QQQBoss Boss)
            {
                if (patternTargetField == null || laserAttackCoroutineField == null || isLastPatternReadyField == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_QQQBoss][LaserAttackType2]获取反射失败");
                    yield break;
                }
                yield return new WaitForSeconds(0.5f);
                Vector2 Center = GetBossZoneCenterPosition(Boss) + new Vector2(0f, 12f);
                float ZoneHalf = 7f;
                float Step = 2f;
                float AngleDownRight = new Vector2(1, 1).GetAngle();
                float AngleDownLeft = new Vector2(-1, 1).GetAngle();
                float AttackSpeed = 0.85f + Boss.hp / Boss.MaxHp * 0.15f;
                if (Hard >= 4) AttackSpeed *= 0.9f;
                bool isLastReady = (bool)isLastPatternReadyField.GetValue(Boss);
                for (int Stack = 0; Stack < 8; Stack++)
                {
                    if (isLastReady) break;

                    List<Vector2> AllSpawnPos = new List<Vector2>();
                    List<float> AllAngle = new List<float>();
                    for (float Offset = Stack % 2 == 0 ? Step * 0.5f : 0; Offset <= ZoneHalf * 2; Offset += Step)
                    {
                        AllSpawnPos.Add(Center + new Vector2(1, -1) * Offset);
                        AllAngle.Add(AngleDownRight);
                        AllSpawnPos.Add(Center + new Vector2(-1, -1) * Offset);
                        AllAngle.Add(AngleDownLeft);
                    }
                    Boss.StartCoroutine(ShootLaserAll(Boss, AllSpawnPos, AllAngle, 0.8f * AttackSpeed));
                    yield return new WaitForSeconds(1.2f * AttackSpeed);
                    isLastReady = (bool)isLastPatternReadyField.GetValue(Boss);
                }
                patternTargetField.SetValue(Boss, null);
                laserAttackCoroutineField.SetValue(Boss, null);
                Boss.NetworkcurrentAction = EAnimation.None;
                yield break;
            }
            public struct BulletSpawnInfo
            {
                public Vector2 Pos;
                public float Dir;
            }
            private static IEnumerator LaserAttackType3(Unit_QQQBoss Boss)
            {
                if (patternTargetField == null || laserAttackCoroutineField == null || isLastPatternReadyField == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_QQQBoss][LaserAttackType2]获取反射失败");
                    yield break;
                }

                yield return new WaitForSeconds(1.5f);
                patternTargetField.SetValue(Boss, null);
                Boss.NetworkcurrentAction = EAnimation.None;
                List<BulletSpawnInfo> spawnList = new List<BulletSpawnInfo>();
                Vector2 Center = GetBossZoneCenterPosition(Boss) + new Vector2(0f, 12f);
                float ZoneHalf = 8f;
                float AngleDownRight = new Vector2(1, 1).GetAngle();
                float AngleDownLeft = new Vector2(-1, 1).GetAngle();
                for (float Offset = 0; Offset <= ZoneHalf * 2; Offset += 0.2f)
                {
                    spawnList.Add(new BulletSpawnInfo() { Pos = Center + new Vector2(1, -1) * Offset, Dir = AngleDownRight });
                    spawnList.Add(new BulletSpawnInfo() { Pos = Center + new Vector2(-1, -1) * Offset, Dir = AngleDownLeft });
                }
                spawnList.Shuffle();
                float Mult = 1.5f - Boss.hp / Boss.MaxHp * 0.5f;
                float MaxStack = 30 * Mult;
                int Count = 0;
                bool isLastReady = (bool)isLastPatternReadyField.GetValue(Boss);
                while (Count < MaxStack)
                {
                    if (isLastReady) break;

                    Boss.StartCoroutine(ShootLaser(Boss, spawnList[0].Pos, spawnList[0].Dir));
                    spawnList.RemoveAt(0);
                    Count += 1;
                    yield return new WaitForSeconds(0.3f);
                    isLastReady = (bool)isLastPatternReadyField.GetValue(Boss);
                }
                laserAttackCoroutineField.SetValue(Boss, null);
                yield break;
            }
            private static IEnumerator ShootLaserAll(Unit_QQQBoss Boss, List<Vector2> AllPos, List<float> AllRot, float AttackSpeed = 0.8f)
            {
                if (AttackLaserMethod == null ||
                    RpcCreateLaserAttackFxMethod == null ||
                    RpcCreateLaserAttackWarningMethod == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_QQQBoss][LaserAttackType3]获取反射失败");
                    yield break;
                }
                for (int i = 0; i < AllPos.Count; i++)
                {
                    RpcCreateLaserAttackWarningMethod.Invoke(Boss, new object[] { AllPos[i], AllRot[i], true });
                }
                yield return new WaitForSeconds(AttackSpeed);
                for (int i = 0; i < AllPos.Count; i++)
                {
                    AttackLaserMethod.Invoke(Boss, new object[] { AllPos[i], AllRot[i] });
                    RpcCreateLaserAttackFxMethod.Invoke(Boss, new object[] { AllPos[i], AllRot[i] });
                }
                yield break;
            }
            private static IEnumerator ShootLaser(Unit_QQQBoss Boss, Vector2 Pos, float Rot, float AttackSpeed = 0.8f)
            {
                if (AttackLaserMethod == null ||
                    RpcCreateLaserAttackFxMethod == null ||
                    RpcCreateLaserAttackWarningMethod == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_QQQBoss][LaserAttackType3]获取反射失败");
                    yield break;
                }
                RpcCreateLaserAttackWarningMethod.Invoke(Boss, new object[] { Pos, Rot, true });
                yield return new WaitForSeconds(AttackSpeed);
                AttackLaserMethod.Invoke(Boss, new object[] { Pos, Rot });
                RpcCreateLaserAttackFxMethod.Invoke(Boss, new object[] { Pos, Rot });
            }

            private static void GetIssenDashEnd(Unit_QQQBoss Boss, Vector2 start, Vector2 direction, out Vector2 end)
            {
                float num = Boss.issenAttackSize.y;
                RaycastHit2D raycastHit2D = Physics2D.Raycast(start, direction, num, CombatManager.BlockCharacterLayerMask);
                if (raycastHit2D)
                {
                    num = Mathf.Max(0.5f, raycastHit2D.distance - 0.25f);
                }
                end = start + direction * num;
            }
            private static IEnumerator IssenDashMoveCoroutine(Unit_QQQBoss Boss, Vector2 end, float angle)
            {
                float timer = 0f;
                while (Vector2.Distance(Boss.transform.position, end) > 0.15f && timer < Boss.issenAttackMoveForceEndTime)
                {
                    Vector2 vector = Boss.transform.position;
                    Vector2 vector2 = end - vector;
                    Vector2 vector3 = vector2.normalized * Mathf.Min(Boss.issenAttackMoveSpeed * Time.deltaTime, vector2.magnitude);
                    Boss.TopdownRigidbody.RequestMove(vector3);
                    timer += Time.deltaTime;
                    yield return null;
                }
                yield break;
            }

            [HarmonyPatch("IssenAttackCoroutine")]
            [HarmonyPrefix]
            static bool IssenAttackCoroutine_Prefix(Unit_QQQBoss __instance, int attackCount, ref IEnumerator __result)
            {
                if (!__instance.isServer) return true;
                if (Hard <= 0) return true;

                if (UsePatternType == 2)
                {
                    __result = IssenAttackType2(__instance);
                }
                else if (UsePatternType == 3)
                {
                    __result = IssenAttackType3(__instance);
                }
                else
                {
                    __result = IssenAttackType(__instance);
                }
                return false;
            }
            private static IEnumerator IssenAttackType(Unit_QQQBoss Boss)
            {
                if (patternTargetField == null ||
                    RpcClearIssenAttackMethod == null ||
                    StopIssenDamageAttacksMethod == null ||
                    isLastPatternReadyField == null ||
                    RpcCreateIssenAttackWarningMethod == null ||
                    RpcStartIssenAttackMethod == null ||
                    RpcPlayIssenAttackStartSoundMethod == null ||
                    RpcPlayIssenAttackFxMethod == null ||
                    StartIssenDamageAttackMethod == null ||
                    IssenExtraAttackCoroutineMethod == null ||
                    issenAttackCoroutineField == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_QQQBoss][IssenAttackType]获取反射失败");
                    yield break;
                }

                Transform patternTarget = (Transform)patternTargetField.GetValue(Boss);
                RpcClearIssenAttackMethod.Invoke(Boss, null);
                StopIssenDamageAttacksMethod.Invoke(Boss, null);

                Vector2 lastIssenStart;
                Vector2 lastIssenEnd;
                int i = 0;
                int attackCount = 3;
                if (Boss.hp < Boss.MaxHp * 0.66f)
                    attackCount += 1;
                if (Boss.hp < Boss.MaxHp * 0.33f)
                    attackCount += 1;

                float Speed = 0.75f + Boss.hp / Boss.MaxHp * 0.25f;
                if (Hard >= 4) Speed *= 0.9f;
                bool isLastReady = (bool)isLastPatternReadyField.GetValue(Boss);
                while (i < attackCount && !isLastReady)
                {
                    if (patternTarget)
                    {
                        Boss.NetworkpreviousPatternTargetPos = patternTarget.position;
                    }
                    Vector2 Vec = Boss.transform.position;
                    Vector2 Dir = (Boss.NetworkpreviousPatternTargetPos - Vec).normalized;
                    if (Dir.sqrMagnitude <= 0.0001f)
                    {
                        Dir = Vector2.right;
                    }
                    GetIssenDashEnd(Boss, Vec, Dir, out Vector2 End);
                    Vector2 vector2 = End - Vec;
                    float Angle = HorayUtility.GetAngleFromVector(vector2);
                    Vector2 center = Vector2.Lerp(Vec, End, 0.5f);
                    RpcCreateIssenAttackWarningMethod.Invoke(Boss, new object[] { Vec, center, Angle });
                    yield return new WaitForSeconds(Boss.issenAnimationStartDelay * Speed);
                    RpcStartIssenAttackMethod.Invoke(Boss, null);
                    yield return new WaitForSeconds(Boss.issenAnimationLeadTime * Speed);
                    RpcPlayIssenAttackStartSoundMethod.Invoke(Boss, null);
                    RpcPlayIssenAttackFxMethod.Invoke(Boss, new object[] { center, Angle });
                    StartIssenDamageAttackMethod.Invoke(Boss, new object[] { center, Angle });
                    yield return IssenDashMoveCoroutine(Boss, End, Angle);
                    float SizeY = Boss.issenAttackSize.y * 0.5f;
                    lastIssenStart = center - Dir * SizeY + Dir;
                    lastIssenEnd = center + Dir * SizeY + Dir;
                    IEnumerator extraEnum = (IEnumerator)IssenExtraAttackCoroutineMethod.Invoke(Boss, new object[] { lastIssenStart, lastIssenEnd });
                    Boss.StartCoroutine(extraEnum);
                    yield return new WaitForSeconds(0.4f * Speed);
                    if (i < attackCount - 1 && Boss.issenAttackDelayBetween > 0f)
                    {
                        yield return new WaitForSeconds(Boss.issenAttackDelayBetween * Speed);
                    }

                    i += 1;
                    isLastReady = (bool)isLastPatternReadyField.GetValue(Boss);
                }
                patternTargetField.SetValue(Boss, null);
                issenAttackCoroutineField.SetValue(Boss, null);
                Boss.NetworkcurrentAction = EAnimation.None;
                yield break;
            }
            private static IEnumerator IssenAttackType2(Unit_QQQBoss Boss)
            {
                if (patternTargetField == null ||
                    RpcClearIssenAttackMethod == null ||
                    StopIssenDamageAttacksMethod == null ||
                    isLastPatternReadyField == null ||
                    RpcCreateIssenAttackWarningMethod == null ||
                    RpcStartIssenAttackMethod == null ||
                    RpcPlayIssenAttackStartSoundMethod == null ||
                    RpcPlayIssenAttackFxMethod == null ||
                    StartIssenDamageAttackMethod == null ||
                    issenAttackCoroutineField == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_QQQBoss][IssenAttackType]获取反射失败");
                    yield break;
                }

                Transform patternTarget = (Transform)patternTargetField.GetValue(Boss);
                RpcClearIssenAttackMethod.Invoke(Boss, null);
                StopIssenDamageAttacksMethod.Invoke(Boss, null);

                int i = 0;
                int attackCount = 3;
                if (Boss.hp < Boss.MaxHp * 0.5f) attackCount += 1;
                float Speed = 0.8f + Boss.hp / Boss.MaxHp * 0.4f;
                if (Hard >= 4)
                {
                    attackCount += 1;
                    Speed *= 0.9f;
                }
                bool isLastReady = (bool)isLastPatternReadyField.GetValue(Boss);
                while (i < attackCount && !isLastReady)
                {
                    if (patternTarget)
                    {
                        Boss.NetworkpreviousPatternTargetPos = patternTarget.position;
                    }
                    Vector2 Vec = Boss.transform.position;
                    Vector2 Dir = (Boss.NetworkpreviousPatternTargetPos - Vec).normalized;
                    if (Dir.sqrMagnitude <= 0.0001f)
                    {
                        Dir = Vector2.right;
                    }
                    GetIssenDashEnd(Boss, Vec, Dir, out Vector2 End);
                    Vector2 vector2 = End - Vec;
                    float Angle = HorayUtility.GetAngleFromVector(vector2);
                    Vector2 center = Vector2.Lerp(Vec, End, 0.5f);
                    RpcCreateIssenAttackWarningMethod.Invoke(Boss, new object[] { Vec, center, Angle });
                    yield return new WaitForSeconds(Boss.issenAnimationStartDelay * Speed);
                    RpcStartIssenAttackMethod.Invoke(Boss, null);
                    yield return new WaitForSeconds(Boss.issenAnimationLeadTime * Speed);
                    RpcPlayIssenAttackStartSoundMethod.Invoke(Boss, null);
                    RpcPlayIssenAttackFxMethod.Invoke(Boss, new object[] { center, Angle });
                    StartIssenDamageAttackMethod.Invoke(Boss, new object[] { center, Angle });
                    yield return IssenDashMoveCoroutine(Boss, End, Angle);
                    ShootPerimeterBulletForIssenAttack(Boss, Angle);
                    if (i < attackCount - 1 && Boss.issenAttackDelayBetween > 0f)
                    {
                        yield return new WaitForSeconds(Boss.issenAttackDelayBetween * Speed);
                    }

                    i += 1;
                    isLastReady = (bool)isLastPatternReadyField.GetValue(Boss);
                }
                patternTargetField.SetValue(Boss, null);
                issenAttackCoroutineField.SetValue(Boss, null);
                Boss.NetworkcurrentAction = EAnimation.None;
                yield break;
            }
            private static void ShootPerimeterBulletForIssenAttack(Unit_QQQBoss Boss, float Angle)
            {
                if (!NetworkServer.active)
                {
                    return;
                }
                if (QQBoss.windmillBulletHellPrefab == null)
                {
                    return;
                }
                for (int i = 0; i < 32; i++)
                {
                    float Rot = Angle + Random.Range(-135f, 135f);
                    GameObject gameObject = Object.Instantiate(QQBoss.windmillBulletHellPrefab, Boss.transform.position, Quaternion.identity);
                    QQBossBulletHell component = gameObject.GetComponent<QQBossBulletHell>();
                    component.moveSpeed *= Random.Range(0.75f, 1.5f);
                    component.Initialize(Boss, Rot, Boss.RequestStandardDamage() * 0.5f);
                    NetworkServer.Spawn(gameObject);
                }
            }
            private static IEnumerator IssenAttackType3(Unit_QQQBoss Boss)
            {
                if (patternTargetField == null ||
                    RpcClearIssenAttackMethod == null ||
                    StopIssenDamageAttacksMethod == null ||
                    isLastPatternReadyField == null ||
                    RpcCreateIssenAttackWarningMethod == null ||
                    RpcStartIssenAttackMethod == null ||
                    RpcPlayIssenAttackStartSoundMethod == null ||
                    RpcPlayIssenAttackFxMethod == null ||
                    StartIssenDamageAttackMethod == null ||
                    issenAttackCoroutineField == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_QQQBoss][IssenAttackType]获取反射失败");
                    yield break;
                }

                Transform patternTarget = (Transform)patternTargetField.GetValue(Boss);
                RpcClearIssenAttackMethod.Invoke(Boss, null);
                StopIssenDamageAttacksMethod.Invoke(Boss, null);
                bool isLastReady = (bool)isLastPatternReadyField.GetValue(Boss);
                if (isLastReady)
                {
                    patternTargetField.SetValue(Boss, null);
                    issenAttackCoroutineField.SetValue(Boss, null);
                    Boss.NetworkcurrentAction = EAnimation.None;
                    yield break;
                }
                if (patternTarget)
                {
                    Boss.NetworkpreviousPatternTargetPos = patternTarget.position;
                }
                Vector2 Vec = Boss.transform.position;
                Vector2 Dir = (Boss.NetworkpreviousPatternTargetPos - Vec).normalized;
                if (Dir.sqrMagnitude <= 0.0001f)
                {
                    Dir = Vector2.right;
                }
                GetIssenDashEnd(Boss, Vec, Dir, out Vector2 End);
                Vector2 vector2 = End - Vec;
                float Angle = HorayUtility.GetAngleFromVector(vector2);
                Vector2 center = Vector2.Lerp(Vec, End, 0.5f);
                RpcCreateIssenAttackWarningMethod.Invoke(Boss, new object[] { Vec, center, Angle });
                yield return new WaitForSeconds(Boss.issenAnimationStartDelay * 1.5f);
                RpcStartIssenAttackMethod.Invoke(Boss, null);
                yield return new WaitForSeconds(Boss.issenAnimationLeadTime * 1.5f);
                RpcPlayIssenAttackStartSoundMethod.Invoke(Boss, null);
                RpcPlayIssenAttackFxMethod.Invoke(Boss, new object[] { center, Angle });
                StartIssenDamageAttackMethod.Invoke(Boss, new object[] { center, Angle });
                yield return IssenDashMoveCoroutine(Boss, End, Angle);

                int Count = 0;
                float MaxCount = 5 + (1f - Boss.hp / Boss.MaxHp) * 3;
                while (Count < MaxCount)
                {
                    Count += 1;
                    Boss.StartCoroutine(UseIssenAttack(Boss, Count));
                    yield return new WaitForSeconds((Boss.issenAnimationStartDelay + Boss.issenAnimationLeadTime) * 0.6f);
                    isLastReady = (bool)isLastPatternReadyField.GetValue(Boss);
                    if (isLastReady) break;
                }
                patternTargetField.SetValue(Boss, null);
                issenAttackCoroutineField.SetValue(Boss, null);
                Boss.NetworkcurrentAction = EAnimation.None;
            }
            private static IEnumerator UseIssenAttack(Unit_QQQBoss Boss, int Count)
            {
                if (RpcCreateIssenAttackWarningMethod == null ||
                    RpcStartIssenAttackMethod == null ||
                    RpcPlayIssenAttackStartSoundMethod == null ||
                    RpcPlayIssenAttackFxMethod == null ||
                    StartIssenDamageAttackMethod == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_QQQBoss][IssenAttackType]获取反射失败");
                    yield break;
                }

                Vector2 Vec = Boss.NetworkpreviousPatternTargetPos;
                float R = HorayUtility.GetAngleFromVector((Vector2)Boss.transform.position - Vec);
                float AddR = Count % 2 == 0 ? -90f : 90f;
                Vector2 Dir = HorayUtility.GetVector2FromAngle(R + AddR + Random.Range(-45f, 45f));
                Vec -= Dir * Boss.issenAttackSize.y * 0.5f;
                GetIssenDashEnd(Boss, Vec, Dir, out Vector2 End);
                Vector2 vector2 = End - Vec;
                float Angle = HorayUtility.GetAngleFromVector(vector2);
                Vector2 center = Vector2.Lerp(Vec, End, 0.5f);

                float Wait = 0.125f + 0.125f * Boss.hp / Boss.MaxHp;
                yield return new WaitForSeconds(Wait);
                RpcCreateIssenAttackWarningMethod.Invoke(Boss, new object[] { Vec, center, Angle });
                yield return new WaitForSeconds(Boss.issenAnimationStartDelay - 0.05f);
                RpcStartIssenAttackMethod.Invoke(Boss, null);
                yield return new WaitForSeconds(Boss.issenAnimationLeadTime - 0.05f);
                RpcPlayIssenAttackStartSoundMethod.Invoke(Boss, null);
                RpcPlayIssenAttackFxMethod.Invoke(Boss, new object[] { center, Angle });
                StartIssenDamageAttackMethod.Invoke(Boss, new object[] { center, Angle });
                yield break;
            }

            [HarmonyPatch("DarknessAttackCoroutine")]
            [HarmonyPrefix]
            static bool DarknessAttackCoroutine_Prefix(Unit_QQQBoss __instance, ref IEnumerator __result)
            {
                if (!__instance.isServer) return true;
                if (Hard <= 0) return true;

                if (UsePatternType == 2)
                {
                    __result = DarknessAttackType2(__instance);
                    return false;
                }
                else if (UsePatternType == 3)
                {
                    __result = DarknessAttackType3(__instance);
                    return false;
                }
                return true;
            }
            private static IEnumerator DarknessAttackType2(Unit_QQQBoss Boss)
            {
                if (isLastPatternReadyField == null
                    || darknessOuterDamageCoroutineField == null
                    || DarknessOuterDamageCoroutineMethod == null
                    || StartDarknessMoveMethod == null
                    || EndDarknessAttackMethod == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_QQQBoss][DarknessAttackType2]获取反射失败");
                    yield break;
                }

                Vector2 areaCenter = GetBossZoneCenterPosition(Boss);
                Boss.NetworkenableDarknessArea = true;
                yield return new WaitForSeconds(Boss.darknessAreaSlamDelay);

                bool isLastPatternReady = (bool)isLastPatternReadyField.GetValue(Boss);
                if (!isLastPatternReady)
                {
                    if (DarknessOuterDamageCoroutineMethod.Invoke(Boss, new object[] { areaCenter }) is IEnumerator darkCoroEnum)
                    {
                        Coroutine runCoro = Boss.StartCoroutine(darkCoroEnum);
                        darknessOuterDamageCoroutineField.SetValue(Boss, runCoro);
                    }
                    StartDarknessMoveMethod.Invoke(Boss, new object[] { areaCenter });

                    yield return new WaitForSeconds(0.5f);
                    int Count = 0;
                    float Dir = HorayUtility.GetAngleFromVector(areaCenter - (Vector2)Boss.transform.position);
                    float HPMult = Boss.hp / Boss.MaxHp;
                    float Time = 0.12f + 0.03f * HPMult;
                    while (Count < 50 - 10 * HPMult)
                    {
                        ShootPerimeterBulletForDarknessAttack(Boss, Dir);
                        yield return new WaitForSeconds(Time);
                        Count += 1;

                        isLastPatternReady = (bool)isLastPatternReadyField.GetValue(Boss);
                        if (isLastPatternReady) break;
                    }
                    yield return new WaitForSeconds(0.5f);
                }
                else
                {
                    yield return new WaitForSeconds(0.25f);
                }
                EndDarknessAttackMethod.Invoke(Boss, null);
                yield break;
            }
            private static void ShootPerimeterBulletForDarknessAttack(Unit_QQQBoss Boss, float Dir)
            {
                if (!NetworkServer.active)
                {
                    return;
                }
                if (QQBoss.windmillBulletHellPrefab == null)
                {
                    return;
                }
                Vector3 Pos = Boss.transform.position + (Vector3)HorayUtility.GetVector2FromAngle(Dir) * 8f;
                Pos += (Vector3)HorayUtility.GetVector2FromAngle(Dir + 90f) * Random.Range(-5f, 5f);
                GameObject gameObject = Object.Instantiate(QQBoss.windmillBulletHellPrefab, Pos, Quaternion.identity);
                QQBossBulletHell component = gameObject.GetComponent<QQBossBulletHell>();
                component.moveSpeed *= 0.7f;
                component.Initialize(Boss, Dir + 180f, Boss.RequestStandardDamage() * 0.5f);
                NetworkServer.Spawn(gameObject);
            }
            private static IEnumerator DarknessAttackType3(Unit_QQQBoss Boss)
            {
                if (isLastPatternReadyField == null
                    || darknessOuterDamageCoroutineField == null
                    || DarknessOuterDamageCoroutineMethod == null
                    || StartDarknessMoveMethod == null
                    || EndDarknessAttackMethod == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_QQQBoss][DarknessAttackType2]获取反射失败");
                    yield break;
                }

                Vector2 areaCenter = GetBossZoneCenterPosition(Boss);
                Boss.NetworkenableDarknessArea = true;
                yield return new WaitForSeconds(Boss.darknessAreaSlamDelay);

                bool isLastPatternReady = (bool)isLastPatternReadyField.GetValue(Boss);
                if (!isLastPatternReady)
                {
                    if (DarknessOuterDamageCoroutineMethod.Invoke(Boss, new object[] { areaCenter }) is IEnumerator darkCoroEnum)
                    {
                        Coroutine runCoro = Boss.StartCoroutine(darkCoroEnum);
                        darknessOuterDamageCoroutineField.SetValue(Boss, runCoro);
                    }
                    StartDarknessMoveMethod.Invoke(Boss, new object[] { areaCenter });

                    yield return new WaitForSeconds(0.5f);
                    int Count = 0;
                    float Speed = 0.75f + 0.25f * Boss.hp / Boss.MaxHp;
                    if (Hard >= 4) Speed *= 0.9f;
                    Vector2 Dir = (areaCenter - (Vector2)Boss.transform.position).normalized;
                    while (Count < 72f / Speed)
                    {
                        Boss.StartCoroutine(ShootLastPatternOneShotLaserForDarknessAttack(Boss, Dir, Count * 8f));
                        yield return new WaitForSeconds(0.1f * Speed);
                        Count += 1;

                        isLastPatternReady = (bool)isLastPatternReadyField.GetValue(Boss);
                        if (isLastPatternReady) break;
                    }
                    yield return new WaitForSeconds(0.5f);
                }
                else
                {
                    yield return new WaitForSeconds(0.25f);
                }
                EndDarknessAttackMethod.Invoke(Boss, null);
                yield break;
            }
            private static IEnumerator ShootLastPatternOneShotLaserForDarknessAttack(Unit_QQQBoss Boss, Vector2 Dir, float Rot)
            {
                if (!NetworkServer.active) yield break;
                if (RpcCreateLastPatternOneShotLaserWarningMethod == null
                || AttackLastPatternOneShotLaserMethod == null
                || RpcSpawnLastPatternOneShotLaserFxMethod == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_QQQBoss][ShootLastPatternOneShotLaserForDarknessAttack]获取反射失败");
                    yield break;
                }

                Vector2 vector = (Vector2)Boss.transform.position + Dir * 1.5f;
                RpcCreateLastPatternOneShotLaserWarningMethod.Invoke(Boss, new object[] { vector, Rot });
                yield return new WaitForSeconds(0.33f);
                AttackLastPatternOneShotLaserMethod.Invoke(Boss, new object[] { vector, Rot });
                RpcSpawnLastPatternOneShotLaserFxMethod.Invoke(Boss, new object[] { vector, Rot });
                yield break;
            }

            private static List<int> RpcCircleAttackSize = new List<int>();
            [HarmonyPatch("CircleAttackCoroutine")]
            [HarmonyPrefix]
            static bool CircleAttackCoroutine_Prefix(Unit_QQQBoss __instance, ref IEnumerator __result)
            {
                if (!__instance.isServer) return true;
                if (Hard <= 0) return true;

                if (UsePatternType == 2)
                {
                    __result = CircleAttackType2(__instance);
                    return false;
                }
                else if (UsePatternType == 3)
                {
                    __result = CircleAttackType3(__instance);
                    return false;
                }
                return true;
            }
            private static IEnumerator CircleAttackType2(Unit_QQQBoss Boss)
            {
                if (patternTargetField == null ||
                    circleAttackCoroutineField == null ||
                    AttackDonutMethod == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_QQQBoss][CircleAttackType2]获取反射失败");
                    yield break;
                }

                Vector2 OrigPos = GetBossZoneCenterPosition(Boss);
                List<Vector2> Pos = new List<Vector2> { OrigPos };
                List<int> Size = new List<int> { 0 };
                for (int i = 1; i <= 4; i++)
                {
                    float Rot = Random.Range(0f, 360f);
                    int Count = 3 + i;
                    for (int k = 0; k < Count; k++)
                    {
                        Pos.Add(OrigPos + HorayUtility.GetVector2FromAngle(360f / Count * k + Rot) * i * 3f);
                        Size.Add(Random.Range(0, Boss.circleAttackSizes.Count));
                    }
                }
                RpcCircleAttackSize = new List<int>();
                for (int i = 0; i < Size.Count; i++)
                {
                    RpcCircleAttackSize = Size;
                }
                Boss.RpcCircleAttack(Vector2.zero, Vector2.zero, Pos.ToArray());
                yield return new WaitForSeconds(Boss.circleAttackWarningTime + Boss.circleAttackAdditionalWaitTime - 0.2f);
                Boss.RpcStartCircleAttackAnimation();
                yield return new WaitForSeconds(0.4f);
                Boss.RpcPlayCircleAttackExplodeEvent();
                for (int i = 0; i < Pos.Count; i++)
                {
                    float AttackSize = Boss.circleAttackSizes.SafeRandomAccess(Size[i]);
                    AttackDonutMethod.Invoke(Boss, new object[] { Pos[i], AttackSize / 2f });
                    int AttackSizeFx = Mathf.Min(Boss.circleAttackFxPrefabs.Count, Size[i]);
                    Boss.RpcAttackDonutFx(Pos[i], AttackSizeFx);
                }

                patternTargetField.SetValue(Boss, null);
                circleAttackCoroutineField.SetValue(Boss, null);
                Boss.RpcEndCircleAttack();
                Boss.NetworkcurrentAction = EAnimation.None;
            }
            private static IEnumerator CircleAttackType3(Unit_QQQBoss Boss)
            {
                if (patternTargetField == null ||
                    circleAttackCoroutineField == null ||
                    AttackDonutMethod == null ||
                    SpawnObeliskSummonerMethod == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_QQQBoss][CircleAttackType3]获取反射失败");
                    yield break;
                }
                float Time = 5f;
                if (obeliskPrefab == null)
                {
                    SpawnObeliskSummonerMethod.Invoke(Boss, new object[] { (Vector2)Boss.transform.position + new Vector2(10f, 10f), 0 });
                    while (Time > 0f)
                    {
                        if (obeliskPrefab != null) break;

                        Time -= 0.5f;
                        yield return new WaitForSeconds(0.5f);
                    }
                    Boss.KillObeliskSummoner();
                }
                CreateObelisk(Boss, Boss.transform.position);
                yield return new WaitForSeconds(Boss.circleAttackWarningTime + Boss.circleAttackAdditionalWaitTime - 0.3f);
                Boss.RpcStartCircleAttackAnimation();
                yield return new WaitForSeconds(0.3f);

                patternTargetField.SetValue(Boss, null);
                circleAttackCoroutineField.SetValue(Boss, null);
                Boss.RpcEndCircleAttack();
                Boss.NetworkcurrentAction = EAnimation.None;
            }
            private static void CreateObelisk(Unit_QQQBoss Boss, Vector2 position)
            {
                if (!NetworkServer.active) return;
                if (!obeliskPrefab) return;
                if (bossEnvironmentOnServerField == null) return;

                BossEnvironment_QQBoss bossEnvironmentOnServer = (BossEnvironment_QQBoss)bossEnvironmentOnServerField.GetValue(Boss);
                GameObject gameObject = Object.Instantiate(obeliskPrefab.gameObject, position, Quaternion.identity, Boss.transform.parent);
                QQQBossObelisk component = gameObject.GetComponent<QQQBossObelisk>();
                if (!component)
                {
                    Object.Destroy(gameObject);
                    return;
                }
                component.Initialize(bossEnvironmentOnServer, Boss);
                NetworkServer.Spawn(gameObject);
            }

            [HarmonyPatch("CircleAttackWarningCoroutine")]
            [HarmonyPrefix]
            static bool CircleAttackWarningCoroutine_Prefix(Unit_QQQBoss __instance, Vector2 start, Vector2 end, Vector2[] echoPositions, ref IEnumerator __result)
            {
                if (!__instance.isServer) return true;
                if (Hard <= 0) return true;
                if (UsePatternType == 2)
                {
                    __result = CircleAttackWarningType2(__instance, echoPositions);
                    return false;
                }
                return true;
            }
            private static IEnumerator CircleAttackWarningType2(Unit_QQQBoss Boss, Vector2[] Pos)
            {
                if (CreateCircleAttackWarningMethod == null ||
                    circleAttackWarningCoroutineField == null ||
                    circleAttackWarningsField == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_QQQBoss][CircleAttackWarningType2]获取反射失败");
                    yield break;
                }

                for (int i = 0; i < Pos.Length; i++)
                {
                    CreateCircleAttackWarningMethod.Invoke(Boss, new object[] { Pos[i], RpcCircleAttackSize.SafeRandomAccess(i) });
                }
                yield return new WaitForSeconds(Boss.circleAttackWarningTime);
                circleAttackWarningCoroutineField.SetValue(Boss, null);
                List<UI_AOEWarning> circleAttackWarnings = (List<UI_AOEWarning>)circleAttackWarningsField.GetValue(Boss);
                circleAttackWarnings.Clear();
                circleAttackWarningsField.SetValue(Boss, circleAttackWarnings);
                yield break;
            }

            [HarmonyPatch("InAndOutAttack")]
            [HarmonyPrefix]
            static void InAndOutAttack_Prefix(Unit_QQQBoss __instance, ref EInAndOutAttackType attackType, ref Transform target)
            {
                if (Hard >= 1)
                {
                    if (TryGetRandomAlivePlayer(out var Player) && Player != null)
                    {
                        attackType = EInAndOutAttackType.RandomPos;
                        target = Player.transform;
                    }
                }
            }

            [HarmonyPatch("GetInAndOutRandomPositionNearTarget")]
            [HarmonyPostfix]
            static void GetInAndOutRandomPositionNearTarget_Postfix(Unit_QQQBoss __instance, Vector2 targetPosition, ref Vector2 __result)
            {
                if (UsePatternType == 2)
                {
                    __result = GetBossZoneCenterPosition(__instance) + HorayUtility.GetVector2FromAngle(Random.Range(0f, 360f)) * 7.5f;
                }
            }

            [HarmonyPatch("InAndOutAttackCoroutine")]
            [HarmonyPrefix]
            static bool InAndOutAttackCoroutine_Prefix(Unit_QQQBoss __instance, List<int> attackOrder, EInAndOutAttackType attackType, ref IEnumerator __result)
            {
                if (!__instance.isServer) return true;
                if (Hard <= 0) return true;

                __result = InAndOutAttackType(__instance, attackOrder);
                return false;
            }
            private static IEnumerator InAndOutAttackType(Unit_QQQBoss Boss, List<int> attackOrder)
            {
                if (patternTargetField == null ||
                    isLastPatternReadyField == null ||
                    currentInAndOutCircleField == null ||
                    GetInAndOutOuterWarningTimeMethod == null ||
                    RpcCreateInAndOutOuterWarningMethod == null ||
                    AttackInAndOutOuterAreaMethod == null ||
                    RpcCreateInAndOutInnerWarningMethod == null ||
                    AttackInAndOutInnerAreaMethod == null ||
                    inAndOutAttackCoroutineField == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_QQQBoss][InAndOutAttackType]获取反射失败");
                    yield break;
                }

                attackOrder.Add(attackOrder[0]);
                if (UsePatternType == 2)
                {
                    attackOrder = new List<int>() { 0, 0, 0, 0 };
                }
                if (UsePatternType == 3)
                {
                    attackOrder = new List<int>() { 0, 1, 1, 1 };
                }
                bool isLastPatternReady = (bool)isLastPatternReadyField.GetValue(Boss);

                yield return new WaitForSeconds(1.8f);
                var currentCircleObj = currentInAndOutCircleField.GetValue(Boss) as QQQBossBlueAttackCircle;
                if (currentCircleObj != null)
                {
                    currentCircleObj.EndRequest();
                    currentInAndOutCircleField.SetValue(Boss, null);
                }
                float Speed = 0.85f + 0.15f * Boss.hp / Boss.MaxHp;
                if (Hard >= 4) Speed *= 0.9f;
                    Vector2 attackCenter = Boss.inAndOutRandomPos;
                if (!isLastPatternReady)
                {
                    yield return new WaitForSeconds(0.2f);
                    Vector2 Vec = (GetBossZoneCenterPosition(Boss) - attackCenter) / 3f * 2f;
                    PlayerAvatar? Player = null;
                    for (int i = 0; i < attackOrder.Count; i++)
                    {
                        float Time = 0.05f + 0.05f * i;
                        if (UsePatternType == 2) Time = 0.1f;
                        if (UsePatternType == 3)
                        {
                            Time = 0.3f;
                            if (Player != null) attackCenter = Player.transform.position;
                            else
                            {
                                if (TryGetRandomAlivePlayer(out var Target) && Target)
                                    Player = Target;
                            }
                        }
                        if (attackOrder[i] == 0)
                        {
                            Vector2 fieldCenter = GetBossZoneCenterPosition(Boss);
                            float num = (float)GetInAndOutOuterWarningTimeMethod.Invoke(Boss, new object[] { EInAndOutAttackType.RandomPos });
                            RpcCreateInAndOutOuterWarningMethod.Invoke(Boss, new object[] { fieldCenter, attackCenter, EInAndOutAttackType.RandomPos, num });
                            yield return new WaitForSeconds((num + 0.25f + Time) * Speed);
                            AttackInAndOutOuterAreaMethod.Invoke(Boss, new object[] { fieldCenter, attackCenter, EInAndOutAttackType.RandomPos });
                        }
                        else
                        {
                            RpcCreateInAndOutInnerWarningMethod.Invoke(Boss, new object[] { attackCenter });
                            yield return new WaitForSeconds((Boss.inAndOutInnerWarningTime + 0.25f + Time) * Speed);
                            AttackInAndOutInnerAreaMethod.Invoke(Boss, new object[] { attackCenter });
                        }
                        yield return new WaitForSeconds(Boss.inAndOutSecondAttackDelay);
                        if (UsePatternType == 2)
                        {
                            attackCenter += Vec;
                        }
                        isLastPatternReady = (bool)isLastPatternReadyField.GetValue(Boss);
                        if (isLastPatternReady) break;
                    }
                }

                patternTargetField.SetValue(Boss, null);
                inAndOutAttackCoroutineField.SetValue(Boss, null);
                Boss.NetworkcurrentAction = EAnimation.None;
                yield break;
            }

            [HarmonyPatch("FallGuysAttackCoroutine")]
            [HarmonyPrefix]
            static bool FallGuysAttackCoroutine_Prefix(Unit_QQQBoss __instance, ref IEnumerator __result)
            {
                if (!__instance.isServer) return true;
                if (Hard <= 0) return true;

                if(UsePatternType == 3)
                {
                    __result = FallGuysAttackType3(__instance);
                    return false;
                }
                else if (UsePatternType == 2)
                {
                    __result = FallGuysAttackType2(__instance);
                    return false;
                }
                return true;
            }
            private static IEnumerator FallGuysAttackType2(Unit_QQQBoss Boss)
            {
                if (isLastPatternReadyField == null ||
                    fallGuysShieldAppliedField == null ||
                    EndFallGuysAttackMethod == null ||
                    fallGuysCellRegionMapField == null ||
                    ActivateFallGuysRegionMethod == null ||
                    ClearFallGuysDamageZonesMethod == null ||
                    fallGuysUsedRegionIdsField == null ||
                    StartFallGuysPerimeterBulletsMethod == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_QQQBoss][FallGuysAttackType2]获取反射失败");
                    yield break;
                }
                bool fallGuysShieldApplied = (bool)fallGuysShieldAppliedField.GetValue(Boss);
                bool isLastPatternReady = (bool)isLastPatternReadyField.GetValue(Boss);

                Boss.NetworkfallGuysAnimationPhase = 1;
                if (fallGuysShieldApplied)
                {
                    fallGuysShieldAppliedField.SetValue(Boss, false);
                    Boss.RemoveShield("FALL_GUYS_SHIELD");
                }
                Boss.AddShield(Boss.MaxHp * 0.3f, "FALL_GUYS_SHIELD");
                fallGuysShieldApplied = true;
                fallGuysShieldAppliedField.SetValue(Boss, true);
                if (Boss.fallGuysAnimationStartDelay > 0f)
                {
                    yield return new WaitForSeconds(Boss.fallGuysAnimationStartDelay);
                }
                StartFallGuysPerimeterBulletsMethod.Invoke(Boss, null);
                Boss.NetworkfallGuysAnimationPhase = 2;
                float Mult = 1f + 0.25f * (1f - Boss.hp / Boss.MaxHp);
                if (Hard >= 4) Mult *= 1.1f;
                if (CreateFallGuysRegionMap(Boss, out var Out))
                {
                    for (int i = 0; i < 5; i++)
                    {
                        if (isLastPatternReady) break;

                        FallGuysRegion(Boss, Out.ToList(), Out.Count.IntMult(0.28f * Mult));
                        yield return new WaitForSeconds(1.6f);
                        ClearFallGuysDamageZonesMethod.Invoke(Boss, new object[] { false });
                        fallGuysUsedRegionIdsField.SetValue(Boss, new HashSet<int>());
                        yield return new WaitForSeconds(0.2f);
                        isLastPatternReady = (bool)isLastPatternReadyField.GetValue(Boss);
                    }
                }
                yield return new WaitForSeconds(1.5f);
                EndFallGuysAttackMethod.Invoke(Boss, new object[] { false });
                if (fallGuysShieldApplied)
                {
                    fallGuysShieldAppliedField.SetValue(Boss, false);
                    Boss.RemoveShield("FALL_GUYS_SHIELD");
                }
                yield break;
            }
            private static bool CreateFallGuysRegionMap(Unit_QQQBoss Boss, out List<(int, int)> OutPut)
            {
                if (fallGuysCellRegionMapField == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_QQQBoss][CreateFallGuysRegionMap]获取反射失败");
                    OutPut = new List<(int, int)>();
                    return false;
                }
                int[,] fallGuysCellRegionMap = (int[,])fallGuysCellRegionMapField.GetValue(Boss);
                int height = fallGuysCellRegionMap.GetLength(0);
                int width = fallGuysCellRegionMap.GetLength(1);
                List<(int, int)> Map = new List<(int, int)>();
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Map.Add((x, y));
                    }
                }
                OutPut = Map;
                return true;
            }
            private static void FallGuysRegion(Unit_QQQBoss Boss, List<(int, int)> Map, int Count)
            {
                if (ActivateFallGuysRegionMethod == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_QQQBoss][FallGuysRegion]获取反射失败");
                    return;
                }
                for (int k = 0; k < Count; k++)
                {
                    int randIdx = Random.Range(0, Map.Count);
                    var (x, y) = Map[randIdx];
                    Map.RemoveAt(randIdx);
                    if (Boss.TryGetFallGuysRegionId(x, y, out var Id))
                    {
                        ActivateFallGuysRegionMethod.Invoke(Boss, new object[] { Id });
                    }
                }
            }
            private static IEnumerator FallGuysAttackType3(Unit_QQQBoss Boss)
            {
                if (isLastPatternReadyField == null ||
                    fallGuysShieldAppliedField == null ||
                    EndFallGuysAttackMethod == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_QQQBoss][FallGuysAttackType2]获取反射失败");
                    yield break;
                }
                bool fallGuysShieldApplied = (bool)fallGuysShieldAppliedField.GetValue(Boss);
                bool isLastPatternReady = (bool)isLastPatternReadyField.GetValue(Boss);

                Boss.NetworkfallGuysAnimationPhase = 1;
                if (fallGuysShieldApplied)
                {
                    fallGuysShieldAppliedField.SetValue(Boss, false);
                    Boss.RemoveShield("FALL_GUYS_SHIELD");
                }
                Boss.AddShield(Boss.MaxHp * 0.3f, "FALL_GUYS_SHIELD");
                fallGuysShieldApplied = true;
                fallGuysShieldAppliedField.SetValue(Boss, true);
                if (Boss.fallGuysAnimationStartDelay > 0f)
                {
                    yield return new WaitForSeconds(Boss.fallGuysAnimationStartDelay);
                }
                Boss.NetworkfallGuysAnimationPhase = 2;
                float Speed = 0.12f + 0.04f * (1f - Boss.hp / Boss.MaxHp);
                if (CreateFallGuysRegionMap(Boss, out var Out)) FallGuysRegion(Boss, Out, Out.Count.IntMult(Speed));
                yield return new WaitForSeconds(0.3f);
                float fallGuysTime = 0f;
                int Count = 0;
                while (fallGuysTime < 6f)
                {
                    if (isLastPatternReady) break;
                    if (Count % 10 == 0)
                        CreateFallGuysBulletToAll(Boss);
                    CreateFallGuysBullet(Boss, Mathf.Max(0f, fallGuysTime - 0.4f));
                    fallGuysTime += 0.04f;
                    Count += 1;
                    yield return new WaitForSeconds(0.04f);
                    isLastPatternReady = (bool)isLastPatternReadyField.GetValue(Boss);
                }
                yield return new WaitForSeconds(1.5f);
                EndFallGuysAttackMethod.Invoke(Boss, new object[] { false });
                if (fallGuysShieldApplied)
                {
                    fallGuysShieldAppliedField.SetValue(Boss, false);
                    Boss.RemoveShield("FALL_GUYS_SHIELD");
                }
                yield break;
            }
            private static void CreateFallGuysBullet(Unit_QQQBoss Boss, float Time)
            {
                if (!NetworkServer.active) return;

                int Max = Hard >= 4 ? 5 : 4;
                float Rot = 360f / Max;
                for (float i = 0f; i < 360f; i += Rot)
                {
                    GameObject gameObject = Object.Instantiate(Boss.fallGuysPerimeterBulletPrefab, Boss.transform.position + new Vector3(0f, 0.5f, 0f), Quaternion.identity);
                    float Vec = i + Time * 50f + Random.Range(-8f, 8f);
                    QQBossBulletHell component = gameObject.GetComponent<QQBossBulletHell>();
                    component.Initialize(Boss, Vec, Boss.RequestStandardDamage() * 0.33f);
                    NetworkServer.Spawn(gameObject);
                }
            }
            private static void CreateFallGuysBulletToAll(Unit_QQQBoss Boss)
            {
                if (!NetworkServer.active) return;

                foreach(PlayerSpawner Spawner in PlayerSpawner.MultiplayerList)
                {
                    PlayerAvatar Player = Spawner.PlayerAvatar;
                    Vector3 Pos = Boss.transform.position + new Vector3(0f, 0.5f, 0f);
                    GameObject gameObject = Object.Instantiate(Boss.fallGuysPerimeterBulletPrefab, Pos, Quaternion.identity);
                    float Vec = HorayUtility.GetAngleFromVector(Player.transform.position - Pos);
                    QQBossBulletHell component = gameObject.GetComponent<QQBossBulletHell>();
                    component.Initialize(Boss, Vec, Boss.RequestStandardDamage() * 0.33f);
                    NetworkServer.Spawn(gameObject);
                }
            }

            [HarmonyPatch("SummonUnitCoroutine")]
            [HarmonyPrefix]
            static bool SummonUnitCoroutine_Prefix(Unit_QQQBoss __instance, Transform summonPoint, ref IEnumerator __result)
            {
                if (!__instance.isServer) return true;
                if (Hard <= 0) return true;

                if (UsePatternType == 3)
                {
                    __result = SummonUnitType3(__instance);
                    return false;
                }
                else if (UsePatternType == 2)
                {
                    __result = SummonUnitType2(__instance);
                    return false;
                }
                return true;
            }
            private static IEnumerator SummonUnitType2(Unit_QQQBoss Boss)
            {
                if (summonUnitCoroutineField == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_QQQBoss][SummonUnitType2]获取反射失败");
                    yield break;
                }
                float Mult = 0.4f + 0.4f * Boss.hp / Boss.MaxHp;
                if (Hard >= 4) Mult *= 0.9f;
                yield return new WaitForSeconds(Mult * 0.3f);
                FireBulletRing(Boss, 120, 0.75f);
                yield return new WaitForSeconds(Mult * 0.1f);
                summonUnitCoroutineField.SetValue(Boss, null);
                Boss.NetworkcurrentAction = EAnimation.None;
            }
            private static IEnumerator SummonUnitType3(Unit_QQQBoss Boss)
            {
                if (summonUnitCoroutineField == null || isLastPatternReadyField == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_QQQBoss][SummonUnitType3]获取反射失败");
                    yield break;
                }

                bool isLastPatternReady = (bool)isLastPatternReadyField.GetValue(Boss);

                float MaxTime = 0f;
                int Count = 0;
                float AddTime = Hard >= 4 ? 0.09f : 0.1f;
                while (MaxTime < 6f + 3f * (1f - Boss.hp / Boss.MaxHp))
                {
                    if (isLastPatternReady) break;

                    Count += 1;
                    if (Count % 25 == 0)
                        FireBulletRing(Boss, 120, 0.75f);
                    FireBulletRing(Boss, 5, Mathf.Sin(MaxTime) * 120f);
                    yield return new WaitForSeconds(AddTime);
                    MaxTime += AddTime;
                    isLastPatternReady = (bool)isLastPatternReadyField.GetValue(Boss);
                }
                summonUnitCoroutineField.SetValue(Boss, null);
                yield return new WaitForSeconds(Boss.summonUnitPatternEndDelay);
                Boss.NetworkcurrentAction = EAnimation.None;
            }
        }

        [HarmonyPatch(typeof(UnitAI_QQQBossObeliskSummoner))]
        public static class UnitAI_QQQBossObeliskSummoner_Patch
        {
            private static readonly FieldInfo? summonerField = AccessTools.Field(typeof(UnitAI_QQQBossObeliskSummoner), "summoner");
            private static readonly HashSet<int> UseAttack = new HashSet<int>();

            [HarmonyPatch("OnAIUpdate_FoundEnemy")]
            [HarmonyPostfix]
            static void OnAIUpdate_FoundEnemy_Prefix(UnitAI_QQQBossObeliskSummoner __instance)
            {
                if (summonerField == null) return;

                if (obeliskPrefab == null)
                {
                    Unit_QQQBossObeliskSummoner summoner = (Unit_QQQBossObeliskSummoner)summonerField.GetValue(__instance);
                    obeliskPrefab = summoner.obeliskPrefab;
                }
            }
        }

        [HarmonyPatch(typeof(QQQBossObelisk))]
        public static class QQQBossObelisk_Patch
        {
            [HarmonyPatch("Initialize")]
            [HarmonyPostfix]
            static void Initialize_Postfix(QQQBossObelisk __instance, BossEnvironment_QQBoss environment, UnitAvatar owner)
            {
                string TName = owner.name.Replace("(Clone)", "");
                if (TName == "QQQBoss")
                {
                    __instance.StartCoroutine(DelayedDestruction(__instance));
                }
            }
            private static IEnumerator DelayedDestruction(QQQBossObelisk Target)
            {
                if (!NetworkServer.active) yield break;

                yield return new WaitForSeconds(20f);
                Target.RequestDestroy();
                yield break;
            }
        }

        [HarmonyPatch(typeof(QQQBossBlueAttackCircle))]
        public static class QQQBossBlueAttackCircle_Patch
        {
            [HarmonyPatch("SetTargetAndRadius", new Type[] { typeof(Transform), typeof(float), typeof(float) })]
            [HarmonyPrefix]
            static void SetTargetAndRadius_Prefix(QQQBossBlueAttackCircle __instance, Transform target, ref float radius, float radiusChangeTime)
            {
                if (Hard >= 1)
                {
                    if (UsePatternType > 1) radius = inAndOutOuterSafeRadius;
                }
            }

            [HarmonyPatch("SetTargetAndRadius", new Type[] { typeof(Vector2), typeof(float), typeof(float) })]
            [HarmonyPrefix]
            static void SetTargetAndRadius_Prefix(QQQBossBlueAttackCircle __instance, Vector2 targetPosition, ref float radius, float radiusChangeTime)
            {
                if (Hard >= 1)
                {
                    if (UsePatternType > 1) radius = inAndOutOuterSafeRadius;
                }
            }
        }

        [HarmonyPatch(typeof(UnitAddon_TimedHPDecay))]
        public static class UnitAddon_TimedHPDecay_Patch
        {
            [HarmonyPatch("Update")]
            [HarmonyPrefix]
            static void Update_Prefix(UnitAddon_TimedHPDecay __instance)
            {
                if (__instance.decayWaitTimer.Check())
                {
                    if (__instance.TryGetComponent<UnitAvatar>(out var unit))
                    {
                        if (unit == null || unit.IsDead) return;
                        string TName = unit.name.Replace("(Clone)", "");
                        if (TName != "QQQBoss_GreenUnit") return;
                        if (unit.GetCustomStatUnsafe("FireBulletRing") <= 0)
                        {
                            unit.AddCustomStatUnsafe("FireBulletRing", 1);
                            FireBulletRing(unit, 36, 0f, Random.Range(0.5f, 1f));
                            __instance.decayHp = unit.MaxHp;
                        }
                    }
                }
            }
        }

        private static GameObject? QBossBullet = null;
        private static void FireBulletRing(UnitAvatar __instance, int Stack, float Rot = 0f, float Speed = 1f)
        {
            if (QBossBullet == null)
            {
                if (Tools.GetPro("QBossRootExplosionBullet", out var Pro) && Pro != null) QBossBullet = Pro;
            }
            if (QBossBullet != null)
            {
                Vector2 vector = __instance.transform.position;
                long hostileFactionLayers = __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack);
                for (int i = 0; i < Stack; i++)
                {
                    Vector2 vector2FromAngle = HorayUtility.GetVector2FromAngle(360f / Stack * i + Rot);
                    Bullet.Pool.Spawn(QBossBullet, vector, false, EDamageFromType.DirectAttack, "RootExplosion", __instance.RequestStandardDamage() * 0.33f, 0, 0f,
                        __instance, hostileFactionLayers, 0f, vector, vector + vector2FromAngle * 20f, null, null, 0f, EDamageElementalType.Physical, true)
                        .speedScale *= Speed;
                }
            }
        }
    }
}