using HarmonyLib;
using SPMod.Tool;
using System;
using System.Collections.Generic;
using UnityEngine;
using static SPMod.Tool.ModuleComponents;
using static UnityEngine.UI.GridLayoutGroup;

namespace SPMod
{
    [HarmonyPatch]
    public static class Skin
    {
        private static readonly List<SpriteReplaceData> SkinList = new List<SpriteReplaceData>();
        private static readonly Dictionary<string, SpriteReplaceData> SpriteToSkin = new Dictionary<string, SpriteReplaceData>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<(Sprite Origin, Sprite Replacement), Sprite> PivotFixed = new Dictionary<(Sprite Origin, Sprite Replacement), Sprite>();
        public static Sprite FixPivot(Sprite origin, Sprite repl)
        {
            if (PivotFixed.TryGetValue((origin, repl), out Sprite Cached)) return Cached;

            Vector2 Pivot = new Vector2(origin.pivot.x / origin.rect.width,
                                        origin.pivot.y / origin.rect.height);
            Sprite Fixed = Sprite.Create(repl.texture, repl.rect, Pivot, origin.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            Fixed.name = repl.name;
            PivotFixed[(origin, repl)] = Fixed;
            return Fixed;
        }
        public static void Add(SpriteReplaceData Data)
        {
            SkinList.Add(Data);
            foreach (string Name in Data.Sprites.Keys)
                SpriteToSkin[Name] = Data;
        }
        private static void Handle(Animator2D_Basic animator, ref Sprite sprite)
        {
            if (sprite == null) return;
            if (ModBullet.HasSpriteName(sprite.name))
            {
                ModSpriteMap? Marker = animator.GetComponentInParent<ModSpriteMap>(true);
                if (Marker != null && ModBullet.TryGetSpriteMap(Marker.Key, out Dictionary<string, Sprite> ModMap)
                    && ModMap.TryGetValue(sprite.name, out Sprite? ModSprite) && ModSprite != null)
                {
                    sprite = FixPivot(sprite, ModSprite);
                    return;
                }
            }
            if (!SpriteToSkin.TryGetValue(sprite.name, out SpriteReplaceData Data)) return;
            UnitAvatar? Player = GetPlayer(animator);
            if (Player == null) return;
            if (!Data.GetModSprite(Player, sprite.name, out Sprite? Value) || Value == null) return;
            sprite = FixPivot(sprite, Value);
        }

        private static UnitAvatar? GetPlayer(Animator2D_Basic animator)
        {
            if (animator.TryGetComponentInParent<UnitAvatar>(out UnitAvatar? Player) && Player != null) return Player;

            SpriteFx? Fx = animator.GetComponentInParent<SpriteFx>();
            if (Fx == null) return null;
            if (Fx.FollowParent != null && Fx.FollowParent.TryGetComponent<UnitAvatar>(out Player) && Player != null) return Player;
            return null;
        }
        public static void GetAddAllModSkin()
        {
            SkinList.Clear();
            SpriteToSkin.Clear();
            Add(new SpriteReplaceData(owner =>
            {
                if (!owner) return false;
                if (!ModConfig.Instance.EnableWeaponModifier) return false;
                return owner.GetCustomStatUnsafe(TextFile.FlameSwordFrost) > 0;
            }, 
            new List<string>()
            {
                "katana_Fire_FlameSword_1",
                "katana_Fire_FlameSword_2",
                "katana_Fire_FlameSword_UpgradeFX00",
                "katana_Fire_FlameSword_UpgradeFX01",
                "katana_Fire_FlameSword_UpgradeFX02",
                "katana_Fire_FlameSword_UpgradeFX03",
                "katana_Fire_FlameSword_UpgradeFX04",
                "katana_Fire_FlameSword_UpgradeFX05",
                "katana_Fire_FlameSword_UpgradeFX06",
                "Katana_FireShura1_Swing0",
                "Katana_FireShura1_Swing1",
                "Katana_FireShura1_Swing2",
                "Katana_FireShura1_Swing3",
                "Katana_FireShura1_Swing4",
                "Katana_FireShura1_Swing5",
                "Katana_FireShura1_Swing6",
                "Katana_FireShura2_Swing0",
                "Katana_FireShura2_Swing1",
                "Katana_FireShura2_Swing2",
                "Katana_FireShura2_Swing3",
                "Katana_FireShura2_Swing4",
                "Katana_FireShura2_Swing5",
                "Katana_FireShura2_Swing6",
                "Katana_FireShura3_Swing0",
                "Katana_FireShura3_Swing1",
                "Katana_FireShura3_Swing2",
                "Katana_FireShura3_Swing3",
                "Katana_FireShura3_Swing4",
                "Katana_FireShura3_Swing5",
                "Katana_FireShura3_Swing6",
                "Katana_FireShura4_Swing0",
                "Katana_FireShura4_Swing1",
                "Katana_FireShura4_Swing2",
                "Katana_FireShura4_Swing3",
                "Katana_FireShura4_Swing4",
                "Katana_FireShura4_Swing5",
                "Katana_FireShura4_Swing6",
                "Katana_FireShura5_Swing0",
                "Katana_FireShura5_Swing1",
                "Katana_FireShura5_Swing2",
                "Katana_FireShura5_Swing3",
                "Katana_FireShura5_Swing4",
                "Katana_FireShura5_Swing5",
                "Katana_FireShura5_Swing6",
                "Katana_FireShura5_Swing7",
            }, "Weapon.SolisEkli"));
        }

        [HarmonyPatch(typeof(Animator2D_MultipleSpriteRenderer))]
        public static class Animator2D_MultipleSpriteRenderer_Patch
        {
            [HarmonyPatch("SetSprite")]
            [HarmonyPrefix]
            static void SetSprite_Prefix(Animator2D_MultipleSpriteRenderer __instance, ref Sprite sprite)
            {
                if (sprite == null) return;
                Handle(__instance, ref sprite);
            }
        }

        [HarmonyPatch(typeof(Animator2D_SpriteRenderer))]
        public static class Animator2D_SpriteRenderer_Patch
        {
            [HarmonyPatch("SetSprite")]
            [HarmonyPrefix]
            static void SetSprite_Prefix(Animator2D_SpriteRenderer __instance, ref Sprite sprite)
            {
                if (sprite == null) return;
                Handle(__instance, ref sprite);
            }
        }
    }
}
