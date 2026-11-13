using System.Buffers;
using System.Text;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

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

    /// <summary>
    /// What the writer of a packet starts with; a longer message grows it as it is written.
    /// </summary>
    private const int PacketCapacity = 256;

    /// <summary>
    /// The opcode and the body of the message, as the client gets them, for the servers of the targets to hand over.
    /// </summary>
    public static CommunityEnvelope ForPacket(Guid origin, IReadOnlyList<int> targets, IResponseHotspotMessage message)
    {
        var writer = new PacketWriter(ArrayPool<byte>.Shared, PacketCapacity, Array.MaxLength);

        try
        {
            writer.WriteByte(message.Opcode.Group);
            writer.WriteByte(message.Opcode.Command);
            message.Write(ref writer);

            if (writer.Overflowed)
            {
                throw new InvalidOperationException($"The message {message.Opcode} does not fit an array.");
            }

            return new CommunityEnvelope(CommunityEnvelopeKind.Packet, origin, targets, string.Empty, writer.Written.ToArray());
        }

        finally
        {
            writer.Dispose();
        }
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
