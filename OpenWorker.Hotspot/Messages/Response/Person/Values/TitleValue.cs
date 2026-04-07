using System.Runtime.InteropServices;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

[StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
public readonly struct TitleValue
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

    public TitleValue(BinaryReader reader)
    {
        Value = reader.ReadInt64();
    }

    public static implicit operator long(TitleValue titleValue) => titleValue.Value;
    public static implicit operator TitleValue(long value) => new() { Value = value };
}
