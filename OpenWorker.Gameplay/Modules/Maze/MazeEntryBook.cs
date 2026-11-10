namespace OpenWorker.Gameplay.Modules.Maze;

/// <summary>
/// What a person has done in the mazes since the game day began: how often it entered each maze that limits entering, how
/// often it entered each group of mazes that share a limit, and how many free revives it used. A new game day clears it
/// (<see cref="MazeEntryLimits"/>). Kept as a document of the person (<c>PersonStates</c>, kind <c>MazeEntry</c>).
/// </summary>
public sealed class MazeEntryBook
{
    /// <summary>
    /// The start of the game day the counts are of, in UTC; a document that is older is of a day that is over.
    /// </summary>
    public DateTime Day { get; set; }

    /// <summary>
    /// The entries of each maze that counts alone today.
    /// </summary>
    public Dictionary<int, int> Mazes { get; set; } = [];

    /// <summary>
    /// The entries of each group of mazes that share a limit today.
    /// </summary>
    public Dictionary<int, int> Groups { get; set; } = [];

    /// <summary>
    /// The free revives the person used today.
    /// </summary>
    public int FreeRevives { get; set; }

    /// <summary>
    /// Clears the counts for the day that starts at <paramref name="day"/>; true when there was anything to clear.
    /// </summary>
    public bool StartDay(DateTime day)
    {
        var changed = Mazes.Count > 0 || Groups.Count > 0 || FreeRevives != 0 || Day != day;

        Mazes.Clear();
        Groups.Clear();
        FreeRevives = 0;
        Day = day;

        return changed;
    }
}
