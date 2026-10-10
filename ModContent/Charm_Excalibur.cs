using Mirror;
using SPMod.Tool;
using System;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_Excalibur : Charm_Active, IAttackableCharm
    {
        public float[] MultByLevel = new float[] { 200, 400, 600, 800, 1000 };
        public Timer CooldownTimer = new Timer(4f);
        public Timer CooldownTimer2 = new Timer(0.1f);
        public GameObject? Shoot = null;
        public NewWeaponFireData? EffectData = null;
        public string DamageId = "[Star's Sephiria Mod]湖中剑";
        [SyncVar]
        public bool UseAddTimer = false;
        [SyncVar]
        public bool UseEffect = false;
        [SyncVar]
        private float SyncPhaseEnd;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Damage", CalculateDamage(this).ToString("#0"), EffectConstant.WeaponColor),
                new Loc.KeywordValue("Mult", MultByLevel.SafeRandomAccess(LevelToIdx(level)).ToString() + "%", GetPositiveColor(virtualLevelOffset)),
            };
        }
        public override float GetCooldownTime(out bool changeByLevel, int level, int virtualLevelOffset)
        {
            changeByLevel = false;
            return CooldownTimer.time;
        }

        public override float GetCooldownRatio()
        {
            if (!isServer)
            {
                float ReMain = SyncPhaseEnd - (float)NetworkTime.time;
                if (ReMain > 0)
                {
                    return 1f - Mathf.Clamp01(ReMain / CooldownTimer.time);
                }
                return 0f;
            }
            if (UseEffect)
            {
                return 0f;
            }
            return 1f - CooldownTimer.Ratio;
        }

        public override void CooldownDecrease(float ratio)
        {
            if (!isServer) return;

            float Delta = ratio * CooldownTimer.time;
            CooldownTimer.Ratio += ratio;
            SyncPhaseEnd -= Delta;
        }

        protected override void OnEnabledEffect()
        {
            if (Shoot == null)
            {
                if (Tools.GetPro("KatanaSwing_GreatDraw_Iaido", out var Pro))
                {
                    Shoot = Pro;
                }
            }
            if (EffectData == null)
            {
                WeaponEntity Weapon = WeaponDatabase.FindWeaponById(WeaponIDs.RendbladeDriftingWillow);
                WeaponSimple_Katana Katana = Weapon.mainWeaponPrefab.GetComponent<WeaponSimple_Katana>();
                EffectData = Katana.greatDrawFireData;
            }
            base.OnEnabledEffect();
            NetworkAvatar.OnAttackUnit += AddDamage;
            CooldownTimer.Ratio = 1f;
            SyncPhaseEnd = (float)NetworkTime.time;
        }

        private void AddDamage(UnitAvatar Target, DamageInstance Damage)
        {
            if (!isServer) return;
            if (!UseAddTimer) return;
            if (Damage.fromType == EDamageFromType.DirectAttack)
            {
                CooldownDecrease(1f / GetCooldownTime(out _, CurrentLevelToIdx(), 0) * 0.1f);
                UseAddTimer = false;
            }
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.OnAttackUnit -= AddDamage;
            CooldownTimer.Ratio = 1f;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (!UseAddTimer)
            {
                if (CooldownTimer2.Update(Time.deltaTime))
                    UseAddTimer = true;
            }
            if (!UseEffect && CooldownTimer.Update(Time.deltaTime))
            {
                UseEffect = true;
            }
        }

        protected override void FireCastingServer(Vector3 aimedPosition, UnitAvatar aimedTarget)
        {
            if (!UseEffect) return;

            int UseMP = Math.Max(1, NetworkAvatar.MP.IntMult(0.2f));
            NetworkAvatar.UseMp(UseMP);

            Vector3 position = NetworkAvatar.transform.position;
            float Damage = CalculateDamage(this);
            MeleeCollision.Pool.Spawn(Shoot, position, true, EDamageFromType.None, DamageId, Damage, 25, 2f, NetworkAvatar, NetworkAvatar.GetHostileFactionLayers(EDamageFromType.None), 0.3f, position, position, null, null, 0f, EDamageElementalType.Physical, false);

            RpcPlaySwingFx();

            UseEffect = false;
            SyncPhaseEnd = (float)(NetworkTime.time + CooldownTimer.time);
        }

        [ClientRpc]
        void RpcPlaySwingFx()
        {
            if (EffectData == null)
            {
                WeaponEntity Weapon = WeaponDatabase.FindWeaponById(WeaponIDs.RendbladeDriftingWillow);
                WeaponSimple_Katana Katana = Weapon.mainWeaponPrefab.GetComponent<WeaponSimple_Katana>();
                EffectData = Katana.greatDrawFireData;
            }
            WeaponController?.currentWeapon?.CreateBasicAttackSwingFxFullyManual(EffectData);
        }

        public float GetDamage(UnitAvatar avatar)
        {
            float Damage = 200 + Tools.BaseStatToMax(avatar) * MultByLevel.SafeRandomAccess(CurrentLevelToIdx()) * 0.01f;
            Damage *= 1f + avatar.GetCustomStatUnsafe(TextFile.MPSkillDamage) * 0.01f;
            Damage *= 1f + (avatar.MaxMp - avatar.MP) * 0.005f;
            return Damage;
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}