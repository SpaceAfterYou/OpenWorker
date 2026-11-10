using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Types;

/// <summary>
/// How often the person has used a kind of item (by its use type) on the game day that is running, and when it last did.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct ItemUseInfoValue
{
    public int Type { get; init; }

    public byte Count { get; init; }

    /// <summary>
    /// When the kind was last used, seconds since 1970.
    /// </summary>
    public long UsedAt { get; init; }
}
