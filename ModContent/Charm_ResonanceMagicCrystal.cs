using System.Collections;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_ResonanceMagicCrystal : Charm_EnergyCrystalBase
    {
        public int[] EnergyGainByLevel = [30, 60, 90];   // 命中获得：武器值 × 这个数值 的能量
        public float MaxEnergy = 500f;                   // 能量上限
        public float DamageBonusPerEnergy = 2f;          // 每消耗 1 点能量增加的风之刃伤害%
        public float MaxConsumePerHit = 10f;             // 每次攻击消耗当前能量的百分比

        private bool Consuming;
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
                new("Mult", DamageBonusPerEnergy.ToString("#0.#") + "%", GetPositiveColor(virtualLevelOffset)),
                new("Consume", MaxConsumePerHit.ToString("#0.#")),
            ];
        }

        protected override void OnResetEnergyCrystal()
        {
            Consuming = false;
        }

        protected override void ApplyWindSwordDamage(DamageInstance damage)
        {
            if (Energy <= 0f) return;
            float Consumed = Energy * Mathf.Clamp01(MaxConsumePerHit * 0.01f);
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
