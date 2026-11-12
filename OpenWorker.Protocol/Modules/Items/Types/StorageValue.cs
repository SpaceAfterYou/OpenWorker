using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Types;

/// <summary>
/// An item in a slot of a storage: the storage, the slot, then the item.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct StorageValue
{
    public required StorageGroup Storage { get; init; }
    public required short Slot { get; init; }
    public required ItemValue Item { get; init; }
}
