namespace OpenWorker.Gameplay.Modules.Tower;

/// <summary>
/// How far a person has climbed the infinite tower and how many chapters it went up today. Kept as a document of the person
/// (<c>PersonStates</c>, kind <c>InfiniteTower</c>); a new game day clears the count (<see cref="InfiniteTower"/>).
/// </summary>
public sealed class InfiniteTowerBook
{
    /// <summary>
    /// The last chapter whose five stages the person has cleared, 0 for none. The next chapter is the highest it may enter.
    /// </summary>
    public int ClearChapter { get; set; }

    /// <summary>
    /// The highest stage the person has cleared, counted over the whole tower: stage + 5 × (chapter − 1).
    /// </summary>
    public int ClearStage { get; set; }

    /// <summary>
    /// The start of the game day <see cref="Entries"/> is of, in UTC.
    /// </summary>
    public DateTime Day { get; set; }

    /// <summary>
    /// How many chapters the person has entered today (each entry into the first stage of a chapter).
    /// </summary>
    public int Entries { get; set; }

    /// <summary>
    /// Starts the game day that begins at <paramref name="day"/>: the count clears. True when anything changed.
    /// </summary>
    public bool StartDay(DateTime day)
    {
        var changed = Entries != 0 || Day != day;

        Entries = 0;
        Day = day;

        return changed;
    }
}
