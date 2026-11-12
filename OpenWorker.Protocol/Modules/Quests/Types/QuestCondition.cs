using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Quests.Types;

[WireMessage(WireMethods.Write)]
public readonly partial struct QuestCondition
{
    public required int Condition { get; init; }
    public required byte Step { get; init; }
}
