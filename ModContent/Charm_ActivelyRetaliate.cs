using Mirror;
using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_ActivelyRetaliate : Charm_StatusInstance, IAttackableCharm
    {
        private readonly string DamageId = "[Star's Sephiria Mod]影武者";
        public float[] MultByLevel = new float[] { 45, 90, 135, 180 };
        public Timer CooldownTimer = new Timer(0.2f);
        public float DamageMult = 1f;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            string text = MultByLevel.SafeRandomAccess(LevelToIdx(level)).ToString();
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Mult", text + "%", GetPositiveColor(virtualLevelOffset)),
                new Loc.KeywordValue("Damage", CalculateDamage(this).ToString("#0"),  EffectConstant.WeaponColor),
            };
        }
        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            NetworkAvatar.OnAttackUnit += AddDamage;
            DamageMult = 1f;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.OnAttackUnit -= AddDamage;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (!isServer)
            {
                return;
            }
            if (DamageMult < 1f && CooldownTimer.Update(Time.deltaTime))
            {
                DamageMult += 0.1f;
            }
        }

        private void AddDamage(UnitAvatar avatar, DamageInstance instance)
        {
            if (DamageMult > 0f && instance.fromType == EDamageFromType.DirectAttack && instance.flag != DamageId)
            {
                float Damage = CalculateDamage(this) * DamageMult;
                var Dam = DamageInstance.GetDamage(NetworkAvatar, DamageId, avatar.transform.position, 4294967295L, Damage, EDamageType.Slice, EDamageFromType.None, Vector2.zero, 0, 0f);
                Dam.elementalType = EDamageElementalType.Physical;
                Dam.flag = DamageId;
                avatar.ApplyDamage(Dam);
                DamageMult = 0f;
            }
        }

        public float GetDamage(UnitAvatar avatar)
        {
            float Value = avatar.GetCustomStatUnsafe(TextFile.StrikeBack) * 0.01f;
            float Damage = Value * avatar.GetCustomStatUnsafe(TextFile.Evasion) * 0.01f;
            float Mult = avatar.GetCustomStatUnsafe(TextFile.PhysicalDamage) * MultByLevel.SafeRandomAccess(CurrentLevelToIdx()) * 0.01f;
            return Damage * Mult * 0.01f;
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}