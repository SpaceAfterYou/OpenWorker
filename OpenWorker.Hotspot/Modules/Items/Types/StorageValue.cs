using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.Hotspot.Modules.Items.Types;

public readonly struct StorageValue
{
    public required ItemValue Item { get; init; }
    public required StorageGroup Storage { get; init; }
    public required short Slot { get; init; }
}