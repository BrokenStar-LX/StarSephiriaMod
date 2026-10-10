using SPMod.Tool;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_AddPerfectGuardTime : Charm_GrowthGuard
    {
        public int[] ValueByLevel = new int[] { 10, 20, 30, 40 };
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            List<Loc.KeywordValue> OrigLoc = base.BuildKeywords(avatar, level, virtualLevelOffset, showAllLevel, ignoreAvatarStatus).ToList();
            OrigLoc.Add(new Loc.KeywordValue("Value", "+" + ValueByLevel.SafeRandomAccess(LevelToIdx(level)).ToString() + "%", GetPositiveColor(virtualLevelOffset)));
            return OrigLoc.ToArray();
        }
        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            NetworkAvatar.additionalPerfectGuardTime += NetworkAvatar.perfectGuardTime * ValueByLevel.SafeRandomAccess(CurrentLevelToIdx()) * 0.01f;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.additionalPerfectGuardTime -= NetworkAvatar.perfectGuardTime * ValueByLevel.SafeRandomAccess(CurrentLevelToIdx()) * 0.01f;
        }

        protected override void OnUpdatedLevel(int oldLevel, int newLevel)
        {
            base.OnUpdatedLevel(oldLevel, newLevel);
            NetworkAvatar.additionalPerfectGuardTime -= NetworkAvatar.perfectGuardTime * ValueByLevel.SafeRandomAccess(LevelToIdx(oldLevel)) * 0.01f;
            NetworkAvatar.additionalPerfectGuardTime += NetworkAvatar.perfectGuardTime * ValueByLevel.SafeRandomAccess(LevelToIdx(newLevel)) * 0.01f;
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}