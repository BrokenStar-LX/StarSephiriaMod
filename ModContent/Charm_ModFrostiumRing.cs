using Mirror;
using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_ModFrostiumRing : Charm_FrostiumRing
    {
        public string ElementalName = "";
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            string value = damageByLevel.SafeRandomAccess(LevelToIdx(level)).ToString();
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("ELEMENTAL", $"<tag={ElementalName}>"),
                new Loc.KeywordValue("DAMAGE", value, GetPositiveColor(virtualLevelOffset)),
                new Loc.KeywordValue("TIME", cooldownTimer.time.ToString("0.##"))
            };
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}