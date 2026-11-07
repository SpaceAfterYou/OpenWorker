using OpenWorker.Hotspot.Messages.Response.Person.Enums;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

public readonly struct ProtectionStateValue
{
    public PasswordProtectionState SecondPassword { get; init; }
    public PasswordProtectionState TradePassword { get; init; }
}
