using System.Runtime.InteropServices;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;

[StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
public readonly struct SpeedValueEntry
{
    [field: FieldOffset(0)]
    public ulong Value { get; }

    [field: FieldOffset(0)]
    public float Move { get; }

    [field: FieldOffset(4)]
    public float Attack { get; }

    public SpeedValueEntry(float move, float attack)
    {
        Move = move;
        Attack = attack;
    }

    public SpeedValueEntry(BinaryReader reader)
    {
        Value = reader.ReadUInt64();
    }
}
