using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Quests.Types;

[WireMessage(WireMethods.Write)]
public readonly partial struct CreateItemValue
{
    public int Item { get; init; }
    public short Count { get; init; }
}
