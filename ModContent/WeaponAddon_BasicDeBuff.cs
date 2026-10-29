using Mirror;
using SPMod.Tool;
using System.Collections.Generic;
using UnityEngine;

namespace SPMod.ModContent
{
    public class WeaponAddon_BasicDeBuff : WeaponAddonCommon_Status
    {
        public float debuffPercent = 80f;
        public CharacterDebuff? debuffPrefab;
        public override Loc.KeywordValue[] BuildKeywords()
        {
            return new List<Loc.KeywordValue>(base.BuildKeywords())
            {
                new Loc.KeywordValue("PERCENT", debuffPercent.ToString() + "%", Color.yellow),
            }.ToArray();
        }

        protected override void OnEnableAddon()
        {
            base.OnEnableAddon();
            parent.Networkowner.OnBasicAttack += OnBasicAttackUnit;
        }

        protected override void OnDisableAddon()
        {
            base.OnDisableAddon();
            parent.Networkowner.OnBasicAttack -= OnBasicAttackUnit;
        }

        private void OnBasicAttackUnit(CombatBehaviour avatar, DamageInstance instance, ProjectileBase projectile)
        {
            if (debuffPrefab == null) return;
            if (instance.fromType != EDamageFromType.DirectAttack) return;
            if (instance.ignoreDebuff) return;
            if (Random.Range(0f, 100f) <= debuffPercent)
            {
                if(avatar is UnitAvatar Target)
                    Target.ApplyDebuff(debuffPrefab, parent.Networkowner.unitAvatar);
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}