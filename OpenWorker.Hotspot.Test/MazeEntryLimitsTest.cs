using System.Buffers.Binary;
using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Gameplay.Modules.Maze.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.UpdateContent.Res.Rows;
using static OpenWorker.Hotspot.Test.MazeRuleRows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// How often a person may enter a maze in a day, the days a casual raid is open, the windows of a maze, and what the client
/// is told about it. The tables are made here.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MazeEntryLimitsTest
{
    private const int Person = 7;
    private const int Actor = 0x2007;

    private sealed class Fixture : IDisposable
    {
        public World World { get; } = World.Create();

        // A Monday, after the start of the game day (11:00).
        public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 9, 12, 0, 0, TimeSpan.Zero));
        public RecordingConnection Connection { get; } = new();
        public MazeEntryLimits Limits { get; }
        public Entity Player { get; }
        public PersonDirty Dirty { get; } = new(Person);

        public Fixture(
            ReadOnlyCollection<MazeRuleRow> rules,
            ReadOnlyCollection<MazeDailyPortalRow>? portals = null,
            ReadOnlyCollection<MazeOpenControlRow>? windows = null)
        {
            Limits = new MazeEntryLimits(
                World,
                new PersonStateStore(null!),
                rules,
                portals ?? new ReadOnlyCollection<MazeDailyPortalRow>([]),
                windows ?? new ReadOnlyCollection<MazeOpenControlRow>([]),
                Time,
                Options.Create(new DailyMissionOptions()),
                NullLogger<MazeEntryLimits>.Instance);

            Player = World.Create(
                Component.GetComponentType(typeof(ActorComponent)),
                Component.GetComponentType(typeof(MazeEntryComponent)),
                Component.GetComponentType(typeof(DirtyComponent)),
                Component.GetComponentType(typeof(ServerSessionComponent)));

            World.Set(Player, new ActorComponent(Actor, ActorType.User));
            World.Set(Player, new MazeEntryComponent { Person = Person, Book = new MazeEntryBook { Day = Limits.Today } });
            World.Set(Player, new DirtyComponent(Dirty));
            World.Set(Player, new ServerSessionComponent(Connection));
        }

        public MazeEntryBook Book => World.Get<MazeEntryComponent>(Player).Book!;

        /// <summary>
        /// What the client was sent, once the writer has delivered at least <paramref name="count"/> frames.
        /// </summary>
        public PlayerFrame[] Sent(int count)
        {
            var until = DateTime.UtcNow.AddSeconds(5);

            while (Connection.Frames.Length < count && DateTime.UtcNow < until)
            {
                Thread.Sleep(10);
            }

            return Connection.Frames.Select(PlayerFrame.Open).ToArray();
        }

        public void Dispose()
        {
            World.Dispose();
        }
    }

    [TestMethod]
    public void AMazeCountsTheEntryWhenThePlayerComesInAndRefusesTheOneAfterTheLimit()
    {
        using var f = new Fixture(Rules(Rule(21061, limit: 2)));

        Assert.IsNull(f.Limits.Check(f.Player, 21061), "nothing entered yet");
        Assert.IsTrue(f.Limits.Entered(f.Player, 21061));
        f.Limits.TellCount(f.Player, 21061);
        Assert.IsNull(f.Limits.Check(f.Player, 21061), "one of two");
        Assert.IsTrue(f.Limits.Entered(f.Player, 21061));
        f.Limits.TellCount(f.Player, 21061);

        Assert.AreEqual(MazeEntryLimits.LimitReached, f.Limits.Check(f.Player, 21061), "two of two");
        Assert.IsNull(f.Limits.Check(f.Player, 21311), "a maze the table does not limit");
        Assert.IsTrue(f.Dirty.Sections.HasFlag(PersonSections.MiscState), "the flush writes the book");

        // The client is told the count of the maze after each entry: its actor, the maze and the count.
        var told = f.Sent(2);

        Assert.AreEqual((byte)WorldOpcode.EnterMazeLimitCountUpdate, told[0].Opcode.Command);
        Assert.AreEqual(Actor, told[0].Actor);
        Assert.AreEqual((21061, 1), (BinaryPrimitives.ReadUInt16LittleEndian(told[0].Body.AsSpan(4)), BinaryPrimitives.ReadUInt16LittleEndian(told[0].Body.AsSpan(6))));
        Assert.AreEqual(2, BinaryPrimitives.ReadUInt16LittleEndian(told[1].Body.AsSpan(6)));
    }

    [TestMethod]
    public void MazesOfOneGroupShareACountAndTheClientHearsOfEach()
    {
        using var f = new Fixture(Rules(Rule(21071, entryGroup: 100, limit: 2), Rule(21072, entryGroup: 100, limit: 2)));

        f.Limits.Entered(f.Player, 21071);
        f.Limits.TellCount(f.Player, 21071);
        f.Limits.Entered(f.Player, 21072);
        f.Limits.TellCount(f.Player, 21072);

        Assert.AreEqual(MazeEntryLimits.LimitReached, f.Limits.Check(f.Player, 21071), "the second maze used the count of the first");
        Assert.AreEqual(MazeEntryLimits.LimitReached, f.Limits.Check(f.Player, 21072));
        Assert.AreEqual(2, f.Book.Groups[100]);
        Assert.IsEmpty(f.Book.Mazes, "a maze of a group has no count of its own");

        // Each entry tells the count of both mazes of the group.
        Assert.HasCount(4, f.Sent(4).Where(e => e.Opcode.Command == (byte)WorldOpcode.EnterMazeLimitCountUpdate).ToArray());
    }

    [TestMethod]
    public async Task ANewGameDayClearsTheCountsAndTheClientIsToldToDoTheSame()
    {
        using var f = new Fixture(Rules(Rule(21061, limit: 1)));

        f.Limits.Entered(f.Player, 21061);
        f.Limits.TryUseFreeRevive(f.Player);

        Assert.AreEqual(MazeEntryLimits.LimitReached, f.Limits.Check(f.Player, 21061));

        // Not yet: the game day lasts until eleven the next morning.
        f.Time.Advance(TimeSpan.FromHours(10));
        await f.Limits.TickAsync(CancellationToken.None);

        Assert.AreEqual(MazeEntryLimits.LimitReached, f.Limits.Check(f.Player, 21061), "the same game day");

        f.Time.Advance(TimeSpan.FromHours(13));
        await f.Limits.TickAsync(CancellationToken.None);

        Assert.IsNull(f.Limits.Check(f.Player, 21061), "a new game day");
        Assert.AreEqual(0, f.Limits.FreeRevivesUsed(f.Player));

        var reset = f.Sent(2).Last();

        Assert.AreEqual((byte)WorldOpcode.EnterMazeLimitCountReset, reset.Opcode.Command);
        Assert.AreEqual(Actor, reset.Actor);
    }

    [TestMethod]
    public void ACasualRaidIsOpenOnlyOnTheDaysItsPortalNames()
    {
        // The portal opens its own raid and the second on Monday, its own and the third on every other day.
        var days = new[]
        {
            ((ushort)1, (ushort)21011, (ushort)22011),
            ((ushort)2, (ushort)21011, (ushort)23011), ((ushort)2, (ushort)21011, (ushort)23011), ((ushort)2, (ushort)21011, (ushort)23011),
            ((ushort)2, (ushort)21011, (ushort)23011), ((ushort)2, (ushort)21011, (ushort)23011), ((ushort)2, (ushort)21011, (ushort)23011)
        };

        using var f = new Fixture(
            Rules(Rule(21011, type: 2, portal: 21011), Rule(22011, type: 2, portal: 21011), Rule(23011, type: 2, portal: 21011)),
            new ReadOnlyCollection<MazeDailyPortalRow>([Portal(21011, days)]));

        Assert.IsNull(f.Limits.Check(f.Player, 22011), "open on a Monday");
        Assert.AreEqual(MazeEntryLimits.NotOpenToday, f.Limits.Check(f.Player, 23011), "not on a Monday");

        f.Time.Advance(TimeSpan.FromDays(1));

        Assert.AreEqual(MazeEntryLimits.NotOpenToday, f.Limits.Check(f.Player, 22011));
        Assert.IsNull(f.Limits.Check(f.Player, 23011), "open on a Tuesday");
    }

    [TestMethod]
    public void AMazeWithWindowsIsOpenOnlyInThemAndOneWithoutIsAlwaysOpen()
    {
        // Mondays from eight in the evening for an hour.
        using var f = new Fixture(Rules(Rule(21061)), windows: new ReadOnlyCollection<MazeOpenControlRow>([Window(21061, day: 2, number: 1, openTime: 2000, minutes: 60)]));

        Assert.IsFalse(f.Limits.IsOpen(21061), "noon");
        Assert.IsTrue(f.Limits.IsOpen(21311), "no window");

        f.Time.Advance(TimeSpan.FromMinutes(8 * 60 + 30));

        Assert.IsTrue(f.Limits.IsOpen(21061), "half past eight");

        f.Time.Advance(TimeSpan.FromHours(1));

        Assert.IsFalse(f.Limits.IsOpen(21061), "half past nine");

        f.Time.Advance(TimeSpan.FromDays(1));

        Assert.IsTrue(f.Limits.IsOpen(21061), "no window on a Tuesday");
    }

    [TestMethod]
    public void TheMessagesAreWrittenAsTheClientReadsThem()
    {
        var actor = new ActorValue(Actor, ActorType.User);

        static byte[] Written(IResponseHotspotMessage message)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            message.Write(writer);
            writer.Flush();

            return stream.ToArray();
        }

        // A count of the list in a byte, then an actor, a maze and a count for each.
        var load = Written(new WorldEnterMazeLimitCountLoadResponse
        {
            Counts = [new MazeLimitCountValue { Actor = actor, Maze = 21061, Count = 2 }]
        });

        Assert.AreEqual(1 + 4 + 2 + 2, load.Length);
        Assert.AreEqual(1, load[0]);
        Assert.AreEqual((21061, 2), (BinaryPrimitives.ReadUInt16LittleEndian(load.AsSpan(5)), BinaryPrimitives.ReadUInt16LittleEndian(load.AsSpan(7))));

        Assert.AreEqual(4 + 2 + 2, Written(new WorldEnterMazeLimitCountUpdateResponse { Actor = actor, Maze = 21061, Count = 1 }).Length);
        Assert.AreEqual(4, Written(new WorldEnterMazeLimitCountResetResponse { Actor = actor }).Length);

        // An error answer is its code, and the number the text of the error is filled in with when there is one.
        var opcode = MessageOpcode.From(GroupOpcode.World, WorldOpcode.EnterReq);

        CollectionAssert.AreEqual(new byte[] { 0x84, 0xD7 }, Written(new ErrorMessageResponse(opcode, 55172)));
        CollectionAssert.AreEqual(new byte[] { 0x84, 0xD7, 0x15, 0xCD, 0x5B, 0x07 }, Written(new ErrorMessageResponse(opcode, 55172, 123456789u)));
        Assert.AreEqual((byte)0x81, new ErrorMessageResponse(opcode, 1).Opcode.Command, "the answer of the request has the error flag");
    }
}
