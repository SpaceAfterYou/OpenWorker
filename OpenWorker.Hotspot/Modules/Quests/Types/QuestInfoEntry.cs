namespace OpenWorker.Hotspot.Modules.Quests.Types;

public readonly struct QuestInfoEntry
{
    public required byte AddHelper { get; init; }
    public required short CompleteBit { get; init; }
    public required bool Failed { get; init; }
    public required IReadOnlyList<QuestCondition> Condition { get; init; }
}