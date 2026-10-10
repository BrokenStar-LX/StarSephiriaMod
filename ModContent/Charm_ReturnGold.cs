using SPMod.Tool;
using System.Linq;

namespace SPMod.ModContent
{
    public class Charm_ReturnGold : Charm_Basic
    {
        public int[] ReturnCoinByLevel = new int[] { 40 };
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            string text;
            if (!showAllLevel)
            {
                text = ReturnCoinByLevel.SafeRandomAccess(LevelToIdx(level)).ToString() + "%";
            }
            else
            {
                text = ReturnCoinByLevel.SafeRandomAccess(0).ToString() + "%" + "→" + ReturnCoinByLevel.SafeRandomAccess(maxLevel).ToString() + "%";
            }
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Coin", text.ToString(), GetPositiveColor(virtualLevelOffset)),
            };
        }
        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            NetworkAvatar.OnTradeBeginServerside += HandleTradeBegin;
            DungeonPatchs.OnLoadNewNumberofReincarnations += ReSave;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.OnTradeBeginServerside -= HandleTradeBegin;
            DungeonPatchs.OnLoadNewNumberofReincarnations -= ReSave;
        }

        private void ReSave()
        {
            if (NetworkAvatar is PlayerAvatar Player)
            {
                foreach (string Falg in Player.savableParameters.ToList())
                {
                    if (Falg.StartsWith("ReturnGold/"))
                        Player.savableParameters.Remove(Falg);
                }
            }
        }

        private void HandleTradeBegin(string socialID)
        {
            if (string.IsNullOrEmpty(socialID))
            {
                return;
            }
            if (NetworkAvatar is PlayerAvatar Player)
            {
                if (Player.savableParameters.Contains("ReturnGold/" + socialID))
                {
                    return;
                }
                Player.savableParameters.Add("ReturnGold/" + socialID);
                if(Player.GetCustomStatUnsafe(TextFile.ReturnCoin) <= 0) Player.AddCustomStatUnsafe(TextFile.ReturnCoin, 1);
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}