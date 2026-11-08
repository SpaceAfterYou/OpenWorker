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
    Skills = 7
}
