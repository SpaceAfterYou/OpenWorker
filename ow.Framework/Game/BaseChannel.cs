using ow.Framework.IO.Network.Sync;
using ow.Framework.IO.Network.Sync.Commands;
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

        private readonly ConcurrentDictionary<Guid, TSession> _internalSessions = new();

        protected BaseChannel(ushort id) =>
            Id = id;

        protected bool TryAdd(TSession session) =>
            _internalSessions.TryAdd(session.Id, session);

        protected bool TryRemove(TSession session, out TSession? @out) =>
            _internalSessions.TryRemove(session.Id, out @out);

        #region Broadcast World

        protected void BroadcastDeferred(TSession session, SChannelBroadcastCharacterInResponse value) =>
            BroadcastExceptDeferred(SCCategory.World, SCWorld.InInfoPc, session, (SPacketWriter writer) =>
            {
                writer.WriteCharacter(value.Character);
                writer.WritePlace(value.Place);
            });

        protected void BroadcastDeferred(TSession session, SChannelBroadcastCharacterOutResponse value) =>
            BroadcastExceptDeferred(SCCategory.World, SCWorld.OutInfoPc, session, (SPacketWriter writer) =>
            {
                writer.Write((byte)1); // count
                writer.Write(value.Id);
            });

        #endregion Broadcast World

        public void BroadcastDeferred(SCCategory category, object command, Action<SPacketWriter> func)
        {
            using SPacketWriter writer = new(category, command);
            func(writer);

            BroadcastDeferred(_internalSessions, writer);
        }

        public void BroadcastExceptDeferred(SCCategory category, object command, TSession except, Action<SPacketWriter> func)
        {
            using SPacketWriter writer = new(category, command);
            func(writer);

            BroadcastDeferred(_internalSessions.Where(pair => except.Id != pair.Key), writer);
        }

        private static void BroadcastDeferred(IEnumerable<KeyValuePair<Guid, TSession>> sessions, SPacketWriter writer)
        {
            byte[] packet = GetRawPacket(writer);
            foreach ((Guid _, TSession session) in sessions)
                SendDeferred(session, packet, writer);
        }

        private static void SendDeferred(TSession session, byte[] packet, SPacketWriter writer) =>
            session.SendAsync(packet, 0, writer.BaseStream.Length);

        private static byte[] GetRawPacket(SPacketWriter writer) => PacketUtils.Pack(writer);
    }
}

// https://youtu.be/l74Ot_2kuNs