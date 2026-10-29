using HeathenEngineering.SteamworksIntegration.API;
using Mirror;
using SPMod.Tool;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static HeathenEngineering.SteamworksIntegration.ItemShoppingCartManager;

namespace SPMod.ModContent
{
    public class Charm_BondVessel : Charm_Active, IModCooldownReceiver
    {
        public Timer CooldownTimer = new Timer(3f);
        public bool UseShootBack = false;
        private float ClientPhaseEnd = 0f;
        private float NextBroadcastTime = 0f;
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
            if (UseShootBack)
            {
                return 0f;
            }
            return 1f - CooldownTimer.Ratio;
        }

        public override void CooldownDecrease(float ratio)
        {
            CooldownTimer.Ratio += ratio;
            BroadcastCooldown();
        }

        public void OnClientCooldown(float PhaseEnd)
        {
            ClientPhaseEnd = PhaseEnd;
        }

        private void BroadcastCooldown()
        {
            float PhaseEnd = UseShootBack ? 0f : (float)NetworkTime.time + CooldownTimer.GetRemainingTime();
            ModNetwork.SendCooldown(netId, PhaseEnd);
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            CooldownTimer.Ratio = 1f;
            UseShootBack = true;
            NextBroadcastTime = (float)NetworkTime.time + 2f;
            BroadcastCooldown();
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
            if (!isServer)
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
        }

        protected override void FireCastingServer(Vector3 aimedPosition, UnitAvatar aimedTarget)
        {
            if (NetworkAvatar is PlayerAvatar player)
            {
                if (player.isInDungeon <= 0) return;
            }
            if (!UseShootBack) return;

            UseShootBack = false;
            CooldownTimer.Ratio = 0f;
            NextBroadcastTime = (float)NetworkTime.time + 2f;
            BroadcastCooldown();

            List<string> TargetCategories = new List<string>();
            List<ItemPosition> TargetPosition = new List<ItemPosition>()
            {
                new ItemPosition(Item.XIdx + 1, Item.YIdx),
                new ItemPosition(Item.XIdx - 1, Item.YIdx)
            };
            foreach (ItemPosition Pos in TargetPosition)
            {
                NewItemOwnInstance TargetNewItem = Inventory.FindItem(Pos);
                if (TargetNewItem == null) return;
                if (TargetNewItem.Entity.categories.Count <= 0) return;

                foreach (string ItemCategories in TargetNewItem.Entity.categories)
                    TargetCategories.Add(ItemCategories);
            }
            List<int> AddItemList = new List<int>();
            List<int> AllCharmList = new List<int>();
            foreach (PlayerSpawner Spawner in PlayerSpawner.MultiplayerList.ToList())
            {
                if (Spawner.PlayerAvatar != NetworkAvatar) continue;
                AllCharmList = Spawner.unlockedCharms.ToList();
                break;
            }
            foreach (int NewItemList in AllCharmList)
            {
                ItemEntity Item = ItemDatabase.FindItemById(NewItemList);
                if (!Item.isDual) continue;
                if (Item.activeType == EItemActiveType.Hidden) continue;
                if (Inventory.HasItem(Item, out var _, out var _, out var _)) continue;
                if (Item.categories.Intersect(TargetCategories).ToList().Count >= 2)
                    AddItemList.Add(Item.id);
            }
            if (AddItemList.Count <= 0) return;

            GridInventory inventory = Inventory;
            int instanceID = Item.InstanceID;
            ItemPosition itemPosition = new ItemPosition(Item.XIdx, Item.YIdx);
            using (new GridInventory.Permission(inventory))
            {
                foreach (ItemPosition Pos in TargetPosition)
                {
                    inventory.ForceRemoveItem(Pos);
                }
                inventory.ForceRemoveItem(itemPosition);
            }
            inventory.AddItemAtPosition(new ItemMetadata(instanceID, AddItemList.SafeRandomAccess(Random.Range(0, AddItemList.Count)), 1), itemPosition, 0, true, false);
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}