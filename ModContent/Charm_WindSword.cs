using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FMODUnity;
using HarmonyLib;
using Mirror;
using SPMod.Tool;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SPMod.ModContent
{
    /// <summary>
    /// 风之刃神器：维护风之刃的待机 / 攻击 / 形态（普通、狂澜、湍流）、符文强化与客户端表现。
    /// 常用查询与触发接口保持 public，方便其他模组读取或联动风之刃效果。
    /// </summary>
    public class Charm_WindSword : Charm_StatusInstance, IAttackableCharm, IModEventReceiver
    {
        public float BaseMult = 200f;                   
        public int AttackCount = 2;
        public Timer CooldownTimer = new(3f);           // 每把剑的攻击间隔（秒，受攻速影响）

        [Header("风之刃实体")]
        public float SwordDistance = 0.6f;              // 第一把剑离玩家的距离
        public float SwordSpacing = 0.4f;               // 剑与剑之间的间隔
        public float SwordFloatY = 0.35f;               // 整排剑的高度
        public float MidHeight = 1.2f;                    // 待机时剑柄瞄准点（剑与角色连线中点）再往上抬的高度
        public float SwordScale = 1f;                   // 剑的贴图缩放
        public float FadeInTime = 0.225f;               // 凝聚时剑淡入的时长
        public float FxAlpha = 1f;                      // 剑身与特效的额外透明度（和 ModFX 的可见度设置叠乘）
        public string SwordSprite = "Item_WindSword_Entity"; // 剑的贴图名

        [Header("狂澜（镇风）")]
        public float RagingTideDamageMult = 1.2f;        // 伤害 = 120% 风之刃伤害
        public float RagingTideCountBonus = 0.5f;        // 每 1 点风之刃额外数量 +50% 伤害
        public float RagingTideChargePerHit = 20f;       // 普通攻击命中积蓄 20% 能量
        public float RagingTideMaxEnergy = 200f;         // 能量上限
        public float RagingTideAttackCost = 100f;        // 发动一次狂澜消耗的能量
        public string RagingTideSwordSprite = "Item_WindSword_Entity2"; // 狂澜状态的剑贴图
        public string RagingTideIconSprite = "Item_WindSword_Icon2";    // 狂澜状态的物品图标

        [Header("湍流（匕首）")]
        public float TurbulenceDamageMult = 0.8f;       // 冲刺攻击伤害 = 80% 风之刃伤害
        public float TurbulenceHeadHeight = 0.7f;       // 翅膀弧心 / 剑柄瞄准点高度（闲置与瞄准一起下移）
        public float TurbulenceArcRadius = 1.2f;        // 剑柄到头部中心的距离
        public float TurbulenceArcSpread = 67.5f;       // 翅膀从正后方往下展开的总角度（只取下半弧，90° 的 75%）
        public float TurbulenceBackOffset = 0.25f;       // 整片翅膀沿背后方向后移，避免盖住玩家
        public float TurbulenceArcTilt = 0f;            // 整片翅膀的额外旋转
        public int TurbulenceMaxQueue = 2;              // 每把剑最多缓存几次狂怒追击
        public float TurbulenceAttackInterval = 0.1f;   // 狂怒追击的出手间隔（秒）
        public string TurbulenceSwordSprite = "Item_WindSword_Entity3"; // 湍流状态的剑贴图
        public string TurbulenceIconSprite = "Item_WindSword_Icon3";    // 湍流状态的物品图标

        [Header("引灵（引灵弩）")]
        public float SpiritDrawDistance = 0.55f;        // 第一支箭离玩家的距离
        public float SpiritDrawSpacing = 0.3f;          // 箭与箭之间的间隔（比普通形态紧凑）
        public float SpiritDrawFloatY = 0.35f;          // 整排箭的高度
        public float SpiritDrawScale = 0.6f;            // 箭体缩放（比普通剑身小40%）
        public float SpiritDrawCooldown = 3f;           // 单支风灵箭的冷却（秒，受攻速影响）
        public string SpiritDrawSwordSprite = "Item_WindSword_Entity4"; // 引灵状态的箭身贴图
        public string SpiritDrawIconSprite = "Item_WindSword_Icon4";    // 引灵状态的物品图标

        [Header("外观扩展（供其他模组）")]
        /// <summary>可选：覆盖所有形态的剑身贴图；为 null 时使用形态默认贴图。</summary>
        public Sprite? SwordSpriteOverride;
        /// <summary>可选：覆盖物品图标；为 null 时使用形态默认图标。</summary>
        public Sprite? SwordIconOverride;
        /// <summary>待机队形的整体位置偏移（世界单位），普通 / 湍流队形都生效。</summary>
        public Vector2 IdlePositionOffset;
        /// <summary>自定义待机位置：参数 (剑序号, 剑数, 玩家位置, 背后方向)，返回世界坐标；为 null 时使用内置队形。</summary>
        public Func<int, int, Vector2, Vector2, Vector2>? IdlePositionProvider;
        /// <summary>自定义待机剑柄朝向点：参数 (剑序号, 剑位置, 玩家位置)，返回世界坐标；为 null 时按形态取头部或中点。</summary>
        public Func<int, Vector2, Vector2, Vector2>? IdleLookAtProvider;

        [Header("攻击")]
        public float AttackSearchRange = 10f;           // 索敌半径：范围内有敌人且剑满才开打
        public float DamageRadiusFactor = 0.6f;         // 伤害半径 = 特效宽度 × 这个系数
        public float FirstAttackStagger = 0.3f;         // 脱战后首次攻击的错开间隔：第 n 把剑等 0.15×n 秒
        public float AttackHitDelay = 0f;               // 命中额外延迟（微调伤害和特效的对齐）
        public float FadeOutTime = 0.18f;               // 出手前在槽位隐身（淡出）的时长
        public float AppearDelay = 0.06f;               // 淡出后到出现在敌人身边的停顿
        public float AppearFadeTime = 0.09f;            // 出现在敌人身边时淡入的时长
        public float DissipateTime = 0.3f;              // 消散渐出时长（从动作过半开始，最多播到动作结束）
        public float DissipateStartRatio = 0.5f;        // 攻击动作进行到多少比例时开始消散
        public float ReturnDelay = 0.1f;                // 消散后回到玩家身后的隐身停留时间
        public float HitFxTime = 0.3f;                  // 命中特效播放时长
        public int SoundSourceWeaponId = 400;           // 音效来源武器（默认 400 普通武士刀）
        public float SlashArc = 180f;                   // 挥砍扫过的总角度
        public float SlashFps = 12f;                    // 挥砍特效帧率（越大播得越快）
        public float SlashPivotDistance = 0.9f;         // 挥砍圆心离敌人的基础距离，也是剑刃绕圆心的固定半径
        public float SizePivotFactor = 1f;              // 敌人半身尺寸乘以这个值，只加进圆心距离，不影响剑刃半径
        public float SlashFxScale = 1f;                 // 挥砍特效缩放
        public Vector2 SlashFxOffset = Vector2.zero;    // 挥砍特效位置微调
        public float SlashFxAngleOffsetLeft = -120f;    // 左砍特效的旋转偏移
        public float SlashFxAngleOffsetRight = -60f;    // 右砍特效的旋转偏移
        public float SlashSwingTime = 0.08f;            // 挥砍从 -90° 扫到 +90° 的时长（越小越快）
        public float SlashWindupTime = 0.12f;           // 挥砍前摇时长
        public float SlashWindupAngle = 25f;            // 前摇时朝反方向多拉的角度
        public float ThrustChargeTime = 0.12f;          // 刺出前蓄力时长
        public float ThrustDuration = 0.08f;            // 刺出冲刺时长（越小飞得越快）
        public float ThrustFxTime = 0.35f;              // 刺出特效播放时长（独立于冲刺速度）
        public float ThrustFxScale = 1f;                // 刺出特效缩放
        public float ThrustDistance = 1.25f;            // 刺出起点/终点离敌人的距离
        public string DamageId = "[Star's Sephiria Mod]WindSword"; // 伤害标识（统计/日志用）

        private const byte EventRuneValues = 1;
        private const byte EventSwordsReset = 2;
        private const byte EventSwordAttack = 4;
        private const byte EventPhantomAttack = 8;
        private const byte EventSpiritArrowUsed = 16;

        private const int SwordSlot = 0;
        private const int SwordFadeOut = 1;
        private const int SwordAppear = 2;
        private const int SwordStrike = 3;

        private const int FurySpecialIndex = 0;    // 匕首狂怒 = 特殊攻击 0
        private const int RagingTideSlotIndex = 0; // 狂澜固定使用 0 号剑
        private class SwordVisual
        {
            public SpriteRenderer Sword = null!;
            public SpriteRenderer? Fx;
            public Sprite[] FxFrames = [];
            public float Fade;
            public float Timer;
            public int State;
            public int Type;
            public uint TargetNetId;
            public UnitAvatar? LookTarget;
            public Vector2 TargetPos;
            public Vector2 SpawnDir;
            public Vector2 Pivot;
            public float Radius;
            public float PivotDistance;
            public float RangeMult = 1f;
            public float SpeedMult = 1f;
            public float DirSign;
            public float StartAngle;
            public float EndAngle;
            public Vector2 StartPos;
            public Vector2 EndPos;
            public bool Placed;
            public bool Phantom;
            public float HiddenTimer;
            public SpriteRenderer? HitFx;
            public Sprite[] HitFxFrames = [];
            public float HitFxTimer;
            public float FxAngle;
            public bool SwingSoundPlayed;
            public bool HitFxPlayed;
            public float StrikeCharge;
            public float StrikeDuration;
            public float StrikeFxDuration;
            public float StrikeTotal;
        }

        private readonly Dictionary<string, int> RuneBonuses = [];
        private readonly Dictionary<string, int> DisplayBonuses = [];
        private readonly List<string> RuneOrder = [];
        private readonly List<StatusInstance> RuneStatuses = [];
        private readonly List<SwordVisual> Swords = [];
        private readonly List<SwordVisual> Phantoms = [];
        private readonly List<float> SwordTimers = [];
        private readonly List<bool> SwordBusy = [];
        private readonly List<int> TurbulenceQueue = [];
        private readonly List<float> SpiritDrawCooldowns = [];
        private float TurbulenceNextAttackAt;
        private bool InCombat;
        private float RagingTideEnergy;
        private bool ControllerEventsSubscribed;
        private SwordForm LastIconForm = SwordForm.Normal;

        private static readonly FieldInfo? FinalComboCrownField = AccessTools.Field(typeof(Charm_FinalComboCritical), "crownObject");
        private static readonly FieldInfo? LastAttackCrownField = AccessTools.Field(typeof(Charm_IncreaseLastAttackDamage_SwordAndShield), "crownObject");

        /// <summary>构建物品提示词：伤害、倍率、剑数、攻击间隔与符文加成。</summary>
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            Color PositiveColor = GetPositiveColor(virtualLevelOffset);
            string PositiveHex = ColorUtility.ToHtmlStringRGBA(PositiveColor);
            List<string> EffectLines = [];
            bool Stack = false;
            foreach (string Keyword in RuneOrder)
            {
                if (!DisplayBonuses.TryGetValue(Keyword, out int Value) || Value == 0) continue;
                if (!Stack)
                {
                    Stack = true;
                    EffectLines.Add("");
                }

                EffectLines.Add($"<tag={Keyword}> <color=#{PositiveHex}>+{Value}%</color>");
            }
            // 单把剑的攻击间隔 = CooldownTimer / 攻速倍率
            float TextTime = CooldownTimer.time / GetAttackSpeedMult(avatar);
            float Mult = BaseMult + avatar.GetCustomStatUnsafe(TextFile.WindSwordMult);
            string text = "0";
            if (IsEffectEnabled)
                text = CalculateDamage(this).ToString("#0");
            return
            [
                new("Damage", text,  PositiveColor),
                new("Mult", Mult.ToString("#0") + "%",  PositiveColor),
                new("Count", GetSwordCount(avatar).ToString(),  PositiveColor),
                new("Time", TextTime.ToString("#0.##"),  EffectConstant.CooldownColor),
                new("Effect", string.Join("\n", EffectLines),  Color.white),
            ];
        }

        /// <summary>风之刃形态：普通 / 镇风·狂澜 / 匕首·湍流 / 引灵·风灵箭。</summary>
        public enum SwordForm
        {
            Normal,
            RagingTide,
            Turbulence,
            SpiritDraw,
        }

        /// <summary>一次攻击的形态档案：攻击类型、范围、速度、伤害倍率与判定。</summary>
        private readonly struct AttackProfile(int type, float rangeMult, float speedMult, float damageMult, EDamageFromType fromType, bool ragingTideDamage = false)
        {
            public readonly int Type = type;
            public readonly float RangeMult = rangeMult;
            public readonly float SpeedMult = speedMult;
            public readonly float DamageMult = damageMult;
            public readonly EDamageFromType FromType = fromType;
            public readonly bool RagingTideDamage = ragingTideDamage;
        }

        /// <summary>当前持有者的风之刃形态。供外部模组查询。</summary>
        public SwordForm Form => GetForm(NetworkAvatar);

        /// <summary>查询指定角色的风之刃形态（普通 / 狂澜 / 湍流 / 风灵箭）。</summary>
        public static SwordForm GetForm(UnitAvatar? avatar)
        {
            if (avatar == null) return SwordForm.Normal;
            if (avatar.GetCustomStatUnsafe(TextFile.WindSwordGreatSword) > 0) return SwordForm.RagingTide;
            if (avatar.GetCustomStatUnsafe(TextFile.WindSwordTurbulence) > 0) return SwordForm.Turbulence;
            if (avatar.GetCustomStatUnsafe(TextFile.WindSwordSpiritDraw) > 0) return SwordForm.SpiritDraw;
            return SwordForm.Normal;
        }

        /// <summary>当前是否为镇风·狂澜形态：只保留一把剑，不再被动攻击，普攻充能后主动挥出一击。</summary>
        public bool IsRagingTide => Form == SwordForm.RagingTide;

        /// <summary>当前是否为湍流形态：风之刃展开成翅膀，只进行冲刺攻击，狂怒时立刻追击。</summary>
        public bool IsTurbulence => Form == SwordForm.Turbulence;

        /// <summary>当前是否为引灵·风灵箭形态：箭排停在身后，不进行被动攻击，特殊攻击时消耗一支。</summary>
        public bool IsSpiritDraw => Form == SwordForm.SpiritDraw;

        /// <summary>当前参与攻击的剑数（天丛云剑神击时后半数剑不参与）。</summary>
        public int ActiveSwordCount => GetActiveSwordCount(GetSwordCount(NetworkAvatar));

        /// <summary>当前狂澜能量（0 ~ RagingTideMaxEnergy）。</summary>
        public float CurrentRagingTideEnergy => RagingTideEnergy;

        /// <summary>按形态生成一次攻击档案（类型 / 范围 / 速度 / 伤害 / 判定）。</summary>
        private AttackProfile GetAttackProfile(int Index, float DamageMult)
        {
            SwordForm Current = Form;
            // 狂澜只作用于槽位剑；万象临时剑保持普通行为
            if (Current == SwordForm.RagingTide && Index >= 0)
                return new AttackProfile(1, GetWeaponRangeMult(NetworkAvatar), 1f, DamageMult, EDamageFromType.DirectAttack, true);

            float SpeedMult = Mathf.Max(0.01f, GetAttackSpeedMult(NetworkAvatar));
            float RangeMult = GetRangeMult(NetworkAvatar);
            if (Current == SwordForm.Turbulence)
            {
                float Mult = DamageMult * TurbulenceDamageMult * NetworkAvatar.GetSpecialAttackMultiplier(true, false, false);
                return new AttackProfile(2, RangeMult, SpeedMult, Mult, EDamageFromType.DirectAttack);
            }
            return new AttackProfile(Random.Range(0, 3), RangeMult, SpeedMult, DamageMult, EDamageFromType.None);
        }

        /// <summary>剑的数量 = 基础 AttackCount + 风之刃额外攻击次数；狂澜固定 1 把。</summary>
        public int GetSwordCount(UnitAvatar? avatar)
        {
            if (GetForm(avatar) == SwordForm.RagingTide) return 1;
            int Bonus = avatar != null ? avatar.GetCustomStatUnsafe(TextFile.WindSwordCount) : 0;
            return Mathf.Max(1, AttackCount + Bonus);
        }

        /// <summary>形态对应的剑身贴图名；普通形态用默认贴图。</summary>
        private string GetFormSwordSprite() => Form switch
        {
            SwordForm.RagingTide => RagingTideSwordSprite,
            SwordForm.Turbulence => TurbulenceSwordSprite,
            SwordForm.SpiritDraw => SpiritDrawSwordSprite,
            _ => SwordSprite,
        };

        /// <summary>形态对应的物品图标名；普通形态返回空（用本体图标）。</summary>
        private string GetFormIconSprite() => Form switch
        {
            SwordForm.RagingTide => RagingTideIconSprite,
            SwordForm.Turbulence => TurbulenceIconSprite,
            SwordForm.SpiritDraw => SpiritDrawIconSprite,
            _ => "",
        };

        /// <summary>狂澜 / 湍流形态下物品图标换图标；剑身贴图由 EnsureSwords 按同一状态切换。</summary>
        public override Sprite GetCustomIcon(ItemPosition pos, bool isIsntance)
        {
            if (!isIsntance) return base.GetCustomIcon(pos, isIsntance);
            if (SwordIconOverride != null) return SwordIconOverride;
            string IconSprite = GetFormIconSprite();
            if (string.IsNullOrEmpty(IconSprite)) return base.GetCustomIcon(pos, isIsntance);
            return Tools.LoadModSprite(IconSprite) ?? base.GetCustomIcon(pos, isIsntance);
        }

        /// <summary>攻击速度倍率：WindSwordSpeed 每 1 点加快 1%，负值同样生效（倍率最低 10%）。</summary>
        public float GetAttackSpeedMult(UnitAvatar? avatar)
        {
            float Speed = avatar != null ? avatar.GetCustomStatUnsafe(TextFile.WindSwordSpeed) : 0f;
            return Mathf.Max(0.1f, 1f + Speed * 0.01f);
        }

        /// <summary>攻击范围倍率：WindSwordRange 每 1 点放大 1%，同时作用于特效大小和判定范围。</summary>
        public float GetRangeMult(UnitAvatar? avatar)
        {
            float Range = avatar != null ? avatar.GetCustomStatUnsafe(TextFile.WindSwordRange) : 0f;
            return 1f + Mathf.Max(0f, Range) * 0.01f;
        }

        /// <summary>狂澜范围：武器范围 + 风之刃范围。</summary>
        public float GetWeaponRangeMult(UnitAvatar? avatar)
        {
            float Range = 0f;
            if (avatar != null)
                Range = Mathf.Max(0f, avatar.GetCustomStatUnsafe(TextFile.WeaponRange))
                      + Mathf.Max(0f, avatar.GetCustomStatUnsafe(TextFile.WindSwordRange));
            return 1f + Range * 0.01f;
        }

        /// <summary>挥砍圆心距离：基础距离 + 敌人半身尺寸 × SizePivotFactor。</summary>
        public float GetSlashPivotDistance(UnitAvatar? Target)
        {
            if (Target == null || Target.TopdownActor == null) return SlashPivotDistance;
            Vector2 Size = Target.TopdownActor.size;
            float BodyRadius = Mathf.Max(Size.x, Size.y) * 0.5f;
            return SlashPivotDistance + BodyRadius * SizePivotFactor;
        }

        /// <summary>一次攻击从出现到挥砍/冲刺结束的时长（不含渐出、出现和消散）。</summary>
        private float GetStrikeVisualTime(int Type)
        {
            return Type == 2 ? ThrustChargeTime + Mathf.Max(ThrustDuration, ThrustFxTime) : SlashWindupTime + Mathf.Max(SlashSwingTime, Mathf.Max(0.05f, 6f / Mathf.Max(1f, SlashFps)));
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            CooldownTimer.Ratio = 1f;
            ApplyRuneStatuses();
            InCombat = false;
            RagingTideEnergy = 0f;
            SubscribeControllerEvents();
            SwordTimers.Clear();
            SwordBusy.Clear();
            TurbulenceQueue.Clear();
            SpiritDrawCooldowns.Clear();
            TurbulenceNextAttackAt = 0f;
            SendSwordsReset();
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            ClearRuneStatuses();
            InCombat = false;
            RagingTideEnergy = 0f;
            UnsubscribeControllerEvents();
            SwordTimers.Clear();
            SwordBusy.Clear();
            TurbulenceQueue.Clear();
            SpiritDrawCooldowns.Clear();
            TurbulenceNextAttackAt = 0f;
            SendSwordsReset();
        }

        /// <summary>神器效果刷新时重新应用符文状态（适用于等级/词条变化）。</summary>
        public override void OnCharmEffectRefreshed()
        {
            base.OnCharmEffectRefreshed();
            if (isServer && IsEffectEnabled) ApplyRuneStatuses();
        }

        /// <summary>为风之刃添加一枚符文（服务器执行），立即应用对应属性并写入存档。</summary>
        public void AddRune(string Keyword, int Value)
        {
            if (!isServer || string.IsNullOrEmpty(Keyword) || Value == 0) return;
            RuneBonuses.TryGetValue(Keyword, out int Current);
            RuneBonuses[Keyword] = Current + Value;
            if (!RuneOrder.Contains(Keyword)) RuneOrder.Add(Keyword);
            ApplyRuneStatuses();
            SaveItemOnServer(SaveManager.CurrentRun);
        }

        /// <summary>存档：把符文列表写入当前存档。</summary>
        public override void SaveItemOnServer(ISaveData saveData)
        {
            base.SaveItemOnServer(saveData);
            string Prefix = GetRuneSavePrefix();
            saveData.SetInt($"{Prefix}_Count", RuneOrder.Count);
            for (int i = 0; i < RuneOrder.Count; i++)
            {
                saveData.SetString($"{Prefix}_{i}_Keyword", RuneOrder[i]);
                saveData.SetInt($"{Prefix}_{i}_Value", RuneBonuses[RuneOrder[i]]);
            }
        }

        /// <summary>读档：从当前存档恢复符文列表。</summary>
        public override void LoadItemOnServer(ISaveData saveData)
        {
            base.LoadItemOnServer(saveData);
            string Prefix = GetRuneSavePrefix();
            RuneBonuses.Clear();
            RuneOrder.Clear();
            int Count = saveData.GetInt($"{Prefix}_Count", 0);
            for (int i = 0; i < Count; i++)
            {
                string Keyword = saveData.GetString($"{Prefix}_{i}_Keyword", "");
                if (string.IsNullOrEmpty(Keyword)) continue;
                RuneBonuses[Keyword] = saveData.GetInt($"{Prefix}_{i}_Value", 0);
                if (!RuneOrder.Contains(Keyword)) RuneOrder.Add(Keyword);
            }
        }

        /// <summary>清除当前风之剑的符文（服务器执行），并清掉 run 存档中的符文数据。</summary>
        public void ClearRunes()
        {
            if (!isServer || NetworkAvatar == null) return;
            RuneOrder.Clear();
            RuneBonuses.Clear();
            DisplayBonuses.Clear();
            ClearRuneStatuses();
            BroadcastValues();
            itemUpdateDirtyBit = true;
            ClearSavedRunes(NetworkAvatar, SaveManager.CurrentRun);
        }

        /// <summary>清除指定玩家在 run 存档中的风之剑符文数据。</summary>
        public static void ClearSavedRunes(UnitAvatar? Avatar, ISaveData? Save)
        {
            if (Save == null) return;
            string Prefix = GetRuneSavePrefix(Avatar);
            int Count = Save.GetInt($"{Prefix}_Count", 0);
            if (Save is SaveData Data)
            {
                for (int i = 0; i < Count; i++)
                {
                    Data.RemoveKey($"{Prefix}_{i}_Keyword");
                    Data.RemoveKey($"{Prefix}_{i}_Value");
                }
            }
            Save.SetInt($"{Prefix}_Count", 0);
        }

        private string GetRuneSavePrefix() => GetRuneSavePrefix(NetworkAvatar);

        private static string GetRuneSavePrefix(UnitAvatar? Avatar)
        {
            int PlayerIdx = Avatar is PlayerAvatar Player && Player.spawner != null ? Player.spawner.currentPlayerIdxForSave : 0;
            return $"CharmSaveData_WindSword_{PlayerIdx}";
        }

        private void ApplyRuneStatuses()
        {
            if (!isServer || NetworkAvatar == null) return;
            ClearRuneStatuses();
            DisplayBonuses.Clear();
            int Bonus = RequestCharmDamageBonusOnRoot();
            foreach (string Keyword in RuneOrder)
            {
                RuneBonuses.TryGetValue(Keyword, out int BaseValue);
                int Value = BaseValue == 0 ? 0 : Mathf.RoundToInt(BaseValue * (100f + Bonus) / 100f);
                DisplayBonuses[Keyword] = Value;
                if (!Tools.GetKeyWordToStatus(Keyword.ToUpper(), out string StatusID)) continue;
                ApplyRuneStatus(StatusID, Value);
            }
            BroadcastValues();
            itemUpdateDirtyBit = true;
        }

        private void ApplyRuneStatus(string StatusID, int Value)
        {
            if (Value == 0) return;
            StatusInstance Instance = StatusDatabase.CreateStatusEntity($"{StatusID}/{Value}");
            if (Instance == null) return;
            Instance.SetTarget(NetworkAvatar);
            Instance.ApplyStatus(fromRuntime: true);
            RuneStatuses.Add(Instance);
        }

        private void ClearRuneStatuses()
        {
            foreach (StatusInstance Instance in RuneStatuses)
            {
                Instance.RemoveStatus();
                Instance.ClearTarget();
            }
            RuneStatuses.Clear();
        }

        private void BroadcastValues()
        {
            if (!isServer) return;
            List<string> Entries = [];
            foreach (string Keyword in RuneOrder)
            {
                if (!DisplayBonuses.TryGetValue(Keyword, out int Value)) continue;
                Entries.Add($"{Keyword}={Value}");
            }
            ModNetwork.SendEvent(netId, EventRuneValues, NetworkAvatar ? NetworkAvatar.transform.position : Vector3.zero, string.Join(",", Entries));
        }

        /// <summary>模组事件入口：符文数值同步、剑重置、槽位/幻影剑攻击演出。</summary>
        public void OnClientEvent(byte Event, Vector3 Pos, string S1, string S2)
        {
            if (Event == EventRuneValues)
            {
                DisplayBonuses.Clear();
                // RuneOrder 只在服务端 AddRune/LoadItemOnServer 里维护，客户端要按广播顺序重建，
                // 否则 BuildKeywords 遍历 RuneOrder 时为空，符文加成不会显示。
                RuneOrder.Clear();
                if (!string.IsNullOrEmpty(S1))
                {
                    foreach (string Entry in S1.Split(','))
                    {
                        string[] Parts = Entry.Split('=');
                        if (Parts.Length != 2) continue;
                        if (int.TryParse(Parts[1], out int Value))
                        {
                            DisplayBonuses[Parts[0]] = Value;
                            if (!RuneOrder.Contains(Parts[0])) RuneOrder.Add(Parts[0]);
                        }
                    }
                }
                itemUpdateDirtyBit = true;
            }
            else if (Event == EventSwordsReset)
            {
                // 重新开始：所有剑回到槽位隐身，等渐入
                foreach (SwordVisual Visual in Swords)
                {
                    Visual.State = SwordSlot;
                    Visual.Timer = 0f;
                    Visual.Fade = 0f;
                }
                for (int i = 0; i < SpiritDrawCooldowns.Count; i++) SpiritDrawCooldowns[i] = 0f;
                ClearPhantoms();
            }
            else if (Event == EventSwordAttack)
            {
                string[] Head = S1.Split('|');
                if (Head.Length < 2) return;
                if (!int.TryParse(Head[0], out int Index) || !int.TryParse(Head[1], out int Type)) return;
                if (Index < 0 || Index >= Swords.Count) return;

                SwordVisual S = Swords[Index];
                S.Type = Mathf.Clamp(Type, 0, 2);
                S.TargetPos = Pos;
                S.PivotDistance = SlashPivotDistance;
                if (Head.Length >= 3 && float.TryParse(Head[2], out float PivotDist))
                    S.PivotDistance = PivotDist;
                S.RangeMult = 1f;
                if (Head.Length >= 4 && float.TryParse(Head[3], out float RangeMult))
                    S.RangeMult = Mathf.Max(0.1f, RangeMult);
                S.SpeedMult = 1f;
                if (Head.Length >= 5 && float.TryParse(Head[4], out float SpeedMult))
                    S.SpeedMult = Mathf.Max(0.01f, SpeedMult);
                S.TargetNetId = 0;
                if (Head.Length >= 6 && uint.TryParse(Head[5], out uint TargetNetId))
                    S.TargetNetId = TargetNetId;
                S.LookTarget = null;
                S.SpawnDir = ParseSpawnDir(S2);
                S.State = SwordFadeOut;
                S.Timer = 0f;
                // 狂澜攻击固定贴图朝向，不跟随玩家位置翻转；回到槽位后仍按原逻辑跟随
                if (IsRagingTide && S.Sword != null) S.Sword.flipX = false;
            }
            else if (Event == EventPhantomAttack)
            {
                string[] Head = S1.Split('|');
                if (Head.Length < 4) return;
                if (!int.TryParse(Head[0], out int Type)) return;
                if (!float.TryParse(Head[1], out float PivotDist)) return;
                if (!float.TryParse(Head[2], out float RangeMult)) return;
                if (!float.TryParse(Head[3], out float SpeedMult)) return;

                SwordVisual S = CreateSwordVisual();
                S.Phantom = true;
                S.Type = Mathf.Clamp(Type, 0, 2);
                S.TargetPos = Pos;
                S.TargetNetId = 0;
                if (Head.Length >= 5 && uint.TryParse(Head[4], out uint TargetNetId))
                    S.TargetNetId = TargetNetId;
                S.LookTarget = null;
                S.PivotDistance = Mathf.Max(0f, PivotDist);
                S.RangeMult = Mathf.Max(0.1f, RangeMult);
                S.SpeedMult = Mathf.Max(0.01f, SpeedMult);
                S.SpawnDir = ParseSpawnDir(S2);
                S.State = SwordFadeOut;
                S.Timer = 0f;
                S.Fade = 0f;
                Phantoms.Add(S);
            }
            else if (Event == EventSpiritArrowUsed)
            {
                string[] Parts = S1.Split('|');
                if (Parts.Length < 2) return;
                if (!int.TryParse(Parts[0], out int Index)) return;
                if (!float.TryParse(Parts[1], out float Cooldown)) return;
                while (SpiritDrawCooldowns.Count <= Index) SpiritDrawCooldowns.Add(0f);
                if (Index >= 0) SpiritDrawCooldowns[Index] = Mathf.Max(0f, Cooldown);
            }
        }

        private static Vector2 ParseSpawnDir(string S2)
        {
            string[] D = S2.Split(',');
            if (D.Length == 2 && float.TryParse(D[0], out float DX) && float.TryParse(D[1], out float DY))
                return new Vector2(DX, DY);
            return Vector2.right;
        }

        /// <summary>单把风之刃的基础伤害（未乘攻击倍率）。</summary>
        public float GetDamage(UnitAvatar avatar)
        {
            float Value = avatar.GetCustomStatUnsafe(TextFile.WindDamage);
            float Base = (BaseMult + avatar.GetCustomStatUnsafe(TextFile.WindSwordMult)) * 0.01f;
            float Mult = 1f + avatar.GetCustomStatUnsafe(TextFile.WindSwordDamage) * 0.01f;
            return Value * Base * Mult;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (!IsEffectEnabled || NetworkAvatar == null) return;
            // 玩家死亡时不进行自动攻击
            if (NetworkAvatar.IsDead) return;

            int Count = GetSwordCount(NetworkAvatar);
            SyncSwordLists(Count);
            int ActiveCount = GetActiveSwordCount(Count);

            if (isServer && !ControllerEventsSubscribed) SubscribeControllerEvents();

            // 狂澜：不再进行被动攻击，能量由普通攻击命中积累
            if (Form == SwordForm.RagingTide)
            {
                InCombat = false;
                return;
            }

            // 引灵：不再进行被动攻击，只推进每支箭的冷却
            if (Form == SwordForm.SpiritDraw)
            {
                InCombat = false;
                TickSwordTimers(ActiveCount);
                return;
            }

            if (!TickSwordTimers(ActiveCount)) return;

            List<UnitAvatar> Targets = FindTargets();
            if (Targets.Count == 0)
            {
                // 脱战：下次进入战斗重新走首轮前摇
                InCombat = false;
                return;
            }

            if (!InCombat)
            {
                InCombat = true;
                ApplyFirstAttackStagger(ActiveCount);
            }

            DispatchAttacks(ActiveCount, Targets);
        }

        /// <summary>天丛云剑神击：后半数剑不参与自动攻击。</summary>
        private int GetActiveSwordCount(int Count)
        {
            if (WeaponPatch.KusanagiState.IsStuck(NetworkAvatar)) return Count - Count / 2;
            return Count;
        }

        /// <summary>推进冷却并返回是否有剑可以出手；湍流缓存里的追击无视冷却。</summary>
        private bool TickSwordTimers(int ActiveCount)
        {
            bool AnyReady = false;
            for (int i = 0; i < ActiveCount; i++)
            {
                SwordTimers[i] -= Time.deltaTime;
                if (!SwordBusy[i] && (SwordTimers[i] <= 0f || TurbulenceQueue[i] > 0)) AnyReady = true;
            }
            return AnyReady;
        }

        private List<UnitAvatar> FindTargets()
        {
            return Tools.SearchEnemiesInRange(NetworkAvatar, NetworkAvatar.transform.position, AttackSearchRange, includeBodySize: true);
        }

        /// <summary>首轮按序号错开，避免所有剑同一帧出手；缓存的追击不参与错开。</summary>
        private void ApplyFirstAttackStagger(int ActiveCount)
        {
            for (int i = 0; i < ActiveCount; i++)
            {
                if (SwordBusy[i] || TurbulenceQueue[i] > 0) continue;
                SwordTimers[i] = Mathf.Max(SwordTimers[i], FirstAttackStagger * (i + 1));
            }
        }

        /// <summary>派发本帧可以出手的剑；湍流缓存按固定间隔依次消费。</summary>
        private void DispatchAttacks(int ActiveCount, List<UnitAvatar> Targets)
        {
            for (int i = 0; i < ActiveCount; i++)
            {
                if (SwordBusy[i]) continue;
                // 湍流缓存的追击无视冷却，按固定间隔依次出手
                bool Queued = TurbulenceQueue[i] > 0;
                if (Queued)
                {
                    if (Time.time < TurbulenceNextAttackAt) continue;
                }
                else if (SwordTimers[i] > 0f) continue;

                UnitAvatar? Target = Targets[Random.Range(0, Targets.Count)];
                if (Target == null || Target.IsDead) continue;

                if (Queued)
                {
                    TurbulenceQueue[i] -= 1;
                    TurbulenceNextAttackAt = Time.time + TurbulenceAttackInterval;
                }
                SwordBusy[i] = true;
                StartCoroutine(SwordAttackRoutine(i, Target));
            }
        }

        /// <summary>单把剑的一次完整攻击：发事件 + 按帧结算伤害 + 等视觉链结束。</summary>
        private IEnumerator SwordAttackRoutine(int Index, UnitAvatar Target)
        {
            try
            {
                if (!IsEffectEnabled || NetworkAvatar == null) yield break;

                float VisualTime = LaunchAttack(Target, 1f, Index);

                // 等这轮动作播完（消散已和动作后半段重叠），再开始冷却
                yield return new WaitForSeconds(VisualTime);

                // 动作结束即完全隐身，开始冷却；狂澜由能量驱动，不进入冷却
                if (Index >= 0 && Index < SwordTimers.Count)
                    SwordTimers[Index] = IsRagingTide ? 0f : Mathf.Max(0.05f, CooldownTimer.time / GetAttackSpeedMult(NetworkAvatar));
            }
            finally
            {
                if (Index >= 0 && Index < SwordBusy.Count) SwordBusy[Index] = false;
                // 狂澜：能量超过上限的部分已经存下，攻击结束立刻把存着的能量继续打出去
                if (Index == RagingTideSlotIndex && IsRagingTide) TryStartStoredRagingTideAttack();
            }
        }

        /// <summary>发起一次风之刃攻击；Index ≥ 0 用槽位剑，否则是万象的临时剑。返回视觉链总时长。</summary>
        private float LaunchAttack(UnitAvatar Target, float DamageMult, int Index = -1)
        {
            AttackProfile Profile = GetAttackProfile(Index, DamageMult);
            Vector3 TargetPos = Target.transform.position;
            // 出现方位由服务端决定，客户端和伤害计算共用同一个
            float SpawnAngle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 SpawnDir = new(Mathf.Cos(SpawnAngle), Mathf.Sin(SpawnAngle));
            float FadeOut = FadeOutTime / Profile.SpeedMult;
            float PivotDist = GetSlashPivotDistance(Target);

            if (Index >= 0)
                ModNetwork.SendEvent(netId, EventSwordAttack, TargetPos,
                    $"{Index}|{Profile.Type}|{PivotDist:F3}|{Profile.RangeMult:F3}|{Profile.SpeedMult:F3}|{Target.netId}", $"{SpawnDir.x:F3},{SpawnDir.y:F3}");
            else
                ModNetwork.SendEvent(netId, EventPhantomAttack, TargetPos,
                    $"{Profile.Type}|{PivotDist:F3}|{Profile.RangeMult:F3}|{Profile.SpeedMult:F3}|{Target.netId}", $"{SpawnDir.x:F3},{SpawnDir.y:F3}");

            float HitDelay = FadeOut + AppearDelay + AttackHitDelay + SlashWindupTime + SlashSwingTime * 0.5f;
            // 伤害和特效同步，但不阻塞这把剑的视觉链
            if (Profile.RagingTideDamage)
            {
                StartCoroutine(RagingTideDamage(Target, TargetPos, SpawnDir, HitDelay, PivotDist, Profile.RangeMult));
            }
            else if (Profile.Type == 2)
            {
                HitDelay = FadeOut + AppearDelay + AttackHitDelay + ThrustChargeTime;
                StartCoroutine(ThrustDamage(Target, TargetPos, SpawnDir, HitDelay, Profile.RangeMult, Profile.DamageMult, Profile.FromType));
            }
            else
            {
                StartCoroutine(SlashDamage(Target, TargetPos, SpawnDir, HitDelay, PivotDist, Profile.RangeMult, Profile.DamageMult, Profile.FromType));
            }
            return FadeOut + AppearDelay + GetStrikeVisualTime(Profile.Type);
        }

        /// <summary>万象魔晶用：独立触发一次不占用槽位的风之刃攻击，不影响已有剑的攻击和冷却。</summary>
        public bool TriggerPhantomAttack(UnitAvatar? Target, float DamageMult = 1f)
        {
            if (!isServer || !IsEffectEnabled || NetworkAvatar == null || Target == null || Target.IsDead) return false;
            LaunchAttack(Target, DamageMult);
            return true;
        }

        private void SubscribeControllerEvents()
        {
            if (!isServer || ControllerEventsSubscribed) return;
            WeaponControllerSimple? Controller = WeaponController;
            if (Controller == null && NetworkAvatar != null)
                Controller = NetworkAvatar.GetComponent<WeaponControllerSimple>();
            if (Controller == null) return;
            Controller.OnBasicAttack += OnBasicAttackUnit;
            Controller.OnSpecialAttackSwing += OnSpecialAttackSwingUnit;
            ControllerEventsSubscribed = true;
        }

        private void UnsubscribeControllerEvents()
        {
            if (!ControllerEventsSubscribed) return;
            WeaponControllerSimple? Controller = WeaponController;
            if (Controller == null && NetworkAvatar != null)
                Controller = NetworkAvatar.GetComponent<WeaponControllerSimple>();
            if (Controller != null)
            {
                Controller.OnBasicAttack -= OnBasicAttackUnit;
                Controller.OnSpecialAttackSwing -= OnSpecialAttackSwingUnit;
            }
            ControllerEventsSubscribed = false;
        }

        /// <summary>普通攻击命中：狂澜积蓄能量；引灵从最前的风之刃发射「曲」。</summary>
        private void OnBasicAttackUnit(CombatBehaviour victim, DamageInstance damage, ProjectileBase projectile)
        {
            if (!isServer || !IsEffectEnabled || NetworkAvatar == null) return;
            if (IsSpiritDraw)
            {
                if (victim != null) TryFireSpiritMelody(victim);
                return;
            }
            if (!IsRagingTide) return;
            if (victim is not UnitAvatar Target || Target.IsDead) return;

            float Gain = RagingTideChargePerHit * Mathf.Max(0.1f, GetAttackSpeedMult(NetworkAvatar));
            RagingTideEnergy = Mathf.Min(RagingTideMaxEnergy, RagingTideEnergy + Gain);
            if (RagingTideEnergy < RagingTideAttackCost) return;
            if (!TryPrepareRagingTideSlot()) return;

            StartRagingTideAttack(Target);
        }

        /// <summary>引灵·曲：让最前一支未冷却的风之刃进入冷却，并从该箭的位置射出一发曲。</summary>
        private void TryFireSpiritMelody(CombatBehaviour Victim)
        {
            // 先确认模板可用，避免模板缺失时白白消耗一支箭
            GameObject? Template = LoadModContent.SpiritDrawMelodyBullet.Template;
            if (Template == null) return;
            if (!TryConsumeFrontSpiritArrow(out Vector3 FirePos, out Vector2 Back)) return;
            UnitAvatar Avatar = NetworkAvatar!;

            float Mult = 0.5f + 0.01f * (Avatar.GetCustomStatUnsafe(TextFile.AttackSpeed) / 3f);
            float Damage = CalculateDamage(this) * Mult;
            Damage *= Avatar.GetSpecialAttackMultiplier(true, false, false);
            Damage *= 1f + 0.1f * Avatar.GetCustomStatUnsafe(TextFile.ScoreCount);

            // 子弹实体挂在地面位置，用 height 抬高到剑所在的视觉高度，避免判定整体上移
            Vector3 PlayerPos = Avatar.transform.position;
            Vector3 GroundPos = new(FirePos.x, PlayerPos.y, PlayerPos.z);
            float Height = Mathf.Max(0f, FirePos.y - PlayerPos.y);
            Vector2 Aim = (Vector2)Victim.transform.position - (Vector2)GroundPos;
            if (Aim.sqrMagnitude < 0.0001f) Aim = Vector2.up;
            Aim.Normalize();

            // 和光轮一样：起始方向在玩家身后 180° 内随机，再由光箭的追踪模块转向目标
            float BackAngle = Mathf.Atan2(Back.y, Back.x) * Mathf.Rad2Deg;
            float LaunchAngle = BackAngle + Random.Range(-90f, 90f);
            float LaunchRad = LaunchAngle * Mathf.Deg2Rad;
            Vector2 LaunchDir = new(Mathf.Cos(LaunchRad), Mathf.Sin(LaunchRad));

            Bullet? Projectile = Bullet.Pool.Spawn(Template, GroundPos, true, EDamageFromType.DirectAttack,
                "[Star's Sephiria Mod]WindSpiritArrow", Damage, 0, 0f, Avatar,
                Avatar.GetHostileFactionLayers(EDamageFromType.DirectAttack), Height,
                GroundPos, GroundPos + (Vector3)(LaunchDir * 2f), null, null, 0f, EffectConstant.Wind);
            if (Projectile == null) return;

            Projectile.swingId = "[Star's Sephiria Mod]WindSpiritArrow";
            int Crit = Avatar.GetCustomStatUnsafe(TextFile.WindSwordCritDamage);
            if (Crit != 0) Projectile.additionalCriticalDamageRate += Crit;

            if (Projectile.MoveModule is BulletMoveModule_LightArrow LightArrow)
                LightArrow.SetChase(Victim as UnitAvatar, LaunchDir, WeaponAddonCrossbow_LightArrow.GetSideTurnSign(LaunchDir, Aim));
        }

        /// <summary>湍流：使用狂怒（特殊攻击 0）时，所有风之刃立刻追击一次；正忙的剑缓存到本次刺击结束后。</summary>
        private void OnSpecialAttackSwingUnit(int idx)
        {
            if (!isServer || !IsEffectEnabled || NetworkAvatar == null || !IsTurbulence) return;
            if (idx != FurySpecialIndex) return;
            TriggerTurbulenceAttack();
        }

        /// <summary>湍流：为每把风之刃缓存一次追击（服务器执行；无视冷却，按攻击间隔依次出手）。</summary>
        public void TriggerTurbulenceAttack()
        {
            if (!isServer || !IsEffectEnabled || NetworkAvatar == null || NetworkAvatar.IsDead) return;

            int Count = GetSwordCount(NetworkAvatar);
            SyncSwordLists(Count);

            List<UnitAvatar> Targets = Tools.SearchEnemiesInRange(NetworkAvatar, NetworkAvatar.transform.position, AttackSearchRange, includeBodySize: true);
            if (Targets.Count == 0) return;

            InCombat = true;
            for (int i = 0; i < Count; i++)
            {
                // 每把剑缓存一次追击；正忙的剑等本次刺击结束后再加入队列
                if (TurbulenceQueue[i] < TurbulenceMaxQueue) TurbulenceQueue[i] += 1;
            }
        }

        /// <summary>
        /// 取最前一支未冷却的风之刃（服务器执行）：令其进入冷却，并返回该箭在槽位上的世界坐标和玩家身后的方向。
        /// </summary>
        private bool TryConsumeFrontSpiritArrow(out Vector3 FirePosition, out Vector2 Back)
        {
            FirePosition = default;
            Back = Vector2.left;
            if (!isServer || !IsEffectEnabled || NetworkAvatar == null || NetworkAvatar.IsDead) return false;
            if (Form != SwordForm.SpiritDraw) return false;
            int Count = GetSwordCount(NetworkAvatar);
            if (Count <= 0) return false;
            SyncSwordLists(Count);

            int Index = -1;
            for (int i = 0; i < Count; i++)
            {
                if (SwordBusy[i] || SwordTimers[i] > 0f) continue;
                Index = i;
                break;
            }
            if (Index < 0) return false;

            // 发射位置和客户端该支箭的悬停槽位一致
            Vector2 PlayerPos = NetworkAvatar.transform.position;
            float Sign = 1f;
            if (NetworkAvatar is PlayerAvatar PlayerAvatar && PlayerAvatar.NetworkaimObject != null)
            {
                Vector2 ToAim = (Vector2)PlayerAvatar.NetworkaimObject.transform.position - PlayerPos;
                if (Mathf.Abs(ToAim.x) > 0.01f) Sign = Mathf.Sign(ToAim.x);
            }
            Back = new Vector2(-Sign, 0f);
            Vector2 Slot = GetIdleSlotPos(SwordForm.SpiritDraw, Index, Count, PlayerPos, Back);
            FirePosition = new Vector3(Slot.x, Slot.y, NetworkAvatar.transform.position.z);

            float Cooldown = Mathf.Max(0.05f, SpiritDrawCooldown / GetAttackSpeedMult(NetworkAvatar));
            SwordTimers[Index] = Cooldown;
            if (isClient && Index < SpiritDrawCooldowns.Count) SpiritDrawCooldowns[Index] = Cooldown;
            ModNetwork.SendEvent(netId, EventSpiritArrowUsed, NetworkAvatar.transform.position, $"{Index}|{Cooldown:F3}");
            return true;
        }

        /// <summary>攻击结束后如果还有 >=100 的存量能量，自动再挥一次，避免能量闲置。</summary>
        private void TryStartStoredRagingTideAttack()
        {
            if (!isServer || !IsEffectEnabled || !IsRagingTide || NetworkAvatar == null) return;
            if (RagingTideEnergy < RagingTideAttackCost) return;
            if (!TryPrepareRagingTideSlot()) return;

            List<UnitAvatar> Targets = Tools.SearchEnemiesInRange(NetworkAvatar, NetworkAvatar.transform.position, AttackSearchRange, includeBodySize: true);
            if (Targets.Count == 0) return;
            UnitAvatar? Target = Targets[Random.Range(0, Targets.Count)];
            if (Target == null || Target.IsDead) return;

            StartRagingTideAttack(Target);
        }

        /// <summary>同步冷却 / 占用 / 缓存三个列表到指定剑数。</summary>
        private void SyncSwordLists(int Count)
        {
            while (SwordTimers.Count < Count) { SwordTimers.Add(0f); SwordBusy.Add(false); }
            while (SwordTimers.Count > Count) { SwordTimers.RemoveAt(SwordTimers.Count - 1); SwordBusy.RemoveAt(SwordBusy.Count - 1); }
            while (TurbulenceQueue.Count < Count) TurbulenceQueue.Add(0);
            while (TurbulenceQueue.Count > Count) TurbulenceQueue.RemoveAt(TurbulenceQueue.Count - 1);
        }

        /// <summary>狂澜只用槽位 0；顺便裁剪列表到 1 把剑，返回剑是否空闲。</summary>
        private bool TryPrepareRagingTideSlot()
        {
            SyncSwordLists(1);
            return !SwordBusy[RagingTideSlotIndex];
        }

        private void StartRagingTideAttack(UnitAvatar Target)
        {
            RagingTideEnergy = Mathf.Max(0f, RagingTideEnergy - RagingTideAttackCost);
            SwordBusy[RagingTideSlotIndex] = true;
            StartCoroutine(SwordAttackRoutine(RagingTideSlotIndex, Target));
        }

        /// <summary>狂澜一击的最终伤害（供外部模组读取/展示用）。</summary>
        public float GetRagingTideDamage()
        {
            if (NetworkAvatar == null) return 0f;
            int Extra = Mathf.Max(0, NetworkAvatar.GetCustomStatUnsafe(TextFile.WindSwordCount));
            float Damage = CalculateDamage(this) * RagingTideDamageMult * (1f + RagingTideCountBonus * Extra);
            Damage *= NetworkAvatar.GetBasicAttackMultiplier(IncludeWeaponDamage: true);
            Damage *= 1f + NetworkAvatar.GetCustomStatUnsafe(TextFile.WeaponDamageBonus) * 0.01f;
            Damage *= 1f + NetworkAvatar.GetCustomStatUnsafe(TextFile.LastBasicAttackDamage) * 0.01f;

            Charm_IncreaseLastAttackDamage_SwordAndShield? LastDamage = NetworkAvatar.FindPlayerCharm<Charm_IncreaseLastAttackDamage_SwordAndShield>();
            if (LastDamage != null)
                Damage *= 1f + LastDamage.damagePercentByLevel.SafeRandomAccess(LastDamage.CurrentLevelToIdx()) * 0.01f;
            return Damage;
        }

        /// <summary>手动把狂澜这一击判定成普攻 + 最后一击。</summary>
        private void DealRagingTideDamage(UnitAvatar Enemy, Vector2 Dir, float Damage, float CritChance)
        {
            DealWindSwordDamage(Enemy, Dir, Damage, EDamageFromType.DirectAttack, "Weapon_BasicAttack", CritChance);
        }

        private float GetRagingTideCritChance()
        {
            Charm_FinalComboCritical? FinalCombo = NetworkAvatar.FindPlayerCharm<Charm_FinalComboCritical>();
            return FinalCombo != null ? FinalCombo.criticalBonusPercentByLevel.SafeRandomAccess(FinalCombo.CurrentLevelToIdx()) : 0f;
        }

        /// <summary>手动播放两个最后一击神器的皇冠挥击特效（本体只在 forceFinalCombo 的弹体上触发）。</summary>
        private void TriggerRagingTideCrowns()
        {
            TriggerCrown(NetworkAvatar.FindPlayerCharm<Charm_FinalComboCritical>(), FinalComboCrownField);
            TriggerCrown(NetworkAvatar.FindPlayerCharm<Charm_IncreaseLastAttackDamage_SwordAndShield>(), LastAttackCrownField);
        }

        private static void TriggerCrown(Charm_Basic? Charm, FieldInfo? CrownField)
        {
            if (Charm == null || CrownField == null) return;
            if (CrownField.GetValue(Charm) is CrownOfPride Crown && Crown != null)
                Crown.CreateCriticalSwing();
        }

        /// <summary>狂澜的命中时机：和右砍特效对齐后手动结算范围伤害。</summary>
        private IEnumerator RagingTideDamage(UnitAvatar? Target, Vector3 TargetPos, Vector2 SpawnDir, float Delay, float PivotDist, float RangeMult)
        {
            // 前摇阶段跟着目标走，出手瞬间锁定落点
            float LockDelay = Mathf.Max(0f, Delay - SlashSwingTime * 0.5f);
            yield return new WaitForSeconds(LockDelay);
            if (NetworkAvatar == null || !IsEffectEnabled) yield break;
            if (Target != null) TargetPos = Target.transform.position;
            // 锁定时按最终落点重算方向（击退/朝向）
            Vector2 HitDir = Vector2.right;
            Vector2 NewDir = (Vector2)TargetPos - (Vector2)NetworkAvatar.transform.position;
            if (NewDir.sqrMagnitude >= 0.0001f) HitDir = NewDir.normalized;
            yield return new WaitForSeconds(Delay - LockDelay);
            if (NetworkAvatar == null || !IsEffectEnabled) yield break;

            // 和挥砍特效同圆心、同半径
            Vector2 Pivot = (Vector2)TargetPos + SpawnDir * PivotDist;
            Vector2 Center = Pivot + SlashFxOffset * RangeMult;
            float Radius = Mathf.Max(0.1f, GetFxWidth(GetRagingTideSlashFrames(), SlashFxScale, 4.25f) * DamageRadiusFactor * RangeMult);
            float Damage = GetRagingTideDamage();
            float CritChance = GetRagingTideCritChance();
            TriggerRagingTideCrowns();
            foreach (UnitAvatar Enemy in Tools.SearchEnemiesInRange(NetworkAvatar, Center, Radius, includeBodySize: true))
                DealRagingTideDamage(Enemy, HitDir, Damage, CritChance);
        }

        private IEnumerator SlashDamage(UnitAvatar? Target, Vector3 TargetPos, Vector2 SpawnDir, float Delay, float PivotDist, float RangeMult, float DamageMult, EDamageFromType FromType)
        {
            // 前摇阶段跟着目标走，蓄力结束的瞬间锁定落点
            float LockDelay = Mathf.Max(0f, Delay - SlashSwingTime * 0.5f);
            yield return new WaitForSeconds(LockDelay);
            if (NetworkAvatar == null || !IsEffectEnabled) yield break;
            if (Target != null) TargetPos = Target.transform.position;
            // 锁定时按最终落点重算方向（击退/朝向）
            Vector2 HitDir = Vector2.right;
            Vector2 NewDir = (Vector2)TargetPos - (Vector2)NetworkAvatar.transform.position;
            if (NewDir.sqrMagnitude >= 0.0001f) HitDir = NewDir.normalized;
            yield return new WaitForSeconds(Delay - LockDelay);
            if (!IsEffectEnabled) yield break;
            // 挥砍：以特效中心为圆心，半径 = 特效宽度 × 系数 × 范围倍率
            Vector2 Pivot = (Vector2)TargetPos + SpawnDir * PivotDist;
            Vector2 Center = Pivot + SlashFxOffset * RangeMult;
            float Radius = Mathf.Max(0.1f, GetFxWidth(GetSlashLeftFrames(), SlashFxScale, 4.25f) * DamageRadiusFactor * RangeMult);
            float Damage = CalculateDamage(this) * DamageMult;
            foreach (UnitAvatar Enemy in Tools.SearchEnemiesInRange(NetworkAvatar, Center, Radius, includeBodySize: true))
                DealDamageTo(Enemy, HitDir, Damage, FromType);
        }

        private IEnumerator ThrustDamage(UnitAvatar? Target, Vector3 TargetPos, Vector2 SpawnDir, float Delay, float RangeMult, float DamageMult, EDamageFromType FromType)
        {
            yield return new WaitForSeconds(Delay);
            if (NetworkAvatar == null || !IsEffectEnabled) yield break;
            // 蓄力结束的瞬间锁定落点
            if (Target != null) TargetPos = Target.transform.position;
            // 锁定时按最终落点重算方向（击退/朝向）
            Vector2 HitDir = Vector2.right;
            Vector2 NewDir = (Vector2)TargetPos - (Vector2)NetworkAvatar.transform.position;
            if (NewDir.sqrMagnitude >= 0.0001f) HitDir = NewDir.normalized;
            // 刺出：沿路径持续判定，同一只怪每次刺击只吃一次（剑的行进距离不随范围变化）
            Vector2 Start = (Vector2)TargetPos + SpawnDir * ThrustDistance;
            Vector2 End = (Vector2)TargetPos - SpawnDir * ThrustDistance;
            float Radius = Mathf.Max(0.1f, GetFxWidth(GetThrustFrames(), ThrustFxScale, 0.8125f) * DamageRadiusFactor * RangeMult);
            float Damage = CalculateDamage(this) * DamageMult;
            HashSet<uint> Hit = [];
            float Elapsed = 0f;
            while (Elapsed < ThrustDuration)
            {
                if (NetworkAvatar == null || !IsEffectEnabled) yield break;
                float T = Mathf.Clamp01(Elapsed / Mathf.Max(0.01f, ThrustDuration));
                Vector2 Pos = Vector2.Lerp(Start, End, T);
                foreach (UnitAvatar Enemy in Tools.SearchEnemiesInRange(NetworkAvatar, Pos, Radius, includeBodySize: true))
                {
                    if (Enemy == null || Enemy.IsDead) continue;
                    if (!Hit.Add(Enemy.netId)) continue;
                    DealDamageTo(Enemy, HitDir, Damage, FromType);
                }
                yield return null;
                Elapsed += Time.deltaTime;
            }
        }

        /// <summary>统一构造一次风之刃伤害：元素、暴击、判定标记都在这里。</summary>
        private void DealWindSwordDamage(UnitAvatar Enemy, Vector2 Dir, float Damage, EDamageFromType FromType, string Flag, float CritChance = 0f)
        {
            if (NetworkAvatar == null || Enemy == null || Enemy.IsDead) return;
            DamageInstance Dam = DamageInstance.GetDamage(NetworkAvatar, DamageId, Enemy.transform.position, 4294967295L, Damage, EDamageType.Slice, FromType, Dir, 0, 0f);
            Dam.elementalType = EffectConstant.Wind;
            Dam.criticalDamageRate += NetworkAvatar.GetCustomStatUnsafe(TextFile.WindSwordCritDamage);
            Dam.criticalChancePercent += CritChance;
            Dam.flag = Flag;
            Enemy.ApplyDamage(Dam);
        }

        private void DealDamageTo(UnitAvatar Enemy, Vector2 Dir, float Damage, EDamageFromType FromType)
        {
            DealWindSwordDamage(Enemy, Dir, Damage, FromType, DamageId);
        }

        /// <summary>贴图宽度换算成世界单位（像素宽 / PPU × 缩放）。</summary>
        private static float GetFxWidth(Sprite[] Frames, float Scale, float Fallback)
        {
            if (Frames.Length == 0) return Fallback * Scale;
            return Frames[0].rect.width / Mathf.Max(1f, Frames[0].pixelsPerUnit) * Scale;
        }

        private void SendSwordsReset()
        {
            ModNetwork.SendEvent(netId, EventSwordsReset, NetworkAvatar != null ? NetworkAvatar.transform.position : transform.position);
        }

        protected override void Update_Client()
        {
            base.Update_Client();
            if (!isClient) return;
            // 形态变化时刷新物品图标（狂澜 Icon2 / 湍流 Icon3）
            SwordForm CurrentForm = Form;
            if (CurrentForm != LastIconForm)
            {
                LastIconForm = CurrentForm;
                itemUpdateDirtyBit = true;
            }
            if (NetworkAvatar == null || !IsEffectEnabled)
            {
                ClearSwords();
                return;
            }
            EnsureSwords();
            UpdateSwords();
            UpdatePhantoms();
        }

        private void EnsureSwords()
        {
            int Count = GetSwordCount(NetworkAvatar);
            while (Swords.Count < Count)
                Swords.Add(CreateSwordVisual());
            while (Swords.Count > Count)
            {
                int Last = Swords.Count - 1;
                DestroyVisual(Swords[Last]);
                Swords.RemoveAt(Last);
            }
            while (SpiritDrawCooldowns.Count < Count) SpiritDrawCooldowns.Add(0f);
            while (SpiritDrawCooldowns.Count > Count) SpiritDrawCooldowns.RemoveAt(SpiritDrawCooldowns.Count - 1);

            // 按形态换剑身贴图；万象的临时剑走 Phantoms，不受影响
            Sprite? Want = SwordSpriteOverride ?? Tools.LoadModSprite(GetFormSwordSprite());
            // 引灵形态的箭体额外缩小
            float FormScale = SwordScale * (Form == SwordForm.SpiritDraw ? SpiritDrawScale : 1f);
            Vector3 Scale = Vector3.one * FormScale;
            foreach (SwordVisual S in Swords)
            {
                if (S.Sword == null) continue;
                if (Want != null && S.Sword.sprite != Want) S.Sword.sprite = Want;
                // 每帧同步缩放，运行时改 SwordScale 也能立即生效
                if (S.Sword.transform.localScale != Scale) S.Sword.transform.localScale = Scale;
            }
        }

        private SwordVisual CreateSwordVisual()
        {
            GameObject Obj = new($"WindSword_{Swords.Count + Phantoms.Count}");
            Obj.transform.position = NetworkAvatar != null ? NetworkAvatar.transform.position : transform.position;
            Obj.transform.localScale = Vector3.one * SwordScale;
            SpriteRenderer Renderer = Obj.AddComponent<SpriteRenderer>();
            Renderer.sprite = SwordSpriteOverride ?? Tools.LoadModSprite(SwordSprite);
            // 剑身要和角色一样吃环境光：Default 排序层 + 游戏受光材质
            ApplyLitMaterial(Renderer);
            Renderer.sortingLayerName = "Default";
            Renderer.sortingOrder = 5;
            Renderer.enabled = false;
            return new SwordVisual { Sword = Renderer };
        }

        private static Material? UnlitSpriteMaterial;
        private static bool UnlitMaterialSearched;

        /// <summary>换成不受环境光影响的 sprite 材质；找不到就保持原材质。</summary>
        private static Material? ApplyUnlitMaterial(Material? Fallback)
        {
            if (!UnlitMaterialSearched)
            {
                UnlitMaterialSearched = true;
                string[] Names = ["Universal Render Pipeline/2D/Sprite-Unlit-Default", "Sprites/Default"];
                foreach (string Name in Names)
                {
                    Shader? Found = Shader.Find(Name);
                    if (Found == null) continue;
                    UnlitSpriteMaterial = new Material(Found) { name = "WindSword_Unlit" };
                    break;
                }
                if (UnlitSpriteMaterial == null) StarSephiriaMod.LogError("[WindSword] 没找到 unlit sprite 材质，剑身会受环境光影响");
            }
            return UnlitSpriteMaterial ?? Fallback;
        }

        /// <summary>用游戏的受光 sprite 材质，让剑身受环境光影响。</summary>
        private static void ApplyLitMaterial(SpriteRenderer Renderer)
        {
            if (CombatManager.Instance != null && CombatManager.Instance.spriteLitMaterial != null)
                Renderer.sharedMaterial = CombatManager.Instance.spriteLitMaterial;
        }

        /// <summary>按 ModFX 的可见度选项取透明度：自己的 FX / 队友的 FX。</summary>
        private static float GetOwnerFxAlpha(UnitAvatar? Avatar)
        {
            int Mode = 0;
            if (OptionsBinding.Instance != null && OptionsBinding.Instance.Options != null)
            {
                int OwnerIndex = Avatar != null && Avatar.isOwned ? 1 : 0;
                Mode = OwnerIndex == 1 ? OptionsBinding.Instance.Options.GetInt("MyFX", 0) : OptionsBinding.Instance.Options.GetInt("PartyMemberFX", 1);
            }
            return Mode switch
            {
                1 => 0.5f,
                2 => 0.15f,
                3 => 0f,
                _ => 1f,
            };
        }

        private static void DestroyVisual(SwordVisual S)
        {
            if (S.Fx != null) Destroy(S.Fx.gameObject);
            if (S.HitFx != null) Destroy(S.HitFx.gameObject);
            if (S.Sword != null) Destroy(S.Sword.gameObject);
        }

        private void UpdateSwords()
        {
            UnitAvatar Avatar = NetworkAvatar!;
            Vector2 PlayerPos = Avatar.transform.position;

            // 只用瞄准点的左右方向决定“背后”，整排不跟着武器角度旋转
            float Sign = 1f;
            if (Avatar is PlayerAvatar Player && Player.NetworkaimObject != null)
            {
                Vector2 ToAim = (Vector2)Player.NetworkaimObject.transform.position - PlayerPos;
                if (Mathf.Abs(ToAim.x) > 0.01f) Sign = Mathf.Sign(ToAim.x);
            }
            Vector2 Back = new(-Sign, 0f);
            // 形态、攻速倍率一帧只查一次
            SwordForm CurrentForm = GetForm(Avatar);
            float SpeedMult = Mathf.Max(0.01f, GetAttackSpeedMult(Avatar));

            // 风灵箭冷却推进：和服务器一致，狂澜形态暂停；死亡时也暂停
            if (!Avatar.IsDead && CurrentForm != SwordForm.RagingTide)
            {
                float Delta = Time.deltaTime;
                for (int i = 0; i < SpiritDrawCooldowns.Count; i++)
                    SpiritDrawCooldowns[i] = Mathf.Max(0f, SpiritDrawCooldowns[i] - Delta);
            }

            // 天丛云剑神击：后半数剑渐隐（不参与自动攻击由服务端处理）
            int ActiveCount = Swords.Count;
            if (WeaponPatch.KusanagiState.IsDivineStrike(Avatar, Swords.Count))
                ActiveCount = Swords.Count - Swords.Count / 2;

            for (int i = 0; i < Swords.Count; i++)
            {
                SwordVisual S = Swords[i];
                if (S.Sword == null) continue;
                switch (S.State)
                {
                    case SwordSlot: UpdateSlotSword(S, CurrentForm, i, PlayerPos, Back, SpeedMult, i >= ActiveCount); break;
                    case SwordFadeOut: UpdateFadeOutSword(S); break;
                    case SwordAppear: UpdateAppearSword(S); break;
                    case SwordStrike: UpdateStrikeSword(S); break;
                }
            }
        }

        private Vector2 GetSlotPos(int Index, Vector2 PlayerPos, Vector2 Back, float Bob = 0f)
        {
            return PlayerPos + Back * (SwordDistance + Index * SwordSpacing) + Vector2.up * (SwordFloatY + Bob);
        }

        /// <summary>湍流待机：以玩家头部为圆心、只取下半弧，在背后向下扇形展开，剑柄朝向头部。</summary>
        private Vector2 GetTurbulenceSlotPos(int Index, int Count, Vector2 PlayerPos, Vector2 Back, float Bob = 0f)
        {
            Vector2 Head = PlayerPos + Vector2.up * TurbulenceHeadHeight;
            // 弧心整体后移，避免剑贴在玩家身上
            Vector2 Center = Head + Back * TurbulenceBackOffset;

            // 只向下半弧展开：从“正后方”朝“正下方”转
            float BackAngle = Mathf.Atan2(Back.y, Back.x) * Mathf.Rad2Deg;
            float Direction = Mathf.DeltaAngle(BackAngle, -90f) >= 0f ? 1f : -1f;
            // 按段均分：把弧分成 Count 段，每把剑放在自己那一段的中间，不贴两边的端点
            float Step = Count > 0 ? TurbulenceArcSpread / Count : 0f;
            float Angle = BackAngle + Direction * (Index + 0.5f) * Step + TurbulenceArcTilt;
            float Rad = Angle * Mathf.Deg2Rad;
            return Center + new Vector2(Mathf.Cos(Rad), Mathf.Sin(Rad)) * (TurbulenceArcRadius + Bob);
        }

        /// <summary>引灵待机：在玩家身后排成一列，间距比普通形态更紧凑。</summary>
        private Vector2 GetSpiritDrawSlotPos(int Index, Vector2 PlayerPos, Vector2 Back, float Bob = 0f)
        {
            return PlayerPos + Back * (SpiritDrawDistance + Index * SpiritDrawSpacing) + Vector2.up * (SpiritDrawFloatY + Bob);
        }

        private Vector2 GetIdleSlotPos(SwordForm CurrentForm, int Index, int Count, Vector2 PlayerPos, Vector2 Back, float Bob = 0f)
        {
            if (IdlePositionProvider != null)
                return IdlePositionProvider(Index, Count, PlayerPos, Back) + IdlePositionOffset;
            Vector2 BasePos = CurrentForm switch
            {
                SwordForm.Turbulence => GetTurbulenceSlotPos(Index, Count, PlayerPos, Back, Bob),
                SwordForm.SpiritDraw => GetSpiritDrawSlotPos(Index, PlayerPos, Back, Bob),
                _ => GetSlotPos(Index, PlayerPos, Back, Bob),
            };
            return BasePos + IdlePositionOffset;
        }

        /// <summary>待机时剑柄朝向的目标点：普通形态是角色与剑的中点上方，湍流是玩家头部。</summary>
        private Vector2 GetIdleLookAt(SwordForm CurrentForm, int Index, Vector2 SwordPos, Vector2 PlayerPos)
        {
            if (IdleLookAtProvider != null) return IdleLookAtProvider(Index, SwordPos, PlayerPos);
            return CurrentForm == SwordForm.Turbulence
                ? PlayerPos + Vector2.up * TurbulenceHeadHeight
                : (PlayerPos + SwordPos) * 0.5f + Vector2.up * MidHeight;
        }

        private void UpdateSlotSword(SwordVisual S, SwordForm CurrentForm, int Index, Vector2 PlayerPos, Vector2 Back, float SpeedMult, bool Inactive = false)
        {
            // 引灵形态：箭排固定朝下，冷却中的箭隐身
            if (CurrentForm == SwordForm.SpiritDraw)
            {
                UpdateSpiritDrawSlotSword(S, Index, PlayerPos, Back, SpeedMult);
                return;
            }
            // 剑悬停在玩家左侧时水平翻转贴图，让剑身朝向一致
            S.Sword.flipX = Back.x < 0f;
            if (Inactive)
            {
                // 神击状态：后半数剑渐隐
                S.Sword.transform.position = GetIdleSlotPos(CurrentForm, Index, Swords.Count, PlayerPos, Back);
                S.Placed = true;
                SetFade(S, Mathf.MoveTowards(S.Fade, 0f, Time.deltaTime / Mathf.Max(0.01f, FadeOutTime)));
                return;
            }
            if (!S.Placed)
            {
                // 新凝聚的剑直接在槽位淡入，不从玩家身上飞出来
                S.Sword.transform.position = GetIdleSlotPos(CurrentForm, Index, Swords.Count, PlayerPos, Back);
                S.Placed = true;
            }
            // 消散回来后先保持隐身一小段，再重新渐入
            if (S.HiddenTimer > 0f)
            {
                S.HiddenTimer -= Time.deltaTime;
                S.Sword.transform.position = GetIdleSlotPos(CurrentForm, Index, Swords.Count, PlayerPos, Back);
                SetFade(S, 0f);
                return;
            }
            float Bob = Mathf.Sin(Time.time * 2.4f + Index * 0.8f) * 0.05f;
            Vector2 Pos = GetIdleSlotPos(CurrentForm, Index, Swords.Count, PlayerPos, Back, Bob);
            S.Sword.transform.position = Vector3.Lerp(S.Sword.transform.position, Pos, 0.3f);

            Vector2 Current = S.Sword.transform.position;
            Vector2 LookAt = GetIdleLookAt(CurrentForm, Index, Current, PlayerPos);
            float HiltAngle = Mathf.Atan2(LookAt.y - Current.y, LookAt.x - Current.x) * Mathf.Rad2Deg - 90f;
            S.Sword.transform.rotation = Quaternion.Euler(0f, 0f, HiltAngle);

            // 待机渐入同样受攻速影响
            float FadeIn = FadeInTime / SpeedMult;
            SetFade(S, Mathf.MoveTowards(S.Fade, 1f, Time.deltaTime / Mathf.Max(0.01f, FadeIn)));
        }

        /// <summary>引灵待机：箭头朝下排成一列；被消耗的箭在冷却期间隐身，冷却结束后渐入。</summary>
        private void UpdateSpiritDrawSlotSword(SwordVisual S, int Index, Vector2 PlayerPos, Vector2 Back, float SpeedMult)
        {
            S.Sword.flipX = false;
            float Cooldown = Index >= 0 && Index < SpiritDrawCooldowns.Count ? SpiritDrawCooldowns[Index] : 0f;
            if (Cooldown > 0f)
            {
                S.Sword.transform.position = GetIdleSlotPos(SwordForm.SpiritDraw, Index, Swords.Count, PlayerPos, Back);
                S.Placed = true;
                SetFade(S, 0f);
                return;
            }
            if (!S.Placed)
            {
                S.Sword.transform.position = GetIdleSlotPos(SwordForm.SpiritDraw, Index, Swords.Count, PlayerPos, Back);
                S.Placed = true;
            }
            float Bob = Mathf.Sin(Time.time * 2.4f + Index * 0.8f) * 0.05f;
            Vector2 Pos = GetIdleSlotPos(SwordForm.SpiritDraw, Index, Swords.Count, PlayerPos, Back, Bob);
            S.Sword.transform.position = Vector3.Lerp(S.Sword.transform.position, Pos, 0.3f);
            // 箭身固定朝下，不跟随剑柄瞄准点旋转
            S.Sword.transform.rotation = Quaternion.identity;
            float FadeIn = FadeInTime / Mathf.Max(0.01f, SpeedMult);
            SetFade(S, Mathf.MoveTowards(S.Fade, 1f, Time.deltaTime / Mathf.Max(0.01f, FadeIn)));
        }

        private void UpdateFadeOutSword(SwordVisual S)
        {
            S.Timer += Time.deltaTime;
            float Duration = FadeOutTime / Mathf.Max(0.01f, S.SpeedMult);
            // 万象的临时剑：消失/出现阶段保持隐身，真正出手时才淡入
            SetFade(S, S.Phantom ? 0f : 1f - Mathf.Clamp01(S.Timer / Mathf.Max(0.01f, Duration)));
            if (S.Timer < Duration) return;
            SetFade(S, 0f);
            S.State = SwordAppear;
            S.Timer = 0f;
        }

        private void UpdateAppearSword(SwordVisual S)
        {
            S.Timer += Time.deltaTime;
            if (S.Timer >= AppearDelay) BeginStrike(S);
        }

        private void BeginStrike(SwordVisual S)
        {
            S.State = SwordStrike;
            S.Timer = 0f;
            // 出现在目标身边时从透明渐入
            SetFade(S, 0f);
            // 前摇开始前再锁一次目标位置，之后在前摇里持续跟随
            S.LookTarget = ResolveUnit(S.TargetNetId);
            if (S.LookTarget != null && !S.LookTarget.IsDead)
                S.TargetPos = S.LookTarget.transform.position;

            // 出现方位用服务端给的，保证和伤害判定一致
            Vector2 SpawnDir = S.SpawnDir;
            if (SpawnDir.sqrMagnitude < 0.0001f)
            {
                float SpawnAngle = Random.Range(0f, Mathf.PI * 2f);
                SpawnDir = new Vector2(Mathf.Cos(SpawnAngle), Mathf.Sin(SpawnAngle));
                S.SpawnDir = SpawnDir;   // 前摇跟踪时也用同一个方向
            }

            if (S.Type == 2)
            {
                S.FxFrames = GetThrustFrames();
                SetupThrustGeometry(S);
                Vector2 ToTarget = (S.TargetPos - S.StartPos).normalized;
                S.FxAngle = Mathf.Atan2(ToTarget.y, ToTarget.x) * Mathf.Rad2Deg - 90f;
            }
            else
            {
                bool Raging = IsRagingTide && !S.Phantom;
                S.FxFrames = Raging ? GetRagingTideSlashFrames() : (S.Type == 0 ? GetSlashLeftFrames() : GetSlashRightFrames());
                SetupSlashGeometry(S);
            }

            // 缓存本次动作时长，避免 UpdateStrikeSword 每帧重算
            S.StrikeCharge = S.Type == 2 ? Mathf.Max(0f, ThrustChargeTime) : Mathf.Max(0f, SlashWindupTime);
            S.StrikeDuration = S.Type == 2
                ? Mathf.Max(0.03f, ThrustDuration)
                : Mathf.Max(0.05f, S.FxFrames.Length / Mathf.Max(1f, SlashFps));
            S.StrikeFxDuration = S.Type == 2 ? Mathf.Max(0.03f, ThrustFxTime) : S.StrikeDuration;
            float StrikeLength = S.Type == 2
                ? Mathf.Max(S.StrikeDuration, S.StrikeFxDuration)
                : Mathf.Max(Mathf.Max(0.03f, SlashSwingTime), S.StrikeDuration);
            S.StrikeTotal = Mathf.Max(0.03f, S.StrikeCharge + StrikeLength);

            if (S.Fx == null)
            {
                GameObject FxObj = new("WindSword_Fx");
                S.Fx = FxObj.AddComponent<SpriteRenderer>();
                Material? UnlitMat = ApplyUnlitMaterial(S.Fx.sharedMaterial);
                if (UnlitMat != null) S.Fx.sharedMaterial = UnlitMat;
                S.Fx.sortingLayerName = "FX";
                S.Fx.sortingOrder = 6;
            }
            // 特效透明度：叠加 ModFX 的可见度选项
            float FxOpacity = Mathf.Clamp01(FxAlpha) * GetOwnerFxAlpha(NetworkAvatar);
            S.Fx.color = new Color(1f, 1f, 1f, FxOpacity);
            // 前摇阶段不放特效，等真正出手时再显示
            S.Fx.enabled = false;
            // 特效随攻击范围放大
            float FxScale = (S.Type == 2 ? ThrustFxScale : SlashFxScale) * S.RangeMult;
            S.Fx.transform.localScale = Vector3.one * FxScale;
            if (S.FxFrames.Length > 0) S.Fx.sprite = S.FxFrames[0];
            // 挥砍特效和剑共用同一个圆心；刺出跟着剑体
            S.Fx.transform.position = S.Type == 2 ? S.TargetPos : S.Pivot + SlashFxOffset * S.RangeMult;
            S.Fx.transform.rotation = Quaternion.Euler(0f, 0f, S.FxAngle);
            // 本次攻击的音效/命中特效重置
            S.SwingSoundPlayed = false;
            S.HitFxPlayed = false;
            S.HitFxTimer = 0f;
            if (S.HitFx != null) S.HitFx.enabled = false;
        }

        private static UnitAvatar? ResolveUnit(uint NetId)
        {
            if (NetId == 0) return null;
            if (NetworkServer.active && NetworkServer.spawned.TryGetValue(NetId, out NetworkIdentity ServerIdentity))
                return ServerIdentity.GetComponent<UnitAvatar>();
            if (NetworkClient.active && NetworkClient.spawned.TryGetValue(NetId, out NetworkIdentity ClientIdentity))
                return ClientIdentity.GetComponent<UnitAvatar>();
            return null;
        }

        private void SetupThrustGeometry(SwordVisual S)
        {
            S.StartPos = S.TargetPos + S.SpawnDir * ThrustDistance;
            S.EndPos = S.TargetPos - S.SpawnDir * ThrustDistance;
        }

        /// <summary>以敌人外侧一点为圆心；圆心距离随体型走，剑刃绕圆心的半径固定，和特效大小一致。</summary>
        private void SetupSlashGeometry(SwordVisual S)
        {
            float PivotDistance = S.PivotDistance > 0f ? S.PivotDistance : SlashPivotDistance;
            S.Pivot = S.TargetPos + S.SpawnDir * PivotDistance;
            S.Radius = SlashPivotDistance;
            float BaseAngle = Mathf.Atan2(S.TargetPos.y - S.Pivot.y, S.TargetPos.x - S.Pivot.x) * Mathf.Rad2Deg;
            S.DirSign = S.Type == 0 ? 1f : -1f;
            S.StartAngle = BaseAngle - S.DirSign * SlashArc * 0.5f;
            S.EndAngle = BaseAngle + S.DirSign * SlashArc * 0.5f;
            S.FxAngle = BaseAngle + (S.Type == 0 ? SlashFxAngleOffsetLeft : SlashFxAngleOffsetRight);
        }

        /// <summary>前摇/蓄力阶段跟随目标：更新落点，并重算挥砍圆心或刺出路径。</summary>
        private void UpdateStrikeAim(SwordVisual S)
        {
            if (S.LookTarget == null || S.LookTarget.IsDead) return;
            Vector2 NewPos = S.LookTarget.transform.position;
            if (((Vector2)S.TargetPos - NewPos).sqrMagnitude < 0.000001f) return;
            S.TargetPos = NewPos;
            if (S.Type == 2)
            {
                SetupThrustGeometry(S);
            }
            else
            {
                SetupSlashGeometry(S);
                if (S.Fx != null)
                {
                    S.Fx.transform.position = S.Pivot + SlashFxOffset * S.RangeMult;
                    S.Fx.transform.rotation = Quaternion.Euler(0f, 0f, S.FxAngle);
                }
            }
        }

        private void UpdateStrikeSword(SwordVisual S)
        {
            float Charge = S.StrikeCharge;
            float Duration = S.StrikeDuration;
            float FxDuration = S.StrikeFxDuration;
            float Total = S.StrikeTotal;

            // 前摇/蓄力阶段持续瞄准目标，真正出手时锁定
            if (S.Timer < Charge) UpdateStrikeAim(S);
            S.Timer += Time.deltaTime;
            float T = Mathf.Clamp01(S.Timer / Total);
            // 先渐入；动作过半后开始渐出，渐出不超过动作剩余时间
            float FadeStart = Total * Mathf.Clamp01(DissipateStartRatio);
            if (S.Timer >= FadeStart)
            {
                float FadeTime = Mathf.Min(DissipateTime / Mathf.Max(0.01f, S.SpeedMult), Mathf.Max(0.01f, Total - FadeStart));
                SetFade(S, 1f - Mathf.Clamp01((S.Timer - FadeStart) / FadeTime));
            }
            else
            {
                SetFade(S, Mathf.MoveTowards(S.Fade, 1f, Time.deltaTime / Mathf.Max(0.01f, AppearFadeTime / Mathf.Max(0.01f, S.SpeedMult))));
            }

            // 出手音效：前摇结束、特效出现时
            if (!S.SwingSoundPlayed && S.Timer >= Charge)
            {
                S.SwingSoundPlayed = true;
                PlaySwingSound(S.Sword.transform.position);
            }
            // 命中特效：挥砍在挥动过半，刺出在冲刺开始
            float HitTime = S.Type == 2 ? Charge : Charge + SlashSwingTime * 0.5f;
            if (!S.HitFxPlayed && S.Timer >= HitTime)
            {
                S.HitFxPlayed = true;
                PlayHitFx(S);
            }
            UpdateHitFx(S);

            if (S.Type == 2)
            {
                Vector2 MoveDir = (S.EndPos - S.StartPos).normalized;
                if (S.Timer < Charge)
                {
                    // 蓄力：往后拉一点，特效不显示
                    Vector2 Pos = S.StartPos - MoveDir * 0.18f * (S.Timer / Mathf.Max(0.01f, Charge));
                    S.Sword.transform.position = Pos;
                    S.Sword.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-MoveDir.y, -MoveDir.x) * Mathf.Rad2Deg - 90f);
                    if (S.Fx != null) S.Fx.enabled = false;
                }
                else
                {
                    // 刺出：剑快速穿过目标，特效跟着剑体、按自己的速度播完
                    float DashT = Mathf.Clamp01((S.Timer - Charge) / Duration);
                    Vector2 Pos = Vector2.Lerp(S.StartPos, S.EndPos, DashT);
                    S.Sword.transform.position = Pos;
                    S.Sword.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-MoveDir.y, -MoveDir.x) * Mathf.Rad2Deg - 90f);
                    if (S.Fx != null)
                    {
                        float FxT = Mathf.Clamp01((S.Timer - Charge) / FxDuration);
                        S.Fx.enabled = S.FxFrames.Length > 0;
                        S.Fx.transform.position = Pos;
                        S.Fx.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(MoveDir.y, MoveDir.x) * Mathf.Rad2Deg - 90f);
                        if (S.FxFrames.Length > 0)
                        {
                            int Frame = Mathf.Clamp(Mathf.FloorToInt(FxT * S.FxFrames.Length), 0, S.FxFrames.Length - 1);
                            S.Fx.sprite = S.FxFrames[Frame];
                        }
                    }
                }
            }
            else
            {
                // 挥砍：先反向拉一下做前摇，再从 -90° 快速扫到 +90°
                if (S.Timer < Charge)
                {
                    float WindupT = S.Timer / Mathf.Max(0.01f, Charge);
                    ApplySlashPose(S, Mathf.Lerp(S.StartAngle - S.DirSign * SlashWindupAngle, S.StartAngle, WindupT));
                    if (S.Fx != null) S.Fx.enabled = false;
                }
                else
                {
                    float Elapsed = S.Timer - Charge;
                    float SwingT = Mathf.Clamp01(Elapsed / Mathf.Max(0.03f, SlashSwingTime));
                    ApplySlashPose(S, Mathf.Lerp(S.StartAngle, S.EndAngle, SwingT));
                    // 特效朝向固定，只播帧
                    if (S.Fx != null && S.FxFrames.Length > 0 && Elapsed <= Duration)
                    {
                        S.Fx.enabled = true;
                        int Frame = Mathf.Clamp(Mathf.FloorToInt(Elapsed / Duration * S.FxFrames.Length), 0, S.FxFrames.Length - 1);
                        S.Fx.sprite = S.FxFrames[Frame];
                    }
                }
            }

            if (T >= 1f)
            {
                if (S.Fx != null) S.Fx.enabled = false;
                if (S.HitFx != null) S.HitFx.enabled = false;
                SetFade(S, 0f);
                S.State = SwordSlot;
                S.Timer = 0f;
                S.HiddenTimer = ReturnDelay;
            }
        }

        private void ApplySlashPose(SwordVisual S, float Angle)
        {
            float Rad = Angle * Mathf.Deg2Rad;
            Vector2 Pos = S.Pivot + new Vector2(Mathf.Cos(Rad), Mathf.Sin(Rad)) * S.Radius;
            S.Sword.transform.position = Pos;
            float HiltAngle = Mathf.Atan2(S.Pivot.y - Pos.y, S.Pivot.x - Pos.x) * Mathf.Rad2Deg - 90f;
            S.Sword.transform.rotation = Quaternion.Euler(0f, 0f, HiltAngle);
        }

        /// <summary>剑身透明度：动作淡入淡出 × FxAlpha × ModFX 可见度，和特效一致。</summary>
        private float GetSwordAlpha(float Fade)
        {
            return Fade * Mathf.Clamp01(FxAlpha) * GetOwnerFxAlpha(NetworkAvatar);
        }

        private void SetFade(SwordVisual S, float Fade)
        {
            if (Mathf.Abs(S.Fade - Fade) < 0.0001f) return;
            S.Fade = Fade;
            Color C = S.Sword.color;
            C.a = GetSwordAlpha(Fade);
            S.Sword.color = C;
            S.Sword.enabled = C.a > 0.001f;
        }

        private static Sprite[]? SlashLeftFrames;
        private static Sprite[]? SlashRightFrames;
        private static Sprite[]? ThrustFrames;

        private static Sprite[] LoadFrames(string Path, string Prefix, int Count)
        {
            List<Sprite> Frames = [];
            for (int i = 0; i < Count; i++)
            {
                Sprite? Frame = Tools.LoadModSpriteForPath(Path, $"{Prefix}{i}");
                if (Frame != null) Frames.Add(Frame);
            }
            return Frames.ToArray();
        }

        private static Sprite[] GetSlashLeftFrames() => SlashLeftFrames ??= LoadFrames("Weapon.Katana_Wind", "Katana_FAttack1_Swing", 5);
        private static Sprite[] GetSlashRightFrames() => SlashRightFrames ??= LoadFrames("Weapon.Katana_Wind", "Katana_FAttack2_Swing", 6);
        private static Sprite[] GetThrustFrames() => ThrustFrames ??= LoadFrames("Weapon.GreatSword_Wind", "GreatSword_FireMinor_DashSwing_", 8);

        private static Sprite[]? GaleQuellSlashFrames;
        private static Sprite[] GetRagingTideSlashFrames() => GaleQuellSlashFrames ??= LoadFrames2("Weapon.GreatSword_GaleQuell", "GaleQuell_SwingFX_", 4);

        private static Sprite[]? HitFrames;
        private static Sprite[] GetHitFrames() => HitFrames ??= LoadFrames2("EffectFx", "FireSpear_SlashFX", 7);

        private static Sprite[] LoadFrames2(string Path, string Prefix, int Count)
        {
            List<Sprite> Frames = [];
            for (int i = 0; i < Count; i++)
            {
                Sprite? Frame = Tools.LoadModSpriteForPath(Path, $"{Prefix}{i:00}");
                if (Frame != null) Frames.Add(Frame);
            }
            return Frames.ToArray();
        }

        private EventReference SwingSound;
        private int CombatSoundsWeaponId = -1;

        /// <summary>从指定武器（默认 400 普通武士刀）的普攻取挥刀音效。</summary>
        private void LoadCombatSounds()
        {
            if (CombatSoundsWeaponId == SoundSourceWeaponId) return;
            CombatSoundsWeaponId = SoundSourceWeaponId;
            SwingSound = default;
            try
            {
                WeaponEntity? Entity = WeaponDatabase.FindWeaponById(SoundSourceWeaponId);
                GameObject? Prefab = Entity != null ? Entity.mainWeaponPrefab : null;
                WeaponSimple? Weapon = Prefab != null ? Prefab.GetComponent<WeaponSimple>() : null;
                if (Weapon == null || Weapon.basicComboAttacks.Length == 0) return;

                foreach (NewWeaponFireData Data in Weapon.basicComboAttacks)
                {
                    if (Data == null) continue;
                    if (SwingSound.IsNull && !Data.swingSoundEvent.IsNull)
                    {
                        SwingSound = Data.swingSoundEvent;
                        break;
                    }
                }
            }
            catch (System.Exception Ex)
            {
                StarSephiriaMod.LogError($"[WindSword] 读取武器音效失败: {Ex.Message}");
            }
        }

        private void PlaySwingSound(Vector3 Position)
        {
            LoadCombatSounds();
            if (SwingSound.IsNull) return;
            SoundManager.PlayOneShot(SwingSound, Position);
        }

        private void PlayHitFx(SwordVisual S)
        {
            if (S.HitFx == null)
            {
                GameObject Obj = new("WindSword_HitFx");
                S.HitFx = Obj.AddComponent<SpriteRenderer>();
                Material? UnlitMat = ApplyUnlitMaterial(S.HitFx.sharedMaterial);
                if (UnlitMat != null) S.HitFx.sharedMaterial = UnlitMat;
                S.HitFx.sortingLayerName = "FX";
                S.HitFx.sortingOrder = 7;
            }
            S.HitFxFrames = GetHitFrames();
            if (S.HitFxFrames.Length == 0) return;
            S.HitFx.enabled = true;
            S.HitFxTimer = 0f;
            S.HitFx.sprite = S.HitFxFrames[0];
            S.HitFx.transform.position = S.Sword.transform.position;
            // 挥砍：随挥动方向再转 90°；右砍扫击方向相反，转 270°；刺出：和冲刺方向一致
            float Angle = S.Type == 2
                ? Mathf.Atan2(S.EndPos.y - S.StartPos.y, S.EndPos.x - S.StartPos.x) * Mathf.Rad2Deg - 90f
                : S.FxAngle + (S.Type == 1 ? 270f : 90f);
            S.HitFx.transform.rotation = Quaternion.Euler(0f, 0f, Angle);
            S.HitFx.flipX = false;
            S.HitFx.color = new Color(1f, 1f, 1f, Mathf.Clamp01(FxAlpha) * GetOwnerFxAlpha(NetworkAvatar));
        }

        private void UpdateHitFx(SwordVisual S)
        {
            if (S.HitFx == null || !S.HitFx.enabled || S.HitFxFrames.Length == 0) return;
            S.HitFxTimer += Time.deltaTime;
            float T = Mathf.Clamp01(S.HitFxTimer / Mathf.Max(0.03f, HitFxTime));
            int Frame = Mathf.Clamp(Mathf.FloorToInt(T * S.HitFxFrames.Length), 0, S.HitFxFrames.Length - 1);
            S.HitFx.sprite = S.HitFxFrames[Frame];
            if (S.Type == 2) S.HitFx.transform.position = S.Sword.transform.position; // 跟着刺击
            if (T >= 1f) S.HitFx.enabled = false;
        }

        /// <summary>万象魔晶召唤的临时剑：播完动作直接销毁，不进槽位。</summary>
        private void UpdatePhantoms()
        {
            for (int i = Phantoms.Count - 1; i >= 0; i--)
            {
                SwordVisual S = Phantoms[i];
                if (S.Sword == null)
                {
                    Phantoms.RemoveAt(i);
                    continue;
                }
                switch (S.State)
                {
                    case SwordFadeOut: UpdateFadeOutSword(S); break;
                    case SwordAppear: UpdateAppearSword(S); break;
                    case SwordStrike: UpdateStrikeSword(S); break;
                }
                if (S.State == SwordSlot)
                {
                    DestroyVisual(S);
                    Phantoms.RemoveAt(i);
                }
            }
        }

        private void ClearPhantoms()
        {
            foreach (SwordVisual S in Phantoms) DestroyVisual(S);
            Phantoms.Clear();
        }

        private void ClearSwords()
        {
            foreach (SwordVisual S in Swords) DestroyVisual(S);
            Swords.Clear();
            ClearPhantoms();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeControllerEvents();
            ClearSwords();
        }

        /// <summary>Mirror 编织标记。</summary>
        public override bool Weaved()
        {
            return true;
        }
    }
}
