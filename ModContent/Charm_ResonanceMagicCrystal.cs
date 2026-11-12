using System.Collections;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_ResonanceMagicCrystal : Charm_EnergyCrystalBase
    {
        public int[] EnergyGainByLevel = [30, 60, 90];   // 命中获得：武器值 × 这个数值 的能量
        public float MaxEnergy = 100f;                   // 能量上限
        public float DamageBonusPerEnergy = 2f;          // 满能量时的增伤倍率（200%）
        public float MaxConsumePerHit = 50f;             // 每次最多消耗的能量（50 = 100% 增伤）

        private bool Consuming;

        protected override float MaxEnergyValue => MaxEnergy;

        protected override int GetEnergyGainByLevel(int level)
        {
            return EnergyGainByLevel.SafeRandomAccess(LevelToIdx(level));
        }

        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            float Gain = GetTooltipGain(avatar, level) / Mathf.Max(1f, MaxEnergy) * 100f;
            float Consume = Mathf.Clamp01(MaxConsumePerHit / Mathf.Max(1f, MaxEnergy)) * 100f;
            float Damage = DamageBonusPerEnergy * Consume * 0.01f * 100f;
            return
            [
                new("Value", Gain.ToString("#0.#") + "%", GetPositiveColor(virtualLevelOffset)),
                new("Mult", (DamageBonusPerEnergy * 100f).ToString("#0") + "%", GetPositiveColor(virtualLevelOffset)),
                new("Consume", Consume.ToString("#0.#") + "%"),
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
            damage.damage *= 1f + DamageBonusPerEnergy * Mathf.Clamp01(Consumed / Mathf.Max(1f, MaxEnergy));

            // 同一次挥砍命中多个敌人时，等这一帧的伤害都结算完再扣能量
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
