using HeathenEngineering.SteamworksIntegration.API;
using Mirror;
using SPMod.Tool;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_Lottery : Charm_Active, IModCooldownReceiver, IModValueReceiver
    {
        public List<StatusGroup> AddStatus = new List<StatusGroup>();
        public bool RandomLottery = false;
        public float LuckMult = 0.5f;
        public float LuckValue = 0;
        public int Level = 0;
        public Timer CooldownTimer = new Timer(3f);
        public bool UseShootBack = false;
        private float ClientPhaseEnd = 0f;
        private float NextBroadcastTime = 0f;
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
                new Loc.KeywordValue("Luck", LuckValue.ToString("#0"), Color.greenYellow),
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

        public void OnClientValues(int A, int B)
        {
            Level = A;
            LuckValue = B;
        }

        private void BroadcastCooldown()
        {
            float PhaseEnd = UseShootBack ? 0f : (float)NetworkTime.time + CooldownTimer.GetRemainingTime();
            ModNetwork.SendCooldown(netId, PhaseEnd);
        }

        private void BroadcastValues()
        {
            ModNetwork.SendValues(netId, Level, (int)LuckValue);
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
            NextBroadcastTime = (float)NetworkTime.time + 2f;
            BroadcastCooldown();
            BroadcastValues();
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
            UseShootBack = true;
            BroadcastCooldown();
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
                LuckValue = Random.Range(0, 100);
                if (Value * 100f > LuckValue)
                {
                    Level += 1;
                }
                BroadcastValues();
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
            BroadcastValues();
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
            else if (NetworkTime.time >= NextBroadcastTime)
            {
                NextBroadcastTime = (float)NetworkTime.time + 2f;
                BroadcastCooldown();
                BroadcastValues();
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

            var MySpawner = PlayerSpawner.MultiplayerList.FirstOrDefault(s => s.PlayerAvatar == NetworkAvatar);
            if (MySpawner == null) return;

            List<int> AddItemList = RandomLottery ? RandomSelectItem(MySpawner) : SelectItem(MySpawner);
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
        private EItemRarity GetLotteryRarity()
        {
            return Level switch
            {
                1 => EItemRarity.Uncommon,
                2 => EItemRarity.Rare,
                3 => EItemRarity.Legend,
                _ => EItemRarity.Common,
            };
        }
        public List<int> RandomSelectItem(PlayerSpawner Spawner)
        {
            List<string> TargetCategories = new List<string>();
            foreach (var Combo in Inventory.lastAppliedComboEffects)
            {
                if (!TargetCategories.Contains(Combo.Key))
                    TargetCategories.Add(Combo.Key);
            }
            bool UseNoCategories = TargetCategories.Count <= 0;
            if (TargetCategories.Count > 0)
            {
                float Value = 100f / (TargetCategories.Count + 1);
                if (Random.Range(0f, 100f) < Value) UseNoCategories = true;
            }

            List<int> AllCharmList = Spawner.unlockedCharms.ToList();
            List<int> AddItemList = new List<int>();
            EItemRarity Rarity = GetLotteryRarity();
            foreach (int NewItemList in AllCharmList)
            {
                ItemEntity TargetItem = ItemDatabase.FindItemById(NewItemList);
                if (TargetItem == null) continue;
                if (TargetItem.id == Item.EntityID) continue;
                if (TargetItem.rarity != Rarity) continue;
                if (TargetItem.activeType == EItemActiveType.Hidden) continue;
                if (!UseNoCategories && !TargetCategories.Any(Category => TargetItem.categories.Contains(Category))) continue;
                if (TargetItem.resourcePrefab.TryGetComponent<Charm_Basic>(out var Charm) && Charm.isUniqueEffect)
                {
                    if (Inventory.HasItem(TargetItem, out var _, out var _, out var _)) continue;
                }
                if (AddItemList.Contains(TargetItem.id)) continue;
                AddItemList.Add(TargetItem.id);
            }
            return AddItemList;
        }
        public List<int> SelectItem(PlayerSpawner Spawner)
        {
            int Count = 0;
            string TargetCategories = "";
            foreach (var Combo in Inventory.lastAppliedComboEffects)
            {
                if (Combo.Value.comboCount > Count)
                {
                    TargetCategories = Combo.Key;
                    Count = Combo.Value.comboCount;
                }
            }
            if (TargetCategories == "") return new List<int>();

            List<int> AllCharmList = Spawner.unlockedCharms.ToList();
            List<int> AddItemList = new List<int>();
            EItemRarity Rarity = GetLotteryRarity();
            foreach (int NewItemList in AllCharmList)
            {
                ItemEntity TargetItem = ItemDatabase.FindItemById(NewItemList);
                if (TargetItem == null) continue;
                if (TargetItem.id == Item.EntityID) continue;
                if (TargetItem.rarity != Rarity) continue;
                if (TargetItem.activeType == EItemActiveType.Hidden) continue;
                if (!TargetItem.categories.Contains(TargetCategories)) continue;
                if (TargetItem.resourcePrefab.TryGetComponent<Charm_Basic>(out var Charm) && Charm.isUniqueEffect)
                {
                    if (Inventory.HasItem(TargetItem, out var _, out var _, out var _)) continue;
                }
                if (AddItemList.Contains(TargetItem.id)) continue;
                AddItemList.Add(TargetItem.id);
            }
            return AddItemList;
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}