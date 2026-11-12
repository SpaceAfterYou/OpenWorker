using System.Buffers.Binary;
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

    /// <summary>
    /// The newest version the key table of <see cref="MessageEncryption" /> holds keys for: four bytes of the table per
    /// version, three of them used, in a table of 100 bytes.
    /// </summary>
    public const ushort MaxVersion = 24;

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
        : this(reader.ReadBytes(HeaderSize))
    {
    }

    public MessageHeader(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < HeaderSize)
        {
            throw new EndOfStreamException("The connection closed inside a message header.");
        }

        Version = BinaryPrimitives.ReadUInt16LittleEndian(bytes);
        Length = BinaryPrimitives.ReadUInt16LittleEndian(bytes[2..]);

        // The client drops the connection on such a header too; without this check
        // ContentLength would wrap around to almost 64 KiB.
        if (Length < HeaderSize)
        {
            throw new InvalidDataException($"Message length {Length} is smaller than the header.");
        }

        // A version past the key table would fail the decryption; the client never sends one.
        if (Version > MaxVersion)
        {
            throw new InvalidDataException($"Message version {Version} has no keys in the key table.");
        }

        Direction = (MessageDirection)bytes[4];
    }

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(Version);
        writer.Write(Length);
        writer.Write((byte)Direction);
    }

    /// <summary>
    /// Writes the header into the first <see cref="HeaderSize"/> bytes, as <see cref="ToBinary"/> does.
    /// </summary>
    public void Write(Span<byte> destination)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(destination, Version);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[2..], Length);

        destination[4] = (byte)Direction;
    }
}