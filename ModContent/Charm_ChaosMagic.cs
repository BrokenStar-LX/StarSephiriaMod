using SPMod.Tool;
using System;

namespace SPMod.ModContent
{
    public class Charm_CharosMagic : Charm_StatusInstance
    {
        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            NetworkAvatar.OnAttackUnitBeforeOperation += AddDamage;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.OnAttackUnitBeforeOperation -= AddDamage;
        }

        private void AddDamage(UnitAvatar Target, DamageInstance instance)
        {
            if (instance.fromType == EDamageFromType.Magic)
            {
                instance.elementalType = EDamageElementalType.Chaos;
                instance.SetCustomColor(true, EffectConstant.ChaosColor);
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}