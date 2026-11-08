using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Quests;

/// <summary>
/// Which column of a table is what, for the tables of quests. The columns are named by position in the generated rows.
/// </summary>
internal static class QuestEpisodeRowExtensions
{
    public static QuestDefinition ToDefinition(this QuestEpisodeRow row)
    {
        var conditions = new[] { row.Field96, row.Field97, row.Field98, row.Field99, row.Field100, row.Field101, row.Field102, row.Field103, row.Field104, row.Field105 }.TakeWhile(e => e != 0).ToArray();

        // The columns before the conditions are, in order: the bit of the finished list (1), the kind (4),
        // five episodes that come first (6..10), the level (17), the hero (18), how it ends (24),
        // whether it can be given up (39), the items given with it (46..49) and the items taken when it ends (50..59).
        var before = new[] { row.Field6, row.Field7, row.Field8, row.Field9, row.Field10 }.Where(e => e != 0).ToArray();

        var given = new List<QuestItem>();

        if (row.Field46 == 1 && row.Field48 != 0)
        {
            given.Add(new QuestItem(row.Field48, 1));
        }

        if (row.Field47 == 1 && row.Field49 != 0)
        {
            given.Add(new QuestItem(row.Field49, 1));
        }

        var taken = Items(
            [row.Field50, row.Field51, row.Field52, row.Field53, row.Field54],
            [row.Field55, row.Field56, row.Field57, row.Field58, row.Field59]);

        return new QuestDefinition(
            row.Id,
            row.Field1,
            Enum.IsDefined((QuestContents)row.Field4) ? (QuestContents)row.Field4 : QuestContents.Side,
            row.Field17,
            // A hero above 99 is a group of heroes in the tables of the server, which the client does not have.
            row.Field18 is > 0 and < 100 ? row.Field18 : 0,
            before,
            row.Field24 == 1,
            row.Field39 == 1,
            given,
            taken,
            row.ToReward(),
            conditions);
    }

    private static QuestRewardDefinition ToReward(this QuestEpisodeRow row)
    {
        // Experience (62), money (63), ether (64), battle points (65), titles (66, 67), how the items are given (68)
        // with their numbers (69..73) and counts (74..78), the same for the item to choose (79, 80..84, 85..89), the helper (94).
        var items = Items(
            [row.Field69, row.Field70, row.Field71, row.Field72, row.Field73],
            [row.Field74, row.Field75, row.Field76, row.Field77, row.Field78]);

        var selectable = Items(
            [row.Field80, row.Field81, row.Field82, row.Field83, row.Field84],
            [row.Field85, row.Field86, row.Field87, row.Field88, row.Field89]);

        return new QuestRewardDefinition(
            row.Field62,
            row.Field63,
            row.Field64,
            row.Field65,
            row.Field66,
            row.Field67,
            row.Field94,
            row.Field68 == 2,
            items,
            row.Field79 == 2,
            selectable);
    }

    public static QuestConditionDefinition ToDefinition(this QuestConditionRow row)
    {
        // The kind (3), the condition that has to be done first (4), the kind of target (6) and the target (7),
        // the first maze (11), how many times (21), the items given (22..25) and taken (26..29) when it is done.
        var given = Items([row.Field22, row.Field23], [row.Field24, row.Field25]);
        var taken = Items([row.Field26, row.Field27], [row.Field28, row.Field29]);

        return new QuestConditionDefinition(
            row.Id,
            (QuestConditionType)row.Field3,
            (QuestConditionTarget)row.Field6,
            row.Field7,
            row.Field4,
            Math.Max(1, (int)row.Field21),
            row.Field11,
            given,
            taken);
    }

    /// <summary>
    /// The item the row gives to each hero, the first hero first.
    /// </summary>
    public static int[] Items(this QuestRewardRow row) => [row.Field1, row.Field2, row.Field3, row.Field4, row.Field5, row.Field6];

    /// <summary>
    /// The groups of the table that a monster counts for.
    /// </summary>
    public static int[] QuestGroups(this MonsterRow row) => new[] { row.Field9, row.Field10, row.Field11, row.Field12, row.Field13 }.Where(e => e != 0).ToArray();

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
