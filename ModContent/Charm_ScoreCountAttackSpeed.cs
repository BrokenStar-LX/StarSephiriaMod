using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_ScoreCountAttackSpeed : Charm_Basic
    {
        private StatusInstance? EffectInstance = null;
        private StatusInstance? EffectInstance2 = null;
        private readonly Timer CheckTime = new Timer(0.5f);
        private int Add = 0;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            int Level = LevelToIdx(level) + 1;
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("AttackSpeed", $"+{6 * Level}%", GetPositiveColor(virtualLevelOffset)),
                new Loc.KeywordValue("WeaponRange", $"+{3 * Level}%", GetPositiveColor(virtualLevelOffset)),
            };
        }
        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            if (NetworkAvatar) NetworkAvatar.AddCustomStatUnsafe(TextFile.ScoreCount, 1);
            ReInstance();
            LoadInstance();
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            if (NetworkAvatar) NetworkAvatar.AddCustomStatUnsafe(TextFile.ScoreCount, -1);
            ReInstance();
        }

        protected override void OnUpdatedLevel(int oldLevel, int newLevel)
        {
            ReInstance();
            LoadInstance();
            base.OnUpdatedLevel(oldLevel, newLevel);
        }

        private void ReInstance()
        {
            if (EffectInstance != null)
            {
                EffectInstance.RemoveStatus();
                EffectInstance.ClearTarget();
                EffectInstance = null;
            }
            if (EffectInstance2 != null)
            {
                EffectInstance2.RemoveStatus();
                EffectInstance2.ClearTarget();
                EffectInstance2 = null;
            }
            Add = 0;
        }
        private void LoadInstance()
        {
            if (NetworkAvatar)
            {
                Add = NetworkAvatar.GetCustomStatUnsafe(TextFile.ScoreCount);
                if (Add > 0)
                {
                    int Count = CurrentLevelToIdx() + 1;
                    EffectInstance = StatusDatabase.CreateStatusEntity("ATTACK_SPEED", Add * Count * 6);
                    EffectInstance.SetTarget(NetworkAvatar);
                    EffectInstance.ApplyStatus(true);
                    EffectInstance2 = StatusDatabase.CreateStatusEntity("WEAPON_RANGE", Add * Count * 3);
                    EffectInstance2.SetTarget(NetworkAvatar);
                    EffectInstance2.ApplyStatus(true);
                }
            }
        }

        protected override void OnUpdate()
        {
            if (NetworkAvatar && CheckTime.Update(Time.deltaTime))
            {
                if (Add != NetworkAvatar.GetCustomStatUnsafe(TextFile.ScoreCount))
                {
                    ReInstance();
                    LoadInstance();
                }
            }
            base.OnUpdate();
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}