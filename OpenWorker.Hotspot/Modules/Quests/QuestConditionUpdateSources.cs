using OpenWorker.Hotspot.Modules.Quests.Enums;

namespace OpenWorker.Hotspot.Modules.Quests;

/// <summary>
/// Reverse-engineered mapping from the original GameServer:
/// which gameplay events call quest condition update handlers.
/// </summary>
public static class QuestConditionUpdateSources
{
    /// <summary>
    /// Generic condition updates routed through UpdateCondition(type, target, objectId, count, ...).
    /// </summary>
    public static readonly QuestConditionUpdateSource[] GeneralUpdates =
    [
        new(QuestConditionType.Trigger, QuestConditionTarget.Object, "Interaction box click"),
        new(QuestConditionType.Hunt, QuestConditionTarget.Monster, "Monster kill"),
        new(QuestConditionType.Hunt, QuestConditionTarget.MonsterGroup, "Monster kill by quest group"),
        new(QuestConditionType.Hunt, QuestConditionTarget.Maze, "Monster kill in maze"),
        new(QuestConditionType.Collect, QuestConditionTarget.Item, "Item added to inventory"),
        new(QuestConditionType.Clear, QuestConditionTarget.Maze, "Maze reward / maze clear"),
        new(QuestConditionType.Clear, QuestConditionTarget.Event, "Hidden event or daily event complete"),
        new(QuestConditionType.Buy, QuestConditionTarget.Item, "Shop purchase"),
        new(QuestConditionType.Sell, QuestConditionTarget.Item, "Shop sell"),
        new(QuestConditionType.Make, QuestConditionTarget.Item, "Crafting"),
        new(QuestConditionType.Upgrade, QuestConditionTarget.Item, "Item upgrade"),
        new(QuestConditionType.Skill, QuestConditionTarget.SkillDeck, "Skill deck / bonus event"),
        new(QuestConditionType.Disassemble, QuestConditionTarget.Item, "Dismantle"),
        new(QuestConditionType.AkashicMake, QuestConditionTarget.Item, "Akashic crafting"),
        new(QuestConditionType.Cultivation, QuestConditionTarget.Item, "Cultivation action"),
        new(QuestConditionType.Harvest, QuestConditionTarget.Item, "Harvest action"),
        new(QuestConditionType.HuntAll, QuestConditionTarget.Monster, "Battle zone mass hunt")
    ];

    /// <summary>
    /// Maze-scoped condition updates via UpdateMazeCondition(..., mazeId, ...).
    /// </summary>
    public static readonly QuestConditionUpdateSource[] MazeUpdates =
    [
        new(QuestConditionType.Clear, QuestConditionTarget.Sector, "Sector clear in maze")
    ];

    /// <summary>
    /// Maze mode terminal updates via UpdateMazeGameMode(type, mazeId, ...).
    /// </summary>
    public static readonly QuestConditionType[] MazeGameModeUpdates =
    [
        QuestConditionType.Protect,  // Defence mode end
        QuestConditionType.Survival  // Survival mode end
    ];
}

public readonly record struct QuestConditionUpdateSource(
    QuestConditionType Type,
    QuestConditionTarget Target,
    string Source);
