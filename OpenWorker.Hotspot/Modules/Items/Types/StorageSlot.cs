using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Types;

/// <summary>
/// A slot of a storage, read in requests and written in responses.
/// </summary>
[WireMessage(WireMethods.Both)]
public readonly partial struct StorageSlot : IWritableData
{
    public StorageGroup Storage { get; init; }
    public short Slot { get; init; }
}
