using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_EnergyStorageMagicCrystal : Charm_EnergyCrystalBase
    {
        public int[] EnergyGainByLevel = [15, 30, 45];   // 命中获得：武器值 × 这个数值 的能量
        public float MaxEnergy = 400f;                   // 能量上限
        public float EnergyPerDamage = 5f;               // 每 X 点能量
        public float DamagePerEnergy = 1f;               // 提高 1% 风之刃伤害（独立乘区）
        public float DecayInterval = 0.2f;               // 流失间隔
        public float DecayPercent = 5f;                  // 每次流失当前能量的百分比
        public float DecayMin = 1f;                      // 每次至少流失 1 点

        private readonly Timer DecayTimer = new(0.2f);

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
            DecayTimer.time = Mathf.Max(0.01f, DecayInterval);
            DecayTimer.SetTimer(0f);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (!IsEffectEnabled || NetworkAvatar == null || Energy <= 0f) return;
            if (!DecayTimer.Update(Time.deltaTime)) return;
            ConsumeEnergy(Mathf.Max(DecayMin, Energy * DecayPercent * 0.01f));
        }

        protected override void ApplyWindSwordDamage(DamageInstance damage)
        {
            if (Energy <= 0f) return;
            damage.damage *= 1f + Energy / Mathf.Max(0.01f, EnergyPerDamage) * DamagePerEnergy * 0.01f;
        }
    }
}
