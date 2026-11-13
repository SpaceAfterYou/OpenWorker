namespace OpenWorker.Gameplay.Modules.Progress;

/// <summary>
/// The kinds of event an achievement counts (the type of a row of <c>tb_Achievement</c>) that the server raises. The table
/// has more, which do not count yet.
/// </summary>
public enum AchievementType : ushort
{
    /// <summary>
    /// An item used up: every use that starts the cooldown of the item.
    /// </summary>
    ItemUsed = 15,

    /// <summary>
    /// A purchase at a shop, once for every request whatever the count of the items.
    /// </summary>
    ShopPurchase = 16,

    /// <summary>
    /// Gold spent on repairs.
    /// </summary>
    RepairCost = 17,

    /// <summary>
    /// A death of the person.
    /// </summary>
    Death = 18,

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
    /// A monster hit by a skill of the person, once for every monster of every attack.
    /// </summary>
    SkillUsed = 26,

    /// <summary>
    /// The longest combo; the counter keeps the highest value.
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
    /// Battle points gained, from whatever source.
    /// </summary>
    BattlePointsGained = 33,

    /// <summary>
    /// A maze cleared, about the maze and, as a second event, about any maze.
    /// </summary>
    MazeCleared = 35,

    /// <summary>
    /// A maze cleared within three minutes.
    /// </summary>
    MazeClearedFast = 36,

    /// <summary>
    /// A maze cleared at rank SS; the five types after it are the ranks below, down to F.
    /// </summary>
    MazeClearedRankSS = 37,

    MazeClearedRankS = 38,

    MazeClearedRankA = 39,

    MazeClearedRankB = 40,

    MazeClearedRankC = 41,

    MazeClearedRankF = 42,

    /// <summary>
    /// The minutes the person has played, counted whole.
    /// </summary>
    PlayTime = 43,

    /// <summary>
    /// A monster of the middle-boss rank killed.
    /// </summary>
    MiddleBossKilled = 44,

    /// <summary>
    /// Ether picked up in a maze.
    /// </summary>
    EtherPicked = 45,

    /// <summary>
    /// A stage of the infinite tower entered.
    /// </summary>
    TowerStageEntered = 46,

    /// <summary>
    /// A maze of a kind other than the plain one cleared, about the kind.
    /// </summary>
    SpecialMazeCleared = 47,

    /// <summary>
    /// A helper gained, about the helper.
    /// </summary>
    HelperGained = 48,

    /// <summary>
    /// An upgrade of an item tried, whatever came of it.
    /// </summary>
    UpgradeTried = 49,

    /// <summary>
    /// An upgrade of an item that did not go up.
    /// </summary>
    UpgradeFailed = 50,

    /// <summary>
    /// A request to make items, once whatever the count.
    /// </summary>
    ItemMade = 51,

    /// <summary>
    /// A request to take items apart, once whatever the count.
    /// </summary>
    ItemDisassembled = 52,

    /// <summary>
    /// A daily mission finished, about the kind of mission (0).
    /// </summary>
    MissionDone = 53,

    /// <summary>
    /// The room of a friend entered.
    /// </summary>
    RoomVisited = 54,

    /// <summary>
    /// A purchase at the exchange, once for every request whatever the count.
    /// </summary>
    ExchangePurchase = 55,

    /// <summary>
    /// A ride of an NPC of a transport.
    /// </summary>
    TransportTaken = 56,

    /// <summary>
    /// The credit points gained with a group of NPCs, about the group.
    /// </summary>
    NpcCredit = 57
}
