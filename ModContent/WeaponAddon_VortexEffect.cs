using Mirror;
using SPMod.Tool;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static SPMod.StarSephiriaPatches;
using static Unit_WeaponEquipped;

namespace SPMod.ModContent
{
    public class WeaponAddon_VortexEffect : WeaponAddonCommon_StatusUnsafe
    {
        private const float TriggerInterval = 0.8f;
        private float NextTriggerTime;
        protected override void OnEnableAddon()
        {
            base.OnEnableAddon();
            parent.Networkowner.OnBasicAttack += OnBasicAttackUnit;
            parent.Networkowner.OnSpecialAttack += OnSpecialAttackUnit;
            parent.Networkowner.OnDashAttack += OnDashAttackUnit;
        }

        protected override void OnDisableAddon()
        {
            base.OnDisableAddon();
            parent.Networkowner.OnBasicAttack -= OnBasicAttackUnit;
            parent.Networkowner.OnSpecialAttack -= OnSpecialAttackUnit;
            parent.Networkowner.OnDashAttack -= OnDashAttackUnit;
        }

        private void OnBasicAttackUnit(CombatBehaviour avatar, DamageInstance instance, ProjectileBase projectile)
        {
            if (instance.fromType == EDamageFromType.DirectAttack)
                OnAttack(AttackType.BasicAttack, avatar, instance, projectile);
        }

        private void OnSpecialAttackUnit(CombatBehaviour avatar, DamageInstance instance, ProjectileBase projectile)
        {
            if (instance.fromType == EDamageFromType.DirectAttack)
                OnAttack(AttackType.SpecialAttack, avatar, instance, projectile);
        }

        private void OnDashAttackUnit(CombatBehaviour avatar, DamageInstance instance, ProjectileBase projectile)
        {
            if (instance.fromType == EDamageFromType.DirectAttack)
                OnAttack(AttackType.DashAttack, avatar, instance, projectile);
        }

        private void OnAttack(AttackType UseType, CombatBehaviour avatar, DamageInstance instance, ProjectileBase projectile)
        {
            if (!parent.isServer) return;
            if (Time.time < NextTriggerTime) return;
            NextTriggerTime = Time.time + TriggerInterval;

            UnitAvatar Player = parent.Networkowner.unitAvatar;
            ComboEffect_Debuff_Patch.VortexEffect(Player, instance.damagePoint);
        }

        public override bool Weaved()
        {
            return true;
        }
        enum AttackType
        {
            BasicAttack,
            SpecialAttack,
            DashAttack,
        }
    }
}