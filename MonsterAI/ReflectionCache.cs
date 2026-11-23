using HarmonyLib;
using System;
using System.Reflection;

namespace SPMod.MonsterAI
{
    /// <summary>
    /// Boss 反射句柄的缓存工厂：一次性生成 FieldRef / MethodDelegate / FastInvoke，
    /// 避免每帧 FieldInfo.GetValue 的反射与装箱开销。
    /// </summary>
    internal static class ReflectionCache
    {
        public static AccessTools.FieldRef<T, F>? FieldRef<T, F>(Type Type, string Name)
        {
            FieldInfo? Field = AccessTools.Field(Type, Name);
            if (Field == null) return null;
            try
            {
                return AccessTools.FieldRefAccess<T, F>(Field);
            }
            catch (Exception ex)
            {
                StarSephiriaMod.LogError($"[ReflectionCache] {Type.Name}.{Name} 字段访问器创建失败: {ex}");
                return null;
            }
        }

        public static AccessTools.FieldRef<F>? StaticFieldRef<F>(Type Type, string Name)
        {
            FieldInfo? Field = AccessTools.Field(Type, Name);
            if (Field == null) return null;
            try
            {
                return AccessTools.StaticFieldRefAccess<F>(Field);
            }
            catch (Exception ex)
            {
                StarSephiriaMod.LogError($"[ReflectionCache] {Type.Name}.{Name} 静态字段访问器创建失败: {ex}");
                return null;
            }
        }

        public static TDelegate? Delegate<TDelegate>(MethodInfo? Method, string Name = "") where TDelegate : Delegate
        {
            if (Method == null) return null;
            try
            {
                // 不依赖 AccessTools.MethodDelegate，兼容游戏自带 Harmony 与 BepInEx 的 HarmonyX
                return (TDelegate)Method.CreateDelegate(typeof(TDelegate));
            }
            catch (Exception ex)
            {
                StarSephiriaMod.LogError($"[ReflectionCache] {Name} 委托创建失败: {ex}");
                return null;
            }
        }

        public static FastInvokeHandler? FastInvoke(MethodInfo? Method, string Name = "")
        {
            if (Method == null) return null;
            try
            {
                return MethodInvoker.GetHandler(Method);
            }
            catch (Exception ex)
            {
                StarSephiriaMod.LogError($"[ReflectionCache] {Name} FastInvoke 创建失败: {ex}");
                return null;
            }
        }
    }
}
