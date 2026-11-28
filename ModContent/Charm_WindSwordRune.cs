using Mirror;
using SPMod.Tool;
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

            Charm_WindSword? WindSword = Player.FindPlayerCharm<Charm_WindSword>();
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
