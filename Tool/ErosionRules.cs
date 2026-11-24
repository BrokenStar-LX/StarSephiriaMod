using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace SPMod.Tool;

[HarmonyPatch]
internal static class ErosionRules
{
    internal const int Off = 1;
    internal const int On = 2;
    internal const int ClearOnExit = 3;
    private const int LegacyFollow = 4;
    internal const int LegacyOff = 5;
    private const int LegacyOn = 6;
    private const int NewRun = 1;
    private const int MigratedOff = 2;
    private const int MigratedOn = 3;
    private const string RunKey = "[Star's Sephiria Mod]RosionRunRules";
    internal const string HardModeKey = "[Star's Sephiria Mod]HardMode";
    private static bool capturesNewRuns;

    internal static bool IsLegacy(int mode) => mode is LegacyFollow or LegacyOff or LegacyOn;

    internal static bool IsEnabled(int mode) => mode switch
    {
        On or ClearOnExit or LegacyOn => true,
        LegacyFollow => ModConfig.HardMode >= 3,
        _ => false,
    };

    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.CreateNewTMP))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> MarkNewRun(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        MethodInfo create = AccessTools.Method(typeof(SaveData), nameof(SaveData.CreateNew), [typeof(string)]);
        var sites = code.Where(instruction => instruction.opcode == OpCodes.Callvirt &&
            Equals(instruction.operand, create)).ToArray();
        capturesNewRuns = sites.Length == 1;
        if (capturesNewRuns)
        {
            sites[0].opcode = OpCodes.Call;
            sites[0].operand = AccessTools.Method(typeof(ErosionRules), nameof(CreateMarkedSave));
        }
        return code;
    }

    private static void CreateMarkedSave(SaveData run, string fileName)
    {
        // CreateNew writes the file before SaveManager publishes the run object.
        run.SetInt(RunKey, NewRun);
        run.CreateNew(fileName);
    }

    internal static int ResolveForRun(SaveData? run, int selected, int hardship)
    {
        if (run == null || !capturesNewRuns)
            return selected switch { Off => LegacyOff, On or ClearOnExit => LegacyOn, _ => LegacyFollow };

        int saved = run.GetInt(RunKey, 0);
        if (saved == 0)
        {
            // Preserve a legacy run's enabled state once. Future activation must not
            // derive it again from the profile's Hard Mode selection.
            bool wasEnabled = selected switch
            {
                Off => false,
                On or ClearOnExit => true,
                _ => hardship >= 3,
            };
            saved = wasEnabled ? MigratedOn : MigratedOff;
            run.SetInt(RunKey, saved);
        }

        return selected switch
        {
            Off or On or ClearOnExit => selected,
            _ => saved == MigratedOn ? On : Off,
        };
    }
}
