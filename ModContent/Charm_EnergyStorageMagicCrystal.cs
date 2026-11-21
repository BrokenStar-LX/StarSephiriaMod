using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_EnergyStorageMagicCrystal : Charm_EnergyCrystalBase
    {
        public int[] EnergyGainByLevel = [20, 40, 60];   // 命中获得：武器值 × 这个数值 的能量
        public float MaxEnergy = 500f;                   // 能量上限
        public float EnergyPerDamage = 5f;               // 每 X 点能量
        public float DamagePerEnergy = 1f;               // 提高 1% 风之刃伤害（并入 Stat_WindSwordDamage）
        public float DecayInterval = 0.2f;              // 流失间隔
        public float DecayPercent = 5f;                  // 每次流失当前能量的百分比
        public float DecayMin = 1f;                      // 每次至少流失 1 点

        private readonly Timer DecayTimer = new(0.2f);
        private int AppliedDamageBonus;                  // 已经加进 Stat_WindSwordDamage 的百分数
        protected override float MaxEnergyValue => MaxEnergy;
        protected override int GetEnergyGainByLevel(int level)
        {
            return EnergyGainByLevel.SafeRandomAccess(LevelToIdx(level));
        }

        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            float Gain = GetTooltipGain(avatar, level);
            return
            [
                new("Value", Gain.ToString("#0.#"), GetPositiveColor(virtualLevelOffset)),
                new("Per", EnergyPerDamage.ToString("#0.#"), GetPositiveColor(virtualLevelOffset)),
                new("Mult", DamagePerEnergy.ToString("#0.#") + "%", GetPositiveColor(virtualLevelOffset)),
            ];
        }

        protected override void OnResetEnergyCrystal()
        {
            RemoveDamageBonus();
            DecayTimer.time = Mathf.Max(0.01f, DecayInterval);
            DecayTimer.SetTimer(0f);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (!IsEffectEnabled || NetworkAvatar == null) return;
            if (Energy > 0f && DecayTimer.Update(Time.deltaTime))
                ConsumeEnergy(Mathf.Max(DecayMin, Energy * DecayPercent * 0.01f));
            RefreshDamageBonus();
        }

        /// <summary>把当前能量换算成 Stat_WindSwordDamage 的整数百分点，和其他风之刃伤害加成共用同一乘区。</summary>
        private void RefreshDamageBonus()
        {
            if (NetworkAvatar == null) return;
            int Wanted = Mathf.FloorToInt(Energy / Mathf.Max(0.01f, EnergyPerDamage) * DamagePerEnergy);
            if (Wanted == AppliedDamageBonus) return;
            NetworkAvatar.AddCustomStatUnsafe(TextFile.WindSwordDamage, Wanted - AppliedDamageBonus);
            AppliedDamageBonus = Wanted;
        }

        private void RemoveDamageBonus()
        {
            if (NetworkAvatar != null && AppliedDamageBonus != 0)
                NetworkAvatar.AddCustomStatUnsafe(TextFile.WindSwordDamage, -AppliedDamageBonus);
            AppliedDamageBonus = 0;
        }
    }
}
