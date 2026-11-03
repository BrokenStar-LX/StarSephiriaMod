using Mirror;
using System.Collections.Generic;
using System.Linq;
using static SPMod.Tool.ModuleComponents;

namespace SPMod.ModContent
{
    public class Charm_Alchemy : Charm_Basic
    {
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            return
            [
                new("Value", "1", GetPositiveColor(virtualLevelOffset)),
            ];
        }
        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            NetworkAvatar.OnAttackUnit += HandleCheck;
            DungeonPatchs.OnLoadNewNumberofReincarnations += ReSave;
            foreach (ModItem Value in LoadModContent.ModItemEntityData)
            {
                if (Value.Item == null) continue;
                if (Value.Item.type != EItemType.Potion) continue;

                int Id = Value.Item.id;
                if (Value.Item.rarity == EItemRarity.Uncommon)
                {
                    if (!UncommonPotion.Contains(Id))
                        UncommonPotion.Add(Id);
                }
                if (Value.Item.rarity == EItemRarity.Rare)
                {
                    if (!RarePotion.Contains(Id))
                        RarePotion.Add(Id);
                }
            }
        }


        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.OnAttackUnit -= HandleCheck;
            DungeonPatchs.OnLoadNewNumberofReincarnations -= ReSave;
        }

        private void ReSave()
        {
            if (NetworkAvatar is PlayerAvatar Player)
            {
                foreach (string Falg in Player.savableParameters.ToList())
                {
                    if (Falg.StartsWith("Alchemy/"))
                        Player.savableParameters.Remove(Falg);
                }
            }
        }

        private void HandleCheck(UnitAvatar victim, DamageInstance instance)
        {
            if (NetworkAvatar.IsDead || !NetworkServer.active)
            {
                return;
            }
            if(NetworkAvatar is PlayerAvatar Player)
            {
                string TargetName = victim.name.Replace("(Clone)", "");
                if (Player.savableParameters.Contains("Alchemy/" + TargetName) || TargetName == "RootDemonPart_Point" || TargetName == "RootDemonPart_Laser")
                {
                    return;
                }
                Player.savableParameters.Add("Alchemy/" + TargetName);
                if (victim.monsterType == EMonsterType.Miniboss)
                    GetFixedPoolPotion(NetworkAvatar, UncommonPotion, 1);
                if (victim.monsterType == EMonsterType.Boss)
                    GetFixedPoolPotion(NetworkAvatar, RarePotion, 1);
            }
        }

        private readonly List<int> UncommonPotion =
        [
            1,
            34,
            40,
            41,
            42,
            43,
            44,
            49,
            50,
            51
        ];
        private readonly List<int> RarePotion =
        [
            28,
            29,
            30,
            31,
            32,
            33,
            38,
            39,
            45,
            47,
            48
        ];

        private static List<int> GetFixedPoolPotion(UnitAvatar Player, List<int> PotionIdPool, int Count)
        {
            List<int> successIds = [];
            if (!NetworkServer.active || Player == null || PotionIdPool == null || PotionIdPool.Count == 0)
                return successIds;

            using (new GridInventory.Permission(Player.Inventory))
            {
                for (int i = 0; i < Count; i++)
                {
                    int randomIdx = UnityEngine.Random.Range(0, PotionIdPool.Count);
                    int potionId = PotionIdPool[randomIdx];

                    ItemEntity entity = ItemDatabase.FindItemById(potionId);
                    if (entity == null) continue;

                    int instanceId = ItemDatabase.GenerateInstanceID(new System.Random());
                    if (Player.Inventory.LocalAddItem(instanceId, potionId, 1, 0, true, false))
                    {
                        successIds.Add(potionId);
                    }
                }
            }
            return successIds;
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}