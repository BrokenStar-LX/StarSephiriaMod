using Mirror;
using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_ShootBack : Charm_Active, IModCooldownReceiver
    {
        public Timer CooldownTimer = new(5f);
        public float Effect = 0.3f;
        public float EffectTimer = 0f;
        public bool UseShootBack = false;
        private float ClientPhaseEnd = 0f;
        private float NextBroadcastTime = 0f;
        public override float GetCooldownTime(out bool changeByLevel, int level, int virtualLevelOffset)
        {
            changeByLevel = false;
            return CooldownTimer.time;
        }

        public override float GetCooldownRatio()
        {
            if (!isServer)
            {
                return ModNetwork.ClientCooldown(ClientPhaseEnd, CooldownTimer.time);
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

        public void OnClientCooldown(float PhaseEnd)
        {
            ClientPhaseEnd = PhaseEnd;
        }

        private void BroadcastCooldown()
        {
            float PhaseEnd = UseShootBack ? 0f : (float)NetworkTime.time + CooldownTimer.GetRemainingTime();
            ModNetwork.SendCooldown(netId, PhaseEnd);
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            NetworkAvatar.OnParry += OnParry;
            CooldownTimer.Ratio = 1f;
            UseShootBack = true;
            NextBroadcastTime = (float)NetworkTime.time + 2f;
            BroadcastCooldown();
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.OnParry -= OnParry;
            CooldownTimer.Ratio = 1f;
            UseShootBack = true;
            BroadcastCooldown();
        }

        private void OnParry(DamageInstance obj)
        {
            if (EffectTimer > 0)
            {
                CooldownTimer.Ratio = 0.9f;
                EffectTimer = 0f;
                BroadcastCooldown();
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
                BroadcastCooldown();
            }
            else if (!UseShootBack && NetworkTime.time >= NextBroadcastTime)
            {
                NextBroadcastTime = (float)NetworkTime.time + 2f;
                BroadcastCooldown();
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
                UseShootBack = false;
                NextBroadcastTime = (float)NetworkTime.time + 2f;
                BroadcastCooldown();
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}