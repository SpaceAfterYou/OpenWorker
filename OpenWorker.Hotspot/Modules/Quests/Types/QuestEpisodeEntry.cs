namespace OpenWorker.Hotspot.Modules.Quests.Types;

public readonly struct QuestEpisodeEntry
{
    public required int Index { get; init; }
    public required QuestInfoEntry Info { get; init; }
}