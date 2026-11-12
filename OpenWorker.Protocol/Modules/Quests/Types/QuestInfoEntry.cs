using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Quests.Types;

[WireMessage(WireMethods.Write)]
public readonly partial struct QuestInfoEntry
{
    public required bool IsAddHelper { get; init; }
    public required short CompleteBit { get; init; }
    public required bool IsFailed { get; init; }

    /// <summary>
    /// The client reads <see cref="QuestModuleDefines.ConditionsPerEpisode"/> conditions without a count.
    /// </summary>
    [WireArray(Count = QuestModuleDefines.ConditionsPerEpisode)]
    public required IReadOnlyList<QuestCondition> Condition { get; init; }
}
