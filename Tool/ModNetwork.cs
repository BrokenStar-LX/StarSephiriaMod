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

    public struct ModAirValveMessage : NetworkMessage
    {
        public uint WeaponNetId;
        public uint OwnerNetId;
        public byte Event;
        public Vector3 Position;
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

        private static readonly Dictionary<byte, Func<NewWeaponFireData?>> SwingFxSources = new Dictionary<byte, Func<NewWeaponFireData?>>();

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

            Writer<ModAirValveMessage>.write = (writer, msg) =>
            {
                writer.WriteVarUInt(msg.WeaponNetId);
                writer.WriteVarUInt(msg.OwnerNetId);
                writer.WriteByte(msg.Event);
                writer.WriteVector3(msg.Position);
            };
            Reader<ModAirValveMessage>.read = reader => new ModAirValveMessage
            {
                WeaponNetId = reader.ReadVarUInt(),
                OwnerNetId = reader.ReadVarUInt(),
                Event = reader.ReadByte(),
                Position = reader.ReadVector3(),
            };
        }

        public static void RegisterClientHandlers()
        {
            NetworkClient.ReplaceHandler<ModCharmMessage>(OnModCharmMessage);
            NetworkClient.ReplaceHandler<ModWeaponSwingFxMessage>(OnModWeaponSwingFxMessage);
            NetworkClient.ReplaceHandler<ModAirValveMessage>(OnModAirValveMessage);
        }

        /// <summary>服务端广播气阀领域的 FX；Event 区分触发瞬间（AirValveGround）和每跳脉冲（AirValve）。</summary>
        public static void SendAirValveFx(uint WeaponNetId, uint OwnerNetId, Vector3 Position, byte Event = ModuleComponents.AirValveZone.FxPulse)
        {
            if (!NetworkServer.active || WeaponNetId == 0) return;
            NetworkServer.SendToAll(new ModAirValveMessage { WeaponNetId = WeaponNetId, OwnerNetId = OwnerNetId, Event = Event, Position = Position });
        }

        private static void OnModAirValveMessage(ModAirValveMessage Msg)
        {
            if (NetworkServer.active) return;

            int OwnerIndex = 0;
            if (NetworkClient.spawned.TryGetValue(Msg.OwnerNetId, out NetworkIdentity Owner) && Owner != null && Owner.isOwned)
                OwnerIndex = 1;

            string Key = Msg.Event == ModuleComponents.AirValveZone.FxTrigger ? "AirValveGround" : "AirValve";
            ModuleComponents.ModFxPool.Spawn(Key, Msg.Position, OwnerIndex);
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
