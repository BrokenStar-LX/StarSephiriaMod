using System.Collections.Generic;
using UnityEngine;
using static SPMod.Tool.ModuleComponents;

namespace SPMod.ModContent
{
    public class WeaponAddon_ForgeAttack : WeaponAddonCommon_StatusUnsafe
    {
        public static List<ForgeAttackData> WindForgeEffect = [];
        protected override void OnEnableAddon()
        {
            base.OnEnableAddon();
            parent.Networkowner.OnSpecialAttack += OnSpecialAttackUnit;
            if (isServer)
                WindForgeEffect.RemoveAll(Data => Data.Player == null);
        }

        protected override void OnDisableAddon()
        {
            base.OnDisableAddon();
            if (parent != null && parent.Networkowner != null)
                parent.Networkowner.OnSpecialAttack -= OnSpecialAttackUnit;

            if (!isServer || parent == null || parent.Networkowner == null) return;
            UnitAvatar? Player = parent.Networkowner.unitAvatar;
            if (Player != null)
                WindForgeEffect.RemoveAll(Data => Data.Player == Player);
        }

        private void OnSpecialAttackUnit(CombatBehaviour victim, DamageInstance instance, ProjectileBase projectile)
        {
            if (!isServer || !isEnabled) return;
            if (victim is not UnitAvatar Target || Target.IsDead) return;

            UnitAvatar? Player = parent.Networkowner.unitAvatar;
            if (Player == null) return;

            if (WindForgeEffect.Exists(Data => Data.Target == Target)) return;
            WindForgeEffect.Add(new ForgeAttackData(Player, Target));
        }

        private void Update()
        {
            if (!isServer || !isEnabled) return;
            if (parent == null || parent.Networkowner == null) return;

            UnitAvatar? Player = parent.Networkowner.unitAvatar;
            if (Player == null) return;

            for (int i = WindForgeEffect.Count - 1; i >= 0; i--)
            {
                ForgeAttackData Data = WindForgeEffect[i];
                if (Data.Player == null)
                {
                    WindForgeEffect.RemoveAt(i);
                    continue;
                }
                if (Data.Player != Player) continue;
                if (Data.Update(Time.deltaTime))
                    WindForgeEffect.RemoveAt(i);
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}
