using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_InvincinleSpiceialAttack : Charm_SweepRange
    {
        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            NetworkAvatar.AddCustomStatUnsafe(TextFile.InvincinleSpiceialAttack, 1);
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.AddCustomStatUnsafe(TextFile.InvincinleSpiceialAttack, -1);
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}