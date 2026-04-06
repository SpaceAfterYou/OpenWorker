using System.Runtime.InteropServices;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

[StructLayout(LayoutKind.Explicit, Size = sizeof(int))]
public readonly struct FatiguePointsValue
{
    [field: FieldOffset(0)]
    public int Value { get; }

    [field: FieldOffset(0)]
    public short Common { get; init; }

    [field: FieldOffset(2)]
    public short Bonus { get; init; }

    public FatiguePointsValue(short common, short bonus)
    {
        Common = common;
        Bonus = bonus;
    }

    public FatiguePointsValue(BinaryReader reader)
    {
        Value = reader.ReadInt32();
    }
}
