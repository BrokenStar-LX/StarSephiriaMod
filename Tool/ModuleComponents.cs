using HarmonyLib;
using Mirror;
using SPMod.ModContent;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static ComboEffectBase;
using static Miracle;
using static SPMod.Tool.ModuleComponents;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;
using Object = UnityEngine.Object;
using StatusGroup = Charm_StatusInstance.StatusGroup;

namespace SPMod.Tool
{
    public class ModAssetId : MonoBehaviour
    {
        public uint AssetId = 0u;
    }
    public class ModFxVisibility : MonoBehaviour
    {
        public Color BaseColor = Color.white;
        public float VisibilityAlpha = 1f;
        public bool UseLitMaterial = false;

        public void SetBaseColor(Color color)
        {
            BaseColor = color;
            Apply();
        }

        /// <summary>ownerIndex：0 = 队友 FX，1 = 自己的 FX，负数 = 不套用多人可见度（系数 1）。</summary>
        public void SetVisibility(int ownerIndex)
        {
            int Mode = 0;
            if (ownerIndex >= 0 && OptionsBinding.Instance != null && OptionsBinding.Instance.Options != null)
            {
                Mode = ownerIndex == 1 ? OptionsBinding.Instance.Options.GetInt("MyFX", 0) : OptionsBinding.Instance.Options.GetInt("PartyMemberFX", 1);
            }
            switch (Mode)
            {
                case 1: VisibilityAlpha = 0.5f; break;
                case 2: VisibilityAlpha = 0.15f; break;
                case 3: VisibilityAlpha = 0f; break;
                default: VisibilityAlpha = 1f; break;
            }
            UseLitMaterial = Mode >= 1 && Mode <= 3;
            Apply();
        }

        public void Apply()
        {
            SpriteFx_Simple? Fx = GetComponent<SpriteFx_Simple>();
            if (Fx == null || Fx.simpleAnimator2D == null) return;

            Material? Mat = UseLitMaterial && CombatManager.Instance != null
                ? CombatManager.Instance.spriteLitMaterial
                : Fx.simpleAnimator2D.defaultMaterial;

            foreach (SpriteRenderer Renderer in Fx.simpleAnimator2D.renderers)
            {
                if (Renderer == null) continue;
                Color C = BaseColor;
                C.a = BaseColor.a * VisibilityAlpha;
                Renderer.color = C;
                Renderer.sharedMaterial = Mat;
            }
        }
    }
    public class ModSpawnTemplate : MonoBehaviour
    {
        // 标记：该模板的复制体在 Instantiate 后需要激活
    }
    public class ModUnitTemplate : ModSpawnTemplate
    {
        // 单位模板标记
    }
    public class ModBulletTemplate : ModSpawnTemplate
    {
        // 子弹模板标记
    }
    public class ModBulletSpeed : MonoBehaviour
    {
        public float Scale = 1f;
        private Bullet? bullet;

        private void Awake()
        {
            bullet = GetComponent<Bullet>();
        }

        private void OnEnable()
        {
            if (bullet != null) bullet.speedScale = Scale;
        }

        private void Update()
        {
            if (bullet != null && bullet.speedScale != Scale) bullet.speedScale = Scale;
        }
    }
    public class ModSpriteMap : MonoBehaviour
    {
        public string Key = "";
    }
    public static class ModuleComponents
    {
        public class SpriteReplaceData
        {
            public Func<UnitAvatar, bool> UseReplace;
            public Dictionary<string, Sprite> Sprites = [];
            public SpriteReplaceData(Func<UnitAvatar, bool> Condition, List<string> SpriteName, string Path = "")
            {
                UseReplace = Condition;
                bool falg = Path == "";
                foreach (string TargetName in SpriteName)
                {
                    Sprite? Value;
                    if (falg)
                    {
                        Value = Tools.LoadModSprite(TargetName);
                    }
                    else
                    {
                        Value = Tools.LoadModSpriteForPath(Path, TargetName);
                    }
                    if (Value != null)
                        Sprites[TargetName] = Value;
                }
            }
            public bool GetModSprite(UnitAvatar owner, string Name, out Sprite? Value)
            {
                if (UseReplace(owner))
                {
                    if (Sprites.TryGetValue(Name, out var TrueSprite))
                    {
                        Value = TrueSprite;
                        return true;
                    }
                }
                Value = null;
                return false;
            }
        }

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

        public struct AllPlayerStatus
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
            public bool SameAs(AllPlayerStatus Other) =>
                Defense == Other.Defense &&
                AllDamage == Other.AllDamage &&
                ElementalDamage == Other.ElementalDamage &&
                Crit == Other.Crit;
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
                    string Id = "[Star's Sephiria Mod]LightPrism";
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

        public class YuisShortDaggerData(UnitAvatar TargetPlayer, int ManaValue)
        {
            public UnitAvatar Player = TargetPlayer;
            public int Value = ManaValue;
            public Timer EffectTimer = new(ManaValue * 0.1f);
            public string DamageId = "[Star's Sephiria Mod]EnchantedBlade";
            public void GetAttack(UnitAvatar Target)
            {
                float BaseDamage = Tools.BaseStatToMax(Player) * 5f;
                BaseDamage *= 1f - EffectTimer.Ratio;
                float Mult = 1f + Player.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                Mult *= 1f + Player.GetCustomStatUnsafe(TextFile.SpecialAttackDamageBonus) * 0.01f;
                Mult *= 1f + Player.GetCustomStatUnsafe(TextFile.MPSkillDamage) * 0.01f;
                float Damage = BaseDamage * Mult;
                var Dam = DamageInstance.GetDamage(Player, DamageId, Target.transform.position, 4294967295L, Damage, EDamageType.Slice, EDamageFromType.DirectAttack, Vector2.zero, 0, 0f);
                Dam.SetCustomColor(true, EffectConstant.MagicColor);
                Target.ApplyDamage(Dam);
            }
        }

        public class SuccessorBoltData(ActiveSkill_Bolt Bolt, float CharmMult = 1f, int ExAttackStack = 1)
        {
            public EDamageElementalType Elemental = Bolt.elementalType;
            public float Base = Bolt.defaultDamageByLevel.SafeRandomAccess(Bolt.Level);
            public float Mult = Bolt.damagePercentByLevel.SafeRandomAccess(Bolt.Level);
            public int Count = (int)Bolt.fireCount * ExAttackStack;
            public bool isMultiShot = (bool)isMultiShotField.GetValue(Bolt);
            public int MultiShotCount = (int)multiShotCountField.GetValue(Bolt);
            public float MultiShotMult = (float)multiShotDamageRatioField.GetValue(Bolt);
            public float CharmDamageMult = CharmMult;
            public static FieldInfo isMultiShotField = AccessTools.Field(typeof(ActiveSkill_Bolt), "isMultiShot");
            public static FieldInfo multiShotCountField = AccessTools.Field(typeof(ActiveSkill_Bolt), "multiShotCount");
            public static FieldInfo multiShotDamageRatioField = AccessTools.Field(typeof(ActiveSkill_Bolt), "multiShotDamageRatio");
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

        public class HeatedBladeAttackData
        {
            public UnitAvatar Player;
            public UnitAvatar Target;
            public float Time;
            public HeatedBladeAttackData(UnitAvatar player, UnitAvatar target, float time)
            {
                Player = player;
                Target = target;
                Time = time;
            }
        }

        public class MomentAttackData
        {
            public UnitAvatar Player;
            public UnitAvatar Target;
            public int Count;
            public float TickTimer = 0.1f;
            public float BaseDamage = 0;
            public MomentAttackData(UnitAvatar player, UnitAvatar target, float damage, int count = 1)
            {
                Player = player;
                Target = target;
                Count = count;
                BaseDamage = damage;
            }
            public float GetDamage()
            {
                float Value = BaseDamage * 0.8f;
                Value *= 1f + Player.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                Value *= 1f + Player.GetCustomStatUnsafe(TextFile.SpecialAttackDamageBonus) * 0.01f;
                Value *= 1f + Player.GetCustomStatUnsafe(TextFile.DirectWeaponDamage) * 0.01f;
                Value *= 1f + Player.GetCustomStatUnsafe(TextFile.DebuffDamage) * 0.01f;
                Value *= 1f + Player.GetCustomStatUnsafe(TextFile.PoisonDebuffDamageBonus) * 0.01f;
                return Value;
            }
            public IEnumerator UpdateMomentAttack(WeaponSimple_Katana Weapon)
            {
                if (Player == null) yield break;
                if (Target == null) yield break;
                if (Weapon == null) yield break;

                float Damage = GetDamage();
                Vector3 EulerAngles = Weapon.Networkowner.shoulder.transform.eulerAngles;
                for (int i = 0; i < Count; i++)
                {
                    if (Player.IsDead) yield break;
                    if (Target.IsDead) yield break;
                    if (Weapon == null) yield break;

                    int Max = 1;
                    float UseDamage = Damage * (1f + i * 0.05f);
                    if (Player.GetCustomStatUnsafe(TextFile.WindErosionLuck) > UnityEngine.Random.Range(0, 100)) Max += 1;
                    for (int k = 0; k < Max; k++)
                    {
                        float Value = UnityEngine.Random.Range(0f, 360f);
                        EulerAngles.z += Value;

                        Vector3 Position = Target.TopdownActor.Position_Center;
                        NewWeaponFireData Data = LoadModContent.WindKatana_Moment.Data;
                        var Dam = DamageInstance.GetDamage(Player, "Weapon_SpecialAttack", Target.transform.position, 4294967295L, UseDamage,
                            EDamageType.Slice, EDamageFromType.DirectAttack, Vector2.zero, 0, 0f);
                        Dam.elementalType = EffectConstant.Wind;
                        Dam.ignoreDebuff = true;
                        Target.ApplyDamage(Dam);
                        if (NetworkClient.active)
                            Weapon.CreateBasicAttackSwingFxFullyManual(Data, Position, EulerAngles);
                        ModNetwork.SendWeaponSwingFx(Weapon.netId, ModNetwork.SwingFxMoment, Position, EulerAngles);
                    }
                    yield return new WaitForSeconds(TickTimer);
                }
                yield break;
            }
        }

        public class AirValveZone
        {
            public static readonly Dictionary<UnitAvatar, AirValveZone> Active = [];
            public const byte FxPulse = 0;
            public const byte FxTrigger = 1;

            public UnitAvatar Player;
            public Vector3 Position;
            public Coroutine? Handle;
            public MonoBehaviour? Host;
            public int Generation;

            private const float Radius = 3f;
            private const float Interval = 0.5f;
            private const int TickCount = 20;

            public AirValveZone(UnitAvatar player, Vector3 position)
            {
                Player = player;
                Position = position;
            }

            public static void Trigger(WeaponSimple_Crossbow Weapon, UnitAvatar Player, Vector3 Position)
            {
                if (Weapon == null || Player == null) return;

                if (Active.TryGetValue(Player, out AirValveZone? Zone) && Zone != null)
                {
                    if (Zone.Host != null && Zone.Handle != null)
                        Zone.Host.StopCoroutine(Zone.Handle);
                    Zone.Position = Position;
                }
                else
                {
                    Zone = new AirValveZone(Player, Position);
                    Active[Player] = Zone;
                }

                Zone.Host = Weapon;
                Zone.Handle = Weapon.StartCoroutine(Zone.Run(Weapon));

                Vector3 FxPos = Position;
                FxPos.y += 0.5f;
                PlayFx(FxPos, FxTrigger, Player);
                ModNetwork.SendWeaponFx(Player.netId, FxPos, FxTrigger);
            }

            private static void PlayFx(Vector3 Position, byte Event, UnitAvatar Owner)
            {
                if (!NetworkClient.active) return;
                int OwnerIndex = Owner != null && Owner.isOwned ? 1 : 0;
                var fx = ModFxPool.Spawn(Event == FxTrigger ? "AirValveGround" : "CyanOrb", Position, OwnerIndex);
                if (Event != FxTrigger)
                {
                    fx?.SetColor(new Color(1f, 1f, 1f, 0.2f));
                    fx?.SetScale(Vector3.one * 1.85f);
                }
            }

            public IEnumerator Run(WeaponSimple_Crossbow Weapon)
            {
                int Gen = ++Generation;
                float Damage = Player.GetCustomStatUnsafe(TextFile.WindDamage) * 0.2f;
                Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.SpecialAttackDamageBonus) * 0.01f;
                Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.FinalWeaponDamage) * 0.01f;
                for (int i = 0; i < TickCount; i++)
                {
                    if (Gen != Generation) yield break;
                    if (Player == null || Player.IsDead) break;

                    foreach (UnitAvatar Target in Tools.SearchEnemiesInRange(Player, Position, Radius))
                    {
                        if (Target == null || Target.IsDead) continue;

                        var Dam = DamageInstance.GetDamage(Player, "Weapon_SpecialAttack", Target.transform.position, 4294967295L, Damage,
                            EDamageType.Slice, EDamageFromType.None, Vector2.zero, 0, 0f);
                        Dam.elementalType = EffectConstant.Wind;
                        Target.ApplyDamage(Dam);

                        bool falg = false;
                        foreach (CharacterDebuff Debuff in Target.Debuffs)
                        {
                            if (Debuff is CharacterDebuff_WindErosion)
                            {
                                if (Debuff.CurrentStack >= Debuff.MaxStackCount)
                                    falg = true;
                                break;
                            }
                        }
                        if (!falg) Target.ApplyDebuff(LoadModContent.WindErosion.Prefab, Player);
                    }
                    PlayFx(Position, FxPulse, Player);
                    if (Weapon != null)
                        ModNetwork.SendWeaponFx(Player.netId, Position, FxPulse);
                    yield return new WaitForSeconds(Interval);
                }
                if (Player != null && Active.TryGetValue(Player!, out AirValveZone? Current) && Current == this)
                    Active.Remove(Player!);
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
            public List<Miracle_StatusInstance.StatInfo> statList = [];
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
                GameObject ModPrefab = new($"Miracle_{ID}");
                Object.DontDestroyOnLoad(ModPrefab);
                Miracle_StatusInstance NewMiracle = ModPrefab.AddComponent<Miracle_StatusInstance>();
                ModAssetId NetId = NewMiracle.AddComponent<ModAssetId>();
                NetId.AssetId = LoadModContent.LoadModAssetId();
                NewMiracle.isEnabled = true;
                NewMiracle.id = ID;
                NewMiracle.autoGenerateEffectString = true;
                NewMiracle.aName = new LocalizedString($"Miracle_{ID}");
                Sprite? Icon = Tools.LoadModSprite($"Miracle_{ID}");
                if (Icon != null)
                    NewMiracle.miracleImage = Icon;
                NewMiracle.statInfo = statList.ToArray();
                NewMiracle.effects = Array.Empty<Effect>();
                if (Categories != "") NewMiracle.categories = [Categories];
                ThisMiracle = ModPrefab;
                Prefab = NewMiracle;
                StarSephiriaMod.registered[NetId.AssetId] = NewMiracle;
                StarSephiriaMod.RegisterModPrefab(ModPrefab);
                return this;
            }
        }

        public class ModItemCategory
        {
            public string ID;//连击ID
            public string Name;//连击名
            public string FruitIco;//水果名
            public Action<GameObject>? SetComboEffect = null;
            public List<ComboStat> Stat = [];//连击属性
            public ModItemCategory(string id, string name, string fruitIco)
            {
                ID = id;
                Name = name;
                FruitIco = fruitIco;
            }
            public GameObject CreatePrefab(string DefaultEffectText = "")
            {
                GameObject ModPrefab = new($"ModItemCategory_{Name}");
                if (SetComboEffect == null)
                {
                    ComboEffectBase Effect = ModPrefab.AddComponent<ComboEffectBase>();
                    Effect.defaultEffect = new LocalizedString(DefaultEffectText);
                    Effect.enabled = false;
                    if (Stat.Count > 0) Effect.addStatByCombo = Stat.ToArray();
                }
                else
                {
                    SetComboEffect.Invoke(ModPrefab);
                }
                ModAssetId NetId = ModPrefab.AddComponent<ModAssetId>();
                NetId.AssetId = LoadModContent.LoadModAssetId();
                StarSephiriaMod.registered[NetId.AssetId] = ModPrefab;
                StarSephiriaMod.RegisterModPrefab(ModPrefab);
                Object.DontDestroyOnLoad(ModPrefab);
                return ModPrefab;
            }
            public ModItemCategory SetStat(ComboStat stat)
            {
                Stat.Add(stat);
                return this;
            }
            public ModItemCategory SetCustomComboEffect(Action<GameObject> action)
            {
                SetComboEffect = action;
                return this;
            }
            public ItemCategoryEntity EndCreate()
            {
                ItemCategoryEntity ModCategory = ScriptableObject.CreateInstance<ItemCategoryEntity>();
                ModCategory.id = ID;
                ModCategory.categoryName = new LocalizedString($"ModItemCategory_{Name}");
                ModCategory.categoryFruitName = new LocalizedString($"ModItemCategory_{Name}Fruit");
                ModCategory.categoryIcon = Tools.LoadModSpriteForPath("Categories", $"Categories_{Name}");
                ModCategory.categoryFruitIcon = Tools.LoadModSpriteForPath("Categories", $"Fruits_{FruitIco}0");
                ModCategory.categoryIceFruitIcon = Tools.LoadModSpriteForPath("Categories", $"Fruits_{FruitIco}1");
                ModCategory.comboEffectPrefab = CreatePrefab();
                return ModCategory;
            }
        }

        public class ModUnitAvatar
        {
            private static readonly FieldInfo? RigidbodyCacheField = AccessTools.Field(typeof(TopdownRigidbody), "cachedRigidbodies");

            public static void EnsureRigidbodyCache()
            {
                if (RigidbodyCacheField != null && RigidbodyCacheField.GetValue(null) == null)
                    TopdownRigidbody.Initialize();
            }

            public string Name;
            public GameObject Template;
            public UnitAvatar? Avatar;
            public Action<GameObject>? SetUnit = null;
            public ModUnitAvatar(string name, GameObject copyTarget)
            {
                Name = name;
                if (copyTarget == null)
                {
                    StarSephiriaMod.LogError($"[ModUnitAvatar]{name} 创建失败：复制目标为空");
                    Template = new GameObject($"ModUnitAvatar_{name}");
                    return;
                }
                EnsureRigidbodyCache();
                Template = Object.Instantiate(copyTarget);
                Template.name = $"ModUnitAvatar_{name}";
                Template.SetActive(false);
                Object.DontDestroyOnLoad(Template);
                Avatar = Template.GetComponent<UnitAvatar>();
                if (Avatar == null)
                    StarSephiriaMod.LogError($"[ModUnitAvatar]{name} 复制目标上没有 UnitAvatar");
                UnitAI_NewBasic? AI = Template.GetComponent<UnitAI_NewBasic>();
                if (AI == null)
                    StarSephiriaMod.LogError($"[ModUnitAvatar]{name} 复制目标上没有 UnitAI_NewBasic");
                else UnitAI_NewBasic.AllInstances.Remove(AI);
            }

            public ModUnitAvatar SetCustom(Action<GameObject> action)
            {
                SetUnit = action;
                return this;
            }

            public ModUnitAvatar SetAvatar(Action<UnitAvatar> action)
            {
                if (Avatar != null) action(Avatar);
                return this;
            }

            public ModUnitAvatar SetAnimationSprites(string path, string vanillaPrefix, string modPrefix, params (string State, int Count)[] states)
            {
                SpriteReplaceData Data = new(Owner => Owner != null && Owner.defaultNameKey == "ModUnit_" + Name, []);
                foreach ((string State, int Count) in states)
                {
                    for (int i = 0; i < Count; i++)
                    {
                        Sprite? Repl = Tools.LoadModSpriteForPath(path, $"{modPrefix}{State}_{i:00}");
                        if (Repl != null) Data.Sprites[$"{vanillaPrefix}{State}_{i:00}"] = Repl;
                    }
                }
                Skin.Add(Data);
                return this;
            }

            public GameObject EndCreate()
            {
                if (Template == null) return null!;
                SetUnit?.Invoke(Template);

                NetworkIdentity? NetId = Template.GetComponent<NetworkIdentity>();
                if (NetId != null) Object.DestroyImmediate(NetId);

                uint AssetId = LoadModContent.LoadModAssetId();
                ModAssetId? ModId = Template.GetComponent<ModAssetId>();
                if (ModId == null) ModId = Template.AddComponent<ModAssetId>();
                ModId.AssetId = AssetId;

                if (Template.GetComponent<ModUnitTemplate>() == null)
                    Template.AddComponent<ModUnitTemplate>();

                StarSephiriaMod.registered[AssetId] = Template;
                StarSephiriaMod.RegisterModPrefab(Template);
                return Template;
            }
        }

        public class ModBullet
        {
            public static readonly List<ModBullet> AllBullets = [];
            private static readonly Dictionary<string, Dictionary<string, Sprite>> SpriteMaps = new(StringComparer.OrdinalIgnoreCase);
            private static readonly HashSet<string> SpriteNames = new(StringComparer.OrdinalIgnoreCase);
            private static readonly List<GameObject> PendingPoolFx = [];
            private static int FxCounter = 0;
            private static FieldInfo? tPrefabsField = null;
            public static bool HasSpriteName(string name)
            {
                return SpriteNames.Contains(name);
            }

            public static bool TryGetSpriteMap(string key, out Dictionary<string, Sprite> map)
            {
                return SpriteMaps.TryGetValue(key, out map!);
            }

            public string Name;
            public Bullet? BulletComp;
            public bool SpriteDebug = false;
            private readonly Func<GameObject?> TemplateGetter;
            private readonly List<Action<GameObject>> Builders = [];
            private GameObject? _template;
            public GameObject Template => _template ??= Create();

            public ModBullet(string name, GameObject copyTarget)
                : this(name, () => copyTarget)
            {
            }
            public ModBullet(string name, Func<GameObject?> copyTargetGetter)
            {
                Name = name;
                TemplateGetter = copyTargetGetter;
            }

            private ModBullet Then(Action<GameObject> action)
            {
                if (_template != null) action(_template);
                else Builders.Add(action);
                return this;
            }

            private GameObject Create()
            {
                GameObject? Source = TemplateGetter();
                if (Source == null)
                {
                    StarSephiriaMod.LogError($"[ModBullet]{Name} 创建失败：复制目标为空");
                    return new GameObject($"ModBullet_{Name}");
                }

                ModUnitAvatar.EnsureRigidbodyCache();
                GameObject Result = Object.Instantiate(Source);
                Result.name = $"ModBullet_{Name}";
                BulletComp = Result.GetComponent<Bullet>();
                if (BulletComp == null)
                    StarSephiriaMod.LogError($"[ModBullet]{Name} 复制目标上没有 Bullet");

                TrailRenderer? Trail = BulletComp?.trailRenderer;
                Transform? TrailParent = Trail?.transform.parent;
                Vector3 TrailPos = Trail != null ? Trail.transform.localPosition : default;
                Quaternion TrailRot = Trail != null ? Trail.transform.localRotation : default;
                Vector3 TrailScale = Trail != null ? Trail.transform.localScale : default;

                Result.SetActive(false);
                Object.DontDestroyOnLoad(Result);
                _template = Result;

                if (Trail != null && BulletComp != null)
                {
                    Trail.transform.SetParent(TrailParent != null ? TrailParent : Result.transform, false);
                    Trail.transform.localPosition = TrailPos;
                    Trail.transform.localRotation = TrailRot;
                    Trail.transform.localScale = TrailScale;
                    BulletComp.trailRenderer = Trail;
                }

                if (SpriteDebug) DumpSpriteNames(Result, BulletComp);

                foreach (Action<GameObject> Build in Builders) Build(Result);

                NetworkIdentity? NetId = Result.GetComponent<NetworkIdentity>();
                if (NetId != null) Object.DestroyImmediate(NetId);

                uint AssetId = LoadModContent.LoadModAssetId();
                ModAssetId? ModId = Result.GetComponent<ModAssetId>();
                if (ModId == null) ModId = Result.AddComponent<ModAssetId>();
                ModId.AssetId = AssetId;

                if (Result.GetComponent<ModBulletTemplate>() == null)
                    Result.AddComponent<ModBulletTemplate>();

                StarSephiriaMod.registered[AssetId] = Result;
                StarSephiriaMod.RegisterModPrefab(Result);
                AllBullets.Add(this);
                TryRegisterToPool();
                return Result;
            }

            public ModBullet SetSpriteDebug(bool Enable = true)
            {
                SpriteDebug = Enable;
                return this;
            }

            public ModBullet SetCustom(Action<GameObject> action)
            {
                return Then(action);
            }

            public ModBullet SetBullet(Action<Bullet> action)
            {
                return Then(T =>
                {
                    Bullet? Comp = T.GetComponent<Bullet>();
                    if (Comp != null) action(Comp);
                });
            }

            /// <summary>给子弹模板挂固定速度倍率；Bullet.OnSpawn 会把 speedScale 重置，所以用组件持续补回。</summary>
            public ModBullet SetSpeedScale(float scale)
            {
                return Then(T =>
                {
                    ModBulletSpeed Speed = T.GetComponent<ModBulletSpeed>();
                    if (Speed == null) Speed = T.AddComponent<ModBulletSpeed>();
                    Speed.Scale = scale;
                });
            }

            public ModBullet SetSpriteSet(string path, params string[] spriteNames)
            {
                return Then(T =>
                {
                    Dictionary<string, Sprite> Map = new(StringComparer.OrdinalIgnoreCase);
                    foreach (string SpriteName in spriteNames)
                    {
                        Sprite? Sprite = Tools.LoadModSpriteForPath(path, SpriteName);
                        if (Sprite != null) Map[SpriteName] = Sprite;
                    }
                    if (Map.Count == 0)
                    {
                        StarSephiriaMod.LogError($"[ModBullet]{Name} 没有加载到任何贴图");
                        return;
                    }

                    ModSpriteMap Marker = T.GetComponent<ModSpriteMap>();
                    if (Marker == null) Marker = T.AddComponent<ModSpriteMap>();
                    Marker.Key = Name;
                    SpriteMaps[Name] = Map;
                    foreach (string Key in Map.Keys) SpriteNames.Add(Key);

                    List<string> Original = SpriteDebug ? CollectSpriteNames(T) : [];
                    int Replaced = ReplaceDirect(T, Map);
                    if (SpriteDebug)
                    {
                        List<string> Missed = [];
                        foreach (string Key in Map.Keys)
                            if (!Original.Contains(Key)) Missed.Add(Key);
                        StarSephiriaMod.LogError($"[ModBullet]{Name} SetSpriteSet 命中 {Replaced} 个" +
                            (Missed.Count > 0 ? $"; 原版里没有这些名字: {string.Join(", ", Missed)}" : ""));
                    }
                });
            }

            /// <summary>按顺序直接替换子弹动画帧，不依赖模板原 sprite 名。</summary>
            public ModBullet SetSpriteFrames(string path, params string[] spriteNames)
            {
                return Then(T =>
                {
                    List<Sprite> Frames = [];
                    foreach (string SpriteName in spriteNames)
                    {
                        Sprite? Frame = Tools.LoadModSpriteForPath(path, SpriteName);
                        if (Frame != null) Frames.Add(Frame);
                    }
                    if (Frames.Count == 0)
                    {
                        StarSephiriaMod.LogError($"[ModBullet]{Name} 没有加载到子弹贴图");
                        return;
                    }
                    Sprite[] Array = Frames.ToArray();
                    if (ApplyDirectFrames(T, Array)) return;
                    if (!ApplyFallbackAnimator(T, Array))
                        StarSephiriaMod.LogError($"[ModBullet]{Name} 未找到任何 SpriteRenderer，子弹贴图未替换");
                });
            }

            private static bool ApplyDirectFrames(GameObject Root, Sprite[] Frames)
            {
                bool Replaced = false;
                foreach (SimpleAnimator2D Anim in Root.GetComponentsInChildren<SimpleAnimator2D>(true)) { Anim.sprites = Frames; Replaced = true; }
                foreach (SimpleAnimator2D_FX Anim in Root.GetComponentsInChildren<SimpleAnimator2D_FX>(true)) { Anim.sprites = Frames; Replaced = true; }
                foreach (BulletSimpleAnimator Anim in Root.GetComponentsInChildren<BulletSimpleAnimator>(true)) { Anim.sprites = Frames; Replaced = true; }
                return Replaced;
            }

            private static bool ApplyFallbackAnimator(GameObject Root, Sprite[] Frames)
            {
                SpriteRenderer? Target = null;
                SpriteRenderer? First = null;
                foreach (SpriteRenderer Renderer in Root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    First ??= Renderer;
                    if (Renderer.GetComponent<Animator2D_Basic>() != null)
                    {
                        Target = Renderer;
                        break;
                    }
                }
                Target ??= First;
                if (Target == null) return false;

                foreach (Animator2D_Basic Old in Root.GetComponentsInChildren<Animator2D_Basic>(true)) Old.enabled = false;
                BulletSimpleAnimator Anim = Target.GetComponent<BulletSimpleAnimator>();
                if (Anim == null) Anim = Target.gameObject.AddComponent<BulletSimpleAnimator>();
                Anim.sprites = Frames;
                Anim.spriteRenderer = Target;
                Anim.loop = true;
                Target.sprite = Frames[0];
                return true;
            }

            private static int ReplaceDirect(GameObject Root, Dictionary<string, Sprite> Map)
            {
                int Replaced = 0;
                void Replace(Sprite[] Sprites)
                {
                    for (int i = 0; i < Sprites.Length; i++)
                    {
                        Sprite? Orig = Sprites[i];
                        if (Orig != null && Map.TryGetValue(Orig.name, out Sprite? Repl))
                        {
                            Sprites[i] = Skin.FixPivot(Orig, Repl);
                            Replaced++;
                        }
                    }
                }
                foreach (SimpleAnimator2D Anim in Root.GetComponentsInChildren<SimpleAnimator2D>(true)) Replace(Anim.sprites);
                foreach (SimpleAnimator2D_FX Anim in Root.GetComponentsInChildren<SimpleAnimator2D_FX>(true)) Replace(Anim.sprites);
                foreach (BulletSimpleAnimator Anim in Root.GetComponentsInChildren<BulletSimpleAnimator>(true)) Replace(Anim.sprites);
                return Replaced;
            }

            public ModBullet SetCreateFxSprites(string path, params string[] spriteNames)
            {
                Dictionary<string, string> NameToFile = new(StringComparer.OrdinalIgnoreCase);
                foreach (string SpriteName in spriteNames) NameToFile[SpriteName] = SpriteName;
                return SetCreateFxSprites(path, NameToFile);
            }

            public ModBullet SetCreateFxSprites(string path, Dictionary<string, string> NameToFile)
            {
                return Then(T =>
                {
                    Bullet? Comp = T.GetComponent<Bullet>();
                    if (Comp != null && Comp.createFxPrefab != null)
                        Comp.createFxPrefab = CloneFxWithMap(Comp.createFxPrefab, path, NameToFile);
                });
            }

            /// <summary>直接替换命中成功 FX（attackSucceededFxPrefab）。传进来的 prefab 需要能进 SpriteFx.Pool，通常用下面带 Sprites 的重载更省事。</summary>
            public ModBullet SetAttackSucceededFx(GameObject Fx)
            {
                return Then(T =>
                {
                    Bullet? Comp = T.GetComponent<Bullet>();
                    if (Comp != null) Comp.attackSucceededFxPrefab = Fx;
                });
            }

            /// <summary>克隆命中成功 FX 并替换其中的 sprite（按原版 sprite 名匹配）。</summary>
            public ModBullet SetAttackSucceededFxSprites(string path, params string[] spriteNames)
            {
                Dictionary<string, string> NameToFile = new(StringComparer.OrdinalIgnoreCase);
                foreach (string SpriteName in spriteNames) NameToFile[SpriteName] = SpriteName;
                return SetAttackSucceededFxSprites(path, NameToFile);
            }

            /// <summary>同上，但可以给同名原版 sprite 指定不同的替换文件（NameToFile: 原版 sprite 名 → 内嵌 PNG 名）。</summary>
            public ModBullet SetAttackSucceededFxSprites(string path, Dictionary<string, string> NameToFile)
            {
                return Then(T =>
                {
                    Bullet? Comp = T.GetComponent<Bullet>();
                    if (Comp != null && Comp.attackSucceededFxPrefab != null)
                        Comp.attackSucceededFxPrefab = CloneFxWithMap(Comp.attackSucceededFxPrefab, path, NameToFile);
                });
            }

            /// <summary>命中成功 FX 的旋转基准角与随机误差（度）。</summary>
            public ModBullet SetAttackSucceededFxAngle(float Offset, float Error = 20f)
            {
                return Then(T =>
                {
                    Bullet? Comp = T.GetComponent<Bullet>();
                    if (Comp == null) return;
                    Comp.attackSucceededFxAngleOffset = Offset;
                    Comp.attackSucceededFxAngleError = Error;
                });
            }

            public ModBullet SetDestroyFxSprites(string path, params string[] spriteNames)
            {
                Dictionary<string, string> NameToFile = new(StringComparer.OrdinalIgnoreCase);
                foreach (string SpriteName in spriteNames) NameToFile[SpriteName] = SpriteName;
                return SetDestroyFxSprites(path, NameToFile);
            }

            public ModBullet SetDestroyFxSprites(string path, Dictionary<string, string> NameToFile)
            {
                return Then(T =>
                {
                    Bullet? Comp = T.GetComponent<Bullet>();
                    if (Comp?.DestroyModule != null && Comp.DestroyModule.destroyFxPrefab != null)
                        Comp.DestroyModule.destroyFxPrefab = CloneFxWithMap(Comp.DestroyModule.destroyFxPrefab, path, NameToFile);
                });
            }

            public ModBullet SetTailFxSprites(string path, params string[] spriteNames)
            {
                Dictionary<string, string> NameToFile = new(StringComparer.OrdinalIgnoreCase);
                foreach (string SpriteName in spriteNames) NameToFile[SpriteName] = SpriteName;
                return SetTailFxSprites(path, NameToFile);
            }

            public ModBullet SetTailFxSprites(string path, Dictionary<string, string> NameToFile)
            {
                return Then(T =>
                {
                    Bullet? Comp = T.GetComponent<Bullet>();
                    if (Comp?.MoveModule == null) return;
                    object Module = Comp.MoveModule;
                    foreach (FieldInfo Field in Module.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (Field.FieldType == typeof(GameObject[]) &&
                            (Field.Name == "tailFxPrefabs" || Field.Name == "moveFxPrefabs"))
                        {
                            if (Field.GetValue(Module) is GameObject[] Prefabs)
                            {
                                for (int i = 0; i < Prefabs.Length; i++)
                                    if (Prefabs[i] != null)
                                        Prefabs[i] = CloneFxWithMap(Prefabs[i], path, NameToFile);
                            }
                        }
                        else if (Field.FieldType == typeof(GameObject) &&
                            (Field.Name == "tailFxPrefab" || Field.Name == "moveFxPrefab"))
                        {
                            if (Field.GetValue(Module) is GameObject Prefab && Prefab != null)
                                Field.SetValue(Module, CloneFxWithMap(Prefab, path, NameToFile));
                        }
                    }
                });
            }

            private GameObject CloneFxWithMap(GameObject Source, string path, string[] spriteNames)
            {
                Dictionary<string, string> NameToFile = new(StringComparer.OrdinalIgnoreCase);
                foreach (string SpriteName in spriteNames) NameToFile[SpriteName] = SpriteName;
                return CloneFxWithMap(Source, path, NameToFile);
            }

            private GameObject CloneFxWithMap(GameObject Source, string path, Dictionary<string, string> NameToFile)
            {
                Dictionary<string, Sprite> Map = new(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, string> Pair in NameToFile)
                {
                    Sprite? Sprite = Tools.LoadModSpriteForPath(path, Pair.Value);
                    if (Sprite != null) Map[Pair.Key] = Sprite;
                }
                if (Map.Count == 0)
                {
                    StarSephiriaMod.LogError($"[ModBullet]{Name} FX 没有加载到任何贴图");
                    return Source;
                }

                GameObject Clone = Object.Instantiate(Source);
                FxCounter++;
                Clone.name = $"ModFx_{Name}_{FxCounter}";
                Clone.SetActive(false);
                Object.DontDestroyOnLoad(Clone);

                ModSpriteMap Marker = Clone.GetComponent<ModSpriteMap>();
                if (Marker == null) Marker = Clone.AddComponent<ModSpriteMap>();
                string Key = $"{Name}_Fx{FxCounter}";
                Marker.Key = Key;
                SpriteMaps[Key] = Map;
                foreach (string SpriteName in Map.Keys) SpriteNames.Add(SpriteName);
                ReplaceDirect(Clone, Map);
                PendingPoolFx.Add(Clone);
                return Clone;
            }

            /// <summary>立即物化并返回 prefab。懒用法可以完全不调它，直接访问 Template。</summary>
            public GameObject EndCreate()
            {
                return Template;
            }

            public void TryRegisterToPool()
            {
                if (Bullet.Pool == null || _template == null) return;
                Bullet? Comp = _template.GetComponent<Bullet>();
                if (Comp == null) return;
                tPrefabsField ??= AccessTools.Field(typeof(ProjectileSpawner<Bullet>), "tPrefabsByName");
                if (tPrefabsField?.GetValue(Bullet.Pool) is Dictionary<string, Bullet> Dict)
                    Dict[_template.name] = Comp;
            }

            private void DumpSpriteNames(GameObject Root, Bullet? Comp)
            {
                List<string> Lines =
                [
                    $"[ModBullet]{Name} 可用贴图名（文件名）:"
                ];
                AppendSpriteNames(Lines, "Bullet", Root);
                if (Comp != null)
                {
                    AppendSpriteNames(Lines, "CreateFx", Comp.createFxPrefab);
                    AppendSpriteNames(Lines, "AttackSucceededFx", Comp.attackSucceededFxPrefab);
                    AppendSpriteNames(Lines, "TileHitFx", Comp.tileHitFxPrefab);
                    if (Comp.DestroyModule != null)
                        AppendSpriteNames(Lines, "DestroyFx", Comp.DestroyModule.destroyFxPrefab);
                    if (Comp.MoveModule != null)
                    {
                        object Module = Comp.MoveModule;
                        foreach (FieldInfo Field in Module.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                        {
                            if (Field.FieldType == typeof(GameObject[]) &&
                                (Field.Name == "tailFxPrefabs" || Field.Name == "moveFxPrefabs"))
                            {
                                if (Field.GetValue(Module) is GameObject[] Prefabs)
                                {
                                    for (int i = 0; i < Prefabs.Length; i++)
                                        AppendSpriteNames(Lines, $"{Field.Name}[{i}]", Prefabs[i]);
                                }
                            }
                            else if (Field.FieldType == typeof(GameObject) &&
                                (Field.Name == "tailFxPrefab" || Field.Name == "moveFxPrefab"))
                            {
                                if (Field.GetValue(Module) is GameObject Prefab)
                                    AppendSpriteNames(Lines, Field.Name, Prefab);
                            }
                        }
                    }
                }
                StarSephiriaMod.LogError(string.Join("\n", Lines));
            }

            private static void AppendSpriteNames(List<string> Lines, string Label, GameObject? Root)
            {
                if (Root == null) return;
                List<string> Names = CollectSpriteNames(Root);
                Lines.Add($"{Label}: {(Names.Count > 0 ? string.Join(", ", Names) : "(无)")}");
            }

            private static List<string> CollectSpriteNames(GameObject Root)
            {
                List<string> Names = [];

                void Add(Sprite? Sprite)
                {
                    if (Sprite == null || Names.Count >= 80) return;
                    if (!Names.Contains(Sprite.name)) Names.Add(Sprite.name);
                }

                foreach (Animator2D_Basic Animator in Root.GetComponentsInChildren<Animator2D_Basic>(true))
                {
                    if (Animator == null || Animator.currentSet == null) continue;
                    foreach (AnimationSet.StateInfo State in Animator.currentSet.sprites)
                    {
                        if (State?.timeline == null) continue;
                        foreach (AnimationSet.StateInfo.SpriteKeyFrame Frame in State.timeline) Add(Frame?.sprite);
                    }
                }
                foreach (SimpleAnimator2D Animator in Root.GetComponentsInChildren<SimpleAnimator2D>(true))
                {
                    if (Animator == null || Animator.sprites == null) continue;
                    foreach (Sprite Sprite in Animator.sprites) Add(Sprite);
                }
                foreach (SimpleAnimator2D_FX Animator in Root.GetComponentsInChildren<SimpleAnimator2D_FX>(true))
                {
                    if (Animator == null || Animator.sprites == null) continue;
                    foreach (Sprite Sprite in Animator.sprites) Add(Sprite);
                }
                foreach (SpriteRenderer Renderer in Root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (Renderer == null) continue;
                    Add(Renderer.sprite);
                }
                return Names;
            }

            public static void RegisterAllToPool()
            {
                foreach (ModBullet Data in AllBullets) Data.TryRegisterToPool();
                RegisterPendingFxToPool();
            }

            private static void RegisterPendingFxToPool()
            {
                if (SpriteFx.Pool == null || PendingPoolFx.Count == 0) return;
                MethodInfo? MakePool = AccessTools.Method(typeof(ObjectPoolingFactory<SpriteFx>), "MakePool");
                FieldInfo? PooledField = AccessTools.Field(typeof(ObjectPoolingFactory<SpriteFx>), "pooledGameObjects");
                if (MakePool == null || PooledField == null) return;

                foreach (GameObject Fx in PendingPoolFx)
                {
                    if (Fx == null) continue;
                    if (PooledField.GetValue(SpriteFx.Pool) is not Dictionary<GameObject, List<SpriteFx>> Pooled) continue;
                    if (Pooled.ContainsKey(Fx)) continue;

                    Fx.SetActive(true);
                    try
                    {
                        MakePool.Invoke(SpriteFx.Pool, [null!, Fx]);
                    }
                    catch (Exception ex)
                    {
                        StarSephiriaMod.LogError($"[ModBullet] 注册 FX 到 SpriteFx.Pool 失败: {Fx.name} {ex.Message}");
                    }
                    Fx.SetActive(false);
                }
            }
        }

        public class ModKeyWord
        {
            public string ID = "";//关键词ID
            public bool DisplayDetails = false;//是否使用提示框
            public Color TextColor;//关键词颜色
            public string UseKeyWordSpriteName = "";//使用模组关键词
            public Sprite? Image = null;//使用原版关键词贴图
            public string Detailed = "";
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
                if (Detailed != "")
                {
                    ModKeyword.detailedValue = new LocalizedString(Detailed);
                }
                return ModKeyword;
            }
            public ModKeyWord SetDetailed(string Name)
            {
                Detailed = Name;
                return this;
            }
            public ModKeyWord StringToSprite(string Name)
            {
                Image = Tools.LoadModSprite($"KeywordIcon_{Name}");
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
            public ModKeyWord CreateStatusAmp(string StatusID, string TargetStatusID, string Symbol = "", bool useLocalized = false)
            {
                StatusEntity ModStatusEntity = ScriptableObject.CreateInstance<StatusEntity>();
                ModStatusEntity.className = $"StatusInstance_CustomAmp/{TargetStatusID}";
                ModStatusEntity.statKeyword = ID;
                ModStatusEntity.id = StatusID;
                if (useLocalized)
                {
                    ModStatusEntity.useLocalizedSymbol = useLocalized;
                    ModStatusEntity.localizedSymbol = new LocalizedString($"ModKeyword_{ID}_Symbol");
                }
                else ModStatusEntity.symbol = Symbol;
                Status = ModStatusEntity;
                return this;
            }
        }

        public class ModDamageKey
        {
            public const string IDPrefix = "[Star's Sephiria Mod]";
            private static readonly Dictionary<DamageIdEntity.ECategory, Sprite?> VanillaIcons = [];
            public string ID;//填的伤害ID，不带前缀
            public DamageIdEntity.ECategory Category;//伤害统计里括号显示的类型
            public Sprite? Icon = null;//不填则按类型取原版图标
            public ModDamageKey(string id, DamageIdEntity.ECategory category, string icon = "")
            {
                ID = id;
                Category = category;
                if (!string.IsNullOrEmpty(icon)) Icon = Tools.LoadModSprite(icon);
            }
            public string DamageId => IDPrefix + ID;
            public ModDamageKey SetIcon(string icon)
            {
                Icon = Tools.LoadModSprite(icon);
                return this;
            }
            public DamageIdEntity EndCreate()
            {
                DamageIdEntity Entity = ScriptableObject.CreateInstance<DamageIdEntity>();
                Entity.name = $"ModDamageKey_{ID}";
                Entity.id = DamageId;
                Entity.aName = new LocalizedString($"ModDamageKey_{ID}");
                Entity.category = Category;
                Entity.icon = Icon != null ? Icon : GetVanillaIcon(Category);
                return Entity;
            }
            private static Sprite? GetVanillaIcon(DamageIdEntity.ECategory category)
            {
                if (VanillaIcons.TryGetValue(category, out Sprite? Cached)) return Cached;
                Sprite? Result = null;
                foreach (DamageIdEntity Entity in Resources.LoadAll<DamageIdEntity>("DamageId"))
                {
                    if (Entity.category != category || Entity.icon == null) continue;
                    Result = Entity.icon;
                    break;
                }
                VanillaIcons[category] = Result;
                return Result;
            }
            /// <summary>把列表注册进 KeywordDatabase，伤害统计/结算排行就能显示名字和类型。</summary>
            public static void RegisterAll(IEnumerable<ModDamageKey> Keys)
            {
                FieldInfo? Field = typeof(KeywordDatabase).GetField("damageIdDictionary", BindingFlags.NonPublic | BindingFlags.Static);
                if (Field?.GetValue(null) is not Dictionary<string, DamageIdEntity> Table)
                {
                    StarSephiriaMod.LogError("[ModDamageKey]注册失败：KeywordDatabase.damageIdDictionary 不可用");
                    return;
                }
                foreach (ModDamageKey Key in Keys)
                {
                    DamageIdEntity Entity = Key.EndCreate();
                    Table[Entity.id] = Entity;
                }
            }
        }

        public class ModItem
        {
            public string Name;
            public bool FlavorText;
            public int CustomCost = -1;
            public int SapphirePrice = 3;
            public EItemRarity Rarity;
            public bool IsDual;
            public bool IsDestroyedOnDiscard;
            public bool IsWitchItem = false;
            public ItemEntity? Item { get; private set; }
            public Action<ItemEntity>? ItemEffect;
            private readonly List<Action<ItemEntity>> Builders = [];
            private GameObject? _prefab;
            public ModItem(string name, EItemRarity rarity = EItemRarity.Common, bool flavor = false)
            {
                Name = name;
                Rarity = rarity;
                FlavorText = flavor;
            }
            public GameObject Prefab => _prefab ??= CreatePrefab();
            private GameObject CreatePrefab()
            {
                GameObject ModPrefab = new($"ModItemEntity_{Name}");
                uint AssetId = LoadModContent.LoadModAssetId();
                ModAssetId Asset = ModPrefab.AddComponent<ModAssetId>();
                Asset.AssetId = AssetId;
                StarSephiriaMod.registered[Asset.AssetId] = ModPrefab;
                StarSephiriaMod.RegisterModPrefab(ModPrefab);
                Object.DontDestroyOnLoad(ModPrefab);
                return ModPrefab;
            }
            public ModItem CustomSet(Action<ModItem> Set)
            {
                Set.Invoke(this);
                return this;
            }
            public ModItem SetCost(int cost, int sapphirePrice = 3)
            {
                CustomCost = cost;
                SapphirePrice = sapphirePrice;
                return this;
            }
            public ModItem SetDual(bool dual = true)
            {
                IsDual = dual;
                return this;
            }
            public ModItem CreateCharm(List<string> Categories, List<StatusGroup> Status, bool Unique = true)
            {
                Builders.Add(Entity =>
                {
                    Entity.categories = Categories;
                    Entity.type = EItemType.Charm;
                    Charm_StatusInstance Effect = Entity.resourcePrefab.AddComponent<Charm_StatusInstance>();
                    Effect.isWeaponRelatedCharm = false;
                    Effect.effectsString = [];
                    Effect.stats = Status.ToArray();
                    int MaxLv = 0;
                    foreach (StatusGroup Group in Status)
                    {
                        if (MaxLv < Group.valuesByLevel.Length) MaxLv = Group.valuesByLevel.Length - 1;
                    }
                    Effect.maxLevel = MaxLv;
                    Effect.isUniqueEffect = Unique;
                    Effect.enabled = false;
                });
                return this;
            }
            public ModItem CustomCreateCharm(List<string> Categories, Action<ItemEntity> Action)
            {
                Builders.Add(Entity =>
                {
                    Entity.categories = Categories;
                    Entity.type = EItemType.Charm;
                    Action(Entity);
                });
                return this;
            }
            public ModItem CreatePotion(List<string> Categories, string StatusId, int Value)
            {
                Builders.Add(Entity =>
                {
                    Entity.categories = Categories;
                    Entity.type = EItemType.Potion;

                    PotionEffect_StatusInstance Effect = Entity.resourcePrefab.AddComponent<PotionEffect_StatusInstance>();
                    Effect.effectString = new LocalizedString("");
                    Effect.stats =
                    [
                        new() { statusID = StatusId, value = Value }
                    ];
                    Effect.canUseInBattle = true;

                    WieldingPotion Potion = Entity.resourcePrefab.AddComponent<WieldingPotion>();
                    Potion.effect = Effect;
                    Effect.connect = Potion;
                    Potion.visual = Entity.icon;
                    Potion.potionDrinkingTime = 1f;
                });
                return this;
            }
            public ModItem SetCustomItemEffect(Action<ItemEntity> Action)
            {
                ItemEffect = Action;
                return this;
            }
            public ItemEntity CreateItem()
            {
                if (Item != null) return Item;
                Item = CreateItemCore();
                foreach (Action<ItemEntity> Build in Builders) Build(Item);
                return Item;
            }
            private ItemEntity CreateItemCore()
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
                ModItem.icon = Tools.LoadModSprite($"Item_{Name}");
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

        public class ModActiveSkill
        {
            private static int NextSkillId = 10000;
            public string Name;
            public Type SkillType;
            public bool UniqueEffect = true;
            public GameObject? Prefab { get; private set; }
            public ActiveSkill? Skill { get; private set; }
            public ActiveSkillEntity? Entity { get; private set; }
            private readonly List<Action<ActiveSkill>> SkillBuilders = [];
            private readonly List<Action<ActiveSkillEntity>> EntityBuilders = [];
            private readonly List<Action<GameObject>> PrefabBuilders = [];
            private int? SkillId;
            private string? NameKey;
            private string? FlavorKey;
            private Sprite? Icon;

            public ModActiveSkill(string name, Type skillType)
            {
                if (!typeof(ActiveSkill).IsAssignableFrom(skillType))
                    throw new ArgumentException($"{skillType.FullName} 不是 ActiveSkill 的子类", nameof(skillType));
                Name = name;
                SkillType = skillType;
            }
            public ModActiveSkill(string name) : this(name, typeof(ActiveSkill_Arrow)) { }

            public ModActiveSkill SetId(int id)
            {
                SkillId = id;
                return this;
            }
            public ModActiveSkill SetNameKey(string key)
            {
                NameKey = key;
                return this;
            }
            public ModActiveSkill SetFlavorKey(string key)
            {
                FlavorKey = key;
                return this;
            }
            public ModActiveSkill SetIcon(Sprite icon)
            {
                Icon = icon;
                return this;
            }
            public ModActiveSkill SetIcon(string spriteName)
            {
                Icon = Tools.LoadModSprite(spriteName);
                return this;
            }
            public ModActiveSkill SetUniqueEffect(bool unique)
            {
                UniqueEffect = unique;
                return this;
            }
            public ModActiveSkill SetPrefab(Action<GameObject> set)
            {
                PrefabBuilders.Add(set);
                return this;
            }
            public ModActiveSkill SetSkill<T>(Action<T> set) where T : ActiveSkill
            {
                if (!typeof(T).IsAssignableFrom(SkillType))
                    throw new ArgumentException($"{typeof(T).FullName} 与 {SkillType.FullName} 不匹配");
                SkillBuilders.Add(Skill => { if (Skill is T Target) set(Target); });
                return this;
            }
            public ModActiveSkill SetEntity(Action<ActiveSkillEntity> set)
            {
                EntityBuilders.Add(set);
                return this;
            }

            public ActiveSkillEntity Create()
            {
                if (Entity != null) return Entity;
                GameObject NewPrefab = new($"ModActiveSkill_{Name}");
                NewPrefab.SetActive(false);
                NewPrefab.AddComponent<ModSpawnTemplate>();
                uint AssetId = LoadModContent.LoadModAssetId();
                ModAssetId Asset = NewPrefab.AddComponent<ModAssetId>();
                Asset.AssetId = AssetId;
                StarSephiriaMod.registered[AssetId] = NewPrefab;
                StarSephiriaMod.RegisterModPrefab(NewPrefab);
                Object.DontDestroyOnLoad(NewPrefab);

                ActiveSkill NewSkill = (ActiveSkill)NewPrefab.AddComponent(SkillType);
                foreach (Action<ActiveSkill> Build in SkillBuilders) Build(NewSkill);
                foreach (Action<GameObject> Build in PrefabBuilders) Build(NewPrefab);

                ActiveSkillEntity NewEntity = ScriptableObject.CreateInstance<ActiveSkillEntity>();
                NewEntity.name = $"ModActiveSkill_{Name}";
                NewEntity.id = SkillId ?? NextSkillId++;
                NewEntity.aName = new LocalizedString(NameKey ?? $"ModSkill_{Name}");
                NewEntity.aFlavorText = new LocalizedString(FlavorKey ?? $"ModSkill_{Name}_Flavor");
                NewEntity.icon = Icon ?? Tools.LoadModSprite($"Skill_{Name}");
                NewEntity.magicPrefab = NewPrefab;
                NewEntity.enabled = true;
                foreach (Action<ActiveSkillEntity> Build in EntityBuilders) Build(NewEntity);

                Prefab = NewPrefab;
                Skill = NewSkill;
                Entity = NewEntity;
                return NewEntity;
            }

            public void BindCharm(ItemEntity Item, int MaxLevel, params string[] EffectsString)
            {
                ActiveSkillEntity NewEntity = Create();
                Charm_Magic Charm = Item.resourcePrefab.GetComponent<Charm_Magic>();
                if (Charm == null) Charm = Item.resourcePrefab.AddComponent<Charm_Magic>();
                Charm.skill = NewEntity;
                Charm.maxLevel = MaxLevel;
                Charm.isWeaponRelatedCharm = false;
                Charm.isUniqueEffect = UniqueEffect;
                if (EffectsString.Length > 0)
                    Charm.effectsString = EffectsString.Select(Key => new LocalizedString(Key)).ToArray();
                Charm.enabled = false;
            }
        }

        public class ModFireData
        {
            private readonly Func<NewWeaponFireData?> TemplateGetter;
            private readonly List<Action<NewWeaponFireData>> Builders = [];
            private readonly Dictionary<string, (string Name, string Path)> SwingSpriteMap = [];
            private readonly List<string> SwingSpriteFolders = [];
            private readonly HashSet<string> ExtendFrameFolders = new(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<(Sprite Origin, Sprite Replacement), Sprite> SwingPivotCache = [];
            private static readonly List<GameObject> SwingFxPrefabs = [];//所有创建过的刀光 FX，跨 GameDataLoader 生命周期保留
            private static int SwingFxCounter;
            public bool SwingSpriteDebug = false;
            public float SwingScale = 1f;
            private NewWeaponFireData? _data;

            public NewWeaponFireData Data
            {
                get
                {
                    if (_data == null) _data = Create();
                    return _data;
                }
            }

            public ModFireData(NewWeaponFireData Source)
            {
                NewWeaponFireData Captured = Source;
                TemplateGetter = () => Captured;
            }

            private ModFireData(Func<NewWeaponFireData?> SourceGetter)
            {
                TemplateGetter = SourceGetter;
            }

            public static ModFireData Basic(int WeaponId, int Index = 0)
            {
                return new ModFireData(() => GetAt(GetWeapon(WeaponId)?.basicComboAttacks, Index));
            }

            public static ModFireData Dash(int WeaponId, int Index = 0)
            {
                return new ModFireData(() => GetAt(GetWeapon(WeaponId)?.dashAttacks, Index));
            }

            public static ModFireData Special(int WeaponId, int Index = 0)
            {
                return new ModFireData(() => GetAt(GetWeapon(WeaponId)?.specialAttacks, Index));
            }

            public static ModFireData QuickDraw(int WeaponId)
            {
                return new ModFireData(() => (GetWeapon(WeaponId) as WeaponSimple_Katana)?.quickDrawFireData);
            }

            public static ModFireData GreatDraw(int WeaponId)
            {
                return new ModFireData(() => (GetWeapon(WeaponId) as WeaponSimple_Katana)?.greatDrawFireData);
            }

            public static ModFireData SecondSpecial(int WeaponId, bool Enhanced = false)
            {
                return new ModFireData(() =>
                {
                    WeaponSimple_QuartterStaff? Staff = GetWeapon(WeaponId) as WeaponSimple_QuartterStaff;
                    return Enhanced ? Staff?.secondSpecialAttackEnhancedFireData : Staff?.secondSpecialAttackFireData;
                });
            }

            public static ModFireData BladeZone(int WeaponId)
            {
                return new ModFireData(() => (GetWeapon(WeaponId) as WeaponSimple_Dagger)?.dashAttackFireData_BladeZone);
            }

            private static WeaponSimple? GetWeapon(int WeaponId)
            {
                WeaponEntity? Entity = WeaponDatabase.FindWeaponById(WeaponId);
                if (Entity == null || Entity.mainWeaponPrefab == null) return null;
                return Entity.mainWeaponPrefab.GetComponent<WeaponSimple>();
            }

            private static NewWeaponFireData? GetAt(NewWeaponFireData[]? Source, int Index)
            {
                if (Source == null || Index < 0 || Index >= Source.Length) return null;
                return Source[Index];
            }

            private NewWeaponFireData Create()
            {
                NewWeaponFireData? Source = TemplateGetter();
                if (Source == null)
                {
                    StarSephiriaMod.LogError("[ModFireData] 模板为空，检查武器 ID / 下标 / 加载顺序");
                    return null!;
                }

                NewWeaponFireData Result = Object.Instantiate(Source);
                foreach (Action<NewWeaponFireData> Build in Builders) Build(Result);
                ApplySwingFx(Result);
                return Result;
            }

            public ModFireData CustomSet(Action<NewWeaponFireData> Set)
            {
                Builders.Add(Set);
                return this;
            }

            public ModFireData SetDamage(float Multiplier)
            {
                Builders.Add(Data => Data.damageMultiplier = Multiplier);
                return this;
            }

            public ModFireData SetDamagePerMP(int Value)
            {
                Builders.Add(Data => Data.addDamagePerUsedMP = Value);
                return this;
            }

            public ModFireData SetStagger(int Level)
            {
                Builders.Add(Data => Data.staggeringLevel = Level);
                return this;
            }

            public ModFireData SetForce(float Power)
            {
                Builders.Add(Data => Data.externalForcePower = Power);
                return this;
            }

            public ModFireData SetSwingID(string Id)
            {
                Builders.Add(Data => Data.swingID = Id);
                return this;
            }

            public ModFireData SetElement(EDamageElementalType Element)
            {
                Builders.Add(Data => Data.damageElementalType = Element);
                return this;
            }

            public ModFireData SetRelatedStat(string Formula)
            {
                Builders.Add(Data =>
                {
                    Data.relatedStatFormula = Formula;
                    Data.useElementalTypeFromRelatedStatFormula = true;
                });
                return this;
            }

            public ModFireData SetDash(float Speed, float Time = -1f)
            {
                Builders.Add(Data =>
                {
                    Data.attackDashSpeed = Speed;
                    if (Time >= 0f) Data.attackDashTime = Time;
                });
                return this;
            }

            public ModFireData SetFinalCombo(bool Final = true)
            {
                Builders.Add(Data => Data.forceFinalCombo = Final);
                return this;
            }

            public ModFireData SetSwingFx(GameObject Fx)
            {
                Builders.Add(Data => Data.swingFxPrefab = Fx);
                return this;
            }

            /// <summary>
            /// 换刀光（swingFxPrefab）里的某个 sprite：original 是原 FX 的 sprite 名，modName 是内嵌 PNG 名。
            /// 会在 Data 创建时克隆 FX prefab、替换帧、并注册进 SpriteFx.Pool。
            /// </summary>
            public ModFireData SetSwingSprite(string original, string modName, string path = "EffectFx")
            {
                SwingSpriteMap[original] = (modName, path);
                return this;
            }

            /// <summary>
            /// 自动把 Sprites/&lt;Path&gt;/ 下所有 PNG 按“文件名 = 原 FX sprite 名”加入刀光换皮表。
            /// 例：SetSwingSpriteFolder("EffectFx.WindSlash")
            /// </summary>
            public ModFireData SetSwingSpriteFolder(string Path)
            {
                if (!string.IsNullOrEmpty(Path)) SwingSpriteFolders.Add(Path);
                return this;
            }

            /// <summary>
            /// 换皮时如果文件夹里的图比原 FX 的帧多，会把多出来的图按文件名数字顺序追加到动画末尾。
            /// 例：原 FX 8 帧 Tornado01~08，文件夹有 Tornado01~10 → 动画变成 10 帧。
            /// </summary>
            public ModFireData SetSwingSpriteFolder(string Path, bool extendFrames)
            {
                SetSwingSpriteFolder(Path);
                if (extendFrames && !string.IsNullOrEmpty(Path)) ExtendFrameFolders.Add(Path);
                return this;
            }

            /// <summary>
            /// 换 BladeZone 投射物上 bladeZoneFxPrefab / bladeZoneFxSlashHitPrefab 的贴图：
            /// 按“文件名 = 原 FX sprite 名”从 Sprites/&lt;Path&gt;/ 取图。
            /// 会克隆 projectilePrefab 和 FX prefab，不影响共用同一套原版资源的武器。
            /// </summary>
            public ModFireData SetBladeZoneFxSprites(string Path)
            {
                Builders.Add(Data =>
                {
                    if (Data is not NewWeaponFireData_SpecialProjectile FireData || FireData.projectilePrefab == null)
                    {
                        StarSephiriaMod.LogError("[ModFireData] SetBladeZoneFxSprites 需要 NewWeaponFireData_SpecialProjectile");
                        return;
                    }

                    Dictionary<string, Sprite> Sprites = LoadFxSpriteFolder(Path);
                    if (Sprites.Count == 0)
                    {
                        StarSephiriaMod.LogError($"[ModFireData] BladeZone 换皮没有加载到任何贴图: {Path}");
                        return;
                    }

                    // 克隆投射物 prefab，避免改到原版无光之刃共用的资源
                    GameObject Source = FireData.projectilePrefab;
                    bool SourceActive = Source.activeSelf;
                    GameObject Projectile;
                    try
                    {
                        if (SourceActive) Source.SetActive(false);
                        Projectile = Object.Instantiate(Source);
                    }
                    finally
                    {
                        if (SourceActive) Source.SetActive(true);
                    }

                    SwingFxCounter++;
                    Projectile.name = "ModBladeZoneProjectile_" + SwingFxCounter;
                    Projectile.SetActive(false);
                    Object.DontDestroyOnLoad(Projectile);
                    Projectile.AddComponent<ModSpawnTemplate>();

                    SpecialProjectile_BladeZone? Zone = Projectile.GetComponent<SpecialProjectile_BladeZone>();
                    if (Zone == null)
                    {
                        StarSephiriaMod.LogError($"[ModFireData] {Source.name} 上没有 SpecialProjectile_BladeZone");
                        Object.Destroy(Projectile);
                        return;
                    }

                    GameObject? ZoneFx = CloneFxWithSprites(Zone.bladeZoneFxPrefab, Sprites, "BladeZoneFx");
                    if (ZoneFx != null) Zone.bladeZoneFxPrefab = ZoneFx;

                    GameObject? HitFx = CloneFxWithSprites(Zone.bladeZoneFxSlashHitPrefab, Sprites, "BladeZoneSlashHitFx");
                    if (HitFx != null) Zone.bladeZoneFxSlashHitPrefab = HitFx;

                    FireData.projectilePrefab = Projectile;

                    // 换成模组自己的 assetId 并注册；否则客机会按原版 assetId 生成原版投射物（贴图也是原版）
                    NetworkIdentity? NetId = Projectile.GetComponent<NetworkIdentity>();
                    if (NetId != null) Object.DestroyImmediate(NetId);

                    uint AssetId = LoadModContent.LoadModAssetId();
                    ModAssetId? ModId = Projectile.GetComponent<ModAssetId>() ?? Projectile.AddComponent<ModAssetId>();
                    ModId.AssetId = AssetId;

                    StarSephiriaMod.registered[AssetId] = Projectile;
                    StarSephiriaMod.RegisterModPrefab(Projectile);
                });
                return this;
            }

            /// <summary>
            /// 近战（NewWeaponFireData_MeleeAttack）专用换皮：按顺序把文件夹里的图替换到原 FX 帧上。
            /// 原帧名需以 originalPrefix 开头（例如 GreatSword0_TurnSwing_Fire_Minor）。
            /// extendFrames 为 true 时，多出来的图会追加到动画末尾；swingScale 缩放整个 FX prefab；
            /// collisionScale 缩放伤害体 MeleeCollision 的判定范围。
            /// 会同时处理 FireData 自己的 swingFxPrefab 和伤害体 MeleeCollision 的 swingFxPrefab。
            /// </summary>
            public ModFireData SetMeleeSwingSpriteFolder(string Path, string originalPrefix, bool extendFrames, float swingScale = 1f, float collisionScale = 1f)
            {
                Builders.Add(Data =>
                {
                    if (Data is not NewWeaponFireData_MeleeAttack Melee || string.IsNullOrEmpty(originalPrefix))
                    {
                        StarSephiriaMod.LogError("[ModFireData] SetMeleeSwingSpriteFolder 只支持 NewWeaponFireData_MeleeAttack");
                        return;
                    }

                    List<Sprite> Ordered = LoadFxSpriteFolderOrdered(Path);
                    if (Ordered.Count == 0)
                    {
                        StarSephiriaMod.LogError($"[ModFireData] 近战换皮没有加载到任何贴图: {Path}");
                        return;
                    }

                    bool Matched = false;

                    GameObject? OwnFx = ReskinFxByOrder(Melee.swingFxPrefab, Ordered, originalPrefix, extendFrames, swingScale);
                    if (OwnFx != null)
                    {
                        Melee.swingFxPrefab = OwnFx;
                        Matched = true;
                    }

                    if (Melee.projectilePrefab != null)
                    {
                        MeleeCollision? SourceCollision = Melee.projectilePrefab.GetComponent<MeleeCollision>();
                        bool FxMatch = SourceCollision != null && FxHasFramesWithPrefix(SourceCollision.swingFxPrefab, originalPrefix);
                        if (SourceCollision != null && (FxMatch || !Mathf.Approximately(collisionScale, 1f)))
                        {
                            GameObject Projectile = CloneInactive(Melee.projectilePrefab);
                            SwingFxCounter++;
                            Projectile.name = "ModMeleeProjectile_" + SwingFxCounter;
                            Projectile.SetActive(false);
                            Object.DontDestroyOnLoad(Projectile);
                            Projectile.AddComponent<ModSpawnTemplate>();

                            MeleeCollision? Collision = Projectile.GetComponent<MeleeCollision>();
                            if (Collision != null)
                            {
                                if (FxMatch)
                                {
                                    GameObject? CollisionFx = ReskinFxByOrder(Collision.swingFxPrefab, Ordered, originalPrefix, extendFrames, swingScale);
                                    if (CollisionFx != null) Collision.swingFxPrefab = CollisionFx;
                                }
                                ScaleMeleeCollision(Collision, collisionScale);
                            }

                            Melee.projectilePrefab = Projectile;
                            RegisterModNetworkPrefab(Projectile);
                            Matched = true;
                        }
                    }

                    if (!Matched)
                    {
                        StarSephiriaMod.LogError($"[ModFireData] 近战换皮没有命中任何帧（前缀 {originalPrefix}）");
                    }
                });
                return this;
            }

            /// <summary>
            /// 近战（NewWeaponFireData_MeleeAttack）：设置伤害体 MeleeCollision 的攻击次数和频率。
            /// multiHit = 总命中次数（1 = 单段）；hitInterval = 每段间隔（秒）；
            /// duration &lt; 0 时保留原持续时间，否则设为该值。
            /// 注意持续时间至少要 (multiHit - 1) * hitInterval，否则打不满。
            /// 建议放在 SetMeleeSwingSpriteFolder 之后调用，这样会复用已经克隆好的伤害体。
            /// </summary>
            public ModFireData SetMeleeMultiHit(int MultiHit, float HitInterval, float Duration = -1f)
            {
                Builders.Add(Data =>
                {
                    if (Data is not NewWeaponFireData_MeleeAttack Melee || Melee.projectilePrefab == null)
                    {
                        StarSephiriaMod.LogError("[ModFireData] SetMeleeMultiHit 需要 NewWeaponFireData_MeleeAttack 且 projectilePrefab 不为空");
                        return;
                    }

                    GameObject Projectile = Melee.projectilePrefab;
                    MeleeCollision? Collision = Projectile.GetComponent<MeleeCollision>();
                    if (Collision == null)
                    {
                        StarSephiriaMod.LogError("[ModFireData] SetMeleeMultiHit 伤害体上没有 MeleeCollision");
                        return;
                    }

                    // 还是原版 prefab 的话先克隆，避免改到共用资源
                    if (Projectile.GetComponent<ModAssetId>() == null)
                    {
                        Projectile = CloneInactive(Projectile);
                        SwingFxCounter++;
                        Projectile.name = "ModMeleeProjectile_" + SwingFxCounter;
                        Projectile.SetActive(false);
                        Object.DontDestroyOnLoad(Projectile);
                        Projectile.AddComponent<ModSpawnTemplate>();
                        RegisterModNetworkPrefab(Projectile);

                        Collision = Projectile.GetComponent<MeleeCollision>();
                        Melee.projectilePrefab = Projectile;
                    }

                    Collision.multiHit = Mathf.Max(1, MultiHit);
                    Collision.multiHitIntervalTimer.time = Mathf.Max(0.01f, HitInterval);
                    if (Duration >= 0f) Collision.durationTimer.time = Duration;

                    float Needed = (Collision.multiHit - 1) * Collision.multiHitIntervalTimer.time + 0.05f;
                    if (Collision.durationTimer.time < Needed)
                    {
                        StarSephiriaMod.LogError($"[ModFireData] 持续时间 {Collision.durationTimer.time:0.##}s 不足以打满 {Collision.multiHit} 段（至少需要约 {Needed:0.##}s）");
                    }
                });
                return this;
            }

            public ModFireData SetSwingSpriteDebug(bool Enable = true)
            {
                SwingSpriteDebug = Enable;
                return this;
            }

            /// <summary>
            /// 刀光整体大小倍率（相对原版 FX）。默认 1。
            /// </summary>
            public ModFireData SetSwingScale(float Scale)
            {
                SwingScale = Scale <= 0f ? 1f : Scale;
                return this;
            }

            /// <summary>
            /// 是否让刀光跟随武器范围（WeaponRange）放大。默认跟随；固定大小传 false。
            /// </summary>
            public ModFireData SetSwingRangeScaling(bool Enable)
            {
                Builders.Add(Data => Data.relatedToMeleeAttackRangeBonus = Enable);
                return this;
            }

            public ModFireData SetHitStop(float Time)
            {
                Builders.Add(Data => Data.hitStopTime = Time);
                return this;
            }

            private void ApplySwingFx(NewWeaponFireData Data)
            {
                GameObject? SourceFx = Data.swingFxPrefab;
                if (SourceFx == null) return;

                bool NeedSprite = SwingSpriteMap.Count > 0 || SwingSpriteFolders.Count > 0;
                bool NeedScale = !Mathf.Approximately(SwingScale, 1f);
                if (!NeedSprite && !NeedScale) return;

                Dictionary<string, Sprite> Sprites = new(StringComparer.OrdinalIgnoreCase);
                List<List<Sprite>> ExtendSets = [];
                if (NeedSprite)
                {
                    foreach (KeyValuePair<string, (string Name, string Path)> Pair in SwingSpriteMap)
                    {
                        Sprite? Value = string.IsNullOrEmpty(Pair.Value.Path)
                            ? Tools.LoadModSprite(Pair.Value.Name)
                            : Tools.LoadModSpriteForPath(Pair.Value.Path, Pair.Value.Name);
                        if (Value != null) Sprites[Pair.Key] = Value;
                    }

                    if (SwingSpriteFolders.Count > 0)
                    {
                        Assembly assembly = Assembly.GetExecutingAssembly();
                        string[] Resources = assembly.GetManifestResourceNames();
                        foreach (string Folder in SwingSpriteFolders)
                        {
                            string Prefix = "SPMod.Sprites." + Folder + ".";
                            List<Sprite> FolderSprites = [];
                            foreach (string Resource in Resources)
                            {
                                if (!Resource.StartsWith(Prefix, StringComparison.Ordinal)) continue;
                                if (!Resource.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;

                                string Name = Resource.Substring(Prefix.Length, Resource.Length - Prefix.Length - 4);
                                Sprite? Value = Tools.LoadModSpriteForPath(Folder, Name);
                                if (Value == null) continue;

                                FolderSprites.Add(Value);
                                if (!Sprites.ContainsKey(Name)) Sprites[Name] = Value;
                            }

                            if (ExtendFrameFolders.Contains(Folder) && FolderSprites.Count > 0)
                            {
                                FolderSprites.Sort((A, B) => CompareSpriteNames(A.name, B.name));
                                ExtendSets.Add(FolderSprites);
                            }
                        }
                    }

                    if (Sprites.Count == 0)
                    {
                        StarSephiriaMod.LogError("[ModFireData] 刀光换皮没有加载到任何贴图");
                        NeedSprite = false;
                    }
                }

                if (!NeedSprite && !NeedScale) return;

                bool SourceActive = SourceFx.activeSelf;
                GameObject Fx;
                try
                {
                    if (SourceActive) SourceFx.SetActive(false);
                    Fx = Object.Instantiate(SourceFx);
                }
                finally
                {
                    if (SourceActive) SourceFx.SetActive(true);
                }

                SwingFxCounter++;
                Fx.name = "ModSwingFx_" + SwingFxCounter;
                Fx.SetActive(false);
                Object.DontDestroyOnLoad(Fx);

                if (NeedScale) Fx.transform.localScale *= SwingScale;

                int FrameCount = 0;
                int SimpleCount = 0;
                int RendererCount = 0;

                if (NeedSprite)
                {
                    Dictionary<AnimationSet, AnimationSet> Cache = [];
                    foreach (Animator2D_Basic Animator in Fx.GetComponentsInChildren<Animator2D_Basic>(true))
                    {
                        if (Animator == null || Animator.currentSet == null) continue;

                        AnimationSet Original = Animator.currentSet;
                        if (!Cache.TryGetValue(Original, out AnimationSet? Set) || Set == null)
                        {
                            Set = Object.Instantiate(Original);
                            foreach (AnimationSet.StateInfo State in Set.sprites)
                            {
                                if (State?.timeline == null) continue;
                                foreach (AnimationSet.StateInfo.SpriteKeyFrame Frame in State.timeline)
                                {
                                    if (Frame?.sprite == null) continue;
                                    if (!Sprites.TryGetValue(Frame.sprite.name, out Sprite? Replacement) || Replacement == null) continue;

                                    Frame.sprite = FixSwingPivot(Frame.sprite, Replacement);
                                    FrameCount++;
                                }
                            }
                            Cache[Original] = Set;
                        }

                        Animator.currentSet = Set;
                        if (Animator.IsReady && AnimatorManager.Instance != null) Animator.Initialize(Set);
                    }

                    foreach (SimpleAnimator2D Animator in Fx.GetComponentsInChildren<SimpleAnimator2D>(true))
                    {
                        if (Animator == null) continue;
                        SimpleCount += ReplaceSwingArray(Animator.sprites, Sprites);
                    }
                    foreach (SimpleAnimator2D_FX Animator in Fx.GetComponentsInChildren<SimpleAnimator2D_FX>(true))
                    {
                        if (Animator == null) continue;
                        SimpleCount += ReplaceSwingArray(Animator.sprites, Sprites);
                    }

                    foreach (SpriteRenderer Renderer in Fx.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        if (Renderer == null || Renderer.sprite == null) continue;
                        if (!Sprites.TryGetValue(Renderer.sprite.name, out Sprite? Replacement) || Replacement == null) continue;

                        Renderer.sprite = FixSwingPivot(Renderer.sprite, Replacement);
                        RendererCount++;
                    }

                    if (SwingSpriteDebug || FrameCount + SimpleCount + RendererCount == 0)
                    {
                        List<string> Seen = CollectSwingSpriteNames(Fx);
                        if (SwingSpriteDebug)
                        {
                            StarSephiriaMod.LogInfo($"[ModFireData] 刀光换皮: AnimationSet {FrameCount} 帧, SimpleAnimator {SimpleCount} 帧, SpriteRenderer {RendererCount} 个; 看到的 sprite: {string.Join(", ", Seen)}");
                        }
                        if (FrameCount + SimpleCount + RendererCount == 0)
                        {
                            StarSephiriaMod.LogError($"[ModFireData] 刀光换皮没有命中任何 sprite；看到的 sprite: {string.Join(", ", Seen)}");
                        }
                    }
                }

                if (ExtendSets.Count > 0)
                {
                    int Added = ExtendSwingFrames(Fx, ExtendSets);
                    if (SwingSpriteDebug && Added > 0)
                    {
                        StarSephiriaMod.LogInfo($"[ModFireData] 刀光追加帧: {Added} 帧");
                    }
                }

                if (SwingSpriteDebug)
                {
                    StarSephiriaMod.LogInfo($"[ModFireData] 刀光大小: x{SwingScale}, 跟随武器范围 {Data.relatedToMeleeAttackRangeBonus}");
                }

                Data.swingFxPrefab = Fx;
                RegisterSwingFx(Fx);
            }

            /// <summary>
            /// 把扩展文件夹里多出来的图按顺序追加到 FX 的动画末尾。
            /// 只处理“现有的帧全部来自这个文件夹”的动画，避免误伤同一 FX 里的其它贴图。
            /// </summary>
            private static int ExtendSwingFrames(GameObject Fx, List<List<Sprite>> ExtendSets)
            {
                int Added = 0;
                foreach (List<Sprite> Ordered in ExtendSets)
                {
                    HashSet<string> Names = new(Ordered.Select(Sprite => Sprite.name), StringComparer.OrdinalIgnoreCase);

                    foreach (SimpleAnimator2D Animator in Fx.GetComponentsInChildren<SimpleAnimator2D>(true))
                    {
                        if (Animator == null || Animator.sprites == null || Animator.sprites.Length == 0) continue;
                        if (!Animator.sprites.All(Sprite => Sprite != null && Names.Contains(Sprite.name))) continue;

                        List<Sprite> Extras = Ordered
                            .Where(Sprite => Animator.sprites.All(Own => Own == null || !string.Equals(Own.name, Sprite.name, StringComparison.OrdinalIgnoreCase)))
                            .ToList();
                        if (Extras.Count == 0) continue;

                        Animator.sprites = [.. Animator.sprites, .. Extras];
                        Added += Extras.Count;
                    }

                    foreach (SimpleAnimator2D_FX Animator in Fx.GetComponentsInChildren<SimpleAnimator2D_FX>(true))
                    {
                        if (Animator == null || Animator.sprites == null || Animator.sprites.Length == 0) continue;
                        if (!Animator.sprites.All(Sprite => Sprite != null && Names.Contains(Sprite.name))) continue;

                        List<Sprite> Extras = Ordered
                            .Where(Sprite => Animator.sprites.All(Own => Own == null || !string.Equals(Own.name, Sprite.name, StringComparison.OrdinalIgnoreCase)))
                            .ToList();
                        if (Extras.Count == 0) continue;

                        Animator.sprites = [.. Animator.sprites, .. Extras];
                        Added += Extras.Count;
                    }

                    foreach (Animator2D_Basic Animator in Fx.GetComponentsInChildren<Animator2D_Basic>(true))
                    {
                        AnimationSet? Set = Animator?.currentSet;
                        if (Set?.sprites == null) continue;

                        foreach (AnimationSet.StateInfo State in Set.sprites)
                        {
                            if (State?.timeline == null || State.timeline.Count == 0) continue;
                            if (!State.timeline.All(Frame => Frame?.sprite != null && Names.Contains(Frame.sprite.name))) continue;

                            List<Sprite> Extras = Ordered
                                .Where(Sprite => State.timeline.All(Frame => Frame?.sprite == null || !string.Equals(Frame.sprite.name, Sprite.name, StringComparison.OrdinalIgnoreCase)))
                                .ToList();
                            if (Extras.Count == 0) continue;

                            int MaxIdx = State.timeline.Max(Frame => Frame?.frameIdx ?? 0);
                            foreach (Sprite Extra in Extras)
                            {
                                MaxIdx += 1;
                                State.timeline.Add(new AnimationSet.StateInfo.SpriteKeyFrame { frameIdx = MaxIdx, sprite = Extra });
                                Added += 1;
                            }
                        }
                    }
                }
                return Added;
            }

            // 名字按“前缀 + 末尾数字”排序，Tornado2 排在 Tornado10 前面。
            private static int CompareSpriteNames(string A, string B)
            {
                int StartA = A.Length;
                while (StartA > 0 && char.IsDigit(A[StartA - 1])) StartA--;
                int StartB = B.Length;
                while (StartB > 0 && char.IsDigit(B[StartB - 1])) StartB--;

                int Prefix = string.Compare(A[..StartA], B[..StartB], StringComparison.OrdinalIgnoreCase);
                if (Prefix != 0) return Prefix;

                long NumA = 0;
                long NumB = 0;
                bool HasNumA = StartA < A.Length && long.TryParse(A[StartA..], out NumA);
                bool HasNumB = StartB < B.Length && long.TryParse(B[StartB..], out NumB);
                if (HasNumA && HasNumB) return NumA.CompareTo(NumB);
                return string.Compare(A, B, StringComparison.OrdinalIgnoreCase);
            }

            private static GameObject CloneInactive(GameObject Source)
            {
                bool Active = Source.activeSelf;
                try
                {
                    if (Active) Source.SetActive(false);
                    return Object.Instantiate(Source);
                }
                finally
                {
                    if (Active) Source.SetActive(true);
                }
            }

            private static void RegisterModNetworkPrefab(GameObject Prefab)
            {
                NetworkIdentity? NetId = Prefab.GetComponent<NetworkIdentity>();
                if (NetId != null) Object.DestroyImmediate(NetId);

                uint AssetId = LoadModContent.LoadModAssetId();
                ModAssetId? ModId = Prefab.GetComponent<ModAssetId>();
                if (ModId == null) ModId = Prefab.AddComponent<ModAssetId>();
                ModId.AssetId = AssetId;

                StarSephiriaMod.registered[AssetId] = Prefab;
                StarSephiriaMod.RegisterModPrefab(Prefab);
            }

            private static void ScaleMeleeCollision(MeleeCollision Collision, float Scale)
            {
                if (Collision == null || Mathf.Approximately(Scale, 1f)) return;

                switch (Collision)
                {
                    case MeleeCollision_Circle_Distance Distance:
                        Distance.radius *= Scale;
                        break;
                    case MeleeCollision_Circle Circle:
                        Circle.radius *= Scale;
                        break;
                    case MeleeCollision_Rectangle Rectangle:
                        Rectangle.size *= Scale;
                        Rectangle.offset *= Scale;
                        break;
                    case MeleeCollision_Capsule Capsule:
                        Capsule.size *= Scale;
                        Capsule.offset *= Scale;
                        break;
                    default:
                        StarSephiriaMod.LogError($"[ModFireData] 未支持的 MeleeCollision 类型，判定范围没有缩放：{Collision.GetType().Name}");
                        break;
                }
            }

            private static bool FxHasFramesWithPrefix(GameObject? Fx, string Prefix)
            {
                if (Fx == null || string.IsNullOrEmpty(Prefix)) return false;

                foreach (SimpleAnimator2D Animator in Fx.GetComponentsInChildren<SimpleAnimator2D>(true))
                {
                    if (Animator != null && Animator.sprites != null && Animator.sprites.Any(Sprite => Sprite != null && Sprite.name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))) return true;
                }
                foreach (SimpleAnimator2D_FX Animator in Fx.GetComponentsInChildren<SimpleAnimator2D_FX>(true))
                {
                    if (Animator != null && Animator.sprites != null && Animator.sprites.Any(Sprite => Sprite != null && Sprite.name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))) return true;
                }
                foreach (Animator2D_Basic Animator in Fx.GetComponentsInChildren<Animator2D_Basic>(true))
                {
                    if (Animator?.currentSet?.sprites == null) continue;
                    foreach (AnimationSet.StateInfo State in Animator.currentSet.sprites)
                    {
                        if (State?.timeline != null && State.timeline.Any(Frame => Frame?.sprite != null && Frame.sprite.name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))) return true;
                    }
                }
                return false;
            }

            private static List<Sprite> LoadFxSpriteFolderOrdered(string Path)
            {
                List<Sprite> Result = [];
                if (string.IsNullOrEmpty(Path)) return Result;

                string Prefix = "SPMod.Sprites." + Path + ".";
                foreach (string Resource in Assembly.GetExecutingAssembly().GetManifestResourceNames())
                {
                    if (!Resource.StartsWith(Prefix, StringComparison.Ordinal)) continue;
                    if (!Resource.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;

                    string Name = Resource.Substring(Prefix.Length, Resource.Length - Prefix.Length - 4);
                    Sprite? Value = Tools.LoadModSpriteForPath(Path, Name);
                    if (Value != null) Result.Add(Value);
                }
                Result.Sort((A, B) => CompareSpriteNames(A.name, B.name));
                return Result;
            }

            private GameObject? ReskinFxByOrder(GameObject? SourceFx, List<Sprite> Ordered, string Prefix, bool ExtendFrames, float Scale)
            {
                if (SourceFx == null || Ordered.Count == 0 || string.IsNullOrEmpty(Prefix)) return null;
                if (!FxHasFramesWithPrefix(SourceFx, Prefix)) return null;

                GameObject Fx = CloneInactive(SourceFx);
                SwingFxCounter++;
                Fx.name = "ModSwingFx_" + SwingFxCounter;
                Fx.SetActive(false);
                Object.DontDestroyOnLoad(Fx);

                if (!Mathf.Approximately(Scale, 1f)) Fx.transform.localScale *= Scale;

                Dictionary<AnimationSet, AnimationSet> Cache = [];
                foreach (Animator2D_Basic Animator in Fx.GetComponentsInChildren<Animator2D_Basic>(true))
                {
                    if (Animator == null || Animator.currentSet == null) continue;

                    AnimationSet Original = Animator.currentSet;
                    if (!Cache.TryGetValue(Original, out AnimationSet? Set) || Set == null)
                    {
                        Set = Object.Instantiate(Original);
                        Cache[Original] = Set;
                    }

                    foreach (AnimationSet.StateInfo State in Set.sprites)
                    {
                        if (State?.timeline == null || State.timeline.Count == 0) continue;

                        List<AnimationSet.StateInfo.SpriteKeyFrame> Frames = State.timeline.OrderBy(Frame => Frame.frameIdx).ToList();
                        if (!Frames.All(Frame => Frame?.sprite != null && Frame.sprite.name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))) continue;

                        Sprite LastOrigin = Frames[^1].sprite;
                        int ReplaceCount = Math.Min(Frames.Count, Ordered.Count);
                        for (int i = 0; i < ReplaceCount; i++)
                        {
                            Frames[i].sprite = FixSwingPivot(Frames[i].sprite, Ordered[i]);
                        }

                        if (ExtendFrames && Ordered.Count > Frames.Count)
                        {
                            int MaxIdx = Frames.Max(Frame => Frame.frameIdx);
                            for (int i = Frames.Count; i < Ordered.Count; i++)
                            {
                                MaxIdx += 1;
                                State.timeline.Add(new AnimationSet.StateInfo.SpriteKeyFrame { frameIdx = MaxIdx, sprite = FixSwingPivot(LastOrigin, Ordered[i]) });
                            }
                        }
                    }

                    Animator.currentSet = Set;
                    if (Animator.IsReady && AnimatorManager.Instance != null) Animator.Initialize(Set);
                }

                foreach (SimpleAnimator2D Animator in Fx.GetComponentsInChildren<SimpleAnimator2D>(true))
                {
                    if (Animator == null || Animator.sprites == null || Animator.sprites.Length == 0) continue;
                    if (!Animator.sprites.All(Sprite => Sprite != null && Sprite.name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))) continue;
                    Animator.sprites = ReplaceSpritesByOrder(Animator.sprites, Ordered, ExtendFrames);
                }

                foreach (SimpleAnimator2D_FX Animator in Fx.GetComponentsInChildren<SimpleAnimator2D_FX>(true))
                {
                    if (Animator == null || Animator.sprites == null || Animator.sprites.Length == 0) continue;
                    if (!Animator.sprites.All(Sprite => Sprite != null && Sprite.name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))) continue;
                    Animator.sprites = ReplaceSpritesByOrder(Animator.sprites, Ordered, ExtendFrames);
                }

                RegisterSwingFx(Fx);
                return Fx;
            }

            private Sprite[] ReplaceSpritesByOrder(Sprite[] Original, List<Sprite> Ordered, bool ExtendFrames)
            {
                bool Extend = ExtendFrames && Ordered.Count > Original.Length;
                int Length = Extend ? Ordered.Count : Original.Length;
                Sprite[] Result = new Sprite[Length];

                for (int i = 0; i < Length; i++)
                {
                    if (i < Original.Length && i < Ordered.Count)
                        Result[i] = FixSwingPivot(Original[i], Ordered[i]);
                    else if (i < Original.Length)
                        Result[i] = Original[i];
                    else
                        Result[i] = FixSwingPivot(Original[^1], Ordered[i]);
                }
                return Result;
            }

            private static Dictionary<string, Sprite> LoadFxSpriteFolder(string Path)
            {
                Dictionary<string, Sprite> Sprites = new(StringComparer.OrdinalIgnoreCase);
                if (string.IsNullOrEmpty(Path)) return Sprites;

                string Prefix = "SPMod.Sprites." + Path + ".";
                foreach (string Resource in Assembly.GetExecutingAssembly().GetManifestResourceNames())
                {
                    if (!Resource.StartsWith(Prefix, StringComparison.Ordinal)) continue;
                    if (!Resource.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;

                    string Name = Resource.Substring(Prefix.Length, Resource.Length - Prefix.Length - 4);
                    if (Sprites.ContainsKey(Name)) continue;

                    Sprite? Value = Tools.LoadModSpriteForPath(Path, Name);
                    if (Value != null) Sprites[Name] = Value;
                }
                return Sprites;
            }

            private GameObject? CloneFxWithSprites(GameObject? Source, Dictionary<string, Sprite> Sprites, string Label)
            {
                if (Source == null) return null;

                bool SourceActive = Source.activeSelf;
                GameObject Fx;
                try
                {
                    if (SourceActive) Source.SetActive(false);
                    Fx = Object.Instantiate(Source);
                }
                finally
                {
                    if (SourceActive) Source.SetActive(true);
                }

                SwingFxCounter++;
                Fx.name = $"Mod{Label}_{SwingFxCounter}";
                Fx.SetActive(false);
                Object.DontDestroyOnLoad(Fx);

                int FrameCount = 0;
                int SimpleCount = 0;
                int RendererCount = 0;

                Dictionary<AnimationSet, AnimationSet> Cache = [];
                foreach (Animator2D_Basic Animator in Fx.GetComponentsInChildren<Animator2D_Basic>(true))
                {
                    if (Animator == null || Animator.currentSet == null) continue;

                    AnimationSet Original = Animator.currentSet;
                    if (!Cache.TryGetValue(Original, out AnimationSet? Set) || Set == null)
                    {
                        Set = Object.Instantiate(Original);
                        foreach (AnimationSet.StateInfo State in Set.sprites)
                        {
                            if (State?.timeline == null) continue;
                            foreach (AnimationSet.StateInfo.SpriteKeyFrame Frame in State.timeline)
                            {
                                if (Frame?.sprite == null) continue;
                                if (!Sprites.TryGetValue(Frame.sprite.name, out Sprite? Replacement) || Replacement == null) continue;

                                Frame.sprite = FixSwingPivot(Frame.sprite, Replacement);
                                FrameCount++;
                            }
                        }
                        Cache[Original] = Set;
                    }

                    Animator.currentSet = Set;
                    if (Animator.IsReady && AnimatorManager.Instance != null) Animator.Initialize(Set);
                }

                foreach (SimpleAnimator2D Animator in Fx.GetComponentsInChildren<SimpleAnimator2D>(true))
                {
                    if (Animator != null) SimpleCount += ReplaceSwingArray(Animator.sprites, Sprites);
                }
                foreach (SimpleAnimator2D_FX Animator in Fx.GetComponentsInChildren<SimpleAnimator2D_FX>(true))
                {
                    if (Animator != null) SimpleCount += ReplaceSwingArray(Animator.sprites, Sprites);
                }

                foreach (SpriteRenderer Renderer in Fx.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (Renderer == null || Renderer.sprite == null) continue;
                    if (!Sprites.TryGetValue(Renderer.sprite.name, out Sprite? Replacement) || Replacement == null) continue;

                    Renderer.sprite = FixSwingPivot(Renderer.sprite, Replacement);
                    RendererCount++;
                }

                if (FrameCount + SimpleCount + RendererCount == 0)
                {
                    StarSephiriaMod.LogError($"[ModFireData] {Label} 换皮没有命中任何 sprite；看到的 sprite: {string.Join(", ", CollectSwingSpriteNames(Fx))}");
                }

                RegisterSwingFx(Fx);
                return Fx;
            }

            private Sprite FixSwingPivot(Sprite Origin, Sprite Replacement)
            {
                if (SwingPivotCache.TryGetValue((Origin, Replacement), out Sprite? Cached) && Cached != null)
                    return Cached;

                Vector2 Pivot = new(
                    Origin.pivot.x / Origin.rect.width,
                    Origin.pivot.y / Origin.rect.height);
                Sprite Fixed = Sprite.Create(Replacement.texture, Replacement.rect, Pivot, Origin.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                Fixed.name = Replacement.name;
                SwingPivotCache[(Origin, Replacement)] = Fixed;
                return Fixed;
            }

            private int ReplaceSwingArray(Sprite[] Sprites, Dictionary<string, Sprite> Map)
            {
                int Count = 0;
                if (Sprites == null) return 0;

                for (int i = 0; i < Sprites.Length; i++)
                {
                    Sprite? Original = Sprites[i];
                    if (Original == null) continue;
                    if (!Map.TryGetValue(Original.name, out Sprite? Replacement) || Replacement == null) continue;

                    Sprites[i] = FixSwingPivot(Original, Replacement);
                    Count++;
                }
                return Count;
            }

            private static List<string> CollectSwingSpriteNames(GameObject Fx)
            {
                List<string> Names = [];

                void Add(Sprite? Sprite)
                {
                    if (Sprite == null || Names.Count >= 80) return;
                    if (!Names.Contains(Sprite.name)) Names.Add(Sprite.name);
                }

                foreach (Animator2D_Basic Animator in Fx.GetComponentsInChildren<Animator2D_Basic>(true))
                {
                    if (Animator == null || Animator.currentSet == null) continue;
                    foreach (AnimationSet.StateInfo State in Animator.currentSet.sprites)
                    {
                        if (State?.timeline == null) continue;
                        foreach (AnimationSet.StateInfo.SpriteKeyFrame Frame in State.timeline) Add(Frame?.sprite);
                    }
                }
                foreach (SimpleAnimator2D Animator in Fx.GetComponentsInChildren<SimpleAnimator2D>(true))
                {
                    if (Animator == null || Animator.sprites == null) continue;
                    foreach (Sprite Sprite in Animator.sprites) Add(Sprite);
                }
                foreach (SimpleAnimator2D_FX Animator in Fx.GetComponentsInChildren<SimpleAnimator2D_FX>(true))
                {
                    if (Animator == null || Animator.sprites == null) continue;
                    foreach (Sprite Sprite in Animator.sprites) Add(Sprite);
                }
                foreach (SpriteRenderer Renderer in Fx.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (Renderer == null) continue;
                    Add(Renderer.sprite);
                }
                return Names;
            }

            private static void RegisterSwingFx(GameObject Fx)
            {
                if (Fx == null) return;
                if (!SwingFxPrefabs.Contains(Fx)) SwingFxPrefabs.Add(Fx);//不管池子当前在不在，都要留底
                if (SpriteFx.Pool == null) return;
                TryRegisterSwingFx(Fx);
            }

            public static void RegisterAllFxToPool()
            {
                if (SpriteFx.Pool == null || SwingFxPrefabs.Count == 0) return;

                SwingFxPrefabs.RemoveAll(Fx => Fx == null);//源 FX 是 DontDestroyOnLoad 的，这里只清真正被销毁的
                foreach (GameObject Fx in SwingFxPrefabs) TryRegisterSwingFx(Fx);
            }

            private static void TryRegisterSwingFx(GameObject Fx)
            {
                if (Fx == null || SpriteFx.Pool == null) return;

                MethodInfo? MakePool = AccessTools.Method(typeof(ObjectPoolingFactory<SpriteFx>), "MakePool");
                FieldInfo? PooledField = AccessTools.Field(typeof(ObjectPoolingFactory<SpriteFx>), "pooledGameObjects");
                if (MakePool == null || PooledField == null) return;
                if (PooledField.GetValue(SpriteFx.Pool) is not Dictionary<GameObject, List<SpriteFx>> Pooled) return;
                if (Pooled.ContainsKey(Fx)) return;

                bool Active = Fx.activeSelf;
                Fx.SetActive(true);
                try
                {
                    MakePool.Invoke(SpriteFx.Pool, [null!, Fx]);
                }
                catch (Exception Ex)
                {
                    StarSephiriaMod.LogError($"[ModFireData] 注册刀光 FX 到 SpriteFx.Pool 失败: {Fx.name} {Ex.Message}");
                }
                finally
                {
                    Fx.SetActive(Active);
                }
            }

            public T As<T>() where T : NewWeaponFireData
            {
                return (T)Data;
            }
        }

        public class ModWeapon
        {
            public int Id;
            public string Name;
            public int TemplateId;
            public int EnhanceFromId;
            public string IconName;
            public bool SpriteDebug = false;

            public uint AssetId { get; private set; }
            public WeaponEntity? WeaponEntity { get; private set; }
            public GameObject? Prefab { get; private set; }
            public WeaponSimple? Simple { get; private set; }

            private readonly List<Action<WeaponSimple>> Builders = [];
            private readonly Dictionary<string, (string Name, string Path)> SpriteMap = [];
            private readonly List<string> SpriteFolders = [];
            private readonly Dictionary<(Sprite Origin, Sprite Replacement), Sprite> SpritePivotCache = [];
            public ModWeapon(string name, int id, int templateId)
            {
                Name = name;
                Id = id;
                TemplateId = templateId;
                IconName = "ModWeapon_" + name + "_Icon";
            }
            public ModWeapon CustomSet(Action<ModWeapon> Set)
            {
                Set.Invoke(this);
                return this;
            }
            public ModWeapon SetEnhanceFrom(int TargetId)
            {
                EnhanceFromId = TargetId;
                return this;
            }
            public ModWeapon CustomCreateWeapon(Action<WeaponSimple> Action)
            {
                Builders.Add(Action);
                return this;
            }
            public ModWeapon SetCrossBowValue(float FireSpeed, float RoldSpeed, int MagazineCount, int MagazineCapacity)
            {
                Builders.Add(Weapon =>
                {
                    if (Weapon is WeaponSimple_Crossbow Crossbow)
                    {
                        Crossbow.fireIntervalTimer.time = FireSpeed;
                        Crossbow.reloadTime = RoldSpeed;
                        Crossbow.defaultMagazineCount = MagazineCount;
                        Crossbow.defaultMagazineCapacity = MagazineCapacity;
                    }
                });
                return this;
            }

            public ModWeapon SetSprite(string original, string modName, string path = "")
            {
                SpriteMap[original] = (modName, path);
                return this;
            }

            /// <summary>
            /// 自动把 Sprites/&lt;Path&gt;/ 下所有 PNG 按“文件名 = 原版 sprite 名”加入换皮表。
            /// 例：SetSpriteFolder("Weapon.Katana_Wind")
            /// 显式 SetSprite 的优先级更高，文件夹只补缺失的键。
            /// </summary>
            public ModWeapon SetSpriteFolder(string Path)
            {
                if (!string.IsNullOrEmpty(Path)) SpriteFolders.Add(Path);
                return this;
            }
            public ModWeapon SetSpriteDebug(bool Enable = true)
            {
                SpriteDebug = Enable;
                return this;
            }
            public ModWeapon SetBasicAttack(int Index, ModFireData Fire)
            {
                Builders.Add(Weapon =>
                {
                    NewWeaponFireData? Data = Fire?.Data;
                    if (Data == null)
                    {
                        StarSephiriaMod.LogError($"[ModWeapon]{Name} SetBasicAttack: ModFireData 为空");
                        return;
                    }
                    if (Index < 0 || Index >= Weapon.basicComboAttacks.Length)
                    {
                        StarSephiriaMod.LogError($"[ModWeapon]{Name} basicComboAttacks 下标 {Index} 越界（长度 {Weapon.basicComboAttacks.Length}）");
                        return;
                    }
                    Weapon.basicComboAttacks[Index] = Data;
                });
                return this;
            }
            public ModWeapon SetDashAttack(int Index, ModFireData Fire)
            {
                Builders.Add(Weapon =>
                {
                    NewWeaponFireData? Data = Fire?.Data;
                    if (Data == null)
                    {
                        StarSephiriaMod.LogError($"[ModWeapon]{Name} SetDashAttack: ModFireData 为空");
                        return;
                    }
                    if (Index < 0 || Index >= Weapon.dashAttacks.Length)
                    {
                        StarSephiriaMod.LogError($"[ModWeapon]{Name} dashAttacks 下标 {Index} 越界（长度 {Weapon.dashAttacks.Length}）");
                        return;
                    }
                    Weapon.dashAttacks[Index] = Data;
                });
                return this;
            }
            public ModWeapon SetSpecialAttack(int Index, ModFireData Fire)
            {
                Builders.Add(Weapon =>
                {
                    NewWeaponFireData? Data = Fire?.Data;
                    if (Data == null)
                    {
                        StarSephiriaMod.LogError($"[ModWeapon]{Name} SetSpecialAttack: ModFireData 为空");
                        return;
                    }
                    if (Index < 0 || Index >= Weapon.specialAttacks.Length)
                    {
                        StarSephiriaMod.LogError($"[ModWeapon]{Name} specialAttacks 下标 {Index} 越界（长度 {Weapon.specialAttacks.Length}）");
                        return;
                    }
                    Weapon.specialAttacks[Index] = Data;
                });
                return this;
            }
            public ModWeapon SetBasicAttacks(params ModFireData[] Fires)
            {
                return SetAttackGroup(0, Fires, "basicComboAttacks", Weapon => Weapon.basicComboAttacks);
            }
            public ModWeapon SetBasicAttacks(int StartIndex, params ModFireData[] Fires)
            {
                return SetAttackGroup(StartIndex, Fires, "basicComboAttacks", Weapon => Weapon.basicComboAttacks);
            }
            public ModWeapon SetDashAttacks(params ModFireData[] Fires)
            {
                return SetAttackGroup(0, Fires, "dashAttacks", Weapon => Weapon.dashAttacks);
            }
            public ModWeapon SetDashAttacks(int StartIndex, params ModFireData[] Fires)
            {
                return SetAttackGroup(StartIndex, Fires, "dashAttacks", Weapon => Weapon.dashAttacks);
            }
            public ModWeapon SetSpecialAttacks(params ModFireData[] Fires)
            {
                return SetAttackGroup(0, Fires, "specialAttacks", Weapon => Weapon.specialAttacks);
            }
            public ModWeapon SetSpecialAttacks(int StartIndex, params ModFireData[] Fires)
            {
                return SetAttackGroup(StartIndex, Fires, "specialAttacks", Weapon => Weapon.specialAttacks);
            }
            public ModWeapon SetKatanaQuickDraw(ModFireData Fire)
            {
                Builders.Add(Weapon =>
                {
                    WeaponSimple_Katana? Katana = Weapon as WeaponSimple_Katana;
                    if (Katana == null)
                    {
                        StarSephiriaMod.LogError($"[ModWeapon]{Name} SetKatanaQuickDraw: 基底不是 WeaponSimple_Katana");
                        return;
                    }
                    NewWeaponFireData? Data = Fire?.Data;
                    if (Data == null)
                    {
                        StarSephiriaMod.LogError($"[ModWeapon]{Name} SetKatanaQuickDraw: ModFireData 为空");
                        return;
                    }
                    Katana.quickDrawFireData = Data;
                });
                return this;
            }
            public ModWeapon SetKatanaGreatDraw(ModFireData Fire)
            {
                Builders.Add(Weapon =>
                {
                    WeaponSimple_Katana? Katana = Weapon as WeaponSimple_Katana;
                    if (Katana == null)
                    {
                        StarSephiriaMod.LogError($"[ModWeapon]{Name} SetKatanaGreatDraw: 基底不是 WeaponSimple_Katana");
                        return;
                    }
                    NewWeaponFireData? Data = Fire?.Data;
                    if (Data == null)
                    {
                        StarSephiriaMod.LogError($"[ModWeapon]{Name} SetKatanaGreatDraw: ModFireData 为空");
                        return;
                    }
                    Katana.greatDrawFireData = Data;
                });
                return this;
            }

            private ModWeapon SetAttackGroup(int StartIndex, ModFireData[] Fires, string Label, Func<WeaponSimple, NewWeaponFireData[]> Select)
            {
                Builders.Add(Weapon =>
                {
                    if (Fires == null || Fires.Length == 0) return;

                    NewWeaponFireData[] Target = Select(Weapon);
                    if (StartIndex < 0 || StartIndex + Fires.Length > Target.Length)
                    {
                        StarSephiriaMod.LogError($"[ModWeapon]{Name} {Label} 范围越界：start={StartIndex}, count={Fires.Length}, length={Target.Length}");
                        return;
                    }

                    for (int i = 0; i < Fires.Length; i++)
                    {
                        NewWeaponFireData? Data = Fires[i]?.Data;
                        if (Data == null)
                        {
                            StarSephiriaMod.LogError($"[ModWeapon]{Name} {Label} 第 {i} 个 ModFireData 为空");
                            continue;
                        }
                        Target[StartIndex + i] = Data;
                    }
                });
                return this;
            }

            public WeaponEntity CreateWeapon()
            {
                if (WeaponEntity != null)
                {
                    if (WeaponDatabase.FindWeaponById(Id) == null)
                        WeaponDatabase.Register(WeaponEntity);
                    return WeaponEntity;
                }

                WeaponEntity? Template = WeaponDatabase.FindWeaponById(TemplateId);
                if (Template == null || Template.mainWeaponPrefab == null)
                {
                    StarSephiriaMod.LogError($"[ModWeapon]找不到模板武器{TemplateId}或它的 prefab");
                    return null!;
                }
                if (WeaponDatabase.FindWeaponById(Id) != null)
                {
                    StarSephiriaMod.LogError($"[ModWeapon]{Name} 的 ID: {Id} 已被占用");
                    return null!;
                }
                Prefab = CreatePrefab(Template, Id);
                Simple = Prefab.GetComponent<WeaponSimple>();
                if (Simple == null)
                {
                    StarSephiriaMod.LogError($"[ModWeapon]{Name}没有WeaponSimple");
                    return null!;
                }

                foreach (Action<WeaponSimple> Build in Builders)
                    Build(Simple);

                Dictionary<string, (string Name, string Path)> Map = BuildSpriteMap();
                if (Map.Count > 0)
                    ApplySpriteMap(Prefab, Map);

                WeaponEntity = CreateWeaponCore(Template, Id);
                WeaponDatabase.Register(WeaponEntity);
                return WeaponEntity;
            }

            private GameObject CreatePrefab(WeaponEntity Template, int Id)
            {
                GameObject Source = Template.mainWeaponPrefab;
                bool SourceActive = Source.activeSelf;
                GameObject NewPrefab;
                try
                {
                    if (SourceActive) Source.SetActive(false);
                    NewPrefab = Object.Instantiate(Source);
                }
                finally
                {
                    if (SourceActive) Source.SetActive(true);
                }

                NewPrefab.name = "ModWeaponEntity_" + Name;
                NewPrefab.SetActive(false);
                NewPrefab.AddComponent<ModSpawnTemplate>();
                Object.DontDestroyOnLoad(NewPrefab);
                NewPrefab.transform.position = new Vector3(100000f, 100000f, 0f);

                WeaponSimple Weapon = NewPrefab.GetComponent<WeaponSimple>();
                Weapon.entityId = Id;
                Weapon.basicComboAttacks = CloneFireData(Weapon.basicComboAttacks).ToArray();
                Weapon.specialAttacks = CloneFireData(Weapon.specialAttacks).ToArray();
                Weapon.dashAttacks = CloneFireData(Weapon.dashAttacks).ToArray();

                AssetId = LoadModContent.LoadModAssetId();

                NetworkIdentity? Identity = NewPrefab.GetComponent<NetworkIdentity>() ?? NewPrefab.AddComponent<NetworkIdentity>();
                ResetNetworkIdentity(Identity);
                LoadModContent.SetModAssetId(Identity, AssetId);

                ModAssetId ModId = NewPrefab.GetComponent<ModAssetId>() ?? NewPrefab.AddComponent<ModAssetId>();
                ModId.AssetId = AssetId;
                StarSephiriaMod.registered[AssetId] = NewPrefab;
                StarSephiriaMod.RegisterModPrefab(NewPrefab);
                return NewPrefab;
            }

            private static readonly MethodInfo? resetStateMethod = AccessTools.Method(typeof(NetworkIdentity), "ResetState");
            private static readonly FieldInfo? hasSpawnedField = AccessTools.Field(typeof(NetworkIdentity), "hasSpawned");
            private static readonly FieldInfo? sceneIdField = AccessTools.Field(typeof(NetworkIdentity), "sceneId");
            private static void ResetNetworkIdentity(NetworkIdentity Identity)
            {
                try
                {
                    resetStateMethod?.Invoke(Identity, null);
                    hasSpawnedField?.SetValue(Identity, false);
                    sceneIdField?.SetValue(Identity, 0uL);
                }
                catch (Exception Ex)
                {
                    StarSephiriaMod.LogError($"[ModWeapon] 重置 NetworkIdentity 失败: {Ex.Message}");
                }
            }

            private WeaponEntity CreateWeaponCore(WeaponEntity Template, int Id)
            {
                Sprite? Icon = Tools.LoadModSpriteForPath("Weapon", IconName);
                WeaponEntity Weapon = ScriptableObject.CreateInstance<WeaponEntity>();
                Weapon.name = "ModWeaponEntity_" + Name;
                Weapon.id = Id;
                Weapon.enabled = true;
                Weapon.activeState = WeaponEntity.EActiveState.Active;
                Weapon.aName = new LocalizedString("ModWeapon_" + Name);
                Weapon.icon = Icon ?? Template.icon;
                Weapon.bigIcon = Icon ?? Template.bigIcon;
                Weapon.mainWeaponPrefab = Prefab;
                Weapon.subWeaponPrefab = Template.subWeaponPrefab;
                Weapon.standardEnhancements = [];
                Weapon.isDefaultWeapon = false;
                Weapon.enhanceFromId = EnhanceFromId;
                return Weapon;
            }

            private List<NewWeaponFireData> CloneFireData(NewWeaponFireData[] Target)
            {
                List<NewWeaponFireData> DataList = [];
                foreach (NewWeaponFireData Data in Target)
                {
                    DataList.Add(Object.Instantiate(Data));
                }
                return DataList;
            }

            private Sprite FixPivot(Sprite Origin, Sprite Replacement)
            {
                if (SpritePivotCache.TryGetValue((Origin, Replacement), out Sprite? Cached) && Cached != null)
                    return Cached;

                Vector2 Pivot = new(
                    Origin.pivot.x / Origin.rect.width,
                    Origin.pivot.y / Origin.rect.height);
                Sprite Fixed = Sprite.Create(Replacement.texture, Replacement.rect, Pivot, Origin.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                Fixed.name = Replacement.name;
                SpritePivotCache[(Origin, Replacement)] = Fixed;
                return Fixed;
            }

            private string PathOf(Transform T)
            {
                string Result = T.name;
                Transform? Root = Prefab?.transform;
                while (T.parent != null && T.parent != Root)
                {
                    T = T.parent;
                    Result = T.name + "/" + Result;
                }
                return Result;
            }

            private Dictionary<string, (string Name, string Path)> BuildSpriteMap()
            {
                Dictionary<string, (string Name, string Path)> Map = new(SpriteMap);
                if (SpriteFolders.Count == 0) return Map;

                Assembly assembly = Assembly.GetExecutingAssembly();
                string[] Resources = assembly.GetManifestResourceNames();
                foreach (string Folder in SpriteFolders)
                {
                    string Prefix = "SPMod.Sprites." + Folder + ".";
                    foreach (string Resource in Resources)
                    {
                        if (!Resource.StartsWith(Prefix, StringComparison.Ordinal)) continue;
                        if (!Resource.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;

                        string Name = Resource.Substring(Prefix.Length, Resource.Length - Prefix.Length - 4);
                        if (!Map.ContainsKey(Name)) Map[Name] = (Name, Folder);
                    }
                }
                return Map;
            }

            private void ApplySpriteMap(GameObject Prefab, Dictionary<string, (string Name, string Path)> Map)
            {
                Dictionary<string, Sprite> Sprites = new(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, (string Name, string Path)> Pair in Map)
                {
                    Sprite? Value = string.IsNullOrEmpty(Pair.Value.Path)
                        ? Tools.LoadModSprite(Pair.Value.Name)
                        : Tools.LoadModSpriteForPath(Pair.Value.Path, Pair.Value.Name);
                    if (Value != null) Sprites[Pair.Key] = Value;
                }
                if (Sprites.Count == 0) return;

                HashSet<string> Used = new(StringComparer.OrdinalIgnoreCase);
                List<string> Seen = [];
                int FrameCount = 0;
                int SimpleCount = 0;
                int RendererCount = 0;

                if (SpriteDebug)
                {
                    foreach (SpriteRenderer Renderer in Prefab.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        if (Renderer == null) continue;
                        StarSephiriaMod.LogInfo(
                            $"[ModWeapon]{Name} renderer {PathOf(Renderer.transform)} sprite={(Renderer.sprite != null ? Renderer.sprite.name : "null")} " +
                            $"enabled={Renderer.enabled} active={Renderer.gameObject.activeInHierarchy} sort={Renderer.sortingLayerName}/{Renderer.sortingOrder} " +
                            $"mat={(Renderer.sharedMaterial != null ? Renderer.sharedMaterial.name : "null")}");
                    }
                }
                Dictionary<AnimationSet, AnimationSet> Cache = [];
                foreach (Animator2D_Basic Animator in Prefab.GetComponentsInChildren<Animator2D_Basic>(true))
                {
                    if (Animator == null || Animator.currentSet == null) continue;

                    AnimationSet Original = Animator.currentSet;
                    if (!Cache.TryGetValue(Original, out AnimationSet? Set) || Set == null)
                    {
                        Set = Object.Instantiate(Original);
                        foreach (AnimationSet.StateInfo State in Set.sprites)
                        {
                            if (State?.timeline == null) continue;
                            foreach (AnimationSet.StateInfo.SpriteKeyFrame Frame in State.timeline)
                            {
                                if (Frame?.sprite == null) continue;
                                if (SpriteDebug && Seen.Count < 80 && !Seen.Contains(Frame.sprite.name)) Seen.Add(Frame.sprite.name);
                                if (!Sprites.TryGetValue(Frame.sprite.name, out Sprite? Replacement) || Replacement == null) continue;

                                Used.Add(Frame.sprite.name);
                                Frame.sprite = FixPivot(Frame.sprite, Replacement);
                                FrameCount++;
                            }
                        }
                        Cache[Original] = Set;
                    }

                    Animator.currentSet = Set;
                    if (Animator.IsReady && AnimatorManager.Instance != null) Animator.Initialize(Set);
                }

                foreach (SimpleAnimator2D Animator in Prefab.GetComponentsInChildren<SimpleAnimator2D>(true))
                {
                    if (Animator == null) continue;
                    SimpleCount += ReplaceSpriteArray(Animator.sprites, Sprites, Used);
                }
                foreach (SimpleAnimator2D_FX Animator in Prefab.GetComponentsInChildren<SimpleAnimator2D_FX>(true))
                {
                    if (Animator == null) continue;
                    SimpleCount += ReplaceSpriteArray(Animator.sprites, Sprites, Used);
                }

                foreach (SpriteRenderer Renderer in Prefab.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (Renderer == null || Renderer.sprite == null) continue;
                    if (SpriteDebug && Seen.Count < 80 && !Seen.Contains(Renderer.sprite.name)) Seen.Add(Renderer.sprite.name);
                    if (!Sprites.TryGetValue(Renderer.sprite.name, out Sprite? Replacement) || Replacement == null) continue;

                    Used.Add(Renderer.sprite.name);
                    Renderer.sprite = FixPivot(Renderer.sprite, Replacement);
                    RendererCount++;
                }

                if (SpriteDebug)
                {
                    List<string> Missed = [];
                    foreach (string Key in Map.Keys)
                    {
                        if (!Used.Contains(Key)) Missed.Add(Key);
                    }

                    StarSephiriaMod.LogInfo($"[ModWeapon]{Name} 换皮: AnimationSet {FrameCount} 帧, SimpleAnimator {SimpleCount} 帧, SpriteRenderer {RendererCount} 个" +
                        (Missed.Count > 0 ? $"; 未命中: {string.Join(", ", Missed)}" : ""));

                    if (Missed.Count > 0 && Seen.Count > 0)
                    {
                        List<string> Unmapped = [];
                        foreach (string SeenName in Seen)
                        {
                            if (!Sprites.ContainsKey(SeenName) && !Unmapped.Contains(SeenName)) Unmapped.Add(SeenName);
                        }
                        if (Unmapped.Count > 0)
                            StarSephiriaMod.LogInfo($"[ModWeapon]{Name} prefab 上未映射的 sprite 名: {string.Join(", ", Unmapped)}");
                    }

                    if (FrameCount + SimpleCount + RendererCount == 0)
                        StarSephiriaMod.LogInfo($"[ModWeapon]{Name} 换皮没命中任何映射；prefab 上见到的 sprite 名: {string.Join(", ", Seen)}");
                }
                else if (FrameCount + SimpleCount + RendererCount == 0)
                {
                    StarSephiriaMod.LogError($"[ModWeapon]{Name} 换皮没有命中任何 sprite（检查 SetSprite 的原名）");
                }
            }

            private int ReplaceSpriteArray(Sprite[] Sprites, Dictionary<string, Sprite> Map, HashSet<string> Used)
            {
                int Count = 0;
                if (Sprites == null) return 0;

                for (int i = 0; i < Sprites.Length; i++)
                {
                    Sprite? Original = Sprites[i];
                    if (Original == null) continue;
                    if (!Map.TryGetValue(Original.name, out Sprite? Replacement) || Replacement == null) continue;

                    Used.Add(Original.name);
                    Sprites[i] = FixPivot(Original, Replacement);
                    Count++;
                }
                return Count;
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
                    component2.sprite = Tools.LoadModSprite($"ModEffectHUD_{Name}");
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

        public class ModCharacterDebuff
        {
            public string Name;
            public string IconName;
            public string FxName;
            public Color TextColor;
            public string VisualFrom = "";
            public GameObject? ThisDebuff = null;
            public CharacterDebuff? Prefab = null;
            public Action<GameObject>? SetDebuff = null;
            public ModCharacterDebuff(string name, string iconName, string fxName, Color color)
            {
                Name = name;
                IconName = iconName;
                FxName = fxName;
                TextColor = color;
            }
            public ModCharacterDebuff SetVisual(string from)
            {
                VisualFrom = from;
                return this;
            }
            public ModCharacterDebuff SetCustomDebuff(Action<GameObject> action)
            {
                SetDebuff = action;
                return this;
            }
            public ModCharacterDebuff EndCreate(int TilesX = 0)
            {
                GameObject ModPrefab = new($"ModCharacterDebuff_{Name}");
                Object.DontDestroyOnLoad(ModPrefab);
                SetDebuff?.Invoke(ModPrefab);
                CharacterDebuff? Debuff = ModPrefab.GetComponent<CharacterDebuff>();
                if (Debuff == null)
                {
                    StarSephiriaMod.LogError($"[ModCharacterDebuff]创建失败：{Name} 没有添加 CharacterDebuff 组件");
                    return this;
                }
                Debuff.enabled = false;
                Debuff.defaultDuration = 10f;
                if (VisualFrom != "")
                {
                    CharacterDebuff? Orig = UnitDatabase.GetDebuff(VisualFrom);
                    if (Orig != null)
                    {
                        if (Orig.buffStackUIPrefab != null)
                        {
                            var StackUI = Object.Instantiate(Orig.buffStackUIPrefab);
                            StackUI.stackText.color = TextColor;
                            Sprite? IconSprite = Tools.LoadModSprite(IconName);
                            if (IconSprite != null)
                            {
                                foreach (Image Icon in StackUI.GetComponentsInChildren<Image>(true))
                                {
                                    if (Icon.gameObject != StackUI.gameObject) Icon.sprite = IconSprite;
                                }
                            }
                            Object.DontDestroyOnLoad(StackUI);
                            Debuff.buffStackUIPrefab = StackUI;
                        }
                        if (Orig.loopFxPrefab != null)
                        {
                            GameObject LoopFx = Object.Instantiate(Orig.loopFxPrefab);
                            Texture2D? Tex = Tools.LoadModSpriteForPath("EffectFx", FxName)?.texture;
                            if (Tex != null && TilesX > 0)
                            {
                                foreach (ParticleSystemRenderer Rend in LoopFx.GetComponentsInChildren<ParticleSystemRenderer>(true))
                                {
                                    Rend.material.mainTexture = Tex;
                                }
                                foreach (ParticleSystem PS in LoopFx.GetComponentsInChildren<ParticleSystem>(true))
                                {
                                    ParticleSystem.TextureSheetAnimationModule Sheet = PS.textureSheetAnimation;
                                    if (Sheet.enabled)
                                    {
                                        Sheet.numTilesX = TilesX;
                                        Sheet.numTilesY = 1;
                                    }
                                    ParticleSystem.EmissionModule Em = PS.emission;
                                    Em.rateOverTimeMultiplier *= 0.5f;
                                }
                            }
                            Object.DontDestroyOnLoad(LoopFx);
                            Debuff.loopFxPrefab = LoopFx;
                        }
                    }
                    else StarSephiriaMod.LogError($"[ModCharacterDebuff]{Name} 找不到视觉来源减益：{VisualFrom}");
                }
                ModAssetId NetId = ModPrefab.AddComponent<ModAssetId>();
                NetId.AssetId = LoadModContent.LoadModAssetId();
                StarSephiriaMod.registered[NetId.AssetId] = ModPrefab;
                StarSephiriaMod.RegisterModPrefab(ModPrefab);
                ThisDebuff = ModPrefab;
                Prefab = Debuff;
                return this;
            }
        }

        public static class ModFxPool
        {
            private class PoolEntry
            {
                public List<Sprite> Sprites = [];
                public float Fps;
                public bool Loop;
                public int Max;
                public string SortingLayer = "";
                public int SortingOrder;
                public readonly Queue<SpriteFx_Simple> Free = new();
                public readonly HashSet<SpriteFx_Simple> FreeSet = [];
                public readonly List<SpriteFx_Simple> All = [];
                public int Cursor;
            }
            private static readonly Dictionary<string, PoolEntry> Pools = [];

            public static void Register(string key, int prewarm, int max, float fps, bool loop, params string[] frameNames)
            {
                Register(key, prewarm, max, fps, loop, "", 0, frameNames);
            }

            /// <summary>sortingLayer/sortingOrder 用来把 FX 放到更高的排序层（游戏里 Background→Ground→Default→FX→UI）。留空则保持 Default/0。</summary>
            public static void Register(string key, int prewarm, int max, float fps, bool loop, string sortingLayer, int sortingOrder, params string[] frameNames)
            {
                RegisterFromPath(key, "EffectFx", prewarm, max, fps, loop, sortingLayer, sortingOrder, frameNames);
            }

            public static void RegisterFromPath(string key, string path, int prewarm, int max, float fps, bool loop, params string[] frameNames)
            {
                RegisterFromPath(key, path, prewarm, max, fps, loop, "", 0, frameNames);
            }

            public static void RegisterFromPath(string key, string path, int prewarm, int max, float fps, bool loop, string sortingLayer, int sortingOrder, params string[] frameNames)
            {
                if (Pools.ContainsKey(key)) return;

                var frames = new List<Sprite>();
                foreach (string name in frameNames)
                {
                    Sprite? sp = Tools.LoadModSpriteForPath(path, name);
                    if (sp != null) frames.Add(sp);
                }
                if (frames.Count == 0)
                {
                    StarSephiriaMod.LogError($"[ModFxPool] {key} 没有可用帧");
                    return;
                }

                var entry = new PoolEntry
                {
                    Sprites = frames,
                    Fps = fps,
                    Loop = loop,
                    Max = Mathf.Max(1, max),
                    SortingLayer = sortingLayer,
                    SortingOrder = sortingOrder
                };
                Pools[key] = entry;

                for (int i = 0; i < Mathf.Min(prewarm, entry.Max); i++)
                {
                    SpriteFx_Simple fx = Create(entry);
                    Return(entry, fx);
                }
            }

            public static SpriteFx_Simple? Spawn(string key, Vector3 pos)
            {
                if (!Pools.TryGetValue(key, out PoolEntry entry)) return null;

                SpriteFx_Simple fx;
                if (entry.Free.Count > 0)
                {
                    fx = entry.Free.Dequeue();
                    entry.FreeSet.Remove(fx);
                }
                else if (entry.All.Count < entry.Max)
                {
                    fx = Create(entry);
                }
                else
                {
                    SpriteFx_Simple victim = entry.All[entry.Cursor++ % entry.All.Count];
                    victim.Despawn();
                    if (entry.Free.Count == 0) return null;
                    fx = entry.Free.Dequeue();
                    entry.FreeSet.Remove(fx);
                }

                fx.transform.position = pos;
                fx.gameObject.SetActive(true);
                fx.simpleAnimator2D.animationTimer.SetTimer(0f);
                fx.OnSpawn();
                if (fx.TryGetComponent(out ModFxVisibility Vis) && Vis != null)
                {
                    Vis.BaseColor = Color.white;
                    Vis.SetVisibility(-1);
                }
                return fx;
            }

            /// <summary>ownerIndex：0 = 队友 FX，1 = 自己的 FX。透明度按多人可见度设置与 SetColor 的 alpha 相乘。</summary>
            public static SpriteFx_Simple? Spawn(string key, Vector3 pos, int ownerIndex)
            {
                SpriteFx_Simple? fx = Spawn(key, pos);
                if (fx != null && fx.TryGetComponent(out ModFxVisibility Vis) && Vis != null)
                    Vis.SetVisibility(ownerIndex);
                return fx;
            }

            public static void Despawn(SpriteFx_Simple fx)
            {
                if (fx != null && fx.gameObject.activeSelf) fx.Despawn();
            }

            private static SpriteFx_Simple Create(PoolEntry entry)
            {
                GameObject go = new("ModFx");
                go.SetActive(false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                if (!string.IsNullOrEmpty(entry.SortingLayer))
                {
                    sr.sortingLayerName = entry.SortingLayer;
                    sr.sortingOrder = entry.SortingOrder;
                }
                var anim = go.AddComponent<SimpleAnimator2D_FX>();
                anim.renderers = [sr];
                anim.sprites = entry.Sprites.ToArray();
                anim.animationType = entry.Loop
                    ? SimpleAnimator2D_FX.EAnimationType.Loop
                    : SimpleAnimator2D_FX.EAnimationType.Once;
                anim.animationTimer = new Timer(1f / entry.Fps);
                anim.defaultMaterial = sr.sharedMaterial;

                var fx = go.AddComponent<SpriteFx_Simple>();
                fx.simpleAnimator2D = anim;
                fx.onDespawned += () => Return(entry, fx);
                go.AddComponent<ModFxVisibility>();
                entry.All.Add(fx);
                Object.DontDestroyOnLoad(go);
                return fx;
            }

            private static void Return(PoolEntry entry, SpriteFx_Simple fx)
            {
                if (entry.FreeSet.Add(fx))
                    entry.Free.Enqueue(fx);
            }
        }
    }
}