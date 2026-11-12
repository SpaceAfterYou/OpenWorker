using System.Runtime.InteropServices;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

[StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
public readonly struct TitleValue : IWireReadable<TitleValue>, IWireWritable
{
    [field: FieldOffset(0)]
    public long Value { get; private init; }

    [field: FieldOffset(0)]
    public int Primary { get; }

    [field: FieldOffset(4)]
    public int Secondary { get; }

    public TitleValue(int primary, int secondary)
    {
        Primary = primary;
        Secondary = secondary;
    }

    public static implicit operator long(TitleValue titleValue) => titleValue.Value;
    public static implicit operator TitleValue(long value) => new() { Value = value };

    /// <summary>
    /// Both titles as one 64-bit value.
    /// </summary>
    public static TitleValue Read(ref PacketReader reader) => new() { Value = reader.ReadInt64() };

    /// <summary>
    /// Both titles as one 64-bit value.
    /// </summary>
    public void Write(ref PacketWriter writer)
    {
        writer.WriteInt64(Value);
    }
}
