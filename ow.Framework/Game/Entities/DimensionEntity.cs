using ow.Framework.Game.Character;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ow.Framework.Game.Entities
{
    public sealed class DimensionEntity
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

        public DimensionEntity(ushort id) => Id = id;

        public bool TryJoin(GameSession session)
        {
            lock (_sessionsLock)
            {
                if (_internalSessions.Count >= Defines.MaxChannelSessions || !_internalSessions.TryAdd(session.Id, session))
                    return false;
            }

            session.Entity.Set(new DimensionMemberEntity(session, this));
            BroadcastCharacterIn(session);

            return true;
        }

        internal void Leave(GameSession session)
        {
            if (_internalSessions.TryRemove(session.Id, out GameSession _))
                BroadcastCharactersOut(session);
        }

        #region Broadcast Character

        private void BroadcastCharacterIn(GameSession session)
        {
            using PacketWriter writer = new(ClientOpcode.CharacterInInfo);

            writer.WriteCharacter(session);
            writer.WritePlace(session.Entity.Get<PlaceEntity>());

            BroadcastExceptAsync(writer, session);
        }

        private void BroadcastCharactersOut(params GameSession[] sessions)
        {
            using PacketWriter writer = new(ClientOpcode.CharacterOutInfo);

            writer.Write((byte)sessions.Length);
            foreach (var session in sessions)
                writer.Write(session.Entity.Get<EntityCharacter>().Id);

            BroadcastExceptAsync(writer, sessions);
        }

        #endregion Broadcast Character

        internal void BroadcastAsync(PacketWriter writer) =>
            BroadcastAsync(_internalSessions, writer);

        internal void BroadcastExceptAsync(PacketWriter writer, GameSession except) =>
            BroadcastAsync(_internalSessions.Where(pair => except.Id != pair.Key), writer);

        internal void BroadcastExceptAsync(PacketWriter writer, params GameSession[] except) =>
            BroadcastAsync(_internalSessions.Where(pair => !except.Any(session => session.Id == pair.Key)), writer);

        private static void BroadcastAsync(IEnumerable<KeyValuePair<Guid, GameSession>> pairs, PacketWriter writer)
        {
            byte[] packet = GetRawPacket(writer);
            foreach (var (_, session) in pairs)
                SendAsync(session, packet, writer.BaseStream.Length);
        }

        private static void SendAsync(GameSession session, byte[] packet, long length)
        {
            if (!session.SendAsync(packet, 0, length))
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