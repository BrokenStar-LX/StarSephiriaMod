using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_NaturalMagicCrystal : Charm_StatusInstance
    {
        public float CooldownTime = 5f;                    // 效果冷却
        public int[] CooldownReduceByLevel = [10, 20, 30]; // 命中减少：武器值 × 这个数值% 的冷却
        public float DamagePerExtraCount = 50f;            // 每 1 点风之刃额外数量，伤害提高 50%

        private readonly Timer Cooldown = new(5f, resetOnTime: false);
        private readonly Timer TriggerCooldown = new(0.1f, resetOnTime: false);
        private float HudTimer;

        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            float Weight = this.GetWeaponWeight(avatar);
            if (Weight <= 0f) Weight = 1f;
            float Reduce = CooldownReduceByLevel.SafeRandomAccess(LevelToIdx(level)) * Weight;
            float Time = CooldownTime / GetAttackSpeedMult(avatar);
            return
            [
                new("Time", Time.ToString("0.##")),
                new("Value", Reduce.ToString("#0.#") + "%", GetPositiveColor(virtualLevelOffset)),
                new("Damage", DamagePerExtraCount.ToString("#0") + "%", GetPositiveColor(virtualLevelOffset)),
            ];
        }

        private static float GetAttackSpeedMult(UnitAvatar? avatar)
        {
            float Speed = avatar != null ? avatar.GetCustomStatUnsafe(TextFile.WindSwordSpeed) : 0f;
            return Mathf.Max(0.1f, 1f + Speed * 0.01f);
        }

        private float GetCooldownTime()
        {
            return Mathf.Max(0.01f, CooldownTime / GetAttackSpeedMult(NetworkAvatar));
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            if (NetworkAvatar == null) return;
            Cooldown.time = GetCooldownTime();
            Cooldown.SetTimer(Cooldown.time);
            TriggerCooldown.time = 0.1f;
            TriggerCooldown.SetTimer(TriggerCooldown.time);
            HudTimer = 0f;
            NetworkAvatar.OnAttackUnit += OnAttackUnitEvent;
            UpdateHud();
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            if (NetworkAvatar == null) return;
            NetworkAvatar.OnAttackUnit -= OnAttackUnitEvent;
            Cooldown.SetTimer(Cooldown.time);
            HudTimer = 0f;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (!IsEffectEnabled || NetworkAvatar == null) return;
            if (TriggerCooldown.Ratio < 1f) TriggerCooldown.Update(Time.deltaTime);
            Cooldown.time = GetCooldownTime();
            if (Cooldown.Ratio < 1f) Cooldown.Update(Time.deltaTime);
            HudTimer += Time.deltaTime;
            if (HudTimer >= (Cooldown.Ratio < 1f ? 0.1f : 0.5f))
            {
                HudTimer = 0f;
                UpdateHud();
            }
        }

        private void OnAttackUnitEvent(UnitAvatar target, DamageInstance damage)
        {
            if (!IsEffectEnabled || NetworkAvatar == null || damage == null) return;
            if (damage.fromType != EDamageFromType.DirectAttack) return;
            if (TriggerCooldown.Ratio < 1f) return;

            if (Cooldown.Ratio < 1f)
            {
                float Weight = this.GetWeaponWeight();
                if (Weight <= 0f) return;
                TriggerCooldown.SetTimer(0f);
                Cooldown.time = GetCooldownTime();
                float Reduce = CooldownReduceByLevel.SafeRandomAccess(CurrentLevelToIdx()) * Weight * 0.01f;
                Cooldown.SetTimer(Mathf.Min(Cooldown.time, Cooldown.GetTimer() + Cooldown.time * Reduce));
                UpdateHud();
                return;
            }

            if (target == null || target.IsDead) return;
            Charm_WindSword? WindSword = Charm_WindSword.FindPlayerCharm<Charm_WindSword>(NetworkAvatar);
            if (WindSword == null) return;

            int ExtraCount = Mathf.Max(0, NetworkAvatar.GetCustomStatUnsafe(TextFile.WindSwordCount));
            float Mult = 1f + DamagePerExtraCount * 0.01f * ExtraCount;
            if (!WindSword.TriggerPhantomAttack(target, Mult)) return;

            TriggerCooldown.SetTimer(0f);
            Cooldown.time = GetCooldownTime();
            Cooldown.SetTimer(0f);
            HudTimer = 0f;
            UpdateHud();
        }

        private void UpdateHud()
        {
            if (NetworkAvatar == null) return;
            NetworkAvatar.SetEffectHUDFillAmount(GetCharmHUDID(), 1f - Cooldown.Ratio);
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}
