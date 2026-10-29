using Mirror;
using SPMod.Tool;
using System;
using System.Collections;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_SilentStrike : Charm_Active, IAttackableCharm, IModCastStateReceiver
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
        public bool UseEffect = false;
        private float ServerCastTime = 0f;
        private float ClientCastTime = 0f;
        private float NextBroadcastTime = 0f;
        private const byte EventState = 0;
        private const byte EventCast = 1;
        private const byte EventStrike = 2;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            string text = "0";
            if (IsEffectEnabled)
                text = CalculateDamage(this).ToString("#0");
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Damage", text, EffectConstant.WeaponColor),
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
                if (ClientCastTime <= 0f) return 0f;
                float Elapsed = (float)NetworkTime.time - ClientCastTime;
                if (Elapsed < 3f) return Mathf.Clamp01(Elapsed / 3f);
                if (Elapsed < 3f + CooldownTimer.time)
                    return Mathf.Clamp01(1f - (Elapsed - 3f) / CooldownTimer.time);
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
            ServerCastTime = 0f;
            BroadcastState(EventState);
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
            ServerCastTime = 0f;
            BroadcastState(EventState);
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

            if (ServerCastTime > 0f)
            {
                float Now = (float)NetworkTime.time;
                if (Now - ServerCastTime >= 3f + CooldownTimer.time)
                {
                    ServerCastTime = 0f;
                    BroadcastState(EventState);
                }
                else if (Now >= NextBroadcastTime)
                {
                    NextBroadcastTime = Now + 2f;
                    BroadcastState(EventState);
                }
            }
        }

        protected override void FireCastingServer(Vector3 aimedPosition, UnitAvatar aimedTarget)
        {
            if (!UseEffect) return;

            ServerCastTime = (float)NetworkTime.time;
            NextBroadcastTime = ServerCastTime + 2f;
            EffectCoroutine = StartCoroutine(StealthCoroutine());
            BroadcastState(EventCast);
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
            if (isClient) PlaySwingFx();
            BroadcastState(EventStrike);
            EvasionHit = 0;
            ProgressTime = 0f;
            yield break;
        }

        private void PlaySwingFx()
        {
            if (EffectData == null)
            {
                WeaponEntity Weapon = WeaponDatabase.FindWeaponById(409);
                WeaponSimple_Katana Katana = Weapon.mainWeaponPrefab.GetComponent<WeaponSimple_Katana>();
                EffectData = Katana.greatDrawFireData;
            }
            WeaponControllerSimple? Controller = WeaponController;
            if (Controller == null && NetworkAvatar != null)
                Controller = NetworkAvatar.GetComponent<WeaponControllerSimple>();
            Controller?.currentWeapon?.CreateBasicAttackSwingFxFullyManual(EffectData);
        }

        public void OnClientCastState(float CastTime, byte Event)
        {
            ClientCastTime = CastTime;
            if (isServer) return;

            if (Event == EventCast)
            {
                if (UIManager.Instance && NetworkAvatar)
                    UIManager.Pool.Spawn<UI_DamageParticle>(NetworkAvatar.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle).SetDamage("Stealth!", EffectConstant.DefenseColor);
            }
            else if (Event == EventStrike)
            {
                PlaySwingFx();
                if (UIManager.Instance && NetworkAvatar)
                    UIManager.Pool.Spawn<UI_DamageParticle>(NetworkAvatar.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle).SetDamage("Sakuragi!", EffectConstant.DefenseColor);
            }
        }

        private void BroadcastState(byte Event)
        {
            ModNetwork.SendCastState(netId, ServerCastTime, Event);
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