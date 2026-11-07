using System.Diagnostics;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.SoulWorker.Network.DataTypes.Enums;

namespace OpenWorker.Hotspot.SoulWorker.Network;

public readonly struct MessageHeader
{
    public ushort Version { get; }
    private ushort Length { get; }
    private MessageDirection Direction { get; }

    public ushort ContentLength => (ushort)(Length - HeaderSize);

    private const byte HeaderSize = 0x05;
    private const byte HeaderVersion = 0x05;

    public MessageHeader(long length)
    {
        var size = length + HeaderSize;
        Debug.Assert(size <= ushort.MaxValue, "Message size is too big");

        Version = HeaderVersion;
        Length = (ushort)size;
        Direction = MessageDirection.Game;
    }

    public MessageHeader(BinaryReader reader)
    {
        Version = reader.ReadUInt16();
        Length = reader.ReadUInt16();
        Direction = reader.ReadMessageDirection();
    }

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(Version);
        writer.Write(Length);
        writer.Write(Direction);
    }
}