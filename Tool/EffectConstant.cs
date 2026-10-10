using System.Collections.Generic;
using Color = UnityEngine.Color;

namespace SPMod.Tool
{
    public static class EffectConstant
    {
        public const int CommonCost = 400;
        public const int UncommonCost = 600;
        public const int RareCost = 800;
        public const int LegendCost = 1100;

        public static Color WeaponColor = new Color(0.9725f, 0.9176f, 0.847f);
        public static Color WeaponColor2 = new Color(0.8745f, 0.4431f, 0.1490f);
        public static Color AttackSpeedColor = new Color(0.9843138f, 1f, 0.7176471f);
        public static Color DashColor = new Color(0.2784314f, 0.8745099f, 0.9843138f);
        public static Color IceColor = new Color(0.8392157f, 0.9137255f, 0.9529412f);
        public static Color FireColor = new Color(1f, 0.7882353f, 0f);
        public static Color LightningColor = new Color(0.5294118f, 1f, 1f);
        public static Color CritColor = new Color(0.9056604f, 0.8040091f, 0.2862228f);
        public static Color PoisonColor = new Color(0.5986261f, 0.136659f, 0.7830189f);
        public static Color MagicColor = new Color(0.2784314f, 0.5529412f, 0.9803922f);
        public static Color CooldownColor = new Color(0.9843138f, 1f, 0.7176471f);
        public static Color WoundColor = new Color(0.764151f, 0.06127627f, 0.1185653f);
        public static Color ElementalColor = new Color(0.9986485f, 0.8537736f, 1f);
        public static Color DefenseColor = new Color(0.6596209f, 0.764151f, 0.7577543f);
        public static Color ChaosColor = new Color(0.7735849f, 0.1569064f, 0.7081023f);
        public static Color PlasmaCrystalColor = new Color(
        (FireColor.r + IceColor.r + LightningColor.r) / 3f,
        (FireColor.g + IceColor.g + LightningColor.g) / 3f,
        (FireColor.b + IceColor.b + LightningColor.b) / 3f);

        public static List<string> BaseStatsName = new List<string>()
        {
            TextFile.PhysicalDamage, TextFile.FireDamage, TextFile.IceDamage, TextFile.LightningDamage
        };
        public static List<string> PartyStatsName = new List<string>()
        {
            TextFile.DamageReduction, TextFile.AllDamageBonus, TextFile.Critical
        };
        public static Dictionary<string, int> ActiveSkillList = new Dictionary<string, int>();
    }
}