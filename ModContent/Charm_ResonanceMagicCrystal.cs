using System.Collections;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_ResonanceMagicCrystal : Charm_EnergyCrystalBase
    {
        public int[] EnergyGainByLevel = [18, 36, 48];   // 命中获得：武器值 × 这个数值 的能量
        public float MaxEnergy = 200f;                   // 能量上限
        public float DamageBonusPerEnergy = 2f;          // 每点消耗能量转化的风之刃伤害%（200% 转化率）
        public float MaxConsumePerHit = 20f;             // 每次最多消耗的能量

        private bool Consuming;
        protected override float MaxEnergyValue => MaxEnergy;
        protected override int GetEnergyGainByLevel(int level)
        {
            return EnergyGainByLevel.SafeRandomAccess(LevelToIdx(level));
        }

        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            float Gain = GetTooltipGain(avatar, level) / Mathf.Max(1f, MaxEnergy) * 100f;
            float Damage = DamageBonusPerEnergy * MaxConsumePerHit;
            return
            [
                new("Value", Gain.ToString("#0.#") + "%", GetPositiveColor(virtualLevelOffset)),
                new("Mult", (DamageBonusPerEnergy * 100f).ToString("#0") + "%", GetPositiveColor(virtualLevelOffset)),
                new("Consume", MaxConsumePerHit.ToString("#0.#")),
                new("Damage", Damage.ToString("#0") + "%", GetPositiveColor(virtualLevelOffset)),
            ];
        }

        protected override void OnResetEnergyCrystal()
        {
            Consuming = false;
        }

        protected override void ApplyWindSwordDamage(DamageInstance damage)
        {
            if (Energy <= 0f) return;
            float Consumed = Mathf.Min(Energy, MaxConsumePerHit);
            damage.damage *= 1f + DamageBonusPerEnergy * Consumed * 0.01f;

            if (Consuming) return;
            Consuming = true;
            StartCoroutine(ConsumeNextFrame(Consumed));
        }

        private IEnumerator ConsumeNextFrame(float Consumed)
        {
            yield return null;
            Consuming = false;
            if (!IsEffectEnabled) yield break;
            ConsumeEnergy(Consumed);
        }
    }
}
