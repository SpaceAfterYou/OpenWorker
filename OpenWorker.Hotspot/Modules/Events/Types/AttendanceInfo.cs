using OpenWorker.Hotspot.Modules.Events.Enums;

namespace OpenWorker.Hotspot.Modules.Events.Types;

public struct AttendanceInfo
{
    public required byte CurrentDay { get; init; }
    public required AttendanceState[] State { get; init; }
    public required int Table { get; init; }
}