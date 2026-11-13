using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.DailyMissions.Types;

/// <summary>
/// The missions of one district.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct DailyMissionGroupValue
{
    public int District { get; init; }

    [WireArray(WireLength.Signed16)]
    public required IReadOnlyCollection<DailyMissionValue> Missions { get; init; }
}
