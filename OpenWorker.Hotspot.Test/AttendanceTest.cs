using OpenWorker.Gameplay.Modules.Attendance;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Hotspot.Modules.Events.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class AttendanceTest
{
    private static readonly TimeSpan Reset = AttendanceRules.DefaultResetTime;

    private static AttendanceCatalog Catalog { get; } = AttendanceCatalog.From(
        [new AttendanceTable(1011, 28), new AttendanceTable(1012, 28)],
        new Dictionary<int, Reward> { [101101] = new([new RewardItem(5, 1)], 100, 0) },
        [new PlayTimeStep(1800, 10), new PlayTimeStep(3600, 11), new PlayTimeStep(5400, 12)]);

    private static DateTimeOffset At(int day, int hour, int minute = 0) => new(2026, 11, day, hour, minute, 0, TimeSpan.Zero);

    [TestMethod]
    public void FirstComeOfTheDayIsTheFirstDayOfTheTable()
    {
        var book = new AttendanceBook();

        var advance = AttendanceRules.Advance(book, Catalog, At(7, 12), Reset);

        Assert.IsTrue(advance.HasTable);
        Assert.IsTrue(advance.Attended);
        Assert.AreEqual(1, advance.Day);
        Assert.AreEqual(1011, book.Table);
        Assert.AreEqual(1, book.Count);
    }

    [TestMethod]
    public void SameDayCountsOnce()
    {
        var book = new AttendanceBook();

        AttendanceRules.Advance(book, Catalog, At(7, 12), Reset);
        var again = AttendanceRules.Advance(book, Catalog, At(7, 20), Reset);

        Assert.IsFalse(again.Attended);
        Assert.AreEqual(1, again.Day);
        Assert.AreEqual(1, book.Count);
    }

    [TestMethod]
    public void NextGameDayStartsAtTheResetHourNotAtMidnight()
    {
        var book = new AttendanceBook();

        AttendanceRules.Advance(book, Catalog, At(7, 12), Reset);

        // After midnight and before 9:00 it is still the same day.
        Assert.IsFalse(AttendanceRules.Advance(book, Catalog, At(8, 8, 59), Reset).Attended);
        Assert.IsTrue(AttendanceRules.Advance(book, Catalog, At(8, 9), Reset).Attended);
        Assert.AreEqual(2, book.Count);
    }

    [TestMethod]
    public void TheCountStopsAtTheLastDayOfTheTable()
    {
        var catalog = AttendanceCatalog.From([new AttendanceTable(1011, 2)], new Dictionary<int, Reward>(), []);
        var book = new AttendanceBook();

        for (var day = 1; day <= 5; day++)
        {
            AttendanceRules.Advance(book, catalog, At(day, 12), Reset);
        }

        Assert.AreEqual(2, book.Count);
    }

    [TestMethod]
    public void NewMonthStartsAnotherTableFromTheBeginning()
    {
        var book = new AttendanceBook();

        AttendanceRules.Advance(book, Catalog, At(7, 12), Reset);
        AttendanceRules.Advance(book, Catalog, At(8, 12), Reset);

        var december = AttendanceRules.Advance(book, Catalog, new DateTimeOffset(2026, 12, 1, 12, 0, 0, TimeSpan.Zero), Reset);

        Assert.AreEqual(1012, book.Table);
        Assert.AreEqual(1, book.Count);
        Assert.IsTrue(december.Attended);
    }

    [TestMethod]
    public void OneTableIsUsedWhenTablesDoNotGoByMonth()
    {
        var catalog = AttendanceCatalog.From([new AttendanceTable(1001, 28)], new Dictionary<int, Reward>(), []);

        Assert.IsTrue(catalog.TryGetTable(At(7, 12), out var table));
        Assert.AreEqual(1001, table.Id);
    }

    [TestMethod]
    public void NoTablesNoAttendance()
    {
        var book = new AttendanceBook();

        var advance = AttendanceRules.Advance(book, AttendanceCatalog.From([], new Dictionary<int, Reward>(), []), At(7, 12), Reset);

        Assert.IsFalse(advance.HasTable);
        Assert.AreEqual(0, book.Count);
    }

    [TestMethod]
    public void UndoGivesTheDayBack()
    {
        var book = new AttendanceBook();
        var before = book.LastDay;

        AttendanceRules.Advance(book, Catalog, At(7, 12), Reset);
        AttendanceRules.Undo(book, before);

        Assert.AreEqual(0, book.Count);
        Assert.IsTrue(AttendanceRules.Advance(book, Catalog, At(7, 13), Reset).Attended);
    }

    [TestMethod]
    public void InfoMarksTheDaysThatWereTaken()
    {
        var book = new AttendanceBook { Table = 1011, Count = 3 };

        var info = book.ToInfo();

        Assert.AreEqual((byte)3, info.CurrentDay);
        Assert.AreEqual(1011, info.Table);
        CollectionAssert.AreEqual(
            new[] { AttendanceState.Taken, AttendanceState.Taken, AttendanceState.Taken, AttendanceState.Available },
            info.State.Take(4).ToArray());
        Assert.HasCount(40, info.State);
    }

    [TestMethod]
    public void PlayTimeReachesTheStepsInOrder()
    {
        var book = new AttendanceBook();

        Assert.HasCount(0, AttendanceRules.AddPlayTime(book, Catalog, 1000));
        Assert.AreEqual(1000, book.PlaySeconds);

        CollectionAssert.AreEqual(new[] { 0 }, AttendanceRules.AddPlayTime(book, Catalog, 800));
        Assert.AreEqual(1, book.PlayStep);
        Assert.AreEqual(0, book.PlaySeconds);
    }

    [TestMethod]
    public void PlayTimeIsNotCarriedToTheNextStep()
    {
        var book = new AttendanceBook();

        CollectionAssert.AreEqual(new[] { 0, 1 }, AttendanceRules.AddPlayTime(book, Catalog, 1800 + 3600 + 5));
        Assert.AreEqual(2, book.PlayStep);
        Assert.AreEqual(5, book.PlaySeconds);
    }

    [TestMethod]
    public void PlayTimeStopsAfterTheLastStep()
    {
        var book = new AttendanceBook { PlayStep = 3 };

        Assert.HasCount(0, AttendanceRules.AddPlayTime(book, Catalog, 100_000));
        Assert.AreEqual(0, book.PlaySeconds);
    }

    [TestMethod]
    public void PlayTimeStartsAgainWithTheGameDay()
    {
        var book = new AttendanceBook();

        AttendanceRules.Advance(book, Catalog, At(7, 12), Reset);
        AttendanceRules.AddPlayTime(book, Catalog, 2000);

        var same = AttendanceRules.Advance(book, Catalog, At(7, 20), Reset);

        Assert.IsFalse(same.PlayTimeReset);
        Assert.AreEqual(1, book.PlayStep);

        var next = AttendanceRules.Advance(book, Catalog, At(8, 9), Reset);

        Assert.IsTrue(next.PlayTimeReset);
        Assert.AreEqual(0, book.PlayStep);
        Assert.AreEqual(0, book.PlaySeconds);
    }

    [TestMethod]
    public void TablesOfTheClientGiveRewardsForEveryDay()
    {
        var catalog = new AttendanceCatalog(
            TestTables.Load<CheckAttendanceInfoRow>("tb_Check_Attendance_Info"),
            TestTables.Load<CheckAttendanceRewardRow>("tb_Check_Attendance_Reward"),
            TestTables.Load<CheckAccessRewardRow>("tb_Check_Access_Reward"));

        Assert.IsTrue(catalog.TryGetTable(1001, out var table));
        Assert.AreEqual(28, table.Days);

        CollectionAssert.AreEqual(new[] { new RewardItem(836000029, 1) }, catalog.GetReward(1001, 1).Items.ToArray());
        Assert.AreEqual(10000, catalog.GetReward(1001, 2).Gold);
        CollectionAssert.AreEqual(new[] { new RewardItem(800000999, 5) }, catalog.GetReward(1001, 3).Items.ToArray());
        Assert.AreEqual(1000, catalog.GetReward(1001, 5).Ether);

        for (var day = 1; day <= table.Days; day++)
        {
            Assert.IsFalse(catalog.GetReward(1001, day).IsEmpty, $"day {day}");
        }

        Assert.IsTrue(catalog.GetReward(1001, 29).IsEmpty);
    }

    [TestMethod]
    public void TableOfTheClientHasThreePlayTimeSteps()
    {
        var catalog = new AttendanceCatalog(
            TestTables.Load<CheckAttendanceInfoRow>("tb_Check_Attendance_Info"),
            TestTables.Load<CheckAttendanceRewardRow>("tb_Check_Attendance_Reward"),
            TestTables.Load<CheckAccessRewardRow>("tb_Check_Access_Reward"));

        CollectionAssert.AreEqual(
            new[] { new PlayTimeStep(1800, 837100001), new PlayTimeStep(3600, 837100001), new PlayTimeStep(5400, 837100001) },
            catalog.PlayTime.ToArray());
    }
}
