namespace OpenWorker.Hotspot.Modules.Events.Types;

public struct AttendancePlayTime
{
    public required byte Step { get; init; }
    public required TimeSpan Played { get; init; }
}