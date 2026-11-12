using System.Runtime.InteropServices;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

[StructLayout(LayoutKind.Explicit, Size = sizeof(int))]
public readonly struct FatiguePointsValue : IWireReadable<FatiguePointsValue>, IWireWritable
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

    private FatiguePointsValue(int value)
    {
        Value = value;
    }

    public static FatiguePointsValue Read(ref PacketReader reader) => new(reader.ReadInt32());

    /// <summary>
    /// Both shorts as one 32-bit value.
    /// </summary>
    public void Write(ref PacketWriter writer)
    {
        writer.WriteInt32(Value);
    }
}
