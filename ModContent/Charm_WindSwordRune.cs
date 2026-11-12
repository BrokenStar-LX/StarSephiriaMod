using Mirror;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_WindSwordRune : Charm_Active
    {
        public string EffectKeyword = "";//关键词ID（对应TextFile里的属性）
        public int Value;//加成数值
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            return
            [
                new("Effect", KeywordDatabase.Convert($"<tag={EffectKeyword}>")),
                new("Value", $"+{Value}%", GetPositiveColor(virtualLevelOffset)),
            ];
        }

        public override float GetCooldownTime(out bool changeByLevel, int level, int virtualLevelOffset)
        {
            changeByLevel = false;
            return 0f;
        }

        protected override void FireCastingServer(Vector3 aimedPosition, UnitAvatar aimedTarget)
        {
            if (NetworkAvatar is not PlayerAvatar Player) return;
            if (Player.isInDungeon <= 0) return;

            Charm_WindSword? WindSword = null;
            foreach (Charm_Basic Charm in Player.Inventory.charms.Values)
            {
                if (Charm is Charm_WindSword Target && Target.IsEffectEnabled)
                {
                    WindSword = Target;
                    break;
                }
            }
            if (WindSword == null) return;//没有风之刃就不消耗

            WindSword.AddRune(EffectKeyword, Value);
            using (new GridInventory.Permission(Player.Inventory))
            {
                Player.Inventory.ForceRemoveItem(Item.XIdx, Item.YIdx);
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}
