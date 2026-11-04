using Mirror;
using SPMod.ModContent;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SPMod.Tool
{
    public struct ModCharmMessage : NetworkMessage
    {
        public uint CharmNetId;
        public byte Kind;
        public byte Event;
        public float Time;
        public int A;
        public int B;
        public Vector3 Pos;
        public string S1;
        public string S2;
    }

    public struct ModWeaponSwingFxMessage : NetworkMessage
    {
        public uint WeaponNetId;
        public byte Kind;
        public Vector3 Position;
        public Vector3 EulerAngles;
    }

    public struct ModWeaponFxMessage : NetworkMessage
    {
        public uint OwnerNetId;
        public byte Event;
        public Vector3 Position;
    }

    public struct ModWeaponHudMessage : NetworkMessage
    {
        public uint AvatarNetId;
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

    public struct ModBossTargetMessage : NetworkMessage
    {
        public uint BossNetId;
        public uint TargetNetId;
    }

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
        // WeaponFx 的 Event 与 AirValveZone.FxPulse(0)/FxTrigger(1) 共用编号，从 2 开始
        public const byte WeaponFxShadowThorns = 2;

        private static readonly Dictionary<byte, Func<NewWeaponFireData?>> SwingFxSources = [];
        private static readonly Dictionary<UnitAvatar, ModWeaponHudMessage> LastHud = [];
        private static readonly Dictionary<UnitAvatar, float> LastHudTime = [];

        static ModNetwork()
        {
            Writer<ModCharmMessage>.write = (writer, msg) =>
            {
                writer.WriteVarUInt(msg.CharmNetId);
                writer.WriteByte(msg.Kind);
                writer.WriteByte(msg.Event);
                writer.WriteFloat(msg.Time);
                writer.WriteVarInt(msg.A);
                writer.WriteVarInt(msg.B);
                writer.WriteVector3(msg.Pos);
                writer.WriteString(msg.S1 ?? string.Empty);
                writer.WriteString(msg.S2 ?? string.Empty);
            };
            Reader<ModCharmMessage>.read = reader => new ModCharmMessage
            {
                CharmNetId = reader.ReadVarUInt(),
                Kind = reader.ReadByte(),
                Event = reader.ReadByte(),
                Time = reader.ReadFloat(),
                A = reader.ReadVarInt(),
                B = reader.ReadVarInt(),
                Pos = reader.ReadVector3(),
                S1 = reader.ReadString(),
                S2 = reader.ReadString(),
            };

            Writer<ModWeaponSwingFxMessage>.write = (writer, msg) =>
            {
                writer.WriteVarUInt(msg.WeaponNetId);
                writer.WriteByte(msg.Kind);
                writer.WriteVector3(msg.Position);
                writer.WriteVector3(msg.EulerAngles);
            };
            Reader<ModWeaponSwingFxMessage>.read = reader => new ModWeaponSwingFxMessage
            {
                WeaponNetId = reader.ReadVarUInt(),
                Kind = reader.ReadByte(),
                Position = reader.ReadVector3(),
                EulerAngles = reader.ReadVector3(),
            };

            Writer<ModWeaponFxMessage>.write = (writer, msg) =>
            {
                writer.WriteVarUInt(msg.OwnerNetId);
                writer.WriteByte(msg.Event);
                writer.WriteVector3(msg.Position);
            };
            Reader<ModWeaponFxMessage>.read = reader => new ModWeaponFxMessage
            {
                OwnerNetId = reader.ReadVarUInt(),
                Event = reader.ReadByte(),
                Position = reader.ReadVector3(),
            };

            Writer<ModWeaponHudMessage>.write = (writer, msg) =>
            {
                writer.WriteVarUInt(msg.AvatarNetId);
                writer.WriteVarInt(msg.YuisMagic);
                writer.WriteVarInt(msg.HeidiHeat);
                writer.WriteVarInt(msg.MinimalismStack);
                writer.WriteVarInt(msg.EverfrostStack);
                writer.WriteVarInt(msg.RuadhsleaStacks);
                writer.WriteVarInt(msg.FrozenSmelt);
                writer.WriteVarInt(msg.LightningsWrath);
                writer.WriteFloat(msg.DraftbladeRemain);
                writer.WriteFloat(msg.DraftbladeTotal);
                writer.WriteFloat(msg.RuadhsleaRemain);
                writer.WriteFloat(msg.RuadhsleaTotal);
                writer.WriteFloat(msg.FiaolasRemain);
                writer.WriteFloat(msg.FiaolasTotal);
                writer.WriteFloat(msg.GlacialBladeRemain);
                writer.WriteFloat(msg.GlacialBladeTotal);
                writer.WriteFloat(msg.OtterRemain);
                writer.WriteFloat(msg.OtterTotal);
                writer.WriteFloat(msg.BrownRabbitRemain);
                writer.WriteFloat(msg.BrownRabbitTotal);
            };
            Reader<ModWeaponHudMessage>.read = reader => new ModWeaponHudMessage
            {
                AvatarNetId = reader.ReadVarUInt(),
                YuisMagic = reader.ReadVarInt(),
                HeidiHeat = reader.ReadVarInt(),
                MinimalismStack = reader.ReadVarInt(),
                EverfrostStack = reader.ReadVarInt(),
                RuadhsleaStacks = reader.ReadVarInt(),
                FrozenSmelt = reader.ReadVarInt(),
                LightningsWrath = reader.ReadVarInt(),
                DraftbladeRemain = reader.ReadFloat(),
                DraftbladeTotal = reader.ReadFloat(),
                RuadhsleaRemain = reader.ReadFloat(),
                RuadhsleaTotal = reader.ReadFloat(),
                FiaolasRemain = reader.ReadFloat(),
                FiaolasTotal = reader.ReadFloat(),
                GlacialBladeRemain = reader.ReadFloat(),
                GlacialBladeTotal = reader.ReadFloat(),
                OtterRemain = reader.ReadFloat(),
                OtterTotal = reader.ReadFloat(),
                BrownRabbitRemain = reader.ReadFloat(),
                BrownRabbitTotal = reader.ReadFloat(),
            };

            Writer<ModBossTargetMessage>.write = (writer, msg) =>
            {
                writer.WriteVarUInt(msg.BossNetId);
                writer.WriteVarUInt(msg.TargetNetId);
            };
            Reader<ModBossTargetMessage>.read = reader => new ModBossTargetMessage
            {
                BossNetId = reader.ReadVarUInt(),
                TargetNetId = reader.ReadVarUInt(),
            };
        }

        public static void RegisterClientHandlers()
        {
            NetworkClient.ReplaceHandler<ModCharmMessage>(OnModCharmMessage);
            NetworkClient.ReplaceHandler<ModWeaponSwingFxMessage>(OnModWeaponSwingFxMessage);
            NetworkClient.ReplaceHandler<ModWeaponFxMessage>(OnModWeaponFxMessage);
            NetworkClient.ReplaceHandler<ModWeaponHudMessage>(OnModWeaponHudMessage);
            NetworkClient.ReplaceHandler<ModBossTargetMessage>(OnModBossTargetMessage);
        }

        /// <summary>客机上的 Boss 当前目标：BossNetId → TargetNetId。0 表示没有目标。</summary>
        public static readonly Dictionary<uint, uint> BossTargetNetIds = [];

        private static void OnModBossTargetMessage(ModBossTargetMessage Msg)
        {
            if (NetworkServer.active || Msg.BossNetId == 0) return;
            if (Msg.TargetNetId == 0)
                BossTargetNetIds.Remove(Msg.BossNetId);
            else
                BossTargetNetIds[Msg.BossNetId] = Msg.TargetNetId;
        }

        /// <summary>服务端广播 Boss 当前目标；TargetNetId 为 0 表示丢失目标。</summary>
        public static void SendBossTarget(uint BossNetId, uint TargetNetId)
        {
            if (!NetworkServer.active || BossNetId == 0) return;
            NetworkServer.SendToAll(new ModBossTargetMessage { BossNetId = BossNetId, TargetNetId = TargetNetId });
        }

        /// <summary>服务端广播武器 FX；Event 区分 AirValve 的触发/脉冲和 ShadowThorns 等。</summary>
        public static void SendWeaponFx(uint OwnerNetId, Vector3 Position, byte Event)
        {
            if (!NetworkServer.active || OwnerNetId == 0) return;
            NetworkServer.SendToAll(new ModWeaponFxMessage
            {
                OwnerNetId = OwnerNetId,
                Event = Event,
                Position = Position,
            });
        }

        private static void OnModWeaponFxMessage(ModWeaponFxMessage Msg)
        {
            if (NetworkServer.active) return;

            string Key = Msg.Event switch
            {
                ModuleComponents.AirValveZone.FxTrigger => "AirValveGround",
                ModuleComponents.AirValveZone.FxPulse => "AirValve",
                WeaponFxShadowThorns => "ShadowThornsFx",
                _ => "",
            };
            if (Key.Length > 0)
                ModuleComponents.ModFxPool.Spawn(Key, Msg.Position, GetOwnerIndex(Msg.OwnerNetId));
        }

        private static int GetOwnerIndex(uint OwnerNetId)
        {
            return NetworkClient.spawned.TryGetValue(OwnerNetId, out NetworkIdentity Owner) && Owner != null && Owner.isOwned ? 1 : 0;
        }

        public static void SendWeaponHud(UnitAvatar Avatar)
        {
            if (!NetworkServer.active || Avatar == null) return;

            ModWeaponHudMessage Msg = new() { AvatarNetId = Avatar.netId };
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

            bool HasPrev = LastHud.TryGetValue(Avatar, out ModWeaponHudMessage Prev);
            bool Changed = !HasPrev || !SameHud(Msg, Prev);

            if (IsHudEmpty(Msg))
            {
                LastHud[Avatar] = Msg;
                LastHudTime.Remove(Avatar);
                if (Changed && HasPrev) NetworkServer.SendToAll(Msg);
                return;
            }

            // 非空快照每秒至少发一次，保证中途加入的客机能收到当前值
            bool Heartbeat = !LastHudTime.TryGetValue(Avatar, out float Last) || Time.time - Last >= 1f;
            if (!Changed && !Heartbeat) return;

            LastHud[Avatar] = Msg;
            LastHudTime[Avatar] = Time.time;
            NetworkServer.SendToAll(Msg);
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

        private static bool SameHud(in ModWeaponHudMessage A, in ModWeaponHudMessage B)
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

        private static bool IsHudEmpty(in ModWeaponHudMessage M)
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

        private static void OnModWeaponHudMessage(ModWeaponHudMessage Msg)
        {
            if (NetworkServer.active) return;
            if (!NetworkClient.spawned.TryGetValue(Msg.AvatarNetId, out NetworkIdentity Identity) || Identity == null) return;
            if (!Identity.TryGetComponent(out UnitAvatar Avatar) || Avatar == null) return;

            ApplyHudInt(WeaponPatch.YuisShortDaggerMagic, Avatar, Msg.YuisMagic);
            ApplyHudInt(WeaponPatch.HeidiDashAttack, Avatar, Msg.HeidiHeat);
            ApplyHudInt(WeaponPatch.MinimalismDashAttack, Avatar, Msg.MinimalismStack);
            ApplyHudInt(WeaponPatch.SilenceofEverfrostAttack, Avatar, Msg.EverfrostStack);
            ApplyHudInt(WeaponPatch.RuadhsleaSpearBuff, Avatar, Msg.RuadhsleaStacks);
            ApplyHudInt(WeaponPatch.FrozenSmeltEffect, Avatar, Msg.FrozenSmelt);
            ApplyHudInt(WeaponPatch.LightningsWrathEffect, Avatar, Msg.LightningsWrath);

            ApplyHudTimer(WeaponPatch.DraftbladeAttack, Avatar, Msg.DraftbladeRemain, Msg.DraftbladeTotal);
            ApplyHudTimer(WeaponPatch.RuadhsleaSpearTime, Avatar, Msg.RuadhsleaRemain, Msg.RuadhsleaTotal);
            ApplyHudTimer(WeaponPatch.FiaolasJudgmentStaffBuff, Avatar, Msg.FiaolasRemain, Msg.FiaolasTotal);
            ApplyHudTimer(WeaponPatch.GlacialBladeEffect, Avatar, Msg.GlacialBladeRemain, Msg.GlacialBladeTotal);
            ApplyHudTimer(StarSephiriaMod.Otter3_AddCrit, Avatar, Msg.OtterRemain, Msg.OtterTotal);
            ApplyHudTimer(StarSephiriaMod.BrownRabbit2Hit, Avatar.Name, Msg.BrownRabbitRemain, Msg.BrownRabbitTotal);
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

        private static void OnModWeaponSwingFxMessage(ModWeaponSwingFxMessage Msg)
        {
            // 主机客户端也会收到 SendToAll，但本机已经直接生成过了
            if (NetworkServer.active) return;
            if (!NetworkClient.spawned.TryGetValue(Msg.WeaponNetId, out NetworkIdentity Identity) || Identity == null) return;
            if (!Identity.TryGetComponent(out WeaponSimple Weapon) || Weapon == null) return;
            if (!SwingFxSources.TryGetValue(Msg.Kind, out Func<NewWeaponFireData?> Getter)) return;

            NewWeaponFireData? Data = Getter();
            if (Data == null) return;
            Weapon.CreateBasicAttackSwingFxFullyManual(Data, Msg.Position, Msg.EulerAngles);
        }

        private static void OnModCharmMessage(ModCharmMessage Msg)
        {
            // 不能在这里因为 NetworkServer.active 直接 return：SendEventToOwner 需要主机自己的客户端处理（如 Lottery 提示）。
            if (!NetworkClient.spawned.TryGetValue(Msg.CharmNetId, out NetworkIdentity Identity) || Identity == null) return;

            if (Identity.TryGetComponent(out Charm_Basic Charm))
            {
                switch (Msg.Kind)
                {
                    case KindCooldown:
                        (Charm as IModCooldownReceiver)?.OnClientCooldown(Msg.Time);
                        break;
                    case KindCastState:
                        (Charm as IModCastStateReceiver)?.OnClientCastState(Msg.Time, Msg.Event);
                        break;
                    case KindValues:
                        (Charm as IModValueReceiver)?.OnClientValues(Msg.A, Msg.B);
                        break;
                    case KindEvent:
                        (Charm as IModEventReceiver)?.OnClientEvent(Msg.Event, Msg.Pos, Msg.S1, Msg.S2);
                        break;
                }
                return;
            }

            // 非 Charm 的模组网络物体（如 CharacterDebuff）也可以接收事件
            if (Msg.Kind == KindEvent && Identity.TryGetComponent(out IModEventReceiver Receiver))
            {
                Receiver.OnClientEvent(Msg.Event, Msg.Pos, Msg.S1, Msg.S2);
                return;
            }

            if (Msg.Kind == KindWeaponTime && !NetworkServer.active && Identity.TryGetComponent(out UnitAvatar Avatar))
            {
                WeaponPatch.TomeOfMimicryMaxTime[Avatar] = Msg.Time;
                WeaponPatch.TomeOfMimicryEffect[Avatar] = Msg.B / 1000f;
            }
        }

        public static float ClientCooldown(float PhaseEnd, float Total)
        {
            if (PhaseEnd <= 0f) return 0f;
            return Mathf.Clamp01((PhaseEnd - (float)NetworkTime.time) / Total);
        }

        public static void SendCooldown(uint CharmNetId, float PhaseEnd)
        {
            Send(CharmNetId, new ModCharmMessage { Kind = KindCooldown, Time = PhaseEnd });
        }

        public static void SendCastState(uint CharmNetId, float CastTime, byte Event)
        {
            Send(CharmNetId, new ModCharmMessage { Kind = KindCastState, Time = CastTime, Event = Event });
        }

        public static void SendValues(uint CharmNetId, int A, int B = 0)
        {
            Send(CharmNetId, new ModCharmMessage { Kind = KindValues, A = A, B = B });
        }

        /// <summary>服务端向所有客户端广播一次事件；目标 NetId 可以是 Charm，也可以是实现了 IModEventReceiver 的其他模组网络物体。</summary>
        public static void SendEvent(uint CharmNetId, byte Event, Vector3 Pos, string S1 = "", string S2 = "")
        {
            Send(CharmNetId, new ModCharmMessage { Kind = KindEvent, Event = Event, Pos = Pos, S1 = S1, S2 = S2 });
        }

        /// <summary>服务端只向目标网络物体的拥有者发送一次事件。</summary>
        public static void SendEventToOwner(uint CharmNetId, byte Event, Vector3 Pos, string S1 = "", string S2 = "")
        {
            if (!NetworkServer.active || CharmNetId == 0) return;
            if (!NetworkServer.spawned.TryGetValue(CharmNetId, out NetworkIdentity Identity) || Identity == null) return;
            NetworkConnectionToClient? Conn = Identity.connectionToClient;
            if (Conn == null) return;
            Conn.Send(new ModCharmMessage { CharmNetId = CharmNetId, Kind = KindEvent, Event = Event, Pos = Pos, S1 = S1, S2 = S2 });
        }

        public static void SendWeaponTime(uint AvatarNetId, float Time, float Progress)
        {
            Send(AvatarNetId, new ModCharmMessage { Kind = KindWeaponTime, Time = Time, B = Mathf.RoundToInt(Progress * 1000f) });
        }

        /// <summary>
        /// 服务端广播一次刀光。WeaponNetId 必须是已 spawn 的武器，客户端靠它找到 WeaponSimple。
        /// 主机不会重复生成（handler 里跳过 NetworkServer.active）。
        /// </summary>
        public static void SendWeaponSwingFx(uint WeaponNetId, byte Kind, Vector3 Position, Vector3 EulerAngles)
        {
            if (!NetworkServer.active || WeaponNetId == 0) return;
            NetworkServer.SendToAll(new ModWeaponSwingFxMessage
            {
                WeaponNetId = WeaponNetId,
                Kind = Kind,
                Position = Position,
                EulerAngles = EulerAngles,
            });
        }

        private static void Send(uint CharmNetId, ModCharmMessage Msg)
        {
            if (!NetworkServer.active || CharmNetId == 0) return;
            Msg.CharmNetId = CharmNetId;
            NetworkServer.SendToAll(Msg);
        }
    }
}
