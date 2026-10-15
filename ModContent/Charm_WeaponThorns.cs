using SPMod.Tool;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace SPMod.ModContent
{
    public class Charm_WeaponThorns : Charm_StatusInstance, IAttackableCharm
    {
        public float[] TimerByLevel = new float[] { 0.4f, 0.3f, 0.2f, 0.1f };
        public Timer CooldownTimer = new Timer(0.1f);
        public bool UseEffect = false;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            string text = TimerByLevel.SafeRandomAccess(LevelToIdx(level)).ToString("#0.0");
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Time", text + "s", GetPositiveColor(virtualLevelOffset)),
                new Loc.KeywordValue("Damage", CalculateDamage(this).ToString("#0"),  EffectConstant.WeaponColor),
            };
        }
        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            NetworkAvatar.OnAttackUnit += AddDamage;
            UseEffect = true;
            CooldownTimer.Ratio = 1f;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.OnAttackUnit -= AddDamage;
        }

        private void AddDamage(UnitAvatar avatar, DamageInstance instance)
        {
            if (!UseEffect) return;

            if (instance.fromType == EDamageFromType.DirectAttack)
            {
                string Id = "[Star's Sephiria Mod]铁蒺藜";
                var Dam = DamageInstance.GetDamage(NetworkAvatar, Id, avatar.transform.position, 4294967295L, CalculateDamage(this), EDamageType.Slice, EDamageFromType.None, Vector2.zero, 0, 0f);
                avatar.ApplyDamage(Dam);
                UseEffect = false;
                CooldownTimer.time = TimerByLevel.SafeRandomAccess(CurrentLevelToIdx());
                CooldownTimer.Ratio = 0f;
            }
        }

        public float GetDamage(UnitAvatar avatar)
        {
            float Damage = avatar.GetCustomStatUnsafe(TextFile.DamageReduction);
            float Mult = avatar.GetCustomStatUnsafe(TextFile.Thorns) * 0.01f;
            return Damage * Mult * 0.2f;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (!UseEffect)
            {
                if (CooldownTimer.Update(Time.deltaTime))
                    UseEffect = true;
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}