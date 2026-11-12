using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Rewards.Types;

/// <summary>
/// An item and its amount, as the client reads the reward lists of several windows.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial record struct RewardItemValue(int Item, short Count);
