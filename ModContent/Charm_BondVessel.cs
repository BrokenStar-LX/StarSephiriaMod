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
    public class Charm_BondVessel : Charm_Active
    {
        public Timer CooldownTimer = new Timer(3f);
        [SyncVar]
        public bool UseShootBack = false;
        [SyncVar]
        private float SyncPhaseEnd;
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
                    return 1f - Mathf.Clamp01(ReMain / CooldownTimer.time);
                }
                return 0f;
            }
            if (UseShootBack)
            {
                return 0f;
            }
            return 1f - CooldownTimer.Ratio;
        }

        public override void CooldownDecrease(float ratio)
        {
            float Delta = ratio * CooldownTimer.time;
            CooldownTimer.Ratio += ratio;
            SyncPhaseEnd -= Delta;
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            CooldownTimer.Ratio = 1f;
            UseShootBack = true;
            SyncPhaseEnd = (float)NetworkTime.time;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            CooldownTimer.Ratio = 1f;
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