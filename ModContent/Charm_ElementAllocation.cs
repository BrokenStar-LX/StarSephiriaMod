using SPMod.Tool;
using System.Collections.Generic;

namespace SPMod.ModContent
{
    public class Charm_ElementAllocation : Charm_StatusInstance
    {
        public int[] ValueByLevel = [20, 40, 60];
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            float Mult = ValueByLevel.SafeRandomAccess(LevelToIdx(level)) * 0.01f;
            if (Tools.CostumeSkinUse(avatar.Name, "WhiteRabbit_2", false))
            {
                Mult *= 1.25f;
            }
            Mult *= 100f;
            return
            [
                new("Value", Mult.ToString() + "%", GetPositiveColor(virtualLevelOffset)),
            ];
        }
        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            StarSephiriaPatches.OnUpdateHighestElemental += OnUpdateHighestElemental;
            LoadInstance();
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            StarSephiriaPatches.OnUpdateHighestElemental -= OnUpdateHighestElemental;
            if (NetworkAvatar) StarSephiriaPatches.UnitAvatar_Patches.RefreshElementalBonus(NetworkAvatar);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
        }

        public void OnUpdateHighestElemental()
        {
            LoadInstance();
        }

        public void GetTwoMaxValue(out int TwoMaxValue)
        {
            int MaxValue = Tools.BaseOrigStatToMax(NetworkAvatar);
            TwoMaxValue = 0;
            foreach (var key in EffectConstant.BaseStatsName)
            {
                int Value = NetworkAvatar.customStats.GetValueOrDefault(key, 0);
                if (Value < MaxValue && TwoMaxValue < Value)
                    TwoMaxValue = Value;
            }
            if (TwoMaxValue == MaxValue) TwoMaxValue = 0;
        }

        private void LoadInstance()
        {
            if (NetworkAvatar)
            {
                GetTwoMaxValue(out int TwoMaxStatusValue);
                if (TwoMaxStatusValue <= 0) return;
                Dictionary<string, int> StatusList = [];
                foreach (var key in EffectConstant.BaseStatsName)
                {
                    int Value = NetworkAvatar.customStats.GetValueOrDefault(key, 0);
                    if (Value == TwoMaxStatusValue)
                        StatusList[key] = Value;
                }
                if (StatusList.Count <= 0) return;
                int ElementalDamage = NetworkAvatar.highestElementalBonus + StarSephiriaMod.PartyStats.ElementalDamage;
                float Mult = ValueByLevel.SafeRandomAccess(CurrentLevelToIdx()) * 0.01f;
                if (Tools.CostumeSkinUse(NetworkAvatar.Name, "WhiteRabbit_2", false))
                {
                    Mult *= 1.25f;
                }
                foreach (var key in StatusList.Keys)
                {
                    int Add = ElementalDamage.IntMult(Mult);
                    NetworkAvatar.calculatedBonusStats[key] = Add;
                }
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}