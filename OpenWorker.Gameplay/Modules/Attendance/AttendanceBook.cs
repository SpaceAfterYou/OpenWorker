using System.Text.Json.Serialization;
using OpenWorker.Hotspot.Modules.Events.Enums;
using OpenWorker.Hotspot.Modules.Events.Types;

namespace OpenWorker.Gameplay.Modules.Attendance;

/// <summary>
/// The days a person has come in the current table and the time it has played today. This is what is stored.
/// </summary>
public sealed class AttendanceBook
{
    /// <summary>
    /// The table the days belong to; a new table starts the count again.
    /// </summary>
    public int Table { get; set; }

    public int Count { get; set; }

    /// <summary>
    /// The start of the game day the person came on last.
    /// </summary>
    public DateTimeOffset LastDay { get; set; }

    /// <summary>
    /// The reward for the time played that the person is waiting for.
    /// </summary>
    public int PlayStep { get; set; }

    public int PlaySeconds { get; set; }

    /// <summary>
    /// The start of the game day the time was counted for.
    /// </summary>
    public DateTimeOffset PlayDay { get; set; }

    /// <summary>
    /// Writes of the book to the database go one after another, so the last one has the latest state.
    /// </summary>
    [JsonIgnore]
    public SemaphoreSlim Gate { get; } = new(1, 1);

    public AttendanceInfo ToInfo()
    {
        var state = new AttendanceState[AttendanceInfo.Days];

        for (var day = 0; day < state.Length; day++)
        {
            state[day] = day < Count ? AttendanceState.Taken : AttendanceState.Available;
        }

        return new AttendanceInfo
        {
            CurrentDay = (byte)Math.Min(Count, AttendanceInfo.Days),
            State = state,
            Table = Table
        };
    }

    public AttendancePlayTime ToPlayTime() => new() { Step = (byte)PlayStep, Seconds = PlaySeconds };
}
