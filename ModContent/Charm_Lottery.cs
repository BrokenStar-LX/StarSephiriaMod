using HeathenEngineering.SteamworksIntegration.API;
using Mirror;
using SPMod.Tool;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_Lottery : Charm_Active
    {
        public List<StatusGroup> AddStatus = new List<StatusGroup>();
        public bool RandomLottery = false;
        public float LuckMult = 0.5f;
        [SyncVar]
        public int Level = 0;
        public Timer CooldownTimer = new Timer(3f);
        [SyncVar]
        public bool UseShootBack = false;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            float UseCoin = GetLevelUpValue(avatar.GetCustomStatUnsafe(TextFile.Luck)) * 100f;
            string LevelText = "ItemRarity_Common";
            switch(Level)
            {
                case 1:
                    LevelText = "ItemRarity_Uncommon";
                    break;
                case 2:
                    LevelText = "ItemRarity_Rare";
                    break;
                case 3:
                    LevelText = "ItemRarity_Legend";
                    break;
            }
            string StatusText = "";
            foreach(StatusGroup Group in AddStatus)
            {
                if (Group.valuesByLevel.SafeRandomAccess(Level) <= 0) continue;
                string Key = StatusDatabase.CreateStatusEntity(Group.statusID, Group.valuesByLevel.SafeRandomAccess(Level))
                    .ToString(true, false, true, GetPositiveColor(virtualLevelOffset));
                StatusText += $"\n{Key}";
            }
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Probability", UseCoin.ToString("#0.0") + "%", GetPositiveColor(virtualLevelOffset)),
                new Loc.KeywordValue("Level", $"<{KeywordDatabase.GetEntity(LevelText).Convert()}>", Color.yellow),
                new Loc.KeywordValue("Status", StatusText, GetPositiveColor(virtualLevelOffset)),
            };
        }

        public override float GetCooldownTime(out bool changeByLevel, int level, int virtualLevelOffset)
        {
            changeByLevel = false;
            return CooldownTimer.time;
        }

        public override float GetCooldownRatio()
        {
            if (UseShootBack)
            {
                return 0f;
            }
            return 1f - CooldownTimer.Ratio;
        }

        public override void CooldownDecrease(float ratio)
        {
            CooldownTimer.Ratio += ratio;
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            if (NetworkAvatar is PlayerAvatar Player)
            {
                Player.OnEnteredFloorServerside += HandleEnterFloor;
            }
            DungeonPatchs.OnLoadNewNumberofReincarnations += ReSave;
            CooldownTimer.Ratio = 1f;
            UseShootBack = true;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            if (NetworkAvatar is PlayerAvatar Player)
            {
                Player.OnEnteredFloorServerside -= HandleEnterFloor;
            }
            DungeonPatchs.OnLoadNewNumberofReincarnations -= ReSave;
            CooldownTimer.Ratio = 1f;
        }

        private void ReSave()
        {
            if (NetworkAvatar is PlayerAvatar Player)
            {
                foreach (string Falg in Player.savableParameters.ToList())
                {
                    if (Falg.StartsWith("Lottery_MoveFloor/"))
                        Player.savableParameters.Remove(Falg);
                }
            }
        }

        private float GetLevelUpValue(int Value)
        {
            if (Level >= 3)
                return 0f;

            float Mult = 100f / (100f + Value);
            float LuckValue = LuckMult;
            if (RandomLottery) LuckValue -= Level * 0.2f;
            return (1f - Mult) * LuckValue;
        }

        private void HandleEnterFloor(string f)
        {
            if (Level >= 3) return;
            if (NetworkAvatar is PlayerAvatar Player)
            {
                if (Player.savableParameters.Contains("Lottery_MoveFloor/" + $"{Item.InstanceID}/" + f))
                {
                    return;
                }
                Player.savableParameters.Add("Lottery_MoveFloor/" + $"{Item.InstanceID}/" + f);
                float Value = GetLevelUpValue(Player.GetCustomStatUnsafe(TextFile.Luck));
                if (Value * 100f > Random.Range(0, 100))
                {
                    Level += 1;
                }
            }
        }

        public override void SaveItemOnServer(ISaveData saveData)
        {
            saveData.SetInt($"CharmSaveData_PotionTransactionProof_{Item.InstanceID}", Level);
            base.SaveItemOnServer(saveData);
        }

        public override void LoadItemOnServer(ISaveData saveData)
        {
            Level = saveData.GetInt($"CharmSaveData_PotionTransactionProof_{Item.InstanceID}", 0);
            base.LoadItemOnServer(saveData);
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
            int Count = 0;
            foreach (var Combo in Inventory.lastAppliedComboEffects)
            {
                if (RandomLottery)
                {
                    if (!TargetCategories.Contains(Combo.Key))
                        TargetCategories.Add(Combo.Key);
                    Count = 1;
                }
                else if (Combo.Value.comboCount > Count)
                {
                    TargetCategories.Clear();
                    TargetCategories.Add(Combo.Key);
                    Count = Combo.Value.comboCount;
                }
            }
            bool UseNoCategories = false;
            if (RandomLottery)
            {
                float Value = 100 / (TargetCategories.Count + 1);
                if (Random.Range(0, 100) < Value) UseNoCategories = true;
            }
            if (UseNoCategories)
            {
                //什么都没有哦
            }
            else if (TargetCategories.Count <= 0 || Count <= 0) return;
            string UseCategories = "";
            if (TargetCategories.Count > 0)
            {
                TargetCategories.Shuffle();
                UseCategories = TargetCategories[0];
            }

            var mySpawner = PlayerSpawner.MultiplayerList.FirstOrDefault(s => s.PlayerAvatar == NetworkAvatar);
            if (mySpawner == null) return;
            List<int> AllCharmList = mySpawner.unlockedCharms.ToList();
            List<int> AddItemList = new List<int>();

            EItemRarity Rarity = EItemRarity.Common;
            switch (Level)
            {
                case 1:
                    Rarity = EItemRarity.Uncommon;
                    break;
                case 2:
                    Rarity = EItemRarity.Rare;
                    break;
                case 3:
                    Rarity = EItemRarity.Legend;
                    break;
            }
            foreach (int NewItemList in AllCharmList)
            {
                ItemEntity TargetItem = ItemDatabase.FindItemById(NewItemList);
                if (TargetItem.id == Item.EntityID) continue;
                if (TargetItem.rarity != Rarity) continue;
                if (TargetItem.activeType == EItemActiveType.Hidden) continue;
                if (!UseNoCategories && !TargetItem.categories.Contains(UseCategories)) continue;
                if (TargetItem.resourcePrefab.TryGetComponent<Charm_Basic>(out var Charm) && Charm.isUniqueEffect)
                {
                    if (Inventory.HasItem(TargetItem, out var _, out var _, out var _)) continue;
                }
                AddItemList.Add(TargetItem.id);
            }
            if (AddItemList.Count <= 0) return;

            if (NetworkAvatar is PlayerAvatar Player)
            {
                foreach (string Falg in Player.savableParameters.ToList())
                {
                    if (Falg.StartsWith("Lottery_MoveFloor/" + $"{Item.InstanceID}/"))
                        Player.savableParameters.Remove(Falg);
                }
            }

            foreach(StatusGroup Target in AddStatus)
            {
                if (Target.valuesByLevel.SafeRandomAccess(Level) <= 0) continue;
                StatusInstance Status = StatusDatabase.CreateStatusEntity(Target.statusID, Target.valuesByLevel.SafeRandomAccess(Level));
                NetworkAvatar.AddOrphanedStatusInstance(Status);
            }
            GridInventory inventory = Inventory;
            int instanceID = Item.InstanceID;
            ItemPosition itemPosition = new ItemPosition(Item.XIdx, Item.YIdx);
            using (new GridInventory.Permission(inventory))
            {
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