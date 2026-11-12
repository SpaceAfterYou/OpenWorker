using System.Runtime.InteropServices;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;

[StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
public readonly struct SpeedValueEntry : IWireReadable<SpeedValueEntry>, IWireWritable
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

    private SpeedValueEntry(ulong value)
    {
        Value = value;
    }

    public static SpeedValueEntry Read(ref PacketReader reader) => new(reader.ReadUInt64());

    /// <summary>
    /// Both speeds as one 64-bit value.
    /// </summary>
    public void Write(ref PacketWriter writer)
    {
        writer.WriteUInt64(Value);
    }
}
