using Mirror;
using SPMod.ModContent;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SPMod.Tool
{
    public interface IModCooldownReceiver
    {
        void OnClientCooldown(float PhaseEnd);
    }

    public interface IModCastStateReceiver
    {
        void OnClientCastState(float CastTime, byte Event);
    }

    public interface IModValueReceiver
    {
        void OnClientValues(int A, int B);
    }

    public interface IModEventReceiver
    {
        void OnClientEvent(byte Event, Vector3 Pos, string S1, string S2);
    }

    public static class ModNetwork
    {
        public const byte KindCooldown = 0;
        public const byte KindCastState = 1;
        public const byte KindValues = 2;
        public const byte KindEvent = 3;
        public const byte KindWeaponTime = 4;
        public const byte SwingFxMoment = 0;
        public const byte WeaponFxShadowThorns = 2;
        public const byte WeaponFxWindForge = 3;

        private const string ModId = "stars-sephiriamod";
        private const string RpcCharm = ModId + ":charm";
        private const string RpcSwingFx = ModId + ":swingFx";
        private const string RpcWeaponFx = ModId + ":weaponFx";
        private const string RpcHud = ModId + ":hud";
        private const string RpcBossTarget = ModId + ":bossTarget";

        private struct HudState
        {
            public int YuisMagic;
            public int HeidiHeat;
            public int MinimalismStack;
            public int EverfrostStack;
            public int RuadhsleaStacks;
            public int FrozenSmelt;
            public int LightningsWrath;
            public float DraftbladeRemain;
            public float DraftbladeTotal;
            public float RuadhsleaRemain;
            public float RuadhsleaTotal;
            public float FiaolasRemain;
            public float FiaolasTotal;
            public float GlacialBladeRemain;
            public float GlacialBladeTotal;
            public float OtterRemain;
            public float OtterTotal;
            public float BrownRabbitRemain;
            public float BrownRabbitTotal;
        }

        private static readonly Dictionary<byte, Func<NewWeaponFireData?>> SwingFxSources = [];
        private static readonly Dictionary<UnitAvatar, HudState> LastHud = [];
        private static readonly Dictionary<UnitAvatar, float> LastHudTime = [];

        /// <summary>客机上的 Boss 当前目标：BossNetId → TargetNetId。0 表示没有目标。</summary>
        public static readonly Dictionary<uint, uint> BossTargetNetIds = [];

        /// <summary>
        /// 注册 ModRpc 处理函数。OnModLoaded 和客户端消息注册时都会调用；RegisterRpc 为覆盖式，重复调用无害。
        /// </summary>
        public static void RegisterHandlers()
        {
            ModRpc.RegisterRpc<NetworkBehaviour>(ModId, "charm", OnModCharmRpc);
            ModRpc.RegisterRpc<WeaponSimple>(ModId, "swingFx", OnModWeaponSwingFxRpc);
            ModRpc.RegisterRpc<UnitAvatar>(ModId, "weaponFx", OnModWeaponFxRpc);
            ModRpc.RegisterRpc<UnitAvatar>(ModId, "hud", OnModWeaponHudRpc);
            ModRpc.RegisterRpc<UnitAvatar>(ModId, "bossTarget", OnModBossTargetRpc);
        }

        public static void RegisterClientHandlers()
        {
            RegisterHandlers();
        }

        private static void OnModBossTargetRpc(UnitAvatar Boss, NetworkReader Reader)
        {
            if (NetworkServer.active) return;
            uint TargetNetId = Reader.ReadVarUInt();
            if (TargetNetId == 0)
                BossTargetNetIds.Remove(Boss.netId);
            else
                BossTargetNetIds[Boss.netId] = TargetNetId;
        }

        /// <summary>服务端广播 Boss 当前目标；TargetNetId 为 0 表示丢失目标。</summary>
        public static void SendBossTarget(uint BossNetId, uint TargetNetId)
        {
            UnitAvatar? Boss = Resolve<UnitAvatar>(BossNetId);
            if (Boss == null) return;
            ModRpc.Send(Boss, RpcBossTarget, Writer => Writer.WriteVarUInt(TargetNetId));
        }

        /// <summary>服务端广播武器 FX；Event 区分 AirValve 的触发/脉冲和 ShadowThorns 等。</summary>
        public static void SendWeaponFx(uint OwnerNetId, Vector3 Position, byte Event)
        {
            UnitAvatar? Owner = Resolve<UnitAvatar>(OwnerNetId);
            if (Owner == null) return;
            ModRpc.Send(Owner, RpcWeaponFx, Writer =>
            {
                Writer.WriteByte(Event);
                Writer.WriteVector3(Position);
            });
        }

        private static void OnModWeaponFxRpc(UnitAvatar Owner, NetworkReader Reader)
        {
            if (NetworkServer.active) return;

            byte Event = Reader.ReadByte();
            Vector3 Position = Reader.ReadVector3();
            string Key = Event switch
            {
                ModuleComponents.AirValveZone.FxTrigger => "AirValveGround",
                ModuleComponents.AirValveZone.FxPulse => "AirValve",
                WeaponFxShadowThorns => "ShadowThornsFx",
                WeaponFxWindForge => "WindForge",
                _ => "",
            };
            if (Key.Length > 0)
                ModuleComponents.ModFxPool.Spawn(Key, Position, Owner.isOwned ? 1 : 0);
        }

        public static void SendWeaponHud(UnitAvatar Avatar)
        {
            if (!NetworkServer.active || Avatar == null) return;

            HudState Msg = new();
            if (WeaponPatch.YuisShortDaggerMagic.TryGetValue(Avatar, out int YuisMagic)) Msg.YuisMagic = YuisMagic;
            if (WeaponPatch.HeidiDashAttack.TryGetValue(Avatar, out int HeidiHeat)) Msg.HeidiHeat = HeidiHeat;
            if (WeaponPatch.MinimalismDashAttack.TryGetValue(Avatar, out int Minimalism)) Msg.MinimalismStack = Minimalism;
            if (WeaponPatch.SilenceofEverfrostAttack.TryGetValue(Avatar, out int Everfrost)) Msg.EverfrostStack = Everfrost;
            if (WeaponPatch.RuadhsleaSpearBuff.TryGetValue(Avatar, out int RuadhsleaStacks)) Msg.RuadhsleaStacks = RuadhsleaStacks;
            if (WeaponPatch.FrozenSmeltEffect.TryGetValue(Avatar, out int FrozenSmelt)) Msg.FrozenSmelt = FrozenSmelt;
            if (WeaponPatch.LightningsWrathEffect.TryGetValue(Avatar, out int LightningsWrath)) Msg.LightningsWrath = LightningsWrath;
            if (WeaponPatch.DraftbladeAttack.TryGetValue(Avatar, out Timer Draftblade))
            {
                Msg.DraftbladeRemain = Draftblade.GetRemainingTime();
                Msg.DraftbladeTotal = Draftblade.time;
            }
            if (WeaponPatch.RuadhsleaSpearTime.TryGetValue(Avatar, out Timer Ruadhslea))
            {
                Msg.RuadhsleaRemain = Ruadhslea.GetRemainingTime();
                Msg.RuadhsleaTotal = Ruadhslea.time;
            }
            if (WeaponPatch.FiaolasJudgmentStaffBuff.TryGetValue(Avatar, out Timer Fiaolas))
            {
                Msg.FiaolasRemain = Fiaolas.GetRemainingTime();
                Msg.FiaolasTotal = Fiaolas.time;
            }
            if (WeaponPatch.GlacialBladeEffect.TryGetValue(Avatar, out Timer GlacialBlade))
            {
                Msg.GlacialBladeRemain = GlacialBlade.GetRemainingTime();
                Msg.GlacialBladeTotal = GlacialBlade.time;
            }
            if (StarSephiriaMod.Otter3_AddCrit.TryGetValue(Avatar, out Timer Otter))
            {
                Msg.OtterRemain = Otter.GetRemainingTime();
                Msg.OtterTotal = Otter.time;
            }
            if (StarSephiriaMod.BrownRabbit2Hit.TryGetValue(Avatar.Name, out Timer BrownRabbit))
            {
                Msg.BrownRabbitRemain = BrownRabbit.GetRemainingTime();
                Msg.BrownRabbitTotal = BrownRabbit.time;
            }

            bool HasPrev = LastHud.TryGetValue(Avatar, out HudState Prev);
            bool Changed = !HasPrev || !SameHud(Msg, Prev);

            if (IsHudEmpty(Msg))
            {
                LastHud[Avatar] = Msg;
                LastHudTime.Remove(Avatar);
                if (Changed && HasPrev) SendHud(Avatar, Msg);
                return;
            }

            // 非空快照每秒至少发一次，保证中途加入的客机能收到当前值
            bool Heartbeat = !LastHudTime.TryGetValue(Avatar, out float Last) || Time.time - Last >= 1f;
            if (!Changed && !Heartbeat) return;

            LastHud[Avatar] = Msg;
            LastHudTime[Avatar] = Time.time;
            SendHud(Avatar, Msg);
        }

        private static void SendHud(UnitAvatar Avatar, HudState Msg)
        {
            ModRpc.Send(Avatar, RpcHud, Writer =>
            {
                Writer.WriteVarInt(Msg.YuisMagic);
                Writer.WriteVarInt(Msg.HeidiHeat);
                Writer.WriteVarInt(Msg.MinimalismStack);
                Writer.WriteVarInt(Msg.EverfrostStack);
                Writer.WriteVarInt(Msg.RuadhsleaStacks);
                Writer.WriteVarInt(Msg.FrozenSmelt);
                Writer.WriteVarInt(Msg.LightningsWrath);
                Writer.WriteFloat(Msg.DraftbladeRemain);
                Writer.WriteFloat(Msg.DraftbladeTotal);
                Writer.WriteFloat(Msg.RuadhsleaRemain);
                Writer.WriteFloat(Msg.RuadhsleaTotal);
                Writer.WriteFloat(Msg.FiaolasRemain);
                Writer.WriteFloat(Msg.FiaolasTotal);
                Writer.WriteFloat(Msg.GlacialBladeRemain);
                Writer.WriteFloat(Msg.GlacialBladeTotal);
                Writer.WriteFloat(Msg.OtterRemain);
                Writer.WriteFloat(Msg.OtterTotal);
                Writer.WriteFloat(Msg.BrownRabbitRemain);
                Writer.WriteFloat(Msg.BrownRabbitTotal);
            });
        }

        /// <summary>玩家离开/销毁时清掉 HUD 缓存，避免字典残留。</summary>
        public static void RemoveHudCache(UnitAvatar Avatar)
        {
            if (Avatar == null) return;
            LastHud.Remove(Avatar);
            LastHudTime.Remove(Avatar);
        }

        public static void ResetHudCache()
        {
            LastHud.Clear();
            LastHudTime.Clear();
        }

        private static bool SameHud(in HudState A, in HudState B)
        {
            return A.YuisMagic == B.YuisMagic
                && A.HeidiHeat == B.HeidiHeat
                && A.MinimalismStack == B.MinimalismStack
                && A.EverfrostStack == B.EverfrostStack
                && A.RuadhsleaStacks == B.RuadhsleaStacks
                && A.FrozenSmelt == B.FrozenSmelt
                && A.LightningsWrath == B.LightningsWrath
                && A.DraftbladeRemain == B.DraftbladeRemain
                && A.DraftbladeTotal == B.DraftbladeTotal
                && A.RuadhsleaRemain == B.RuadhsleaRemain
                && A.RuadhsleaTotal == B.RuadhsleaTotal
                && A.FiaolasRemain == B.FiaolasRemain
                && A.FiaolasTotal == B.FiaolasTotal
                && A.GlacialBladeRemain == B.GlacialBladeRemain
                && A.GlacialBladeTotal == B.GlacialBladeTotal
                && A.OtterRemain == B.OtterRemain
                && A.OtterTotal == B.OtterTotal
                && A.BrownRabbitRemain == B.BrownRabbitRemain
                && A.BrownRabbitTotal == B.BrownRabbitTotal;
        }

        private static bool IsHudEmpty(in HudState M)
        {
            return M.YuisMagic == 0
                && M.HeidiHeat == 0
                && M.MinimalismStack == 0
                && M.EverfrostStack == 0
                && M.RuadhsleaStacks == 0
                && M.FrozenSmelt == 0
                && M.LightningsWrath == 0
                && M.DraftbladeRemain == 0f
                && M.RuadhsleaRemain == 0f
                && M.FiaolasRemain == 0f
                && M.GlacialBladeRemain == 0f
                && M.OtterRemain == 0f
                && M.BrownRabbitRemain == 0f;
        }

        private static void OnModWeaponHudRpc(UnitAvatar Avatar, NetworkReader Reader)
        {
            if (NetworkServer.active) return;

            int YuisMagic = Reader.ReadVarInt();
            int HeidiHeat = Reader.ReadVarInt();
            int MinimalismStack = Reader.ReadVarInt();
            int EverfrostStack = Reader.ReadVarInt();
            int RuadhsleaStacks = Reader.ReadVarInt();
            int FrozenSmelt = Reader.ReadVarInt();
            int LightningsWrath = Reader.ReadVarInt();
            float DraftbladeRemain = Reader.ReadFloat();
            float DraftbladeTotal = Reader.ReadFloat();
            float RuadhsleaRemain = Reader.ReadFloat();
            float RuadhsleaTotal = Reader.ReadFloat();
            float FiaolasRemain = Reader.ReadFloat();
            float FiaolasTotal = Reader.ReadFloat();
            float GlacialBladeRemain = Reader.ReadFloat();
            float GlacialBladeTotal = Reader.ReadFloat();
            float OtterRemain = Reader.ReadFloat();
            float OtterTotal = Reader.ReadFloat();
            float BrownRabbitRemain = Reader.ReadFloat();
            float BrownRabbitTotal = Reader.ReadFloat();

            ApplyHudInt(WeaponPatch.YuisShortDaggerMagic, Avatar, YuisMagic);
            ApplyHudInt(WeaponPatch.HeidiDashAttack, Avatar, HeidiHeat);
            ApplyHudInt(WeaponPatch.MinimalismDashAttack, Avatar, MinimalismStack);
            ApplyHudInt(WeaponPatch.SilenceofEverfrostAttack, Avatar, EverfrostStack);
            ApplyHudInt(WeaponPatch.RuadhsleaSpearBuff, Avatar, RuadhsleaStacks);
            ApplyHudInt(WeaponPatch.FrozenSmeltEffect, Avatar, FrozenSmelt);
            ApplyHudInt(WeaponPatch.LightningsWrathEffect, Avatar, LightningsWrath);

            ApplyHudTimer(WeaponPatch.DraftbladeAttack, Avatar, DraftbladeRemain, DraftbladeTotal);
            ApplyHudTimer(WeaponPatch.RuadhsleaSpearTime, Avatar, RuadhsleaRemain, RuadhsleaTotal);
            ApplyHudTimer(WeaponPatch.FiaolasJudgmentStaffBuff, Avatar, FiaolasRemain, FiaolasTotal);
            ApplyHudTimer(WeaponPatch.GlacialBladeEffect, Avatar, GlacialBladeRemain, GlacialBladeTotal);
            ApplyHudTimer(StarSephiriaMod.Otter3_AddCrit, Avatar, OtterRemain, OtterTotal);
            ApplyHudTimer(StarSephiriaMod.BrownRabbit2Hit, Avatar.Name, BrownRabbitRemain, BrownRabbitTotal);
        }

        private static void ApplyHudInt(Dictionary<UnitAvatar, int> Target, UnitAvatar Avatar, int Value)
        {
            if (Value > 0)
            {
                if (!Target.TryGetValue(Avatar, out int Old) || Old != Value)
                    Target[Avatar] = Value;
            }
            else Target.Remove(Avatar);
        }

        private static void ApplyHudTimer(Dictionary<UnitAvatar, Timer> Target, UnitAvatar Avatar, float Remain, float Total)
        {
            if (Remain <= 0f || Total <= 0f)
            {
                Target.Remove(Avatar);
                return;
            }
            if (!Target.TryGetValue(Avatar, out Timer T) || T == null)
                Target[Avatar] = T = new Timer(Total, false);
            T.time = Total;
            T.SetTimer(Total - Remain);
        }

        private static void ApplyHudTimer(Dictionary<string, Timer> Target, string Name, float Remain, float Total)
        {
            if (Remain <= 0f || Total <= 0f)
            {
                Target.Remove(Name);
                return;
            }
            if (!Target.TryGetValue(Name, out Timer T) || T == null)
                Target[Name] = T = new Timer(Total, false);
            T.time = Total;
            T.SetTimer(Total - Remain);
        }

        /// <summary>
        /// 注册“按 Kind 生成刀光”所用的 ModFireData。每个客户端都要注册，服务端发送后才能解析。
        /// </summary>
        public static void RegisterSwingFx(byte Kind, Func<NewWeaponFireData?> Getter)
        {
            if (Getter == null) return;
            SwingFxSources[Kind] = Getter;
        }

        private static void OnModWeaponSwingFxRpc(WeaponSimple Weapon, NetworkReader Reader)
        {
            byte Kind = Reader.ReadByte();
            Vector3 Position = Reader.ReadVector3();
            Vector3 EulerAngles = Reader.ReadVector3();

            // 主机客户端也会收到，但本机已经直接生成过了
            if (NetworkServer.active) return;
            if (!SwingFxSources.TryGetValue(Kind, out Func<NewWeaponFireData?> Getter)) return;

            NewWeaponFireData? Data = Getter();
            if (Data == null) return;
            Weapon.CreateBasicAttackSwingFxFullyManual(Data, Position, EulerAngles);
        }

        private static void WriteCharmPayload(NetworkWriter Writer, byte Kind, byte Event, float Time, int A, int B, Vector3 Pos, string S1, string S2)
        {
            Writer.WriteByte(Kind);
            Writer.WriteByte(Event);
            Writer.WriteFloat(Time);
            Writer.WriteVarInt(A);
            Writer.WriteVarInt(B);
            Writer.WriteVector3(Pos);
            Writer.WriteString(S1 ?? string.Empty);
            Writer.WriteString(S2 ?? string.Empty);
        }

        private static void OnModCharmRpc(NetworkBehaviour Behaviour, NetworkReader Reader)
        {
            byte Kind = Reader.ReadByte();
            byte Event = Reader.ReadByte();
            float Time = Reader.ReadFloat();
            int A = Reader.ReadVarInt();
            int B = Reader.ReadVarInt();
            Vector3 Pos = Reader.ReadVector3();
            string S1 = Reader.ReadString();
            string S2 = Reader.ReadString();

            // 不能在这里因为 NetworkServer.active 直接 return：SendEventToOwner 需要主机自己的客户端处理（如 Lottery 提示）。
            if (Behaviour is Charm_Basic Charm)
            {
                switch (Kind)
                {
                    case KindCooldown:
                        (Charm as IModCooldownReceiver)?.OnClientCooldown(Time);
                        break;
                    case KindCastState:
                        (Charm as IModCastStateReceiver)?.OnClientCastState(Time, Event);
                        break;
                    case KindValues:
                        (Charm as IModValueReceiver)?.OnClientValues(A, B);
                        break;
                    case KindEvent:
                        (Charm as IModEventReceiver)?.OnClientEvent(Event, Pos, S1, S2);
                        break;
                }
                return;
            }

            // 非 Charm 的模组网络物体（如 CharacterDebuff、ActiveSkill）也可以接收事件
            if (Kind == KindEvent && Behaviour is IModEventReceiver Receiver)
            {
                Receiver.OnClientEvent(Event, Pos, S1, S2);
                return;
            }

            if (Kind == KindWeaponTime && !NetworkServer.active && Behaviour is UnitAvatar Avatar)
            {
                WeaponPatch.TomeOfMimicryMaxTime[Avatar] = Time;
                WeaponPatch.TomeOfMimicryEffect[Avatar] = B / 1000f;
            }
        }

        /// <summary>服务端按 netId 找到目标组件；KindWeaponTime 的目标是 UnitAvatar，其余优先找 Charm，再找事件接收器。</summary>
        private static NetworkBehaviour? Resolve(uint NetId, byte Kind)
        {
            if (Kind == KindWeaponTime) return Resolve<UnitAvatar>(NetId);

            if (!NetworkServer.active || NetId == 0) return null;
            if (!NetworkServer.spawned.TryGetValue(NetId, out NetworkIdentity Identity) || Identity == null) return null;
            if (Identity.TryGetComponent(out Charm_Basic Charm)) return Charm;
            foreach (NetworkBehaviour Behaviour in Identity.GetComponents<NetworkBehaviour>())
            {
                if (Behaviour is IModEventReceiver) return Behaviour;
            }
            return null;
        }

        private static T? Resolve<T>(uint NetId) where T : NetworkBehaviour
        {
            if (!NetworkServer.active || NetId == 0) return null;
            if (!NetworkServer.spawned.TryGetValue(NetId, out NetworkIdentity Identity) || Identity == null) return null;
            return Identity.TryGetComponent(out T Target) ? Target : null;
        }

        private static void SendCharm(uint NetId, byte Kind, byte Event = 0, float Time = 0f, int A = 0, int B = 0, Vector3 Pos = default, string S1 = "", string S2 = "")
        {
            NetworkBehaviour? Target = Resolve(NetId, Kind);
            if (Target == null) return;
            ModRpc.Send(Target, RpcCharm, Writer => WriteCharmPayload(Writer, Kind, Event, Time, A, B, Pos, S1, S2));
        }

        public static float ClientCooldown(float PhaseEnd, float Total)
        {
            if (PhaseEnd <= 0f) return 0f;
            return Mathf.Clamp01((PhaseEnd - (float)NetworkTime.time) / Total);
        }

        public static void SendCooldown(uint CharmNetId, float PhaseEnd)
        {
            SendCharm(CharmNetId, KindCooldown, Time: PhaseEnd);
        }

        public static void SendCastState(uint CharmNetId, float CastTime, byte Event)
        {
            SendCharm(CharmNetId, KindCastState, Event, CastTime);
        }

        public static void SendValues(uint CharmNetId, int A, int B = 0)
        {
            SendCharm(CharmNetId, KindValues, A: A, B: B);
        }

        /// <summary>服务端向所有客户端广播一次事件；目标 NetId 可以是 Charm，也可以是实现了 IModEventReceiver 的其他模组网络物体。</summary>
        public static void SendEvent(uint CharmNetId, byte Event, Vector3 Pos, string S1 = "", string S2 = "")
        {
            SendCharm(CharmNetId, KindEvent, Event, Pos: Pos, S1: S1, S2: S2);
        }

        /// <summary>服务端只向目标网络物体的拥有者发送一次事件。</summary>
        public static void SendEventToOwner(uint CharmNetId, byte Event, Vector3 Pos, string S1 = "", string S2 = "")
        {
            if (!NetworkServer.active || CharmNetId == 0) return;
            if (!NetworkServer.spawned.TryGetValue(CharmNetId, out NetworkIdentity Identity) || Identity == null) return;
            if (!Identity.TryGetComponent(out Charm_Basic Charm)) return;
            NetworkConnectionToClient? Conn = Charm.connectionToClient;
            if (Conn == null) return;
            ModRpc.SendTo(Conn, Charm, RpcCharm, Writer => WriteCharmPayload(Writer, KindEvent, Event, 0f, 0, 0, Pos, S1, S2));
        }

        public static void SendWeaponTime(uint AvatarNetId, float Time, float Progress)
        {
            SendCharm(AvatarNetId, KindWeaponTime, Time: Time, B: Mathf.RoundToInt(Progress * 1000f));
        }

        /// <summary>
        /// 服务端广播一次刀光。WeaponNetId 必须是已 spawn 的武器；主机不会重复生成（handler 里跳过 NetworkServer.active）。
        /// </summary>
        public static void SendWeaponSwingFx(uint WeaponNetId, byte Kind, Vector3 Position, Vector3 EulerAngles)
        {
            WeaponSimple? Weapon = Resolve<WeaponSimple>(WeaponNetId);
            if (Weapon == null) return;
            ModRpc.Send(Weapon, RpcSwingFx, Writer =>
            {
                Writer.WriteByte(Kind);
                Writer.WriteVector3(Position);
                Writer.WriteVector3(EulerAngles);
            });
        }
    }
}
