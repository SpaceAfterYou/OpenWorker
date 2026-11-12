using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Persons.Types;

/// <summary>
/// An item of a reward.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct RewardItemValue
{
    public int Item { get; init; }
    public short Count { get; init; }
}
