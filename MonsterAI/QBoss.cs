using HarmonyLib;
using Mirror;
using SPMod.Tool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace SPMod.MonsterAI
{
    [HarmonyPatch]
    public static class QBoss
    {
        public static int Hard
        {
            get { return ModConfig.HardMode; }
        }
        private static bool Start = false;
        // Unit_QBoss 反射字段
        private static readonly FieldInfo? levitationTimerField;
        private static readonly FieldInfo? geometryProjectileCoroutineField;
        private static readonly MethodInfo? RpcSetSortingOrderMethod;
        private static readonly MethodInfo? SpawnGeometryShapeMethod;
        private static readonly MethodInfo? RpcGeometryProjectileEndMethod;
        // UnitAI_QBossAdv 反射字段
        private static readonly FieldInfo? qBossField;
        private static readonly FieldInfo? currentPatternCoroutineField;
        private static readonly MethodInfo? patternTreeLaserCoroutineMethod;
        static QBoss()
        {
            try
            {
                // UnitAI_QBossAdv 反射
                Type type = typeof(UnitAI_QBossAdv);
                qBossField = AccessTools.Field(type, "qBoss");
                currentPatternCoroutineField = AccessTools.Field(type, "currentPatternCoroutine");
                patternTreeLaserCoroutineMethod = AccessTools.Method(type, "PatternTreeLaserCoroutine");
                StarSephiriaMod.LogInfoBoss("[UnitAI_QBossAdv]初始化反射成功");

                Type bossType = typeof(Unit_QBoss);
                levitationTimerField = AccessTools.Field(bossType, "levitationTimer");
                geometryProjectileCoroutineField = AccessTools.Field(bossType, "geometryProjectileCoroutine");
                RpcSetSortingOrderMethod = AccessTools.Method(bossType, "RpcSetSortingOrder");
                SpawnGeometryShapeMethod = AccessTools.Method(bossType, "SpawnGeometryShape");
                RpcGeometryProjectileEndMethod = AccessTools.Method(bossType, "RpcGeometryProjectileEnd");
                StarSephiriaMod.LogInfoBoss("[Unit_QBoss]初始化反射成功");
            }
            catch (Exception ex)
            {
                StarSephiriaMod.LogError($"[QBoss]反射初始化失败: {ex}");
            }
        }

        [HarmonyPatch(typeof(Unit_QBoss))]
        public static class Unit_QBoss_Patch
        {
            private static bool StompAttack = false;
            private static readonly List<int> StompAttackType = new List<int> { };
            private static bool MeleeAttackEffect = false;
            private static Vector3 CrackMeleeShootPos = Vector3.zero;
            private static bool GeometryProjectileUse = false;
            private static readonly List<int> GeometryProjectileType = new List<int> { };
            private static bool CrackMeleeAttackUse = false;
            private static readonly List<int> CrackMeleeAttackType = new List<int> { };
            private static bool MeleeAttackUse = false;
            private static readonly List<int> MeleeAttackType = new List<int> { };
            public static UnitAvatar? Target = null;

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            static void Update_Postfix(Unit_QBoss __instance)
            {
                if (!__instance.isServer) return;
                __instance.rootExplosionRadius = 15f + 5f * Hard;
                if (Hard >= 2 && __instance.stompJumpPhase == Unit_QBoss.EStompJumpPhase.JumpingWait)
                {
                    if(!StompAttack && Hard >= 3)
                    {
                        int No = -1;
                        if (StompAttackType.Count > 0)
                        {
                            No = StompAttackType[0];
                            StompAttackType.RemoveAt(0);
                        }
                        if (StompAttackType.Count <= 0)
                        {
                            StompAttackType.Add(0);
                            StompAttackType.Add(1);
                            StompAttackType.Shuffle();
                            if (No >= 0)
                                StompAttackType.Remove(No);
                        }
                    }
                    StompAttack = true;
                }
                if (StompAttack && __instance.stompJumpPhase == Unit_QBoss.EStompJumpPhase.Down)
                {
                    int SType = 0;
                    if (StompAttackType.Count > 0)
                    {
                        SType = StompAttackType[0];
                        StarSephiriaMod.LogInfoBoss($"砸地：{SType}");
                    }
                    if (SType == 0)
                        __instance.StartCoroutine(FireBulletAndDestroy(__instance));
                    else
                        __instance.StartCoroutine(CrackMeleeAttack(__instance, true));
                    StompAttack = false;
                }
                if (MeleeAttackEffect && Target != null)
                {
                    int Stack = 5;
                    float Dir = (Target.transform.position - __instance.transform.position).GetAngle();
                    float MaxAngle = 180f;
                    for (float i = -MaxAngle * 0.5f; i < MaxAngle * 0.5f; i += MaxAngle / Stack)
                    {
                        Vector2 vector2FromAngle = HorayUtility.GetVector2FromAngle(Dir + i);
                        GameObject gameObject = Object.Instantiate(__instance.stompDownGunpowderPrefab, __instance.transform.position, Quaternion.identity);
                        QBossGunpowder component = gameObject.GetComponent<QBossGunpowder>();
                        NetworkServer.Spawn(gameObject);
                        component.Initialize(__instance, vector2FromAngle, __instance.RequestStandardDamage());
                    }
                    Target = null;
                    MeleeAttackEffect = false;
                }
            }
            private static IEnumerator FireBulletAndDestroy(Unit_QBoss __instance)
            {
                float angleStep = 360f / 36;
                float randomOffset = Random.Range(0f, 360f);
                yield return new WaitForSeconds(0.2f);
                FireBulletRing(__instance, randomOffset, angleStep);
                float Rot = Hard >= 4 ? 0.33f : 0.5f;
                float RTime = Hard >= 4 ? 0.3f : 0.4f;
                if (Hard >= 3)
                {
                    yield return new WaitForSeconds(RTime);
                    FireBulletRing(__instance, randomOffset + angleStep * Rot, angleStep);
                }
                if (Hard >= 4)
                {
                    yield return new WaitForSeconds(RTime);
                    FireBulletRing(__instance, randomOffset + angleStep * Rot * 2f, angleStep);
                }
                yield break;
            }
            private static void FireBulletRing(Unit_QBoss __instance, float offsetAngle, float angleStep, float Speed = 1f)
            {
                if (Tools.GetPro("QBossRootExplosionBullet", out var Pro) && Pro != null)
                {
                    Vector2 vector = __instance.transform.position + Vector3.up * 0.5f;
                    long hostileFactionLayers = __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack);
                    for (int i = 0; i < 36; i++)
                    {
                        Vector2 vector2FromAngle = HorayUtility.GetVector2FromAngle(offsetAngle + angleStep * i);
                        Bullet a = Bullet.Pool.Spawn(Pro, vector, false, EDamageFromType.DirectAttack, "RootExplosion", __instance.RequestStandardDamage() * 0.33f, 0, 0f,
                            __instance, hostileFactionLayers, 0f, vector, vector + vector2FromAngle * 20f, null, null, 0f, EDamageElementalType.Physical, true);
                        a.speedScale = Speed;
                    }
                }
            }

            [HarmonyPatch("Anim_CrackMeleeAttack_Fire")]
            [HarmonyPostfix]
            static void Anim_CrackMeleeAttack_Fire_Postfix(Unit_QBoss __instance)
            {
                if (!__instance.isServer) return;
                if (Hard >= 1)
                {
                    if (Hard >= 2 && !CrackMeleeAttackUse)
                    {
                        int No = -1;
                        if (CrackMeleeAttackType.Count > 0)
                        {
                            No = CrackMeleeAttackType[0];
                            CrackMeleeAttackType.RemoveAt(0);
                        }
                        if (CrackMeleeAttackType.Count <= 0)
                        {
                            CrackMeleeAttackType.Add(0);
                            CrackMeleeAttackType.Add(1);
                            CrackMeleeAttackType.Shuffle();
                            if (No >= 0)
                                CrackMeleeAttackType.Remove(No);
                        }
                        CrackMeleeAttackUse = true;
                    }
                    int UType = 0;
                    if (CrackMeleeAttackType.Count > 0)
                    {
                        UType = CrackMeleeAttackType[0];
                        StarSephiriaMod.LogInfoBoss($"裂隙打击：{UType}");
                    }
                    __instance.StartCoroutine(CrackMeleeAttack(__instance, true, UType));
                    CrackMeleeAttackUse = false;
                }
            }
            private static IEnumerator CrackMeleeAttack(Unit_QBoss __instance, bool UseSecond, int Type = 0)
            {
                yield return new WaitForSeconds(0.25f);
                CrackMeleeShootPos = __instance.transform.position;
                if (Type == 0)
                {
                    ShootCrackMelee(__instance, 5f, 6);
                    if (Hard >= 3 && UseSecond)
                    {
                        yield return new WaitForSeconds(0.5f);
                        ShootCrackMelee(__instance, 10f, 12);
                    }
                    if (Hard >= 4)
                    {
                        yield return new WaitForSeconds(0.3f);
                        if (Target == null) yield break;
                        for (int i = 0; i < 3; i++)
                        {
                            yield return new WaitForSeconds(0.3f);
                            List<GameObject> shuffledPrefabs = new List<GameObject>(__instance.crackPrefabs);
                            SpawnShuffled(__instance, shuffledPrefabs[i], Target.transform.position);
                        }
                    }
                    yield break;
                }
                if (Type == 1)
                {
                    float angleStep = 360f / 36;
                    float randomOffset = Random.Range(0f, 360f);
                    yield return new WaitForSeconds(0.1f);
                    FireBulletRing(__instance, randomOffset, angleStep, 1.5f);
                    float Rot = Hard >= 4 ? 0.33f : 0.5f;
                    float RTime = Hard >= 4 ? 0.2f : 0.3f;
                    if (Hard >= 3)
                    {
                        yield return new WaitForSeconds(RTime);
                        FireBulletRing(__instance, randomOffset + angleStep * Rot, angleStep, 1.5f);
                    }
                    if (Hard >= 4)
                    {
                        yield return new WaitForSeconds(RTime);
                        FireBulletRing(__instance, randomOffset + angleStep * Rot * 2f, angleStep, 1.5f);
                    }
                }
            }
            private static void ShootCrackMelee(Unit_QBoss __instance, float Dis, int Stack)
            {
                List<GameObject> shuffledPrefabs = new List<GameObject>(__instance.crackPrefabs);
                Vector3 Pos = CrackMeleeShootPos;
                float MaxAngle = 360f;
                int Type = Random.Range(0, shuffledPrefabs.Count);
                for (float i = 0f; i < MaxAngle; i += MaxAngle / Stack)
                {
                    Vector2 Speed = HorayUtility.GetVector2FromAngle(i);
                    SpawnShuffled(__instance, shuffledPrefabs[Type], Pos + (Vector3)Speed * Dis);
                    Type += 1;
                    if (Type >= shuffledPrefabs.Count) Type -= shuffledPrefabs.Count;
                }
            }
            private static void SpawnShuffled(Unit_QBoss __instance, GameObject shuffled, Vector2 Pos)
            {
                GameObject gameObject3 = Object.Instantiate(shuffled, Pos, Quaternion.identity);
                QBossCrack component = gameObject3.GetComponent<QBossCrack>();
                component.Initialize(__instance, __instance.RequestStandardDamage());
                NetworkServer.Spawn(gameObject3);
            }

            [HarmonyPatch("Anim_MeleeAttack_First_Warning")]
            [HarmonyPostfix]
            static void Anim_MeleeAttack_First_Warning_Postfix(Unit_QBoss __instance)
            {
                if (!__instance.isServer) return;
                if (Hard >= 2)
                {
                    if (!MeleeAttackUse)
                    {
                        int No = -1;
                        if (MeleeAttackType.Count > 0)
                        {
                            No = MeleeAttackType[0];
                            MeleeAttackType.RemoveAt(0);
                        }
                        if (MeleeAttackType.Count <= 0)
                        {
                            MeleeAttackType.Add(0);
                            MeleeAttackType.Add(1);
                            MeleeAttackType.Shuffle();
                            if (No >= 0)
                                MeleeAttackType.Remove(No);
                        }
                        MeleeAttackUse = true;
                    }
                    int UType = 0;
                    if (MeleeAttackType.Count > 0)
                    {
                        UType = MeleeAttackType[0];
                        StarSephiriaMod.LogInfoBoss($"二连击：{UType}");
                    }
                    if (UType == 1 && Target != null)
                    {
                        __instance.StartCoroutine(MeleeMove(__instance, Target));
                    }
                }
            }
            private static IEnumerator MeleeMove(Unit_QBoss __instance, UnitAvatar Target)
            {
                yield return new WaitForSeconds(0.1f);
                float Speed = 7f;
                while (MeleeAttackUse)
                {
                    Vector2 Vec = Target.transform.position - __instance.transform.position;
                    __instance.TopdownRigidbody.RequestMove(Vec * Speed * Time.deltaTime);
                    Speed *= 0.96f;
                    yield return null;
                }
                yield break;
            }

            [HarmonyPatch("Anim_MeleeAttack_First_Fire")]
            [HarmonyPostfix]
            static void Anim_MeleeAttack_First_Fire_Postfix(Unit_QBoss __instance)
            {
                if (!__instance.isServer) return;
                MeleeAttackUse = false;
                int UType = 0;
                if (MeleeAttackType.Count > 0)
                {
                    UType = MeleeAttackType[0];
                }
                if (UType == 0)
                {
                    if (Hard >= 1)
                    {
                        MeleeAttackEffect = true;
                    }
                }
                else if(Hard >= 3)
                {
                    __instance.StartCoroutine(CrackMeleeAttack(__instance, false));
                }
            }

            [HarmonyPatch("Anim_MeleeAttack_Second_Fire")]
            [HarmonyPostfix]
            static void Anim_MeleeAttack_Second_Fire_Postfix(Unit_QBoss __instance)
            {
                if (!__instance.isServer) return;
                if (Hard >= 3)
                {
                    __instance.StartCoroutine(CrackMeleeAttack(__instance, true));
                }
            }

            [HarmonyPatch("GeometryProjectileCoroutine")]
            [HarmonyPrefix]
            static bool GeometryProjectileCoroutine_Prefix(Unit_QBoss __instance, UnitAvatar target, ref IEnumerator __result)
            {
                if (!__instance.isServer) return true;
                if (Hard <= 0) return true;

                int GPType = 0;
                if (Hard >= 2 && !GeometryProjectileUse)
                {
                    int No = -1;
                    if (GeometryProjectileType.Count > 0)
                    {
                        No = GeometryProjectileType[0];
                        GeometryProjectileType.RemoveAt(0);
                    }
                    if (GeometryProjectileType.Count <= 0)
                    {
                        GeometryProjectileType.Add(0);
                        GeometryProjectileType.Add(1);
                        GeometryProjectileType.Add(2);
                        GeometryProjectileType.Shuffle();
                        if (NetworkServer.connections.Count > 1)
                            GeometryProjectileType.Remove(1);
                        if (No >= 0)
                            GeometryProjectileType.Remove(No);
                    }
                    GeometryProjectileUse = true;
                }
                if (GeometryProjectileType.Count > 0)
                {
                    GPType = GeometryProjectileType[0];
                    StarSephiriaMod.LogInfoBoss($"意念球：{GPType}");
                }
                __result = GeometryProjectileCoroutine(__instance, target, GPType);
                GeometryProjectileUse = false;
                return false;
            }
            private static IEnumerator GeometryProjectileCoroutine(Unit_QBoss __instance, UnitAvatar target, int GPType)
            {
                if (levitationTimerField == null || RpcSetSortingOrderMethod == null || SpawnGeometryShapeMethod == null || RpcGeometryProjectileEndMethod == null || geometryProjectileCoroutineField == null)
                {
                    StarSephiriaMod.LogError("[QBoss]初始化反射错误");
                    yield break;
                }
                __instance.NetworklevitationPhase = Unit_QBoss.ELevitationPhase.Rising;

                levitationTimerField.SetValue(__instance, 0f);
                RpcSetSortingOrderMethod.Invoke(__instance, new object[] { 5 });
                while (__instance.levitationPhase == Unit_QBoss.ELevitationPhase.Rising)
                {
                    yield return null;
                }

                float RTime = 1.8f;
                if (Hard >= 3) RTime = 1.2f;
                yield return new WaitForSeconds(RTime);
                float travelTime = __instance.geometryProjectileRadius / __instance.geometryProjectileSpeed;
                switch (GPType)
                {
                    case 0:
                        {
                            travelTime *= 1f - 0.12f * Hard;
                            int Count = Hard >= 4 ? 8 : 4;
                            Unit_QBoss.GeometryShapeData[] shapes = new Unit_QBoss.GeometryShapeData[]
                            {
                                new Unit_QBoss.GeometryShapeData(){  vertexCount = Count, angleOffset = 0 },
                                new Unit_QBoss.GeometryShapeData(){  vertexCount = Count, angleOffset = 45 },
                                new Unit_QBoss.GeometryShapeData(){  vertexCount = Count, angleOffset = 0 },
                                new Unit_QBoss.GeometryShapeData(){  vertexCount = Count, angleOffset = 45 },
                                new Unit_QBoss.GeometryShapeData(){  vertexCount = 8, angleOffset = 0 }
                            };
                            for (int s = 0; s < shapes.Length; s++)
                            {
                                Vector2 vector = target ? target.transform.position : __instance.transform.position;
                                SpawnGeometryShapeMethod.Invoke(__instance, new object[] { vector, shapes[s] });
                                yield return new WaitForSeconds(travelTime + 0.5f);
                                if (s < shapes.Length - 1)
                                {
                                    yield return new WaitForSeconds(__instance.geometryShapeInterval);
                                }
                            }
                        }
                        break;
                    case 1:
                        {
                            travelTime += 0.5f;
                            Vector2 vector = __instance.transform.position + Vector3.up * 0.5f;
                            long hostileFactionLayers = __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack);
                            float angleStep = 360f / 108;
                            float randomOffset = Random.Range(0f, 360f);
                            int Stack = 8;
                            for (int s = 0; s < Stack; s++)
                            {
                                if (Tools.GetPro("QBossRootExplosionBullet", out var Pro) && Pro != null)
                                {
                                    for (int i = 0; i < 108; i++)
                                    {
                                        Vector2 vector2FromAngle = HorayUtility.GetVector2FromAngle(randomOffset + angleStep * i);
                                        Bullet.Pool.Spawn(Pro, vector, false, EDamageFromType.DirectAttack, "RootExplosion", __instance.RequestStandardDamage() * 0.5f, 0, 0f,
                                            __instance, hostileFactionLayers, 0f, vector, vector + vector2FromAngle * 20f, null, null, 0f, EDamageElementalType.Physical, true);
                                    }
                                }
                                travelTime *= 1f - s * 0.05f;
                                yield return new WaitForSeconds(travelTime);
                                if (s < Stack - 1)
                                {
                                    yield return new WaitForSeconds(__instance.geometryShapeInterval);
                                }
                            }
                        }
                        break;
                    case 2:
                        {
                            float AllTime = (travelTime + 0.5f) * 4f;
                            float OneTime = 0.2f;
                            int Stack = (int)(AllTime / OneTime);
                            int ExAttack = 0;
                            for (int s = 0; s < Stack; s++)
                            {
                                Vector2 Pos = target ? target.transform.position : __instance.transform.position;
                                yield return new WaitForSeconds(0.05f);
                                List<GameObject> shuffledPrefabs = new List<GameObject>(__instance.crackPrefabs);
                                int Type = Random.Range(0, shuffledPrefabs.Count);
                                GameObject gameObject3 = Object.Instantiate(shuffledPrefabs[Type], Pos, Quaternion.identity);
                                QBossCrack component = gameObject3.GetComponent<QBossCrack>();
                                component.Initialize(__instance, __instance.RequestStandardDamage());
                                NetworkServer.Spawn(gameObject3);
                                if (ExAttack > 6)
                                {
                                    SpawnGeometryShapeMethod.Invoke(__instance, new object[]
                                    {
                                        Pos, new Unit_QBoss.GeometryShapeData()
                                        {
                                            vertexCount = 4,
                                            angleOffset = Hard >= 4 ? 45 : 0
                                        }
                                    });
                                    ExAttack = 0;
                                }
                                ExAttack += 1;
                                yield return new WaitForSeconds(OneTime - 0.05f);
                            }
                            yield return new WaitForSeconds(__instance.geometryShapeInterval);
                        }
                        break;
                }
                __instance.NetworkgeometryProjectileTarget = null;
                RpcGeometryProjectileEndMethod.Invoke(__instance, null);
                __instance.NetworklevitationPhase = Unit_QBoss.ELevitationPhase.Descending;
                while (__instance.levitationPhase == Unit_QBoss.ELevitationPhase.Descending)
                {
                    yield return null;
                }
                __instance.NetworkcurrentAction = Unit_QBoss.EAnimation.None;
                __instance.steeringSpeed = 1f;
                geometryProjectileCoroutineField.SetValue(__instance, null);
                yield break;
            }
        }

        [HarmonyPatch(typeof(UnitAI_QBossAdv))]
        public static class UnitAI_QBossAdv_Patch
        {
            private static int Oldpattern = -1;

            [HarmonyPatch("Awake")]
            [HarmonyPostfix]
            static void Awake_Postfix(UnitAI_QBossAdv __instance)
            {
                Oldpattern = -1;
                Start = false;
            }

            [HarmonyPatch("OnAIUpdate_FoundEnemy")]
            [HarmonyPrefix]
            static bool OnAIUpdate_FoundEnemy_Prefix(UnitAI_QBossAdv __instance)
            {
                if (Hard <= 0) return true;
                try
                {
                    if (qBossField == null || currentPatternCoroutineField == null || patternTreeLaserCoroutineMethod == null)
                    {
                        StarSephiriaMod.LogError("[UnitAI_QBossAdv][OnAIUpdate_FoundEnemy]反射获取失败");
                        return true;
                    }

                    // 通过反射获取字段值
                    Unit_QBoss qBoss = (Unit_QBoss)qBossField.GetValue(__instance);
                    Coroutine currentPatternCoroutine = (Coroutine)currentPatternCoroutineField.GetValue(__instance);
                    if (__instance.hardPatternActivated && __instance.additionalCrackTimer.Update(Time.deltaTime))
                    {
                        List<UnitAvatar> list = new List<UnitAvatar>();
                        foreach (PlayerSpawner playerSpawner in PlayerSpawner.MultiplayerList)
                        {
                            if (playerSpawner && playerSpawner.PlayerAvatar)
                            {
                                list.Add(playerSpawner.PlayerAvatar);
                            }
                        }
                        list.Shuffle();
                        if (list.Count > 0)
                        {
                            int Stack = 1 + Hard;
                            if(Stack > list.Count) Stack = list.Count;
                            for (int i = 0; i < Stack; i++)
                            {
                                qBoss.CreateCrack(list[i], __instance.additionalCrackCount);
                            }
                            __instance.additionalCrackTimer.Ratio = 0f;
                        }
                    }
                    if (!Start && Hard >= 3 && __instance.isServer)
                    {
                        Start = true;
                        qBoss.AddCustomStatUnsafe(TextFile.ReceivedDamageReduction, 30);
                    }
                    if (currentPatternCoroutine == null && qBoss.currentAction == Unit_QBoss.EAnimation.None && __instance.patternTimer.Update(Time.deltaTime))
                    {
                        if (__instance.patternList.Count == 0)
                        {
                            __instance.patternList.Add(0);
                            __instance.patternList.Add(1);
                            __instance.patternList.Add(2);
                            __instance.patternList.Add(3);
                            __instance.patternList.Add(4);
                            __instance.patternList.Add(5);
                            __instance.patternList.Shuffle();
                            if (Oldpattern > 0)
                            {
                                __instance.patternList.Remove(Oldpattern);
                                __instance.patternList.Insert(3, Oldpattern);
                            }
                        }
                        int Pattern = __instance.patternList[0];
                        Oldpattern = Pattern;
                        StarSephiriaMod.LogInfoBoss($"[克里弗]当前攻击{Pattern}");
                        __instance.patternList.RemoveAt(0);
                        if (qBoss.GetCustomStatUnsafe(TextFile.ReceivedDamageReduction) > 0)
                            qBoss.AddCustomStatUnsafe(TextFile.ReceivedDamageReduction, -2);
                        qBoss.SetFastMoveTarget(__instance.CurrentTarget);
                        switch (Pattern)
                        {
                            case 0:
                                {
                                    qBoss.MeleeAttack(true);
                                    Unit_QBoss_Patch.Target = __instance.CurrentTarget;
                                    break;
                                }
                            case 1:
                                if (patternTreeLaserCoroutineMethod.Invoke(__instance, null) is IEnumerator coroutine)
                                {
                                    currentPatternCoroutine = __instance.StartCoroutine(coroutine);
                                    currentPatternCoroutineField.SetValue(__instance, currentPatternCoroutine);
                                }
                                break;
                            case 2:
                                qBoss.StompAttack(__instance.CurrentTarget);
                                break;
                            case 3:
                                qBoss.CrackMeleeAttack(__instance.CurrentTarget, __instance.hardPatternActivated);
                                break;
                            case 4:
                                qBoss.RootExplosion(__instance.CurrentTarget);
                                break;
                            case 5:
                                qBoss.GeometryProjectile(__instance.CurrentTarget);
                                break;
                        }
                    }
                    return false;  // 阻止原方法执行
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError($"[UnitAI_QBossAdv][OnAIUpdate_FoundEnemy]出现错误{ex}");
                    return true;  // 出错时执行原方法
                }
            }
        }
    }
}