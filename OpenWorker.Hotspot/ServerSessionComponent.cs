using System.Buffers;
using System.Net.Sockets;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.SoulWorker.Network;

namespace OpenWorker.Hotspot;

[EntityComponent(EntityComponentService.All)]
public readonly struct ServerSessionComponent(TcpClient client)
{
    private readonly SessionWriter _writer = new(client);

    /// <summary>
    /// Where the client connected from, for the logs. Empty when the socket was not connected or is gone.
    /// </summary>
    public string? RemoteEndPoint { get; } = DescribeRemote(client);

    private static string? DescribeRemote(TcpClient client)
    {
        try
        {
            return client.Client?.RemoteEndPoint?.ToString();
        }

        catch (Exception e) when (e is ObjectDisposedException or SocketException)
        {
            return null;
        }
    }

    /// <summary>
    /// Queues the message for this connection. Never waits for the client, and a connection that is
    /// already gone just drops the message.
    /// </summary>
    public void Send(IResponseHotspotMessage message)
    {
        TrySend(message);
    }

    public bool TrySend(IResponseHotspotMessage message)
    {
        var pool = ArrayPool<byte>.Shared;

        var buffer = pool.Rent(MessageHeader.HeaderSize + MessageHeader.MaxContentLength);

        try
        {
            /* using */ var stream = new MemoryStream(buffer);

            /* using */ var writer = new BinaryWriter(stream);

            stream.Position = MessageHeader.HeaderSize;

            writer.Write(message.Opcode);

            message.Write(writer);

            var header = new MessageHeader(stream.Position - MessageHeader.HeaderSize);

            MessageEncryption.Exchange(buffer.AsSpan(MessageHeader.HeaderSize, header.ContentLength), header.Version);

            stream.Position = 0;

            header.ToBinary(writer);

            writer.Flush();

            var length = MessageHeader.HeaderSize + header.ContentLength;

            var frame = pool.Rent(length);

            buffer.AsSpan(0, length).CopyTo(frame);

            return _writer.TryEnqueue(frame, length);
        }

        finally
        {
            pool.Return(buffer);
        }
    }

    /// <summary>
    /// Closes the connection after everything queued has been sent.
    /// </summary>
    public void Disconnect()
    {
        _writer.Complete();
    }
}
