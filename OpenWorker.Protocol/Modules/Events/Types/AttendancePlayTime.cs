using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Events.Types;

/// <summary>
/// The reward for the time played: the one the person is waiting for and how many seconds it has played towards it.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct AttendancePlayTime : IWritableData
{
    public required byte Step { get; init; }

    public required int Seconds { get; init; }
}
