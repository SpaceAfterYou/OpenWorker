﻿using Microsoft.Extensions.Logging;
using NetCoreServer;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Providers;
using ow.Framework.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;

namespace ow.Framework.IO.Network
{
    public class GameSession : TcpSession
    {
        private readonly ILogger _logger;
        private readonly HandlerProvider _provider;

        public GameSession(GameServer server, HandlerProvider provider, ILogger logger) : base(server)
        {
            _provider = provider;
            _logger = logger;
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

        public GameSession SendAsync(PacketWriter pw)
        {
            if (!SendAsync(PacketUtils.Pack(pw), 0, pw.BaseStream.Length))
            {
#if !DEBUG
                throw new NetworkException();
#else
                Debug.Assert(false);
#endif // !DEBUG
            }

            return this;
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