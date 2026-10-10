using SPMod.Tool;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_ElementAllocation : Charm_StatusInstance
    {
        public int[] ValueByLevel = new int[] { 20, 40, 60 };
        private readonly List<string> EffectInstance = new List<string>();
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Value", ValueByLevel.SafeRandomAccess(LevelToIdx(level)).ToString() + "%", GetPositiveColor(virtualLevelOffset)),
            };
        }
        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            StarSephiriaPatches.OnUpdateHighestElemental += OnUpdateHighestElemental;
            ReInstance();
            LoadInstance();
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            StarSephiriaPatches.OnUpdateHighestElemental -= OnUpdateHighestElemental;
            ReInstance();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
        }

        public void OnUpdateHighestElemental()
        {
            ReInstance();
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

        private void ReInstance()
        {
            foreach(string Key in EffectInstance.ToList())
            {
                NetworkAvatar.calculatedBonusStats[Key] = 0;
                EffectInstance.Remove(Key);
            }
        }
        private void LoadInstance()
        {
            if (NetworkAvatar)
            {
                GetTwoMaxValue(out int TwoMaxStatusValue);
                if (TwoMaxStatusValue <= 0) return;
                Dictionary<string, int> StatusList = new Dictionary<string, int>();
                foreach (var key in EffectConstant.BaseStatsName)
                {
                    int Value = NetworkAvatar.customStats.GetValueOrDefault(key, 0);
                    if (Value == TwoMaxStatusValue)
                        StatusList[key] = Value;
                }
                if (StatusList.Count <= 0) return;
                int ElementalDamage = NetworkAvatar.highestElementalBonus + StarSephiriaMod.PartyStats.Value.ElementalDamage;
                foreach (var key in StatusList.Keys)
                {
                    int Add = ElementalDamage.IntMult(ValueByLevel.SafeRandomAccess(CurrentLevelToIdx()) * 0.01f);
                    NetworkAvatar.calculatedBonusStats[key] = Add;
                    EffectInstance.Add(key);
                }
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}