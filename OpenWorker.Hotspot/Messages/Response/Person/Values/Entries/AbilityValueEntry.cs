using System.Runtime.InteropServices;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;

[StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
public readonly struct AbilityValueEntry
{
    [field: FieldOffset(0)]
    public ulong Value { get; }

    [field: FieldOffset(0)]
    public int Current { get; }

    [field: FieldOffset(4)]
    public int Max { get; }

    public AbilityValueEntry(int current, int max)
    {
        Current = current;
        Max = max;
    }

    public AbilityValueEntry(BinaryReader reader)
    {
        Value = reader.ReadUInt64();
    }
}
