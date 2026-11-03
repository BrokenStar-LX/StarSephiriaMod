using Mirror;
using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_RemovePotion : Charm_Active, IModCooldownReceiver, IModValueReceiver
    {
        public Timer CooldownTimer = new(3f);
        public bool UseShootBack = false;
        private float ClientPhaseEnd = 0f;
        private float NextBroadcastTime = 0f;
        private int PotionStack = 0;
        private readonly Timer CheckTime = new(5f);
        private StatusInstance? EffectInstance = null;
        private bool IsServerSafe => netIdentity != null && isServer;

        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            return
            [
                new("AddValue", "+1%", GetPositiveColor(virtualLevelOffset)),
                new("Stack", $"{PotionStack}%", Color.yellow),
            ];
        }

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
            CooldownTimer.Ratio += ratio;
            if (CooldownTimer.Check())
            {
                UseShootBack = true;
            }
            BroadcastCooldown();
        }

        public void OnClientCooldown(float PhaseEnd)
        {
            ClientPhaseEnd = PhaseEnd;
        }

        public void OnClientValues(int A, int B)
        {
            PotionStack = A;
        }

        private void BroadcastCooldown()
        {
            float PhaseEnd = UseShootBack ? 0f : (float)NetworkTime.time + CooldownTimer.GetRemainingTime();
            ModNetwork.SendCooldown(netId, PhaseEnd);
        }

        private void BroadcastValues()
        {
            ModNetwork.SendValues(netId, PotionStack);
        }

        public override void SaveItemOnServer(ISaveData saveData)
        {
            base.SaveItemOnServer(saveData);
            saveData.SetInt($"CharmSaveData_RemovePotion_{Item.InstanceID}", PotionStack);
        }

        public override void LoadItemOnServer(ISaveData saveData)
        {
            base.LoadItemOnServer(saveData);
            PotionStack = saveData.GetInt($"CharmSaveData_RemovePotion_{Item.InstanceID}", 0);
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            CooldownTimer.Ratio = 1f;
            UseShootBack = true;
            NextBroadcastTime = (float)NetworkTime.time + 2f;
            ReInstance();
            LoadInstance();
            BroadcastCooldown();
            BroadcastValues();
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            CooldownTimer.Ratio = 1f;
            UseShootBack = true;
            ReInstance();
            BroadcastCooldown();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (!IsServerSafe) return;

            if (!UseShootBack && CooldownTimer.Update(Time.deltaTime))
            {
                UseShootBack = true;
                BroadcastCooldown();
            }
            if (PotionStack > 0 && CheckTime.Update(Time.deltaTime))
            {
                ReInstance();
                LoadInstance();
            }
            if (NetworkTime.time >= NextBroadcastTime)
            {
                NextBroadcastTime = (float)NetworkTime.time + 2f;
                BroadcastCooldown();
                BroadcastValues();
            }
        }

        private void ReInstance()
        {
            if (EffectInstance != null)
            {
                EffectInstance.RemoveStatus();
                EffectInstance.ClearTarget();
                EffectInstance = null;
            }
        }

        private void LoadInstance()
        {
            if (!NetworkAvatar || PotionStack <= 0) return;

            EffectInstance = StatusDatabase.CreateStatusEntity("FINAL_DAMAGE", PotionStack);
            EffectInstance.SetTarget(NetworkAvatar);
            EffectInstance.ApplyStatus(true);
        }

        protected override void FireCastingServer(Vector3 aimedPosition, UnitAvatar aimedTarget)
        {
            if (NetworkAvatar is PlayerAvatar player && player.isInDungeon <= 0) return;
            if (!UseShootBack) return;

            if (NetworkAvatar.IsInBattle)
            {
                UseShootBack = false;
                CooldownTimer.Ratio = 0f;
                NextBroadcastTime = (float)NetworkTime.time + 2f;
                BroadcastCooldown();
            }

            ItemPosition TargetPosition = new(Item.XIdx, Item.YIdx - 1);
            NewItemOwnInstance? TargetNewItem = Inventory.FindItem(TargetPosition);
            if (TargetNewItem == null) return;
            if (TargetNewItem.Entity.type != EItemType.Potion) return;

            int Quantity = Mathf.Max(1, TargetNewItem.Quantity);
            int Gain = TargetNewItem.Entity.rarity switch
            {
                EItemRarity.Common => 1,
                EItemRarity.Uncommon => 2,
                EItemRarity.Rare => 4,
                EItemRarity.Legend => 6,
                _ => 0,
            } * Quantity;
            if (Gain > 0)
            {
                NetworkAvatar.AddOrphanedStatusInstance(StatusDatabase.CreateStatusEntity($"DRINK_POTION_STACK/{Gain}"));
            }
            PotionStack += TargetNewItem.Entity.rarity switch
            {
                EItemRarity.Common => 1,
                EItemRarity.Uncommon => 1,
                EItemRarity.Rare => 2,
                EItemRarity.Legend => 2,
                _ => 0,
            } * Quantity;
            ReInstance();
            LoadInstance();
            SaveItemOnServer(SaveManager.CurrentRun);
            BroadcastValues();

            GridInventory inventory = Inventory;
            using (new GridInventory.Permission(inventory))
            {
                inventory.ForceRemoveItem(TargetPosition);
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}
