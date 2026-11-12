using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

[WireMessage(WireMethods.Write)]
public readonly partial record struct ItemOpenSlotGroupEntry(StorageGroup Group, short SlotCount, byte GradeLevel);
