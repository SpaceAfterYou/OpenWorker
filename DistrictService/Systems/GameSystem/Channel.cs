using Core.Systems.NetSystem.Opcodes;
using Core.Systems.NetSystem.Packets;
using Core.Systems.NetSystem.Requests.Character;
using Core.Systems.NetSystem.Requests.Chat;
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

        #region Broadcast Channel

        public void BroadcastChatMessage(Session session, in ReceiveRequest request) =>
            BroadcastChatMessage(session, request.Type, request.Message);

        public void BroadcastChatMessage(Session session, ChatType type, string message)
        {
            using WriterPacket writer = new(ClientOpcode.ChatMessage);

            writer.Write(session.Character.Id);
            writer.Write(type);
            writer.WriteByteLengthUnicodeString(message);

            BroadcastAsync(writer);
        }

        #endregion Broadcast Channel

        #region Broadcast Character

        public void BroadcastCharacterSetLevel(Session session)
        {
            using WriterPacket writer = new(ClientOpcode.ChatMessage);

            writer.Write(session.Character.Id);
            writer.Write(session.Character.Level);

            BroadcastAsync(writer);
        }

        public void BrodcastCharacterToggleWeapon(Session session, in ToggleWeaponRequest request)
        {
            using WriterPacket writer = new(ClientOpcode.ChatMessage);

            writer.Write(session.Character.Id);
            writer.Write(request.Position);
            writer.Write(request.Rotation);
            writer.Write(request.Toggle);
            writer.Write(request.Unknown1);

            BroadcastAsync(writer);
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

            BroadcastAsync(writer);
        }

        #endregion Broadcast Character

        #region Broadcast Gesture

        public void BrodcastGestureDo(Session session, in DoRequest request)
        {
            using WriterPacket writer = new(ClientOpcode.GestureDo);

            writer.Write(session.Character.Id);
            writer.Write(request.GestureId);
            writer.Write(request.Position);
            writer.Write(request.Unknown1);
            writer.Write(request.Rotation);

            BroadcastAsync(writer);
        }

        #endregion Broadcast Gesture

        #region Broadcast Storage

        public void BrodcastItemUpgradeResponse(Session session, BaseItem item)
        { }

        public void BrodcastItemMove(Session session, params BaseItem[] slots)
        {
            // var size
            //     = sizeof(uint) /* Count */
            //     + (SystemDefinition.ItemSize * slots.Length) /* Items */
            //     + sizeof(byte) /* Unknown */;
            //
            // using WriterPacket writer = new(size, ClientOpcode.StorageItemMoveBrodcast);
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

        #endregion Broadcast Storage

        #region Broadcast Movement

        public void BrodcastMovementMove(Session session, in MoveRequest request)
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

            BroadcastAsync(writer);
        }

        public void BrodcastMovementStop(Session session, in StopRequest request)
        {
            using WriterPacket writer = new(ClientOpcode.MovementStopBrodcast);

            writer.Write(session.Character.Id);
            writer.Write(request.Unknown1);
            writer.Write(request.Position);
            writer.Write(request.Rotation);
            writer.Write(request.Unknown4);
            writer.Write(request.Unknown5);

            BroadcastAsync(writer);
        }

        public void BrodcastMovementJump(Session session, in JumpRequest request)
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

            BroadcastAsync(writer);
        }

        public void BrodcastLoopMotionEnd(Session session)
        {
            using WriterPacket writer = new(ClientOpcode.MovementJump);

            writer.Write(session.Character.Id);

            BroadcastAsync(writer);
        }

        #endregion Broadcast Movement

        private void BroadcastAsync(WriterPacket writer)
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