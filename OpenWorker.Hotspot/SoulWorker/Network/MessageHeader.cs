using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.SoulWorker.Network.DataTypes.Enums;

namespace OpenWorker.Hotspot.SoulWorker.Network;

public readonly struct MessageHeader
{
    public ushort Version { get; }
    private ushort Length { get; }
    private MessageDirection Direction { get; }

    public ushort ContentLength => (ushort)(Length - HeaderSize);

    /// <summary>
    /// Largest payload that fits the 16-bit length field, which also counts the header.
    /// </summary>
    public const int MaxContentLength = ushort.MaxValue - HeaderSize;

    public const byte HeaderSize = 0x05;
    private const byte HeaderVersion = 0x05;

    public MessageHeader(long length)
    {
        if (length is < 0 or > MaxContentLength)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "Message size is out of range.");
        }

        var size = length + HeaderSize;

        Version = HeaderVersion;
        Length = (ushort)size;
        Direction = MessageDirection.Game;
    }

    public MessageHeader(BinaryReader reader)
    {
        Version = reader.ReadUInt16();
        Length = reader.ReadUInt16();

        // The client drops the connection on such a header too; without this check
        // ContentLength would wrap around to almost 64 KiB.
        if (Length < HeaderSize)
        {
            throw new InvalidDataException($"Message length {Length} is smaller than the header.");
        }

        Direction = reader.ReadMessageDirection();
    }

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(Version);
        writer.Write(Length);
        writer.Write(Direction);
    }
}