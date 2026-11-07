using OpenWorker.Gameplay.Modules.DailyMissions;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Hotspot.Modules.DailyMissions.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class DailyMissionTest
{
    private static DateTimeOffset At(int hour, int minute = 0) => new(2026, 11, 7, hour, minute, 0, TimeSpan.Zero);

    private static DailyMissionDefinition Mission(
        int id,
        DailyMissionType type = DailyMissionType.Special,
        int district = 10003,
        DailyMissionFinish finish = DailyMissionFinish.Kill,
        DailyMissionTarget target = DailyMissionTarget.Monster,
        int targetId = 0,
        int conditionType = 0,
        int count = 3,
        string start = "1000-01-01 00:00:00",
        string end = "1000-01-01 23:00:00")
    {
        return new DailyMissionDefinition(
            id, type, district, finish, target, targetId, 0, conditionType, 0, 0, 0, count,
            DailyMissionDates.Parse(start, end), [new RewardItem(800000999, 1)]);
    }

    [TestMethod]
    public void TheDayStartsAtTheResetTime()
    {
        var reset = DailyMissionRules.DefaultResetTime;

        var (start, end) = DailyMissionRules.GetDay(At(12), reset);

        Assert.AreEqual(At(11), start);
        Assert.AreEqual(At(11).AddDays(1), end);
    }

    [TestMethod]
    public void BeforeTheResetTimeTheDayIsStillTheOldOne()
    {
        var (start, end) = DailyMissionRules.GetDay(At(10, 59), DailyMissionRules.DefaultResetTime);

        Assert.AreEqual(At(11).AddDays(-1), start);
        Assert.AreEqual(At(11), end);
    }

    [TestMethod]
    public void TheResetMomentBelongsToTheNewDay()
    {
        var (start, _) = DailyMissionRules.GetDay(At(11), DailyMissionRules.DefaultResetTime);

        Assert.AreEqual(At(11), start);
    }

    [TestMethod]
    public void WaitAndDurationCountSecondsFromNow()
    {
        Assert.AreEqual(3600, DailyMissionRules.GetRemain(At(14), At(15)));
        Assert.AreEqual(0, DailyMissionRules.GetRemain(At(16), At(15)));

        Assert.AreEqual(3600, DailyMissionRules.GetDuration(At(14), At(15), At(16)));
        Assert.AreEqual(1800, DailyMissionRules.GetDuration(At(15, 30), At(15), At(16)));
        Assert.AreEqual(0, DailyMissionRules.GetDuration(At(17), At(15), At(16)));
    }

    [TestMethod]
    public void MissionOfTheDayIsOpenOnlyInItsDay()
    {
        var definition = Mission(1);
        var (start, end) = DailyMissionRules.GetDay(At(12), DailyMissionRules.DefaultResetTime);
        var entry = new DailyMissionEntry { Mission = 1, Start = start, End = end };

        Assert.IsTrue(DailyMissionRules.IsOpen(definition, entry, At(12)));
        Assert.IsTrue(DailyMissionRules.IsOpen(definition, entry, end.AddSeconds(-1)));
        Assert.IsFalse(DailyMissionRules.IsOpen(definition, entry, end));
        Assert.IsFalse(DailyMissionRules.IsOpen(definition, entry, start.AddSeconds(-1)));
    }

    [TestMethod]
    public void GuerrillaMissionIsOpenInItsHourEveryDay()
    {
        var definition = Mission(2, DailyMissionType.Guerrilla, start: "1000-01-01 15:00:00", end: "1000-01-01 16:00:00");
        var entry = new DailyMissionEntry { Mission = 2 };

        Assert.IsFalse(DailyMissionRules.IsOpen(definition, entry, At(14, 59)));
        Assert.IsTrue(DailyMissionRules.IsOpen(definition, entry, At(15)));
        Assert.IsTrue(DailyMissionRules.IsOpen(definition, entry, At(15, 59)));
        Assert.IsFalse(DailyMissionRules.IsOpen(definition, entry, At(16)));
    }

    [TestMethod]
    public void EventMissionIsOpenBetweenItsDates()
    {
        var definition = Mission(3, DailyMissionType.Event, start: "2026-11-01 00:00:00", end: "2026-11-30 00:00:00");
        var entry = new DailyMissionEntry { Mission = 3 };

        Assert.IsTrue(DailyMissionRules.IsOpen(definition, entry, At(1)));
        Assert.IsFalse(DailyMissionRules.IsOpen(definition, entry, At(1).AddMonths(1)));
        Assert.IsFalse(DailyMissionRules.IsOpen(definition, entry, At(1).AddMonths(-1)));
    }

    [TestMethod]
    public void UnreadableDateIsTheYear2000()
    {
        var dates = DailyMissionDates.Parse("nonsense", "");

        Assert.AreEqual(new DateTime(2000, 1, 1), dates.Start);
        Assert.AreEqual(new DateTime(2000, 1, 1), dates.End);
    }

    [TestMethod]
    public void SpecialMissionsAreLimitedByTypeAndByFinish()
    {
        Assert.AreEqual(0, DailyMissionRules.CheckCount(DailyMissionType.Special, 0, DailyMissionFinish.Kill, 1));
        Assert.AreEqual(2, DailyMissionRules.CheckCount(DailyMissionType.Special, 2, DailyMissionFinish.Kill, 2));
        Assert.AreEqual(2, DailyMissionRules.CheckCount(DailyMissionType.Special, 2, DailyMissionFinish.MazeClear, 1));
        Assert.AreEqual(2, DailyMissionRules.CheckCount(DailyMissionType.Special, 2, DailyMissionFinish.PartyMazeClear, 1));
        Assert.AreEqual(2, DailyMissionRules.CheckCount(DailyMissionType.Special, 2, DailyMissionFinish.Friend, 1));
        Assert.AreEqual(1, DailyMissionRules.CheckCount(DailyMissionType.Special, 5, DailyMissionFinish.Kill, 0));
    }

    [TestMethod]
    public void GuerrillaAndEventLimits()
    {
        Assert.AreEqual(1, DailyMissionRules.CheckCount(DailyMissionType.Guerrilla, 1, DailyMissionFinish.Kill, 0));
        Assert.AreEqual(2, DailyMissionRules.CheckCount(DailyMissionType.Guerrilla, 0, DailyMissionFinish.Kill, 1));
        Assert.AreEqual(0, DailyMissionRules.CheckCount(DailyMissionType.Event, 100, DailyMissionFinish.Kill, 100));
    }

    [TestMethod]
    public void DrawKeepsToTheLimitsOfEveryDistrict()
    {
        var all = new List<DailyMissionDefinition>();

        foreach (var district in new[] { 1, 2 })
        {
            for (var i = 0; i < 6; i++)
            {
                all.Add(Mission(district * 100 + i, district: district, finish: DailyMissionFinish.Kill));
                all.Add(Mission(district * 100 + 10 + i, district: district, finish: DailyMissionFinish.MazeClear, target: DailyMissionTarget.Maze));
                all.Add(Mission(district * 100 + 20 + i, district: district, finish: DailyMissionFinish.Friend, target: DailyMissionTarget.Person));
            }
        }

        var catalog = DailyMissionCatalog.From(all);

        for (var seed = 0; seed < 20; seed++)
        {
            var drawn = DailyMissionRules.Draw(catalog, DailyMissionType.Special, At(12), new Random(seed));

            foreach (var district in new[] { 1, 2 })
            {
                var own = drawn.Select(id => catalog.TryGet(id, out var e) ? e : null!).Where(e => e.District == district).ToArray();

                Assert.HasCount(4, own);
                Assert.AreEqual(2, own.Count(e => e.Finish == DailyMissionFinish.Kill));
                Assert.AreEqual(1, own.Count(e => e.Finish == DailyMissionFinish.MazeClear));
                Assert.AreEqual(1, own.Count(e => e.Finish == DailyMissionFinish.Friend));
            }
        }
    }

    [TestMethod]
    public void DrawPrefersAGuerrillaMissionThatHasNotStarted()
    {
        var late = Mission(1, DailyMissionType.Guerrilla, start: "1000-01-01 20:00:00", end: "1000-01-01 21:00:00");
        var early = Mission(2, DailyMissionType.Guerrilla, finish: DailyMissionFinish.MazeClear, start: "1000-01-01 08:00:00", end: "1000-01-01 09:00:00");
        var catalog = DailyMissionCatalog.From([late, early]);

        for (var seed = 0; seed < 10; seed++)
        {
            CollectionAssert.AreEqual(new[] { 1 }, DailyMissionRules.Draw(catalog, DailyMissionType.Guerrilla, At(12), new Random(seed)));
        }
    }

    [TestMethod]
    public void DrawLeavesOutEventsThatAreOver()
    {
        var over = Mission(1, DailyMissionType.Event, start: "2020-01-01 00:00:00", end: "2020-02-01 00:00:00");
        var running = Mission(2, DailyMissionType.Event, start: "2026-01-01 00:00:00", end: "2027-01-01 00:00:00");

        var drawn = DailyMissionRules.Draw(DailyMissionCatalog.From([over, running]), DailyMissionType.Event, At(12), new Random(1));

        CollectionAssert.AreEqual(new[] { 2 }, drawn);
    }

    [TestMethod]
    public void KillCountsByTheKindOfAttack()
    {
        var catalog = DailyMissionCatalog.From([]);
        var skill = Mission(1, targetId: 77, conditionType: (int)DailyMissionCondition.SkillAttack);

        Assert.IsTrue(DailyMissionRules.CountsKill(skill, catalog, 77, 1u << 3));
        Assert.IsFalse(DailyMissionRules.CountsKill(skill, catalog, 77, 1u << 1));
        Assert.IsFalse(DailyMissionRules.CountsKill(skill, catalog, 78, 1u << 3));
    }

    [TestMethod]
    public void KillWithoutConditionCountsWithAnyAttack()
    {
        var plain = Mission(1, targetId: 77);

        Assert.IsTrue(DailyMissionRules.CountsKill(plain, DailyMissionCatalog.From([]), 77, 1));
    }

    [TestMethod]
    public void EventMissionIgnoresTheKindOfAttack()
    {
        var @event = Mission(1, DailyMissionType.Event, targetId: 77, conditionType: 3);

        Assert.IsTrue(DailyMissionRules.CountsKill(@event, DailyMissionCatalog.From([]), 77, 1));
    }

    [TestMethod]
    public void KillOfAGroupAndOfAFactionGoThroughTheMonsterTable()
    {
        var catalog = DailyMissionCatalog.From([], new Dictionary<int, DailyMissionMonster>
        {
            [10] = new(7, 1100001, 0, 0),
            [11] = new(6, 0, 0, 1100001)
        });

        var group = Mission(1, target: DailyMissionTarget.MonsterGroup, targetId: 1100001);
        var faction = (Mission(2, target: DailyMissionTarget.MonsterFaction) with { TargetFaction = 7 });
        var all = Mission(3, target: DailyMissionTarget.MonsterAll);

        Assert.IsTrue(DailyMissionRules.CountsKill(group, catalog, 10, 1));
        Assert.IsTrue(DailyMissionRules.CountsKill(group, catalog, 11, 1));
        Assert.IsFalse(DailyMissionRules.CountsKill(group, catalog, 12, 1));
        Assert.IsTrue(DailyMissionRules.CountsKill(faction, catalog, 10, 1));
        Assert.IsFalse(DailyMissionRules.CountsKill(faction, catalog, 11, 1));
        Assert.IsTrue(DailyMissionRules.CountsKill(all, catalog, 12, 1));
    }

    [TestMethod]
    public void MazeClearCountsByMazeRankTimeAndType()
    {
        var anyMaze = Mission(1, finish: DailyMissionFinish.MazeClear, target: DailyMissionTarget.Maze);
        var rank = Mission(2, finish: DailyMissionFinish.MazeClear, target: DailyMissionTarget.Maze, targetId: 21111, conditionType: (int)DailyMissionCondition.RankSS);
        var fast = (Mission(3, finish: DailyMissionFinish.MazeClear, target: DailyMissionTarget.Maze, conditionType: (int)DailyMissionCondition.Time) with { ConditionValue = 300 });
        var type = (Mission(4, finish: DailyMissionFinish.MazeClear, target: DailyMissionTarget.Maze, conditionType: (int)DailyMissionCondition.MazeType) with { ConditionValue = 2 });

        Assert.IsTrue(DailyMissionRules.CountsMazeClear(anyMaze, new DailyMissionMazeClear(21111, 1, 999)));

        Assert.IsTrue(DailyMissionRules.CountsMazeClear(rank, new DailyMissionMazeClear(21111, 5, 999)));
        Assert.IsFalse(DailyMissionRules.CountsMazeClear(rank, new DailyMissionMazeClear(21111, 4, 999)));
        Assert.IsFalse(DailyMissionRules.CountsMazeClear(rank, new DailyMissionMazeClear(21112, 5, 999)));

        Assert.IsTrue(DailyMissionRules.CountsMazeClear(fast, new DailyMissionMazeClear(1, 1, 300)));
        Assert.IsFalse(DailyMissionRules.CountsMazeClear(fast, new DailyMissionMazeClear(1, 1, 301)));

        Assert.IsTrue(DailyMissionRules.CountsMazeClear(type, new DailyMissionMazeClear(1, 1, 1, 2, 0, 0, [])));
        Assert.IsFalse(DailyMissionRules.CountsMazeClear(type, new DailyMissionMazeClear(1, 1, 1, 3, 0, 0, [])));
    }

    [TestMethod]
    public void MazeClearWithAPartyCountsTheMembersOfSimilarLevelAndOfTheClass()
    {
        var party = new DailyMissionPartyMember[] { new(50, 1), new(52, 2), new(70, 2) };

        var levels = (Mission(1, finish: DailyMissionFinish.PartyMazeClear, target: DailyMissionTarget.Maze, conditionType: (int)DailyMissionCondition.LevelGap) with { ConditionValue = 5, ConditionCount = 2 });
        var classes = (Mission(2, finish: DailyMissionFinish.PartyMazeClear, target: DailyMissionTarget.Maze, conditionType: (int)DailyMissionCondition.Class) with { ConditionClass = 2, ConditionCount = 2 });
        var same = (Mission(3, finish: DailyMissionFinish.PartyMazeClear, target: DailyMissionTarget.Maze, conditionType: (int)DailyMissionCondition.Class) with { ConditionClass = 7, ConditionCount = 1 });

        var clear = new DailyMissionMazeClear(1, 1, 1, 0, 51, 2, party);

        Assert.IsTrue(DailyMissionRules.CountsMazeClear(levels, clear));
        Assert.IsTrue(DailyMissionRules.CountsMazeClear(classes, clear));
        Assert.IsTrue(DailyMissionRules.CountsMazeClear(same, clear));
        Assert.IsFalse(DailyMissionRules.CountsMazeClear(levels, clear with { Party = party[..1] }));
        Assert.IsFalse(DailyMissionRules.CountsMazeClear(same, clear with { Class = 3 }));
    }

    [TestMethod]
    public void BookKeepsOnlyKnownMissionsAndTrackerOfAcceptedOnes()
    {
        var catalog = DailyMissionCatalog.From([Mission(1), Mission(2)]);
        var book = new DailyMissionBook
        {
            Missions =
            [
                new DailyMissionEntry { Mission = 1, State = DailyMissionState.Accept, Helper = true },
                new DailyMissionEntry { Mission = 2, State = DailyMissionState.Success, Helper = true },
                new DailyMissionEntry { Mission = 9, State = DailyMissionState.Accept, Helper = true },
                new DailyMissionEntry { Mission = 1 }
            ]
        };

        book.Normalize(catalog);

        CollectionAssert.AreEqual(new[] { 1, 2 }, book.Missions.Select(e => e.Mission).ToArray());
        Assert.AreEqual(1, book.HelperCount);
        Assert.IsFalse(book.Find(2)!.Helper);
    }

    [TestMethod]
    public void TableRowsBecomeDefinitions()
    {
        var catalog = new DailyMissionCatalog(
            TestTables.Load<DailyMissionRow>("tb_Daily_Mission"),
            TestTables.Load<MonsterRow>("tb_Monster"));

        Assert.IsTrue(catalog.TryGet(1100001, out var first));
        Assert.AreEqual(DailyMissionType.Special, first.Type);
        Assert.AreEqual(10003, first.District);
        Assert.AreEqual(DailyMissionFinish.Kill, first.Finish);
        Assert.AreEqual(DailyMissionTarget.MonsterGroup, first.Target);
        Assert.AreEqual(1100001, first.TargetId);
        Assert.AreEqual(25, first.MissionCount);
        CollectionAssert.AreEqual(new[] { new RewardItem(800000999, 1), new RewardItem(820999001, 1) }, first.Rewards.ToArray());

        Assert.IsTrue(catalog.TryGet(2100001, out var guerrilla));
        Assert.AreEqual(DailyMissionType.Guerrilla, guerrilla.Type);
        Assert.AreEqual(TimeSpan.FromHours(15), guerrilla.Dates.Start.TimeOfDay);
        Assert.AreEqual(TimeSpan.FromHours(16), guerrilla.Dates.End.TimeOfDay);

        // The monster 918 is in the group of the mission 1100005, which asks for a kill with a skill.
        Assert.IsTrue(catalog.TryGetMonster(918, out var monster));
        Assert.AreEqual(1100005, monster.Group1);

        Assert.IsTrue(catalog.TryGet(1100005, out var skill));
        Assert.IsTrue(DailyMissionRules.CountsKill(skill, catalog, 918, 1u << (int)DailyMissionCondition.SkillAttack));
        Assert.IsFalse(DailyMissionRules.CountsKill(skill, catalog, 918, 1u << (int)DailyMissionCondition.NormalAttack));
    }

    [TestMethod]
    public void RealTableGivesEveryDistrictItsMissions()
    {
        var catalog = new DailyMissionCatalog(
            TestTables.Load<DailyMissionRow>("tb_Daily_Mission"),
            TestTables.Load<MonsterRow>("tb_Monster"));

        for (var seed = 0; seed < 10; seed++)
        {
            var special = DailyMissionRules.Draw(catalog, DailyMissionType.Special, At(12), new Random(seed));
            var guerrilla = DailyMissionRules.Draw(catalog, DailyMissionType.Guerrilla, At(12), new Random(seed));

            Assert.HasCount(4 * 5, special);
            Assert.HasCount(4, guerrilla);
            Assert.AreEqual(special.Count, special.Distinct().Count());
        }
    }
}
