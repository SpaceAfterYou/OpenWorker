using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Quests;

/// <summary>
/// The definitions of the quests, read from the rows of their tables.
/// </summary>
internal static class QuestEpisodeRowExtensions
{
    private const byte ItemsAsTheyAre = 1;
    private const byte ItemsByClass = 2;

    public static QuestDefinition ToDefinition(this QuestEpisodeRow row)
    {
        var conditions = new[] { row.Condition1, row.Condition2, row.Condition3, row.Condition4, row.Condition5, row.Condition6, row.Condition7, row.Condition8, row.Condition9, row.Condition10 }.TakeWhile(e => e != 0).ToArray();

        var before = new[] { row.PreviousEpisode1, row.PreviousEpisode2, row.PreviousEpisode3, row.PreviousEpisode4, row.PreviousEpisode5 }.Where(e => e != 0).ToArray();

        var given = new List<QuestItem>();

        if (row.AcceptGives1 == 1 && row.AcceptItem1 != 0)
        {
            given.Add(new QuestItem(row.AcceptItem1, 1));
        }

        if (row.AcceptGives2 == 1 && row.AcceptItem2 != 0)
        {
            given.Add(new QuestItem(row.AcceptItem2, 1));
        }

        var taken = Items(
            [row.TakenItem1, row.TakenItem2, row.TakenItem3, row.TakenItem4, row.TakenItem5],
            [row.TakenCount1, row.TakenCount2, row.TakenCount3, row.TakenCount4, row.TakenCount5]);

        return new QuestDefinition(
            row.Id,
            row.CompleteBit,
            Enum.IsDefined((QuestContents)row.Contents) ? (QuestContents)row.Contents : QuestContents.Side,
            row.Level,
            // A hero above 99 is a group of heroes in the tables of the server, which the client does not have.
            row.Hero is > 0 and < 100 ? row.Hero : 0,
            before,
            row.FinishesByItself == 1,
            row.CannotGiveUp == 1,
            given,
            taken,
            row.ToReward(),
            conditions)
        {
            Links = new[] { row.LinkEpisode1, row.LinkEpisode2, row.LinkEpisode3, row.LinkEpisode4, row.LinkEpisode5 }.Where(e => e != 0).ToArray(),
            StartsByItself = row.AutoStart == 1,
            StartNpc = row.StartObjectType == (byte)QuestConditionTarget.Npc ? row.StartObject : 0,
            FinishNpc = row.FinishObjectType == (byte)QuestConditionTarget.Npc ? row.FinishObject : 0
        };
    }

    private static QuestRewardDefinition ToReward(this QuestEpisodeRow row)
    {
        // A list of items is given as items when its column says 1 and by the class of the hero when it says 2; any other
        // value gives nothing.
        var items = row.RewardItemsBy is ItemsAsTheyAre or ItemsByClass
            ? Items(
                [row.RewardItem1, row.RewardItem2, row.RewardItem3, row.RewardItem4, row.RewardItem5],
                [row.RewardCount1, row.RewardCount2, row.RewardCount3, row.RewardCount4, row.RewardCount5])
            : [];

        var selectable = row.SelectItemsBy is ItemsAsTheyAre or ItemsByClass
            ? Items(
                [row.SelectItem1, row.SelectItem2, row.SelectItem3, row.SelectItem4, row.SelectItem5],
                [row.SelectCount1, row.SelectCount2, row.SelectCount3, row.SelectCount4, row.SelectCount5])
            : [];

        return new QuestRewardDefinition(
            row.RewardExperience,
            row.RewardGold,
            row.RewardEther,
            row.RewardBattlePoints,
            row.RewardPrefix,
            row.RewardSuffix,
            row.RewardHelper,
            row.RewardItemsBy == ItemsByClass,
            items,
            row.SelectItemsBy == ItemsByClass,
            selectable);
    }

    public static QuestConditionDefinition ToDefinition(this QuestConditionRow row)
    {
        var given = Items([row.GivenItem1, row.GivenItem2], [row.GivenCount1, row.GivenCount2]);
        var taken = Items([row.TakenItem1, row.TakenItem2], [row.TakenCount1, row.TakenCount2]);

        return new QuestConditionDefinition(
            row.Id,
            (QuestConditionType)row.Type,
            (QuestConditionTarget)row.Target,
            row.TargetId,
            row.Previous,
            Math.Max(1, (int)row.Count),
            row.Maze,
            given,
            taken);
    }

    /// <summary>
    /// The item the row gives to each hero, the first hero first.
    /// </summary>
    public static int[] Items(this QuestRewardRow row) => [row.HaruItem, row.ErwinItem, row.LilyItem, row.JinItem, row.StellaItem, row.IrisItem];

    /// <summary>
    /// The title a group of titles gives to each hero, the first hero first.
    /// </summary>
    public static int[] Titles(this TitleRewardRow row) => [row.HaruTitle, row.ErwinTitle, row.LilyTitle, row.JinTitle, row.StellaTitle, row.IrisTitle];

    /// <summary>
    /// The groups of the table that a monster counts for.
    /// </summary>
    public static int[] QuestGroups(this MonsterRow row) => new[] { row.QuestGroup1, row.QuestGroup2, row.QuestGroup3, row.QuestGroup4, row.QuestGroup5 }.Where(e => e != 0).ToArray();

    private static QuestItem[] Items(int[] items, int[] counts)
    {
        var result = new List<QuestItem>();

        for (var i = 0; i < items.Length; i++)
        {
            if (items[i] != 0 && counts[i] > 0)
            {
                result.Add(new QuestItem(items[i], (short)Math.Min(counts[i], short.MaxValue)));
            }
        }

        return result.ToArray();
    }
}
