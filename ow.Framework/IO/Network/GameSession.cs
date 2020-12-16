using DefaultEcs;
using Microsoft.Extensions.Logging;
using NetCoreServer;
using ow.Framework.Game.Entities;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Providers;
using ow.Framework.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;

namespace ow.Framework.IO.Network
{
    public sealed class GameSession : TcpSession
    {
        public Entity Entity { get; }

        private readonly ILogger<GameSession> _logger;
        private readonly HandlerProvider _provider;

        public static implicit operator Entity(GameSession session) =>
            session.Entity;

        #region Send Service

        public GameSession SendServiceCurrentDate()
        {
            using PacketWriter writer = new(ClientOpcode.CurrentDate);

            DateTimeOffset dateTime = DateTimeOffset.Now;

            writer.Write(dateTime.ToUnixTimeSeconds());
            writer.Write((ushort)dateTime.Year);
            writer.Write((ushort)dateTime.Month);
            writer.Write((ushort)dateTime.Day);
            writer.Write((ushort)dateTime.Hour);
            writer.Write((ushort)dateTime.Minute);
            writer.Write((ushort)dateTime.Second);
            writer.Write(Convert.ToUInt16(TimeZoneInfo.Local.IsDaylightSavingTime(dateTime)));

            return SendAsync(writer);
        }

        #endregion Send Service

        public GameSession SendAsync(PacketWriter pw)
        {
            if (!SendAsync(PacketUtils.Pack(pw), 0, pw.BaseStream.Length))
#if !DEBUG
                throw new NetworkException();
#else
                Debug.Assert(false);
#endif // !DEBUG

            return this;
        }

        public GameSession(GameServer server, HandlerProvider provider, World entities, ILogger<GameSession> logger) : base(server)
        {
            Entity = entities.CreateEntity();

            _provider = provider;
            _logger = logger;
        }

        protected override void Dispose(bool disposingManagedResources)
        {
            if (Entity.Has<DimensionMemberEntity>())
                Entity.Get<DimensionMemberEntity>().Leave();

            if (disposingManagedResources)
                Entity.Dispose();

            base.Dispose(disposingManagedResources);
        }

        protected override void OnDisconnected() =>
            _logger.LogDebug($"{Id} disconnected");

        protected override void OnConnected() =>
            _logger.LogDebug($"{Id} connected");

        protected override void OnError(SocketError error) =>
            _logger.LogError($"{error}");

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            using MemoryStream ms = new(buffer, (int)offset, (int)size, false);
            using BinaryReader br = new(ms);

            try
            {
                do
                {
                    // SoulWorker Magic
                    ms.Position += sizeof(ushort);

                    // Packet Length
                    int length = br.ReadUInt16() - Defines.PacketEncryptedHeaderSize;

                    // ???
                    ms.Position += sizeof(byte);

                    ProcessPacket(br.ReadBytes(length));
                } while (br.BaseStream.Position < br.BaseStream.Length);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.LogError(ex, "Shit happened");
#if !DEBUG
                Disconnect();
#endif
                return;
            }
        }

        private void ProcessPacket(byte[] raw)
        {
            PacketUtils.Exchange(ref raw);

            using MemoryStream ms = new(raw, false);
            using BinaryReader br = new(ms);

            ushort opcode = br.ReadUInt16();
            DebugLogOpcode(opcode);

            _provider[opcode].Invoke(this, br);
        }

        [Conditional("DEBUG")]
        private void DebugLogOpcode(ushort opcode)
        {
            ServerOpcode o = (ServerOpcode)ConvertUtils.LeToBeUInt16(opcode);

            if (o != ServerOpcode.Heartbeat)
                _logger.LogDebug($"@event [{(ServerOpcode)ConvertUtils.LeToBeUInt16(opcode)}]");
        }
    }
}