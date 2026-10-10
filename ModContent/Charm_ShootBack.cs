using Mirror;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_ShootBack : Charm_Active
    {
        public Timer CooldownTimer = new Timer(5f);
        public float Effect = 0.3f;
        public float EffectTimer = 0f;
        [SyncVar]
        public bool UseShootBack = false;
        [SyncVar]
        private float SyncPhaseEnd;
        public override float GetCooldownTime(out bool changeByLevel, int level, int virtualLevelOffset)
        {
            changeByLevel = false;
            return CooldownTimer.time;
        }

        public override float GetCooldownRatio()
        {
            if (!isServer)
            {
                float ReMain = SyncPhaseEnd - (float)NetworkTime.time;
                if (ReMain > 0)
                {
                    return 1f - Mathf.Clamp01(ReMain / GetCooldownTime(out _, 0, 0));
                }
                return 0f;
            }
            if (UseShootBack)
            {
                return 0f;
            }
            return 1f - CooldownTimer.Ratio;
        }

        public override void CooldownDecrease(float ratio)
        {
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            NetworkAvatar.OnParry += OnParry;
            CooldownTimer.Ratio = 1f;
            SyncPhaseEnd = (float)NetworkTime.time;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.OnParry -= OnParry;
            CooldownTimer.Ratio = 1f;
        }

        private void OnParry(DamageInstance obj)
        {
            if (EffectTimer > 0)
            {
                float OldRatio = CooldownTimer.Ratio;
                CooldownTimer.Ratio = 0.9f;
                float DeltaSec = (CooldownTimer.Ratio - OldRatio) * CooldownTimer.time;
                SyncPhaseEnd -= DeltaSec;
                EffectTimer = 0f;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (!isServer)
            {
                return;
            }
            if (EffectTimer > 0)
            {
                EffectTimer -= Time.deltaTime;
            }
            if (!UseShootBack && CooldownTimer.Update(Time.deltaTime))
            {
                UseShootBack = true;
            }
        }

        protected override void FireCastingServer(Vector3 aimedPosition, UnitAvatar aimedTarget)
        {
            if (!UseShootBack) return;
            if (WeaponController)
            {
                WeaponController.StartParryInvincible(Effect);
                EffectTimer = Effect;
                CooldownTimer.Ratio = 0f;
                SyncPhaseEnd = (float)(NetworkTime.time + CooldownTimer.time);
                UseShootBack = false;
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}