using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_Caliburn : Charm_Basic, IAttackableCharm
    {
        public float[] DamageByLevel = new float[] { 8, 16, 24, 32, 40 };
        public int NeedMana = 400;
        public bool Deformation = false;
        public bool UseEffect = false;
        public Timer CooldownTimer = new Timer(0.1f);
        private readonly string DamageId = "[Star's Sephiria Mod]石中剑";
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Damage", DamageByLevel.SafeRandomAccess(LevelToIdx(level)).ToString(),  GetPositiveColor(virtualLevelOffset)),
                new Loc.KeywordValue("Max", NeedMana.ToString(),  EffectConstant.MagicColor),
            };
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
            if (NetworkAvatar.MaxMp > NeedMana)
            {
                if (Deformation)
                {
                    return;
                }
                int id = -1;
                if (LoadModContent.GetModItem.TryGetValue("Excalibur", out ItemEntity ModItem))
                {
                    id = ModItem.id;
                    if (NetworkAvatar.Inventory.HasItem(ModItem, out _, out _, out _)) return;
                }
                if (id < 0) return;

                ItemPosition itemPosition = new ItemPosition(Item.XIdx, Item.YIdx);
                GridInventory inventory = Inventory;
                int instanceID = Item.InstanceID;
                using (new GridInventory.Permission(inventory))
                {
                    inventory.ForceRemoveItem(Item.XIdx, Item.YIdx);
                }
                inventory.AddItemAtPosition(new ItemMetadata(instanceID, id, 1), itemPosition, 0, true, false);
                Deformation = true;
            }
        }

        private void AddDamage(UnitAvatar avatar, DamageInstance instance)
        {
            if (!UseEffect) return;
            if (instance.fromType == EDamageFromType.DirectAttack)
            {
                float Damage = CalculateDamage(this);
                var Dam = DamageInstance.GetDamage(NetworkAvatar, DamageId, avatar.transform.position, 4294967295L, Damage, EDamageType.Slice, EDamageFromType.None, Vector2.zero, 0, 0f);
                Dam.elementalType = EDamageElementalType.Physical;
                avatar.ApplyDamage(Dam);
                UseEffect = false;
            }
        }

        public float GetDamage(UnitAvatar avatar)
        {
            float Damage = DamageByLevel.SafeRandomAccess(CurrentLevelToIdx());
            Damage += Tools.BaseStatToMax(avatar) * 0.5f;
            return Damage;
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}