namespace OpenWorker.Hotspot.Messages.Response.Person;

public readonly struct MapValue
{
    // private ulong Sequence { get; init; } // 24 bits
    public MapChannelValue Channel { get; init; } // 8 bits
    public short Location { get; init; } // 16 bits
    public short Server { get; init; } // 16 bits

    public MapValue(BinaryReader reader)
    {
        var value = reader.ReadUInt64();
        // Sequence = value & 0xFFFFFFUL,
        Channel = (byte)((value >> 24) & 0xFFUL);
        Location = (short)((value >> 32) & 0xFFFFUL);
        Server = (short)((value >> 48) & 0xFFFFUL);
    }

    // public static implicit operator MapValue(ulong value)
    // {
    //     return new MapValue
    //     {
    //         // Sequence = value & 0xFFFFFFUL,
    //         Channel = (byte)((value >> 24) & 0xFFUL),
    //         Location = (short)((value >> 32) & 0xFFFFUL),
    //         Server = (short)((value >> 48) & 0xFFFFUL),
    //     };
    // }
    
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
