using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

public readonly record struct StorageItemValue(StorageGroup Group, short Index, ItemValue Item);