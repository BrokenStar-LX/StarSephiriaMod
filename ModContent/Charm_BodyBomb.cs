using Mirror;
using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_BodyBomb : Charm_Active, IAttackableCharm, IModCooldownReceiver
    {
        public float[] MaxByLevel = [90, 180, 270, 360, 450];
        public Timer CooldownTimer = new(1f);
        public GameObject? Boom = null;
        public string DamageId = "[Star's Sephiria Mod]UnhallowedRitePaper";
        public bool UseShootBack = false;
        private float ClientPhaseEnd = 0f;
        private float NextBroadcastTime = 0f;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            float text = MaxByLevel.SafeRandomAccess(LevelToIdx(level));
            return
            [
                new("Max", text.ToString("#0"), GetPositiveColor(virtualLevelOffset)),
            ];
        }

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
            CooldownTimer.Ratio += ratio;
            BroadcastCooldown();
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
            if(Boom == null)
            {
                if (Tools.GetPro("CrossbowMine_DroneEnhanced", out var Pro))
                {
                    Boom = Pro;
                }
            }
            base.OnEnabledEffect();
            CooldownTimer.Ratio = 1f;
            UseShootBack = true;
            NextBroadcastTime = (float)NetworkTime.time + 2f;
            BroadcastCooldown();
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            CooldownTimer.Ratio = 1f;
            UseShootBack = true;
            BroadcastCooldown();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (!isServer)
            {
                return;
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

            UnitAvatar? MaxLifeFollower = null;
            foreach (UnitAvatar Follower in NetworkAvatar.followers)
            {
                if (MaxLifeFollower == null)
                    MaxLifeFollower = Follower;
                else if (Follower.hp > MaxLifeFollower.hp)
                    MaxLifeFollower = Follower;
            }
            if (MaxLifeFollower != null)
            {
                if (Boom != null)
                {
                    Vector3 Pos = MaxLifeFollower.transform.position;
                    Bullet a = Bullet.Pool.Spawn(Boom, Pos, true, EDamageFromType.None, DamageId, CalculateDamage(this), 0, 0f, NetworkAvatar,
                        NetworkAvatar.GetHostileFactionLayers(EDamageFromType.None), 0.3f, Pos, Pos, null, null, 0f, EDamageElementalType.Fire);
                    a.Destroy();

                    var Dam = DamageInstance.GetDamage(null, DamageId, MaxLifeFollower.transform.position, 4294967295L, MaxByLevel.SafeRandomAccess(CurrentLevelToIdx()), EDamageType.Slice, EDamageFromType.None, Vector2.zero, 0, 0f);
                    MaxLifeFollower.ApplyDamage(Dam);

                    CooldownTimer.Ratio = 0f;
                    UseShootBack = false;
                    NextBroadcastTime = (float)NetworkTime.time + 2f;
                    BroadcastCooldown();
                }
            }
        }

        public float GetDamage(UnitAvatar avatar)
        {
            float Value = MaxByLevel.SafeRandomAccess(CurrentLevelToIdx());
            Value *= 1f + avatar.GetCustomStatUnsafe(TextFile.FollowerDamage) * 0.01f;
            return Value;
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}