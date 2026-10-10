using SPMod.Tool;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

namespace SPMod.ModContent
{
    public class Charm_MagicBookAddDamage : Charm_Basic
    {
        private StatusInstance? EffectInstance = null;
        private StatusInstance? EffectInstance2 = null;
        private StatusInstance? EffectInstance3 = null;
        private StatusInstance? EffectInstance4 = null;
        public int[] AddByLevel = new int[] { 1, 1, 2 };
        public int[] AddByLevel2 = new int[] { 2, 4, 4 };
        private readonly Timer CheckTime = new Timer(0.5f);
        private int Add = 0;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            string Add = "+" + AddByLevel.SafeRandomAccess(LevelToIdx(level));
            string Level = "+" + AddByLevel2.SafeRandomAccess(LevelToIdx(level)) + "%";
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Add", Add, GetPositiveColor(virtualLevelOffset)),
                new Loc.KeywordValue("Add2", Level, GetPositiveColor(virtualLevelOffset)),
            };
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            ReInstance();
            LoadInstance();
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
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
            if (EffectInstance3 != null)
            {
                EffectInstance3.RemoveStatus();
                EffectInstance3.ClearTarget();
                EffectInstance3 = null;
            }
            if (EffectInstance4 != null)
            {
                EffectInstance4.RemoveStatus();
                EffectInstance4.ClearTarget();
                EffectInstance4 = null;
            }
            Add = 0;
        }
        private void LoadInstance()
        {
            if (NetworkAvatar)
            {
                Add = GetMagicStack();
                if (Add > 0)
                {
                    int Count = AddByLevel.SafeRandomAccess(CurrentLevelToIdx());
                    EffectInstance = StatusDatabase.CreateStatusEntity("FIRE_DAMAGE", Count * Add);
                    EffectInstance.SetTarget(NetworkAvatar);
                    EffectInstance.ApplyStatus(true);
                    EffectInstance2 = StatusDatabase.CreateStatusEntity("LIGHTNING_DAMAGE", Count * Add);
                    EffectInstance2.SetTarget(NetworkAvatar);
                    EffectInstance2.ApplyStatus(true);
                    EffectInstance3 = StatusDatabase.CreateStatusEntity("ICE_DAMAGE", Count * Add);
                    EffectInstance3.SetTarget(NetworkAvatar);
                    EffectInstance3.ApplyStatus(true);
                    Count = AddByLevel2.SafeRandomAccess(CurrentLevelToIdx());
                    EffectInstance4 = StatusDatabase.CreateStatusEntity("MAGIC_DAMAGE_BONUS", Count * Add);
                    EffectInstance4.SetTarget(NetworkAvatar);
                    EffectInstance4.ApplyStatus(true);
                }
            }
        }
        private int GetMagicStack()
        {
            int Stack = 0;
            if (NetworkAvatar.TryGetComponent<SkillController>(out SkillController skillController))
            {
                foreach (Charm_Basic Charm in skillController.GridInventory.charms.Values)
                {
                    if (Charm is Charm_Magic Magic)
                    {
                        if (Magic == null) continue;
                        if (Magic.ContainedMagic.magicPrefab.TryGetComponent<ActiveSkill>(out var Target) && Target != null)
                        {
                            Stack += 1;
                        }
                    }
                }
            }
            return Stack;
        }

        protected override void OnUpdate()
        {
            if (NetworkAvatar && CheckTime.Update(Time.deltaTime))
            {
                if (Add != GetMagicStack())
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