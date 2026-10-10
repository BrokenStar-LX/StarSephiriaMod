using Newtonsoft.Json;
using SPMod.Tool;
using Unity.VisualScripting;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_LuckCrit : Charm_Basic
    {
        private readonly Timer CheckTime = new Timer(0.5f);
        public int[] MultByLevel = new int[] { 40, 60, 80, 100 };
        private StatusInstance? EffectInstance = null;
        private int Add = 0;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            float Value = avatar.GetCustomStatUnsafe(TextFile.Luck) * MultByLevel.SafeRandomAccess(LevelToIdx(level)) * 0.01f;
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Value", "+" + ((int)Value).ToString() + "%".ToString(), GetPositiveColor(virtualLevelOffset)),
                new Loc.KeywordValue("Mult", MultByLevel.SafeRandomAccess(LevelToIdx(level)).ToString() + "%".ToString(), GetPositiveColor(virtualLevelOffset)),
            };
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            NetworkAvatar.OnAttackUnitBeforeOperation += AddDamage;
            NetworkAvatar.OnAttackUnitBeforeOperation_AsLeader += AsLeader;
            ReInstance();
            LoadInstance();
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.OnAttackUnitBeforeOperation -= AddDamage;
            NetworkAvatar.OnAttackUnitBeforeOperation_AsLeader -= AsLeader;
            ReInstance();
        }

        private void AsLeader(UnitAvatar Follower, UnitAvatar Target, DamageInstance Damage)
        {
            AddDamage(Target, Damage);
        }

        private void AddDamage(UnitAvatar Target, DamageInstance Damage)
        {
            float Chance = NetworkAvatar.GetCustomStatUnsafe(TextFile.Luck) * MultByLevel.SafeRandomAccess(CurrentLevelToIdx()) * 0.01f;
            Damage.ManualCritChance(Chance, NetworkAvatar.GetCustomStatUnsafe(TextFile.Execution) > 0);
        }

        private void ReInstance()
        {
            if (EffectInstance != null)
            {
                EffectInstance.RemoveStatus();
                EffectInstance.ClearTarget();
                EffectInstance = null;
            }
            Add = 0;
        }
        private void LoadInstance()
        {
            if (NetworkAvatar)
            {
                float Value = NetworkAvatar.GetCustomStatUnsafe(TextFile.Luck) * MultByLevel.SafeRandomAccess(CurrentLevelToIdx()) * 0.01f;
                Add = (int)Value;
                if (Add > 0)
                {
                    EffectInstance = StatusDatabase.CreateStatusEntity("CRITICAL_DAMAGE_RATE", Add);
                    EffectInstance.SetTarget(NetworkAvatar);
                    EffectInstance.ApplyStatus(true);
                }
            }
        }

        protected override void OnUpdate()
        {
            if (NetworkAvatar && CheckTime.Update(Time.deltaTime))
            {
                float Value = NetworkAvatar.GetCustomStatUnsafe(TextFile.Luck) * MultByLevel.SafeRandomAccess(CurrentLevelToIdx()) * 0.01f;
                if (Add != (int)Value)
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