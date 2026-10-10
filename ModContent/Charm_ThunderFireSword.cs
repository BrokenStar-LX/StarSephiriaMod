using SPMod.Tool;
using System;
using static SPMod.Tool.ModuleComponents;
using static UnityEngine.UI.GridLayoutGroup;

namespace SPMod.ModContent
{
    public class Charm_ThunderFireSword : Charm_StatusInstance
    {
        public int Recycling = 1;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("RECVERY", Recycling.ToString(), GetPositiveColor(virtualLevelOffset))
            };
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            NetworkAvatar.OnAttackUnitBeforeOperation += AddDamage;
            NetworkAvatar.OnUniversalBattleCallback += OnUniversalBattleCallback;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.OnAttackUnitBeforeOperation -= AddDamage;
            NetworkAvatar.OnUniversalBattleCallback -= OnUniversalBattleCallback;
        }

        private void AddDamage(UnitAvatar avatar, DamageInstance instance)
        {
            if (!NetworkAvatar.IsInBattle) return;
            UnitAvatar Player = NetworkAvatar;
            if (instance.id == "Ability_FlameSword")
            {
                int Count = Math.Max(1, Player.GetCustomStatUnsafe(TextFile.DarkCloudMultishot));
                StarSephiriaPatches.ModCloudAttack.Add(new CloudAttackData(Player, avatar, 0.8f, 0.2f, Count));
            }
            else if (instance.id == "Ability_DarkCloud")
            {
                float Mult = 1f + Player.GetCustomStatUnsafe(TextFile.FlameSwordDamage) * 0.01f;
                Mult *= 1f + MathF.Max(33f, Player.GetCustomStatUnsafe(TextFile.FlameSwordLuck) * 0.33f) * 0.01f;
                instance.damage *= Mult;
                instance.criticalChancePercent += Player.GetCustomStatUnsafe(TextFile.FlameSwordCritical) * 0.01f;
                instance.criticalDamageRate += Player.GetCustomStatUnsafe(TextFile.FlameSwordCriticalDamageRate);
                instance.ignoreDefense += Player.GetCustomStatUnsafe(TextFile.FlameSwordIgnoreDefense);
            }
        }

        private void OnUniversalBattleCallback(string Name, int Amount)
        {
            if(Name == "ADDFLAMESWORD")
            {
                UnitAvatar Player = NetworkAvatar;
                if (Player.Inventory)
                {
                    ComboEffectBase Combo = Player.Inventory.FindComboEffect("DARKCLOUD");
                    if (Combo != null && Combo is ComboEffect_DarkCloud Effect)
                        Effect.AddCloud(1);
                }
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}