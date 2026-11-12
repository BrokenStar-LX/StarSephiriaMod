using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace SPMod.ModContent
{
    public class ComboEffect_WindSword : ComboEffectBase
    {
        public const string ItemKey = "WindSword";
        public int ActivateComboCount = 2;
        public float RemoveDelay = 0.2f;
        public LocalizedString CreateEffectString = new("ModComboEffect_WindSword_Create");
        private bool isGranting;
        private bool isRemoving;
        public override int GetHighestComboCount()
        {
            return Mathf.Max(base.GetHighestComboCount(), ActivateComboCount);
        }

        protected override void OnRequestComboData(UnitAvatar avatar, List<ComboEffectElement> elements)
        {
            base.OnRequestComboData(avatar, elements);
            string EffectText = CreateEffectString.ToString();
            if (LoadModContent.GetModItem.TryGetValue(ItemKey, out ItemEntity WindSword) && WindSword != null)
                EffectText = EffectText.Replace("{ITEMNAME}", WindSword.Name);
            elements.Add(new ComboEffectElement
            {
                comboCount = ActivateComboCount,
                effectName = EffectText
            });
        }

        protected override int OnEnableEffect(int comboCount, int oldComboCount)
        {
            int Applied = base.OnEnableEffect(comboCount, oldComboCount);
            if (comboCount >= ActivateComboCount && Applied < ActivateComboCount)
                Applied = ActivateComboCount;
            if (isServer)
            {
                if (comboCount >= ActivateComboCount) BeginGrant();
                else if (comboCount > 0) BeginRemove();
            }
            return Applied;
        }

        protected override void OnDisableEffect()
        {
            base.OnDisableEffect();
            if (isServer) BeginRemove();
        }

        private void BeginRemove()
        {
            if (isRemoving) return;
            if (Networkavatar is not PlayerAvatar Player || Player.Inventory == null) return;
            if (!LoadModContent.GetModItem.TryGetValue(ItemKey, out ItemEntity WindSword) || WindSword == null) return;
            if (!HasWindSword(Player.Inventory, WindSword)) return;
            isRemoving = true;
            Player.StartCoroutine(RemoveRoutine(this, Player, categoryName, RemoveDelay));
        }

        private void BeginGrant()
        {
            if (isGranting || Networkavatar is not PlayerAvatar Player) return;
            Player.StartCoroutine(GrantRoutine(Player));
        }

        private IEnumerator GrantRoutine(PlayerAvatar Player)
        {
            isGranting = true;
            try
            {
                yield return null;
                if (!NetworkServer.active || !isEnabled || comboCount < ActivateComboCount) yield break;
                if (!LoadModContent.GetModItem.TryGetValue(ItemKey, out ItemEntity WindSword) || WindSword == null) yield break;

                GridInventory Inventory = Player.Inventory;
                if (HasWindSword(Inventory, WindSword)) yield break;
                if (!Inventory.HasEmptySlot(out _)) yield break;

                int InstanceID = ItemDatabase.GenerateInstanceID(new System.Random());
                using (new GridInventory.Permission(Inventory))
                {
                    Inventory.LocalAddItem(InstanceID, WindSword.id, 1, 0, true, false);
                }
            }
            finally
            {
                isGranting = false;
            }
        }

        private static bool HasWindSword(GridInventory Inventory, ItemEntity WindSword)
        {
            if (Inventory.HasItem(WindSword, out _, out _, out _)) return true;
            foreach (ItemMetadata SubBag in Inventory.subBagMatrix.Values)
                if (SubBag.entityID == WindSword.id) return true;
            return false;
        }

        private static IEnumerator RemoveRoutine(ComboEffect_WindSword Effect, PlayerAvatar Player, string CategoryName, float Delay)
        {
            try
            {
                yield return new WaitForSeconds(Delay);
                if (!NetworkServer.active || Player == null) yield break;

                if (Player.Inventory.FindComboEffect(CategoryName) is ComboEffect_WindSword Current && Current.isEnabled && Current.comboCount >= Current.ActivateComboCount)
                    yield break;

                if (!LoadModContent.GetModItem.TryGetValue(ItemKey, out ItemEntity WindSword) || WindSword == null) yield break;
                GridInventory Inventory = Player.Inventory;
                if (!HasWindSword(Inventory, WindSword)) yield break;

                using (new GridInventory.Permission(Inventory))
                {
                    if (Inventory.HasItem(WindSword, out sbyte X, out sbyte Y, out _))
                        Inventory.ForceRemoveItem(X, Y);
                }
                foreach (KeyValuePair<sbyte, ItemMetadata> SubBag in new List<KeyValuePair<sbyte, ItemMetadata>>(Inventory.subBagMatrix))
                {
                    if (SubBag.Value.entityID == WindSword.id)
                        Inventory.ServerDecreaseSubBagItemQuantity(SubBag.Key, SubBag.Value.quantity);
                }
            }
            finally
            {
                if (Effect != null) Effect.isRemoving = false;
            }
        }
    }
}
