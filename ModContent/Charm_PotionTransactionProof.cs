using Mirror;
using SPMod.Tool;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_PotionTransactionProof : Charm_Active
    {
        private static readonly System.Random _serverRng = new System.Random();
        public const int Expenses = 50;
        public Timer CooldownTimer = new Timer(3f);
        [SyncVar]
        public int Stack = 1;
        [SyncVar]
        public bool UseShootBack = false;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            float UseCoin = Stack * Expenses / (1f + avatar.GetCustomStatUnsafe(TextFile.Negotiation) * 0.01f);
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("UseCoin", ((int)UseCoin).ToString(), Color.yellow),
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

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            CooldownTimer.Ratio = 1f;
            UseShootBack = true;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
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

        public override void SaveItemOnServer(ISaveData saveData)
        {
            saveData.SetInt($"CharmSaveData_PotionTransactionProof_{Item.InstanceID}", Stack);
            base.SaveItemOnServer(saveData);
        }

        public override void LoadItemOnServer(ISaveData saveData)
        {
            Stack = saveData.GetInt($"CharmSaveData_PotionTransactionProof_{Item.InstanceID}", 1);
            base.LoadItemOnServer(saveData);
        }

        protected override void FireCastingServer(Vector3 aimedPosition, UnitAvatar aimedTarget)
        {
            if (!UseShootBack) return;
            float UseCoin = Stack * Expenses / (1f + NetworkAvatar.GetCustomStatUnsafe(TextFile.Negotiation) * 0.01f);
            if (NetworkAvatar.Money < (int)UseCoin)
            {
                UseShootBack = false;
                CooldownTimer.Ratio = 0f;
                RpcUIText("No Money!");
                return;
            }

            int Base = Stack - 1;
            List<int> CardPool = GetCardPool(NetworkAvatar, PotionToLevel, Mathf.FloorToInt(Base / 3), Base);
            if (CardPool.Count > 0)
            {
                UseShootBack = false;
                CooldownTimer.Ratio = 0.9f;
                GetFixedPoolPotion(NetworkAvatar, CardPool, 1);
                NetworkAvatar.SubMoney((int)UseCoin);
                RpcUIText($"-{(int)UseCoin}{KeywordDatabase.GetEntity("Leaf").GetIcon()}!");
                Stack += 1;
            }
        }

        [ClientRpc]
        private void RpcUIText(string Text)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Pool.Spawn<UI_DamageParticle>(NetworkAvatar.transform.position + (Vector3)Random.insideUnitCircle).SetDamage(Text, EffectConstant.CritColor);
            }
        }

        private readonly List<(int weight, int itemId)> PotionToLevel = new List<(int weight, int itemId)>
        {
            (0, 0),   //小再生
            (1, 1),   //大再生
            (2, 27),  //苹果汁
            (8, 28),  //大物理药
            (8, 29),  //大冰药
            (8, 30),  //大火药
            (8, 31),  //大雷药
            (6, 32),  //大随机药
            (5, 33),  //小骰子药
            (9, 34),  //大骰子药
            (5, 35),  //全伤药
            (0, 37),  //再生样品
            (7, 38),  //附魔药
            (10, 39), //蘑菇汤
            (4, 40),  //小物理药
            (4, 41),  //小冰药
            (4, 42),  //小火药
            (4, 43),  //小雷药
            (3, 44),  //小随机药
            (5, 45),  //大生命药
            (6, 47),  //大防御药
            (6, 48),  //大闪避药
            (3, 49),  //小防御药
            (3, 50),  //小闪避药
            (5, 51),  //元素药
        };
        private static List<int> GetCardPool(UnitAvatar Player, List<(int, int)> Orig, int Min, int Max)
        {
            List<int> Pool = new List<int>();
            foreach (var Target in Orig)
            {
                int Value = Target.Item1;
                int UseMin = Min;
                int UseMax = Max;
                if (Player.GetCustomStatUnsafe(TextFile.Luck) > Random.Range(0, 100))
                {
                    UseMin += 1;
                }
                if (Player.GetCustomStatUnsafe(TextFile.Luck) > Random.Range(0, 100))
                {
                    UseMax += 2;
                }
                UseMin = Mathf.Clamp(UseMin, 0, 10);
                UseMax = Mathf.Clamp(UseMax, 0, 10);
                Value = Mathf.Clamp(Value, 0, 10);
                if (Value < UseMin) continue;
                if (Value > UseMax) continue;
                Pool.Add(Target.Item2);
            }
            return Pool;
        }
        private static List<int> GetFixedPoolPotion(UnitAvatar Player, List<int> PotionIdPool, int Count)
        {
            List<int> successIds = new List<int>();
            if (!NetworkServer.active || Player == null || PotionIdPool == null || PotionIdPool.Count == 0)
                return successIds;

            using (new GridInventory.Permission(Player.Inventory))
            {
                for (int i = 0; i < Count; i++)
                {
                    int randomIdx = Random.Range(0, PotionIdPool.Count);
                    int potionId = PotionIdPool[randomIdx];

                    ItemEntity entity = ItemDatabase.FindItemById(potionId);
                    if (entity == null) continue;

                    int instanceId = ItemDatabase.GenerateInstanceID(_serverRng);
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