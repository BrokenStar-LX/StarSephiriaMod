using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    /// <summary>能量类魔晶（共振/蓄能）的公共逻辑：武器命中充能、0.1 秒获取间隔、能量 HUD。</summary>
    public abstract class Charm_EnergyCrystalBase : Charm_StatusInstance
    {
        private const string WindSwordDamageId = "[Star's Sephiria Mod]WindSword";

        public float GainInterval = 0.1f;   // 两次能量获取的最小间隔

        protected float Energy;
        private int LastHudValue = -1;
        private readonly Timer GainCooldown = new(0.1f, resetOnTime: false);

        protected abstract float MaxEnergyValue { get; }
        protected abstract int GetEnergyGainByLevel(int level);

        /// <summary>风之刃伤害结算前的额外处理（damage.id 已确认是风之刃）。</summary>
        protected virtual void ApplyWindSwordDamage(DamageInstance damage) { }

        /// <summary>启用/停用时重置子类的附加状态。</summary>
        protected virtual void OnResetEnergyCrystal() { }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            if (NetworkAvatar == null) return;
            Energy = 0f;
            LastHudValue = -1;
            GainCooldown.time = Mathf.Max(0.01f, GainInterval);
            GainCooldown.SetTimer(GainCooldown.time);
            OnResetEnergyCrystal();
            NetworkAvatar.OnAttackUnit += OnAttackUnitEvent;
            NetworkAvatar.OnAttackUnitBeforeOperation += OnWindSwordDamage;
            UpdateHud(force: true);
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            if (NetworkAvatar == null) return;
            NetworkAvatar.OnAttackUnit -= OnAttackUnitEvent;
            NetworkAvatar.OnAttackUnitBeforeOperation -= OnWindSwordDamage;
            Energy = 0f;
            LastHudValue = -1;
            OnResetEnergyCrystal();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (GainCooldown.Ratio < 1f)
                GainCooldown.Update(Time.deltaTime);
        }

        private void OnAttackUnitEvent(UnitAvatar target, DamageInstance damage)
        {
            if (!IsEffectEnabled || damage == null) return;
            if (damage.fromType != EDamageFromType.DirectAttack) return;
            if (GainCooldown.Ratio < 1f) return;   // 0.1 秒内只能获得一次能量
            float Weight = this.GetWeaponWeight();
            if (Weight <= 0f) return;
            GainCooldown.SetTimer(0f);
            AddEnergy(GetEnergyGainByLevel(CurrentLevelToIdx()) * Weight);
        }

        private void OnWindSwordDamage(UnitAvatar target, DamageInstance damage)
        {
            if (!IsEffectEnabled || damage == null) return;
            if (damage.id != WindSwordDamageId) return;
            ApplyWindSwordDamage(damage);
        }

        protected void AddEnergy(float value)
        {
            float Before = Energy;
            Energy = Mathf.Clamp(Energy + value, 0f, MaxEnergyValue);
            if (Energy != Before) UpdateHud();
        }

        protected void ConsumeEnergy(float value)
        {
            Energy = Mathf.Max(0f, Energy - value);
            UpdateHud();
        }

        /// <summary>tooltip 用：当前等级一次命中的实际获取量（已乘武器值）。</summary>
        protected float GetTooltipGain(UnitAvatar? avatar, int level)
        {
            float Weight = this.GetWeaponWeight(avatar);
            if (Weight <= 0f) Weight = 1f;
            return GetEnergyGainByLevel(level) * Weight;
        }

        protected void UpdateHud(bool force = false)
        {
            if (NetworkAvatar == null) return;
            float Max = MaxEnergyValue;
            int Display = Mathf.Clamp(Mathf.RoundToInt(Energy), 0, Mathf.RoundToInt(Max));
            if (force || Display != LastHudValue)
            {
                LastHudValue = Display;
                NetworkAvatar.SetEffectHUDValue(GetCharmHUDID(), Display.ToString());
            }
            NetworkAvatar.SetEffectHUDFillAmount(GetCharmHUDID(), Max > 0f ? 1f - Energy / Max : 1f);
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}
