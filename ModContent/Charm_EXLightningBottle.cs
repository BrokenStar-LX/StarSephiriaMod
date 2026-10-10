
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_EXLightningBottle : Charm_LightningPouch
    {
        public bool Falg = false;
        public float[] SpeedByLevel = new float[] { 1.2f, 0.9f, 0.6f, 0.3f };
        public Timer CooldownTimer = new Timer(1f);
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            string text = SpeedByLevel.SafeRandomAccess(LevelToIdx(level)) + "s";
            List<Loc.KeywordValue> LocKey = base.BuildKeywords(avatar, level, virtualLevelOffset, showAllLevel, ignoreAvatarStatus).ToList();
            LocKey.Add(new Loc.KeywordValue("CD", text.ToString(), GetPositiveColor(virtualLevelOffset)));
            return LocKey.ToArray();
        }
        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            WeaponController.OnDashAttack += OnDashAttack;
            CooldownTimer.Ratio = 1f;
            Falg = true;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            WeaponController.OnDashAttack -= OnDashAttack;
            CooldownTimer.Ratio = 1f;
            Falg = true;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (!Falg)
            {
                if (CooldownTimer.Update(Time.deltaTime)) Falg = true;
            }
        }

        private void OnDashAttack(CombatBehaviour Combat, DamageInstance Damage, ProjectileBase Projectile)
        {
            if (Combat == null || Damage == null || Projectile == null) return;

            if (NetworkAvatar && Falg)
            {
                Falg = false;
                CooldownTimer.time = SpeedByLevel.SafeRandomAccess(CurrentLevelToIdx());
                ComboEffectBase comboEffectBase = NetworkAvatar.Inventory.FindComboEffect("DARKCLOUD");
                if (comboEffectBase != null && comboEffectBase.isEnabled && comboEffectBase is ComboEffect_DarkCloud comboEffect_DarkCloud)
                    comboEffect_DarkCloud.UseCloud(true);
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}