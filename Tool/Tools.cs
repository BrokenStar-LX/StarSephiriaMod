using Mirror;
using SPMod.ModContent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static AvatarStatsHooker;
using static Miracle_StatusInstance;
using static SPMod.Tool.ModuleComponents;
using StatusGroup = Charm_StatusInstance.StatusGroup;

namespace SPMod.Tool
{
    public static class Tools
    {
        public static void CopyPotionFX(this ItemEntity Entity, int VanillaId)
        {
            ItemEntity? Vanilla = ItemDatabase.FindItemById(VanillaId);
            if (Vanilla == null || Vanilla.resourcePrefab == null || Entity.resourcePrefab == null) return;

            WieldingPotion? SourcePotion = Vanilla.resourcePrefab.GetComponent<WieldingPotion>();
            PotionEffect? SourceEffect = Vanilla.resourcePrefab.GetComponent<PotionEffect>();
            WieldingPotion? TargetPotion = Entity.resourcePrefab.GetComponent<WieldingPotion>();
            PotionEffect? TargetEffect = Entity.resourcePrefab.GetComponent<PotionEffect>();

            if (TargetPotion != null && SourcePotion != null)
            {
                TargetPotion.drinkFxPrefab = SourcePotion.drinkFxPrefab;
                TargetPotion.potionDrinkingTime = SourcePotion.potionDrinkingTime;
            }
            if (TargetEffect != null && SourceEffect != null)
            {
                TargetEffect.gulpSoundEvent = SourceEffect.gulpSoundEvent;
                TargetEffect.drinkSoundEvent = SourceEffect.drinkSoundEvent;
                TargetEffect.canUseInBattle = SourceEffect.canUseInBattle;
            }
            if (TargetEffect is PotionEffect_StatusInstance TargetStatus && SourceEffect is PotionEffect_StatusInstance SourceStatus)
            {
                TargetStatus.effectFxPrefab = SourceStatus.effectFxPrefab;
                TargetStatus.effectSoundEvent = SourceStatus.effectSoundEvent;
                TargetStatus.statLogString = SourceStatus.statLogString;
                TargetStatus.statLogEndString = SourceStatus.statLogEndString;
                TargetStatus.changeString_Up = SourceStatus.changeString_Up;
                TargetStatus.changeString_Down = SourceStatus.changeString_Down;
            }
        }
        public static AllPlayerStatus GetMaxPlayerStatus()
        {
            int Defense = 0, AllDamage = 0, Elemental = 0, Crit = 0;
            foreach (PlayerSpawner Spawner in PlayerSpawner.MultiplayerList)
            {
                PlayerAvatar Player = Spawner.PlayerAvatar;
                if (Player == null) continue;
                Defense = Math.Max(Defense, Player.GetCustomStatUnsafe(TextFile.AllPlayerDefense));
                AllDamage = Math.Max(AllDamage, Player.GetCustomStatUnsafe(TextFile.AllPlayerAllDamage));
                Elemental = Math.Max(Elemental, Player.GetCustomStatUnsafe(TextFile.AllPlayerElementalDamage));
                Crit = Math.Max(Crit, Player.GetCustomStatUnsafe(TextFile.AllPlayerCrit));
            }
            int Count = PlayerSpawner.MultiplayerList.Count;
            float Mult = Count <= 1 ? 1f : 1f / ((Count - 1) * 0.5f);
            return new AllPlayerStatus(Defense.IntMult(Mult), AllDamage.IntMult(Mult), Elemental.IntMult(Mult), Crit.IntMult(Mult));
        }

        public static int GetRosionStack(this UnitAvatar Player)
        {
            int i = Player.GetCustomStatUnsafe(TextFile.CurrentRosion);
            if (ModConfig.HardMode >= 4) i *= 3;
            if (PlayerSpawner.MultiplayerList.Count > 1) return Mathf.Min(90, i);  
            return i;
        }

        public static List<string> SetCategories(params string[] categories)
        {
            return categories.ToList();
        }

        public static List<StatInfo> SetStatInfo(params string[] stat)
        {
            List<StatInfo> infos = [];
            foreach (string statName in stat)
            {
                string[] parts = statName.Split('=');
                string left = parts[0];
                string right = parts[1];
                StatInfo statInfo = new() { status = left };
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

        public static void UpdateModEffectTime(this UnitAvatar __instance, ref Timer Target, Action<UnitAvatar>? Effect = null, Action<UnitAvatar>? End = null)
        {
            Effect?.Invoke(__instance);
            if (Target.Update(Time.deltaTime))
            {
                End?.Invoke(__instance);
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

        /// <summary>神器的武器值（AttackWeightPerSwing）。WeaponController 只在服务端 Connect 赋值，客机用 avatar 兜底。</summary>
        public static float GetWeaponWeight(this Charm_Basic Charm, UnitAvatar? Avatar = null)
        {
            WeaponControllerSimple? Controller = Charm.WeaponController;
            if (Controller == null && Avatar != null && Avatar.TryGetComponent<WeaponControllerSimple>(out WeaponControllerSimple AvatarController))
                Controller = AvatarController;
            if (Controller != null && Controller.currentWeapon != null)
                return Mathf.Max(0f, Controller.currentWeapon.AttackWeightPerSwing);
            return 0f;
        }

        public static WeaponEntity SetAddons(this WeaponEntity Weapon, Action<WeaponAddon[]> action)
        {
            if (Weapon.mainWeaponPrefab == null)
                return Weapon;
            if (Weapon.mainWeaponPrefab.TryGetComponent<WeaponSimple>(out WeaponSimple Target))
            {
                action.Invoke(Target.addons);
            }
            return Weapon;
        }

        public static WeaponAddonCommon_Status.Stat SetAddonStatus(ECustomStat statName, int V)
        {
            return new WeaponAddonCommon_Status.Stat
            {
                statId = statName,
                value = V
            };
        }

        public static WeaponEntity SetStatus(this WeaponEntity Weapon, params WeaponAddonCommon_Status.Stat[] Stats)
        {
            if (Weapon.mainWeaponPrefab == null)
                return Weapon;
            if (Weapon.mainWeaponPrefab.TryGetComponent<WeaponSimple>(out WeaponSimple Target))
            {
                foreach (var Addon in Target.addons)
                {
                    if (Addon is WeaponAddonCommon_Status V)
                    {
                        V.status = Stats;
                    }
                }
            }
            return Weapon;
        }

        public static WeaponAddonCommon_StatusUnsafe.Stat SetAddonStatusUnsafe(string statName, int V, string Symbol = "")
        {
            return new WeaponAddonCommon_StatusUnsafe.Stat
            {
                statId = statName,
                value = V,
                displaySymbol = Symbol
            };
        }

        public static WeaponEntity SetStatusUnsafe(this WeaponEntity Weapon, params WeaponAddonCommon_StatusUnsafe.Stat[] Stats)
        {
            if (Weapon.mainWeaponPrefab == null)
                return Weapon;
            if (Weapon.mainWeaponPrefab.TryGetComponent<WeaponSimple>(out WeaponSimple Target))
            {
                foreach(var Addon in Target.addons)
                {
                    if (Addon is WeaponAddonCommon_StatusUnsafe V)
                    {
                        V.status = Stats;
                    }
                }
            }
            return Weapon;
        }

        public static WeaponEntity SetAddonStat(this WeaponEntity Weapon, string StatId, int Value)
        {
            if (Weapon.mainWeaponPrefab == null)
                return Weapon;
            if (!Weapon.mainWeaponPrefab.TryGetComponent<WeaponSimple>(out WeaponSimple Target))
                return Weapon;

            bool Found = false;
            foreach (WeaponAddon Addon in Target.addons)
            {
                if (Addon is WeaponAddonCommon_Status Status)
                {
                    foreach (WeaponAddonCommon_Status.Stat S in Status.status)
                    {
                        if (string.Equals(S.statId.ToString(), StatId, StringComparison.OrdinalIgnoreCase))
                        {
                            S.value = Value;
                            Found = true;
                            break;
                        }
                    }
                }
                if (Found) break;
                if (Addon is WeaponAddonCommon_StatusUnsafe Unsafe)
                {
                    foreach (WeaponAddonCommon_StatusUnsafe.Stat S in Unsafe.status)
                    {
                        if (string.Equals(S.statId, StatId, StringComparison.OrdinalIgnoreCase))
                        {
                            S.value = Value;
                            Found = true;
                            break;
                        }
                    }
                }
                if (Found) break;
            }

            if (!Found)
            {
                if (Value == 0)
                    return Weapon;

                foreach (WeaponAddon Addon in Target.addons)
                {
                    if (Addon is WeaponAddonCommon_Status Status && Enum.TryParse(StatId, true, out ECustomStat EnumStat))
                    {
                        Status.status = Status.status.Append(new WeaponAddonCommon_Status.Stat
                        {
                            statId = EnumStat,
                            value = Value
                        }).ToArray();
                        Found = true;
                        break;
                    }
                    if (Addon is WeaponAddonCommon_StatusUnsafe Unsafe)
                    {
                        Unsafe.status = Unsafe.status.Append(new WeaponAddonCommon_StatusUnsafe.Stat
                        {
                            statId = StatId,
                            value = Value
                        }).ToArray();
                        Found = true;
                        break;
                    }
                }
            }

            if (!Found)
                StarSephiriaMod.LogError($"[SetAddonStat]{Weapon.id} 的 addon 中没有 {StatId}，{Value} 未应用");
            return Weapon;
        }

        public static WeaponEntity SetSimple(this WeaponEntity Weapon, Action<WeaponSimple> Action)
        {
            if (Weapon.mainWeaponPrefab == null)
                return Weapon;
            if (Weapon.mainWeaponPrefab.TryGetComponent<WeaponSimple>(out WeaponSimple Target))
                Action(Target);
            return Weapon;
        }

        public static WeaponEntity ChangeAddon<T>(this WeaponEntity Weapon, Action<T> Action) where T : WeaponAddon
        {
            if (Weapon.mainWeaponPrefab == null)
                return Weapon;
            if (!Weapon.mainWeaponPrefab.TryGetComponent<WeaponSimple>(out WeaponSimple Target))
                return Weapon;

            foreach (WeaponAddon Addon in Target.addons)
            {
                if (Addon is T Typed)
                {
                    Action(Typed);
                    return Weapon;
                }
            }
            StarSephiriaMod.LogError($"[ChangeAddon]{Weapon.id} 的 addon 中没有 {typeof(T).Name}");
            return Weapon;
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

        public static EDamageElementalType GetHighestDamageElemental(UnitAvatar avatar, out string BaseName)
        {
            return GetHighestDamageElemental(avatar, out BaseName, out _);
        }

        public static EDamageElementalType GetHighestDamageElemental(UnitAvatar avatar, out string BaseName, out int MaxValue)
        {
            MaxValue = int.MinValue;
            BaseName = TextFile.PhysicalDamage;
            int Count = 0;
            foreach (string Base in EffectConstant.BaseStatsName)
            {
                int Value = avatar.GetCustomStatUnsafe(Base);
                if (Value > MaxValue)
                {
                    MaxValue = Value;
                    Count = 1;
                    BaseName = Base;
                }
                else if (Value == MaxValue)
                {
                    Count++;
                    if (UnityEngine.Random.Range(0, Count) == 0)
                        BaseName = Base;
                }
            }
            if (EffectConstant.BaseStatusToElemental.TryGetValue(BaseName, out var Target))
                return Target;
            BaseName = TextFile.PhysicalDamage;
            return EDamageElementalType.Physical;
        }

        public static int BaseStatToMax(UnitAvatar Player)
        {
            int Max = int.MinValue;
            foreach (string BaseStat in EffectConstant.BaseStatsName)
            {
                int Value = Player.GetCustomStatUnsafe(BaseStat);
                if (Value > Max) Max = Value;
            }
            return Max;
        }

        public static int BaseStatToAverage(UnitAvatar Player)
        {
            int Sum = 0;
            foreach (string BaseStat in EffectConstant.BaseStatsName)
            {
                Sum += Player.GetCustomStatUnsafe(BaseStat);
            }
            return Sum / EffectConstant.BaseStatsName.Count;
        }

        public static int BaseStatToMin(UnitAvatar Player)
        {
            int Min = int.MaxValue;
            foreach (string BaseStat in EffectConstant.BaseStatsName)
            {
                int Value = Player.GetCustomStatUnsafe(BaseStat);
                if (Value < Min) Min = Value;
            }
            return Min;
        }

        public static int BaseOrigStatToMax(UnitAvatar Player)
        {
            List<int> Stat = [];
            foreach (string BaseStat in EffectConstant.BaseStatsName)
            {
                Stat.Add(Player.customStats.GetValueOrDefault(BaseStat, 0));
            }
            return Stat.Max();
        }

        public static float GetMaxLife(UnitAvatar __instance)
        {
            float baseHp = __instance.maxHp;
            float finalHp = __instance.finalMaxHp;
            return baseHp + (baseHp * finalHp / 100f);
        }

        public static bool GetMatchDebuffType(this CharacterDebuff DeBuff, UnitAvatar Player, Type Base)
        {
            if (DeBuff.NetworkAttacker != Player) return false;

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

        public static bool CostumeSkinUse(string playerName, string skinKey, bool Net = true)
        {
            if (!ModConfig.Instance.EnableCostumeModifier || (!NetworkServer.active && Net)) return false;

            return StarSephiriaMod.CostumeSkinKey.TryGetValue(playerName, out string useKey) && useKey == skinKey;
        }

        public static Vector2 RotateTarget(Vector3 playerPos, Vector2 targetPos, float angleDegrees)
        {
            Vector2 player2D = new(playerPos.x, playerPos.y);
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
                if (WeaponPatch.DamageIdx.TryGetValue((Player, WeaponType), out (string, int) ForIdx))
                {
                    Data = ForIdx;
                    WeaponPatch.DamageIdx[(Player, WeaponType)] = ("Nothing", -1);
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

        public static Dictionary<string, GameObject> CachePro = [];
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

        public static Dictionary<string, Sprite> ModSpriteList = [];
        public static Sprite? LoadModSprite(string Name)
        {
            return LoadModSpriteInternal("", Name);
        }
        public static Sprite? LoadModSpriteForPath(string Path, string Name)
        {
            return LoadModSpriteInternal(Path, Name);
        }
        private static Sprite? LoadModSpriteInternal(string Path, string Name)
        {
            string Key = string.IsNullOrEmpty(Path) ? Name : $"{Path}.{Name}";
            if (ModSpriteList.TryGetValue(Key, out var Value))
            {
                return Value;
            }
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string Resource = string.IsNullOrEmpty(Path) ? $"SPMod.Sprites.{Name}.png" : $"SPMod.Sprites.{Path}.{Name}.png";
                using Stream stream = assembly.GetManifestResourceStream(Resource);
                if (stream == null)
                {
                    StarSephiriaMod.LogError($"找不到嵌入资源：{Resource}，请检查命名和生成操作");
                    return null;
                }

                byte[] imageData = new byte[stream.Length];
                stream.Read(imageData, 0, imageData.Length);
                Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
                texture.LoadImage(imageData);
                texture.name = Name;
                Sprite a = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 16f);
                a.name = Name;
                a.texture.filterMode = FilterMode.Point;
                ModSpriteList[Key] = a;
                return a;
            }
            catch (Exception ex)
            {
                StarSephiriaMod.LogError($"加载嵌入图标失败：{ex.Message}");
                return null;
            }
        }

        public static List<TMP_SpriteAsset> InjectedKeywordSpriteAssets = [];
        public static void InjectKeywordSpriteAssets()
        {
            if (InjectedKeywordSpriteAssets.Count > 0) return;
            try
            {
                const string ResourcePrefix = "SPMod.Sprites.";
                const string IconPrefix = "KeywordIcon_";
                Assembly assembly = Assembly.GetExecutingAssembly();
                List<Sprite> Icons = [];
                foreach (string Resource in assembly.GetManifestResourceNames())
                {
                    if (!Resource.StartsWith(ResourcePrefix + IconPrefix) || !Resource.EndsWith(".png")) continue;
                    string Name = Resource.Substring(ResourcePrefix.Length, Resource.Length - ResourcePrefix.Length - 4);
                    Sprite? Icon = LoadModSprite(Name);
                    if (Icon != null) Icons.Add(Icon);
                }
                if (Icons.Count == 0)
                {
                    StarSephiriaMod.LogInfo("TMP 图标注入：没有找到 KeywordIcon_ 开头的嵌入图标");
                    return;
                }

                TMP_SpriteAsset Asset = CreateKeywordSpriteAsset(Icons);
                InjectedKeywordSpriteAssets.Add(Asset);

                TMP_SpriteAsset? Default = TMP_Settings.defaultSpriteAsset;
                if (Default != null) AddSpriteAssetFallback(Default, Asset);
                else TMP_Settings.defaultSpriteAsset = Asset;

                foreach (TMP_SpriteAsset Existing in Resources.FindObjectsOfTypeAll<TMP_SpriteAsset>())
                {
                    if (Existing != null && Existing.name == "Keyword") AddSpriteAssetFallback(Existing, Asset);
                }
                StarSephiriaMod.LogInfo($"TMP 图标注入：已注册 {string.Join(", ", Icons.Select(Icon => Icon.name))}");
            }
            catch (Exception ex)
            {
                StarSephiriaMod.LogError($"TMP 图标注入失败：{ex}");
            }
        }
        private static void AddSpriteAssetFallback(TMP_SpriteAsset Target, TMP_SpriteAsset Fallback)
        {
            if (Target == Fallback || Target.fallbackSpriteAssets == null) return;
            if (!Target.fallbackSpriteAssets.Contains(Fallback)) Target.fallbackSpriteAssets.Add(Fallback);
        }
        private static TMP_SpriteAsset CreateKeywordSpriteAsset(List<Sprite> Icons)
        {
            const int Padding = 2;
            int TotalWidth = Icons.Sum(Icon => Icon.texture.width) + Padding * (Icons.Count - 1);
            int TotalHeight = Icons.Max(Icon => Icon.texture.height);
            Texture2D Sheet = new(TotalWidth, TotalHeight, TextureFormat.RGBA32, false)
            {
                name = "ModKeyword",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Sheet.SetPixels(new Color[TotalWidth * TotalHeight]);

            TMP_SpriteAsset Asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            Asset.name = "Keyword";
            Asset.spriteSheet = Sheet;
            Asset.material = new Material(Shader.Find("TextMeshPro/Sprite"))
            {
                mainTexture = Sheet
            };
            Asset.spriteInfoList = [];

            int Offset = 0;
            foreach (Sprite Icon in Icons)
            {
                Texture2D Source = Icon.texture;
                Sheet.SetPixels(Offset, 0, Source.width, Source.height, Source.GetPixels());
                Asset.spriteInfoList.Add(new TMP_Sprite
                {
                    name = Icon.name,
                    sprite = Icon,
                    x = Offset,
                    y = 0,
                    width = Source.width,
                    height = Source.height,
                });
                Offset += Source.width + Padding;
            }
            Sheet.Apply(false);

            for (int i = 0; i < Asset.spriteInfoList.Count; i++)
            {
                TMP_Sprite Entry = Asset.spriteInfoList[i];
                Entry.id = i;
                Entry.scale = 1f;
                Entry.xAdvance = Entry.width;
                Entry.yOffset = Entry.height * 0.9f;
                Entry.unicode = 0xE000 + i;
                Entry.hashCode = TMP_TextUtilities.GetSimpleHashCode(Entry.name);
            }
            Asset.UpdateLookupTables();

            if (Asset.spriteCharacterTable != null && Asset.spriteGlyphTable != null)
            {
                for (int i = 0; i < Asset.spriteCharacterTable.Count && i < Asset.spriteGlyphTable.Count; i++)
                    Asset.spriteCharacterTable[i].glyph = Asset.spriteGlyphTable[i];
            }
            return Asset;
        }

        public static EStatValueSign GetStandardSign(float value, float defaultValue = 0f)
        {
            if (Mathf.Abs(value - defaultValue) <= float.Epsilon)
            {
                return EStatValueSign.Neutral;
            }

            if (value < defaultValue)
            {
                return EStatValueSign.Negative;
            }

            return EStatValueSign.Positive;
        }

        /// <summary>
        /// 搜索范围内所有敌对单位。
        /// </summary>
        /// <param name="owner">攻击者，用于排除自身和判断阵营</param>
        /// <param name="center">范围中心</param>
        /// <param name="radius">范围半径</param>
        /// <param name="attackingLayer"> 0=只打地面, 1=无视高度</param>
        /// <param name="includeBodySize">是否把目标身体体积补偿进范围</param>
        public static List<UnitAvatar> SearchEnemiesInRange(UnitAvatar owner, Vector3 center, float radius, int attackingLayer = 0, bool includeBodySize = false)
        {
            List<UnitAvatar> Target = [];
            foreach (UnitAvatar target in CombatManager.Instance.AllCreatures)
            {
                if (target == null || target == owner) continue;
                if (target.IsDead || target.hp <= 0f) continue;
                if (target.IsInvulnerable) continue;
                if (target.canBeTarget.IsFalse()) continue;
                if (!target.gameObject.activeSelf) continue;
                if (attackingLayer != -1 && target.TopdownActor != null && target.TopdownActor.HitboxLayer != attackingLayer) continue;
                if (!CombatManager.ContainsAttackableFaction(target.GetHostileFactionLayers(EDamageFromType.None), owner.faction)) continue;
                float checkRadius = radius;
                if (includeBodySize && target.TopdownActor != null)
                {
                    Vector2 size = target.TopdownActor.size;
                    checkRadius += Mathf.Max(size.x, size.y) * 0.5f;
                }
                Vector2 targetPos = target.transform.position;
                if ((targetPos - (Vector2)center).sqrMagnitude > checkRadius * checkRadius) continue;
                Target.Add(target);
            }
            return Target;
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
            Dictionary<string, bool> result = [];
            foreach (var kvp in dict)
            {
                if (SaveManager.CurrentRun.GetBool(prefix + $"_{kvp}", false))
                    result.Add(kvp, true);
                else
                    result.Add(kvp, false);
            }
            return result;
        }

        public static bool TryGetComponentInParent<T>(this Component Source, out T? Result, bool IncludeInactive = false) where T : Component
        {
            Result = null;
            if (!Source) return false;
            Result = Source.GetComponentInParent<T>(IncludeInactive);
            return Result != null;
        }

        public static bool TryGetComponentInParent<T>(this GameObject Source, out T? Result, bool IncludeInactive = false) where T : Component
        {
            Result = null;
            if (!Source) return false;
            Result = Source.GetComponentInParent<T>(IncludeInactive);
            return Result != null;
        }
    }
}