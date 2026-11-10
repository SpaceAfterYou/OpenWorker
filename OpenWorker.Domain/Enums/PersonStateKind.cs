namespace OpenWorker.Domain.Enums;

/// <summary>
/// Which part of the person the state document belongs to. The values are stored, so they never change.
/// </summary>
public enum PersonStateKind : byte
{
    SoulMetry = 1,
    DailyMission = 2,
    Booster = 3,
    Attendance = 4,
    Helper = 5,
    Quest = 6,

    /// <summary>
    /// The skills the person has learned, its decks and its skill points.
    /// </summary>
    Skills = 7,

    /// <summary>
    /// The cards the person has put into its akashic record book.
    /// </summary>
    AkashicRecords = 8,

    /// <summary>
    /// How far each achievement has come and which rewards of achievements are taken.
    /// </summary>
    Achievements = 9,

    /// <summary>
    /// The points of the districts and of the groups of mazes, and the rewards taken for them.
    /// </summary>
    SharePoints = 10,

    /// <summary>
    /// The credit of the person with each group of NPCs of the cities.
    /// </summary>
    NpcCredit = 11,

    /// <summary>
    /// The cooldowns of item groups that outlast a server and how often the kinds of item limited for a day were used.
    /// </summary>
    ItemUse = 12,

    /// <summary>
    /// How often the person has entered each maze today, and how many free revives it has used.
    /// </summary>
    MazeEntry = 13,

    /// <summary>
    /// The buffs the person carries from one server to the next and through a relog, with the time they have left.
    /// </summary>
    Buffs = 14
}
