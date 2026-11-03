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
    public class Charm_RemoveArtifact : Charm_Active, IModCooldownReceiver, IModEventReceiver
    {
        public Timer CooldownTimer = new(3f);
        public bool UseShootBack = false;
        private float ClientPhaseEnd = 0f;
        private float NextBroadcastTime = 0f;
        public SyncList<int> RemoveArtifactList = [];
        public SyncList<string> AssignedCategory = [];
        private readonly List<int> ClientRemoveArtifactList = [];
        private readonly List<string> ClientAssignedCategory = [];
        private bool IsServerSafe => netIdentity != null && isServer;
        private int RemoveArtifactCount => IsServerSafe ? RemoveArtifactList.Count : ClientRemoveArtifactList.Count;
        private IEnumerable<int> DisplayArtifactList => IsServerSafe ? (IEnumerable<int>)RemoveArtifactList : ClientRemoveArtifactList;
        private IEnumerable<string> DisplayCategoryList => IsServerSafe ? (IEnumerable<string>)AssignedCategory : ClientAssignedCategory;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            string text = "";
            int Line = 1;
            foreach (int Id in DisplayArtifactList)
            {
                ItemEntity Item = ItemDatabase.FindItemById(Id);
                if (Item != null)
                {
                    text += "\n" + Line.ToString() + "." + Item.Name.ToString();
                    Line += 1;
                }
            }
            return
            [
                new("Charm", text, Color.yellow),
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
                if (RemoveArtifactCount >= 3) return 1f;
                return ModNetwork.ClientCooldown(ClientPhaseEnd, CooldownTimer.time);
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

        public void OnClientCooldown(float PhaseEnd)
        {
            ClientPhaseEnd = PhaseEnd;
        }

        public void OnClientEvent(byte Event, Vector3 Pos, string S1, string S2)
        {
            if (IsServerSafe) return;
            if (Event != 1) return;

            ClientRemoveArtifactList.Clear();
            if (!string.IsNullOrEmpty(S1))
            {
                foreach (string Id in S1.Split(','))
                {
                    if (int.TryParse(Id, out int Value))
                        ClientRemoveArtifactList.Add(Value);
                }
            }
            ClientAssignedCategory.Clear();
            if (!string.IsNullOrEmpty(S2))
            {
                foreach (string Category in S2.Split('\n'))
                    ClientAssignedCategory.Add(Category);
            }
        }

        private void BroadcastCooldown()
        {
            float PhaseEnd;
            if (RemoveArtifactList.Count >= 3) PhaseEnd = float.PositiveInfinity;
            else if (UseShootBack) PhaseEnd = 0f;
            else PhaseEnd = (float)NetworkTime.time + CooldownTimer.GetRemainingTime();
            ModNetwork.SendCooldown(netId, PhaseEnd);
        }

        private void BroadcastList()
        {
            string Ids = string.Join(",", RemoveArtifactList);
            string Categories = string.Join("\n", AssignedCategory);
            ModNetwork.SendEvent(netId, 1, NetworkAvatar ? NetworkAvatar.transform.position : Vector3.zero, Ids, Categories);
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
            List<int> SaveList = [];
            for (int i = 0; i < 3; i++)
            {
                SaveList.Add(saveData.GetInt($"CharmSaveData_RemoveArtifactList_{Item.InstanceID}_{i}", 0));
            }
            RemoveArtifactList.Clear();
            foreach (var item in SaveList.Where(x => x != 0))
                RemoveArtifactList.Add(item);

            List<string> SaveList2 = [];
            for (int i = 0; i < 6; i++)
            {
                string Target = saveData.GetString($"CharmSaveData_RemoveArtifactList2_{Item.InstanceID}_{i}", "");
                if (Target == "") break;
                SaveList2.Add(Target);
            }
            AssignedCategory.Clear();
            foreach (var cat in SaveList2)
                AssignedCategory.Add(cat);
            BroadcastList();
        }

        public override void CooldownDecrease(float ratio)
        {
            CooldownTimer.Ratio += ratio;
            BroadcastCooldown();
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            StarSephiriaPatches.OnGenerateItemsList += OnGenerateItemsList;
            CooldownTimer.Ratio = 1f;
            UseShootBack = true;
            NextBroadcastTime = (float)NetworkTime.time + 2f;
            BroadcastCooldown();
            BroadcastList();
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
            return DisplayCategoryList;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            StarSephiriaPatches.OnGenerateItemsList -= OnGenerateItemsList;
            CooldownTimer.Ratio = 1f;
            UseShootBack = true;
            BroadcastCooldown();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (!IsServerSafe) return;
            if (RemoveArtifactList.Count >= 3)
            {
                if (NetworkTime.time >= NextBroadcastTime)
                {
                    NextBroadcastTime = (float)NetworkTime.time + 2f;
                    BroadcastCooldown();
                    BroadcastList();
                }
                return;
            }
            if (!UseShootBack && CooldownTimer.Update(Time.deltaTime))
            {
                UseShootBack = true;
                BroadcastCooldown();
            }
            if (NetworkTime.time >= NextBroadcastTime)
            {
                NextBroadcastTime = (float)NetworkTime.time + 2f;
                BroadcastCooldown();
                BroadcastList();
            }
        }

        protected override void FireCastingServer(Vector3 aimedPosition, UnitAvatar aimedTarget)
        {
            if (NetworkAvatar is PlayerAvatar player)
            {
                if (player.isInDungeon <= 0) return;
            }
            if (!UseShootBack || RemoveArtifactList.Count >= 3) return;

            UseShootBack = false;
            CooldownTimer.Ratio = 0f;
            NextBroadcastTime = (float)NetworkTime.time + 2f;
            BroadcastCooldown();

            ItemPosition TargetPosition = new(Item.XIdx, Item.YIdx - 1);
            NewItemOwnInstance TargetNewItem = Inventory.FindItem(TargetPosition);
            if (TargetNewItem == null) return;
            if (TargetNewItem.Entity.type != EItemType.Charm) return;
            if (TargetNewItem.Entity.categories.Count <= 0) return; 

            foreach (string ItemCategories in TargetNewItem.Entity.categories)
            {
                AssignedCategory.Add(ItemCategories);
            }
            RemoveArtifactList.Add(TargetNewItem.Entity.id);
            SaveItemOnServer(SaveManager.CurrentRun);
            NextBroadcastTime = (float)NetworkTime.time + 2f;
            BroadcastCooldown();
            BroadcastList();

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