using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Post.Types;

[WireMessage(WireMethods.Read)]
public readonly partial struct PostSendItemValue
{
    public StorageSlot Slot { get; init; }
    public short Count { get; init; }
}
