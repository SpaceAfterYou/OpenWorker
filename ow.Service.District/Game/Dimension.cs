using ow.Framework;
using ow.Framework.Game.Character;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Requests.Character;
using ow.Framework.IO.Network.Requests.Chat;
using ow.Framework.IO.Network.Requests.Gesture;
using ow.Framework.IO.Network.Requests.Movement;
using ow.Framework.Utils;
using ow.Service.District.Game.Items;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ow.Service.District.Game
{
    public sealed class Dimension
    {
        public ushort Id { get; }
        public IReadOnlyDictionary<Guid, GameSession> Sessions => InternalSessions;

        public ChannelLoadStatus Status => InternalSessions.Count switch
        {
            > 64 => ChannelLoadStatus.Full,
            > 48 => ChannelLoadStatus.High,
            > 32 => ChannelLoadStatus.Medium,
            _ => ChannelLoadStatus.Low
        };

        public bool TryJoin(GameSession session)
        {
            if (InternalSessions.Count >= Defines.MaxChannelSessions || !InternalSessions.TryAdd(session.Id, session))
                return false;

            session.Entity.Set(this);
            BroadcastCharacterIn(session);

            return true;
        }

        public void Leave(GameSession session)
        {
            InternalSessions.Remove(session.Id);
            BroadcastCharacterOut(session);
        }

        public Dimension(ushort id) => Id = id;

        #region Broadcast Channel

        public void BroadcastChatMessage(GameSession session, in ReceiveRequest request) =>
            BroadcastChatMessage(session, request.Type, request.Message);

        public void BroadcastChatMessage(GameSession session, ChatType type, string message)
        {
            using PacketWriter writer = new(ClientOpcode.ChatMessage);

            EntityCharacter character = session.Entity.Get<EntityCharacter>();
            writer.Write(character.Id);

            writer.WriteChatType(type);
            writer.WriteByteLengthUnicodeString(message);

            BroadcastAsync(writer);
        }

        #endregion Broadcast Channel

        #region Broadcast Character

        public void BroadcastCharacterSetLevel(GameSession session)
        {
            using PacketWriter writer = new(ClientOpcode.ChatMessage);

            EntityCharacter character = session.Entity.Get<EntityCharacter>();
            writer.Write(character.Id);
            writer.Write(character.Level);

            BroadcastAsync(writer);
        }

        public void BroadcastCharacterToggleWeapon(GameSession session, in ToggleWeaponRequest request)
        {
            using PacketWriter writer = new(ClientOpcode.ChatMessage);

            EntityCharacter character = session.Entity.Get<EntityCharacter>();
            writer.Write(character.Id);

            writer.WriteVector3(request.Position);
            writer.Write(request.Rotation);
            writer.Write(request.Toggle);
            writer.Write(request.Unknown1);

            BroadcastAsync(writer);
        }

        private GameSession BroadcastCharacterIn(GameSession session)
        { return session; } // => SendAsync(Responses.Character.InInfoResponse.Create(session));

        private void BroadcastCharacterOut(params GameSession[] sessions)
        {
            using PacketWriter writer = new(ClientOpcode.CharacterOutInfo);

            writer.Write((byte)sessions.Length);
            foreach (var session in sessions)
            {
                EntityCharacter character = session.Entity.Get<EntityCharacter>();
                writer.Write(character.Id);
            }

            BroadcastAsync(writer);
        }

        #endregion Broadcast Character

        #region Broadcast Gesture

        public void BroadcastGestureDo(GameSession session, in DoRequest request)
        {
            using PacketWriter writer = new(ClientOpcode.GestureDo);

            EntityCharacter character = session.Entity.Get<EntityCharacter>();
            writer.Write(character.Id);

            writer.Write(request.GestureId);
            writer.WriteVector3(request.Position);
            writer.Write(request.Unknown1);
            writer.Write(request.Rotation);

            BroadcastAsync(writer);
        }

        #endregion Broadcast Gesture

        #region Broadcast Storage

        public void BroadcastItemUpgradeResponse(GameSession session, BaseItem item)
        { }

        public void BroadcastItemMove(GameSession session, params BaseItem[] slots)
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

        public void BroadcastMovementMove(GameSession session, in MoveRequest request)
        {
            using PacketWriter writer = new(ClientOpcode.MovementStop);

            EntityCharacter character = session.Entity.Get<EntityCharacter>();
            writer.Write(character.Id);

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

        public void BroadcastMovementStop(GameSession session, in StopRequest request)
        {
            using PacketWriter writer = new(ClientOpcode.MovementStop);

            EntityCharacter character = session.Entity.Get<EntityCharacter>();
            writer.Write(character.Id);

            writer.Write(request.Unknown1);
            writer.WriteVector3(request.Position);
            writer.Write(request.Rotation);
            writer.Write(request.Unknown4);
            writer.Write(request.Unknown5);

            BroadcastAsync(writer);
        }

        public void BroadcastMovementJump(GameSession session, in JumpRequest request)
        {
            using PacketWriter writer = new(ClientOpcode.MovementJump);

            EntityCharacter character = session.Entity.Get<EntityCharacter>();
            writer.Write(character.Id);

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

        public void BroadcastLoopMotionEnd(GameSession session)
        {
            using PacketWriter writer = new(ClientOpcode.MovementJump);

            EntityCharacter character = session.Entity.Get<EntityCharacter>();
            writer.Write(character.Id);

            BroadcastAsync(writer);
        }

        #endregion Broadcast Movement

        private void BroadcastAsync(PacketWriter writer)
        {
            byte[] packet = PacketUtils.Pack(writer);
            foreach (GameSession session in InternalSessions.Values)
            {
                bool result = session.SendAsync(packet, 0, writer.BaseStream.Length);
                Debug.Assert(result);
            }
        }

        private void BroadcastExceptAsync(GameSession except, PacketWriter writer)
        {
            byte[] packet = PacketUtils.Pack(writer);
            foreach (GameSession session in InternalSessions.Values.Where(s => s.Id != except.Id))
            {
                bool result = session.SendAsync(packet, 0, writer.BaseStream.Length);
                Debug.Assert(result);
            }
        }

        private Dictionary<Guid, GameSession> InternalSessions { get; } = new();
    }
}