using Mirror;
using SPMod.Tool;

namespace SPMod.ModContent
{
    public class Charm_MultiPlayerAddStatus : Charm_Basic
    {
        public int[] ValueByLevel = new int[] { 1, 2, 3, 4 };
        public int[] Value2ByLevel = new int[] { 1, 2, 3, 4 };
        public int PlayerStack = 0;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            string text;
            string text2;
            if (!showAllLevel)
            {
                text = ValueByLevel.SafeRandomAccess(LevelToIdx(level)).ToString() + "%";
                text2 = Value2ByLevel.SafeRandomAccess(LevelToIdx(level)).ToString();
            }
            else
            {
                text = ValueByLevel.SafeRandomAccess(0).ToString() + "%" + "→" + ValueByLevel.SafeRandomAccess(maxLevel).ToString() + "%";
                text2 = Value2ByLevel.SafeRandomAccess(0).ToString() + "→" + Value2ByLevel.SafeRandomAccess(maxLevel).ToString();
            }
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("AllDamage", "+" + text.ToString(), GetPositiveColor(virtualLevelOffset)),
                new Loc.KeywordValue("Defense", "+" + text2.ToString(), GetPositiveColor(virtualLevelOffset)),
            };
        }

        protected override void OnUpdate()
        {
            if(PlayerStack != NetworkServer.connections.Count)
            {
                OnUpdatedLevel(CurrentLevelToIdx(), CurrentLevelToIdx());
            }
            base.OnUpdate();
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            if (NetworkAvatar)
            {
                PlayerStack = NetworkServer.connections.Count;
                NetworkAvatar.AddCustomStatUnsafe(TextFile.AllPlayerDefense, ValueByLevel.SafeRandomAccess(CurrentLevelToIdx()) * PlayerStack);
                NetworkAvatar.AddCustomStatUnsafe(TextFile.AllPlayerAllDamage, Value2ByLevel.SafeRandomAccess(CurrentLevelToIdx()) * PlayerStack);
            }
        }

        protected override void OnUpdatedLevel(int oldLevel, int newLevel)
        {
            base.OnUpdatedLevel(oldLevel, newLevel);
            if (NetworkAvatar && maxLevel > 0)
            {
                int Level = LevelToIdx(oldLevel);
                NetworkAvatar.AddCustomStatUnsafe(TextFile.AllPlayerDefense, -ValueByLevel.SafeRandomAccess(Level) * PlayerStack);
                NetworkAvatar.AddCustomStatUnsafe(TextFile.AllPlayerAllDamage, -ValueByLevel.SafeRandomAccess(Level) * PlayerStack);
                PlayerStack = NetworkServer.connections.Count;
                Level = LevelToIdx(newLevel);
                NetworkAvatar.AddCustomStatUnsafe(TextFile.AllPlayerDefense, ValueByLevel.SafeRandomAccess(Level) * PlayerStack);
                NetworkAvatar.AddCustomStatUnsafe(TextFile.AllPlayerAllDamage, Value2ByLevel.SafeRandomAccess(Level) * PlayerStack);
            }
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            if (NetworkAvatar)
            {
                NetworkAvatar.AddCustomStatUnsafe(TextFile.AllPlayerDefense, -ValueByLevel.SafeRandomAccess(CurrentLevelToIdx()) * PlayerStack);
                NetworkAvatar.AddCustomStatUnsafe(TextFile.AllPlayerAllDamage, -Value2ByLevel.SafeRandomAccess(CurrentLevelToIdx()) * PlayerStack);
                PlayerStack = 0;
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}