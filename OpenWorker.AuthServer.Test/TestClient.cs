using System.Buffers;
using System.Net;
using System.Net.Sockets;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Login.Requests;
using OpenWorker.Hotspot.SoulWorker.Network;

namespace OpenWorker.AuthServer.Test;

internal sealed class TestServer() : TcpListener(IPAddress.Loopback, 0);

internal sealed class TestClient : TcpClient
{
    private void Send(IResponseHotspotMessage message)
    {
        var pool = ArrayPool<byte>.Shared;

        var buffer = pool.Rent(short.MaxValue);

        try
        {
            MessageHeader header;

            {
                /* using */ var stream = new MemoryStream(buffer);

                /* using */ var writer = new BinaryWriter(stream);

                writer.Write(message.Opcode);

                message.ToBinary(writer);

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
    
    // internal void SendLogin(string username, string password)
    // {
    //     var message = new LoginAuthRequest(username, password, "12-34-56-78-90-12");
    //     Send(message);
    // }
    
    internal Task LoopAsync(CancellationToken token)
    {
        return Task.Factory.StartNew(
            () => InternalLoopAsync(token).ConfigureAwait(false),
            token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    private Task InternalLoopAsync(CancellationToken token)
    {
        while (token.IsCancellationRequested is false)
        {
            /* using */ var stream = GetStream();

            /* using */ var reader = new BinaryReader(stream);

            var opcode = new MessageOpcode(reader);
        }
        
        return Task.CompletedTask;
    }
}