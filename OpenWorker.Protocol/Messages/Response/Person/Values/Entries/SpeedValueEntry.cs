using System.Runtime.InteropServices;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;

[StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
public readonly struct SpeedValueEntry : IWireWritable
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

    /// <summary>
    /// Both speeds as one 64-bit value.
    /// </summary>
    public void Write(ref PacketWriter writer)
    {
        writer.WriteUInt64(Value);
    }
}
