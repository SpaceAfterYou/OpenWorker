using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Events.Types;

/// <summary>
/// The reward for the time played: the one the person is waiting for and how many seconds it has played today. The
/// client shows the time of the step less these seconds and counts it down by itself.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct AttendancePlayTime
{
    public required byte Step { get; init; }

    public required int Seconds { get; init; }
}
