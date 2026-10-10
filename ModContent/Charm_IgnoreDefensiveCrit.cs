using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_IgnoreDefensiveCrit : Charm_Basic, IAttackableCharm
    {
        private readonly string DamageId = "[Star's Sephiria Mod]增幅器组件";
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            float Damage = Mathf.Max(0, CalculateDamage(this));
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Damage", Damage.ToString("#0"),  EffectConstant.WeaponColor),
            };
        }
        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            NetworkAvatar.OnAttackUnit += AddDamage;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.OnAttackUnit -= AddDamage;
        }

        private void AddDamage(UnitAvatar avatar, DamageInstance instance)
        {
            float Damage = CalculateDamage(this);
            if (instance.IsCriticalOrExecutionAttack && instance.flag != DamageId && Damage > 0)
            {
                var Dam = DamageInstance.GetDamage(NetworkAvatar, DamageId, avatar.transform.position, 4294967295L, Damage, EDamageType.Slice, EDamageFromType.None, Vector2.zero, 0, 0f);
                Dam.ignoreDefense = 30;
                Dam.flag = DamageId;
                avatar.ApplyDamage(Dam);
            }
        }

        public float GetDamage(UnitAvatar avatar)
        {
            int Add = avatar.GetCustomStatUnsafe(TextFile.TrueDamage);
            int Down = Tools.BaseStatToAverage(avatar);
            float Damage = 20 + Add - Down;
            return Damage;
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}