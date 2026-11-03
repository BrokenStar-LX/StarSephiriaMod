using Mirror;
using SPMod.Tool;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_EnhancedCompass : Charm_Basic, IDependencyConditionCharm
    {
        private Charm_Basic? BuffedTarget;
        private ItemPosition BuffedPos = new(-1, -1);
        private Charm_Basic? ClientBuffedTarget;
        private ItemPosition ClientBuffedPos = new(-1, -1);
        private float NextClientSync;
        public Sprite? ActiveIcon = null;
        public SyncHashSet<string> AssignedCategory = [];
        public int DownCharmDamage = 50;

        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            return
            [
                new("Down", DownCharmDamage.ToString() + "%", Color.red)
            ];
        }

        public Charm_EnhancedCompass()
        {
            InitSyncObject(AssignedCategory);
        }

        private void Awake()
        {
            AssignedCategory.OnChange += OnAssignedCategoryChanged;
        }

        private void OnAssignedCategoryChanged(SyncSet<string>.Operation op, string item)
        {
            itemUpdateDirtyBit = true;
        }

        public override void RefreshCharm()
        {
            base.RefreshCharm();
            SyncBuff();
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            SyncBuff();
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            RestoreBuff();
            SearchCategory();
        }

        protected override void OnDisconnected()
        {
            base.OnDisconnected();
            RestoreBuff();
        }

        protected override void OnDestroy()
        {
            AssignedCategory.OnChange -= OnAssignedCategoryChanged;
            RestoreBuff();
            RestoreClientBuff();
            base.OnDestroy();
        }

        protected override void Update_Client()
        {
            base.Update_Client();
            if (NetworkServer.active) return;
            if (Time.unscaledTime < NextClientSync) return;
            NextClientSync = Time.unscaledTime + 0.5f;
            SyncBuffForDisplay();
        }

        public bool IsDependencyValid(Charm_Basic request)
        {
            if (request == this) return false;
            if (request is IAttackableCharm Attackable)
                return request is not Charm_Magic && Attackable.IsAttackableCharm() && request.maxLevel > 0;
            return false;
        }

        private ItemPosition GetTargetPosition()
        {
            return new ItemPosition((int)xIdx, yIdx + 1);
        }

        private bool TryGetTarget(out Charm_Basic? Target, out ItemPosition Position)
        {
            Target = null;
            Position = new ItemPosition(-1, -1);
            if (NetworkAvatar == null || Inventory == null) return false;

            NewItemOwnInstance? Item = Inventory.FindItem(GetTargetPosition());
            if (Item == null || Item.Charm == null) return false;
            if (Item.Charm is Charm_Magic) return false;
            if (Item.Charm is not IAttackableCharm Attackable || !Attackable.IsAttackableCharm()) return false;

            Target = Item.Charm;
            Position = Item.Position;
            return true;
        }

        public override void OnPreSetEffectRefreshed()
        {
            base.OnPreSetEffectRefreshed();
            SearchCategory();
        }

        private void SearchCategory()
        {
            AssignedCategory.Clear();
            if (!IsEffectEnabled || !NetworkAvatar || !Inventory) return;
            if (TryGetTarget(out Charm_Basic? Target, out _) && Target != null)
            {
                foreach (string Category in Target.GetItemCategory())
                    AssignedCategory.Add(Category);
            }
        }

        public override IEnumerable<string> GetItemCategory()
        {
            return AssignedCategory;
        }

        public override Sprite GetCustomIcon(ItemPosition pos, bool isInstance)
        {
            if (!IsEffectEnabled || !isInstance)
                return base.GetCustomIcon(pos, isInstance);

            if (ActiveIcon == null)
                ActiveIcon = Tools.LoadModSprite("Item_EnhancedCompass2");

            if (ActiveIcon != null && TryGetTarget(out _, out _))
                return ActiveIcon;

            return base.GetCustomIcon(pos, isInstance);
        }

        protected override int OnRequestCharmDamageBonus(Charm_Basic rootCharm, ref CharmDamageBonusContext context)
        {
            if (!IsEffectEnabled || rootCharm == null || BuffedTarget == null) return 0;
            if (rootCharm != BuffedTarget || !CharmLevelBoost.IsApplied(rootCharm)) return 0;
            return -DownCharmDamage;
        }

        private void SyncBuff()
        {
            if (!NetworkServer.active || NetworkAvatar == null) return;

            if (!IsEffectEnabled || !TryGetTarget(out Charm_Basic? Target, out ItemPosition Position))
            {
                RestoreBuff();
                return;
            }

            if (BuffedTarget != Target)
            {
                RestoreBuff();
                CharmLevelBoost.Apply(Target!);
                BuffedTarget = Target;
                BuffedPos = Position;
                Inventory!.AddCharmDependency(Position, this);
                Inventory!.maxLevelMatrix[Position] = Target!.maxLevel;
                Target!.RefreshCharm();
                CharmLevelBoost.RefreshEffectLevel(Target!);
                return;
            }

            if (BuffedPos != Position)
            {
                Inventory!.RemoveCharmDependency(BuffedPos, this);
                Inventory!.AddCharmDependency(Position, this);
                BuffedPos = Position;
                Inventory!.maxLevelMatrix[Position] = Target!.maxLevel;
            }
        }

        private void RestoreBuff()
        {
            if (BuffedTarget is null) return;

            Charm_Basic Target = BuffedTarget;
            ItemPosition Position = BuffedPos;
            BuffedTarget = null;
            BuffedPos = new ItemPosition(-1, -1);

            GridInventory? Inv = Inventory;
            NewItemOwnInstance? Item = Inv != null && Position.x >= 0 ? Inv.FindItem(Position) : null;
            bool StillThere = Item != null && Item.Charm != null && Item.Charm == Target && Target != null;
            bool SelfRemoved = Inv != null && !(Inv.charms.TryGetValue(new ItemPosition(xIdx, yIdx), out Charm_Basic? Self) && Self == this);

            if (Inv != null && Position.x >= 0)
                Inv.RemoveCharmDependency(Position, this);

            CharmLevelBoost.Restore(Target!);
            CharmLevelBoost.RefreshEffectLevel(Target!);

            if (Inv != null && Position.x >= 0)
            {
                if (!StillThere)
                    Inv.maxLevelMatrix[Position] = -1;
                else if (!SelfRemoved)
                    Inv.maxLevelMatrix[Position] = Target!.maxLevel;
            }
        }

        private void SyncBuffForDisplay()
        {
            if (NetworkAvatar == null || Inventory == null) return;

            if (!IsEffectEnabled || !TryGetTarget(out Charm_Basic? Target, out ItemPosition Position))
            {
                RestoreClientBuff();
                return;
            }

            if (ClientBuffedTarget != Target)
            {
                RestoreClientBuff();
                CharmLevelBoost.Apply(Target!);
                ClientBuffedTarget = Target;
                ClientBuffedPos = Position;
            }
            else if (ClientBuffedPos != Position)
            {
                ClientBuffedPos = Position;
            }
        }

        private void RestoreClientBuff()
        {
            if (ClientBuffedTarget is null) return;

            Charm_Basic Target = ClientBuffedTarget;
            ClientBuffedTarget = null;
            ClientBuffedPos = new ItemPosition(-1, -1);
            CharmLevelBoost.Restore(Target);
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}
