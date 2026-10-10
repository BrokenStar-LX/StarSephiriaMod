using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_InvincibleDefense : Charm_StatusInstance
    {
        private Timer Cd = new Timer(1.2f, true, true);
        private bool UseEffect = true;
        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            NetworkAvatar.OnGuardSucceeded += OnGuardSucceeded;
            NetworkAvatar.OnAddSubShield += OnAddSubShield;
            NetworkAvatar.OnRemoveSubShield += OnRemoveSubShield;
            foreach (UnitSubShield unitSubShield in NetworkAvatar.subShieldList)
            {
                unitSubShield.OnGuardSucceeded += OnGuardSucceeded_SubShield;
            }
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.OnGuardSucceeded -= OnGuardSucceeded;
            NetworkAvatar.OnAddSubShield -= OnAddSubShield;
            NetworkAvatar.OnRemoveSubShield -= OnRemoveSubShield;
            foreach (UnitSubShield unitSubShield in NetworkAvatar.subShieldList)
            {
                unitSubShield.OnGuardSucceeded -= OnGuardSucceeded_SubShield;
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if(Cd.Update(Time.deltaTime))
            {
                UseEffect = true;
            }
        }

        private void OnAddSubShield(UnitSubShield shield)
        {
            shield.OnGuardSucceeded += OnGuardSucceeded_SubShield;
        }

        private void OnRemoveSubShield(UnitSubShield shield)
        {
            shield.OnGuardSucceeded -= OnGuardSucceeded_SubShield;
        }

        private void OnGuardSucceeded_SubShield(DamageInstance damage, UnitSubShield shield)
        {
            OnGuardSucceeded(damage, false);
        }

        private void OnGuardSucceeded(DamageInstance damage, bool perfectGuard)
        {
            if (!UseEffect) return;
            if (WeaponController)
            {
                WeaponController.StartParryInvincible(0.4f);
                UseEffect = false;
                Cd = new Timer(1.2f, true, true);
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}