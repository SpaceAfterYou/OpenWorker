using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Quests.Types;

public readonly struct QuestEpisodeEntry : IWritableData
{
    public required int Episode { get; init; }
    public required QuestInfoEntry Info { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Episode);

        Info.Write(writer);
    }
}
