using HarmonyLib;
using SPMod.Tool;
using System.Reflection;

namespace SPMod.ModContent
{
    public class Charm_ElectricDarkCloud : Charm_Basic
    {
        private readonly MethodInfo AttackField = AccessTools.Method(typeof(CharacterDebuff_Electric), "Attack");
        private readonly MethodInfo AttackField_Plasma = AccessTools.Method(typeof(CharacterDebuff_Plasma), "Attack");

        public float[] ChanceByLevel = new float[] { 8f, 16f, 24f, 32f };
        public string Id = "[Star's Sephiria Mod]瓶中闪电";
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Mult", ChanceByLevel.SafeRandomAccess(LevelToIdx(level)).ToString() + "%", GetPositiveColor(virtualLevelOffset))
            };
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            NetworkAvatar.OnAttackUnitBeforeOperation += AddDamage;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.OnAttackUnitBeforeOperation -= AddDamage;
        }

        private void AddDamage(UnitAvatar avatar, DamageInstance instance)
        {
            if (instance.id == "Ability_DarkCloud")
            {
                if (UnityEngine.Random.Range(0f, 100f) > ChanceByLevel.SafeRandomAccess(CurrentLevelToIdx())) return;

                float DamageMult = 1f + NetworkAvatar.GetCustomStatUnsafe(TextFile.DebuffDuration) * 0.01f;
                foreach (CharacterDebuff DeBuff in avatar.Debuffs)
                {
                    if (DeBuff is CharacterDebuff_Electric Electric)
                    {
                        AttackField.Invoke(Electric, new object[] { avatar, 1, DamageMult });
                        break;
                    }
                    if (DeBuff is CharacterDebuff_Plasma Plasma)
                    {
                        AttackField_Plasma.Invoke(Plasma, new object[] { avatar, 1, DamageMult });
                        break;
                    }
                }
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}