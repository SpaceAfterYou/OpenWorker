using OpenWorker.Gameplay.Modules.State;

namespace OpenWorker.Gameplay.Modules.Attendance;

/// <summary>
/// What bringing a book to the present changed. <paramref name="Unclaimed"/> are the play time steps the day that ended
/// had reached and the person had not claimed.
/// </summary>
public readonly record struct AttendanceAdvance(bool HasTable, bool Attended, int Day, bool PlayTimeReset, int[] Unclaimed);

public static class AttendanceRules
{
    /// <summary>
    /// The day of attendance starts at this time.
    /// </summary>
    public static readonly TimeSpan DefaultResetTime = TimeSpan.FromHours(9);

    /// <summary>
    /// Brings the book to the game day of <paramref name="now"/>: the person has come on this day, the table is
    /// the one of the month, and the time played counts from the start of the day.
    /// </summary>
    public static AttendanceAdvance Advance(AttendanceBook book, AttendanceCatalog catalog, DateTimeOffset now, TimeSpan reset)
    {
        var (day, _) = GameDay.Get(now, reset);

        var playReset = book.PlayDay < day;
        int[] unclaimed = [];

        if (playReset)
        {
            unclaimed = Reached(book, catalog);
            book.PlayStep = 0;
            book.PlaySeconds = 0;
            book.PlayDay = day;
        }

        if (!catalog.TryGetTable(now, out var table))
        {
            return new AttendanceAdvance(false, false, 0, playReset, unclaimed);
        }

        // A new table starts the count again.
        if (book.Table != table.Id)
        {
            book.Table = table.Id;
            book.Count = 0;
            book.LastDay = default;
        }

        if (book.LastDay >= day || book.Count >= table.Days)
        {
            return new AttendanceAdvance(true, false, book.Count, playReset, unclaimed);
        }

        book.Count++;
        book.LastDay = day;

        return new AttendanceAdvance(true, true, book.Count, playReset, unclaimed);
    }

    /// <summary>
    /// Takes back the day that was just counted, which is what happens when its reward could not be given.
    /// </summary>
    public static void Undo(AttendanceBook book, DateTimeOffset previousLastDay)
    {
        book.Count--;
        book.LastDay = previousLastDay;
    }

    /// <summary>
    /// Counts time played today, up to the time of the last step. Returns whether the count changed.
    /// </summary>
    public static bool AddPlayTime(AttendanceBook book, AttendanceCatalog catalog, int seconds)
    {
        var last = catalog.LastPlayTimeSeconds;

        if (seconds <= 0 || book.PlaySeconds >= last)
        {
            return false;
        }

        book.PlaySeconds = (int)Math.Min((long)book.PlaySeconds + seconds, last);

        return true;
    }

    /// <summary>
    /// The steps from the one the person waits for that the time of the day has reached.
    /// </summary>
    public static int[] Reached(AttendanceBook book, AttendanceCatalog catalog)
    {
        var reached = new List<int>();

        for (var step = book.PlayStep; catalog.HasStep(step) && book.PlaySeconds >= catalog.PlayTime[step].Seconds; step++)
        {
            reached.Add(step);
        }

        return [.. reached];
    }

    /// <summary>
    /// The step the person waits for is reached: the time of the day is at its mark.
    /// </summary>
    public static bool IsReady(AttendanceBook book, AttendanceCatalog catalog)
    {
        return catalog.HasStep(book.PlayStep) && book.PlaySeconds >= catalog.PlayTime[book.PlayStep].Seconds;
    }
}
