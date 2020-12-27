using ow.Framework.Game.Enums;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Framework.IO.Network.Sync;
using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ow.Framework.Game
{
    public abstract class BaseDimension
    {
        public ushort Id { get; }
        public IReadOnlyDictionary<Guid, SyncSession> Sessions => _internalSessions;

        private readonly ConcurrentDictionary<Guid, SyncSession> _internalSessions = new();

        public ChannelLoadStatus Status => _internalSessions.Count switch
        {
            > 64 => ChannelLoadStatus.Full,
            > 48 => ChannelLoadStatus.High,
            > 32 => ChannelLoadStatus.Medium,
            _ => ChannelLoadStatus.Low
        };

        protected BaseDimension(ushort id) => Id = id;

        protected bool Join(SyncSession session) => _internalSessions.TryAdd(session.Id, session);

        protected bool Leave(SyncSession session) => _internalSessions.TryRemove(session.Id, out SyncSession _);

        #region Broadcast Character

        protected void BroadcastAsync(SyncSession session, DimensionBrodcastCharacterInResponse value) =>
            BroadcastExceptAsync(ClientOpcode.CharacterInInfo, session, (PacketWriter writer) =>
            {
                writer.WriteCharacter(value.Character);
                writer.WritePlace(value.Place);
            });

        protected void BroadcastAsync(SyncSession session, DimensionBrodcastCharacterOutResponse value) =>
            BroadcastExceptAsync(ClientOpcode.CharacterOutInfo, session, (PacketWriter writer) =>
            {
                writer.Write((byte)1); // length
                writer.Write(value.Id);
            });

        #endregion Broadcast Character

        public void BroadcastAsync(ClientOpcode opcode, Action<PacketWriter> func)
        {
            using PacketWriter writer = new(opcode);
            func(writer);

            BroadcastAsync(_internalSessions, writer);
        }

        public void BroadcastExceptAsync(ClientOpcode opcode, SyncSession except, Action<PacketWriter> func)
        {
            using PacketWriter writer = new(opcode);
            func(writer);

            BroadcastAsync(_internalSessions.Where(pair => except.Id != pair.Key), writer);
        }

        private static void BroadcastAsync(IEnumerable<KeyValuePair<Guid, SyncSession>> pairs, PacketWriter writer)
        {
            byte[] packet = GetRawPacket(writer);
            foreach ((Guid _, SyncSession session) in pairs)
                SendAsync(session, packet, writer.BaseStream.Length);
        }

        private static void SendAsync(SyncSession session, byte[] packet, long length)
        {
            /// [ TODO ] Change checking

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
