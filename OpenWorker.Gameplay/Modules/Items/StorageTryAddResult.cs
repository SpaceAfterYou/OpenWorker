using Arch.Core;

namespace OpenWorker.Gameplay.Modules.Items;

public readonly record struct StorageTryAddSlotInfo(short Slot, short Count);

public readonly record struct StorageTryAddResult(bool State, Entity Storage, StorageTryAddSlotInfo[] Info);
