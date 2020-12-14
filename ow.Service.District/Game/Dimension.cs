using ow.Framework;
using ow.Framework.Game.Character;
using ow.Framework.Game.Datas;
using ow.Framework.Game.Enums;
using ow.Framework.Game.Storage;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Requests.Character;
using ow.Framework.IO.Network.Requests.Chat;
using ow.Framework.IO.Network.Requests.Gesture;
using ow.Framework.IO.Network.Requests.Movement;
using ow.Framework.Utils;
using ow.Service.District.Game.Items;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ow.Service.District.Game
{
    public sealed class Dimension
    {
        public ushort Id { get; }
        public IReadOnlyDictionary<Guid, GameSession> Sessions => _internalSessions;

        private readonly ConcurrentDictionary<Guid, GameSession> _internalSessions = new();
        private readonly object _sessionsLock = new();

        public ChannelLoadStatus Status => _internalSessions.Count switch
        {
            > 64 => ChannelLoadStatus.Full,
            > 48 => ChannelLoadStatus.High,
            > 32 => ChannelLoadStatus.Medium,
            _ => ChannelLoadStatus.Low
        };

        public bool TryJoin(GameSession session)
        {
            lock (_sessionsLock)
            {
                if (_internalSessions.Count >= Defines.MaxChannelSessions || !_internalSessions.TryAdd(session.Id, session))
                    return false;
            }

            session.Entity.Set(this);
            BroadcastCharacterIn(session);

            return true;
        }

        public void Leave(GameSession session)
        {
            if (_internalSessions.TryRemove(session.Id, out GameSession _))
                BroadcastCharactersOut(session);
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

        private void BroadcastCharacterIn(GameSession session)
        {
            using PacketWriter writer = new(ClientOpcode.CharacterInInfo);

            writer.WriteCharacter(session.Entity.Get<EntityCharacter>(), session.Entity.Get<IStorage>());
            writer.WritePlace(session.Entity.Get<Place>());

            BroadcastExceptAsync(writer, session);
        }

        private void BroadcastCharactersOut(params GameSession[] sessions)
        {
            using PacketWriter writer = new(ClientOpcode.CharacterOutInfo);

            writer.Write((byte)sessions.Length);
            foreach (var session in sessions)
            {
                EntityCharacter character = session.Entity.Get<EntityCharacter>();
                writer.Write(character.Id);
            }

            BroadcastExceptAsync(writer, sessions);
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

        private void BroadcastAsync(PacketWriter writer) =>
            BroadcastAsync(_internalSessions, writer);

        private void BroadcastExceptAsync(PacketWriter writer, GameSession except) =>
            BroadcastAsync(_internalSessions.Where(pair => except.Id != pair.Key), writer);

        private void BroadcastExceptAsync(PacketWriter writer, params GameSession[] except) =>
            BroadcastAsync(_internalSessions.Where(pair => !except.Any(session => session.Id == pair.Key)), writer);

        private static void BroadcastAsync(IEnumerable<KeyValuePair<Guid, GameSession>> pairs, PacketWriter writer)
        {
            byte[] packet = GetRawPacket(writer);
            foreach (var (_, session) in pairs)
                SendAsync(session, writer, packet);
        }

        private static void SendAsync(GameSession session, PacketWriter writer, byte[] packet)
        {
            if (!session.SendAsync(packet, 0, writer.BaseStream.Length))
#if !DEBUG
                throw new NetworkException();
#else
                Debug.Assert(false);
#endif // !DEBUG
        }

        private static byte[] GetRawPacket(PacketWriter writer) => PacketUtils.Pack(writer);
    }
}

// https://youtu.be/7mosZiponDg