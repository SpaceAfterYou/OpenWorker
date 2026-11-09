namespace OpenWorker.Gameplay.Modules.Persistence;

/// <summary>
/// The parts of a person that are written separately. The code that changes one marks it (<see cref="PersonDirty"/>);
/// the next flush writes the marked parts as they are then.
/// </summary>
[Flags]
public enum PersonSections : ushort
{
    None = 0,

    /// <summary>
    /// Where the person stands in a district. Nobody marks it: the flush compares the place with the one it wrote last.
    /// </summary>
    Place = 1 << 0,

    Currencies = 1 << 1,

    /// <summary>
    /// Level and experience.
    /// </summary>
    Level = 1 << 2,

    /// <summary>
    /// Learned skills, decks and skill points.
    /// </summary>
    Skills = 1 << 3,

    Quests = 1 << 4,

    /// <summary>
    /// Items, how far the storages are extended, the quick slots.
    /// </summary>
    Inventory = 1 << 5,

    /// <summary>
    /// Soul metry, daily missions, boosters, attendance, helpers.
    /// </summary>
    MiscState = 1 << 6,

    /// <summary>
    /// The cards in the akashic record book.
    /// </summary>
    AkashicRecords = 1 << 7,

    /// <summary>
    /// Achievements and the points of districts and groups of mazes.
    /// </summary>
    Progress = 1 << 8,

    /// <summary>
    /// The parts kept in the row of the person.
    /// </summary>
    Row = Currencies | Level | Inventory
}
