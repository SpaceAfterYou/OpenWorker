using Core.Systems.NetSystem.Opcodes;
using Core.Systems.NetSystem.Packets;
using Core.Systems.NetSystem.Requests.Character;
using Core.Systems.NetSystem.Requests.Gesture;
using Core.Systems.NetSystem.Requests.Movement;
using DistrictService.Systems.GameSystem.Items;
using DistrictService.Systems.NetSystem;
using SoulWorker;
using SoulWorker.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DistrictService.Systems.GameSystem
{
    public sealed class Channel
    {
        public ushort Id { get; }
        public IReadOnlyDictionary<Guid, Session> Sessions => InternalSessions;

        public ChannelLoadStatus Status => InternalSessions.Count switch
        {
            > 64 => ChannelLoadStatus.Full,
            > 48 => ChannelLoadStatus.High,
            > 32 => ChannelLoadStatus.Medium,
            _ => ChannelLoadStatus.Low
        };

        public bool TryJoin(Session session)
        {
            if (InternalSessions.Count >= Core.Constants.MaxChannelSessions || !InternalSessions.TryAdd(session.Id, session))
            {
                return false;
            }

            session.Channel = this;
            BroadcastCharacterIn(session);

            return true;
        }

        public void Leave(Session session)
        {
            InternalSessions.Remove(session.Id);
            BroadcastCharacterOut(session);
        }

        public Channel(ushort id) => Id = id;

        #region Brodcast Channel

        public void BroadcastChatMessage(Session session, ChatType type, string message)
        {
            using WriterPacket writer = new(ClientOpcode.ChatMessage);

            writer.Write(session.Character.Id);
            writer.Write(type);
            writer.WriteByteLengthUnicodeString(message);

            BrodcastAsync(writer);
        }

        #endregion Brodcast Channel

        #region Brodcast Character

        public void BrodcastCharacterSetLevel(Session session)
        {
            using WriterPacket writer = new(ClientOpcode.ChatMessage);

            writer.Write(session.Character.Id);
            writer.Write(session.Character.Level);

            BrodcastAsync(writer);
        }

        public void BrodcastCharacterToggleWeapon(Session session, in ToggleWeaponRequest request)
        {
            using WriterPacket writer = new(ClientOpcode.ChatMessage);

            writer.Write(session.Character.Id);
            writer.Write(request.Position);
            writer.Write(request.Rotation);
            writer.Write(request.Toggle);
            writer.Write(request.Unknown1);

            BrodcastAsync(writer);
        }

        private Session BroadcastCharacterIn(Session session)
        { return session; } // => SendAsync(Responses.Character.InInfoResponse.Create(session));

        private void BroadcastCharacterOut(params Session[] sessions)
        {
            using WriterPacket writer = new(ClientOpcode.CharacterOutInfo);

            writer.Write((byte)sessions.Length);
            foreach (var item in sessions)
            {
                writer.Write(item.Character.Id);
            }

            BrodcastAsync(writer);
        }

        #endregion Brodcast Character

        #region Brodcast Gesture

        public void BroadcastGestureDo(Session session, in DoRequest request)
        {
            using WriterPacket writer = new(ClientOpcode.GestureDo);

            writer.Write(session.Character.Id);
            writer.Write(request.GestureId);
            writer.Write(request.Position);
            writer.Write(request.Unknown1);
            writer.Write(request.Rotation);

            BrodcastAsync(writer);
        }

        #endregion Brodcast Gesture

        #region Brodcast Storage

        public void BrodcastItemUpgradeResponse(Session session, BaseItem item)
        { }

        public void BroadcastItemMove(Session session, params BaseItem[] slots)
        {
            // var size
            //     = sizeof(uint) /* Count */
            //     + (SystemDefinition.ItemSize * slots.Length) /* Items */
            //     + sizeof(byte) /* Unknown */;
            //
            // using WriterPacket writer = new(size, ClientOpcode.StorageItemMoveBroadcast);
            //
            // writer.Write(slots.Length);
            // foreach (var item in slots)
            // {
            //     writer.Write(item);
            // }
            //
            // writer.Write((byte)0);
            //
            // SendExceptAsync(session, writer);
        }

        #endregion Brodcast Storage

        #region Brodcast Movement

        public void SendMovementMove(Session session, in MoveRequest request)
        {
            using WriterPacket writer = new(ClientOpcode.MovementStopBrodcast);

            writer.Write(session.Character.Id);
            writer.Write(request.Unknown1);
            writer.Write(request.Position);
            writer.Write(request.Rotation);
            writer.Write(request.Interpolated);
            writer.Write(request.Unknown5);
            writer.Write(request.Unknown6);
            writer.Write(request.Unknown7);
            writer.Write(request.Unknown8);

            BrodcastAsync(writer);
        }

        public void BroadcastMovementStop(Session session, in StopRequest request)
        {
            using WriterPacket writer = new(ClientOpcode.MovementStopBrodcast);

            writer.Write(session.Character.Id);
            writer.Write(request.Unknown1);
            writer.Write(request.Position);
            writer.Write(request.Rotation);
            writer.Write(request.Unknown4);
            writer.Write(request.Unknown5);

            BrodcastAsync(writer);
        }

        public void SendMovementJump(Session session, in JumpRequest request)
        {
            using WriterPacket writer = new(ClientOpcode.MovementJump);

            writer.Write(session.Character.Id);
            writer.Write(request.Unknown1);
            writer.Write(request.Unknown2);
            writer.Write(request.Location);
            writer.Write(request.Unknown3);
            writer.Write(request.Position);
            writer.Write(request.Rotation);
            writer.Write(request.Interpolated);
            writer.Write(request.Unknown5);
            writer.Write(request.Unknown6);

            BrodcastAsync(writer);
        }

        public void BroadcastLoopMotionEnd(Session session)
        {
            using WriterPacket writer = new(ClientOpcode.MovementJump);

            writer.Write(session.Character.Id);

            BrodcastAsync(writer);
        }

        #endregion Brodcast Movement

        private void BrodcastAsync(WriterPacket writer)
        {
            byte[] packet = UtilsPacket.Pack(writer);
            foreach (var session in InternalSessions.Values)
            {
                bool result = session.SendAsync(packet, 0, writer.Length);
                Debug.Assert(result);
            }
        }

        private void BrodcastExceptAsync(Session session, WriterPacket writer)
        {
            byte[] packet = UtilsPacket.Pack(writer);
            foreach (var s in InternalSessions.Values.Where(s => s.Id != session.Id))
            {
                bool result = session.SendAsync(packet, 0, writer.Length);
                Debug.Assert(result);
            }
        }

        private Dictionary<Guid, Session> InternalSessions { get; } = new();
    }
}