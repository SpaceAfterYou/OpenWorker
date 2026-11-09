namespace OpenWorker.Gameplay.Modules.Progress;

/// <summary>
/// The kinds of event an achievement counts (the type of a row of <c>tb_Achievement</c>) that the server raises. The table
/// has more; those are listed in <c>docs/notes-person-progress.md</c> with the reason they do not count yet.
/// </summary>
public enum AchievementType : ushort
{
    /// <summary>
    /// A monster killed, about its number and, as a second event, about its type.
    /// </summary>
    MonsterKilled = 19,

    /// <summary>
    /// A monster of the lowest rank killed.
    /// </summary>
    WeakMonsterKilled = 20,

    NormalMonsterKilled = 21,
    StrongMonsterKilled = 22,
    BossKilled = 23,
    RaidBossKilled = 24,

    /// <summary>
    /// A monster killed, about its faction.
    /// </summary>
    FactionKilled = 25,

    /// <summary>
    /// The longest combo; the counter keeps the highest value. Nothing raises it yet.
    /// </summary>
    Combo = 27,

    MainQuestFinished = 28,
    RepeatQuestFinished = 29,
    SideQuestFinished = 30,

    /// <summary>
    /// Gold picked up in a maze.
    /// </summary>
    GoldPicked = 31,

    /// <summary>
    /// The level of the person; the counter is the level.
    /// </summary>
    LevelReached = 32,

    /// <summary>
    /// A maze cleared, about the maze and, as a second event, about any maze.
    /// </summary>
    MazeCleared = 35,

    /// <summary>
    /// A maze cleared within three minutes.
    /// </summary>
    MazeClearedFast = 36,

    /// <summary>
    /// A monster of the middle-boss rank killed.
    /// </summary>
    MiddleBossKilled = 44,

    /// <summary>
    /// Ether picked up in a maze.
    /// </summary>
    EtherPicked = 45,

    /// <summary>
    /// A maze of a kind other than the plain one cleared, about the kind.
    /// </summary>
    SpecialMazeCleared = 47,

    /// <summary>
    /// A helper gained, about the helper.
    /// </summary>
    HelperGained = 48
}
