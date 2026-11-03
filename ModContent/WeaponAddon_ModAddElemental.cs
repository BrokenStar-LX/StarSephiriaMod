using Mirror;
using SPMod.Tool;
using System.Collections.Generic;
using UnityEngine;

namespace SPMod.ModContent
{
    public class WeaponAddon_ModAddElemental : WeaponAddonCommon_StatusUnsafe
    {
        public string ValueName = "";
        public string DamageId = "";
        public int DamageMult = 40;
        public EDamageElementalType elementalType = EDamageElementalType.Fire;
        public override Loc.KeywordValue[] BuildKeywords()
        {
            return new List<Loc.KeywordValue>(base.BuildKeywords())
            {
                new("Mult", DamageMult.ToString() + "%", Color.white),
            }.ToArray();
        }

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
            UnitAvatar unitAvatar = parent.Networkowner.unitAvatar;
            int BaseValue = unitAvatar.GetCustomStatUnsafe(ValueName);
            int AddMult = unitAvatar.GetCustomStatUnsafe(TextFile.AdditionalElementalDamageBonus);
            int Mult = DamageMult + AddMult;
            float Damage = BaseValue * Mult * 0.01f;
            if (projectile) Damage *= projectile.swingMultiplier;
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
            DamageInstance damage = DamageInstance.GetDamage(unitAvatar, DamageId, instance.damagePoint, instance.targetFactionLayers, Damage, instance.damageType, EDamageFromType.None, default, 0, 0f);
            damage.elementalType = elementalType;
            avatar.ApplyDamage(damage);
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