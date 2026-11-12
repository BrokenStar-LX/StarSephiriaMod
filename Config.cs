using System;
using UnityEngine;

public class ModConfigData
{
    public bool IsTestMode { get; set; } = false;
    public bool DisplayDisabledItems { get; set; } = false;
    public bool DebugLogging { get; set; } = true;
    public bool DebugLoggingWeapon { get; set; } = false;
    public bool DebugLoggingDungeon { get; set; } = false;
    public bool DebugLoggingBoss { get; set; } = false;
    public bool DebugLoggingProjectile { get; set; } = false;

    public bool EnableCostumeModifier { get; set; } = true;
    public bool EnableCostumeTransmog { get; set; } = true;
    public bool EnableWeaponModifier { get; set; } = true;
    public bool EnableDamageStatistics { get; set; } = true;

    private int _monochromeMode = 0;
    public int MonochromeMode
    {
        get { return _monochromeMode; }
        set { _monochromeMode = Math.Clamp(value, 0, 4); }
    }
    public bool MonochromeMode_TABLET { get; set; } = false;

    public bool ModifyDefenseFormula { get; set; } = true;
    public bool ModifyEvasionFormula { get; set; } = true;
    public bool ModifyMerchant { get; set; } = true;
    private int _modifyConnectionsStack = 0;
    public int ModifyConnectionsStack
    {
        get { return _modifyConnectionsStack; }
        set { _modifyConnectionsStack = Math.Clamp(value, 0, 5); }
    }
    public bool ModifyDummyDamage { get; set; } = true;
    public bool ModifyMiracle { get; set; } = true;
    public bool ModifyPassive { get; set; } = true;
    public bool ModifyCharm { get; set; } = true;
    public bool ModifyRandomWeaponOnStart { get; set; } = true;

    public bool DisableUniqueEffects { get; set; } = false;
}

public static class ModConfig
{
    private static int _hardModeFrame = -1;
    private static int _hardModeValue;
    public static int HardMode
    {
        get
        {
            int Frame = Time.frameCount;
            if (Frame == _hardModeFrame) return _hardModeValue;

            int Value = 0;
            bool Found = DungeonManager.Instance && DungeonManager.Instance.hardModeEnvironment.TryGetValue("[Star's Sephiria Mod]HardMode", out Value);
            if (!Found)
                Value = SaveManager.Current.GetInt("HardModeShard_[Star's Sephiria Mod]HardMode_Level", 0);

            _hardModeFrame = Frame;
            _hardModeValue = Value;
            return Value;
        }
    }
    public static bool InfiniteMode
    {
        get
        {
            if (DungeonManager.Instance)
            {
                if (DungeonManager.Instance.hardModeEnvironment.TryGetValue("[Star's Sephiria Mod]InfiniteMode", out int Value))
                    return Value > 0;
            }
            return SaveManager.Current.GetInt("HardModeShard_[Star's Sephiria Mod]InfiniteMode_Level", 0) > 0;
        }
    }
    public static bool BossRushMode
    {
        get
        {
            if (DungeonManager.Instance)
            {
                if (DungeonManager.Instance.hardModeEnvironment.TryGetValue("[Star's Sephiria Mod]BossRushMode", out int Value))
                    return Value > 0;
            }
            return SaveManager.Current.GetInt("HardModeShard_[Star's Sephiria Mod]BossRushMode_Level", 0) > 0;
        }
    }
    public static bool PollutedBlood
    {
        get
        {
            if (DungeonManager.Instance)
            {
                if (DungeonManager.Instance.hardModeEnvironment.TryGetValue("[Star's Sephiria Mod]PollutedBlood", out int Value))
                    return Value > 0;
            }
            return SaveManager.Current.GetInt("HardModeShard_[Star's Sephiria Mod]PollutedBlood_Level", 0) > 0;
        }
    }
    public static bool EvilGuardian
    {
        get
        {
            if (DungeonManager.Instance)
            {
                if (DungeonManager.Instance.hardModeEnvironment.TryGetValue("[Star's Sephiria Mod]EvilGuardian", out int Value))
                    return Value > 0;
            }
            return SaveManager.Current.GetInt("HardModeShard_[Star's Sephiria Mod]EvilGuardian_Level", 0) > 0;
        }
    }
    public static ModConfigData Instance { get; private set; } = new ModConfigData();

    private static ModOptions? _options;
    private static bool _registered;

    public static void Initialize(ModOptions? options)
    {
        _options = options;
        try
        {
            if (_options != null && !_registered)
            {
                RegisterOptions(_options);
                _registered = true;
            }
            ApplyOptions();
        }
        catch (Exception ex)
        {
            HorayDebug.LogErrorDev($"[配置] 注册模组选项失败，使用默认值：{ex.Message}");
        }
    }

    private static void RegisterOptions(ModOptions o)
    {
        o.DisplayName = "Star's SephiriaMod";

        AddToggle(o, "enable_weapon_modifier", "ModOption_WeaponModifier", () => Instance.EnableWeaponModifier, v => Instance.EnableWeaponModifier = v);
        AddToggle(o, "enable_costume_modifier", "ModOption_CostumeModifier", () => Instance.EnableCostumeModifier, v => Instance.EnableCostumeModifier = v);
        AddToggle(o, "enable_costume_transmog", "ModOption_CostumeTransmog", () => Instance.EnableCostumeTransmog, v => Instance.EnableCostumeTransmog = v);
        AddToggle(o, "enable_damage_statistics", "ModOption_DamageStatistics", () => Instance.EnableDamageStatistics, v => Instance.EnableDamageStatistics = v);

        AddToggle(o, "modify_defense_formula", "ModOption_DefenseFormula", () => Instance.ModifyDefenseFormula, v => Instance.ModifyDefenseFormula = v);
        AddToggle(o, "modify_evasion_formula", "ModOption_EvasionFormula", () => Instance.ModifyEvasionFormula, v => Instance.ModifyEvasionFormula = v);
        AddToggle(o, "modify_merchant", "ModOption_Merchant", () => Instance.ModifyMerchant, v => Instance.ModifyMerchant = v);
        AddToggle(o, "modify_dummy_damage", "ModOption_DummyDamage", () => Instance.ModifyDummyDamage, v => Instance.ModifyDummyDamage = v);
        AddToggle(o, "modify_miracle", "ModOption_Miracle", () => Instance.ModifyMiracle, v => Instance.ModifyMiracle = v);
        AddToggle(o, "modify_passive", "ModOption_Passive", () => Instance.ModifyPassive, v => Instance.ModifyPassive = v);
        AddToggle(o, "modify_charm", "ModOption_Charm", () => Instance.ModifyCharm, v => Instance.ModifyCharm = v);
        AddToggle(o, "modify_random_weapon_on_start", "ModOption_RandomWeaponOnStart", () => Instance.ModifyRandomWeaponOnStart, v => Instance.ModifyRandomWeaponOnStart = v);
        AddToggle(o, "disable_unique_effects", "ModOption_DisableUniqueEffects", () => Instance.DisableUniqueEffects, v => Instance.DisableUniqueEffects = v);

        AddSelection(o, "monochrome_mode", "ModOption_MonochromeMode",
            ["ModOption_Monochrome_0", "ModOption_Monochrome_1", "ModOption_Monochrome_2", "ModOption_Monochrome_3", "ModOption_Monochrome_4"],
            () => Instance.MonochromeMode, v => Instance.MonochromeMode = v);
        AddToggle(o, "monochrome_mode_tablet", "ModOption_MonochromeTablet", () => Instance.MonochromeMode_TABLET, v => Instance.MonochromeMode_TABLET = v);

        AddSelection(o, "modify_connections_stack", "ModOption_ConnectionsStack",
            ["ModOption_ConnectionsStack_0", "ModOption_ConnectionsStack_1", "ModOption_ConnectionsStack_2", "ModOption_ConnectionsStack_3", "ModOption_ConnectionsStack_4", "ModOption_ConnectionsStack_5"],
            () => Instance.ModifyConnectionsStack, v => Instance.ModifyConnectionsStack = v);
    }

    private static void AddToggle(ModOptions o, string key, string labelKey, Func<bool> get, Action<bool> set)
    {
        ModToggleOption opt = o.AddToggle(key, get(), new LocalizedString(labelKey), v =>
        {
            set(v);
            o.Save();
        });
        set(opt.Value);
    }

    private static void AddSelection(ModOptions o, string key, string labelKey, string[] choiceKeys, Func<int> get, Action<int> set)
    {
        LocalizedString[] choices = new LocalizedString[choiceKeys.Length];
        for (int i = 0; i < choiceKeys.Length; i++)
            choices[i] = new LocalizedString(choiceKeys[i]);

        ModSelectionOption opt = o.AddSelectionBox(key, choices, get(), new LocalizedString(labelKey), v =>
        {
            set(v);
            o.Save();
        });
        set(opt.Value);
    }

    private static void ApplyOptions()
    {
        if (_options == null) return;

        Instance.EnableWeaponModifier = _options.GetBool("enable_weapon_modifier", Instance.EnableWeaponModifier);
        Instance.EnableCostumeModifier = _options.GetBool("enable_costume_modifier", Instance.EnableCostumeModifier);
        Instance.EnableCostumeTransmog = _options.GetBool("enable_costume_transmog", Instance.EnableCostumeTransmog);
        Instance.EnableDamageStatistics = _options.GetBool("enable_damage_statistics", Instance.EnableDamageStatistics);

        Instance.ModifyDefenseFormula = _options.GetBool("modify_defense_formula", Instance.ModifyDefenseFormula);
        Instance.ModifyEvasionFormula = _options.GetBool("modify_evasion_formula", Instance.ModifyEvasionFormula);
        Instance.ModifyMerchant = _options.GetBool("modify_merchant", Instance.ModifyMerchant);
        Instance.ModifyDummyDamage = _options.GetBool("modify_dummy_damage", Instance.ModifyDummyDamage);
        Instance.ModifyMiracle = _options.GetBool("modify_miracle", Instance.ModifyMiracle);
        Instance.ModifyPassive = _options.GetBool("modify_passive", Instance.ModifyPassive);
        Instance.ModifyCharm = _options.GetBool("modify_charm", Instance.ModifyCharm);
        Instance.ModifyRandomWeaponOnStart = _options.GetBool("modify_random_weapon_on_start", Instance.ModifyRandomWeaponOnStart);
        Instance.DisableUniqueEffects = _options.GetBool("disable_unique_effects", Instance.DisableUniqueEffects);

        Instance.MonochromeMode = _options.GetInt("monochrome_mode", Instance.MonochromeMode);
        Instance.MonochromeMode_TABLET = _options.GetBool("monochrome_mode_tablet", Instance.MonochromeMode_TABLET);

        Instance.ModifyConnectionsStack = Math.Clamp(_options.GetInt("modify_connections_stack", Instance.ModifyConnectionsStack), 0, 5);
    }
}
