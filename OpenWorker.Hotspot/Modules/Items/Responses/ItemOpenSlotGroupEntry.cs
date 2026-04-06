using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

public readonly record struct ItemOpenSlotGroupEntry(StorageGroup Group, short SlotCount, byte GradeLevel);
