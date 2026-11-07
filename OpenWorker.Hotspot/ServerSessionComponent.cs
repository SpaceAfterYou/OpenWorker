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
    /// <summary>
    /// Sends one complete frame. Other connections send here too when they broadcast, so a frame is
    /// written in one piece: mixed frames would break the stream for the client.
    /// </summary>
    public void Send(IResponseHotspotMessage message)
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

            lock (client)
            {
                // Disposed in client
                client.GetStream().Write(buffer, 0, MessageHeader.HeaderSize + header.ContentLength);
            }
        }

        finally
        {
            pool.Return(buffer);
        }
    }

    /// <summary>
    /// Sends to another player: a connection that is gone must not break the session of the sender.
    /// </summary>
    public bool TrySend(IResponseHotspotMessage message)
    {
        try
        {
            Send(message);

            return true;
        }

        catch (Exception e) when (e is IOException or ObjectDisposedException or InvalidOperationException)
        {
            client.Close();

            return false;
        }
    }

    public void Disconnect()
    {
        client.Close();
    }
}