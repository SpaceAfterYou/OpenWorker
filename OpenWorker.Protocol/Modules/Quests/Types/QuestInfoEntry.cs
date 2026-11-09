using System.Diagnostics;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Quests.Types;

public readonly struct QuestInfoEntry : IWritableData
{
    public required bool IsAddHelper { get; init; }
    public required short CompleteBit { get; init; }
    public required bool IsFailed { get; init; }
    public required IReadOnlyList<QuestCondition> Condition { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(IsAddHelper);
        writer.Write(CompleteBit);
        writer.Write(IsFailed);

        Debug.Assert(Condition.Count == QuestModuleDefines.ConditionsPerEpisode);

        foreach (var condition in Condition)
        {
            condition.Write(writer);
        }
    }
}
