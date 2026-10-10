using HarmonyLib;
using SPMod.Tool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SPMod.MonsterAI
{
    [HarmonyPatch]
    public static class BirdDemon
    {
        public static int Hard
        {
            get { return ModConfig.HardMode; }
        }
        // UnitAI_BirdDemon 反射字段
        private static readonly FieldInfo? birdDemonField;
        private static readonly FieldInfo? currentPatternIndexField;
        private static readonly FieldInfo? poolCountField;

        // Unit_BirdDemon 反射字段
        private static readonly FieldInfo? meleeAttackDirectionVerticalField;
        private static readonly FieldInfo? meleeAttackZoneBottomLeftField;
        private static readonly FieldInfo? meleeAttackZoneTopRightField;
        private static readonly MethodInfo? createMeleeAttackMethod;
        static BirdDemon()
        {
            try
            {
                // UnitAI_BirdDemon 反射
                Type type = typeof(UnitAI_BirdDemon);
                birdDemonField = AccessTools.Field(type, "birdDemon");
                currentPatternIndexField = AccessTools.Field(type, "currentPatternIndex");
                poolCountField = AccessTools.Field(type, "poolCount");
                StarSephiriaMod.LogInfoBoss("[UnitAI_BirdDemon]初始化反射");

                // Unit_BirdDemon 反射
                type = typeof(Unit_BirdDemon);
                meleeAttackDirectionVerticalField = AccessTools.Field(type, "meleeAttackDirectionVertical");
                meleeAttackZoneBottomLeftField = AccessTools.Field(type, "meleeAttackZoneBottomLeft");
                meleeAttackZoneTopRightField = AccessTools.Field(type, "meleeAttackZoneTopRight");
                createMeleeAttackMethod = AccessTools.Method(type, "CreateMeleeAttack", new Type[] { typeof(Vector2), typeof(Vector2) });
                StarSephiriaMod.LogInfoBoss("[Unit_BirdDemon]初始化反射");
            }
            catch (Exception ex)
            {
                StarSephiriaMod.LogError($"[BirdDemon]反射初始化失败: {ex}");
            }
        }

        [HarmonyPatch(typeof(Unit_BirdDemon))]
        public static class Unit_BirdDemon_Patch
        {
            private static Coroutine? StompAttackCoroutine;

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            static void Update_Postfix(Unit_BirdDemon __instance)
            {
                __instance.circleBulletShieldTimer.time = 8f + Hard * 2f;
            }

            [HarmonyPatch("RpcCreateMeleeAttackWarning")]
            [HarmonyPrefix]
            static void RpcCreateMeleeAttackWarning_Prefix(Unit_BirdDemon __instance, ref int count)
            {
                if (Hard >= 2) count += 4;
                if (Hard >= 3) count += 4;
            }

            [HarmonyPatch("MeleeAttackCoroutine")]
            [HarmonyPrefix]
            static bool MeleeAttackCoroutine_Prefix(Unit_BirdDemon __instance, ref IEnumerator __result)
            {
                if (Hard < 1) return true;
                try
                {
                    __result = CustomMeleeAttackCoroutine(__instance);
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[Unit_BirdDemon][StompCoroutine]出现错误{ex}");
                    return true;
                }
            }

            static IEnumerator CustomMeleeAttackCoroutine(Unit_BirdDemon __instance)
            {
                if (meleeAttackDirectionVerticalField == null || meleeAttackZoneBottomLeftField == null || meleeAttackZoneTopRightField == null || createMeleeAttackMethod == null)
                {
                    StarSephiriaMod.LogError("[Unit_BirdDemon][CustomMeleeAttackCoroutine]反射字段失败");
                    yield break;
                }

                bool meleeAttackDirectionVertical = (bool)meleeAttackDirectionVerticalField.GetValue(__instance);
                Vector2 meleeAttackZoneBottomLeft = (Vector2)meleeAttackZoneBottomLeftField.GetValue(__instance);
                Vector2 meleeAttackZoneTopRight = (Vector2)meleeAttackZoneTopRightField.GetValue(__instance);
                if (meleeAttackZoneBottomLeft == null || meleeAttackZoneTopRight == null)
                {
                    StarSephiriaMod.LogError("[Unit_BirdDemon][CustomMeleeAttackCoroutine]反射字段错误");
                    yield break;
                }

                int count = 4;
                if (meleeAttackDirectionVertical) count += 3;
                if (Hard >= 2) count += 4;
                if (Hard >= 3) count += 4;

                for (int i = 0; i < count; i++)
                {
                    if (i != 0)
                    {
                        yield return new WaitForSeconds(0.125f);
                    }
                    Vector2 vector = __instance.transform.position;
                    if (meleeAttackDirectionVertical)
                    {
                        vector.y = (meleeAttackZoneBottomLeft.y + meleeAttackZoneTopRight.y) / 2f;
                    }
                    else
                    {
                        vector.x = (meleeAttackZoneBottomLeft.x + meleeAttackZoneTopRight.x) / 2f;
                    }
                    Vector2 vector2 = new Vector2(1f, 1f);
                    if (meleeAttackDirectionVertical)
                    {
                        vector2.y = meleeAttackZoneTopRight.y - meleeAttackZoneBottomLeft.y;
                    }
                    else
                    {
                        vector2.x = meleeAttackZoneTopRight.x - meleeAttackZoneBottomLeft.x;
                    }
                    Vector2 vector3;
                    if (meleeAttackDirectionVertical)
                    {
                        vector3 = new Vector2(1.25f, 0f);
                    }
                    else
                    {
                        vector3 = new Vector2(0f, 1.25f);
                    }
                    if (i == 0)
                    {
                        createMeleeAttackMethod?.Invoke(__instance, new object[] { vector, vector2 });
                    }
                    else
                    {
                        createMeleeAttackMethod?.Invoke(__instance, new object[] { vector + vector3 * i, vector2 });
                        createMeleeAttackMethod?.Invoke(__instance, new object[] { vector - vector3 * i, vector2 });
                    }
                }
                yield break;
            }

            [HarmonyPatch("CreateStompAttack")]
            [HarmonyPostfix]
            public static void CreateStompAttack_Postfix(Unit_BirdDemon __instance)
            {
                if (!__instance.isServer) return;
                if (StompAttackCoroutine != null)
                {
                    __instance.StopCoroutine(StompAttackCoroutine);
                }
                StompAttackCoroutine = __instance.StartCoroutine(CustomCreateStompAttack(__instance));
            }

            public static List<Bullet> HomingBullet = new List<Bullet>();
            static IEnumerator CustomCreateStompAttack(Unit_BirdDemon __instance)
            {
                if (!Tools.GetPro("AskardHomingBullet", out GameObject? Bullt)) yield break;
                if (!Tools.GetPro("AskardGlitchHomingBullet", out GameObject? BulltGlitch)) yield break;

                if (Hard < 2)
                {
                    int MaxBulletCount = 5;
                    for (int i = 0; i < 5; i += 1)
                    {
                        Vector3 ToPos = HorayUtility.GetVector3FromAngle(360f / MaxBulletCount * i);
                        ToPos.y *= 0.75f;
                        Vector3 Pos = __instance.transform.position + ToPos * 3f + Vector3.down * 0.01f;
                        Bullet a = Bullet.Pool.Spawn(Bullt, Pos, false, EDamageFromType.DirectAttack, "BasicAttack", __instance.RequestStandardDamage(), 25, 5f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0.6f, Pos, Pos + ToPos * 3f, null, null, 0f, EDamageElementalType.Physical, true);
                        a.TurnOnHoming(true, null, 0.75f);
                        yield return new WaitForSeconds(0.05f);
                    }
                }
                else
                {
                    for (int i = 0; i < 3; i += 1)
                    {
                        Vector3 ToPos = HorayUtility.GetVector3FromAngle(360f / 3 * i);
                        ToPos.y *= 0.75f;
                        Vector3 Pos = __instance.transform.position + ToPos * 1.5f + Vector3.down * 0.01f;
                        Bullet a = Bullet.Pool.Spawn(BulltGlitch, Pos, false, EDamageFromType.DirectAttack, "BasicAttack", __instance.RequestStandardDamage(), 25, 5f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0.6f, Pos, Pos + ToPos * 3f, null, null, 0f, EDamageElementalType.Physical, true);
                        a.TurnOnHoming(true, null, 1f);
                        yield return new WaitForSeconds(0.05f);
                    }
                    for (int i = 0; i < 5; i += 1)
                    {
                        Vector3 ToPos = HorayUtility.GetVector3FromAngle(360f / 5 * i);
                        ToPos.y *= 0.75f;
                        Vector3 Pos = __instance.transform.position + ToPos * 3f + Vector3.down * 0.01f;
                        Bullet a = Bullet.Pool.Spawn(Bullt, Pos, false, EDamageFromType.DirectAttack, "BasicAttack", __instance.RequestStandardDamage(), 25, 5f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0.6f, Pos, Pos + ToPos * 3f, null, null, 0f, EDamageElementalType.Physical, true);
                        a.TurnOnHoming(true, null, 0.5f);
                        yield return new WaitForSeconds(0.05f);
                    }
                }
                yield break;
            }
        }

        [HarmonyPatch(typeof(UnitAI_BirdDemon))]
        public static class UnitAI_BirdDemon_Patch
        {
            private static bool Attack = false;

            [HarmonyPatch("OnAIUpdate_FoundEnemy")]
            [HarmonyPrefix]
            static bool OnAIUpdate_FoundEnemy_Prefix(UnitAI_BirdDemon __instance)
            {
                if (Hard <= 0) return true;
                try
                {
                    if (birdDemonField == null || currentPatternIndexField == null || poolCountField == null)
                    {
                        StarSephiriaMod.LogInfoBoss("[UnitAI_BirdDemon][OnAIUpdate_FoundEnemy]反射获取失败");
                        return false;
                    }

                    Unit_BirdDemon birdDemon = (Unit_BirdDemon)birdDemonField.GetValue(__instance);

                    int currentPatternIndex = (int)currentPatternIndexField.GetValue(__instance);
                    int poolCount = (int)poolCountField.GetValue(__instance);
                    if (birdDemon == null)
                    {
                        StarSephiriaMod.LogInfoBoss("[UnitAI_BirdDemon][OnAIUpdate_FoundEnemy]反射错误");
                        return false;
                    }

                    if (birdDemon.patternState != Unit_BirdDemon.EPatternState.Idle)
                    {
                        return false;
                    }
                    if (birdDemon.IsFastMoving || !birdDemon.IsAppeared)
                    {
                        return false;
                    }
                    float ReMult = 0f;
                    if (__instance.patternIntervalTimer.Update(Time.deltaTime))
                    {
                        if (currentPatternIndex == -1)
                        {
                            if (__instance.patternList.Count == 0)
                            {
                                if (poolCount == 0)
                                {
                                    poolCountField.SetValue(__instance, poolCount + 1);
                                    if (Hard >= 3)
                                    {
                                        __instance.patternList.Add(1);
                                    }
                                    if (__instance.Environment && __instance.Environment is BossEnvironment_BirdDemon)
                                    {
                                        __instance.patternList.Add(5);
                                    }
                                    else
                                    {
                                        __instance.patternList.Add(6);
                                    }
                                }
                                else
                                {
                                    poolCountField.SetValue(__instance, poolCount + 1);
                                    if (Hard < 3)
                                    {
                                        __instance.patternList.Add(0);
                                        __instance.patternList.Add(2);
                                    }
                                    else
                                    {
                                        __instance.patternList.Add(3);
                                    }
                                    __instance.patternList.Add(3);
                                    __instance.patternList.Add(4);
                                    __instance.patternList.Add(6);
                                    __instance.patternList.Shuffle();
                                    if (Hard < 2)
                                    {
                                        if (birdDemon.hp < birdDemon.MaxHp / 2 && poolCount % 2 == 0)
                                        {
                                            __instance.patternList.Add(1);
                                        }
                                        else if (__instance.Environment && __instance.Environment is BossEnvironment_BirdDemon)
                                        {
                                            __instance.patternList.Add(5);
                                        }
                                    }
                                    else if (Hard < 3)
                                    {
                                        if (birdDemon.hp < birdDemon.MaxHp * 0.75f && poolCount % 2 == 0)
                                        {
                                            __instance.patternList.Add(1);
                                        }
                                        if (__instance.Environment && __instance.Environment is BossEnvironment_BirdDemon)
                                        {
                                            __instance.patternList.Add(5);
                                        }
                                    }
                                    else
                                    {
                                        __instance.patternList.Add(1);
                                        if (__instance.Environment && __instance.Environment is BossEnvironment_BirdDemon)
                                        {
                                            __instance.patternList.Add(5);
                                        }
                                    }
                                }
                            }
                            currentPatternIndex = __instance.patternList[0];
                            StarSephiriaMod.LogInfoBoss($"乌鸦当前技能{currentPatternIndex}");
                            __instance.patternList.RemoveAt(0);
                        }
                        Vector2 vector = __instance.CurrentTarget.transform.position - __instance.transform.position;
                        switch (currentPatternIndex)
                        {
                            case 0:
                                birdDemon.FastMove(__instance.CurrentTarget.transform.position, delegate
                                { birdDemon.DoubleAttack(__instance.CurrentTarget.transform); }, 1f);
                                currentPatternIndex = -1;
                                break;
                            case 1:
                                birdDemon.CircleBullet();
                                currentPatternIndex = -1;
                                break;
                            case 2:
                                birdDemon.RangeAttack(__instance.CurrentTarget.transform);
                                currentPatternIndex = -1;
                                break;
                            case 3:
                                {
                                    Vector2 vector2 = __instance.CurrentTarget.transform.position;
                                    if (__instance.Environment)
                                    {
                                        int Value = Random.Range(0, 100);
                                        if (Value <= 33)
                                            vector2 = (vector2 + (Vector2)__instance.Environment.BossZoneCenter) * 0.5f;
                                        else if (Value <= 66)
                                            vector2 = __instance.Environment.BossZoneCenter;
                                    }
                                    birdDemon.FastMove(vector2, delegate { birdDemon.OuterTornado(); }, __instance.tornadoAttackDistance - 1f);
                                    if (Hard >= 3)
                                    {
                                        if (Hard >= 4)
                                        {
                                            birdDemon.DoubleAttack(__instance.CurrentTarget.transform);
                                            birdDemon.RangeAttack(__instance.CurrentTarget.transform);
                                        }
                                        else
                                        {
                                            if (Attack)
                                                birdDemon.DoubleAttack(__instance.CurrentTarget.transform);
                                            else
                                                birdDemon.RangeAttack(__instance.CurrentTarget.transform);
                                            Attack = !Attack;
                                        }
                                    }
                                    currentPatternIndex = -1;
                                    break;
                                }
                            case 4:
                                {
                                    Vector2 vector2 = __instance.CurrentTarget.transform.position;
                                    birdDemon.FastMove(vector2, delegate { birdDemon.Stomp(); }, __instance.tornadoAttackDistance - 1f);
                                    if (Hard >= 4) ReMult = 0.25f;
                                    currentPatternIndex = -1;
                                }
                                break;
                            case 5:
                                if (__instance.Environment)
                                {
                                    BossEnvironment_BirdDemon? bossEnvironment_BirdDemon = __instance.Environment as BossEnvironment_BirdDemon;
                                    if (bossEnvironment_BirdDemon != null)
                                    {
                                        Vector2 vector3 = __instance.transform.position;
                                        if (__instance.Environment)
                                        {
                                            vector3 = __instance.Environment.BossZoneCenter + (Vector3)Random.insideUnitCircle * 5f;
                                        }
                                        List<GameObject> list = bossEnvironment_BirdDemon.areaAttackPatterns.ToList();
                                        if (__instance.hardPatternActivated)
                                        {
                                            list.AddRange(bossEnvironment_BirdDemon.areaAttackPatterns_Hard);
                                        }
                                        birdDemon.Tornado(list, __instance.CurrentTarget, vector3);
                                    }
                                }
                                currentPatternIndex = -1;
                                break;
                            case 6:
                                if (__instance.Environment)
                                {
                                    birdDemon.MeleeAttack(__instance.Environment.BossZoneBottomLeft, __instance.Environment.BossZoneTopRight);
                                }
                                else
                                {
                                    birdDemon.MeleeAttack(__instance.transform.position + new Vector3(-5f, -5f), __instance.transform.position + new Vector3(5f, 5f));
                                }
                                if (Hard >= 4) ReMult = 0.25f;
                                currentPatternIndex = -1;
                                break;
                        }
                        if (currentPatternIndex == -1) __instance.patternIntervalTimer.Ratio = ReMult;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[UnitAI_BirdDemon][OnAIUpdate_FoundEnemy]出现错误{ex}");
                    return true;
                }
            }
        }
    }
}