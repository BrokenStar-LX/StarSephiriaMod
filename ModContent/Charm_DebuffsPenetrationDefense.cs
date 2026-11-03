using System;
using System.Linq;

namespace SPMod.ModContent
{
    public class Charm_DebuffsPenetrationDefense : Charm_StatusInstance
    {
        public int[] ValueByLevel = [3, 6, 9, 12];
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            string Text = ValueByLevel.SafeRandomAccess(LevelToIdx(level)).ToString() + "%";
            return
            [
                new("Value", Text, GetPositiveColor(virtualLevelOffset)),
            ];
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
            bool falg = false;
            foreach(CharacterDebuff Debuff in Target.Debuffs)
            {
                if (Debuff is CharacterDebuff_WindErosion)
                {
                    falg = true;
                    break;
                }
            }
            if (falg)
            {
                instance.ignoreDefense += ValueByLevel.SafeRandomAccess(CurrentLevelToIdx());
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}