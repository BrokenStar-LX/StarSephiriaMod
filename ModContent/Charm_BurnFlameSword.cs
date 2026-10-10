using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_BurnFlameSword : Charm_StatusInstance
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

        private void AddDamage(UnitAvatar avatar, DamageInstance instance)
        {
            if (!NetworkAvatar.IsInBattle) return;
            if (instance.IsSameElementalType(EDamageElementalType.Fire) && instance.damageType == EDamageType.ElementalEffectDamage)
            {
                ComboEffectBase Combo = NetworkAvatar.Inventory.FindComboEffect("FLAMESWORD");
                if (Combo != null && Combo is ComboEffect_FlameSword FlameSword)
                {
                    if (avatar != null)
                    {
                        Vector3 TargetPos = avatar.transform.position;
                        int Count = 1 + NetworkAvatar.GetCustomStatUnsafe(TextFile.FlameSwordAdditionalAttack);
                        for (int i = 0; i < Count; i++)
                        {
                            Vector3 ShootPos = TargetPos + (Vector3)Random.insideUnitCircle.normalized * 2.5f;
                            FlameSword.FireSword(ShootPos, TargetPos);
                        }
                    }
                }
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}