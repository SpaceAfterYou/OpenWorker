using System.Text.Json;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Hotspot.Modules.Quests.Enums;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class QuestRulesTest
{
    private const int Npc = 7;
    private const int Wolf = 50;
    private const int WolfPack = 900;

    private static QuestConditionDefinition Talk { get; } = new(1001, QuestConditionType.Talk, QuestConditionTarget.Npc, Npc, 0, 1, 0, [], []);

    private static QuestConditionDefinition Hunt { get; } = new(1002, QuestConditionType.Hunt, QuestConditionTarget.Monster, Wolf, 1001, 3, 0, [], []);

    private static QuestConditionDefinition Pack { get; } = new(1003, QuestConditionType.Hunt, QuestConditionTarget.MonsterGroup, WolfPack, 0, 2, 0, [], []);

    private static QuestConditionDefinition Sector { get; } = new(1004, QuestConditionType.Clear, QuestConditionTarget.Sector, 10001, 0, 1, 20011, [], []);

    private static QuestConditionDefinition Craft { get; } = new(1005, QuestConditionType.Make, QuestConditionTarget.Item, 0, 0, 1, 0, [], []);

    private static QuestDefinition First { get; } = new(
        100,
        1,
        QuestContents.Main,
        0,
        0,
        [],
        false,
        false,
        [new QuestItem(710001, 1)],
        [],
        new QuestRewardDefinition(100, 10, 5, 3, 100100, 200100, 0, false, [new QuestItem(710002, 2)], false, []),
        [1001, 1002, 1003]);

    private static QuestDefinition Second { get; } = new(
        200,
        2,
        QuestContents.Side,
        30,
        2,
        [100],
        true,
        true,
        [],
        [],
        new QuestRewardDefinition(0, 0, 0, 0, 0, 0, 0, true, [new QuestItem(10, 1)], true, [new QuestItem(11, 1), new QuestItem(12, 3)]),
        [1004, 1005]);

    private static QuestCatalog Catalog { get; } = QuestCatalog.From(
        [First, Second],
        [Talk, Hunt, Pack, Sector, Craft],
        new Dictionary<int, int[]>
        {
            [10] = [0, 810001, 0, 0, 0, 0],
            [11] = [811001, 811002, 0, 0, 0, 0],
            [12] = [812001, 812002, 0, 0, 0, 0]
        },
        new Dictionary<int, int[]> { [Wolf] = [WolfPack] });

    private static QuestBook BookWith(params QuestEpisodeState[] states) => new() { Episodes = [.. states] };

    private static QuestEpisodeState Taken(QuestDefinition quest) => QuestRules.Start(quest, true);

    private static List<QuestStep> Kill(QuestBook book, int monster)
    {
        var groups = Catalog.GetMonsterGroups(monster);

        return QuestRules.Apply(
            book,
            Catalog,
            e => e.Type == QuestConditionType.Hunt &&
                 (e.Target == QuestConditionTarget.Monster && QuestRules.AcceptsParameter(e, monster) ||
                  e.Target == QuestConditionTarget.MonsterGroup && groups.Any(g => QuestRules.AcceptsParameter(e, g))),
            1);
    }

    #region Taking an episode

    [TestMethod]
    public void AnEpisodeWithNoObstacleCanBeTaken()
    {
        Assert.IsNull(QuestRules.CheckAccept(Catalog, new QuestBook(), First, 1, 1, false));
    }

    [TestMethod]
    public void AnEpisodeIsTakenOnce()
    {
        var book = BookWith(Taken(First));

        Assert.AreEqual(QuestErrors.AlreadyTaken, QuestRules.CheckAccept(Catalog, book, First, 1, 1, false));
    }

    [TestMethod]
    public void AFinishedEpisodeIsNotTakenAgain()
    {
        var book = new QuestBook();
        book.SetFinished(First.CompleteBit, true);

        Assert.AreEqual(QuestErrors.AlreadyFinished, QuestRules.CheckAccept(Catalog, book, First, 1, 1, false));
    }

    [TestMethod]
    public void ARepeatedEpisodeIsNeverFinishedForGood()
    {
        var repeat = First with { Contents = QuestContents.Repeat };
        var book = new QuestBook();
        book.SetFinished(repeat.CompleteBit, true);

        Assert.IsNull(QuestRules.CheckAccept(Catalog, book, repeat, 1, 1, false));
    }

    [TestMethod]
    public void TheLevelAndTheHeroOfTheEpisodeAreChecked()
    {
        var book = new QuestBook();
        book.SetFinished(First.CompleteBit, true);

        Assert.AreEqual(QuestErrors.LevelTooLow, QuestRules.CheckAccept(Catalog, book, Second, 29, 2, false));
        Assert.AreEqual(QuestErrors.WrongHero, QuestRules.CheckAccept(Catalog, book, Second, 30, 1, false));
        Assert.IsNull(QuestRules.CheckAccept(Catalog, book, Second, 30, 2, false));
    }

    [TestMethod]
    public void TheEpisodesThatComeFirstHaveToBeFinished()
    {
        Assert.AreEqual(QuestErrors.NotAvailableYet, QuestRules.CheckAccept(Catalog, new QuestBook(), Second, 30, 2, false));
        Assert.IsNull(QuestRules.CheckAccept(Catalog, new QuestBook(), Second, 30, 2, true), "a script takes it anyway");
    }

    [TestMethod]
    public void AUnknownEpisodeBeforeIsNotFinished()
    {
        var odd = Second with { Before = [999] };

        Assert.AreEqual(QuestErrors.NotAvailableYet, QuestRules.CheckAccept(Catalog, new QuestBook(), odd, 30, 2, false));
    }

    [TestMethod]
    public void ThereIsALimitOfEpisodesButNotForScripts()
    {
        var book = new QuestBook { Episodes = [.. Enumerable.Range(1, QuestRules.MaxEpisodes).Select(e => new QuestEpisodeState { Episode = 5000 + e })] };

        Assert.AreEqual(QuestErrors.TooManyEpisodes, QuestRules.CheckAccept(Catalog, book, First, 1, 1, false));
        Assert.IsNull(QuestRules.CheckAccept(Catalog, book, First, 1, 1, true));
    }

    [TestMethod]
    public void AnEpisodeWithNoConditionsCanNotBeTaken()
    {
        var empty = First with { Conditions = [] };

        Assert.AreEqual(QuestErrors.UnknownEpisode, QuestRules.CheckAccept(Catalog, new QuestBook(), empty, 1, 1, false));
    }

    #endregion Taking an episode

    #region Counting acts

    [TestMethod]
    public void AKillMovesTheConditionThatNamesTheMonster()
    {
        var book = BookWith(Taken(First));
        book.Episodes[0].Done = 1;

        var steps = Kill(book, Wolf);

        // The monster is in a group too, so both conditions move.
        Assert.HasCount(2, steps);
        Assert.AreEqual((byte)1, book.Episodes[0].Steps[1]);
        Assert.AreEqual((byte)1, book.Episodes[0].Steps[2]);
        Assert.IsFalse(steps.Any(e => e.Completed));
    }

    [TestMethod]
    public void AKillOfAnotherMonsterMovesNothing()
    {
        var book = BookWith(Taken(First));
        book.Episodes[0].Done = 1;

        Assert.IsEmpty(Kill(book, 51));
    }

    [TestMethod]
    public void AConditionIsDoneWhenItsCounterIsReached()
    {
        var book = BookWith(Taken(First));
        book.Episodes[0].Done = 1;

        Kill(book, Wolf);
        Kill(book, Wolf);

        var steps = Kill(book, Wolf);

        Assert.AreEqual((byte)3, book.Episodes[0].Steps[1]);
        Assert.IsTrue(book.Episodes[0].IsDone(1));
        Assert.IsTrue(steps.Single(e => e.Condition.Id == Hunt.Id).Completed);
        Assert.IsTrue(book.Episodes[0].IsDone(2), "the group counter is 2");
    }

    [TestMethod]
    public void ADoneConditionDoesNotCountMore()
    {
        var book = BookWith(Taken(First));
        book.Episodes[0].Done = 1;

        for (var i = 0; i < 5; i++)
        {
            Kill(book, Wolf);
        }

        Assert.AreEqual((byte)3, book.Episodes[0].Steps[1]);
        Assert.AreEqual((byte)2, book.Episodes[0].Steps[2]);
        Assert.IsEmpty(Kill(book, Wolf));
    }

    [TestMethod]
    public void AnActCanNotGoPastTheCounter()
    {
        var book = BookWith(Taken(First));
        book.Episodes[0].Done = 1;

        var steps = QuestRules.Apply(book, Catalog, e => e.Id == Hunt.Id, 200);

        Assert.AreEqual((byte)3, book.Episodes[0].Steps[1]);
        Assert.IsTrue(steps.Single().Completed);
        Assert.AreEqual((byte)0, steps.Single().Before);
    }

    [TestMethod]
    public void AConditionWaitsForTheOneThatHasToComeFirst()
    {
        var book = BookWith(Taken(First));

        Assert.IsEmpty(Kill(book, Wolf).Where(e => e.Condition.Id == Hunt.Id));
        Assert.AreEqual((byte)0, book.Episodes[0].Steps[1]);

        QuestRules.Apply(book, Catalog, e => QuestRules.Matches(e, QuestConditionType.Talk, QuestConditionTarget.Npc, Npc), 1);

        Assert.IsTrue(book.Episodes[0].IsDone(0));
        Assert.IsNotEmpty(Kill(book, Wolf).Where(e => e.Condition.Id == Hunt.Id));
    }

    [TestMethod]
    public void AFailedEpisodeStopsCounting()
    {
        var book = BookWith(Taken(First));
        book.Episodes[0].Done = 1;
        book.Episodes[0].Failed = true;

        Assert.IsEmpty(Kill(book, Wolf));
    }

    [TestMethod]
    public void AnUndoneStepGoesBack()
    {
        var book = BookWith(Taken(First));
        book.Episodes[0].Done = 1;

        var step = QuestRules.Apply(book, Catalog, e => e.Id == Hunt.Id, 3).Single();

        step.Undo();

        Assert.AreEqual((byte)0, book.Episodes[0].Steps[1]);
        Assert.IsFalse(book.Episodes[0].IsDone(1));
        Assert.IsTrue(book.Episodes[0].IsDone(0));
    }

    [TestMethod]
    public void AnEpisodeIsReadyWhenAllItsConditionsAreDone()
    {
        var state = Taken(First);

        Assert.IsFalse(QuestRules.IsReady(First, state));

        state.Done = 0b011;
        Assert.IsFalse(QuestRules.IsReady(First, state));

        state.Done = 0b111;
        Assert.IsTrue(QuestRules.IsReady(First, state));
        Assert.AreEqual((short)7, QuestRules.GetClientBits(First, state));
    }

    [TestMethod]
    public void ABitOfAConditionTheEpisodeDoesNotHaveIsNotTold()
    {
        var state = Taken(First);
        state.Done = 0b1111_1111;

        Assert.AreEqual((short)7, QuestRules.GetClientBits(First, state));
    }

    [TestMethod]
    public void WhatAConditionNamesDecidesWhichActsCount()
    {
        Assert.IsTrue(QuestRules.Matches(Talk, QuestConditionType.Talk, QuestConditionTarget.Npc, Npc));
        Assert.IsFalse(QuestRules.Matches(Talk, QuestConditionType.Talk, QuestConditionTarget.Npc, Npc + 1));
        Assert.IsFalse(QuestRules.Matches(Talk, QuestConditionType.Talk, QuestConditionTarget.Monster, Npc));
        Assert.IsFalse(QuestRules.Matches(Talk, QuestConditionType.Hunt, QuestConditionTarget.Npc, Npc));
    }

    [TestMethod]
    public void ConditionsThatNameNothingCountAnything()
    {
        // Making, buying and the like accept every item when the condition names none.
        Assert.IsTrue(QuestRules.Matches(Craft, QuestConditionType.Make, QuestConditionTarget.Item, 123456));

        // Killing and collecting always name what they are about.
        var anyKill = Hunt with { TargetId = 0 };
        Assert.IsFalse(QuestRules.Matches(anyKill, QuestConditionType.Hunt, QuestConditionTarget.Monster, Wolf));
    }

    [TestMethod]
    public void ASectorConditionIsBoundToItsMaze()
    {
        var book = BookWith(Taken(Second));

        Assert.IsEmpty(QuestRules.Apply(book, Catalog, e => e.Maze == 20012 && QuestRules.Matches(e, QuestConditionType.Clear, QuestConditionTarget.Sector, 10001), 1));
        Assert.IsNotEmpty(QuestRules.Apply(book, Catalog, e => e.Maze == 20011 && QuestRules.Matches(e, QuestConditionType.Clear, QuestConditionTarget.Sector, 10001), 1));
    }

    #endregion Counting acts

    #region Rewards

    [TestMethod]
    public void TheRewardHasWhatTheEpisodeGives()
    {
        var payout = QuestRules.GetPayout(First, Catalog, 1, 0);

        Assert.AreEqual(10, payout.Reward.Gold);
        Assert.AreEqual(5, payout.Reward.Ether);
        Assert.AreEqual(3, payout.Reward.BattlePoints);
        Assert.AreEqual(100, payout.Reward.Experience);
        CollectionAssert.AreEqual(new[] { new QuestItem(710002, 2) }, payout.Items.ToArray());
        Assert.AreEqual(100100, payout.Prefix);
        Assert.AreEqual(200100, payout.Suffix);
    }

    [TestMethod]
    public void TheItemOfARowDependsOnTheHero()
    {
        var items = QuestRules.GetPayout(Second, Catalog, 2, 0).Items;

        CollectionAssert.AreEqual(new[] { new QuestItem(810001, 1) }, items.ToArray());
        Assert.IsEmpty(QuestRules.GetPayout(Second, Catalog, 1, 0).Items, "the row has no item for the first hero");
    }

    [TestMethod]
    public void OnlyTheChosenItemIsGiven()
    {
        var first = QuestRules.GetPayout(Second, Catalog, 1, 11).Items;
        var other = QuestRules.GetPayout(Second, Catalog, 2, 12).Items;

        CollectionAssert.AreEqual(new[] { new QuestItem(811001, 1) }, first.ToArray());
        CollectionAssert.AreEqual(new[] { new QuestItem(810001, 1), new QuestItem(812002, 3) }, other.ToArray());
        Assert.IsEmpty(QuestRules.GetPayout(Second, Catalog, 1, 13).Items, "an item that is not offered is not given");
    }

    #endregion Rewards

    #region The book

    [TestMethod]
    public void FinishedEpisodesAreBits()
    {
        var book = new QuestBook();

        Assert.IsFalse(book.IsFinished(458));
        Assert.IsTrue(book.SetFinished(458, true));
        Assert.IsTrue(book.IsFinished(458));
        Assert.IsFalse(book.IsFinished(457));
        Assert.AreEqual((byte)0b100, book.Finished[57]);

        Assert.IsTrue(book.SetFinished(458, false));
        Assert.IsFalse(book.IsFinished(458));
    }

    [TestMethod]
    public void ABitOutsideTheListIsRefused()
    {
        var book = new QuestBook();

        Assert.IsFalse(book.SetFinished(1024, true));
        Assert.IsFalse(book.SetFinished(-1, true));
        Assert.IsFalse(book.IsFinished(1024));
    }

    [TestMethod]
    public void ABookSurvivesBeingStored()
    {
        var book = BookWith(Taken(First), Taken(Second));
        book.Episodes[0].Steps[1] = 2;
        book.Episodes[0].Done = 1;
        book.Episodes[1].Failed = true;
        book.SetFinished(77, true);

        var json = JsonSerializer.Serialize(book);
        var read = JsonSerializer.Deserialize<QuestBook>(json)!;

        Assert.HasCount(2, read.Episodes);
        Assert.AreEqual(100, read.Episodes[0].Episode);
        Assert.AreEqual((byte)2, read.Episodes[0].Steps[1]);
        Assert.AreEqual((short)1, read.Episodes[0].Done);
        Assert.IsTrue(read.Episodes[1].Failed);
        Assert.IsTrue(read.IsFinished(77));
    }

    [TestMethod]
    public void ABookIsMadeToFitTheTables()
    {
        var odd = new QuestEpisodeState { Episode = 100, Steps = [1, 2], Done = 0b1111_1111 };
        var book = BookWith(odd, new QuestEpisodeState { Episode = 4242 }, new QuestEpisodeState { Episode = 100 });
        book.Finished = new byte[3];
        book.SetFinished(3, true);

        book.Normalize(Catalog);

        Assert.HasCount(1, book.Episodes, "an episode the tables lack goes, and so does a second copy");
        Assert.HasCount(10, book.Episodes[0].Steps);
        Assert.AreEqual((byte)2, book.Episodes[0].Steps[1]);
        Assert.AreEqual((short)7, book.Episodes[0].Done);
        Assert.HasCount(128, book.Finished);
        Assert.IsTrue(book.IsFinished(3));
    }

    [TestMethod]
    public void TheHelperHasRoomForSevenEpisodes()
    {
        var book = new QuestBook { Episodes = [.. Enumerable.Range(1, 9).Select(e => new QuestEpisodeState { Episode = e, Helper = e <= 7 })] };

        Assert.AreEqual(7, book.HelperCount);
        Assert.AreEqual(QuestRules.MaxHelperEpisodes, book.HelperCount);
    }

    #endregion The book
}
