using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Chat.Types;

/// <summary>
/// The slot of the item a request uses up.
/// </summary>
[WireMessage]
public readonly partial struct UseItemFrom
{
    public StorageGroup Storage { get; }
    public short Slot { get; }
}
