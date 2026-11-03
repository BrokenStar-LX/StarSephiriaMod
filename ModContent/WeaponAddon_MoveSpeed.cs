using Mirror;
using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    public class WeaponAddon_MoveSpeed : WeaponAddon
    {
        public int TargetId = 0;
        public float Bonus = 0.05f;
        private UnitAvatar? AppliedAvatar;
        protected override void OnEnableAddon()
        {
            base.OnEnableAddon();
            if (parent == null || parent.Networkowner == null) return;

            UnitAvatar Avatar = parent.Networkowner.unitAvatar;
            if (Avatar == null) return;

            AppliedAvatar = Avatar;
            Avatar.NetworkmoveSpeedMultiplier = Avatar.moveSpeedMultiplier + Bonus;
        }

        protected override void OnDisableAddon()
        {
            base.OnDisableAddon();
            Restore();
        }

        private void OnDestroy()
        {
            Restore();
        }

        private void Restore()
        {
            if (AppliedAvatar == null) return;
            AppliedAvatar.NetworkmoveSpeedMultiplier = AppliedAvatar.moveSpeedMultiplier - Bonus;
            AppliedAvatar = null;
        }

        public override Loc.KeywordValue[] BuildKeywords()
        {
            return
            [
                new("Value", (Bonus * 100f).ToString("#0") + "%", Color.green)
            ];
        }
    }
}