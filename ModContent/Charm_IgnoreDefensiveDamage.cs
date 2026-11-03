using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_IgnoreDefensiveDamage : Charm_MinHPKill, IAttackableCharm
    {
        private readonly string DamageId = "[Star's Sephiria Mod]白蛋壳";
        public float[] MaxByLevel = [400, 800, 1200, 1600, 2000];
        public Timer CooldownTimer = new(1.5f);
        public bool UseEffect = false;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            string text = MaxByLevel.SafeRandomAccess(LevelToIdx(level)).ToString();
            string text2 = "0";
            if (IsEffectEnabled)
                text2 = CalculateDamage(this).ToString("#0");
            return
            [
                new("Mult", text + "%", GetPositiveColor(virtualLevelOffset)),
                new("Damage", text2,  EffectConstant.WeaponColor),
            ];
        }
        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            if (killFxPrefab == null)
            {
                ItemEntity Item = ItemDatabase.FindItemById(1118);
                if (Item.resourcePrefab.TryGetComponent<Charm_MinHPKill>(out var CharmEffect))
                    killFxPrefab = CharmEffect.killFxPrefab;
            }
            NetworkAvatar.OnAttackUnit += AddDamage;
            minHPPercentByLevel = [-999f];
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.OnAttackUnit -= AddDamage;
            minHPPercentByLevel = [-999f];
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (!isServer)
            {
                return;
            }
            if (!UseEffect && CooldownTimer.Update(Time.deltaTime))
            {
                UseEffect = true;
            }
        }

        private void AddDamage(UnitAvatar avatar, DamageInstance instance)
        {
            if (UseEffect && instance.fromType == EDamageFromType.DirectAttack && instance.flag != DamageId)
            {
                float Damage = CalculateDamage(this);
                var Dam = DamageInstance.GetDamage(NetworkAvatar, DamageId, avatar.transform.position, 4294967295L, Damage, EDamageType.Slice, EDamageFromType.None, Vector2.zero, 0, 0f);
                Dam.elementalType = EDamageElementalType.Normal;
                Dam.ignoreDefense = 100;
                Dam.criticalChancePercent = -99999;
                Dam.flag = DamageId;
                Dam.SetElementalTypeToNormal(true, new Color(0.647f, 0.458f, 1f, 1f));
                avatar.ApplyDamage(Dam);
                RpcCreateKillFx(avatar.transform.position);
                if (Dam.damageResult < avatar.hp)
                {
                    UseEffect = false;
                    CooldownTimer.Ratio = 0f;
                }
            }
        }

        public float GetDamage(UnitAvatar avatar)
        {
            int Add = avatar.GetCustomStatUnsafe(TextFile.TrueDamage);
            float Damage = Add * MaxByLevel.SafeRandomAccess(CurrentLevelToIdx()) * 0.01f;
            return Mathf.Max(0, Damage);
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}