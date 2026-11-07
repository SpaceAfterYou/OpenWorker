using OpenWorker.Hotspot.Modules.Quests.Enums;

namespace OpenWorker.Gameplay.Modules.Quests;

/// <summary>
/// The kind of an episode, as the table says.
/// </summary>
public enum QuestContents : byte
{
    Main = 0,
    Side = 1,
    Repeat = 2,
    Helper = 3
}

/// <summary>
/// An item and how many of it.
/// </summary>
public readonly record struct QuestItem(int Item, short Count);

/// <summary>
/// What an episode gives when it is finished.
/// </summary>
/// <param name="Experience">Experience points.</param>
/// <param name="Gold">Money.</param>
/// <param name="Ether">Ether.</param>
/// <param name="BattlePoints">Battle points.</param>
/// <param name="Prefix">The title that goes before the name, or 0.</param>
/// <param name="Suffix">The title that goes after the name, or 0.</param>
/// <param name="Helper">The helper the person gets, or 0.</param>
/// <param name="ItemsByClass">Whether the numbers in <see cref="Items"/> are rows of the table that has an item for each hero.</param>
/// <param name="Items">What is given.</param>
/// <param name="SelectByClass">Whether the numbers in <see cref="Selectable"/> are rows of the table that has an item for each hero.</param>
/// <param name="Selectable">What the person may choose one thing from.</param>
public sealed record QuestRewardDefinition(
    int Experience,
    int Gold,
    int Ether,
    int BattlePoints,
    int Prefix,
    int Suffix,
    int Helper,
    bool ItemsByClass,
    IReadOnlyList<QuestItem> Items,
    bool SelectByClass,
    IReadOnlyList<QuestItem> Selectable)
{
    public static QuestRewardDefinition None { get; } = new(0, 0, 0, 0, 0, 0, 0, false, [], false, []);
}

/// <summary>
/// An episode of a quest.
/// </summary>
/// <param name="Id">The number the client knows it by.</param>
/// <param name="CompleteBit">The place of the episode in the list of the finished ones.</param>
/// <param name="Contents">The kind of the episode.</param>
/// <param name="Level">The level the person needs to accept it.</param>
/// <param name="Hero">The hero who can accept it, or 0 for everyone.</param>
/// <param name="Before">The episodes that have to be finished first.</param>
/// <param name="FinishesByItself">Whether it ends when its last condition is done, without going back to anyone.</param>
/// <param name="CannotGiveUp">Whether the person is not allowed to give it up.</param>
/// <param name="GivenOnAccept">The items the person gets with it.</param>
/// <param name="TakenOnFinish">The items taken from the person when it is finished.</param>
/// <param name="Reward">What it gives.</param>
/// <param name="Conditions">The conditions, in the order the client shows them.</param>
public sealed record QuestDefinition(
    int Id,
    int CompleteBit,
    QuestContents Contents,
    int Level,
    int Hero,
    IReadOnlyList<int> Before,
    bool FinishesByItself,
    bool CannotGiveUp,
    IReadOnlyList<QuestItem> GivenOnAccept,
    IReadOnlyList<QuestItem> TakenOnFinish,
    QuestRewardDefinition Reward,
    IReadOnlyList<int> Conditions)
{
    /// <summary>
    /// The bits that mean every condition is done.
    /// </summary>
    public short AllDone => (short)((1 << Conditions.Count) - 1);
}

/// <summary>
/// One step of an episode.
/// </summary>
/// <param name="Id">The number the client knows it by.</param>
/// <param name="Type">What the person has to do.</param>
/// <param name="Target">What kind of thing it is done to.</param>
/// <param name="TargetId">Which thing. 0 means any, for the kinds of conditions that allow it.</param>
/// <param name="Previous">The condition that has to be done before this one counts, or 0.</param>
/// <param name="Counter">How many times it has to be done.</param>
/// <param name="Maze">The maze it has to be done in, or 0.</param>
/// <param name="GivenOnDone">The items the person gets when it is done.</param>
/// <param name="TakenOnDone">The items taken from the person when it is done.</param>
public sealed record QuestConditionDefinition(
    int Id,
    QuestConditionType Type,
    QuestConditionTarget Target,
    int TargetId,
    int Previous,
    int Counter,
    int Maze,
    IReadOnlyList<QuestItem> GivenOnDone,
    IReadOnlyList<QuestItem> TakenOnDone);
