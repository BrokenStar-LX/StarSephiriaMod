using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_FlameSwordAutoAttack : Charm_StatusInstance
    {
        public int[] AttoAttackLuckByLevel = new int[] { 10, 20, 30, 40, 50 };
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            string text;
            if (!showAllLevel)
            {
                text = AttoAttackLuckByLevel.SafeRandomAccess(LevelToIdx(level)).ToString() + "%";
            }
            else
            {
                text = AttoAttackLuckByLevel.SafeRandomAccess(0).ToString() + "%" + "→" + AttoAttackLuckByLevel.SafeRandomAccess(maxLevel).ToString() + "%";
            }
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("LUCK", text.ToString(), GetPositiveColor(virtualLevelOffset)),
            };
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            if (NetworkAvatar)
            {
                NetworkAvatar.OnUniversalBattleCallback += OnUniversalBattleCallback;
            }
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            if (NetworkAvatar)
            {
                NetworkAvatar.OnUniversalBattleCallback -= OnUniversalBattleCallback;
            }
        }

        private void OnUniversalBattleCallback(string Name, int Amount)
        {
            if(Name == "ADDFLAMESWORD")
            {
                if (Random.value <= AttoAttackLuckByLevel.SafeRandomAccess(CurrentLevelToIdx()) * 0.01f)
                {
                    UnitAvatar Player = NetworkAvatar;
                    if (Player.Inventory)
                    {
                        ComboEffectBase comboEffectBase = Player.Inventory.FindComboEffect("FLAMESWORD");
                        if (comboEffectBase != null && comboEffectBase is ComboEffect_FlameSword comboEffect_FlameSword)
                        {
                            UnitAvatar Target = PlayerInputController.SearchTargetNearestPoint(Player, Player.transform.position, 60f);
                            if (Target != null)
                            {
                                Vector3 TargetPos = Target.transform.position;
                                Vector3 ShootPos = TargetPos + (Vector3)Random.insideUnitCircle.normalized * 2.5f;
                                comboEffect_FlameSword.FireSword(ShootPos, TargetPos);
                            }
                        }
                    }
                }
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}