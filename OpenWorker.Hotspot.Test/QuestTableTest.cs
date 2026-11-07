using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Hotspot.Modules.Quests;
using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Checks the code against the tables of the client: set OPENWORKER_TEST_RES to the folder with the *.res files.
/// What the columns mean was found from their values, and these tests are what keeps the guess honest.
/// </summary>
[TestClass]
public sealed class QuestTableTest
{
    private static QuestCatalog Catalog { get; set; } = null!;

    private static System.Collections.ObjectModel.ReadOnlyCollection<QuestEpisodeRow> Episodes { get; set; } = null!;

    private static System.Collections.ObjectModel.ReadOnlyCollection<QuestConditionRow> Conditions { get; set; } = null!;

    private static System.Collections.ObjectModel.ReadOnlyCollection<QuestRewardRow> Rewards { get; set; } = null!;

    [ClassInitialize]
    public static void Load(TestContext context)
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENWORKER_TEST_RES")))
        {
            return;
        }

        Episodes = TestTables.Load<QuestEpisodeRow>("tb_Quest_Episode");
        Conditions = TestTables.Load<QuestConditionRow>("tb_Quest_Condition");
        Rewards = TestTables.Load<QuestRewardRow>("tb_Quest_Reward");

        Catalog = new QuestCatalog(Episodes, Conditions, Rewards, TestTables.Load<MonsterRow>("tb_Monster"));
    }

    [TestMethod]
    public void EveryEpisodeHasConditionsThatExist()
    {
        TestTables.Load<QuestEpisodeRow>("tb_Quest_Episode");

        foreach (var quest in Catalog.Episodes)
        {
            Assert.IsNotEmpty(quest.Conditions, $"episode {quest.Id} has no conditions");
            Assert.IsLessThanOrEqualTo(QuestModuleDefines.ConditionsPerEpisode, quest.Conditions.Count);

            foreach (var condition in quest.Conditions)
            {
                Assert.IsTrue(Catalog.TryGetCondition(condition, out _), $"episode {quest.Id} names the condition {condition}");
            }
        }
    }

    [TestMethod]
    public void ConditionsAreListedWithoutGaps()
    {
        TestTables.Load<QuestEpisodeRow>("tb_Quest_Episode");

        foreach (var row in Episodes)
        {
            var ids = new[] { row.Field96, row.Field97, row.Field98, row.Field99, row.Field100, row.Field101, row.Field102, row.Field103, row.Field104, row.Field105 };
            var used = ids.TakeWhile(e => e != 0).Count();

            Assert.IsTrue(ids.Skip(used).All(e => e == 0), $"episode {row.Id} has a gap among its conditions");
        }
    }

    [TestMethod]
    public void AnEpisodeFindsItsConditionsInTheTableOrStopsThere()
    {
        TestTables.Load<QuestEpisodeRow>("tb_Quest_Episode");

        // The table names a condition the other table lacks, and the episode stops counting at it.
        var named = Episodes.Single(e => e.Id == 100165);

        Assert.AreEqual(1001652, named.Field97);
        Assert.IsTrue(Catalog.TryGetEpisode(100165, out var quest));
        CollectionAssert.AreEqual(new[] { named.Field96 }, quest.Conditions.ToArray());
    }

    [TestMethod]
    public void TheBitsOfFinishedEpisodesAreDistinctAndFit()
    {
        TestTables.Load<QuestEpisodeRow>("tb_Quest_Episode");

        var bits = Catalog.Episodes.Select(e => e.CompleteBit).ToList();

        Assert.AreEqual(bits.Count, bits.Distinct().Count());
        Assert.IsTrue(bits.All(e => e is > 0 and < 1024));
    }

    [TestMethod]
    public void TheEpisodesThatComeFirstExist()
    {
        TestTables.Load<QuestEpisodeRow>("tb_Quest_Episode");

        foreach (var quest in Catalog.Episodes)
        {
            foreach (var before in quest.Before)
            {
                Assert.IsTrue(Catalog.TryGetEpisode(before, out _), $"episode {quest.Id} wants {before} first");
            }
        }
    }

    [TestMethod]
    public void TheConditionThatComesFirstIsInTheSameEpisode()
    {
        TestTables.Load<QuestEpisodeRow>("tb_Quest_Episode");

        foreach (var quest in Catalog.Episodes)
        {
            foreach (var id in quest.Conditions)
            {
                Catalog.TryGetCondition(id, out var condition);

                Assert.IsTrue(condition!.Previous == 0 || quest.Conditions.Contains(condition.Previous), $"condition {id} of {quest.Id} waits for {condition.Previous}");
            }
        }
    }

    [TestMethod]
    public void ConditionsNameWhatTheirKindCanName()
    {
        TestTables.Load<QuestEpisodeRow>("tb_Quest_Episode");

        foreach (var condition in Catalog.Conditions)
        {
            Assert.IsTrue(Enum.IsDefined(condition.Type), $"condition {condition.Id} has the kind {condition.Type}");
            Assert.IsTrue(Enum.IsDefined(condition.Target), $"condition {condition.Id} has the target {condition.Target}");
            Assert.IsInRange(1, 255, condition.Counter);

            var expected = condition.Type switch
            {
                QuestConditionType.Talk => QuestConditionTarget.Npc,
                QuestConditionType.Collect => QuestConditionTarget.Item,
                QuestConditionType.Trigger => QuestConditionTarget.Object,
                _ => condition.Target
            };

            Assert.AreEqual(expected, condition.Target, $"condition {condition.Id}");
        }
    }

    [TestMethod]
    public void OnlyTheEpisodesOfHelpersGiveHelpers()
    {
        TestTables.Load<QuestEpisodeRow>("tb_Quest_Episode");

        foreach (var quest in Catalog.Episodes)
        {
            Assert.AreEqual(quest.Contents == QuestContents.Helper, quest.Reward.Helper != 0, $"episode {quest.Id}");
        }
    }

    [TestMethod]
    public void TheRowsOfItemsByHeroExist()
    {
        TestTables.Load<QuestEpisodeRow>("tb_Quest_Episode");

        var rows = Rewards.Select(e => (int)e.Id).ToHashSet();

        foreach (var quest in Catalog.Episodes.Where(e => e.Reward.ItemsByClass))
        {
            foreach (var item in quest.Reward.Items)
            {
                Assert.Contains(item.Item, rows, $"episode {quest.Id} wants the row {item.Item}");
            }
        }
    }

    [TestMethod]
    public void TheFirstEpisodeIsReadAsItIsInTheTable()
    {
        TestTables.Load<QuestEpisodeRow>("tb_Quest_Episode");

        Assert.IsTrue(Catalog.TryGetEpisode(100001, out var quest));
        Assert.AreEqual(1, quest.CompleteBit);
        Assert.AreEqual(QuestContents.Main, quest.Contents);
        Assert.IsTrue(quest.FinishesByItself);
        CollectionAssert.AreEqual(new[] { 1000011, 1000012, 1000013, 1000014 }, quest.Conditions.ToArray());

        Assert.IsTrue(Catalog.TryGetEpisode(100002, out var second));
        CollectionAssert.AreEqual(new[] { 100001 }, second.Before.ToArray());
        Assert.IsTrue(Catalog.TryGetCondition(1000021, out var kill));
        Assert.AreEqual(QuestConditionType.Hunt, kill.Type);
        Assert.AreEqual(QuestConditionTarget.Monster, kill.Target);
        Assert.AreEqual(30101021, kill.TargetId);
        Assert.AreEqual(12, kill.Counter);
    }

    [TestMethod]
    public void AnEpisodeThatGivesItemsGivesThemToTheHero()
    {
        TestTables.Load<QuestEpisodeRow>("tb_Quest_Episode");

        Assert.IsTrue(Catalog.TryGetEpisode(100015, out var direct));

        var payout = QuestRules.GetPayout(direct, Catalog, 1, 0);

        CollectionAssert.AreEqual(new[] { new QuestItem(640101001, 3), new QuestItem(630101001, 5) }, payout.Items.ToArray());

        // The reward of a quest of the main line is the same for every hero, but each has its own row.
        var byHero = Catalog.Episodes.First(e => e.Reward.ItemsByClass);
        var items = Enumerable.Range(1, 6).Select(hero => QuestRules.GetPayout(byHero, Catalog, hero, 0).Items.Count).ToArray();

        Assert.IsTrue(items.Any(e => e > 0), "some hero gets the reward");
    }

    [TestMethod]
    public void AMonsterKnowsTheGroupsItCountsFor()
    {
        TestTables.Load<QuestEpisodeRow>("tb_Quest_Episode");

        var groups = Conditions.Where(e => e.Field3 == (byte)QuestConditionType.Hunt && e.Field6 == (byte)QuestConditionTarget.MonsterGroup).Select(e => e.Field7).ToHashSet();
        var monsters = TestTables.Load<MonsterRow>("tb_Monster");

        // Every group a condition names has a monster that belongs to it.
        var known = monsters.SelectMany(e => Catalog.GetMonsterGroups(e.Id)).ToHashSet();

        Assert.IsTrue(groups.All(known.Contains), "a group of the conditions has no monster");
    }
}
