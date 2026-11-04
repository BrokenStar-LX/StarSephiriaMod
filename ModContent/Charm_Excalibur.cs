using Mirror;
using SPMod.Tool;
using System;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_Excalibur : Charm_Active, IAttackableCharm, IModCooldownReceiver, IModEventReceiver
    {
        public float[] MultByLevel = [200, 400, 600, 800, 1000];
        public Timer CooldownTimer = new(4f);
        public Timer CooldownTimer2 = new(0.1f);
        public GameObject? Shoot = null;
        public NewWeaponFireData? EffectData = null;
        public string DamageId = "[Star's Sephiria Mod]Excalibur";
        public bool UseAddTimer = false;
        public bool UseEffect = false;
        private float ClientPhaseEnd = 0f;
        private float NextBroadcastTime = 0f;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            string text = "0";
            if (IsEffectEnabled)
                text = CalculateDamage(this).ToString("#0");
            return
            [
                new("Damage", text, EffectConstant.WeaponColor),
                new("Mult", MultByLevel.SafeRandomAccess(LevelToIdx(level)).ToString() + "%", GetPositiveColor(virtualLevelOffset)),
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
            if (UseEffect)
            {
                return 0f;
            }
            return 1f - CooldownTimer.Ratio;
        }

        public override void CooldownDecrease(float ratio)
        {
            if (!isServer) return;

            CooldownTimer.Ratio += ratio;
            BroadcastCooldown();
        }

        public void OnClientCooldown(float PhaseEnd)
        {
            ClientPhaseEnd = PhaseEnd;
        }

        private void BroadcastCooldown()
        {
            float PhaseEnd = UseEffect ? 0f : (float)NetworkTime.time + CooldownTimer.GetRemainingTime();
            ModNetwork.SendCooldown(netId, PhaseEnd);
        }

        protected override void OnEnabledEffect()
        {
            if (Shoot == null)
            {
                if (Tools.GetPro("KatanaSwing_GreatDraw_Iaido", out var Pro))
                {
                    Shoot = Pro;
                }
            }
            if (EffectData == null)
            {
                WeaponEntity Weapon = WeaponDatabase.FindWeaponById(WeaponIDs.RendbladeDriftingWillow);
                WeaponSimple_Katana Katana = Weapon.mainWeaponPrefab.GetComponent<WeaponSimple_Katana>();
                EffectData = Katana.greatDrawFireData;
            }
            base.OnEnabledEffect();
            NetworkAvatar.OnAttackUnit += AddDamage;
            CooldownTimer.Ratio = 0f;
            UseEffect = true;
            NextBroadcastTime = (float)NetworkTime.time + 2f;
            BroadcastCooldown();
        }

        private void AddDamage(UnitAvatar Target, DamageInstance Damage)
        {
            if (!isServer) return;
            if (!UseAddTimer) return;
            if (Damage.fromType == EDamageFromType.DirectAttack)
            {
                CooldownDecrease(1f / GetCooldownTime(out _, CurrentLevelToIdx(), 0) * 0.1f);
                UseAddTimer = false;
            }
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.OnAttackUnit -= AddDamage;
            CooldownTimer.Ratio = 0f;
            UseEffect = true;
            BroadcastCooldown();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (!UseAddTimer)
            {
                if (CooldownTimer2.Update(Time.deltaTime))
                    UseAddTimer = true;
            }
            if (!UseEffect && CooldownTimer.Update(Time.deltaTime))
            {
                UseEffect = true;
                BroadcastCooldown();
            }
            else if (!UseEffect && NetworkTime.time >= NextBroadcastTime)
            {
                NextBroadcastTime = (float)NetworkTime.time + 2f;
                BroadcastCooldown();
            }
        }

        protected override void FireCastingServer(Vector3 aimedPosition, UnitAvatar aimedTarget)
        {
            if (!UseEffect) return;

            int UseMP = Math.Max(1, NetworkAvatar.MP.IntMult(0.2f));
            NetworkAvatar.UseMp(UseMP);

            Vector3 position = NetworkAvatar.transform.position;
            float Damage = CalculateDamage(this);
            MeleeCollision.Pool.Spawn(Shoot, position, true, EDamageFromType.None, DamageId, Damage, 25, 2f, NetworkAvatar, NetworkAvatar.GetHostileFactionLayers(EDamageFromType.None), 0.3f, position, position, null, null, 0f, EDamageElementalType.Chaos, false);

            if (isClient) PlaySwingFx();
            ModNetwork.SendEvent(netId, 1, NetworkAvatar.transform.position);

            UseEffect = false;
            NextBroadcastTime = (float)NetworkTime.time + 2f;
            BroadcastCooldown();
        }

        private void PlaySwingFx()
        {
            if (EffectData == null)
            {
                WeaponEntity Weapon = WeaponDatabase.FindWeaponById(WeaponIDs.RendbladeDriftingWillow);
                WeaponSimple_Katana Katana = Weapon.mainWeaponPrefab.GetComponent<WeaponSimple_Katana>();
                EffectData = Katana.greatDrawFireData;
            }
            // WeaponController 只在服务端 Connect 里赋值，客机要用 NetworkAvatar 现取
            WeaponControllerSimple? Controller = WeaponController;
            if (Controller == null && NetworkAvatar != null)
                Controller = NetworkAvatar.GetComponent<WeaponControllerSimple>();
            Controller?.currentWeapon?.CreateBasicAttackSwingFxFullyManual(EffectData);
        }

        public void OnClientEvent(byte Event, Vector3 Pos, string S1, string S2)
        {
            if (isServer) return;
            if (Event == 1) PlaySwingFx();
        }

        public float GetDamage(UnitAvatar avatar)
        {
            float Damage = 200 + Tools.BaseStatToMax(avatar) * MultByLevel.SafeRandomAccess(CurrentLevelToIdx()) * 0.01f;
            Damage *= 1f + avatar.GetCustomStatUnsafe(TextFile.MPSkillDamage) * 0.01f;
            Damage *= 1f + (avatar.MaxMp - avatar.MP) * 0.005f;
            return Damage;
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}