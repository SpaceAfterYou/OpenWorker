using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Channel.Community;

/// <summary>
/// A message another server has already written, only to be handed to the client.
/// </summary>
public readonly struct RawResponse(byte group, byte command, ReadOnlyMemory<byte> content) : IResponseHotspotMessage
{
    public MessageOpcode Opcode => new(group, command);

    public void Write(ref PacketWriter writer)
    {
        writer.WriteBytes(content.Span);
    }

    /// <summary>
    /// Takes a packet envelope body apart: the opcode, then the content.
    /// </summary>
    public static bool TryCreate(ReadOnlyMemory<byte> body, out RawResponse response)
    {
        if (body.Length < 2)
        {
            response = default;

            return false;
        }

        response = new RawResponse(body.Span[0], body.Span[1], body[2..]);

        return true;
    }
}
