using ow.Service.District.Game.Items;
using ow.Service.District.Network;
using ow.Framework;
using ow.Framework.Game.Statuses;
using ow.Framework.Game.Types;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Requests.Character;
using ow.Framework.IO.Network.Requests.Chat;
using ow.Framework.IO.Network.Requests.Gesture;
using ow.Framework.IO.Network.Requests.Movement;
using ow.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ow.Service.District.Game
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
            if (InternalSessions.Count >= Defines.MaxChannelSessions || !InternalSessions.TryAdd(session.Id, session))
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
            using PacketWriter writer = new(ClientOpcode.ChatMessage);

            writer.Write(session.Character.Id);
            writer.WriteChatType(type);
            writer.WriteByteLengthUnicodeString(message);

            BroadcastAsync(writer);
        }

        #endregion Broadcast Channel

        #region Broadcast Character

        public void BroadcastCharacterSetLevel(Session session)
        {
            using PacketWriter writer = new(ClientOpcode.ChatMessage);

            writer.Write(session.Character.Id);
            writer.Write(session.Character.Level);

            BroadcastAsync(writer);
        }

        public void BroadcastCharacterToggleWeapon(Session session, in ToggleWeaponRequest request)
        {
            using PacketWriter writer = new(ClientOpcode.ChatMessage);

            writer.Write(session.Character.Id);
            writer.WriteVector3(request.Position);
            writer.Write(request.Rotation);
            writer.Write(request.Toggle);
            writer.Write(request.Unknown1);

            BroadcastAsync(writer);
        }

        private Session BroadcastCharacterIn(Session session)
        { return session; } // => SendAsync(Responses.Character.InInfoResponse.Create(session));

        private void BroadcastCharacterOut(params Session[] sessions)
        {
            using PacketWriter writer = new(ClientOpcode.CharacterOutInfo);

            writer.Write((byte)sessions.Length);
            foreach (var item in sessions)
            {
                writer.Write(item.Character.Id);
            }

            BroadcastAsync(writer);
        }

        #endregion Broadcast Character

        #region Broadcast Gesture

        public void BroadcastGestureDo(Session session, in DoRequest request)
        {
            using PacketWriter writer = new(ClientOpcode.GestureDo);

            writer.Write(session.Character.Id);
            writer.Write(request.GestureId);
            writer.WriteVector3(request.Position);
            writer.Write(request.Unknown1);
            writer.Write(request.Rotation);

            BroadcastAsync(writer);
        }

        #endregion Broadcast Gesture

        #region Broadcast Storage

        public void BroadcastItemUpgradeResponse(Session session, BaseItem item)
        { }

        public void BroadcastItemMove(Session session, params BaseItem[] slots)
        {
            // var size
            //     = sizeof(uint) /* Count */
            //     + (SystemDefinition.ItemSize * slots.Length) /* Items */
            //     + sizeof(byte) /* Unknown */;
            //
            // using PacketWriter writer = new(size, ClientOpcode.StorageItemMoveBroadcast);
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

        public void BroadcastMovementMove(Session session, in MoveRequest request)
        {
            using PacketWriter writer = new(ClientOpcode.MovementStop);

            writer.Write(session.Character.Id);
            writer.Write(request.Unknown1);
            writer.WriteVector3(request.Position);
            writer.Write(request.Rotation);
            writer.WriteVector2(request.Interpolated);
            writer.Write(request.Unknown5);
            writer.Write(request.Unknown6);
            writer.Write(request.Unknown7);
            writer.Write(request.Unknown8);

            BroadcastAsync(writer);
        }

        public void BroadcastMovementStop(Session session, in StopRequest request)
        {
            using PacketWriter writer = new(ClientOpcode.MovementStop);

            writer.Write(session.Character.Id);
            writer.Write(request.Unknown1);
            writer.WriteVector3(request.Position);
            writer.Write(request.Rotation);
            writer.Write(request.Unknown4);
            writer.Write(request.Unknown5);

            BroadcastAsync(writer);
        }

        public void BroadcastMovementJump(Session session, in JumpRequest request)
        {
            using PacketWriter writer = new(ClientOpcode.MovementJump);

            writer.Write(session.Character.Id);
            writer.Write(request.Unknown1);
            writer.Write(request.Unknown2);
            writer.Write(request.Location);
            writer.Write(request.Unknown3);
            writer.WriteVector3(request.Position);
            writer.Write(request.Rotation);
            writer.WriteVector2(request.Interpolated);
            writer.Write(request.Unknown5);
            writer.Write(request.Unknown6);

            BroadcastAsync(writer);
        }

        public void BroadcastLoopMotionEnd(Session session)
        {
            using PacketWriter writer = new(ClientOpcode.MovementJump);

            writer.Write(session.Character.Id);

            BroadcastAsync(writer);
        }

        #endregion Broadcast Movement

        private void BroadcastAsync(PacketWriter writer)
        {
            byte[] packet = PacketUtils.Pack(writer);
            foreach (Session session in InternalSessions.Values)
            {
                bool result = session.SendAsync(packet, 0, writer.BaseStream.Length);
                Debug.Assert(result);
            }
        }

        private void BroadcastExceptAsync(Session except, PacketWriter writer)
        {
            byte[] packet = PacketUtils.Pack(writer);
            foreach (Session session in InternalSessions.Values.Where(s => s.Id != except.Id))
            {
                bool result = session.SendAsync(packet, 0, writer.BaseStream.Length);
                Debug.Assert(result);
            }
        }

        private Dictionary<Guid, Session> InternalSessions { get; } = new();
    }
}
