using OpenWorker.Gameplay.Modules.State;

namespace OpenWorker.Gameplay.Modules.Attendance;

/// <summary>
/// What bringing a book to the present changed.
/// </summary>
public readonly record struct AttendanceAdvance(bool HasTable, bool Attended, int Day, bool PlayTimeReset);

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

        if (playReset)
        {
            book.PlayStep = 0;
            book.PlaySeconds = 0;
            book.PlayDay = day;
        }

        if (!catalog.TryGetTable(now, out var table))
        {
            return new AttendanceAdvance(false, false, 0, playReset);
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
            return new AttendanceAdvance(true, false, book.Count, playReset);
        }

        book.Count++;
        book.LastDay = day;

        return new AttendanceAdvance(true, true, book.Count, playReset);
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
    /// Counts time towards the reward the person is waiting for. Returns the steps that were reached, which are
    /// at most the ones that are left.
    /// </summary>
    public static List<int> AddPlayTime(AttendanceBook book, AttendanceCatalog catalog, int seconds)
    {
        var reached = new List<int>();

        while (seconds > 0 && catalog.HasStep(book.PlayStep))
        {
            var need = catalog.PlayTime[book.PlayStep].Seconds - book.PlaySeconds;
            var take = Math.Min(seconds, need);

            book.PlaySeconds += take;
            seconds -= take;

            if (book.PlaySeconds < catalog.PlayTime[book.PlayStep].Seconds)
            {
                break;
            }

            reached.Add(book.PlayStep);
            book.PlayStep++;
            book.PlaySeconds = 0;
        }

        return reached;
    }
}
