using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Requests;

/// <summary>
/// One end of a move of an item: the storage, the item, the slot. Read in the requests to move, combine and divide,
/// written in the answer to a move.
/// </summary>
[WireMessage(WireMethods.Both)]
public readonly partial struct MoveItemValue
{
    public StorageGroup Storage { get; init; }
    public int Item { get; init; }
    public short Index { get; init; }
}
