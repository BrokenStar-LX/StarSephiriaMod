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
    public class Charm_RemoveArtifact : Charm_Active
    {
        public Timer CooldownTimer = new Timer(3f);
        [SyncVar]
        public bool UseShootBack = false;
        [SyncVar]
        private float SyncPhaseEnd;
        public SyncList<int> RemoveArtifactList = new SyncList<int>();
        public SyncList<string> AssignedCategory = new SyncList<string>();
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            string text = "";
            int Line = 1;
            foreach (int Id in RemoveArtifactList)
            {
                ItemEntity Item = ItemDatabase.FindItemById(Id);
                if (Item != null)
                {
                    text += "\n" + Line.ToString() + "." + Item.Name.ToString();
                    Line += 1;
                }
            }
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Charm", text, Color.yellow),
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
                float ReMain = SyncPhaseEnd - (float)NetworkTime.time;
                if (ReMain > 0)
                {
                    return 1f - Mathf.Clamp01(ReMain / CooldownTimer.time);
                }
                return 0f;
            }
            if (RemoveArtifactList.Count >= 3)
            {
                return 1f;
            }
            if (UseShootBack)
            {
                return 0f;
            }
            return 1f - CooldownTimer.Ratio;
        }

        public override void SaveItemOnServer(ISaveData saveData)
        {
            base.SaveItemOnServer(saveData);
            int Stack = 0;
            foreach (int ID in RemoveArtifactList)
            {
                saveData.SetInt($"CharmSaveData_RemoveArtifactList_{Item.InstanceID}_{Stack}", ID);
                Stack += 1;
            }
            Stack = 0;
            foreach (string ID in AssignedCategory)
            {
                saveData.SetString($"CharmSaveData_RemoveArtifactList2_{Item.InstanceID}_{Stack}", ID);
                Stack += 1;
            }
        }

        public override void LoadItemOnServer(ISaveData saveData)
        {
            base.LoadItemOnServer(saveData);
            List<int> SaveList = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                SaveList.Add(saveData.GetInt($"CharmSaveData_RemoveArtifactList_{Item.InstanceID}_{i}", 0));
            }
            RemoveArtifactList.Clear();
            foreach (var item in SaveList.Where(x => x != 0))
                RemoveArtifactList.Add(item);

            List<string> SaveList2 = new List<string>();
            for (int i = 0; i < 6; i++)
            {
                string Target = saveData.GetString($"CharmSaveData_RemoveArtifactList2_{Item.InstanceID}_{i}", "");
                if (Target == "") break;
                SaveList2.Add(Target);
            }
            AssignedCategory.Clear();
            foreach (var cat in SaveList2)
                AssignedCategory.Add(cat);
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
            StarSephiriaPatches.OnGenerateItemsList += OnGenerateItemsList;
            CooldownTimer.Ratio = 1f;
            UseShootBack = true;
            SyncPhaseEnd = (float)NetworkTime.time;
        }

        private void OnGenerateItemsList(Sephirite GenerateItems, UnitAvatar Player)
        {
            if (Player == NetworkAvatar)
            {
                GenerateItems.appearLimit += RemoveArtifactList.Count;
                foreach (int Id in RemoveArtifactList)
                {
                    if (!GenerateItems.appearedItems.Contains(Id))
                        GenerateItems.appearedItems.Enqueue(Id);
                }
            }
        }

        public override IEnumerable<string> GetItemCategory()
        {
            return AssignedCategory;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            StarSephiriaPatches.OnGenerateItemsList -= OnGenerateItemsList;
            CooldownTimer.Ratio = 1f;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (!isServer || RemoveArtifactList.Count >= 3)
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
            if (!UseShootBack || RemoveArtifactList.Count >= 3) return;

            UseShootBack = false;
            CooldownTimer.Ratio = 0f;

            ItemPosition TargetPosition = new ItemPosition(Item.XIdx, Item.YIdx - 1);
            NewItemOwnInstance TargetNewItem = Inventory.FindItem(TargetPosition);
            if (TargetNewItem == null) return;
            if (TargetNewItem.Entity.type != EItemType.Charm) return;
            if (TargetNewItem.Entity.categories.Count <= 0) return; 

            foreach (string ItemCategories in TargetNewItem.Entity.categories)
            {
                AssignedCategory.Add(ItemCategories);
            }
            RemoveArtifactList.Add(TargetNewItem.Entity.id);

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