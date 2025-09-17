using OpenWorker.Hotspot.Modules.DailyMissions.Enums;

namespace OpenWorker.Hotspot.Modules.DailyMissions.Types;

public readonly struct DailyMissionValue
{
    public int Mission { get; init; }
    public DailyMissionType Type { get; init; }
    public DailyMissionState State { get; init; }
    public DailyMissionHelperState Helper { get; init; }
    public short Value { get; init; }
    public DateTimeOffset Accept { get; init; }
    public DateTimeOffset Start { get; init; }
    public DateTimeOffset End { get; init; }
}