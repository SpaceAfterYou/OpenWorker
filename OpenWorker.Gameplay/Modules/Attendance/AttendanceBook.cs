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
    /// The reward for the time played that the person is waiting for: how many were taken today.
    /// </summary>
    public int PlayStep { get; set; }

    /// <summary>
    /// The time played today, which the client counts down against the time of each step.
    /// </summary>
    public int PlaySeconds { get; set; }

    /// <summary>
    /// The start of the game day the time was counted for.
    /// </summary>
    public DateTimeOffset PlayDay { get; set; }

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

    /// <summary>
    /// The client shows the step ready when the time of the step less these seconds is zero, and reads a time past the
    /// step as a wait of a million hours: the seconds told stop at the time of the step the person waits for.
    /// </summary>
    public AttendancePlayTime ToPlayTime(AttendanceCatalog catalog)
    {
        var seconds = catalog.HasStep(PlayStep) ? Math.Min(PlaySeconds, catalog.PlayTime[PlayStep].Seconds) : PlaySeconds;

        return new AttendancePlayTime { Step = (byte)PlayStep, Seconds = seconds };
    }
}
