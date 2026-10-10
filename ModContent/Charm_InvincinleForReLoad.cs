using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_InvincinleForReLoad : Charm_GrowthCrossbow
    {
        private Timer EffectTime = new Timer(0.8f, true, true);
        private bool UseEffect = true;
        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            WeaponPatch.OnCrossbowUseReLoad += OnCrossbowUseReLoad;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            WeaponPatch.OnCrossbowUseReLoad -= OnCrossbowUseReLoad;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (EffectTime.Update(Time.deltaTime))
            {
                UseEffect = true;
            }
        }

        private void OnCrossbowUseReLoad()
        {
            if (!UseEffect) return;
            if (WeaponController)
            {
                WeaponController?.StartParryInvincible(0.3f);
                UseEffect = false;
                EffectTime = new Timer(0.8f, true, true);
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}