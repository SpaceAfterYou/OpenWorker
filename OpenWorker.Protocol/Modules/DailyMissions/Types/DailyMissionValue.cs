using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.DailyMissions.Enums;

namespace OpenWorker.Hotspot.Modules.DailyMissions.Types;

/// <summary>
/// A mission as the client reads it: 41 bytes.
/// </summary>
public readonly struct DailyMissionValue : IWritableData
{
    public int Mission { get; init; }

    public DailyMissionType Type { get; init; }

    public DailyMissionState State { get; init; }

    public DailyMissionHelperState Helper { get; init; }

    /// <summary>
    /// How far the person has got.
    /// </summary>
    public short Value { get; init; }

    public DateTimeOffset Accept { get; init; }

    public DateTimeOffset Start { get; init; }

    public DateTimeOffset End { get; init; }

    /// <summary>
    /// Seconds until the mission opens, 0 if it is open.
    /// </summary>
    public int Remain { get; init; }

    /// <summary>
    /// Seconds the mission stays open.
    /// </summary>
    public int Duration { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Mission);
        writer.Write((byte)Type);
        writer.Write((byte)State);
        writer.Write((byte)Helper);
        writer.Write(Value);
        writer.Write(Accept.ToUnixTimeSeconds());
        writer.Write(Start.ToUnixTimeSeconds());
        writer.Write(End.ToUnixTimeSeconds());
        writer.Write(Remain);
        writer.Write(Duration);
    }
}
