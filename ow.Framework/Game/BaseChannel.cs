using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network.Sync;
using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Framework.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ow.Framework.Game
{
    public abstract class BaseChannel<TSession>
        where TSession : SSessionBase
    {
        public ushort Id { get; }
        public IReadOnlyDictionary<Guid, TSession> Sessions => _internalSessions;

        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<Guid, TSession> _internalSessions = new();

        protected BaseChannel(ushort id, ILogger logger) => (Id, _logger) = (id, logger);

        protected bool TryAdd(TSession session) => _internalSessions.TryAdd(session.Id, session);

        protected bool TryRemove(TSession session, out TSession? @out) => _internalSessions.TryRemove(session.Id, out @out);

        #region Broadcast Character

        protected void BroadcastAsync(TSession session, SChannelBroadcastCharacterInResponse value) =>
            BroadcastExceptAsync(ClientOpcode.CharacterInInfo, session, (PacketWriter writer) =>
            {
                writer.WriteCharacter(value.Character);
                writer.WritePlace(value.Place);
            });

        protected void BroadcastAsync(TSession session, SChannelBroadcastCharacterOutResponse value) =>
            BroadcastExceptAsync(ClientOpcode.CharacterOutInfo, session, (PacketWriter writer) =>
            {
                writer.Write((byte)1); // count
                writer.Write(value.Id);
            });

        #endregion Broadcast Character

        public void BroadcastAsync(ClientOpcode opcode, Action<PacketWriter> func)
        {
            using PacketWriter writer = new(opcode, _logger);
            func(writer);

            BroadcastAsync(_internalSessions, writer);
        }

        public void BroadcastExceptAsync(ClientOpcode opcode, TSession except, Action<PacketWriter> func)
        {
            using PacketWriter writer = new(opcode, _logger);
            func(writer);

            BroadcastAsync(_internalSessions.Where(pair => except.Id != pair.Key), writer);
        }

        private static void BroadcastAsync(IEnumerable<KeyValuePair<Guid, TSession>> pairs, PacketWriter writer)
        {
            byte[] packet = GetRawPacket(writer);
            foreach ((Guid _, TSession session) in pairs)
                SendAsync(session, packet, writer.BaseStream.Length);
        }

        private static void SendAsync(TSession session, byte[] packet, long length) =>
            session.SendAsync(packet, 0, length);

        private static byte[] GetRawPacket(PacketWriter writer) => PacketUtils.Pack(writer);
    }
}