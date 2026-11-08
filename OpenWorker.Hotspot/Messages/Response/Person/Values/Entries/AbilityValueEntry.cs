using System.Runtime.InteropServices;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;

[StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
public readonly struct AbilityValueEntry : IWireWritable
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

    /// <summary>
    /// The current and the largest value as one 64-bit value.
    /// </summary>
    public void Write(ref PacketWriter writer)
    {
        writer.WriteUInt64(Value);
    }
}
