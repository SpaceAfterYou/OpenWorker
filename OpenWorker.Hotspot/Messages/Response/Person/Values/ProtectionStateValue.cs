using System.Runtime.InteropServices;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

[StructLayout(LayoutKind.Explicit, Size = sizeof(short))]
public readonly struct ProtectionStateValue
{
    [field: FieldOffset(0)]
    public short Value { get; }

    [field: FieldOffset(0)]
    public bool HasSecondPassword { get; }

    [field: FieldOffset(1)]
    public bool HasTradePassword { get; }

    public ProtectionStateValue(bool hasSecondPassword, bool hasTradePassword)
    {
        HasSecondPassword = hasSecondPassword;
        HasTradePassword = hasTradePassword;
    }
}
