using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_Convergence : Charm_CustomStatus, IAttackableCharm
    {
        public float[] DamageByLevel = [16, 32, 48];
        public bool UseEffect = false;
        public Timer CooldownTimer = new(0.8f);
        private readonly string DamageId = "[Star's Sephiria Mod]Convergence";
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            float Value = GetChance(avatar) * 100f;
            return
            [
                new("Damage", DamageByLevel.SafeRandomAccess(LevelToIdx(level)).ToString(),  GetPositiveColor(virtualLevelOffset)),
                new("Value", Value.ToString("#0.0") + "%", Color.yellow),
            ];
        }
        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            NetworkAvatar.OnAttackUnit += AddDamage;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.OnAttackUnit -= AddDamage;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (!UseEffect)
            {
                if (CooldownTimer.Update(Time.deltaTime))
                    UseEffect = true;
            }
        }

        private float GetChance(UnitAvatar avatar)
        {
            if (avatar == null) return 0f;
            if (avatar.TryGetComponent<WeaponControllerSimple>(out var Controller))
            {
                if (Controller.currentWeapon != null)
                {
                    return Controller.currentWeapon.AttackWeightPerSwing * 0.75f;
                }
            }
            return 0f;
        }

        private void AddDamage(UnitAvatar avatar, DamageInstance instance)
        {
            if (instance.fromType == EDamageFromType.DirectAttack)
            {
                float Damage = CalculateDamage(this);
                var Dam = DamageInstance.GetDamage(NetworkAvatar, DamageId, avatar.transform.position, 4294967295L, Damage, EDamageType.Slice, EDamageFromType.None, Vector2.zero, 0, 0f);
                Dam.elementalType = EffectConstant.Wind;
                Dam.criticalChancePercent = -99999;
                avatar.ApplyDamage(Dam);

                if (GetChance(NetworkAvatar) * 100f > Random.Range(0, 100))
                {
                    if (!UseEffect) return;
                    UseEffect = false;
                    CooldownTimer.Ratio = 0f;
                    avatar.ApplyDebuff(LoadModContent.WindErosion.Prefab, NetworkAvatar);
                }
            }
        }

        public float GetDamage(UnitAvatar avatar)
        {
            float Damage = DamageByLevel.SafeRandomAccess(CurrentLevelToIdx());
            return Damage;
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}