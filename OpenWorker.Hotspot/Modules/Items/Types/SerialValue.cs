using Arch.Core;

namespace OpenWorker.Hotspot.Modules.Items.Types;

public readonly struct SerialValue
{
    internal long Value { get; init; }

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
    
    public SerialValue(Arch.Core.World world, Entity entity)
    {
        Value = world.Get<StorageItemSerialComponent>(entity).Serial;
    }

    public SerialValue(BinaryReader reader)
    {
        Value = reader.ReadInt64();
    }
}