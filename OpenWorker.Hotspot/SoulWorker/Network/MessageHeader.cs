using System.Diagnostics;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.SoulWorker.Network.DataTypes.Enums;

namespace OpenWorker.Hotspot.SoulWorker.Network;

public readonly struct MessageHeader
{
    public short Version { get; }
    private short Length { get; }
    private MessageDirection Direction { get; }

    public short ContentLength => (short)(Length - HeaderSize);

    private const byte HeaderSize = 0x05;
    private const byte HeaderVersion = 0x05;

    public MessageHeader(long length)
    {
        var size = length + HeaderSize;
        Debug.Assert(size <= short.MaxValue, "Message size is too big");

        Version = HeaderVersion;
        Length = (short)size;
        Direction = MessageDirection.Game;
    }

    public MessageHeader(BinaryReader reader)
    {
        Version = reader.ReadInt16();
        Length = reader.ReadInt16();
        Direction = reader.ReadMessageDirection();
    }

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(Version);
        writer.Write(Length);
        writer.Write(Direction);
    }
}