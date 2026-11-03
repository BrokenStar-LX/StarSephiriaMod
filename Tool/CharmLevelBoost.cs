using Mirror;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SPMod.Tool
{
    /// <summary>
    /// 把目标神器的最大等级翻倍，并按原成长趋势延长所有按等级取值的数组。
    /// 只修改运行时组件实例；Restore 会恢复原数组引用和原 maxLevel。
    /// </summary>
    public static class CharmLevelBoost
    {
        private sealed class Backup
        {
            public int OriginalMaxLevel;
            public readonly List<(FieldInfo Field, Array Original)> Arrays = [];
            public readonly List<(Charm_StatusInstance.StatusGroup Group, int[] Original)> StatValues = [];
        }

        // 原版攻击神器中不带 ByLevel、但按等级取值的数组
        private static readonly Dictionary<Type, string[]> ExtraArrays = new()
        {
            { typeof(Charm_DashDamage), ["physicalDashDamage"] },
        };

        private static readonly Dictionary<Charm_Basic, Backup> Applied = [];
        private static readonly MethodInfo? OnUpdatedLevelMethod = typeof(Charm_Basic).GetMethod("OnUpdatedLevel", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool IsApplied(Charm_Basic Charm)
        {
            return Charm is not null && Applied.ContainsKey(Charm);
        }

        public static void Apply(Charm_Basic Charm)
        {
            if (Charm == null || Applied.ContainsKey(Charm)) return;

            int OriginalMaxLevel = Charm.maxLevel;
            if (OriginalMaxLevel <= 0) return;

            int NewLength = OriginalMaxLevel * 2 + 1;
            Backup Data = new() { OriginalMaxLevel = OriginalMaxLevel };

            if (Charm is Charm_StatusInstance Status && Status.stats != null)
            {
                foreach (Charm_StatusInstance.StatusGroup Group in Status.stats)
                {
                    if (Group == null || Group.valuesByLevel == null) continue;
                    int[]? NewValues = ExtendInt(Group.valuesByLevel, NewLength);
                    if (NewValues == null) continue;
                    Data.StatValues.Add((Group, Group.valuesByLevel));
                    Group.valuesByLevel = NewValues;
                }
            }

            HashSet<string> ExtraNames = [];
            foreach (KeyValuePair<Type, string[]> Entry in ExtraArrays)
            {
                if (Entry.Key.IsAssignableFrom(Charm.GetType()))
                    ExtraNames.UnionWith(Entry.Value);
            }

            foreach (FieldInfo Field in Charm.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (Field.IsInitOnly || Field.IsLiteral) continue;

                bool ByLevel = Field.Name.Contains("ByLevel", StringComparison.OrdinalIgnoreCase);
                if (!ByLevel && !ExtraNames.Contains(Field.Name)) continue;

                if (Field.FieldType == typeof(int[]))
                {
                    if (Field.GetValue(Charm) is not int[] Old) continue;
                    int[]? NewArray = ExtendInt(Old, NewLength);
                    if (NewArray == null) continue;
                    Data.Arrays.Add((Field, Old));
                    Field.SetValue(Charm, NewArray);
                }
                else if (Field.FieldType == typeof(float[]))
                {
                    if (Field.GetValue(Charm) is not float[] Old) continue;
                    float[]? NewArray = ExtendFloat(Old, NewLength);
                    if (NewArray == null) continue;
                    Data.Arrays.Add((Field, Old));
                    Field.SetValue(Charm, NewArray);
                }
            }

            Charm.maxLevel = OriginalMaxLevel * 2;
            Applied[Charm] = Data;
        }

        public static void Restore(Charm_Basic Charm)
        {
            if (Charm is null || !Applied.TryGetValue(Charm, out Backup Data)) return;

            if (Charm != null) // Unity 对象可能已经销毁
            {
                foreach ((FieldInfo Field, Array Original) in Data.Arrays)
                {
                    try
                    {
                        Field.SetValue(Charm, Original);
                    }
                    catch (Exception Ex)
                    {
                        Debug.LogError($"[CharmLevelBoost] 恢复 {Field.Name} 失败: {Ex.Message}");
                    }
                }
                foreach ((Charm_StatusInstance.StatusGroup Group, int[] Original) in Data.StatValues)
                {
                    if (Group != null) Group.valuesByLevel = Original;
                }
                Charm.maxLevel = Data.OriginalMaxLevel;
            }

            Applied.Remove(Charm!);
        }

        public static void RestoreAll()
        {
            foreach (Charm_Basic Charm in new List<Charm_Basic>(Applied.Keys))
                Restore(Charm);
        }

        /// <summary>
        /// 让目标按当前的 maxLevel / DisplayedLevel 重新计算生效等级。
        /// Apply / Restore 改了 maxLevel 后，EnableEffect 不会自动重算 limitedEffectEnabledLevel，
        /// 这里手动走一遍目标自己的 OnUpdatedLevel，避免效果停在旧等级。
        /// </summary>
        public static void RefreshEffectLevel(Charm_Basic Charm)
        {
            if (!NetworkServer.active || Charm == null || !Charm.IsEffectEnabled) return;

            int OldLevel = Charm.limitedEffectEnabledLevel;
            int NewLevel = Mathf.Min(Charm.maxLevel, Charm.DisplayedLevel);
            if (OldLevel == NewLevel) return;

            Charm.NetworklimitedEffectEnabledLevel = NewLevel;

            if (OnUpdatedLevelMethod == null) return;

            try
            {
                OnUpdatedLevelMethod.Invoke(Charm, [OldLevel, NewLevel]);
            }
            catch (Exception Ex)
            {
                Debug.LogError($"[CharmLevelBoost] OnUpdatedLevel 调用失败: {Ex.Message}");
            }
        }

        // 按整体平均差值向后延伸。
        // 平均差值为 0 或负数时不再增长，只把数组补长到新上限（保持最后一级的值），
        // 这样直接下标访问的类也不会越界，冷却类数值也不会继续往下掉。
        private static int[]? ExtendInt(int[] Old, int NewLength)
        {
            if (Old.Length == 0 || Old.Length >= NewLength) return null;

            float Step = Old.Length >= 2 ? (Old[^1] - Old[0]) / (float)(Old.Length - 1) : 0f;
            int[] Result = new int[NewLength];
            Array.Copy(Old, Result, Old.Length);

            if (Step <= 0f)
            {
                for (int i = Old.Length; i < NewLength; i++) Result[i] = Old[^1];
                return Result;
            }

            for (int i = Old.Length; i < NewLength; i++)
                Result[i] = Mathf.RoundToInt(Old[^1] + Step * (i - (Old.Length - 1)));
            return Result;
        }

        private static float[]? ExtendFloat(float[] Old, int NewLength)
        {
            if (Old.Length == 0 || Old.Length >= NewLength) return null;

            float Step = Old.Length >= 2 ? (Old[^1] - Old[0]) / (Old.Length - 1) : 0f;
            float[] Result = new float[NewLength];
            Array.Copy(Old, Result, Old.Length);

            if (Step <= 0f)
            {
                for (int i = Old.Length; i < NewLength; i++) Result[i] = Old[^1];
                return Result;
            }

            for (int i = Old.Length; i < NewLength; i++)
                Result[i] = Old[^1] + Step * (i - (Old.Length - 1));
            return Result;
        }
    }
}
