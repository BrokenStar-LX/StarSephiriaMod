using SPMod.Tool;
using System;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_GoldDamage : Charm_StatusInstance
    {
        public int[] CoinToDamageByLevel = new int[] { 500 };
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            int Add = 0;
            int All = 0;
            if(avatar)
            {
                int Coin = avatar.GetCustomStatUnsafe(TextFile.GoldObtained);
                if (Coin <= 0) Coin = avatar.Money;
                Add += (int)MathF.Floor(Coin / CoinToDamageByLevel.SafeRandomAccess(LevelToIdx(level)));
                All = Coin;
            }
            string text = CoinToDamageByLevel.SafeRandomAccess(LevelToIdx(level)).ToString();
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Coin", text.ToString(), GetPositiveColor(virtualLevelOffset)),
                new Loc.KeywordValue("Add", Add.ToString() + "%" + $" ({All})", Color.yellow),
            };
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            if (NetworkAvatar)
            {
                NetworkAvatar.AddCustomStatUnsafe(TextFile.CoinToDamage, CoinToDamageByLevel.SafeRandomAccess(CurrentLevelToIdx()));
            }
        }

        protected override void OnUpdatedLevel(int oldLevel, int newLevel)
        {
            base.OnUpdatedLevel(oldLevel, newLevel);
            if (NetworkAvatar)
            {
                int Level = LevelToIdx(oldLevel);
                NetworkAvatar.AddCustomStatUnsafe(TextFile.CoinToDamage, -CoinToDamageByLevel.SafeRandomAccess(Level));
                Level = LevelToIdx(newLevel);
                NetworkAvatar.AddCustomStatUnsafe(TextFile.CoinToDamage, CoinToDamageByLevel.SafeRandomAccess(Level));
            }
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            if (NetworkAvatar)
            {
                NetworkAvatar.AddCustomStatUnsafe(TextFile.CoinToDamage, -CoinToDamageByLevel.SafeRandomAccess(CurrentLevelToIdx()));
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}