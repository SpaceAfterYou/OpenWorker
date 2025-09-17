namespace OpenWorker.Hotspot.Modules.Quests.Types;

public readonly struct QuestCondition
{
    public required int Condition { get; init; }
    public required byte Step { get; init; }
}