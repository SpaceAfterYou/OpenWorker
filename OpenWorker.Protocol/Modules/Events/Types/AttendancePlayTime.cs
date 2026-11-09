using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Events.Types;

/// <summary>
/// The reward for the time played: the one the person is waiting for and how many seconds it has played towards it.
/// </summary>
public readonly struct AttendancePlayTime : IWritableData
{
    public required byte Step { get; init; }

    public required int Seconds { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Step);
        writer.Write(Seconds);
    }
}
