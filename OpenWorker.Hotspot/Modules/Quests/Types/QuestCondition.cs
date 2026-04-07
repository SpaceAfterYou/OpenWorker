using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Quests.Types;

public readonly struct QuestCondition : IWritableData
{
    public required int Condition { get; init; }
    public required byte Step { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Condition);
        writer.Write(Step);
    }
}
