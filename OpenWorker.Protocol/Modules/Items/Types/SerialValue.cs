using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Types;

/// <summary>
/// The serial of an item, one 64-bit number on the wire.
/// </summary>
public readonly struct SerialValue : IWireReadable<SerialValue>, IWireWritable
{
    internal long Value { get; init; }

    /// <summary>
    /// The serial as the client sees it, one number for one item.
    /// </summary>
    public long Number => Value;

    public long Offset
    {
        get => Value & 0xFFFFFFFF; // 32 бита
        init => Value = (Value & ~0xFFFFFFFFL) | (value & 0xFFFFFFFF);
    }

    public int Minute
    {
        get => (int)((Value >> 32) & 0x3F); // 6 бит
        init => Value = (Value & ~(0x3FL << 32)) | ((long)(value & 0x3F) << 32);
    }

    public int Hour
    {
        get => (int)((Value >> 38) & 0x1F); // 5 бит
        init => Value = (Value & ~(0x1FL << 38)) | ((long)(value & 0x1F) << 38);
    }

    public int Day
    {
        get => (int)((Value >> 43) & 0x1F); // 5 бит
        init => Value = (Value & ~(0x1FL << 43)) | ((long)(value & 0x1F) << 43);
    }

    public int Month
    {
        get => (int)((Value >> 48) & 0xF); // 4 бита
        init => Value = (Value & ~(0xFL << 48)) | ((long)(value & 0xF) << 48);
    }

    public int Years
    {
        get => (int)((Value >> 52) & 0xF); // 4 бита
        init => Value = (Value & ~(0xFL << 52)) | ((long)(value & 0xF) << 52);
    }

    public int Group
    {
        get => (int)((Value >> 56) & 0xF); // 4 бита
        init => Value = (Value & ~(0xFL << 56)) | ((long)(value & 0xF) << 56);
    }

    public int Server
    {
        get => (int)((Value >> 60) & 0xF); // 4 бита
        init => Value = (Value & ~(0xFL << 60)) | ((long)(value & 0xF) << 60);
    }

    public SerialValue(long value)
    {
        Value = value;
    }

    public SerialValue(BinaryReader reader)
    {
        Value = reader.ReadInt64();
    }

    public static SerialValue Read(ref PacketReader reader) => new(reader.ReadInt64());

    public void Write(ref PacketWriter writer) => writer.WriteInt64(Value);
}
