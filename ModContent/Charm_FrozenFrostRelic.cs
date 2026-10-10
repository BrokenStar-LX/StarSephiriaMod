using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SPMod.ModContent
{
    public class Charm_FrozenFrostRelic : Charm_CustomStatus
    {
        public float Chance = 20;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            string text = Chance + "%";
            List<Loc.KeywordValue> LocKey = base.BuildKeywords(avatar, level, virtualLevelOffset, showAllLevel, ignoreAvatarStatus).ToList();
            LocKey.Add(new Loc.KeywordValue("Chance", text.ToString(), Color.yellow));
            return LocKey.ToArray();
        }
        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            WeaponController.OnSwingCreated += OnBaisCreated;
            NetworkAvatar.OnAttackUnit += AddDamage;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            WeaponController.OnSwingCreated -= OnBaisCreated;
            NetworkAvatar.OnAttackUnit -= AddDamage;
        }

        private void OnBaisCreated(bool isFinalCombo, ProjectileBase Projectile)
        {
            if (Projectile == null) return;
            if (NetworkAvatar)
            {
                Projectile.additionalDamagePercent -= 30f;
            }
        }

        private void AddDamage(UnitAvatar avatar, DamageInstance instance)
        {
            if (instance.flag.Contains("FROSTRELIC"))
            {
                if(Random.Range(0f, 100f) < Chance)
                {
                    avatar.ApplyDebuff(CombatManager.Instance.frostbiteDebuffPrefab, NetworkAvatar);
                }
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}