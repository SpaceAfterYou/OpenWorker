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
    SharePoints = 10
}
