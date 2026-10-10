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
    public static class OinkKing
    {
        public static int Hard
        {
            get { return ModConfig.HardMode; }
        }
        // AI 反射
        private static readonly FieldInfo? oinkKingField;
        private static readonly FieldInfo? summonCastingField;
        private static readonly FieldInfo? isPatternDoingField;
        private static readonly FieldInfo? currentPatternCoroutineField;
        private static readonly MethodInfo? Pattern0CoroutineMethod;
        private static readonly MethodInfo? Pattern1CoroutineMethod;
        private static readonly MethodInfo? PatternCoroutine_SummonMagicMethod;
        // UnitAvatar 反射
        private static readonly FieldInfo? currentBulletHellCoroutineField;
        private static readonly MethodInfo? RpcBulletHellEndMethod;

        static OinkKing()
        {
            // AI 反射
            Type type = typeof(UnitAI_OinkKing);
            oinkKingField = AccessTools.Field(type, "oinkKing");
            summonCastingField = AccessTools.Field(type, "summonCasting");
            isPatternDoingField = AccessTools.Field(type, "isPatternDoing");
            currentPatternCoroutineField = AccessTools.Field(type, "currentPatternCoroutine");
            Pattern0CoroutineMethod = AccessTools.Method(type, "Pattern0Coroutine");
            Pattern1CoroutineMethod = AccessTools.Method(type, "Pattern1Coroutine");
            PatternCoroutine_SummonMagicMethod = AccessTools.Method(type, "PatternCoroutine_SummonMagic");

            // UnitAvatar 反射
            type = typeof(Unit_OinkKing);
            currentBulletHellCoroutineField = AccessTools.Field(type, "currentBulletHellCoroutine");
            RpcBulletHellEndMethod = AccessTools.Method(type, "RpcBulletHellEnd");
        }

        [HarmonyPatch(typeof(Unit_OinkKing))]
        public static class Unit_OinkKing_Patch
        {
            private static int BulletHellType1Type = 1;
            public static IEnumerator BulletHellType1(Unit_OinkKing __instance, TopdownActorRenderingMetadata target)
            {
                yield return new WaitForSeconds(0.5f);
                GameObject Pro = __instance.bulletHellProjectilePrefab;
                Vector3 Pos = __instance.bulletHellPoint.position;
                float Damage = __instance.RequestStandardDamage() * 0.33f;
                long Layers = __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack);

                BulletHellType1Type = 1;
                if (Hard >= 3) BulletHellType1Type = Random.Range(1, 4);
                switch(BulletHellType1Type)
                {
                    case 1:
                        for (int i = 0; i < 6; i++)
                        {
                            if (__instance.bulletHellFireSound) __instance.bulletHellFireSound.Play();

                            Bullet.Pool.Spawn(Pro, Pos, false, EDamageFromType.DirectAttack, "BasicAttack", Damage, 0, 0f, __instance, Layers, 0f, Pos, target.transform.position + new Vector3(0f, target.CenterYPos), null, null, 0f, EDamageElementalType.Physical, true);
                            yield return new WaitForSeconds(0.4f);
                        }
                        break;
                    case 2:
                        yield return new WaitForSeconds(2.5f);
                        for (float num = Random.Range(0f, 30f); num < 360f; num += 24f)
                        {
                            Bullet.Pool.Spawn(Pro, Pos, false, EDamageFromType.DirectAttack, "BasicAttack", Damage, 0, 0f, __instance, Layers, 0f, Pos, Pos + HorayUtility.GetVector3FromAngle(num), null, null, 0f, EDamageElementalType.Physical, true);
                        }
                        break;
                    default:
                        float angle = 0f;
                        for (int i = 0; i < 12; i++)
                        {
                            if (__instance.bulletHellFireSound) __instance.bulletHellFireSound.Play();
                            Bullet.Pool.Spawn(Pro, Pos, false, EDamageFromType.DirectAttack, "BasicAttack", Damage, 0, 0f, __instance, Layers, 0f, Pos, Pos + HorayUtility.GetVector3FromAngle(angle), null, null, 0f, EDamageElementalType.Physical, true);
                            yield return new WaitForSeconds(0.2f);
                            angle += 24f;
                        }
                        break;
                }
            }

            [HarmonyPatch("BulletHellType2")]
            [HarmonyPrefix]
            static bool BulletHellType2_Prefix(Unit_OinkKing __instance, ref IEnumerator __result)
            {
                if (Hard < 3) return true;
                try
                {
                    __result = BulletHellType2(__instance);
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[Unit_OinkKing][BulletHellType2]出现错误{ex}");
                    return true;
                }
            }
            public static IEnumerator BulletHellType2(Unit_OinkKing __instance)
            {
                if (RpcBulletHellEndMethod == null)
                {
                    StarSephiriaMod.LogError("[Unit_OinkKing][BulletHellType2]获取反射失败");
                    yield break;
                }
                yield return new WaitForSeconds(0.5f);
                GameObject Pro = __instance.bulletHellProjectilePrefab;
                Vector3 Pos = __instance.bulletHellPoint.position;
                float Damage = __instance.RequestStandardDamage() * 0.33f;
                long Layers = __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack);
                for (int i = 0; i < 6; i++)
                {
                    if (Hard >= 3)
                    {
                        foreach (PlayerSpawner Player in PlayerSpawner.MultiplayerList)
                        {
                            if (Player)
                            {
                                Vector3 Vec = (Player.transform.position - Pos).normalized;
                                Bullet.Pool.Spawn(Pro, Pos, false, EDamageFromType.DirectAttack, "BasicAttack", Damage, 0, 0f, __instance, Layers, 0f, Pos, Pos + Vec * 2f, null, null, 0f, EDamageElementalType.Physical, true);
                            }
                        }
                    }

                    if (__instance.bulletHellFireSound) __instance.bulletHellFireSound.Play();
                    for (float num = Random.Range(0f, 15f); num < 360f; num += 15f)
                    {
                        Bullet.Pool.Spawn(Pro, Pos, false, EDamageFromType.DirectAttack, "BasicAttack", Damage, 0, 0f, __instance, Layers, 0f, Pos, Pos + HorayUtility.GetVector3FromAngle(num), null, null, 0f, EDamageElementalType.Physical, true);
                    }
                    yield return new WaitForSeconds(0.5f);
                }
                __instance.steeringSpeed += 1f;
                RpcBulletHellEndMethod.Invoke(__instance, null);
                yield break;
            }

            [HarmonyPatch("BulletHellType3")]
            [HarmonyPrefix]
            static bool BulletHellType3_Prefix(Unit_OinkKing __instance, TopdownActorRenderingMetadata target, ref IEnumerator __result)
            {
                if (Hard < 3) return true;
                try
                {
                    __result = BulletHellType3(__instance, target);
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[Unit_OinkKing][BulletHellType3]出现错误{ex}");
                    return true;
                }
            }
            public static IEnumerator BulletHellType3(Unit_OinkKing __instance, TopdownActorRenderingMetadata target)
            {
                if (RpcBulletHellEndMethod == null)
                {
                    StarSephiriaMod.LogError("[Unit_OinkKing][BulletHellType2]获取反射失败");
                    yield break;
                }
                yield return new WaitForSeconds(0.5f);
                GameObject Pro = __instance.bulletHellProjectilePrefab;
                Vector3 Pos = __instance.bulletHellPoint.position;
                float Damage = __instance.RequestStandardDamage() * 0.33f;
                long Layers = __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack);
                for (int i = 0; i < 18; i++)
                {
                    if (__instance.bulletHellFireSound) __instance.bulletHellFireSound.Play();

                    for (int k = -3; k <= 3; k++)
                    {
                        float angle = HorayUtility.GetAngle(target.transform.position + new Vector3(0f, target.CenterYPos), Pos);
                        Bullet.Pool.Spawn(Pro, Pos, false, EDamageFromType.DirectAttack, "BasicAttack", Damage, 0, 0f, __instance, Layers, 0f, Pos, Pos + HorayUtility.GetVector3FromAngle(angle + 25f * k), null, null, 0f, EDamageElementalType.Physical, true);
                    }
                    yield return new WaitForSeconds(0.15f);
                }
                __instance.steeringSpeed += 1f;
                RpcBulletHellEndMethod.Invoke(__instance, null);
                yield break;
            }

            [HarmonyPatch("BulletHellType4")]
            [HarmonyPrefix]
            static bool BulletHellType4_Prefix(Unit_OinkKing __instance, ref IEnumerator __result)
            {
                if (Hard < 3) return true;
                try
                {
                    __result = BulletHellType4(__instance);
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[Unit_OinkKing][BulletHellType4]出现错误{ex}");
                    return true;
                }
            }
            public static IEnumerator BulletHellType4(Unit_OinkKing __instance)
            {
                if (RpcBulletHellEndMethod == null)
                {
                    StarSephiriaMod.LogError("[Unit_OinkKing][BulletHellType2]获取反射失败");
                    yield break;
                }
                yield return new WaitForSeconds(0.5f);
                GameObject Pro = __instance.bulletHellProjectilePrefab;
                Vector3 Pos = __instance.bulletHellPoint.position;
                float Damage = __instance.RequestStandardDamage() * 0.33f;
                long Layers = __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack);
                float angle = 0f;
                for (int i = 0; i < 55; i++)
                {
                    if (__instance.bulletHellFireSound) __instance.bulletHellFireSound.Play();
                    Bullet.Pool.Spawn(Pro, Pos, false, EDamageFromType.DirectAttack, "BasicAttack", Damage, 0, 0f, __instance, Layers, 0f, Pos, Pos + HorayUtility.GetVector3FromAngle(angle), null, null, 0f, EDamageElementalType.Physical, true);
                    Bullet.Pool.Spawn(Pro, Pos, false, EDamageFromType.DirectAttack, "BasicAttack", Damage, 0, 0f, __instance, Layers, 0f, Pos, Pos + HorayUtility.GetVector3FromAngle(angle + 180f), null, null, 0f, EDamageElementalType.Physical, true);
                    yield return new WaitForSeconds(0.06f);
                    angle += 15f;
                }
                __instance.steeringSpeed += 1f;
                RpcBulletHellEndMethod.Invoke(__instance, null);
                yield break;
            }

            [HarmonyPatch("BulletHellType5")]
            [HarmonyPrefix]
            static bool BulletHellType5_Prefix(Unit_OinkKing __instance, TopdownActorRenderingMetadata target, ref IEnumerator __result)
            {
                if (Hard < 3) return true;
                try
                {
                    __result = BulletHellType5(__instance, target);
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[Unit_OinkKing][BulletHellType3]出现错误{ex}");
                    return true;
                }
            }
            public static IEnumerator BulletHellType5(Unit_OinkKing __instance, TopdownActorRenderingMetadata target)
            {
                if (RpcBulletHellEndMethod == null)
                {
                    StarSephiriaMod.LogError("[Unit_OinkKing][BulletHellType2]获取反射失败");
                    yield break;
                }
                yield return new WaitForSeconds(0.5f);
                GameObject Pro = __instance.bulletHellProjectilePrefab;
                Vector3 Pos = __instance.bulletHellPoint.position;
                float Damage = __instance.RequestStandardDamage() * 0.33f;
                long Layers = __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack);
                for (int i = 0; i < 40; i++)
                {
                    if (__instance.bulletHellFireSound) __instance.bulletHellFireSound.Play();

                    float Ang = HorayUtility.GetAngle(target.transform.position + new Vector3(0f, target.CenterYPos), __instance.bulletHellPoint.position);
                    for (int k = -1; k <= 1; k += 2)
                    {
                        float Sin = 70f * Mathf.Sin(i * 3.14159274f / 20f);
                        Vector3 vector3FromAngle = HorayUtility.GetVector3FromAngle(Ang + Sin * k);
                        Bullet.Pool.Spawn(Pro, Pos, false, EDamageFromType.DirectAttack, "BasicAttack", Damage, 0, 0f, __instance, Layers, 0f, Pos, Pos + vector3FromAngle, null, null, 0f, EDamageElementalType.Physical, true);

                        float Cos = 70f * Mathf.Cos(i * 3.14159274f / 20f);
                        vector3FromAngle = HorayUtility.GetVector3FromAngle(Ang + Cos * k);
                        Bullet.Pool.Spawn(Pro, Pos, false, EDamageFromType.DirectAttack, "BasicAttack", Damage, 0, 0f, __instance, Layers, 0f, Pos, Pos + vector3FromAngle, null, null, 0f, EDamageElementalType.Physical, true);
                    }
                    yield return new WaitForSeconds(0.125f);
                }
                __instance.steeringSpeed += 1f;
                RpcBulletHellEndMethod.Invoke(__instance, null);
                yield break;
            }
        }

        [HarmonyPatch(typeof(UnitAI_OinkKing))]
        public static class UnitAI_OinkKing_Patch
        {
            private static int Oldpattern = -1;
            private static readonly List<int> Pattern = new List<int>();
            private static readonly Timer GravityExplosionTimer = new Timer(6f);

            [HarmonyPatch("OnAIUpdate_FoundEnemy")]
            [HarmonyPrefix]
            static bool OnAIUpdate_FoundEnemy_Prefix(UnitAI_OinkKing __instance)
            {
                if (Hard <= 0) return true;
                try
                {
                    if (oinkKingField == null ||
                        summonCastingField == null ||
                        isPatternDoingField == null ||
                        currentPatternCoroutineField == null ||
                        Pattern0CoroutineMethod == null ||
                        Pattern1CoroutineMethod == null ||
                        PatternCoroutine_SummonMagicMethod == null)
                    {
                        StarSephiriaMod.LogError("[UnitAI_OinkKing][Update]获取反射失败");
                        return true;
                    }

                    Unit_OinkKing oinkKing = (Unit_OinkKing)oinkKingField.GetValue(__instance);
                    bool summonCasting = (bool)summonCastingField.GetValue(__instance);
                    bool isPatternDoing = (bool)isPatternDoingField.GetValue(__instance);
                    Coroutine currentPatternCoroutine = (Coroutine)currentPatternCoroutineField.GetValue(__instance);
                    if (Hard <= 2)
                        GravityExplosionTimer.time = 6f;
                    else
                    {
                        GravityExplosionTimer.time = 2f + 4f * oinkKing.hp / oinkKing.MaxHp;
                    }
                    if (summonCasting)
                    {
                        __instance.Avatar.ForceAimToPosition(__instance.transform.position + new Vector3(__instance.Avatar.TopdownRigidbody.Direction * 3f, 0f));
                    }
                    if (__instance.IsDramaticDying)
                    {
                        return false;
                    }
                    if (__instance.CurrentTarget && !summonCasting && GravityExplosionTimer.Update(Time.deltaTime))
                    {
                        oinkKing.GravityBallExplosion(Hard, __instance.CurrentTarget);
                    }
                    if (oinkKing.CurrentAction != Unit_OinkKing.EAnimation.None || isPatternDoing)
                    {
                        return false;
                    }
                    float IdleSpeed = 1f;
                    if (__instance.hardPatternActivated) IdleSpeed *= 1.25f;
                    if (Hard >= 4) IdleSpeed *= 1.1f;
                    IdleSpeed *= 1f + 0.1f * Hard;
                    if (__instance.mainPatternTimer.Update(Time.deltaTime * IdleSpeed))
                    {
                        if (Pattern.Count <= 0)
                        {
                            Pattern.Add(0);
                            Pattern.Add(1);
                            Pattern.Add(2);
                            Pattern.Add(3);
                            Pattern.Shuffle();
                            if (Oldpattern >= 0)
                            {
                                Pattern.Remove(Oldpattern);
                                Pattern.Insert(2, Oldpattern);
                            }
                        }
                        int UsePattern = Pattern[0];
                        Oldpattern = UsePattern;
                        Pattern.RemoveAt(0);
                        StarSephiriaMod.LogInfoBoss($"猪猪当前技能：{UsePattern}");
                        isPatternDoing = true;

                        IEnumerator Pattern0Coroutine = (IEnumerator)Pattern0CoroutineMethod.Invoke(__instance, null);
                        IEnumerator Pattern1Coroutine = (IEnumerator)Pattern1CoroutineMethod.Invoke(__instance, null);
                        IEnumerator PatternCoroutine_SummonMagic = (IEnumerator)PatternCoroutine_SummonMagicMethod.Invoke(__instance, null);
                        switch (UsePattern)
                        {
                            case 1:
                                currentPatternCoroutine = __instance.StartCoroutine(Pattern1Coroutine);
                                currentPatternCoroutineField.SetValue(__instance, currentPatternCoroutine);
                                break;
                            case 2:
                                currentPatternCoroutine = __instance.StartCoroutine(Pattern1Coroutine);
                                currentPatternCoroutineField.SetValue(__instance, currentPatternCoroutine);
                                break;
                            case 3:
                                currentPatternCoroutine = __instance.StartCoroutine(PatternCoroutine_SummonMagic);
                                currentPatternCoroutineField.SetValue(__instance, currentPatternCoroutine);
                                __instance.mainPatternTimer.Ratio = 0.5f;
                                break;
                            default:
                                currentPatternCoroutine = __instance.StartCoroutine(Pattern0Coroutine);
                                currentPatternCoroutineField.SetValue(__instance, currentPatternCoroutine);
                                break;
                        }
                    }
                    isPatternDoingField.SetValue(__instance, isPatternDoing);
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[UnitAI_OinkKing][OnAIUpdate_FoundEnemy]出现错误: {ex}");
                    return true;
                }
            }

            [HarmonyPatch("Pattern1Coroutine")]
            [HarmonyPrefix]
            static bool Pattern1Coroutine_Prefix(UnitAI_OinkKing __instance, ref IEnumerator __result)
            {
                if (Hard < 0) return true;
                try
                {
                    __result = Pattern1Coroutine(__instance);
                    return false;
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[UnitAI_OinkKing][Pattern1Coroutine]出现错误{ex}");
                    return true;
                }
            }
            private static IEnumerator Pattern1Coroutine(UnitAI_OinkKing __instance)
            {
                if (oinkKingField == null || isPatternDoingField == null || currentBulletHellCoroutineField == null)
                {
                    StarSephiriaMod.LogError("[UnitAI_OinkKing][Pattern1Coroutine]获取反射失败");
                    yield break;
                }
               
                Unit_OinkKing oinkKing = (Unit_OinkKing)oinkKingField.GetValue(__instance);
                if (Hard >= 3 && __instance.CurrentTarget)
                {
                    currentBulletHellCoroutineField.SetValue(oinkKing, oinkKing.StartCoroutine(Unit_OinkKing_Patch.BulletHellType1(oinkKing, __instance.CurrentTarget.TopdownActor)));
                }
                float Mult = oinkKing.hp / oinkKing.MaxHp;
                for (int i = 0; i < (Mult < 0.33f ? 3 : 2); i++)
                {
                    Mult = oinkKing.hp / oinkKing.MaxHp;
                    Vector3 delta = ((Vector3)__instance.Avatar.CurrentLookingPosition - __instance.transform.position).normalized;
                    if (Hard >= 4)
                    {
                        oinkKing.RockFall(HorayUtility.GetVector3FromAngle(HorayUtility.GetAngleFromVector((Vector2)delta) + 90f), __instance.Environment.BossZone);
                        yield return new WaitForSeconds(1f);
                        yield return new WaitForSeconds(0.25f * Mult);
                        oinkKing.RockFall(delta, __instance.Environment.BossZone);
                        yield return new WaitForSeconds(1f);
                        yield return new WaitForSeconds(0.25f * Mult);
                    }
                    else
                    {
                        yield return new WaitForSeconds(0.4f);
                        oinkKing.RockFall(delta, __instance.Environment.BossZone);
                        yield return new WaitForSeconds(0.3f * Mult);
                        yield return new WaitForSeconds(0.8f);
                    }
                }
                yield return new WaitForSeconds(0.4f);
                isPatternDoingField.SetValue(__instance, false);
                yield break;
            }
        }
    }
}