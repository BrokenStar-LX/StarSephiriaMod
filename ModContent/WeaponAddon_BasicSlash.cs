using Mirror;
using SPMod.Tool;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static Unit_WeaponEquipped;

namespace SPMod.ModContent
{
    public class WeaponAddon_BasicSlash : WeaponAddonCommon_StatusUnsafe
    {
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
            UnitAvatar Player = parent.Networkowner.unitAvatar;
            Vector3 EulerAngles = parent.Networkowner.shoulder.transform.eulerAngles;
            EulerAngles.z += Random.Range(0f, 360f);
            if (NetworkClient.active)
                parent.CreateBasicAttackSwingFxFullyManual(LoadModContent.WindKatana_Moment.Data, instance.damagePoint, EulerAngles);
            ModNetwork.SendWeaponSwingFx(parent.netId, ModNetwork.SwingFxMoment, instance.damagePoint, EulerAngles);
            for (int i = 0; i < 2; i++)
            {
                UnitAvatar unitAvatar = parent.Networkowner.unitAvatar;
                float Damage = unitAvatar.GetCustomStatUnsafe(TextFile.PhysicalDamage);
                if (i == 0) Damage = unitAvatar.GetCustomStatUnsafe(TextFile.WindDamage);
                if (projectile) Damage *= projectile.swingMultiplier * 0.5f;
                Damage *= 1f + unitAvatar.GetCustomStatUnsafe(TextFile.WeaponDamageBonus) * 0.01f;
                Damage *= 1f + unitAvatar.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                switch (UseType)
                {
                    case AttackType.BasicAttack:
                        Damage *= 1f + unitAvatar.GetCustomStatUnsafe(TextFile.BasicAttackDamageBonus) * 0.01f;
                        break;
                    case AttackType.SpecialAttack:
                        Damage *= 1f + unitAvatar.GetCustomStatUnsafe(TextFile.SpecialAttackDamageBonus) * 0.01f;
                        break;
                    case AttackType.DashAttack:
                        Damage *= 1f + unitAvatar.GetCustomStatUnsafe(TextFile.DashAttackDamageBonus) * 0.01f;
                        break;
                }
                DamageInstance damage = DamageInstance.GetDamage(Player, instance.id, instance.damagePoint, instance.targetFactionLayers, Damage, instance.damageType, EDamageFromType.DirectAttack, default, 0, 0f);
                if (i == 0)
                {
                    damage.fromType = EDamageFromType.None;
                    damage.elementalType = EffectConstant.Wind;
                    damage.id = "[Star's Sephiria Mod]武器附加伤害(风)";
                }
                else damage.elementalType = instance.elementalType;
                avatar.ApplyDamage(damage);
            }
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