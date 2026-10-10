using Mirror;
using SPMod.ModContent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static Miracle_StatusInstance;
using static SPMod.Tool.ModuleComponents;
using StatusGroup = Charm_StatusInstance.StatusGroup;

namespace SPMod.Tool
{
    public static class Tools
    {
        public static ItemEntity CreateCharm(this ItemEntity Entity, List<string> Categories, List<StatusGroup> Status, bool Unique = true)
        {
            Entity.categories = Categories;
            Entity.type = EItemType.Charm;
            Charm_StatusInstance Effect = Entity.resourcePrefab.AddComponent<Charm_StatusInstance>();
            Effect.isWeaponRelatedCharm = false;
            Effect.effectsString = new LocalizedString[0] { };
            Effect.stats = Status.ToArray();
            int MaxLv = 0;
            foreach (StatusGroup Group in Status)
            {
                if (MaxLv < Group.valuesByLevel.Length) MaxLv = Group.valuesByLevel.Length - 1;
            }
            Effect.maxLevel = MaxLv;
            Effect.isUniqueEffect = Unique;
            Effect.enabled = false;
            return Entity;
        }
        public static ItemEntity CustomCreateCharm(this ItemEntity Entity, List<string> Categories, Action<ItemEntity> Action)
        {
            Entity.categories = Categories;
            Entity.type = EItemType.Charm;
            Action(Entity);
            return Entity;
        }
        public static ItemEntity SetCustomItemEffect(this ItemEntity Entity, Action<ItemEntity> Action)
        {
            string GetName = Entity.name.Replace("ModItemEntity_", "");
            LoadModContent.GetModAction[GetName] = Action;
            return Entity;
        }
        public static AllPlayerStatus GetMaxPlayerStatus()
        {
            AllPlayerStatus maxStatus = new AllPlayerStatus(0, 0, 0, 0);
            foreach (var Status in StarSephiriaMod.PartyEffect)
            {
                AllPlayerStatus Curren = Status.Value;
                if (maxStatus.Defense < Curren.Defense)
                {
                    maxStatus.Defense = Curren.Defense;
                }
                if (maxStatus.AllDamage < Curren.AllDamage)
                {
                    maxStatus.AllDamage = Curren.AllDamage;
                }
                if (maxStatus.Crit < Curren.Crit)
                {
                    maxStatus.Crit = Curren.Crit;
                }
                if (maxStatus.ElementalDamage < Curren.ElementalDamage)
                {
                    maxStatus.ElementalDamage = Curren.ElementalDamage;
                }
            }
            float MaxStackMult = 1f / (MathF.Max(1, PlayerSpawner.MultiplayerList.Count - 1) * 0.5f);
            if (PlayerSpawner.MultiplayerList.Count <= 1) MaxStackMult = 1f;
            maxStatus.Defense = maxStatus.Defense.IntMult(MaxStackMult);
            maxStatus.AllDamage = maxStatus.AllDamage.IntMult(MaxStackMult);
            maxStatus.Crit = maxStatus.Crit.IntMult(MaxStackMult);
            maxStatus.ElementalDamage = maxStatus.ElementalDamage.IntMult(MaxStackMult);
            return maxStatus;
        }

        public static int GetRosionStack(this UnitAvatar Player)
        {
            int i = Player.GetCustomStatUnsafe(TextFile.CurrentRosion);
            if (ModConfig.HardMode >= 4) i *= 3;
            if (PlayerSpawner.MultiplayerList.Count > 1) return Mathf.Min(90, i);  
            return i;
        }

        public static int GetMaxPlayerStatusForName(string Name)
        {
            int Value = 0;
            switch(Name)
            {
                case TextFile.AllDamageBonus:
                    Value = StarSephiriaMod.PartyStats.Value.AllDamage;
                    break;
                case TextFile.DamageReduction:
                    Value = StarSephiriaMod.PartyStats.Value.Defense;
                    break;
                case TextFile.Critical:
                    Value = StarSephiriaMod.PartyStats.Value.Crit * 100;
                    break;
            }
            return Value;
        }

        public static List<string> SetCategories(params string[] categories)
        {
            return categories.ToList();
        }

        public static List<StatInfo> SetStatInfo(params string[] stat)
        {
            List<StatInfo> infos = new List<StatInfo>();
            foreach (string statName in stat)
            {
                string[] parts = statName.Split('=');
                string left = parts[0];
                string right = parts[1];
                StatInfo statInfo = new StatInfo{ status = left };
                if (right == "None")
                    statInfo.type = EEffectType.None;
                if (right == "Positive")
                    statInfo.type = EEffectType.Positive;
                if (right == "Negative")
                    statInfo.type = EEffectType.Negative;
                infos.Add(statInfo);
            }
            return infos;
        }

        public static void UpdateModEffectTime(this UnitAvatar __instance, ref Dictionary<UnitAvatar, Timer> Target, Action<UnitAvatar>? Effect = null, Action<UnitAvatar>? End = null)
        {
            if (Target.TryGetValue(__instance, out Timer tim))
            {
                Effect?.Invoke(__instance);
                if (tim.Update(Time.deltaTime))
                {
                    Target.SafeRemove(__instance);
                    End?.Invoke(__instance);
                }
            }
        }

        public static StatusGroup SetStatusGroup(string statName, params int[] values)
        {
            return new StatusGroup
            {
                statusID = statName,
                valuesByLevel = values
            };
        }

        public static WeaponEntity SetCrossBowValue(this WeaponEntity Weapon, bool Mult, float FireSpeed, float RoldSpeed = -1f, int MagazineCount = -1, int MagazineCapacity = -1)
        {
            if (Weapon.mainWeaponPrefab.TryGetComponent<WeaponSimple_Crossbow>(out WeaponSimple_Crossbow Target))
            {
                if (Mult)
                {
                    if (StarSephiriaMod.CrossBowFireTime.TryGetValue(Weapon.id, out float Value))
                        Target.fireIntervalTimer.time = Value / FireSpeed;
                }
                else Target.fireIntervalTimer.time = FireSpeed;
                if (RoldSpeed > 0) Target.reloadTime = RoldSpeed;
                if (MagazineCount > 0) Target.defaultMagazineCount = MagazineCount;
                if (MagazineCapacity > 0) Target.defaultMagazineCapacity = MagazineCapacity;
            }
            return Weapon;
        }

        public static string GetLocalization(string key)
        {
            if (LocalizationManager.Instance == null) return key;
            return LocalizationManager.Instance.GetText(LocalizationManager.Instance.CurrentLanguage, key);
        }

        public static void ManualCritChance(this DamageInstance Damage, float Chance, bool Execution = false)
        {
            if (UnityEngine.Random.Range(0, 100f) < Chance)
            {
                Damage.isCriticalAttack = true;
                if (Execution)
                {
                    if (UnityEngine.Random.Range(0, 100f) < Chance)
                    {
                        Damage.isExecutionAttack = true;
                    }
                }
            }
        }

        public static bool EqualsAny(this string str, string values)
        {
            if (string.IsNullOrEmpty(str)) return false;
            if (values == str) return true;
            return false;
        }

        public static bool EqualsAny(this string str, params string[] values)
        {
            if (string.IsNullOrEmpty(str)) return false;
            if (values.Contains(str)) return true;
            return false;
        }

        public static void StatChange(UnitAvatar Player, string Id, string ValueA, string ValueB, ref Dictionary<string, int> SaveValue, ref int result, float Efficiency = 1f)
        {
            if (Id == ValueA)
            {
                SaveValue[Player.Name] = result;
                result = 0;
            }
            if (Id == ValueB && SaveValue.TryGetValue(Player.Name, out int Value))
            {
                result += (int)(Value * Efficiency);
            }
        }

        public static EDamageElementalType BaseStatTypeToMax(UnitAvatar Player)
        {
            int[] numbers = {
                                Player.GetCustomStat(ECustomStat.PhysicalDamage),
                                Player.GetCustomStat(ECustomStat.FireDamage),
                                Player.GetCustomStat(ECustomStat.IceDamage),
                                Player.GetCustomStat(ECustomStat.LightningDamage)
                            };
            if (numbers.Max() == numbers[1]) return EDamageElementalType.Fire;
            if (numbers.Max() == numbers[2]) return EDamageElementalType.Ice;
            if (numbers.Max() == numbers[3]) return EDamageElementalType.Lightning;
            return EDamageElementalType.Physical;
        }

        public static int BaseStatToMax(UnitAvatar Player)
        {
            int[] numbers = {Player.GetCustomStat(ECustomStat.PhysicalDamage),
                        Player.GetCustomStat(ECustomStat.FireDamage),
                        Player.GetCustomStat(ECustomStat.IceDamage),
                        Player.GetCustomStat(ECustomStat.LightningDamage)
                        };
            return numbers.Max();
        }

        public static int BaseStatToAverage(UnitAvatar Player)
        {
            int[] numbers = {Player.GetCustomStat(ECustomStat.PhysicalDamage),
                        Player.GetCustomStat(ECustomStat.FireDamage),
                        Player.GetCustomStat(ECustomStat.IceDamage),
                        Player.GetCustomStat(ECustomStat.LightningDamage)
                        };
            return (int)numbers.Average();
        }

        public static int BaseStatToMin(UnitAvatar Player)
        {
            int[] numbers = {Player.GetCustomStat(ECustomStat.PhysicalDamage),
                        Player.GetCustomStat(ECustomStat.FireDamage),
                        Player.GetCustomStat(ECustomStat.IceDamage),
                        Player.GetCustomStat(ECustomStat.LightningDamage)
                        };
            return numbers.Min();
        }

        public static int BaseOrigStatToMax(UnitAvatar Player)
        {
            int[] numbers = {
                Player.customStats.GetValueOrDefault(TextFile.PhysicalDamage, 0),
                Player.customStats.GetValueOrDefault(TextFile.FireDamage, 0),
                Player.customStats.GetValueOrDefault(TextFile.IceDamage, 0),
                Player.customStats.GetValueOrDefault(TextFile.LightningDamage, 0),
                        };
            return numbers.Max();
        }

        public static float GetMaxLife(UnitAvatar __instance)
        {
            float baseHp = __instance.maxHp;
            float finalHp = __instance.finalMaxHp;
            return baseHp + (baseHp * finalHp / 100f);
        }

        public static bool GetMatchDebuffType(this CharacterDebuff DeBuff, PlayerAvatar Player, Type Base)
        {
            bool ElectricIce = Player.GetCustomStatUnsafe(TextFile.ElectricIce) > 0;
            bool Plasma = Player.GetCustomStatUnsafe(TextFile.PlasmaActive) > 0;
            if (Base == typeof(CharacterDebuff_Burn))
            {
                if (DeBuff is CharacterDebuff_Burn || DeBuff is CharacterDebuff_Plasma)
                {
                    return true;
                }
                if (DeBuff is CharacterDebuff_Frostbite && ElectricIce)
                {
                    return true;
                }
            }
            if (Base == typeof(CharacterDebuff_Electric))
            {
                if (DeBuff is CharacterDebuff_Electric || DeBuff is CharacterDebuff_Plasma)
                {
                    return true;
                }
                if (DeBuff is CharacterDebuff_Frostbite && ElectricIce)
                {
                    return true;
                }
            }
            if (Base == typeof(CharacterDebuff_Freeze))
            {
                if (DeBuff is CharacterDebuff_Freeze || DeBuff is CharacterDebuff_Frostbite)
                {
                    return true;
                }
            }
            return DeBuff.GetType() == Base;
        }

        public static float GetExMP(UnitAvatar __instance)
        {
            float baseHp = __instance.MaxMp;
            return baseHp - 50;
        }

        public static bool CostumeSkinUse(string playerName, string costumeID, string skinID, bool Net = true)
        {
            if (!ModConfig.Instance.EnableCostumeModifier || !NetworkServer.active && Net) return false;

            if (!StarSephiriaMod.CostumeType.TryGetValue(playerName, out string useCostume) || costumeID != useCostume)
                return false;

            if (!StarSephiriaMod.CostumeSkinDate.TryGetValue(costumeID + skinID, out string skinData))
                return false;

            if (!StarSephiriaMod.CostumeSkinType.TryGetValue(playerName, out string skin))
                return false;

            return skinData == skin;
        }

        public static bool CostumeSkinUse(string UseSkinName, string CostumeSkinID)
        {
            if (!ModConfig.Instance.EnableCostumeModifier) return false;

            if (!StarSephiriaMod.CostumeSkinDate.TryGetValue(CostumeSkinID, out string skinData))
                return false;

            return UseSkinName == CostumeSkinID;
        }

        public static Vector2 RotateTarget(Vector3 playerPos, Vector2 targetPos, float angleDegrees)
        {
            Vector2 player2D = new Vector2(playerPos.x, playerPos.y);
            Vector2 dir = targetPos - player2D;

            float angle = Mathf.Atan2(dir.y, dir.x) + angleDegrees * Mathf.Deg2Rad;
            float length = dir.magnitude;

            return player2D + new Vector2(Mathf.Cos(angle) * length, Mathf.Sin(angle) * length);
        }

        public static int CustomStatMult(this int result, float Multiplier, float BaseVale = 100)
        {
            return (int)((BaseVale + result) * Multiplier);
        }

        public static int GetComboCount(this UnitAvatar avatar, string ComboName)
        {
            if (avatar != null && avatar.Inventory != null)
            {
                ComboEffectBase comboEffect = avatar.Inventory.FindComboEffect(ComboName);
                if (comboEffect != null && comboEffect.isEnabled)
                {
                    return Mathf.Min(comboEffect.NetworkcomboCount / 2, 5);
                }
            }
            return 0;
        }

        public static bool IsMove()
        {
            bool hasMoveInput = Keyboard.current != null && (
                                Keyboard.current.aKey.isPressed ||
                                Keyboard.current.wKey.isPressed ||
                                Keyboard.current.sKey.isPressed ||
                                Keyboard.current.dKey.isPressed);
            return hasMoveInput;
        }

        public static int IntMult(this int Base, float Mult)
        {
            return (int)(Base * Mult);
        }

        public static float WeaponDamageMult(this float result, float Orig, float Target)
        {
            return result / Orig * Target;
        }

        public static bool GetWeaponType(this UnitAvatar Player, int TargetType)
        {
            if(StarSephiriaMod.WeaponType.TryGetValue(Player.Name, out int WeaponType) && WeaponType == TargetType)
                return true;
            return false;
        }

        public static bool GetWeaponTypeAndData(this UnitAvatar Player, out int Weapon, out (string, int) Data)
        {
            Weapon = -1; Data = ("Nothing", -1);
            if (StarSephiriaMod.WeaponType.TryGetValue(Player.Name, out int WeaponType))
            {
                Weapon = WeaponType;
                if (WeaponPatch.DamageIdx.TryGetValue((Player.Name, WeaponType), out (string, int) ForIdx))
                {
                    Data = ForIdx;
                    WeaponPatch.DamageIdx[(Player.Name, WeaponType)] = ("Nothing", -1);
                    return true;
                }
                return true;
            }
            return false;
        }

        public static float NegligenceAttackMult(float Attack, float Mult)
        {
            return (1f - 1f / (1f + Attack * 0.01f)) * Mult;
        }

        public static bool SafeRemove<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary.Remove(key);
                return true;
            }
            return false;
        }

        public static float StackDamageMult(int Stack, float Mult)
        {
            float AddDamageMult = 1f - Mult;
            return (Stack - AddDamageMult * (Stack - 1)) / Stack;
        }

        public static string CostumeText(this string costumeText)
        {
            try
            {
                if (string.IsNullOrEmpty(costumeText))
                {
                    return "无效果";
                }
                string processedText = costumeText.Replace("[绿]", "").Replace("[红]", "").Trim();
                return processedText;
            }
            catch (Exception ex)
            {
                StarSephiriaMod.LogError($"处理服装文本失败：{ex.Message}");
                return "效果加载中";
            }
        }

        public static Dictionary<string, GameObject> CachePro = new Dictionary<string, GameObject>();
        public static bool GetPro(string Name, out GameObject? Pro)
        {
            if (!NetworkServer.active)
            {
                Pro = null;
                return false;
            }
            if (CachePro.TryGetValue(Name, out GameObject CacheTargetPro))
            {
                Pro = CacheTargetPro;
                return true;
            }
            if (StarSephiriaPatches.Projectile.TryGetValue(Name, out GameObject TargetPro))
            {
                Pro = TargetPro;
                CachePro[Name] = TargetPro;
                return true;
            }
            Pro = null;
            StarSephiriaMod.LogError($"没有弹幕预制件：{Name}");
            return false;
        }

        public static bool GetKeyWordToStatus(string KeyWordName, out string StatusName)
        {
            if (StarSephiriaMod.KeyWordToStatusName.TryGetValue(KeyWordName, out string Value))
            {
                StatusName = Value;
                return true;
            }
            StarSephiriaMod.LogError($"不存在[{KeyWordName}]对应的Status");
            StatusName = "";
            return false;
        }

        public static Sprite? LoadEmbeddedSprite(string Name)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                using Stream stream = assembly.GetManifestResourceStream($"SPMod.Sprites.{Name}.png");
                if (stream == null)
                {
                    StarSephiriaMod.LogError($"找不到嵌入资源：{Name}，请检查命名和生成操作");
                    return null;
                }

                byte[] imageData = new byte[stream.Length];
                stream.Read(imageData, 0, imageData.Length);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.LoadImage(imageData);
                Sprite a = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 16f);
                a.name = Name;
                a.texture.filterMode = FilterMode.Point;
                return a;
            }
            catch (Exception ex)
            {
                StarSephiriaMod.LogError($"加载嵌入图标失败：{ex.Message}");
                return null;
            }
        }

        // 值计算辅助方法
        public static float CalculateValue(float Base, float Mult)
        {
            int Stack = DungeonPatchs.NumberofReincarnations;
            if (Stack <= 0) return Base;

            Stack -= 1;
            float BaseMult = Mult;
            while (Stack > 0)
            {
                Mult *= BaseMult;
                Stack -= 1;
            }
            Mult += 4f;
            return Base * Mult;
        }

        public static void SaveList(string prefix, List<string> dict, bool falg)
        {
            foreach (var kvp in dict)
            {
                SaveManager.CurrentRun.SetBool(prefix + $"_{kvp}", falg);
            }
        }

        public static Dictionary<string, bool> LoadList(string prefix, List<string> dict)
        {
            Dictionary<string, bool> result = new Dictionary<string, bool>();
            foreach (var kvp in dict)
            {
                if (SaveManager.CurrentRun.GetBool(prefix + $"_{kvp}", false))
                    result.Add(kvp, true);
                else
                    result.Add(kvp, false);
            }
            return result;
        }
    }
}