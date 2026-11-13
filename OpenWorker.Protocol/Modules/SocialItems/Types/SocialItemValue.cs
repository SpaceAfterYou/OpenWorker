using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.SocialItems.Types;

/// <summary>
/// A social object as the client keeps it: its actor, the player who put it down (0 for none), the item it came from, its row
/// of the social item table, the milliseconds it has left and the actors who use it.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial record struct SocialItemValue
{
    public int Actor { get; init; }

    public int Owner { get; init; }

    public int Item { get; init; }

    public short SocialItem { get; init; }

    public long Remaining { get; init; }

    [WireArray(WireLength.Byte)]
    public IReadOnlyList<int> Users { get; init; }
}
