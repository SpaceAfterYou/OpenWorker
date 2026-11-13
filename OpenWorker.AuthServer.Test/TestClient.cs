using System.Buffers;
using System.Net;
using System.Net.Sockets;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Login.Requests;
using OpenWorker.Hotspot.SoulWorker.Network;
using OpenWorker.Wire;

namespace OpenWorker.AuthServer.Test;

internal sealed class TestServer() : TcpListener(IPAddress.Loopback, 0);

internal sealed class TestClient : TcpClient
{
    private void Send(IResponseHotspotMessage message)
    {
        var pool = ArrayPool<byte>.Shared;

        var buffer = pool.Rent(MessageHeader.MaxContentLength);

        try
        {
            MessageHeader header;

            {
                /* using */ var stream = new MemoryStream(buffer);

                /* using */ var writer = new BinaryWriter(stream);

                writer.Write(message.Opcode.Value);

                WireBridge.Write(writer, message);

                header = new MessageHeader(stream.Position);

                var span = buffer.AsSpan(0, header.ContentLength);
                MessageEncryption.Exchange(span, header.Version);
            }

            {
                /* using */ var stream = GetStream();

                /* using */ var writer = new BinaryWriter(stream);

                header.ToBinary(writer);

                writer.Write(buffer, 0, header.ContentLength);
            }
        }

        finally
        {
            pool.Return(buffer);
        }
    }
    
    /// <summary>
    /// Reads from the server until cancelled. The caller owns the task: awaiting it shows how the loop ended.
    /// </summary>
    internal Task LoopAsync(CancellationToken token)
    {
        return Task.Run(() => ReadLoop(token), token);
    }

    private void ReadLoop(CancellationToken token)
    {
        /* using */ var stream = GetStream();

        /* using */ var reader = new BinaryReader(stream);

        while (token.IsCancellationRequested is false)
        {
            _ = new MessageOpcode(reader);
        }
    }
}
