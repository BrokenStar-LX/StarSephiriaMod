using HarmonyLib;
using Mirror;
using SPMod.ModContent;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static ComboEffectBase;
using static Miracle;
using static SPMod.Tool.ModuleComponents;
using static UnityEngine.GraphicsBuffer;
using Object = UnityEngine.Object;

namespace SPMod.Tool
{
    public class ModAssetId : MonoBehaviour
    {
        public uint AssetId = 0u;
    }
    public static class ModuleComponents
    {
        public class Counter
        {
            public int Count = 0;
            public int MaxCount;
            public Timer UseTimer;
            public Counter(int count, float T)
            {
                MaxCount = count;
                UseTimer = new Timer(T, true, true);
            }
            public bool Update(float Speed, out bool Take)
            {
                Take = false;
                if (Count > MaxCount) Take = true;
                if (UseTimer.Update(Speed)) return true;
                return false;
            }
        }

        public class CountingTimer
        {
            public int Count;
            public Timer UseTimer;
            public CountingTimer(int count, float T)
            {
                Count = count;
                UseTimer = new Timer(T);
            }
            public bool Update(float Speed)
            {
                if (UseTimer.Update(Speed))
                {
                    if (Count > 0)
                    {
                        Count -= 1;
                        return true;
                    }
                }
                return false;
            }
            public bool Active()
            {
                return Count > 0;
            }
        }

        public class PollutedBloodData
        {
            public float CurrentHP;
            public float RecoveryHP = 0;
            public bool NoHPUp = false;
            public PollutedBloodData(float Current)
            {
                CurrentHP = Current;
            }
        }

        public class AllPlayerStatus
        {
            public int Defense = 0;
            public int AllDamage = 0;
            public int ElementalDamage = 0;
            public int Crit = 0;
            public AllPlayerStatus(int defense, int allDamage, int elementalDamage, int crit)
            {
                Defense = defense;
                AllDamage = allDamage;
                ElementalDamage = elementalDamage;
                Crit = crit;
            }
        }

        public class LegacyofTheIllusionistAttackData
        {
            public int Count;
            public float AttackTime;
            public float AttackSpeed = 1f;
            public float CharmDamageMult = 0f;
            public int AttackStack = 1;
            public bool Check = false;
            public LegacyofTheIllusionistAttackData(int count, float attackTime)
            {
                Count = count;
                AttackTime = attackTime;
            }

            public void Update(WeaponSimple_GreatSword Weapon, UnitAvatar Player)
            {
                if (Count > 0)
                {
                    if (AttackTime <= 0f)
                    {
                        UnitAvatar Target = PlayerInputController.SearchTargetNearestPoint(Player, Player.transform.position, 40f);
                        if (Target != null)
                        {
                            Weapon.StartCoroutine(LegacyofTheIllusionistShootBullt(Weapon, Player, Target));
                            Count -= 1;
                            AttackTime += 1f;
                        }
                    }
                    else AttackTime -= Time.deltaTime * AttackSpeed;
                }
            }

            private IEnumerator LegacyofTheIllusionistShootBullt(WeaponSimple_GreatSword Weapon, UnitAvatar Player, UnitAvatar Target)
            {
                if (!Player.IsInBattle)
                    yield break;
                if (Target == null)
                    yield break;
                if (!Tools.GetPro("GreatswordLaser", out GameObject? Pro))
                    yield break;

                for(int i = 0; i < AttackStack; i++)
                {
                    Vector3 Pos = Weapon.FirePosition(0);
                    Vector3 Vec = Vector3.Normalize(Target.transform.position - Pos);
                    string Id = "[Star's Sephiria Mod]光棱镜";
                    float Damage = Tools.BaseStatToMax(Player) * 2f;
                    float Mult = 1f + Player.GetCustomStatUnsafe(TextFile.MagicDamageBonus) * 0.01f;
                    Mult *= 1f + Player.GetCustomStatUnsafe(TextFile.MPSkillDamage) * 0.01f;
                    Mult *= 1f + CharmDamageMult;
                    Damage *= Mult;
                    Bullet a = Bullet.Pool.Spawn(Pro, Pos, true, EDamageFromType.DirectAttack, Id, Damage, 0, 0f, Player, Player.GetHostileFactionLayers(EDamageFromType.DirectAttack), 0.3f, Pos, Pos + Vec * 3f, null, null, 0f, EDamageElementalType.Chaos, true);
                    a.useCustomColorType = Bullet.ECustomColorType.Chaos;
                    a.customColor = EffectConstant.ChaosColor;
                    a.additionalCriticalChancePercent += Player.GetCustomStatUnsafe(TextFile.MagicCritical) * 0.01f;
                    a.additionalCriticalDamageRate += Player.GetCustomStatUnsafe(TextFile.MagicCriticalDamageBonus);

                    yield return new WaitForSeconds(0.3f / AttackSpeed);
                }
            }
        }

        public class SuccessorBoltData
        {
            public EDamageElementalType Elemental = EDamageElementalType.Normal;
            public float Base = 0f;
            public float Mult = 1f;
            public int Count = 1;
            public bool isMultiShot = false;
            public int MultiShotCount = 1;
            public float MultiShotMult = 1f;
            public float CharmDamageMult = 1f;
            public static FieldInfo isMultiShotField = AccessTools.Field(typeof(ActiveSkill_Bolt), "isMultiShot");
            public static FieldInfo multiShotCountField = AccessTools.Field(typeof(ActiveSkill_Bolt), "multiShotCount");
            public static FieldInfo multiShotDamageRatioField = AccessTools.Field(typeof(ActiveSkill_Bolt), "multiShotDamageRatio");
            public SuccessorBoltData(ActiveSkill_Bolt Bolt, float CharmMult = 1f, int ExAttackStack = 1)
            {
                Elemental = Bolt.elementalType;
                Base = Bolt.defaultDamageByLevel.SafeRandomAccess(Bolt.Level);
                Mult = Bolt.damagePercentByLevel.SafeRandomAccess(Bolt.Level);
                Count = (int)Bolt.fireCount * ExAttackStack;
                isMultiShot = (bool)isMultiShotField.GetValue(Bolt);
                MultiShotCount = (int)multiShotCountField.GetValue(Bolt);
                MultiShotMult = (float)multiShotDamageRatioField.GetValue(Bolt);
                CharmDamageMult = CharmMult;
            }
        }

        public class CloudAttackData
        {
            public bool StartAttack = false;
            public UnitAvatar Player;
            public UnitAvatar Target;
            public float DamageMult;
            public int Count;
            public float AttackCooling;
            public CloudAttackData(UnitAvatar player, UnitAvatar target, float damageMult = 1f, float attackCooling = 0.2f, int count = 1)
            {
                Player = player;
                Target = target;
                DamageMult = damageMult;
                AttackCooling = attackCooling;
                Count = count;
            }
            public IEnumerator UpdateCloudAttack(ComboEffect_DarkCloud ComboEffect)
            {
                if (ComboEffect == null) yield break;
                if (ComboEffect.comboCount < 2) yield break;
                if (ComboEffect.darkCloud <= 0) yield break;
                if (Target == null) yield break;
                if (Target.IsDead) yield break;

                for (int i = 0; i < Count; i++)
                {
                    ComboEffect.UseCloudToTarget(Target, (int)(DamageMult * 100f));
                    yield return new WaitForSeconds(AttackCooling);
                }
                StarSephiriaPatches.ModCloudAttack.Remove(this);
                yield break;
            }
        }

        public class NoHitRecord
        {
            public bool InBattle = false;
            public int CurrenHit;
            public NoHitRecord(int Curren)
            {
                CurrenHit = Curren;
            }
        }

        public class DPSRecord
        {
            public bool UseDPS = false;
            public bool InBattle = false;
            public bool EndBattle = false;
            public float TrueDuration = 0f;
            public float Duration = 0f;
            public float TotalDamage = 0f;
            public float DPS => TrueDuration > 1f ? TotalDamage / TrueDuration : TotalDamage;
        }

        public class ModHardModeShardEntity
        {
            public string Name;
            public string Description;
            public string ShardKey;
            public EHardModeShardType ShardType;
            public int ShardMaxLevel;
            public int[] EffectPerLevel;
            public int[] StepIncreasePerLevel;
            public ModHardModeShardEntity(string name, string shardKey, EHardModeShardType shardType, int shardMaxLevel, int[] effectPerLevel, int[] stepIncreasePerLevel)
            {
                Name = "ModHardModeShard_" + name + "_Name";
                Description = "ModHardModeShard_" + name + "_Description";
                ShardKey = shardKey;
                ShardType = shardType;
                ShardMaxLevel = shardMaxLevel;
                EffectPerLevel = effectPerLevel;
                StepIncreasePerLevel = stepIncreasePerLevel;
            }
            public HardModeShardEntity EndCreate()
            {
                HardModeShardEntity a = ScriptableObject.CreateInstance<HardModeShardEntity>();
                a.aName = new LocalizedString(Name);
                a.aDescription = new LocalizedString(Description);
                a.shardKey = ShardKey;
                a.shardType = ShardType;
                a.shardMaxLevel = ShardMaxLevel;
                a.effectPerLevel = EffectPerLevel;
                a.stepIncreasePerLevel = StepIncreasePerLevel;
                return a;
            }
        }

        public class ModMiracle
        {
            public string ID;
            public string Categories;//连击
            public GameObject? ThisMiracle = null;
            public Object? Prefab = null;
            public List<Miracle_StatusInstance.StatInfo> statList = new List<Miracle_StatusInstance.StatInfo>();
            public ModMiracle(string id, string categories = "")
            {
                ID = id;
                Categories = categories;
            }
            public ModMiracle SetStat(params string[] Stat)
            {
                statList = Tools.SetStatInfo(Stat);
                return this;
            }
            public ModMiracle EndCreate()
            {
                GameObject ModPrefab = new GameObject($"Miracle_{ID}");
                Object.DontDestroyOnLoad(ModPrefab);
                Miracle_StatusInstance NewMiracle = ModPrefab.AddComponent<Miracle_StatusInstance>();
                ModAssetId NetId = NewMiracle.AddComponent<ModAssetId>();
                NetId.AssetId = LoadModContent.LoadModAssetId();
                NewMiracle.isEnabled = true;
                NewMiracle.id = ID;
                NewMiracle.autoGenerateEffectString = true;
                NewMiracle.aName = new LocalizedString($"Miracle_{ID}");
                Sprite? Icon = Tools.LoadEmbeddedSprite($"Miracle_{ID}");
                if (Icon != null)
                    NewMiracle.miracleImage = Icon;
                NewMiracle.statInfo = statList.ToArray();
                NewMiracle.effects = Array.Empty<Effect>();
                if (Categories != "") NewMiracle.categories = new string[1] { Categories };
                ThisMiracle = ModPrefab;
                Prefab = NewMiracle;
                StarSephiriaMod.registered[NetId.AssetId] = NewMiracle;
                return this;
            }
        }

        public class ModItemCategory
        {
            public string ID;//连击ID
            public string Name;//连击名
            public string FruitIco;//水果名
            public GameObject Prefab;
            public List<ComboStat> Stat = new List<ComboStat>();//连击属性
            public ModItemCategory(string id, string name, string fruitIco)
            {
                ID = id;
                Name = name;
                Prefab = CreatePrefab();
                FruitIco = fruitIco;
            }
            public GameObject CreatePrefab()
            {
                GameObject ModPrefab = new GameObject($"ModItemCategory_{Name}");
                ComboEffectBase Effect = ModPrefab.AddComponent<ComboEffectBase>();
                Effect.defaultEffect = new LocalizedString("");
                Effect.enabled = false;
                ModAssetId NetId = ModPrefab.AddComponent<ModAssetId>();
                NetId.AssetId = LoadModContent.LoadModAssetId();
                Object.DontDestroyOnLoad(ModPrefab);
                StarSephiriaMod.registered[NetId.AssetId] = ModPrefab;
                return ModPrefab;
            }
            public ModItemCategory UseStat()
            {
                if (Prefab.TryGetComponent<ComboEffectBase>(out ComboEffectBase Effect))
                {
                    Effect.addStatByCombo = Stat.ToArray();
                }
                return this;
            }
            public ItemCategoryEntity EndCreate()
            {
                ItemCategoryEntity ModCategory = ScriptableObject.CreateInstance<ItemCategoryEntity>();
                ModCategory.id = ID;
                ModCategory.categoryName = new LocalizedString($"ModItemCategory_{Name}");
                ModCategory.categoryFruitName = new LocalizedString($"ModItemCategory_{Name}Fruit");
                ModCategory.categoryIcon = Tools.LoadEmbeddedSprite($"Categories_{Name}");
                ModCategory.categoryFruitIcon = Tools.LoadEmbeddedSprite($"Fruits_{FruitIco}0");
                ModCategory.categoryIceFruitIcon = Tools.LoadEmbeddedSprite($"Fruits_{FruitIco}1");
                ModCategory.comboEffectPrefab = Prefab;
                return ModCategory;
            }
            public ModItemCategory SetStat(ComboStat stat)
            {
                Stat.Add(stat);
                return this;
            }
        }

        public class ModKeyWord
        {
            public string ID = "";//关键词ID
            public bool DisplayDetails = false;//是否使用提示框
            public Color TextColor;//关键词颜色
            public string UseKeyWordSpriteName = "";//使用模组关键词
            public Sprite? Image = null;//使用原版关键词贴图
            public StatusEntity? Status = null;
            public ModKeyWord(string id, bool displayDetails, Color color)
            {
                ID = id;
                DisplayDetails = displayDetails;
                TextColor = color;
            }
            public KeywordEntity EndCreate()
            {
                KeywordEntity ModKeyword = ScriptableObject.CreateInstance<KeywordEntity>();
                ModKeyword.keyword = ID;
                ModKeyword.visualText = new LocalizedString($"ModKeyword_{ID}");
                ModKeyword.textColor = TextColor;
                ModKeyword.keywordImage = Image;
                ModKeyword.displayDetails = DisplayDetails;
                return ModKeyword;
            }
            public ModKeyWord StringToSprite(string Name)
            {
                Image = Tools.LoadEmbeddedSprite(Name);
                return this;
            }
            public ModKeyWord UseKeyWordSprite(string Name)
            {
                UseKeyWordSpriteName = Name;
                return this;
            }
            public ModKeyWord CreateStatus(string StatusID, string Symbol = "", bool useLocalized = false)
            {
                StatusEntity ModStatusEntity = ScriptableObject.CreateInstance<StatusEntity>();
                ModStatusEntity.className = $"StatusInstance_Custom/{ID}";
                ModStatusEntity.statKeyword = ID;
                ModStatusEntity.id = StatusID;
                if(useLocalized)
                {
                    ModStatusEntity.useLocalizedSymbol = useLocalized;
                    ModStatusEntity.localizedSymbol = new LocalizedString($"ModKeyword_{ID}_Symbol");
                }
                else ModStatusEntity.symbol = Symbol;
                Status = ModStatusEntity;
                return this;
            }
        }

        public class ModItem
        {
            public string Name;
            public bool FlavorText;
            public int CustomCost = -1;
            public int SapphirePrice = 3;
            public EItemRarity Rarity;
            public GameObject Prefab;
            public ModItem(string name, EItemRarity rarity = EItemRarity.Common, bool flavor = false)
            {
                Name = name;
                Rarity = rarity;
                FlavorText = flavor;
                Prefab = CreatePrefab();
            }
            private GameObject CreatePrefab()
            {
                GameObject ModPrefab = new GameObject($"ModItemEntity_{Name}");
                uint AssetId = LoadModContent.LoadModAssetId();
                ModAssetId Asset = ModPrefab.AddComponent<ModAssetId>();
                Asset.AssetId = AssetId;
                StarSephiriaMod.registered[Asset.AssetId] = ModPrefab;
                Object.DontDestroyOnLoad(ModPrefab);
                return ModPrefab;
            }
            public ModItem SetCost(int cost, int sapphirePrice = 3)
            {
                CustomCost = cost;
                SapphirePrice = sapphirePrice;
                return this;
            }
            public ItemEntity CreateItem(bool IsDual = false, bool IsDestroyedOnDiscard = false)
            {
                ItemEntity ModItem = ScriptableObject.CreateInstance<ItemEntity>();
                ModItem.name = "ModItemEntity_" + Name;
                if (IsDual)
                {
                    ModItem.rarity = EItemRarity.Rare;
                    ModItem.isDual = true;
                }
                else ModItem.rarity = Rarity;
                ModItem.activeType = EItemActiveType.Default;
                ModItem.aName = new LocalizedString("ModItemEntity_" + Name);
                string Flavor = "";
                if (FlavorText) Flavor = "ModItemEntity_" + Name + "_Flavor";
                ModItem.aFlavorText = new LocalizedString(Flavor);
                ModItem.resourcePrefab = Prefab;
                ModItem.icon = Tools.LoadEmbeddedSprite($"Item_{Name}");
                ModItem.isDestroyedOnDiscard = IsDestroyedOnDiscard;
                ModItem.sapphirePrice = SapphirePrice;
                if (CustomCost >= 0) ModItem.cost = CustomCost;
                else
                {
                    switch(ModItem.rarity)
                    {
                        case EItemRarity.Common:
                            ModItem.cost = EffectConstant.CommonCost;
                            break;
                        case EItemRarity.Uncommon:
                            ModItem.cost = EffectConstant.UncommonCost;
                            break;
                        case EItemRarity.Rare:
                            ModItem.cost = EffectConstant.RareCost;
                            break;
                        case EItemRarity.Legend:
                        case EItemRarity.Eternal:
                            ModItem.cost = EffectConstant.LegendCost;
                            break;
                    }
                }
                return ModItem;
            }
        }

        public class ModEffectHUD
        {
            public string Name;
            public bool FlavorText;
            public bool UseNumText;
            public ModEffectHUD(string name, bool Text = false, bool flavor = false)
            {
                Name = name;
                FlavorText = flavor;
                UseNumText = Text;
            }
            public void SetEffectHUD(GameObject prefab)
            {
                UI_EffectHUD_Basic component = prefab.GetComponent<UI_EffectHUD_Basic>();
                component.effectName = new LocalizedString($"ModEffectHUD_{Name}");
                component.effectFlavorText = FlavorText ? TextFile.GetModTooltip($"ModEffectHUD_{Name}_Effect") : new LocalizedString("");
                component.effectType = UI_EffectHUD_Basic.EEffectType.Condition;
                if (prefab.transform is RectTransform rectTransform)
                {
                    Image component2 = rectTransform.GetChild(1).GetComponent<Image>();
                    component2.sprite = Tools.LoadEmbeddedSprite($"ModEffectHUD_{Name}");
                }
                if (component is UI_EffectHUD_Stack Stack && !UseNumText)
                {
                    Stack.stackText.text = "";
                }
            }

            public EffectHUDEntity EndCreate(GameObject prefab)
            {
                EffectHUDEntity HUD = ScriptableObject.CreateInstance<EffectHUDEntity>();
                HUD.name = "ModEffectHUD_" + Name;
                HUD.id = Name;
                HUD.hudPrefab = prefab;
                return HUD;
            }
        }
    }
}