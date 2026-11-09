using OpenWorker.Hotspot.Modules.Persons.Types;

namespace OpenWorker.Gameplay.Modules.Npcs;

/// <summary>
/// The credit of a person with one group of NPCs.
/// </summary>
public sealed class NpcCreditEntry
{
    public int Group { get; set; }

    /// <summary>
    /// The grade reached, 0 to <see cref="NpcCreditTables.MaxGrade"/>.
    /// </summary>
    public byte Grade { get; set; }

    /// <summary>
    /// The credit gathered towards the next grade.
    /// </summary>
    public int Point { get; set; }

    /// <summary>
    /// The credit gathered on the day of <see cref="Day"/>, which the grade limits.
    /// </summary>
    public short DayPoint { get; set; }

    /// <summary>
    /// The start of the game day <see cref="DayPoint"/> counts, Unix seconds.
    /// </summary>
    public long Day { get; set; }

    /// <summary>
    /// When the credit last changed, Unix seconds.
    /// </summary>
    public long UpdatedAt { get; set; }

    public NpcCreditValue ToValue() => new()
    {
        Group = Group,
        Grade = Grade,
        Point = Point,
        CurrentPoint = DayPoint,
        UpdatedAt = UpdatedAt
    };
}

/// <summary>
/// An item a grade gives that did not fit the bags when the grade was reached: given when there is room.
/// </summary>
public sealed class NpcCreditReward
{
    public int Item { get; set; }

    public short Count { get; set; }
}

/// <summary>
/// The credit of a person with every group of NPCs it has dealt with. Kept as a document of the person
/// (<c>PersonStates</c>, kind <c>NpcCredit</c>).
/// </summary>
public sealed class NpcCreditBook
{
    public List<NpcCreditEntry> Groups { get; set; } = [];

    public List<NpcCreditReward> Rewards { get; set; } = [];

    public NpcCreditEntry? Find(int group) => Groups.Find(e => e.Group == group);

    /// <summary>
    /// Starts a new day for every group whose day has passed: what they gathered that day goes back to nothing.
    /// </summary>
    public bool StartDay(long today)
    {
        var changed = false;

        foreach (var entry in Groups.Where(e => e.Day < today))
        {
            entry.Day = today;
            entry.DayPoint = 0;
            changed = true;
        }

        return changed;
    }
}
