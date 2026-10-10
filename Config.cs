using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;

public class ModConfigData
{
    // 基础开关
    public bool IsTestMode { get; set; } = false;
    public bool DisplayDisabledItems { get; set; } = false;
    public bool EnableCostumeModifier { get; set; } = true;
    public bool EnableWeaponModifier { get; set; } = true;
    public bool EnableDamageStatistics { get; set; } = true;

    // 游戏模式
    private int _monochromeMode = 0;
    public int MonochromeMode
    {
        get { return _monochromeMode; }
        set { _monochromeMode = Math.Clamp(value, 0, 4); }
    }
    public bool MonochromeMode_TABLET { get; set; } = false;

    // 机制更改
    public bool ModifyDefenseFormula { get; set; } = true;
    public bool ModifyEvasionFormula { get; set; } = true;
    public bool ModifyMerchant { get; set; } = true;
    public int ModifyConnectionsStack { get; set; } = 0;
    public bool ModifyDummyDamage { get; set; } = true;
    public bool ModifyMiracle { get; set; } = true;
    public bool ModifyPassive { get; set; } = true;
    public bool ModifyCharm { get; set; } = true;
    public bool ModifyRandomWeaponOnStart { get; set; } = true;

    // 新增内容
    public bool AddWeaponEnhancements { get; set; } = true;
    public bool AddItem { get; set; } = true;
    public bool AddMiracle { get; set; } = true;
    public bool AddCategory { get; set; } = true;

    // 其他
    public bool Ultimate { get; set; } = false;

    // 神器更改
    public bool DisableUniqueEffects { get; set; } = false;

    // 调试选项
    public bool DebugLogging { get; set; } = false;
    public bool DebugLoggingWeapon { get; set; } = false;
    public bool DebugLoggingDungeon { get; set; } = false;
    public bool DebugLoggingBoss { get; set; } = false;
    public bool DebugLoggingProjectile { get; set; } = false;
}

public static class ModConfig
{
    public static int HardMode
    {
        get
        {
            if (DungeonManager.Instance)
            {
                if (DungeonManager.Instance.hardModeEnvironment.TryGetValue("[Star's Sephiria Mod]HardMode", out int Value))
                    return Value;
            }
            return SaveManager.Current.GetInt("HardModeShard_[Star's Sephiria Mod]HardMode_Level", 0);
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

    private static readonly string ConfigFilePath = GetConfigPath();

    public static void Initialize()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                string json = File.ReadAllText(ConfigFilePath);
                Instance = JsonConvert.DeserializeObject<ModConfigData>(json) ?? new ModConfigData();
                HorayDebug.Log($"[配置] 加载成功：{ConfigFilePath}");
            }
            else
            {
                Instance = new ModConfigData();
                Save();
                HorayDebug.Log($"[配置] 首次运行，生成默认配置：{ConfigFilePath}");
            }
        }
        catch (Exception ex)
        {
            Instance = new ModConfigData();
            Save();
            HorayDebug.LogErrorDev($"[配置] 加载失败，已重置：{ex.Message}");
        }
    }

    public static void Save()
    {
        try
        {
            string json = JsonConvert.SerializeObject(Instance, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            HorayDebug.LogErrorDev($"[配置] 保存失败：{ex.Message}");
        }
    }

    private static string GetConfigPath()
    {
        string dllLocation = Assembly.GetExecutingAssembly().Location;
        string dllDirectory = Path.GetDirectoryName(dllLocation)!;
        return Path.Combine(dllDirectory, "config.json");
    }
}