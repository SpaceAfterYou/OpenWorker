using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Response.Person;

/// <summary>
/// A map on the wire: 64 bits, the channel in bits 24 to 31, the location in 32 to 47, the server in 48 to 63.
/// </summary>
public readonly struct MapValue : IWireReadable<MapValue>, IWireWritable
{
    // private ulong Sequence { get; init; } // 24 bits
    public MapChannelValue Channel { get; init; } // 8 bits
    public short Location { get; init; } // 16 bits
    public short Server { get; init; } // 16 bits

    public MapValue(BinaryReader reader) : this(reader.ReadUInt64())
    {
    }

    private MapValue(ulong value)
    {
        // Sequence = value & 0xFFFFFFUL,
        Channel = (byte)((value >> 24) & 0xFFUL);
        Location = (short)((value >> 32) & 0xFFFFUL);
        Server = (short)((value >> 48) & 0xFFFFUL);
    }

    public static MapValue Read(ref PacketReader reader) => new(reader.ReadUInt64());

    public void Write(ref PacketWriter writer) => writer.WriteUInt64(this);

    public static implicit operator ulong(MapValue value)
    {
        ulong result = 0;

        // value |= (value.Sequence & 0xFFFFFFUL);
        result |= ((ulong)value.Channel & 0xFFUL) << 24;
        result |= ((ulong)(short)value.Location & 0xFFFFUL) << 32;
        result |= ((ulong)(short)value.Server & 0xFFFFUL) << 48;

        return result;
    }
}
