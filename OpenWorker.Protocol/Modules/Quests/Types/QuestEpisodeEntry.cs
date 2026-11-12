using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Quests.Types;

[WireMessage(WireMethods.Write)]
public readonly partial struct QuestEpisodeEntry
{
    public required int Episode { get; init; }
    public required QuestInfoEntry Info { get; init; }
}
