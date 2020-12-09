using Core.Systems.NetSystem.Opcodes;
using Core.Systems.NetSystem.Packets;
using Core.Systems.NetSystem.Providers;
using Microsoft.Extensions.Logging;
using NetCoreServer;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;

namespace Core.Systems.NetSystem
{
    public class SwSession : TcpSession
    {
        private readonly ILogger _logger;
        private readonly HandlerProvider _provider;

        public SwSession(TcpServer server, HandlerProvider provider, ILogger logger) : base(server)
        {
            _provider = provider;
            _logger = logger;
        }

        protected override void OnDisconnected()
        {
            _logger.LogDebug($"{Id} disconnected");
            base.OnDisconnected();
        }

        protected override void OnConnected()
        {
            _logger.LogDebug($"{Id} connected");
            base.OnConnected();
        }

        protected override void OnError(SocketError error)
        {
            _logger.LogError($"{error}");
            base.OnError(error);
        }

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
                    int length = br.ReadUInt16() - UtilsPacket.EncryptedHeaderSize;

                    // ???
                    ms.Position += sizeof(byte);

                    ProcessPacket(br.ReadBytes(length));
                } while (br.BaseStream.Position < br.BaseStream.Length);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.LogError(ex, "Unknown exception");
#if !DEBUG
                Disconnect();
#endif
                return;
            }

            base.OnReceived(buffer, offset, size);
        }

        private void ProcessPacket(byte[] raw)
        {
            UtilsPacket.Exchange(ref raw);

            using MemoryStream ms = new(raw, false);
            using BinaryReader br = new(ms);

            ushort opcode = br.ReadUInt16();
            DebugLogOpcode(opcode);

            _provider[opcode].Invoke(this, br);
        }

        public SwSession SendAsync(WriterPacket pw)
        {
            if (!SendAsync(UtilsPacket.Pack(pw), 0, pw.Length))
            {
#if !DEBUG
                this.Disconnect();
#endif // !DEBUG
            }

            return this;
        }

        [Conditional("DEBUG")]
        private void DebugLogOpcode(ushort opcode)
        {
            var o = (HandlerOpcode)Convert.LeToBeUInt16(opcode);
            if (o != HandlerOpcode.Heartbeat)
            {
                _logger.LogDebug($"@event [{(HandlerOpcode)Convert.LeToBeUInt16(opcode)}]");
            }
        }
    }
}