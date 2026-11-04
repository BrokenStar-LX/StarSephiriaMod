using HarmonyLib;
using Mirror;
using SPMod.ModContent;
using SPMod.Tool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using static SPMod.WeaponPatch;
using static UnityEngine.UI.GridLayoutGroup;
using HeatedBladeAttackData = SPMod.Tool.ModuleComponents.HeatedBladeAttackData;
using Random = UnityEngine.Random;

namespace SPMod
{
    [HarmonyPatch]
    public static class CharmPatch
    {
        public static ItemPosition[] Directions =
        [
            new(-1, 0),
            new(1, 0),
            new(0, -1),
            new(0, 1),
            new(-1, -1),
            new(1, -1),
            new(-1, 1),
            new(1, 1)
        ];

        public static float FrostRelicAddMult(UnitAvatar avatar, float Mult = 1f)
        {
            if (avatar.GetCustomStatUnsafe(TextFile.FrostRelicAddMult) <= 0) return 0f;

            bool flag = avatar.GetCustomStatUnsafe(TextFile.FrostRelicFlame) > 0;
            float Damage = avatar.GetCustomStatUnsafe(flag ? TextFile.FireDamage : TextFile.IceDamage);
            float Mult2 = avatar.GetCustomStatUnsafe(TextFile.FrostRelicAddMult) * 0.01f;
            float Mult3 = 1f + avatar.GetCustomStatUnsafe(TextFile.FrostRelicDamage) * 0.01f;
            return Damage * Mult * Mult2 * Mult3;
        }

        [HarmonyPatch(typeof(Charm_Guillotine))]
        public static class Charm_Guillotine_Patch
        {
            [HarmonyPatch("GetDamage")]
            [HarmonyPostfix]
            static void GetDamage_Prefix(Charm_Guillotine __instance, UnitAvatar avatar, ref float __result)
            {
                __result += FrostRelicAddMult(avatar);
            }
        }

        [HarmonyPatch(typeof(Charm_IceSword))]
        public static class Charm_IceSword_Patch
        {
            [HarmonyPatch("GetDamage")]
            [HarmonyPostfix]
            static void GetDamage_Prefix(Charm_IceSword __instance, UnitAvatar avatar, ref float __result)
            {
                __result += FrostRelicAddMult(avatar);
            }
        }

        [HarmonyPatch(typeof(Charm_IceSpear))]
        public static class Charm_IceSpear_Patch
        {
            [HarmonyPatch("GetDamage")]
            [HarmonyPostfix]
            static void GetDamage_Prefix(Charm_IceSpear __instance, UnitAvatar avatar, ref float __result)
            {
                __result += FrostRelicAddMult(avatar);
            }
        }

        [HarmonyPatch(typeof(Charm_IceHammer))]
        public static class Charm_IceHammer_Patch
        {
            private static Dictionary<UnitAvatar, Charm_IceHammer> IceHammerEffect = [];

            [HarmonyPatch("OnEnabledEffect")]
            [HarmonyPostfix]
            static void OnEnabledEffect_Postfix(Charm_IceHammer __instance)
            {
                __instance.NetworkAvatar.OnParry += OnParry;
                IceHammerEffect[__instance.NetworkAvatar] = __instance;
            }

            private static void OnParry(DamageInstance Damage)
            {
                if (!ModConfig.Instance.EnableWeaponModifier) return;
                if (Damage.victim is UnitAvatar Player)
                {
                    if (Player.GetCustomStatUnsafe(TextFile.IceHammerScythe) <= 0) return;
                    if (IceHammerEffect.TryGetValue(Player, out var IceHammer))
                    {
                        IceHammer.chargingCharm.AddTimer(10f);
                    }
                }
            }

            [HarmonyPatch("OnDisabledEffect")]
            [HarmonyPostfix]
            static void OnDisabledEffect_Postfix(Charm_IceHammer __instance)
            {
                IceHammerEffect.SafeRemove(__instance.NetworkAvatar);
                __instance.NetworkAvatar.OnParry -= OnParry;
            }

            [HarmonyPatch("GetDamage")]
            [HarmonyPostfix]
            static void GetDamage_Postfix(Charm_IceHammer __instance, UnitAvatar avatar, ref float __result)
            {
                float Mult = 1f;
                if (__instance.NetworkAvatar.GetCustomStatUnsafe(TextFile.IceHammerScythe) > 0)
                {
                    Mult = 0.5f;
                }
                __result += FrostRelicAddMult(avatar, Mult);
            }
        }

        [HarmonyPatch(typeof(Charm_AirSlash))]
        public static class Charm_AirSlash_Patch
        {
            private static readonly float[] DamageToLevel = [ 20f, 40f, 60f, 80f, 100f, 120f, 140f, 160f, 180f, 200f, 220f, 240f, 260f, 280f, 300f];

            [HarmonyPatch("GetDamage")]
            [HarmonyPostfix]
            static void GetDamage_Prefix(Charm_AirSlash __instance, UnitAvatar avatar, ref float __result)
            {
                float Mult = 1f;
                if (__instance.NetworkAvatar.IsWeapon(WeaponIDs.GreatswordOfExorcism))
                {
                    bool flag = avatar.GetCustomStatUnsafe(TextFile.FrostRelicFlame) > 0;
                    float Base = 20;
                    float DamageType = avatar.GetCustomStatUnsafe(flag ? TextFile.FireDamage : TextFile.IceDamage);
                    float PercentByLeve = DamageToLevel.SafeRandomAccess(__instance.CurrentLevelToIdx());
                    float Damage = Base + DamageType * PercentByLeve * 0.01f;
                    Damage *= 1f + avatar.GetCustomStatUnsafe(TextFile.FrostRelicDamage) * 0.01f;
                    float ExAttackStack = 1f + avatar.GetCustomStatUnsafe(TextFile.ChargingCharmAmplify);
                    float OrigPercentByLeve = __instance.damagePercentByLevel.SafeRandomAccess(__instance.CurrentLevelToIdx());
                    Mult *= PercentByLeve / OrigPercentByLeve * ExAttackStack;
                    Damage *= ExAttackStack;
                    __result = Damage;
                }
                __result += FrostRelicAddMult(avatar, Mult);
            }

            [HarmonyPatch("AirSlashCoroutine")]
            [HarmonyPrefix]
            static void AirSlashCoroutine_Prefix(Charm_AirSlash __instance, ref int fireCount, ref float damageMultiplier, ref float mpBonus, ref float mpDamage_MaxMP)
            {
                if (__instance.NetworkAvatar.IsWeapon(WeaponIDs.GreatswordOfExorcism))
                {
                    fireCount = 1;
                }
            }
        }

        [HarmonyPatch(typeof(Charm_IceBow))]
        public static class Charm_IceBow_Patch
        {
            [HarmonyPatch("OnUpdate")]
            [HarmonyPrefix]
            static void OnUpdate_Prefix(Charm_IceBow __instance)
            {
                if (IceBowAttack.TryGetValue(__instance.NetworkAvatar, out bool falg) && falg)
                {
                    Vector2 aimedPosition = __instance.NetworkAvatar.AimedPosition;
                    Vector3 targetPos = aimedPosition;
                    UnitAvatar unitAvatar = PlayerInputController.SearchTargetNearestPoint(__instance.NetworkAvatar, aimedPosition, 16f);
                    if (unitAvatar != null)
                    {
                        targetPos = unitAvatar.transform.position;
                    }
                    else
                    {
                        unitAvatar = PlayerInputController.SearchTargetNearestPoint(__instance.NetworkAvatar, __instance.NetworkAvatar.transform.position, 16f);
                        if (unitAvatar != null)
                        {
                            targetPos = unitAvatar.transform.position;
                        }
                    }
                    FireArrow(__instance, targetPos);
                    __instance.RpcFireAnimation();
                    IceBowAttack[__instance.NetworkAvatar] = false;
                }
            }

            private static void FireArrow(Charm_IceBow __instance, Vector3 TargetPos)
            {
                bool Flame = __instance.NetworkAvatar.GetCustomStatUnsafe(TextFile.FrostRelicFlame) > 0;
                UnitAvatar Player = __instance.NetworkAvatar;
                float Damage = Charm_Basic.CalculateDamage(__instance) * 0.5f;
                EDamageElementalType EType = EDamageElementalType.Ice;
                if (Flame) EType = EDamageElementalType.Fire;

                Vector3 To = TargetPos - __instance.bowAnimator.transform.position;
                float Magnitude = To.magnitude;
                GameObject gameObject = Flame ? __instance.bulletPrefab_Flame : __instance.bulletPrefab;
                Vector3 Pos = __instance.bowAnimator.transform.position;
                Vector3 Vec = Pos + HorayUtility.GetVector3FromAngle(To.GetAngle()) * Magnitude;
                long Layers = Player.GetHostileFactionLayers(EDamageFromType.None);
                Bullet.Pool.Spawn(gameObject, Pos, true, EDamageFromType.None, __instance.damageId, Damage, 25, 2f, Player, Layers, 0f, Pos, Vec, null, null, 0f, EType)
                    .swingId = "FROSTRELIC";
            }

            [HarmonyPatch("GetDamage")]
            [HarmonyPostfix]
            static void GetDamage_Prefix(Charm_IceSpear __instance, UnitAvatar avatar, ref float __result)
            {
                __result += FrostRelicAddMult(avatar, 0.25f);
            }
        }

        [HarmonyPatch(typeof(Charm_Basic))]
        public static class Charm_Basic_Patch
        {
            [HarmonyPatch("OnEnabledEffect")]
            [HarmonyPostfix]
            static void OnEnabledEffect_Postfix(Charm_Basic __instance)
            {
                if (__instance.Item.EntityID == 1008 || __instance.Item.EntityID == 1245 || __instance.Item.EntityID == 1251)
                {
                    __instance.NetworkAvatar.AddCustomStatUnsafe(TextFile.ScoreCount, 1);
                }
                if (__instance.Item.EntityID == 1108)
                {
                    __instance.NetworkAvatar.AddCustomStatUnsafe(TextFile.TelescopeCount, 1);
                }
            }

            [HarmonyPatch("OnDisabledEffect")]
            [HarmonyPostfix]
            static void OnDisabledEffect_Postfix(Charm_Basic __instance)
            {
                if (__instance.Item.EntityID == 1008 || __instance.Item.EntityID == 1245 || __instance.Item.EntityID == 1251)
                {
                    __instance.NetworkAvatar.AddCustomStatUnsafe(TextFile.ScoreCount, -1);
                }
                if (__instance.Item.EntityID == 1108)
                {
                    __instance.NetworkAvatar.AddCustomStatUnsafe(TextFile.TelescopeCount, -1);
                }
            }

            [HarmonyPatch("OnUpdate")]
            [HarmonyPostfix]
            static void OnUpdate_Postfix(Charm_Basic __instance)
            {
                if (__instance.NetworkAvatar)
                {
                    if (__instance is Charm_Active Active)
                    {
                        UnitAvatar Player = __instance.NetworkAvatar;
                        if (Player.IsWeaponUseNet(WeaponIDs.TheBarnacle))
                        {
                            if (TheBarnacleEffect.TryGetValue(__instance.NetworkAvatar, out int Value) && Value > 0)
                            {
                                float Mult = 1f;
                                if (__instance.Item.Entity.rarity == EItemRarity.Legend) Mult = 0.2f;
                                if (Active.GetCooldownRatio() > 0f)
                                    Active.CooldownDecrease(1f / Active.GetCooldownTime(out _, __instance.CurrentLevelToIdx(), 0) * Mult);
                                TheBarnacleEffect[__instance.NetworkAvatar] -= 1;
                            }
                        }
                    }
                }
            }

            [HarmonyPatch("RequestCharmDamageBonusOnRoot")]
            [HarmonyPostfix]
            static void RequestCharmDamageBonusOnRoot_Postfix(Charm_Basic __instance, ref int __result)
            {
                if (__instance == null || __instance.netIdentity == null || __instance.netIdentity.netId == 0)
                    return;
                if (__instance.NetworkAvatar)
                {
                    UnitAvatar Player = __instance.NetworkAvatar;
                    if (Tools.CostumeSkinUse(Player.Name, "Turtle_2", false))
                    {
                        ItemEntity itemEntity = ItemDatabase.FindItemById(__instance.Item.EntityID);
                        int Add = 0;
                        switch (itemEntity.rarity)
                        {
                            case EItemRarity.Common:
                                Add = 5;
                                break;
                            case EItemRarity.Uncommon:
                                Add = 10;
                                break;
                            case EItemRarity.Rare:
                                Add = 20;
                                break;
                            case EItemRarity.Legend:
                                Add = 40;
                                break;
                        }
                        if (Add > 0) __result += Add;
                    }//乌龟2
                    if (Tools.CostumeSkinUse(Player.Name, "Bird_3", false))
                    {
                        if (__instance is Charm_Active && __instance is not Charm_SpearofZeus)
                            __result += Player.GetCustomStatUnsafe(TextFile.DashAttackDamageBonus).IntMult(0.8f);
                    }//老鹰3
                }
            }

            [HarmonyPatch("CalculateDamage")]
            [HarmonyPostfix]
            static void CalculateDamage_Postfix(Charm_Basic charm, ref float __result)
            {
                if (charm == null || charm.netIdentity == null || charm.netIdentity.netId == 0)
                {
                    return;
                }
                if (charm.NetworkAvatar)
                {
                    UnitAvatar Player = charm.NetworkAvatar;
                    if (Player.IsWeaponUseNet(WeaponIDs.TheBarnacle))
                    {
                        if (charm is Charm_Active)
                        {
                            float Mult = 1f + charm.RequestCharmDamageBonusOnRoot() * 0.01f;
                            float DamageMult = 2f;
                            if (charm.Item.Entity.rarity == EItemRarity.Rare) DamageMult = 0.1f;
                            __result += Player.GetCustomStatUnsafe(TextFile.DamageReduction) * DamageMult * Mult;
                        }
                    }//藤壶
                    if (Tools.CostumeSkinUse(Player.Name, "Deer_3", false))
                    {
                        if (charm.Item.Position == new ItemPosition(0, 0))
                        {
                            if ((charm is Charm_Magic) == false && (charm is Charm_Active) == false)
                            {
                                float Mult = 1f + charm.RequestCharmDamageBonusOnRoot() * 0.01f;
                                __result += Tools.BaseStatToMax(Player) * 0.8f * Mult;
                            }
                        }
                    }//鹿3
                }
            }
        }

        [HarmonyPatch(typeof(Charm_CompassDamageUp))]
        public static class Charm_CompassDamageUp_Patch
        {
            [HarmonyPatch("OnRequestCharmDamageBonus")]
            [HarmonyPostfix]
            static void OnRequestCharmDamageBonus_Postfix(Charm_CompassDamageUp __instance, Charm_Basic rootCharm, ref CharmDamageBonusContext context, ref int __result)
            {
                if (__instance == null || rootCharm == null) return;

                UnitAvatar? Player = __instance.NetworkAvatar;
                if (Player == null || !Tools.CostumeSkinUse(Player.Name, "Turtle_3", false)) return;
                if (__result <= 0) return;

                int Index = GetNeedleIndex(__instance, rootCharm);
                if (Index > 4)
                    __result = 0;
                else if (Index <= 2)
                    __result *= 2;
            }

            private static int GetNeedleIndex(Charm_CompassDamageUp Needle, Charm_Basic Root)
            {
                GridInventory? Inv = Needle.Inventory;
                if (Inv == null) return 1;

                int Index = 1;
                Charm_Basic? Current = Needle;
                for (int Step = 0; Step < 32 && Current != null && Current != Root; Step++)
                {
                    if (Current is not Charm_Compass Compass || Compass.parentOffsets == null) break;

                    Charm_Basic? Parent = null;
                    foreach (Vector2Int Offset in Compass.parentOffsets)
                    {
                        ItemPosition Pos = new(Current.xIdx + Offset.x, Current.yIdx + Offset.y);
                        Charm_Basic? Candidate = Inv.FindItem(Pos)?.Charm;
                        if (Candidate != null && Candidate != Current)
                        {
                            Parent = Candidate;
                            break;
                        }
                    }

                    if (Parent == null) break;
                    if (Parent is Charm_CompassDamageUp) Index++;
                    Current = Parent;
                }
                return Index;
            }
        }

        [HarmonyPatch(typeof(Charm_Active))]
        public static class Charm_Active_Patch
        {
            [HarmonyPatch("FireCasting")]
            [HarmonyPrefix]
            static void FireCasting_Prefix(Charm_Active __instance, Vector3 aimedPosition, UnitAvatar aimedTarget)
            {
                if (__instance.NetworkAvatar && !__instance.NetworkAvatar.IsDead)
                {
                    if (__instance.NetworkAvatar)
                    {
                        UnitAvatar Player = __instance.NetworkAvatar;
                        if (Tools.CostumeSkinUse(Player.Name, "Bird_3"))
                        {
                            if (__instance.GetCooldownRatio() <= 0f)
                                Player.CurrentDashModule.RestoreDashCount(1);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Charm_Magic))]
        public static class Charm_Magic_Patch
        {
            [HarmonyPatch("OnUpdate")]
            [HarmonyPostfix]
            static void OnUpdate_Postfix(Charm_Magic __instance)
            {
                if (!__instance.isServer) return;
                if (__instance.IsEffectEnabled)
                {
                    int Value = __instance.NetworkAvatar.GetCustomStatUnsafe(TextFile.MagicCooldownReduction);
                    if (__instance.CooldownRatio > 1f - Value * 0.01f)
                    {
                        __instance.AddCooldownBonus(Value * 0.01f);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Charm_Reddew))]
        public static class Charm_Reddew_Patch
        {
            private readonly static FieldInfo? chanceField = AccessTools.Field(typeof(Charm_Reddew), "chance");
            private readonly static MethodInfo? RpcCreateFxMethod = AccessTools.Method(typeof(Charm_Reddew), "RpcCreateFx");

            [HarmonyPatch("SummonDueFromVictim")]
            [HarmonyPrefix]
            static bool SummonDueFromVictim_Prefix(Charm_Reddew __instance, UnitAvatar unit, DamageInstance damage)
            {
                if (chanceField == null || RpcCreateFxMethod == null) return true;

                bool chance = (bool)chanceField.GetValue(__instance);
                if (__instance.NetworkAvatar.IsDead || !damage.IsCriticalOrExecutionAttack || !chance || damage.id == __instance.damageId) return false;
                chanceField.SetValue(__instance, false);
                RpcCreateFxMethod.Invoke(__instance, [(Vector2)unit.transform.position]);
                EDamageElementalType ElementalType = Tools.GetHighestDamageElemental(__instance.NetworkAvatar, out _);
                float Damage = Charm_Basic.CalculateDamage(__instance);
                List<UnitAvatar> TargetList = Tools.SearchEnemiesInRange(__instance.NetworkAvatar, unit.transform.position, __instance.attackRadius);
                foreach (UnitAvatar Target in TargetList)
                {
                    var Dam = DamageInstance.GetDamage(__instance.NetworkAvatar, __instance.damageId, Target.transform.position, __instance.NetworkAvatar.GetHostileFactionLayers(EDamageFromType.None), Damage, EDamageType.Projectile, EDamageFromType.None, Vector2.zero, 0, 0f);
                    Dam.criticalChancePercent = -999999f;
                    Dam.elementalType = ElementalType;
                    Target.ApplyDamage(Dam);
                }
                return false;
            }

            [HarmonyPatch("GetDamage")]
            [HarmonyPostfix]
            static void GetDamage_Postfix(Charm_Reddew __instance, UnitAvatar avatar, ref float __result)
            {
                Tools.GetHighestDamageElemental(avatar, out string StatusName);
                float Mult = __instance.damagePercentByLevel.SafeRandomAccess(__instance.CurrentLevelToIdx()) / 100f;
                __result = __instance.NetworkAvatar.GetCustomStatUnsafe(StatusName) * Mult;
            }
        }

        [HarmonyPatch(typeof(Charm_FireChakram))]
        public static class Charm_FireChakram_Patch
        {
            private static float Speed = -1f;

            [HarmonyPatch("OnUpdate")]
            [HarmonyPostfix]
            static void OnUpdate_Postfix(Charm_FireChakram __instance)
            {
                if (!ModConfig.Instance.EnableWeaponModifier || !__instance.isServer) return;

                if (Speed == -1f)
                {
                    Speed = __instance.angleSpeed;
                    return;
                }

                UnitAvatar Player = __instance.NetworkAvatar;
                if (Player.IsWeapon(WeaponIDs.Viduka))
                {
                    __instance.angleSpeed = Speed * 2f;
                }
                else
                {
                    __instance.angleSpeed = Speed;
                }
            }
        }

        [HarmonyPatch(typeof(Charm_FlameGround_Meteor))]
        public static class Charm_FlameGround_Meteor_Patch
        {
            [HarmonyPatch("GetDamage")]
            [HarmonyPostfix]
            static void GetDamage_Prefix(Charm_FlameGround_Meteor __instance, UnitAvatar avatar, ref float __result)
            {
                UnitAvatar Player = __instance.NetworkAvatar;
                if (Player.IsWeapon(WeaponIDs.RedSnakeCrush))
                {
                    if (Player.GetCustomStatUnsafe(TextFile.BurnEvo) > 0)
                    {
                        __result *= 28f / 18f;
                    }
                    __result *= 1f + Player.GetCustomStatUnsafe(TextFile.BurnStack) * 0.2f;
                    __result *= 1f + Player.GetCustomStatUnsafe(TextFile.BurnDamage) * 0.01f;
                    __result *= 1f + Player.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.01f;
                    __result *= 1f + Player.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 0.01f;
                }
            }
        }

        [HarmonyPatch(typeof(Charm_EchoOfTheGlacier))]
        public static class Charm_EchoOfTheGlacier_Patch
        {
            private readonly static MethodInfo? CreateFrostbiteMethod = AccessTools.Method(typeof(Charm_EchoOfTheGlacier), "CreateFrostbite");
            private static readonly int[] MultByLevel = [50, 100, 150, 200, 250, 300, 350, 400];

            [HarmonyPatch("BuildKeywords")]
            [HarmonyPostfix]
            static void BuildKeywords_Postfix(Charm_EchoOfTheGlacier __instance, UnitAvatar avatar, int level, int virtualLevelOffset, ref Loc.KeywordValue[] __result)
            {
                string value = MultByLevel.SafeRandomAccess(__instance.LevelToIdx(level)).ToString() + "%";
                __result = [__result[0], new("Mult", value, Charm_Basic.GetPositiveColor(virtualLevelOffset))];
            }

            [HarmonyPatch("OnUpdate")]
            [HarmonyPostfix]
            static void OnUpdate_Postfix(Charm_EchoOfTheGlacier __instance)
            {
                UnitAvatar Player = __instance.NetworkAvatar;
                if (Player != null)
                {
                    if (CreateFrostbiteMethod != null)
                    {
                        if (SilenceofEverfrostCharm.TryGetValue(Player, out bool Attack) && Attack)
                        {
                            SilenceofEverfrostCharm[Player] = false;
                            CreateFrostbiteMethod.Invoke(__instance, [Player.transform.position, true]);
                        }
                    }
                }
            }

            [HarmonyPatch("GetDamage")]
            [HarmonyPostfix]
            static void GetDamage_Postfix(Charm_EchoOfTheGlacier __instance, UnitAvatar avatar, ref float __result)
            {
                if (ModConfig.Instance.ModifyCharm)
                {
                    float Damage = avatar.GetCustomStatUnsafe(TextFile.IceDamage) * MultByLevel.SafeRandomAccess(__instance.CurrentLevelToIdx()) * 0.01f;
                    SilenceofEverfrostCharmDamage[avatar] = __instance.RequestCharmDamageBonusOnRoot() * 0.01f;
                    float Mult = 1f;
                    if (avatar.IsWeapon(WeaponIDs.SilenceofEverfrost))
                    {
                        Mult += 1f;
                        Mult *= 1f + avatar.GetCustomStatUnsafe(TextFile.FreezeDamage) * 0.01f;
                        Mult *= 1f + avatar.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.01f;
                        Mult *= 1f + avatar.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 0.01f;
                    }
                    __result = Damage * Mult;
                }
            }
        }

        [HarmonyPatch(typeof(Charm_CreateBulletOnSweep))]
        public static class Charm_CreateBulletOnSweep_Patch
        {
            [HarmonyPatch("OnUpdate")]
            [HarmonyPostfix]
            static void OnUpdate_Postfix(Charm_CreateBulletOnSweep __instance)
            {
                if (__instance.NetworkAvatar.IsWeapon(WeaponIDs.Malaspina))
                {
                    float Value = 0.75f / (1f + __instance.NetworkAvatar.GetCustomStatUnsafe(TextFile.AttackSpeed) * 0.01f);
                    if (__instance.cooldownTimer.Ratio < 1f - Value)
                        __instance.cooldownTimer.Ratio = 1f - Value;
                }
            }

            [HarmonyPatch("GetDamage")]
            [HarmonyPostfix]
            static void GetDamage_Prefix(Charm_CreateBulletOnSweep __instance, UnitAvatar avatar, ref float __result)
            {
                if (__instance.NetworkAvatar.IsWeapon(WeaponIDs.Malaspina))
                {
                    __result *= 1f + avatar.GetCustomStatUnsafe(TextFile.FreezeDamage) * 0.01f;
                    __result *= 1f + avatar.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.01f;
                    __result *= 1f + avatar.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 0.01f;
                }
            }
        }

        [HarmonyPatch(typeof(Charm_FlamePlantRoot))]
        public static class Charm_FlamePlantRoot_Patch
        {
            [HarmonyPatch("OnUpdate")]
            [HarmonyPostfix]
            static void OnUpdate_Postfix(Charm_FlamePlantRoot __instance)
            {
                UnitAvatar? Player = __instance.Avatar;
                if (Player == null || !Player.IsWeapon(WeaponIDs.HeatedBlade)) return;

                List<HeatedBladeAttackData> Attacks = StarSephiriaPatches.ModHeatedBladeAttack;
                if (Attacks.Count == 0) return;

                for (int i = Attacks.Count - 1; i >= 0; i--)
                {
                    HeatedBladeAttackData Attack = Attacks[i];
                    if (Attack.Player != Player) continue;
                    Attacks.RemoveAt(i);

                    if (Attack.Target == null || Attack.Target.IsDead) continue;

                    if (!StarSephiriaPatches.HeatedBladeStack.TryGetValue(Player.Name, out int Stack))
                    {
                        StarSephiriaPatches.HeatedBladeStack[Player.Name] = 1;
                        continue;
                    }

                    Stack += 1;
                    StarSephiriaPatches.HeatedBladeStack[Player.Name] = Stack;
                    if (Stack < 5) continue;

                    float Mult = Charm_Basic.CalculateDamage(__instance);
                    Mult *= 1f + Player.GetCustomStatUnsafe(TextFile.BurnDamage) * 0.01f;
                    Mult *= 1f + Player.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.01f;
                    Mult *= 1f + Player.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 0.01f;
                    if (Player.GetCustomStatUnsafe(TextFile.BurnEvo) > 0)
                        Mult *= 28f / 18f;
                    Mult *= 1f + Player.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                    Mult *= 1f + Player.GetCustomStatUnsafe(TextFile.SpecialAttackDamageBonus) * 0.01f;

                    Vector3 Pos = Attack.Target.transform.position;
                    Bullet.Pool.Spawn(__instance.bulletPrefab, Pos, true, EDamageFromType.None, __instance.damageId, Mult * 0.75f, 0, 0f, Player, Player.GetHostileFactionLayers(EDamageFromType.None), 10, Pos, Pos + Vector3.down, null, null, 0f, EDamageElementalType.Fire, true);
                    StarSephiriaPatches.HeatedBladeStack[Player.Name] = Stack - 5;
                }
            }
        }

        [HarmonyPatch(typeof(Charm_DebuffDamage))]
        public static class Charm_DebuffDamage_Patch
        {
            [HarmonyPatch("OnEnabledEffect")]
            [HarmonyPostfix]
            public static void OnEnabledEffect_Postfix(Charm_DebuffDamage __instance)
            {
                if (ModConfig.Instance.ModifyCharm)
                {
                    __instance.NetworkAvatar.OnAttackUnitBeforeOperation_AsLeader += AddDamage_AsLeader;
                }
            }

            [HarmonyPatch("OnDisabledEffect")]
            [HarmonyPostfix]
            public static void OnDisabledEffect_Postfix(Charm_DebuffDamage __instance)
            {
                if (ModConfig.Instance.ModifyCharm)
                {
                    __instance.NetworkAvatar.OnAttackUnitBeforeOperation_AsLeader -= AddDamage_AsLeader;
                }
            }

            private static void AddDamage_AsLeader(UnitAvatar Follower, UnitAvatar Target, DamageInstance instance)
            {
                int Count = 0;
                foreach (CharacterDebuff Debuff in Target.Debuffs)
                {
                    if (Debuff.CurrentStack > 0)
                        Count += 1;
                }
                if (Target.isInStun)
                {
                    Count++;
                }
                if (Count > 0)
                {
                    instance.damage *= 1f + Count * 0.1f;
                }
            }
        }

        [HarmonyPatch(typeof(Charm_MiniBallista))]
        public static class Charm_MiniBallista_Patch
        {
            [HarmonyPatch("GetDamage")]
            [HarmonyPostfix]
            static void GetDamage_Prefix(Charm_MiniBallista __instance, UnitAvatar avatar, ref float __result)
            {
                if (avatar.IsWeapon(WeaponIDs.BallistaSword))
                {
                    float Mult = 50f;
                    Mult += __instance.RequestCharmDamageBonusOnRoot() / 4f;
                    __result += avatar.GetCustomStatUnsafe(TextFile.LightningDamage) * Mult * 0.01f;
                }
            }
        }

        [HarmonyPatch(typeof(UnitAI_MiniBallista))]
        public static class UnitAI_MiniBallista_Patches
        {
            public static FieldInfo? miniBalistaField = AccessTools.Field(typeof(UnitAI_MiniBallista), "miniBalista");
            public static FieldInfo? raycastTimerField = AccessTools.Field(typeof(UnitAI_MiniBallista), "raycastTimer");
            public static FieldInfo? noObstaclesField = AccessTools.Field(typeof(UnitAI_MiniBallista), "noObstacles");
            public static FieldInfo? attackIntervalTimerField = AccessTools.Field(typeof(UnitAI_MiniBallista), "attackIntervalTimer");

            [HarmonyPatch("OnAIUpdate_FoundEnemy")]
            [HarmonyPrefix]
            static bool OnAIUpdate_FoundEnemy_Prefix(UnitAI_MiniBallista __instance)
            {
                UnitAvatar Player = __instance.Avatar.NetworkLeader;
                if (Player == null || !Player.IsWeapon(WeaponIDs.BallistaSword)) return true;

                if (miniBalistaField == null || raycastTimerField == null || noObstaclesField == null || attackIntervalTimerField == null)
                {
                    StarSephiriaMod.LogError($"[UnitAI_MiniBallista][OnAIUpdate_FoundEnemy]私有字段反射获取失败");
                    return true;
                }
                Unit_MiniBallista? miniBalista = miniBalistaField.GetValue(__instance) as Unit_MiniBallista;
                bool noObstacles = (bool)noObstaclesField.GetValue(__instance);
                if (miniBalista == null || attackIntervalTimerField.GetValue(__instance) is not Timer attackIntervalTimer || raycastTimerField.GetValue(__instance) is not Timer raycastTimer)
                {
                    StarSephiriaMod.LogError($"[UnitAI_MiniBallista][OnAIUpdate_FoundEnemy]私有字段类型错误");
                    return true;
                }

                Vector3 PosOffset = -0.85f * (Player.CurrentLookingPosition - (Vector2)Player.transform.position).normalized;
                Vector3 OldPos = __instance.Avatar.transform.position;
                __instance.Avatar.transform.position = Vector3.Lerp((Player.transform.position + PosOffset), OldPos, 0.8f);
                if (miniBalista.IsAttackingServerside)
                {
                    return false;
                }
                Vector2 vector = (__instance.CurrentTarget.transform.position + __instance.transform.position) / 2f;
                if (raycastTimer.Update(Time.deltaTime))
                {
                    if (!Physics2D.CapsuleCast(vector, new Vector2(1f, PosOffset.magnitude), CapsuleDirection2D.Vertical, HorayUtility.GetAngleFromVector(PosOffset), Vector2.zero, 0f, CombatManager.TileLayerMask))
                    {
                        noObstaclesField.SetValue(__instance, true);
                    }
                    else
                    {
                        noObstaclesField.SetValue(__instance, false);
                    }
                }
                float Mult = 1f + __instance.Avatar.GetBonusAttackSpeed() * 0.01f;
                float ShootStack = miniBalista.bulletCount;
                if (ShootStack > 5f) ShootStack = 3f + ShootStack / 2.5f;
                Mult *= 0.4f + 0.6f * ShootStack;
                if (noObstacles && attackIntervalTimer.Update(Time.deltaTime * Mult))
                {
                    miniBalista.Attack();
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(Unit_MiniBallista))]
        public static class Unit_MiniBallista_Patches
        {
            [HarmonyPatch("Attack")]
            [HarmonyPrefix]
            static bool Attack_Prefix(Unit_MiniBallista __instance)
            {
                UnitAvatar Player = __instance.NetworkLeader;
                if (Player == null || !Player.IsWeapon(WeaponIDs.BallistaSword)) return true;

                Vector3 Y = new(0f, __instance.TopdownActor.YPos);
                Vector3 Vec = HorayUtility.GetVector3FromAngle(__instance.bulletFirePoint.eulerAngles.z) * 5f;
                Vector3 Pos = __instance.bulletFirePoint.position - Y;
                Bullet bullet = Bullet.Pool.Spawn(__instance.bulletPrefab, Pos, true, EDamageFromType.DirectAttack, "BasicAttack", __instance.RequestStandardDamage(), 0, 0.5f, __instance, __instance.GetHostileFactionLayers(EDamageFromType.DirectAttack), Y.y, Pos, Pos + Vec, null, null, 0f, EDamageElementalType.Lightning, true);
                bullet.tileCollision = Bullet.ETileCollision.Pierce;
                bullet.TurnOnHoming(true, null, 0.125f);
                return false;
            }
        }

        [HarmonyPatch(typeof(Unit_Grenadier))]
        public static class Unit_Grenadier_Patches
        {
            [HarmonyPatch("Attack")]
            [HarmonyPostfix]
            static void Attack_Postfix(Unit_Grenadier __instance)
            {
                if (__instance.Leader is PlayerAvatar Player)
                {
                    if (Player.IsWeapon(WeaponIDs.KalimsForm))
                    {
                        if (__instance.GetCustomStatUnsafe("GRENADIERBUFF") <= 0)
                        {
                            __instance.StartInvulnerable();
                            __instance.AddCustomStatUnsafe("GRENADIERBUFF", 1);
                        }
                    }
                    else
                    {
                        if (__instance.GetCustomStatUnsafe("GRENADIERBUFF") > 0)
                        {
                            __instance.StopInvulnerable();
                            __instance.AddCustomStatUnsafe("GRENADIERBUFF", -1);
                        }
                    }//卡里姆的形象
                }
            }

            [HarmonyPatch("CreateAttackCoroutineServer")]
            [HarmonyPrefix]
            static void CreateAttackCoroutineServer_Prefix(Unit_Grenadier __instance, ref int count)
            {
                if (ModConfig.Instance.EnableWeaponModifier)
                {
                    while (__instance.GetCustomStatUnsafe(TextFile.JellyfishDoubleAttack) >= count)
                        count += 1;
                }
            }
        }

        [HarmonyPatch(typeof(Charm_SummonGreenBat))]
        public static class Charm_SummonGreenBat_Patch
        {
            [HarmonyPatch("GetDamage")]
            [HarmonyPostfix]
            static void GetDamage_Prefix(Charm_SummonGreenBat __instance, UnitAvatar avatar, ref float __result)
            {
                if (__instance.NetworkAvatar.mp >= 1 && __instance.Item.EntityID == 1015)
                {
                    int Value = __instance.NetworkAvatar.GetCustomStatUnsafe(TextFile.EcologicalSphereEffect);
                    if (Value > 0)
                    {
                        __instance.NetworkAvatar.UseMp(1);
                        __result += Value - Tools.BaseStatToMax(avatar) * 0.5f;
                        __result *= 1f + __instance.NetworkAvatar.GetCustomStatUnsafe(TextFile.MPSkillDamage) * 0.01f;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Charm_EvasionRestore))]
        public static class Charm_EvasionRestore_Patch
        {
            private static Dictionary<UnitAvatar, double> EvasionRestoreCD = [];
            private const float CooldownDuration = 1.2f;

            [HarmonyPatch("OnDisabledEffect")]
            [HarmonyPostfix]
            static void OnDisabledEffect_Postfix(Charm_EvasionRestore __instance)
            {
                EvasionRestoreCD.SafeRemove(__instance.NetworkAvatar);
            }

            [HarmonyPatch("HandleEvade")]
            [HarmonyPrefix]
            static bool HandleEvade_Prefix(Charm_EvasionRestore __instance, DamageInstance instance)
            {
                    if (!NetworkServer.active)
                    return true;

                UnitAvatar Player = __instance.NetworkAvatar;
                if (Player == null || !ModConfig.Instance.ModifyCharm)
                    return true;

                double Now = NetworkTime.time;
                if (EvasionRestoreCD.TryGetValue(Player, out double EndTime))
                {
                    if (Now < EndTime)
                    {
                        return false;
                    }
                }
                EvasionRestoreCD[Player] = Now + CooldownDuration;
                return true;
            }
        }

        [HarmonyPatch(typeof(Charm_AddStatByDefense))]
        public static class Charm_AddStatByDefense_Patch
        {
            private readonly static FieldInfo? addedField = AccessTools.Field(typeof(Charm_AddStatByDefense), "added");
            private readonly static FieldInfo? addedValueField = AccessTools.Field(typeof(Charm_AddStatByDefense), "addedValue");

            [HarmonyPatch("AddStat")]
            [HarmonyPrefix]
            static void AddStat_Prefix(Charm_AddStatByDefense __instance, int defense)
            {
                if (addedField == null) return;
                if (!(bool)addedField.GetValue(__instance))
                {
                    int Value = __instance.addByLevel.SafeRandomAccess(__instance.CurrentLevelToIdx()) * Mathf.FloorToInt(defense / 10f);
                    __instance.NetworkAvatar.AddCustomStatUnsafe(TextFile.WindDamage, Value);
                }
            }

            [HarmonyPatch("RemoveStat")]
            [HarmonyPrefix]
            static void RemoveStat_Prefix(Charm_AddStatByDefense __instance)
            {
                if (addedField == null || addedValueField == null) return;
                if ((bool)addedField.GetValue(__instance))
                {
                    int Value = (int)addedValueField.GetValue(__instance);
                    __instance.NetworkAvatar.AddCustomStatUnsafe(TextFile.WindDamage, -Value);
                }
            }
        }

        [HarmonyPatch(typeof(Charm_WoodenBox))]
        public static class Charm_WoodenBox_Patch
        {
            private readonly static FieldInfo? appliedField = AccessTools.Field(typeof(Charm_WoodenBox), "applied");
            private readonly static FieldInfo? appliedLevelField = AccessTools.Field(typeof(Charm_WoodenBox), "appliedLevel");

            [HarmonyPatch("ApplyEffect")]
            [HarmonyPrefix]
            static void ApplyEffect_Prefix(Charm_WoodenBox __instance, int level, int applyCount)
            {
                if (appliedField == null) return;
                if ((int)appliedField.GetValue(__instance) == 0 && applyCount > 0)
                {
                    int Value = __instance.apPerQuickSlotCharmByLevel.SafeRandomAccess(__instance.LevelToIdx(level)) * applyCount;
                    __instance.NetworkAvatar.AddCustomStatUnsafe(TextFile.WindDamage, Value);
                }
            }

            [HarmonyPatch("RemoveEffect")]
            [HarmonyPrefix]
            static void RemoveEffect_Prefix(Charm_WoodenBox __instance)
            {
                if (appliedField == null || appliedLevelField == null) return;
                int Applied = (int)appliedField.GetValue(__instance);
                if (Applied != 0)
                {
                    int Level = (int)appliedLevelField.GetValue(__instance);
                    int Value = __instance.apPerQuickSlotCharmByLevel.SafeRandomAccess(Level) * Applied;
                    __instance.NetworkAvatar.AddCustomStatUnsafe(TextFile.WindDamage, -Value);
                }
            }
        }

        [HarmonyPatch(typeof(Charm_FireBulletInRange))]
        public static class Charm_FireBulletInRange_Patch
        {
            public static GameObject? WindBullet = null;
            private static readonly FieldInfo? chanceField = AccessTools.Field(typeof(Charm_FireBulletInRange), "chance");
            private static readonly FieldInfo? coroutineField = AccessTools.Field(typeof(Charm_FireBulletInRange), "currentCoroutine");
            private static readonly MethodInfo? rpcFireFxMethod = AccessTools.Method(typeof(Charm_FireBulletInRange), "RpcFireFx");

            private static readonly string[] WindBodySprites =
            [
                "ElementalBomb_Fire_00", "ElementalBomb_Fire_01", "ElementalBomb_Fire_02", "ElementalBomb_Fire_03",
                "ElementalBomb_Fire_04", "ElementalBomb_Fire_05", "ElementalBomb_Fire_06", "ElementalBomb_Fire_07",
            ];

            private static readonly Dictionary<string, string> WindExplosionSprites = new()
            {
                { "DynamiteFX00", "DynamiteFX00" },
                { "DynamiteFX01", "DynamiteFX01_0" },
                { "DynamiteFX02", "DynamiteFX02_0" },
                { "DynamiteFX03", "DynamiteFX03_0" },
                { "DynamiteFX04", "DynamiteFX04_0" },
                { "DynamiteFX05", "DynamiteFX05" },
                { "DynamiteFX06", "DynamiteFX06" },
                { "DynamiteFX07", "DynamiteFX07" },
                { "DynamiteFX08", "DynamiteFX08" },
                { "DynamiteFX09", "DynamiteFX09" },
                { "DynamiteFX10", "DynamiteFX10" },
                { "DynamiteFX11", "DynamiteFX11" },
                { "DynamiteFX12", "DynamiteFX12" },
                { "DynamiteFX13", "DynamiteFX13" },
            };

            private class BulletData
            {
                public GameObject Prefab;
                public EDamageElementalType Type;
                public BulletData(GameObject prefab, EDamageElementalType type)
                {
                    Prefab = prefab;
                    Type = type;
                }
            }

            public static void EnsureWindBullet(Charm_FireBulletInRange Charm)
            {
                if (WindBullet != null) return;
                if (Charm == null || Charm.bulletPrefabs == null || Charm.bulletPrefabs.Count <= (int)EDamageElementalType.Fire) return;
                GameObject BaseBullet = Charm.bulletPrefabs[(int)EDamageElementalType.Fire];
                if (BaseBullet == null) return;

                WindBullet = new ModuleComponents.ModBullet("ElementalBomb_Wind", BaseBullet)
                    .SetSpriteSet("Bullet", WindBodySprites)
                    .SetDestroyFxSprites("EffectFx", WindExplosionSprites)
                    .SetBullet(B =>
                    {
                        if (B.DestroyModule is BulletDestroyModule_Explode Explode)
                        {
                            Explode.inheritElementalType = true;
                            Explode.elementalType = EffectConstant.Wind;
                        }
                    })
                    .EndCreate();
            }

            [HarmonyPatch("GetDamage")]
            [HarmonyPrefix]
            static bool GetDamage_Prefix(Charm_FireBulletInRange __instance, UnitAvatar avatar, ref float __result)
            {
                if (avatar == null) return true;
                Tools.GetHighestDamageElemental(avatar, out _, out int MaxValue);
                __result = MaxValue * (__instance.damageRatioByLevel.SafeRandomAccess(__instance.CurrentLevelToIdx()) / 100f);
                return false;
            }

            [HarmonyPatch("FireCastingServer")]
            [HarmonyPrefix]
            static bool FireCastingServer_Prefix(Charm_FireBulletInRange __instance, Vector3 aimedPosition, UnitAvatar aimedTarget)
            {
                if (chanceField == null || coroutineField == null) return true;
                if (WindBullet == null || __instance.bulletPrefabs == null || __instance.bulletPrefabs.Count < 4) return true;

                UnitAvatar? Avatar = __instance.NetworkAvatar;
                if (Avatar == null) return true;

                int Physical = Avatar.GetCustomStat(ECustomStat.PhysicalDamage);
                int Fire = Avatar.GetCustomStat(ECustomStat.FireDamage);
                int Ice = Avatar.GetCustomStat(ECustomStat.IceDamage);
                int Lightning = Avatar.GetCustomStat(ECustomStat.LightningDamage);
                int Wind = Avatar.GetCustomStatUnsafe(TextFile.WindDamage);
                Tools.GetHighestDamageElemental(Avatar, out _, out int Max);
                if (Wind <= 0 || Wind < Max) return true;
                if (!(bool)chanceField.GetValue(__instance)) return false;

                chanceField.SetValue(__instance, false);
                __instance.NetworkestimatedFireChanceTime = (float)(NetworkTime.time + __instance.coolDownTimer.time);

                List<BulletData> Pool = [];
                if (Physical == Max) Pool.Add(new BulletData(__instance.bulletPrefabs[0], EDamageElementalType.Physical));
                if (Fire == Max) Pool.Add(new BulletData(__instance.bulletPrefabs[1], EDamageElementalType.Fire));
                if (Ice == Max) Pool.Add(new BulletData(__instance.bulletPrefabs[2], EDamageElementalType.Ice));
                if (Lightning == Max) Pool.Add(new BulletData(__instance.bulletPrefabs[3], EDamageElementalType.Lightning));
                if (Wind >= Max) Pool.Add(new BulletData(WindBullet, EffectConstant.Wind));
                if (Pool.Count == 0) return true;

                int Count = __instance.countByLevel.SafeRandomAccess(__instance.CurrentLevelToIdx());
                float Damage = Charm_Basic.CalculateDamage(__instance);
                coroutineField.SetValue(__instance, __instance.StartCoroutine(ThrowBulletCoroutine(__instance, aimedPosition, Count, Pool, Damage)));
                return false;
            }

            private static IEnumerator ThrowBulletCoroutine(Charm_FireBulletInRange Instance, Vector3 AimedPosition, int Count, List<BulletData> Pool, float Damage)
            {
                int Thrown = 0;
                while (Thrown < Count)
                {
                    BulletData Data = Pool[Random.Range(0, Pool.Count)];
                    Vector3 Target = AimedPosition + (Vector3)Random.insideUnitCircle * Instance.fireRadiusError;
                    Bullet.Pool.Spawn(Data.Prefab, Instance.NetworkAvatar.transform.position, true, EDamageFromType.None, Instance.damageId, Damage, 25, 5f, Instance.NetworkAvatar, Instance.NetworkAvatar.GetHostileFactionLayers(EDamageFromType.None), 0f, Instance.NetworkAvatar.transform.position, Target, null, null, 0f, Data.Type);
                    rpcFireFxMethod?.Invoke(Instance, null);
                    Thrown++;
                    if (Thrown < Count)
                    {
                        yield return new WaitForSeconds(Instance.fireInterval + Random.Range(0f - Instance.fireIntervalError, Instance.fireIntervalError));
                        continue;
                    }
                    break;
                }
            }
        }
    }
}