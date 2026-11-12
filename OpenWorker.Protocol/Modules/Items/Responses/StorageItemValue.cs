using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

/// <summary>
/// An item in a slot of a storage, as the lists of the bags carry it: the storage, the slot, then the item.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial record struct StorageItemValue(StorageGroup Group, short Index, ItemValue Item);
