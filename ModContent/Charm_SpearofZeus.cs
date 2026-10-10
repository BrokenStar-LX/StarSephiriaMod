using HarmonyLib;
using Mirror;
using SPMod.Tool;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace SPMod.ModContent
{
    public class Charm_SpearofZeus : Charm_Basic, IAttackableCharm
    {
        public float[] BaseByLevel = new float[] { 10, 20, 30, 40 };
        public float[] MultByLevel = new float[] { 80, 160, 240, 320 };
        public const float BaseCooldown = 6f;
        public const int DownValue = 10;
        public int MaxShootCount = 1;
        public GameObject? Thunder = null;
        public string DamageId = "[Star's Sephiria Mod]宙斯之矛";
        public ChargingCharm? chargingCharm = null;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            string Base = BaseByLevel.SafeRandomAccess(LevelToIdx(level)).ToString();
            string Mult = MultByLevel.SafeRandomAccess(LevelToIdx(level)).ToString();
            string Status = avatar.GetCustomStatUnsafe("FROSTRELICFLAME") > 0 ? "<tag=FireDamage>" : "<tag=IceDamage>";
            string Time = BaseCooldown + "s";
            string Down = DownValue + "%";
            float Value = avatar.GetCustomStatUnsafe(TextFile.ChargingCharmBonus);
            if (Value > 0)
            {
                Time = BaseCooldown + "s" + " -> " + (BaseCooldown / (1f + Value * 0.01f)).ToString("#0.0") + "s";
            }
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Time", Time, EffectConstant.CooldownColor),
                new Loc.KeywordValue("Down", Down, EffectConstant.CooldownColor),
                new Loc.KeywordValue("Stat", Status, Color.white),
                new Loc.KeywordValue("Mult", Mult + "%", GetPositiveColor(virtualLevelOffset)),
                new Loc.KeywordValue("Base", Base, GetPositiveColor(virtualLevelOffset)),
                new Loc.KeywordValue("Damage", CalculateDamage(this).ToString("#0"), EffectConstant.WeaponColor),
            };
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            if (Thunder == null)
            {
                if (Tools.GetPro("LightningTouch", out var Pro))
                {
                    Thunder = Pro;
                }
            }
            if (chargingCharm != null)
            {
                chargingCharm.EnableEffect();
                chargingCharm.OnFire += Fire;
            }
            NetworkAvatar.OnAttackUnitBeforeOperation += AddDamage;
        }

        protected override void OnConnected(int instanceID)
        {
            base.OnConnected(instanceID);
            chargingCharm?.SetAvatar(NetworkAvatar);
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.OnAttackUnitBeforeOperation -= AddDamage;
            if (chargingCharm != null)
            {
                chargingCharm.DisableEffect();
                chargingCharm.OnFire -= Fire;
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (NetworkAvatar.IsDead || CombatManager.Instance.PeaceMode || !NetworkAvatar.IsInBattle || chargingCharm == null)
            {
                return;
            }
            if (chargingCharm.isAvailable)
            {
                chargingCharm.ActivateChargingCharm(1f, true, "");
            }
        }
        public override void OnCharmEffectRefreshed()
        {
            base.OnCharmEffectRefreshed();
            if (isServer)
            {
                chargingCharm?.UpdateChargeTime();
            }
        }

        private void AddDamage(UnitAvatar avatar, DamageInstance instance)
        {
            if (!NetworkServer.active) return;
            if (!chargingCharm) return;
            if (instance.id == "Ability_DarkCloud")
            {
                chargingCharm.AddTimer(chargingCharm.GetChargeTimer() * 0.1f);
            }
        }

        private UnitAvatar? SearchTarget()
        {
            float MaxDis = 8f;
            UnitAvatar? UseTarget = null;
            foreach (UnitAvatar Target in CombatManager.Instance.AllCreatures.ToList())
            {
                if (Target == NetworkAvatar) continue;
                if (Target.hp <= 0) continue;
                if (Target.IsDead) continue;
                if (Target.canBeTarget.IsFalse()) continue;
                if (!Target.gameObject.activeSelf) continue;
                float Dis = Vector3.Distance(Target.transform.position, NetworkAvatar.transform.position);
                if (Dis > MaxDis) continue;
                if (!CombatManager.ContainsAttackableFaction(Target.GetHostileFactionLayers(EDamageFromType.None), NetworkAvatar.faction)) continue;
                if (Dis < MaxDis)
                {
                    UseTarget = Target;
                    MaxDis = Dis;
                }
            }
            if (UseTarget != null)
            {
                return UseTarget;
            }
            return null;
        }

        private void Fire(int fireCount, float damageMultiplier, float mpBonus, float mpDamage_MaxMP, bool canRetrigger, string parameter)
        {
            StartCoroutine(FireCoroutine(fireCount, damageMultiplier, canRetrigger));
        }

        private IEnumerator FireCoroutine(int fireCount, float Mult, bool canRetrigger)
        {
            if (Thunder == null) yield break;
            if (chargingCharm == null) yield break;
            UnitAvatar? Target = SearchTarget();
            if (Target == null) yield break;

            for (int i = 0; i < fireCount; i++)
            {
                Vector3 position = Target.transform.position;
                bool flag = NetworkAvatar.GetCustomStatUnsafe(TextFile.FrostRelicFlame) > 0;
                EDamageElementalType Elemental = flag ? EDamageElementalType.FireAndLightning : EDamageElementalType.IceAndLightning;
                var bullet = Bullet.Pool.Spawn(Thunder, position, true, EDamageFromType.None, DamageId, CalculateDamage(this) * Mult, 25, 2f, NetworkAvatar, NetworkAvatar.GetHostileFactionLayers(EDamageFromType.None), 0.3f, position, position, null, null, 0f, Elemental, false);
                bullet.swingId = "FROSTRELIC";
                yield return new WaitForSeconds(0.3f);
            }
            if (canRetrigger)
            {
                yield return chargingCharm.CalculateRetriggerCoroutine(Mult);
            }
        }

        public float GetDamage(UnitAvatar avatar)
        {
            bool flag = NetworkAvatar.GetCustomStatUnsafe(TextFile.FrostRelicFlame) > 0;
            float Base = BaseByLevel.SafeRandomAccess(CurrentLevelToIdx());
            float Damage = (NetworkAvatar.GetCustomStatUnsafe(flag ? TextFile.FireDamage : TextFile.IceDamage) + NetworkAvatar.GetCustomStatUnsafe(TextFile.LightningDamage));
            float Mult = MultByLevel.SafeRandomAccess(CurrentLevelToIdx()) * 0.01f;
            Mult *= 1f + avatar.GetCustomStatUnsafe(TextFile.DarkCloudDamage) * 0.01f;
            Mult *= 1f + avatar.GetCustomStatUnsafe(TextFile.FrostRelicDamage) * 0.01f;
            Mult *= 1f + Math.Min(100, avatar.GetCustomStatUnsafe(TextFile.DarkCloudLuck)) * 0.01f;
            return Base + Damage * Mult + CharmPatch.FrostRelicAddMult(avatar);
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}