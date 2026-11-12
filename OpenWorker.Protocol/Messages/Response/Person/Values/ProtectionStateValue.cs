using OpenWorker.Hotspot.Messages.Response.Person.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

/// <summary>
/// Where the second password and the trade password of the account stand.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct ProtectionStateValue
{
    public PasswordProtectionState SecondPassword { get; init; }
    public PasswordProtectionState TradePassword { get; init; }
}
