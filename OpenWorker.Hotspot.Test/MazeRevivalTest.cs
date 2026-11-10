using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Gameplay.Modules.Maze.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.UpdateContent.Res.Rows;
using static OpenWorker.Hotspot.Test.MazeRuleRows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// When a fallen player may get up in a maze, and how: the rules of the reference, with the codes it answers with. The tables
/// are made here.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MazeRevivalTest
{
    private const int Plain = 21311;
    private const int Tower = 20101;
    private const int Mode = 21101;

    private sealed class Fixture : IDisposable
    {
        public World World { get; } = World.Create();
        public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 9, 12, 0, 0, TimeSpan.Zero));
        public MazeEntryLimits Limits { get; }
        public MazeRevival Revival { get; }
        public Entity Player { get; }

        public Fixture()
        {
            // A plain maze whose players may use two revive items, a maze that lets nobody get up and a maze of the mode that
            // gets its players up by itself.
            var rules = Rules(
                Rule(Plain, revivalItems: 2),
                Rule(Tower, type: 7, revivalType: 1),
                Rule(Mode, type: 12, revivalType: 2, revivalItems: 10));

            Limits = new MazeEntryLimits(
                World,
                new PersonStateStore(null!),
                rules,
                new ReadOnlyCollection<MazeDailyPortalRow>([]),
                new ReadOnlyCollection<MazeOpenControlRow>([]),
                Time,
                Options.Create(new DailyMissionOptions()),
                NullLogger<MazeEntryLimits>.Instance);

            Revival = new MazeRevival(rules, Limits);

            Player = World.Create(Component.GetComponentType(typeof(MazeEntryComponent)), Component.GetComponentType(typeof(DirtyComponent)));
            World.Set(Player, new MazeEntryComponent { Person = 7, Book = new MazeEntryBook { Day = Limits.Today } });
            World.Set(Player, new DirtyComponent(new PersonDirty(7)));
        }

        public void Dispose()
        {
            World.Dispose();
        }
    }

    [TestMethod]
    public void AFallenPlayerGetsUpOnlyTheWaysTheReferenceLetsItAndIsRefusedWithItsCodes()
    {
        using var f = new Fixture();

        Assert.AreEqual(MazeRevival.NotFallen, f.Revival.Check(f.Player, Plain, fallen: false, ReviveType.MazeFree), "only a fallen player gets up");
        Assert.AreEqual(MazeRevival.NotInThisMaze, f.Revival.Check(f.Player, Tower, fallen: true, ReviveType.MazeFree), "a maze that lets nobody get up");
        Assert.AreEqual(MazeRevival.NotInThisMaze, f.Revival.Check(f.Player, Tower, fallen: false, ReviveType.MazeFree), "that comes before the fall");

        // The buff of another player, the safe place of a district and the way of the server itself are not a player's to ask.
        Assert.AreEqual(MazeRevival.NotAllowed, f.Revival.Check(f.Player, Plain, fallen: true, ReviveType.Buff));
        Assert.AreEqual(MazeRevival.NotAllowed, f.Revival.Check(f.Player, Plain, fallen: true, ReviveType.SafetyZone));
        Assert.AreEqual(MazeRevival.NotAllowed, f.Revival.Check(f.Player, Plain, fallen: true, ReviveType.Auto));

        // For cash, which a server without a shop cannot take.
        Assert.AreEqual(MazeRevival.NoFreeRevive, f.Revival.Check(f.Player, Plain, fallen: true, ReviveType.Cash));

        // The mode that revives its players, and no other maze.
        Assert.IsNull(f.Revival.Check(f.Player, Mode, fallen: true, ReviveType.ModeMazeAuto));
        Assert.AreEqual(MazeRevival.NotInThisMode, f.Revival.Check(f.Player, Plain, fallen: true, ReviveType.ModeMazeAuto));
    }

    [TestMethod]
    public void TheFreeRevivesOfADayAreUsedUpAndComeBackWithTheNextDay()
    {
        using var f = new Fixture();

        for (var used = 0; used < MazeEntryLimits.FreeRevivesPerDay; used++)
        {
            Assert.IsNull(f.Revival.Check(f.Player, Plain, fallen: true, ReviveType.MazeFree), $"free revive {used + 1}");
        }

        Assert.AreEqual(MazeRevival.NoFreeRevive, f.Revival.Check(f.Player, Plain, fallen: true, ReviveType.MazeFree), "all of them are used");
        Assert.IsTrue(f.World.Get<MazeEntryComponent>(f.Player).Book!.FreeRevives >= MazeEntryLimits.FreeRevivesPerDay);

        f.Time.Advance(TimeSpan.FromHours(24));

        Assert.IsNull(f.Revival.Check(f.Player, Plain, fallen: true, ReviveType.MazeFree), "the next game day");
    }

    [TestMethod]
    public async Task ARevivalItemIsUsedAsOftenAsTheMazeAllowsAndNoMoreThenInTheMazeThePlayerIsIn()
    {
        using var f = new Fixture();

        Assert.AreEqual(MazeRevival.NotFallen, f.Revival.CheckItem(f.Player, Plain, fallen: false));
        Assert.AreEqual(MazeRevival.NotInThisMaze, f.Revival.CheckItem(f.Player, Tower, fallen: true));

        // Two in this maze, as the row says; the third is refused.
        Assert.IsNull(f.Revival.CheckItem(f.Player, Plain, fallen: true));
        f.Revival.ItemUsed(f.Player);
        Assert.IsNull(f.Revival.CheckItem(f.Player, Plain, fallen: true));
        f.Revival.ItemUsed(f.Player);

        Assert.AreEqual(MazeRevival.ItemsOver, f.Revival.CheckItem(f.Player, Plain, fallen: true));

        // The count belongs to the connection of the player: another connection starts again.
        await f.Revival.OnSessionClosedAsync(f.Player);

        Assert.IsNull(f.Revival.CheckItem(f.Player, Plain, fallen: true));
    }
}
