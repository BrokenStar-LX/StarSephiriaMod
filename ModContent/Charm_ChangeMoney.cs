using Mirror;
using SPMod.Tool;
using System.Collections.Generic;
using System.Linq;

namespace SPMod.ModContent
{
    public class Charm_ChangeMoney : Charm_Basic
    {
        public float MoneyMult = 3f;
        protected override void OnEnabledEffect()
        {
            Item.Entity.cost = EffectConstant.UncommonCost.IntMult(MoneyMult);
            base.OnEnabledEffect();
        }

        protected override void OnDisabledEffect()
        {
            Item.Entity.cost = EffectConstant.UncommonCost;
            base.OnDisabledEffect();
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}