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
    public void Send(IResponseHotspotMessage message)
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

                message.Write(writer);

                header = new MessageHeader(stream.Position);

                var span = buffer.AsSpan(0, header.ContentLength);
                MessageEncryption.Exchange(span, header.Version);
            }

            {
                // Disposed in client
                /* using */ var stream = client.GetStream();

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
    
    public void Disconnect()
    {
        client.Close();
    }
}