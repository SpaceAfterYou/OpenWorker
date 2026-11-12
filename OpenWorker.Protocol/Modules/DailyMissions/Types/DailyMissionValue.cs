using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.DailyMissions.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.DailyMissions.Types;

/// <summary>
/// A mission as the client reads it: 41 bytes.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct DailyMissionValue : IWritableData
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
}
