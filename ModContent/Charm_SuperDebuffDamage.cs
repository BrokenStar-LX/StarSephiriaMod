using System;

namespace SPMod.ModContent
{
    public class Charm_SuperDebuffDamage : Charm_Basic
    {
        public int[] AddDamageByLevel = new int[] { 1, 2, 3, 4, 5 };
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Add", AddDamageByLevel.SafeRandomAccess(LevelToIdx(level)).ToString() + "%", GetPositiveColor(virtualLevelOffset)),
            };
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            NetworkAvatar.OnAttackUnitBeforeOperation += AddDamage;
            NetworkAvatar.OnAttackUnitBeforeOperation_AsLeader += AddDamage_AsLeader;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.OnAttackUnitBeforeOperation -= AddDamage;
            NetworkAvatar.OnAttackUnitBeforeOperation_AsLeader -= AddDamage_AsLeader;
        }

        private void AddDamage_AsLeader(UnitAvatar Follower, UnitAvatar Target, DamageInstance instance)
        {
            AddDamage(Target, instance);
        }

        private void AddDamage(UnitAvatar Target, DamageInstance instance)
        {
            int Count = 0;
            foreach(CharacterDebuff Debuff in Target.Debuffs)
            {
                if (Debuff.CurrentStack > 0)
                    Count += Math.Min(10, (int)Debuff.CurrentStack);
            }
            if (Target.isInStun)
            {
                Count++;
            }
            if (Count > 0)
            {
                instance.damage *= 1f + Count * AddDamageByLevel.SafeRandomAccess(CurrentLevelToIdx()) * 0.01f;
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}