using Mirror;
using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_BodyBomb : Charm_Active, IAttackableCharm
    {
        public float[] MaxByLevel = new float[] { 140, 280, 420, 560, 700 };
        public Timer CooldownTimer = new Timer(1f);
        public GameObject? Boom = null;
        public string DamageId = "[Star's Sephiria Mod]邪祭咒纸";
        [SyncVar]
        public bool UseShootBack = false;
        [SyncVar]
        private float SyncPhaseEnd;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            string text;
            if (!showAllLevel)
            {
                text = MaxByLevel.SafeRandomAccess(LevelToIdx(level)).ToString();
            }
            else
            {
                text = MaxByLevel.SafeRandomAccess(0).ToString() + "→" + MaxByLevel.SafeRandomAccess(maxLevel).ToString();
            }
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Max", text.ToString(), GetPositiveColor(virtualLevelOffset)),
            };
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
                float ReMain = SyncPhaseEnd - (float)NetworkTime.time;
                if (ReMain > 0)
                {
                    return 1f - Mathf.Clamp01(ReMain / CooldownTimer.time);
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
            float Delta = ratio * CooldownTimer.time;
            CooldownTimer.Ratio += ratio;
            SyncPhaseEnd -= Delta;
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
            SyncPhaseEnd = (float)NetworkTime.time;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            CooldownTimer.Ratio = 1f;
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
                    SyncPhaseEnd = (float)(NetworkTime.time + CooldownTimer.time);
                }
            }
        }

        public float GetDamage(UnitAvatar avatar)
        {
            return MaxByLevel.SafeRandomAccess(CurrentLevelToIdx());
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}