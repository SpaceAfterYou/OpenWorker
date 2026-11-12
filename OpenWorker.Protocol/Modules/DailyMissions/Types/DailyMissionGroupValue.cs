using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.DailyMissions.Types;

/// <summary>
/// The missions of one district.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct DailyMissionGroupValue : IWritableData
{
    public int District { get; init; }

    [WireArray(WireLength.Signed16)]
    public required IReadOnlyCollection<DailyMissionValue> Missions { get; init; }
}
