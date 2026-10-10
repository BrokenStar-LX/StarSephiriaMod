using SPMod.Tool;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace SPMod.ModContent
{
    public class Charm_DodgeThorns : Charm_StatusInstance, IAttackableCharm
    {
        public int[] DamageMultByLevel = new int[] { 50, 100, 150, 200 };
        public Timer CooldownTimer = new Timer(0.5f);
        private StatusInstance? EffectInstance = null;
        private StatusInstance? EffectInstance2 = null;
        public float AddValue = 0;
        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            NetworkAvatar.OnAttackUnit += AddDamage;
            ReInstance();
            LoadInstance();
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.OnAttackUnit -= AddDamage;
            ReInstance();
        }

        private void AddDamage(UnitAvatar avatar, DamageInstance instance)
        {
            if (instance.id == "Ability_Thorns" || instance.id == "[Star's Sephiria Mod]反击")
            {
                string Id = "[Star's Sephiria Mod]坎坷命运";
                var Dam = DamageInstance.GetDamage(NetworkAvatar, Id, avatar.transform.position, 4294967295L, CalculateDamage(this), EDamageType.Slice, EDamageFromType.None, Vector2.zero, 0, 0f);
                avatar.ApplyDamage(Dam);
            }
        }

        public float GetDamage(UnitAvatar avatar)
        {
            float Damage = avatar.GetCustomStatUnsafe(TextFile.DamageReduction);
            float Mult = avatar.GetCustomStatUnsafe(TextFile.Thorns) * 0.01f;
            float Damage2 = avatar.GetCustomStatUnsafe(TextFile.Evasion) * 0.01f;
            float Mult2 = avatar.GetCustomStatUnsafe(TextFile.StrikeBack) * 0.01f;
            return (Damage * Mult + Damage2 * Mult2) * 0.8f;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (CooldownTimer.Update(Time.deltaTime))
            {
                ReInstance();
                LoadInstance();
            }
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
            AddValue = 0;
        }

        private void LoadInstance()
        {
            if (NetworkAvatar)
            {
                float Value = NetworkAvatar.GetCustomStatUnsafe(TextFile.Thorns) * 0.5f;
                if (Value > 0)
                {
                    EffectInstance = StatusDatabase.CreateStatusEntity("STRIKE_BACK", (int)Value);
                    EffectInstance.SetTarget(NetworkAvatar);
                    EffectInstance.ApplyStatus(true);
                    AddValue = Value;
                }
                Value = NetworkAvatar.GetCustomStatUnsafe(TextFile.StrikeBack) * 0.5f;
                Value -= AddValue;
                if (Value > 0)
                {
                    EffectInstance2 = StatusDatabase.CreateStatusEntity("THORNS", (int)Value);
                    EffectInstance2.SetTarget(NetworkAvatar);
                    EffectInstance2.ApplyStatus(true);
                }
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}