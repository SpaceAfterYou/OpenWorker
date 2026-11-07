namespace OpenWorker.Gameplay.Modules.State;

/// <summary>
/// The game counts its days from a time of the morning, not from midnight.
/// </summary>
public static class GameDay
{
    /// <summary>
    /// The day that is running at <paramref name="now"/>: it started at the last <paramref name="reset"/> and lasts until the next.
    /// </summary>
    public static (DateTimeOffset Start, DateTimeOffset End) Get(DateTimeOffset now, TimeSpan reset)
    {
        var start = new DateTimeOffset(now.Date + reset, now.Offset);

        if (start > now)
        {
            start = start.AddDays(-1);
        }

        return (start, start.AddDays(1));
    }
}
