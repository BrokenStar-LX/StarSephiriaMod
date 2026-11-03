using Mirror;
using SPMod.Tool;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_CharmStatusInheritance : Charm_StatusInstance, IDependencyConditionCharm
    {
        public Sprite? ActiveIcon = null;
        private bool PrevPositionValid;
        private ItemPosition PrevPosition = new(-1, -1);
        public SyncHashSet<string> AssignedCategory = [];
        private int LastTargetLevelIdx = -1;
        private float NextClientRefresh;
        public Charm_CharmStatusInheritance()
        {
            InitSyncObject(AssignedCategory);
        }

        private void Awake()
        {
            SyncHashSet<string> syncHashSet = AssignedCategory;
            syncHashSet.OnChange = (Action<SyncSet<string>.Operation, string>)Delegate.Combine(syncHashSet.OnChange, new Action<SyncSet<string>.Operation, string>(this.OnAssignedCategoryChanged));
        }

        private new void OnDestroy()
        {
            SyncHashSet<string> syncHashSet = AssignedCategory;
            syncHashSet.OnChange = (Action<SyncSet<string>.Operation, string>)Delegate.Remove(syncHashSet.OnChange, new Action<SyncSet<string>.Operation, string>(this.OnAssignedCategoryChanged));
        }

        private void OnAssignedCategoryChanged(SyncSet<string>.Operation op, string item)
        {
            itemUpdateDirtyBit = true;
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            if (PrevPositionValid)
            {
                Inventory.RemoveCharmDependency(PrevPosition, this);
                PrevPositionValid = false;
            }
            SearchCategory();
        }

        protected override void Update_Client()
        {
            base.Update_Client();
            if (NetworkServer.active) return;
            if (Time.unscaledTime < NextClientRefresh) return;
            NextClientRefresh = Time.unscaledTime + 0.5f;
            RefreshClientStats();
        }

        private void RefreshClientStats()
        {
            if (!Inventory) return;

            int NewMaxLevel = 0;
            List<StatusGroup> Groups = [];
            if (HasValidTarget(out Charm_Basic? Target) && Target is Charm_StatusInstance TargetCharm)
            {
                foreach (StatusGroup AStatus in TargetCharm.stats)
                {
                    if (AStatus == null || NoCopyStat.Contains(AStatus.statusID)) continue;
                    Groups.Add(new StatusGroup { statusID = AStatus.statusID, valuesByLevel = AStatus.valuesByLevel });
                }
                NewMaxLevel = TargetCharm.maxLevel;
            }

            if (maxLevel == NewMaxLevel && SameStats(stats, Groups)) return;

            stats = Groups.ToArray();
            maxLevel = NewMaxLevel;
            itemUpdateDirtyBit = true;
        }

        private static bool SameStats(StatusGroup[] Current, List<StatusGroup> Next)
        {
            if (Current.Length != Next.Count) return false;
            for (int i = 0; i < Current.Length; i++)
            {
                if (Current[i] == null || Current[i].statusID != Next[i].statusID) return false;
                if (Current[i].valuesByLevel != Next[i].valuesByLevel) return false;
            }
            return true;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (!NetworkServer.active) return;
            if (!HasValidTarget(out var Target))
            {
                LastTargetLevelIdx = -1;
                return;
            }
            if (Target is Charm_StatusInstance TargetCharm)
            {
                int currentLvIdx = TargetCharm.CurrentLevelToIdx();
                if (LastTargetLevelIdx != currentLvIdx)
                {
                    LastTargetLevelIdx = currentLvIdx;
                    RefreshCharm();
                }
            }
        }

        public override void OnPreSetEffectRefreshed()
        {
            base.OnPreSetEffectRefreshed();
            SearchCategory();
            RefreshCharm();
        }

        private void SearchCategory()
        {
            AssignedCategory.Clear();
            if (!NetworkAvatar || !Inventory)
            {
                return;
            }
            if (HasValidTarget(out var Target) && Target != null)
            {
                foreach (string text in Target.GetItemCategory())
                {
                    AssignedCategory.Add(text);
                }
            }
        }

        public override IEnumerable<string> GetItemCategory()
        {
            return AssignedCategory;
        }

        public List<string> NoCopyStat =
        [
            "FURY_INVINCIBLE_TIME",
            "DASH_INVINCIBLE_TIME_BONUS"
        ];
        public override void RefreshCharm()
        {
            base.RefreshCharm();
            ItemPosition targetPos = GetTargetPosition();
            if (PrevPositionValid && PrevPosition != targetPos)
            {
                Inventory.RemoveCharmDependency(PrevPosition, this);
                PrevPositionValid = false;
                PrevPosition = new ItemPosition(-1, -1);
            }
            if (!PrevPositionValid)
            {
                Inventory.AddCharmDependency(targetPos, this);
                PrevPosition = targetPos;
                PrevPositionValid = true;
            }

            stats = [];
            maxLevel = 0;
            OnUpdatedLevel(CurrentLevelToIdx(), CurrentLevelToIdx());
            if (HasValidTarget(out var Target))
            {
                if (Target is Charm_StatusInstance TargetCharm)
                {
                    List<StatusGroup> SGLisr = [];
                    foreach (StatusGroup AStatus in TargetCharm.stats.ToList())
                    {
                        if (NoCopyStat.Contains(AStatus.statusID)) continue;
                        SGLisr.Add(new StatusGroup { statusID = AStatus.statusID, valuesByLevel = AStatus.valuesByLevel });
                    }
                    stats = SGLisr.ToArray();
                    maxLevel = TargetCharm.maxLevel;
                    OnUpdatedLevel(CurrentLevelToIdx(), CurrentLevelToIdx());
                }
            }
            SyncMaxLevel();
        }

        private void SyncMaxLevel()
        {
            if (!NetworkServer.active || !Inventory) return;
            ItemPosition Pos = new((int)xIdx, (int)yIdx);
            if (Inventory.maxLevelMatrix.TryGetValue(Pos, out int Value) && Value == maxLevel) return;
            Inventory.maxLevelMatrix[Pos] = maxLevel;
        }

        private ItemPosition GetTargetPosition()
        {
            return new ItemPosition((int)xIdx, yIdx - 1);
        }

        public override bool Weaved()
        {
            return true;
        }

        public bool IsDependencyValid(Charm_Basic request)
        {
            if (request == this)
            {
                return false;
            }
            if (request.Item.Entity.isDual)
            {
                return false;
            }
            if (request is Charm_StatusInstance Status)
            {
                return Status.stats.Count() > 0;
            }
            return false;
        }

        private bool HasValidTarget(out Charm_Basic? Charm)
        {
            if (Inventory == null)
            {
                Charm = null;
                return false;
            }

            NewItemOwnInstance item = Inventory.FindItem(GetTargetPosition());
            if (item == null || item.Charm == null)
            {
                Charm = null;
                return false;
            }

            Charm = item.Charm;
            return IsDependencyValid(item.Charm);
        }

        public override Sprite GetCustomIcon(ItemPosition pos, bool isInstance)
        {
            if (!IsEffectEnabled || !isInstance)
            {
                return base.GetCustomIcon(pos, isInstance);
            }
            if (ActiveIcon == null)
                ActiveIcon = Tools.LoadModSprite($"Item_BluePointer2");
            if (ActiveIcon != null && AssignedCategory.Count > 0)
            {
                return ActiveIcon;
            }
            return base.GetCustomIcon(pos, isInstance);
        }
    }
}