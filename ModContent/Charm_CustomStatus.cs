namespace SPMod.ModContent
{
    public class Charm_CustomStatus : Charm_StatusInstance
    {
        public int[] ValueByLevel = new int[0];
        public string ValueName = "";
        public string Ending = "";
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            string text;
            if (!showAllLevel)
            {
                text = ValueByLevel.SafeRandomAccess(LevelToIdx(level)).ToString() + Ending;
            }
            else
            {
                text = ValueByLevel.SafeRandomAccess(0).ToString() + Ending + "→" + ValueByLevel.SafeRandomAccess(maxLevel).ToString() + Ending;
            }
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Value", text.ToString(), GetPositiveColor(virtualLevelOffset)),
            };
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            if (NetworkAvatar)
            {
                NetworkAvatar.AddCustomStatUnsafe(ValueName, ValueByLevel.SafeRandomAccess(CurrentLevelToIdx()));
            }
        }

        protected override void OnUpdatedLevel(int oldLevel, int newLevel)
        {
            base.OnUpdatedLevel(oldLevel, newLevel);
            if (NetworkAvatar && maxLevel > 0)
            {
                int Level = LevelToIdx(oldLevel);
                NetworkAvatar.AddCustomStatUnsafe(ValueName, -ValueByLevel.SafeRandomAccess(Level));
                Level = LevelToIdx(newLevel);
                NetworkAvatar.AddCustomStatUnsafe(ValueName, ValueByLevel.SafeRandomAccess(Level));
            }
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            if (NetworkAvatar)
            {
                NetworkAvatar.AddCustomStatUnsafe(ValueName, -ValueByLevel.SafeRandomAccess(CurrentLevelToIdx()));
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}