using Mirror;
using SPMod.Tool;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace SPMod.ModContent
{
    public class WeaponAddon_DeBuffAttack : WeaponAddonCommon_Status
    {
        public float debuffPercent = 60f;
        public float AdddebuffPercent = 40f;
        public CharacterDebuff? debuffPrefab;
        public override Loc.KeywordValue[] BuildKeywords()
        {
            return new List<Loc.KeywordValue>(base.BuildKeywords())
            {
                new Loc.KeywordValue("PERCENT", debuffPercent.ToString() + "%", Color.yellow),
                new Loc.KeywordValue("ADDPERCENT", AdddebuffPercent.ToString() + "%", Color.yellow),
            }.ToArray();
        }

        protected override void OnEnableAddon()
        {
            base.OnEnableAddon();
            parent.Networkowner.unitAvatar.OnAttackUnit += InflictDebuff;
        }

        protected override void OnDisableAddon()
        {
            base.OnDisableAddon();
            parent.Networkowner.unitAvatar.OnAttackUnit -= InflictDebuff;
        }

        private void InflictDebuff(UnitAvatar avatar, DamageInstance instance)
        {
            if (debuffPrefab == null) return;
            if (instance.fromType != EDamageFromType.DirectAttack) return;
            if (instance.ignoreDebuff) return;
            float Value = debuffPercent;
            bool falg = false;
            foreach (CharacterDebuff DeBuff in avatar.Debuffs)
            {
                if (DeBuff is CharacterDebuff_WindErosion)
                {
                    if (DeBuff.CurrentStack >= DeBuff.MaxStackCount)
                    {
                        falg = true;
                    }
                    break;
                }
            }
            if (!falg) Value += AdddebuffPercent;
            if (Random.Range(0f, 100f) <= Value)
                avatar.ApplyDebuff(debuffPrefab, parent.Networkowner.unitAvatar);
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}