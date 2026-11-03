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
            return
            [
                new("ELEMENTAL", $"<tag={ElementalName}>"),
                new("DAMAGE", value, GetPositiveColor(virtualLevelOffset)),
                new("TIME", cooldownTimer.time.ToString("0.##"))
            ];
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}