using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Shop.Types;

/// <summary>
/// An appearance a purchase opened, and until when (seconds since 1970, 0 for good).
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct CashAppearanceValue
{
    public required ushort Appearance { get; init; }
    public required long EndsAt { get; init; }
}
