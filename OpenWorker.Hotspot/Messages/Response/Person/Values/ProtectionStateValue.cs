using System.Runtime.InteropServices;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

[StructLayout(LayoutKind.Explicit, Size = sizeof(short))]
public readonly struct ProtectionStateValue(BinaryReader reader)
{
    [field: FieldOffset(0)]
    public short Value { get; private init; } = reader.ReadInt16();

    [field: FieldOffset(0)]
    public bool HasSecondPassword { get; init; }

    [field: FieldOffset(1)]
    public bool HasTradePassword { get; init; }

    public static implicit operator short(ProtectionStateValue value) => value.Value;
    public static implicit operator ProtectionStateValue(short value) => new() { Value = value };
}
