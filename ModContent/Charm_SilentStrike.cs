using Mirror;
using SPMod.Tool;
using System;
using System.Collections;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_SilentStrike : Charm_Active, IAttackableCharm
    {
        public float[] MultByLevel = new float[] { 1000, 2000, 3000, 4000, 5000 };
        public Timer CooldownTimer = new Timer(12f);
        public bool StartEffect = false;
        public GameObject? Shoot = null;
        public NewWeaponFireData? EffectData = null;
        public int EvasionHit = 0;
        public string DamageId = "[Star's Sephiria Mod]隐秘斗篷";
        private StatusInstance? MoveSpeedInstance = null;
        private Coroutine? EffectCoroutine = null;
        public float ProgressTime = 0f;
        [SyncVar]
        public bool UseEffect = false;
        [SyncVar]
        private float SyncPhaseEnd;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Damage", CalculateDamage(this).ToString("#0"), EffectConstant.WeaponColor),
                new Loc.KeywordValue("Mult", MultByLevel.SafeRandomAccess(LevelToIdx(level)).ToString() + "%", GetPositiveColor(virtualLevelOffset)),
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
                    if (ReMain <= 3f)
                    {
                        return Mathf.Clamp01(ReMain);
                    }
                    return 1f - Mathf.Clamp01((ReMain - 3f) / CooldownTimer.time);
                }
                return 0f;
            }
            if (UseEffect)
            {
                return 0f;
            }
            if (ProgressTime > 0f)
            {
                return ProgressTime / 3f;
            }
            return 1f - CooldownTimer.Ratio;
        }

        protected override void OnEnabledEffect()
        {
            if (Shoot == null)
            {
                if (Tools.GetPro("KatanaSwing_GreatDraw", out var Pro))
                {
                    Shoot = Pro;
                }
            }
            if (EffectData == null)
            {
                WeaponEntity Weapon = WeaponDatabase.FindWeaponById(409);
                WeaponSimple_Katana Katana = Weapon.mainWeaponPrefab.GetComponent<WeaponSimple_Katana>();
                EffectData = Katana.greatDrawFireData;
            }

            base.OnEnabledEffect();
            NetworkAvatar.OnEvade += OnEvade;
            NetworkAvatar.OnCalculateDamage += OnCalculateDamage;
            NetworkAvatar.OnAttackUnitBeforeOperation += OnAttack;
            CooldownTimer.Ratio = 1f;
            SyncPhaseEnd = (float)NetworkTime.time;
        }

        private void OnAttack(UnitAvatar Target, DamageInstance Damage)
        {
            if (StartEffect) Damage.damage *= 0.3f;
        }

        private void OnCalculateDamage(DamageInstance Damage)
        {
            if (StartEffect)
            {
                Damage.absoluteEvasionPercent += 100f;
                Damage.flag = "UseSpecialEvade";
            }
        }

        private void OnEvade(DamageInstance Damage)
        {
            if (StartEffect)
            {
                EvasionHit += 1;
                NetworkAvatar.StartTempInvulnerable(0.1f);
            }
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            CooldownTimer.Ratio = 1f;
            EndEffect();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (ProgressTime <= 0f)
            {
                if (!UseEffect && CooldownTimer.Update(Time.deltaTime))
                {
                    UseEffect = true;
                }
            }
        }

        protected override void FireCastingServer(Vector3 aimedPosition, UnitAvatar aimedTarget)
        {
            if (!UseEffect) return;

            SyncPhaseEnd = (float)(NetworkTime.time + 3f + CooldownTimer.time);
            EffectCoroutine = StartCoroutine(StealthCoroutine());
            if (UIManager.Instance && NetworkAvatar)
            {
                UIManager.Pool.Spawn<UI_DamageParticle>(NetworkAvatar.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle).SetDamage("Stealth!", EffectConstant.DefenseColor);
            }
            UseEffect = false;
        }

        private void EndEffect()
        {
            if (EffectCoroutine == null) return;

            if (MoveSpeedInstance != null)
            {
                MoveSpeedInstance.RemoveStatus();
                MoveSpeedInstance.ClearTarget();
            }
            StartEffect = false;
            EvasionHit = 0;
            ProgressTime = 0f;
            EffectCoroutine = null;
            SyncPhaseEnd = (float)NetworkTime.time;
        }

        private IEnumerator StealthCoroutine()
        {
            MoveSpeedInstance ??= StatusDatabase.CreateStatusEntity("MOVE_SPEED", 100);

            MoveSpeedInstance.SetTarget(NetworkAvatar);
            MoveSpeedInstance.ApplyStatus(true);

            StartEffect = true;
            while (ProgressTime < 3f)
            {
                float AddTime = Time.deltaTime;
                ProgressTime += AddTime;
                if (EvasionHit >= 10) break;
                yield return null;
            }
            MoveSpeedInstance.RemoveStatus();
            MoveSpeedInstance.ClearTarget();

            StartEffect = false;
            WeaponController?.StartParryInvincible(0.5f);
            yield return new WaitForSeconds(0.1f);

            Vector3 position = NetworkAvatar.transform.position;
            float Damage = CalculateDamage(this);
            MeleeCollision.Pool.Spawn(Shoot, position, true, EDamageFromType.None, DamageId, Damage, 25, 2f, NetworkAvatar, NetworkAvatar.GetHostileFactionLayers(EDamageFromType.None), 0.3f, position, position, null, null, 0f, EDamageElementalType.Physical, false);
            if (UIManager.Instance && NetworkAvatar)
            {
                UIManager.Pool.Spawn<UI_DamageParticle>(NetworkAvatar.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle).SetDamage("Sakuragi!", EffectConstant.DefenseColor);
            }
            RpcPlaySwingFx();
            EvasionHit = 0;
            ProgressTime = 0f;
            yield break;
        }

        [ClientRpc]
        void RpcPlaySwingFx()
        {
            if (EffectData == null)
            {
                WeaponEntity Weapon = WeaponDatabase.FindWeaponById(409);
                WeaponSimple_Katana Katana = Weapon.mainWeaponPrefab.GetComponent<WeaponSimple_Katana>();
                EffectData = Katana.greatDrawFireData;
            }
            WeaponController?.currentWeapon?.CreateBasicAttackSwingFxFullyManual(EffectData);
        }

        public float GetDamage(UnitAvatar avatar)
        {
            float Damage = avatar.GetCustomStatUnsafe(TextFile.Evasion) * 0.01f;
            float Mult = MultByLevel.SafeRandomAccess(CurrentLevelToIdx()) * 0.01f;
            return Damage * Mult;
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}