using Mirror;
using SPMod.Tool;
using System.Collections.Generic;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_ModKey : Charm_Active, IModCooldownReceiver, IModValueReceiver
    {
        public int[] MaxByLevel = [2, 4, 6, 8];
        public int ElementalType = 0;
        public Timer CooldownTimer = new(3f);
        public bool UseShootBack = false;
        public List<Sprite> ItemIcons = [];
        private float ClientPhaseEnd = 0f;
        private float NextBroadcastTime = 0f;
        private float NextValueBroadcastTime = 0f;
        private bool IsServerSafe => netIdentity != null && isServer;
        public override float GetCooldownTime(out bool changeByLevel, int level, int virtualLevelOffset)
        {
            changeByLevel = false;
            return CooldownTimer.time;
        }

        public override float GetCooldownRatio()
        {
            if (!IsServerSafe)
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
            if (!IsServerSafe) return;
            CooldownTimer.Ratio += ratio;
            BroadcastCooldown();
        }

        public void OnClientCooldown(float PhaseEnd)
        {
            ClientPhaseEnd = PhaseEnd;
        }

        public void OnClientValues(int A, int B)
        {
            ElementalType = Mathf.Clamp(A, 0, EffectConstant.BaseStatsName.Count - 1);
            RefreshStats();
            itemUpdateDirtyBit = true;
        }

        private void BroadcastCooldown()
        {
            float PhaseEnd = UseShootBack ? 0f : (float)NetworkTime.time + CooldownTimer.GetRemainingTime();
            ModNetwork.SendCooldown(netId, PhaseEnd);
        }

        private void BroadcastValues()
        {
            ModNetwork.SendValues(netId, ElementalType);
        }

        private void RefreshStats()
        {
            string Value = "PHYSICAL_DAMAGE";
            if (Tools.GetKeyWordToStatus(EffectConstant.BaseStatsName.SafeRandomAccess(ElementalType), out string TargetName))
            {
                Value = TargetName;
            }
            stats = [ new StatusGroup() { statusID = Value, valuesByLevel = MaxByLevel } ];
        }

        protected override void OnEnabledEffect()
        {
            RefreshStats();
            base.OnEnabledEffect();
            CooldownTimer.Ratio = 1f;
            UseShootBack = true;
            NextBroadcastTime = (float)NetworkTime.time + 2f;
            NextValueBroadcastTime = (float)NetworkTime.time + 2f;
            BroadcastCooldown();
            BroadcastValues();
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
            if (!IsServerSafe)
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
            if (NetworkTime.time >= NextValueBroadcastTime)
            {
                NextValueBroadcastTime = (float)NetworkTime.time + 2f;
                BroadcastValues();
            }
        }
        public override IEnumerable<string> GetItemCategory()
        {
            string Category = "STURDY";
            switch (ElementalType)
            {
                case 1:
                    Category = "EMBER";
                    break;
                case 2:
                    Category = "GLACIER";
                    break;
                case 3:
                    Category = "MAGITECH";
                    break;
                case 4:
                    Category = "HURRICANE";
                    break;
            }
            return [ Category ];
        }
        public override Sprite GetCustomIcon(ItemPosition pos, bool isIsntance)
        {
            if (!IsEffectEnabled || !isIsntance || ItemIcons.Count <= 0)
            {
                return base.GetCustomIcon(pos, isIsntance);
            }
            return ItemIcons.SafeRandomAccess(ElementalType);
        }

        public override void SaveItemOnServer(ISaveData saveData)
        {
            saveData.SetInt($"CharmSaveData_ModKey_{Item.InstanceID}", ElementalType);
            base.SaveItemOnServer(saveData);
        }

        public override void LoadItemOnServer(ISaveData saveData)
        {
            ElementalType = saveData.GetInt($"CharmSaveData_ModKey_{Item.InstanceID}", 0);
            BroadcastValues();
        }

        protected override void FireCastingServer(Vector3 aimedPosition, UnitAvatar aimedTarget)
        {
            if (!UseShootBack) return;

            if (NetworkAvatar.IsInBattle)
            {
                UseShootBack = false;
                CooldownTimer.Ratio = 0f;
                NextBroadcastTime = (float)NetworkTime.time + 2f;
                BroadcastCooldown();
            }
            ElementalType += 1;
            if (ElementalType >= EffectConstant.BaseStatsName.Count) ElementalType = 0;
            RefreshStats();
            if (IsEffectEnabled) OnUpdatedLevel(CurrentLevelToIdx(), CurrentLevelToIdx());
            NextValueBroadcastTime = (float)NetworkTime.time + 2f;
            BroadcastValues();
            itemUpdateDirtyBit = true;
            if (Inventory != null)
            {
                using (new GridInventory.Permission(Inventory)) { }
            }
        }
        public override bool Weaved()
        {
            return true;
        }
    }
}
