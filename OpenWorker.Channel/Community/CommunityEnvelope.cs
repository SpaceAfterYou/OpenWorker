using System.Text;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Channel.Community;

public enum CommunityEnvelopeKind : byte
{
    /// <summary>
    /// A finished message for the clients of the persons.
    /// </summary>
    Packet = 1,

    /// <summary>
    /// Something the other servers have to do, named by the sender.
    /// </summary>
    Event = 2
}

/// <summary>
/// What goes between the servers. No targets means everybody who is connected to the receiving server.
/// </summary>
public sealed record CommunityEnvelope(
    CommunityEnvelopeKind Kind,
    Guid Origin,
    IReadOnlyList<int> Targets,
    string Name,
    ReadOnlyMemory<byte> Body)
{
    private const int MaxTargets = 4096;

    public static CommunityEnvelope ForPacket(Guid origin, IReadOnlyList<int> targets, IResponseHotspotMessage message)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(message.Opcode.Group);
        writer.Write(message.Opcode.Command);
        message.Write(writer);
        writer.Flush();

        return new CommunityEnvelope(CommunityEnvelopeKind.Packet, origin, targets, string.Empty, stream.ToArray());
    }

    public byte[] ToBytes()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8);

        writer.Write((byte)Kind);
        writer.Write(Origin.ToByteArray());
        writer.Write(Targets.Count);

        foreach (var target in Targets)
        {
            writer.Write(target);
        }

        writer.Write(Name);
        writer.Write(Body.Length);
        writer.Write(Body.Span);
        writer.Flush();

        return stream.ToArray();
    }

    /// <summary>
    /// Anything that does not read to the end is somebody else's noise on the channel and is dropped.
    /// </summary>
    public static bool TryRead(ReadOnlyMemory<byte> bytes, out CommunityEnvelope envelope)
    {
        envelope = null!;

        try
        {
            using var stream = new MemoryStream(bytes.ToArray());
            using var reader = new BinaryReader(stream, Encoding.UTF8);

            var kind = (CommunityEnvelopeKind)reader.ReadByte();
            var origin = new Guid(reader.ReadBytes(16));
            var count = reader.ReadInt32();

            if (!Enum.IsDefined(kind) || count is < 0 or > MaxTargets)
            {
                return false;
            }

            var targets = new int[count];

            for (var i = 0; i < count; ++i)
            {
                targets[i] = reader.ReadInt32();
            }

            var name = reader.ReadString();
            var length = reader.ReadInt32();

            if (length < 0 || length > stream.Length - stream.Position)
            {
                return false;
            }

            envelope = new CommunityEnvelope(kind, origin, targets, name, reader.ReadBytes(length));

            return true;
        }

        catch (Exception e) when (e is EndOfStreamException or ArgumentException or FormatException)
        {
            return false;
        }
    }
}
