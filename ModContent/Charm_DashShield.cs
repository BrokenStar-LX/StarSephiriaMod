using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_DashShield : Charm_Basic
    {
        public int[] BaseShieldByLevel = new int[] { 2, 4, 6, 8, 10 };
        public int[] ShieldDamageByLevel = new int[] { 1, 1, 2, 2, 3 };
        public Timer CooldownTimer = new Timer(8f);
        public bool UseEffect = false;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            string text = BaseShieldByLevel.SafeRandomAccess(LevelToIdx(level)).ToString();
            string text2 = ShieldDamageByLevel.SafeRandomAccess(LevelToIdx(level)).ToString() + "%";
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Value", text.ToString(), GetPositiveColor(virtualLevelOffset)),
                new Loc.KeywordValue("Mult", text2.ToString(), GetPositiveColor(virtualLevelOffset)),
            };
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            WeaponController.OnSwingCreated_Dash += OnCreatedDash;
            WeaponController.OnDashAttack += OnDashAttack;
            CooldownTimer.Ratio = 1f;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            WeaponController.OnSwingCreated_Dash -= OnCreatedDash;
            WeaponController.OnDashAttack -= OnDashAttack;
            NetworkAvatar.RemoveShield("Charm_DodgeShield");
            CooldownTimer.Ratio = 1f;
        }

        private void OnCreatedDash(ProjectileBase Projectile)
        {
            if (Projectile == null) return;

            if (NetworkAvatar)
            {
                Projectile.additionalDamagePercent += NetworkAvatar.Shield * ShieldDamageByLevel.SafeRandomAccess(CurrentLevelToIdx());
            }
        }

        private void OnDashAttack(CombatBehaviour Combat, DamageInstance Damage, ProjectileBase Projectile)
        {
            if (Combat == null || Damage == null || Projectile == null) return;

            if (NetworkAvatar)
            {
                if (UseEffect)
                {
                    float Value = BaseShieldByLevel.SafeRandomAccess(CurrentLevelToIdx());
                    if ((int)Value > 0)
                    {
                        NetworkAvatar.AddShield(Value, "Charm_DodgeShield");
                    }
                    CooldownTimer.Ratio = 0f;
                    UseEffect = false;
                }
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (CooldownTimer.Update(Time.deltaTime))
            {
                UseEffect = true;
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}