using System.Buffers.Binary;
using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Drops;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Gameplay.Modules.Maze.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Gameplay.Modules.Progress.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The end of a maze: the score of a run from the reward tables of the server, the rewards of its rank, the window of each
/// member of a party, the fatigue a maze takes and gives back, and the achievements of a clear by rank. The tables are made
/// here; the rewards are given against the item tables of the client (OPENWORKER_TEST_RES).
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MazeResultsTest
{
    private const int Maze = 21311;

    private static ReadOnlyCollection<T> Rows<T>(params Action<BinaryWriter>[] writes) where T : struct
    {
        var rows = new List<T>();

        foreach (var write in writes)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            write(writer);
            stream.Position = 0;

            using var reader = new BinaryReader(stream);

            rows.Add((T)Activator.CreateInstance(typeof(T), reader)!);
        }

        return new ReadOnlyCollection<T>(rows);
    }

    internal static ReadOnlyCollection<T> None<T>() => new([]);

    /// <summary>The points of 21311 as the server's table has them, with points for a party of two.</summary>
    private static ReadOnlyCollection<MazeRewardStandardRow> Standard() => Rows<MazeRewardStandardRow>(w =>
    {
        w.Write((short)Maze);
        w.Write((ushort)1000);
        w.Write((short)5);
        w.Write((short)2000);
        w.Write((short)0);

        foreach (var points in new short[] { 0, 100, 0, 0 })
        {
            w.Write(points);
        }

        foreach (var points in new[] { 11221, 12342, 14584, 24808, 27646 })
        {
            w.Write(points);
        }
    });

    /// <summary>A row of points of what a player did, by the count it is about.</summary>
    private static Action<BinaryWriter> Play(ushort count, uint combo = 0, uint damage = 0, uint hitsRate = 100) => w =>
    {
        w.Write((ushort)(count + 1));
        w.Write((uint)count);
        w.Write(combo);
        w.Write(0u);
        w.Write(0u);
        w.Write(0u);
        w.Write(damage);
        w.Write(hitsRate);
    };

    /// <summary>The server's row of the maze: the clear time, the experience and the gold, the level and the maze before.</summary>
    private static Action<BinaryWriter> Rule(ushort maze, byte level, ushort clearTime, uint experience, uint gold, uint before = 0) => w =>
    {
        w.Write(maze);
        w.Write((byte)0);
        w.Write((ushort)33);
        w.Write(level);
        w.Write((byte)0);
        w.Write((ushort)4);
        w.Write(new byte[2 + 1 + 1 + 4]);
        w.Write(0u);
        w.Write(0u);
        w.Write(new byte[1 + 2]);
        w.Write((byte)0);
        w.Write((byte)0);
        w.Write((byte)1);
        w.Write(new byte[1 + 3 + 12]);
        w.Write(before);
        w.Write(new byte[4]);
        w.Write(clearTime);
        w.Write(experience);
        w.Write(gold);
        w.Write((ushort)0);
        w.Write((ushort)0);
        w.Write((ushort)0);
        w.Write(0u);
        w.Write(0f);
        w.Write((ushort)0);
        w.Write(new byte[51]);
    };

    private static ReadOnlyCollection<MazeRankShareRow> RankShares() => Rows<MazeRankShareRow>(
        Enumerable.Range(0, 6).Select<int, Action<BinaryWriter>>(rank => w =>
        {
            w.Write((byte)rank);
            w.Write(0u);
            w.Write(0u);
            w.Write((byte)rank);
            w.Write(0.02f * (rank + 1));
            w.Write(0.2f * (rank + 1));
            w.Write(1f);
        }).ToArray());

    /// <summary>The client's row of a maze: its id, the level it asks for, who may enter and its fatigue.</summary>
    internal static MazeInfoRow MazeRow(short id, byte level, short fatigue, byte admission = 0, byte type = 0)
    {
        var bytes = new byte[4096];

        BitConverter.TryWriteBytes(bytes, id);
        bytes[2] = type;
        bytes[5] = level;
        bytes[6] = admission;
        BitConverter.TryWriteBytes(bytes.AsSpan(7), fatigue);

        return new MazeInfoRow(new BinaryReader(new MemoryStream(bytes)));
    }

    internal static MazeScoring Scoring(ReadOnlyCollection<MazePlayScoreRow> plays, ReadOnlyCollection<MazeRuleRow> rules, ReadOnlyCollection<MazePartyShareRow>? parties = null) => new(
        Standard(),
        RankShares(),
        Rows<MazeTimeScoreRow>(w =>
        {
            w.Write((ushort)1);
            w.Write((ushort)1);
            w.Write((ushort)3000);
            w.Write(0.006f);
        }),
        Rows<MazeDifficultyScoreRow>(w =>
        {
            w.Write((byte)1);
            w.Write((byte)0);
            w.Write(1f);
        }),
        Rows<MazeLevelScoreRow>(w =>
        {
            w.Write((byte)8);
            w.Write((byte)7);
            w.Write(0.94f);
        }),
        plays,
        parties ?? None<MazePartyShareRow>(),
        Rows<MazeComboRevisionRow>(w =>
        {
            w.Write((ushort)Hero.Haru);
            w.Write(1.5f);
            w.Write(1f);
        }),
        rules,
        None<MazeInfoRow>());

    [TestMethod]
    public void AClearIsScoredByWhatThePlayerDidAndRankedByTheTable()
    {
        var scoring = Scoring(
            Rows<MazePlayScoreRow>(Play(75, combo: 4950), Play(99, damage: 10000), Play(20, hitsRate: 97)),
            Rows<MazeRuleRow>(Rule(Maze, level: 6, clearTime: 369, experience: 13820, gold: 1069)));

        // Haru, level 14, a longest combo of 50, six kills, all of the damage, 21 hits taken, alone, 200 seconds.
        var fast = scoring.Score(new MazeRun(Maze, (int)Hero.Haru, 14, 50, 6, 100, 21, 1, 200_000));

        // The clear, the combo of 50 times 1.5 (row 76), the kills, the damage (row 100), kept at 97 percent for 21 hits;
        // then times one plus 169 seconds left at 0.006, and 0.94 for a level eight above the maze.
        var points = (1000f + 4950f + 2000f + 10000f) * 97f / 100f;
        var expected = points * (169 * 0.006f + 1f) * 0.94f;

        Assert.AreEqual(expected, fast.PlayPoint, 1f);
        Assert.AreEqual((byte)5, fast.Rank, "above the points of S is SS");
        Assert.AreEqual(1000, fast.ClearPoint);
        Assert.AreEqual(0.12f, fast.ExperienceShare, 0.0001f, "the share of SS");

        // The same run past the clear time counts one second left, and falls to A.
        var slow = scoring.Score(new MazeRun(Maze, (int)Hero.Haru, 14, 50, 6, 100, 21, 1, 400_000));

        Assert.AreEqual(points * (1 * 0.006f + 1f) * 0.94f, slow.PlayPoint, 1f);
        Assert.AreEqual((byte)3, slow.Rank);

        // A maze without points has no score: F, with the whole of its experience and gold.
        Assert.AreEqual(new MazeScore(0f, 0, 0, 1f, 1f), scoring.Score(new MazeRun(Maze + 10, 1, 14, 50, 6, 100, 0, 1, 1)));
    }

    /// <summary>
    /// Players of a maze with the inventory of a new person of the client's tables, and their windows.
    /// </summary>
    private sealed class Fixture : IDisposable
    {
        private const int BoxOfF = 900001;
        private const int BoxAbove = 900002;

        private readonly Dictionary<Entity, RecordingConnection> _connections = [];

        public World World { get; } = World.Create();
        public InventoryService Inventory { get; }
        public MazeResults Results { get; }

        /// <summary>The item of the bucket of F, and the item of every bucket above it.</summary>
        public int ItemOfF { get; }

        public int ItemAbove { get; }

        private readonly ItemCatalog _catalog;
        private readonly LocalItemSerials _serials = new();

        public Fixture(ReadOnlyCollection<MazePlayScoreRow>? plays = null, ReadOnlyCollection<MazePartyShareRow>? parties = null)
        {
            var items = TestTables.Load<ItemRow>("tb_Item");

            _catalog = new ItemCatalog(items, TestTables.Load<ItemClassifyRow>("tb_Item_Classify"), TestTables.Load<InvenSlotExtendRow>("tb_Inven_Slot_Extend"),
                TestTables.Load<BankSlotExtendRow>("tb_Bank_Slot_Extend"));
            Inventory = new InventoryService(World, _catalog, _serials);

            var stacks = items.Where(e => e.Stack >= 10 && _catalog.HomeOf(e.Id) == StorageGroup.Common).Select(e => e.Id).Skip(50).Take(2).ToArray();

            ItemOfF = stacks[0];
            ItemAbove = stacks[1];

            // A reward row whose buckets have the usual rates: the rank F reaches its own bucket only.
            var reward = Rows<MazeRewardItemRow>(w =>
            {
                w.Write((uint)Maze);
                w.Write((ushort)0);
                w.Write(0u);

                for (var bucket = 0; bucket < MazeRewardItemRow.RankCount; bucket++)
                {
                    w.Write((uint)(bucket == 0 ? BoxOfF : BoxAbove));
                }

                foreach (var rate in new ushort[] { 10, 30, 120, 480, 1920, 7680 })
                {
                    w.Write(rate);
                }

                w.Write(new byte[MazeRewardItemRow.RankCount * 8]);
                w.Write(0u);
                w.Write(0u);
                w.Write((byte)0);
                w.Write((byte)0);
            });

            var keys = new MazeKeyRewards(Inventory, reward, Rows<ItemRandomBoxRow>(Box(BoxOfF, ItemOfF, 2), Box(BoxAbove, ItemAbove, 1)), NullLogger<MazeKeyRewards>.Instance);

            // A maze worth 100 experience and 50 gold; its points are too high for a run without kills to pass F.
            var scoring = Scoring(plays ?? None<MazePlayScoreRow>(), Rows<MazeRuleRow>(Rule(Maze, level: 1, clearTime: 1, experience: 100, gold: 50)), parties);

            Results = new MazeResults(World, keys, scoring: scoring, inventory: Inventory);
        }

        private static Action<BinaryWriter> Box(int id, int item, int count) => w =>
        {
            w.Write(id);
            w.Write((byte)1);
            w.Write(1000);
            w.Write(0);
            w.Write((short)0);
            w.Write((short)0);
            w.Write((short)0);

            for (var i = 0; i < 15; i++)
            {
                w.Write(i == 0 ? (short)1000 : (short)0);
            }

            w.Write(0);
            w.Write(0);

            for (var i = 0; i < 15; i++)
            {
                w.Write(i == 0 ? item : 0);
            }

            for (var i = 0; i < 15; i++)
            {
                w.Write(i == 0 ? count : 0);
            }
        };

        public Entity Player(int id, string name)
        {
            var player = World.Create(
                Component.GetComponentType(typeof(ActorComponent)),
                Component.GetComponentType(typeof(InventoryComponent)),
                Component.GetComponentType(typeof(PersonInfoComponent)),
                Component.GetComponentType(typeof(CurrencyComponent)),
                Component.GetComponentType(typeof(LevelComponent)),
                Component.GetComponentType(typeof(ServerSessionComponent)));

            var person = new PersonPersistent
            {
                Id = id,
                Name = name,
                Hero = Hero.Haru,
                DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
                EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0,
                Location = 0, PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0,
                Account = null!, Gate = null!
            };

            var connection = new RecordingConnection();

            _connections[player] = connection;
            World.Set(player, new ActorComponent(id, ActorType.User));
            World.Set(player, new InventoryComponent(new InventoryFactory(_catalog, TestTables.Load<CreateClothRow>("tb_Create_Cloth"), _serials).CreateNew(person)));
            World.Set(player, new PersonInfoComponent { Name = name, Hero = Hero.Haru });
            World.Set(player, new CurrencyComponent());
            World.Set(player, new LevelComponent(1, 0));
            World.Set(player, new ServerSessionComponent(connection));

            return player;
        }

        public async Task<PlayerFrame[]> ReceivedAsync(Entity player, MazeOpcode command, int count)
        {
            PlayerFrame[] Of() => _connections[player].Frames.Select(PlayerFrame.Open).Where(e => e.Is(GroupOpcode.Maze, (byte)command)).ToArray();

            await MetricAssertions.UntilAsync(() => Of().Length >= count, $"The player gets {count} of 11/{(byte)command:x2}.");

            return Of();
        }

        public PlayerFrame[] Received(Entity player, MazeOpcode command) =>
            _connections[player].Frames.Select(PlayerFrame.Open).Where(e => e.Is(GroupOpcode.Maze, (byte)command)).ToArray();

        public void Dispose()
        {
            World.Dispose();
        }
    }

    [TestMethod]
    public async Task TheRewardOfARankIsGivenOnceAndItsItemComesFromTheBucketOfThatRank()
    {
        using var f = new Fixture();
        var player = f.Player(7, "Haru");

        f.Results.Start(player, Maze);
        f.Results.Complete(player);
        f.Results.Complete(player);

        var window = (await f.ReceivedAsync(player, MazeOpcode.eSUB_CMD_MAZE_REWARD_INFO, 1)).Single().Body;

        Assert.AreEqual(0, window[0], "too few points for more than F");
        Assert.AreEqual(2, BinaryPrimitives.ReadInt32LittleEndian(window.AsSpan(15)), "the experience of the maze at the share of F");
        Assert.AreEqual(10, BinaryPrimitives.ReadInt32LittleEndian(window.AsSpan(19)), "and its gold");
        Assert.AreEqual(f.ItemOfF, BinaryPrimitives.ReadInt32LittleEndian(window.AsSpan(43)), "the item of the bucket of F");
        Assert.AreEqual(2, BinaryPrimitives.ReadInt16LittleEndian(window.AsSpan(47)));

        Assert.AreEqual(10, f.Inventory.Gold(player), "the gold is given once");
        Assert.AreEqual(2, f.Inventory.CountOf(player, f.ItemOfF), "the item is given once");
        Assert.AreEqual(0, f.Inventory.CountOf(player, f.ItemAbove), "F never reaches the buckets above it");
        Assert.AreEqual((byte)0, f.Results.LastClear(player)!.Value.Rank);
        Assert.IsEmpty(f.Received(player, MazeOpcode.eSUB_CMD_MAZE_REWARD_PARTY_INFO), "a player alone gets no list of members");
    }

    [TestMethod]
    public async Task EachMemberOfAPartyGetsItsOwnResultAndTheListNamesEveryMemberWithItsRank()
    {
        // The share of the damage is what tells the two apart: three quarters are worth much more than a quarter.
        using var f = new Fixture(Rows<MazePlayScoreRow>(Play(74, damage: 24000), Play(24, damage: 100)));
        var first = f.Player(7, "Haru");
        var second = f.Player(8, "Lily");

        f.Results.Start(first, Maze);
        f.Results.Start(second, Maze);
        f.Results.OnMonstersHit(second, 1, 300);
        f.Results.OnMonstersHit(first, 1, 100);
        f.Results.Complete([first, second]);

        var windows = new[] { first, second }.Select(e => f.Received(e, MazeOpcode.eSUB_CMD_MAZE_REWARD_INFO).Single().Body).ToArray();

        Assert.AreEqual((byte)0, windows[0][0]);
        Assert.AreEqual((byte)4, windows[1][0], "the member with most of the damage ranks S");

        foreach (var member in new[] { first, second })
        {
            var list = (await f.ReceivedAsync(member, MazeOpcode.eSUB_CMD_MAZE_REWARD_PARTY_INFO, 1)).Single().Body;

            Assert.AreEqual(2, list[0], "both members are listed for each");

            // The order of the scores: the second member first, as place 1 with its rank, then the first.
            Assert.AreEqual(1, list[1]);
            Assert.AreEqual(4, list[2]);
            Assert.AreEqual(8, BinaryPrimitives.ReadInt32LittleEndian(list.AsSpan(3)));

            // The name is its length in bytes with the terminator, the characters and the terminator.
            var next = 1 + 1 + 1 + 4 + 2 + "Lily".Length * 2 + 1 + 4;

            Assert.AreEqual(2, list[next]);
            Assert.AreEqual(0, list[next + 1]);
            Assert.AreEqual(7, BinaryPrimitives.ReadInt32LittleEndian(list.AsSpan(next + 2)));
        }
    }

    /// <summary>
    /// A person in a world with fatigue, a level and a clock.
    /// </summary>
    private sealed class FatigueFixture : IDisposable
    {
        public World World { get; } = World.Create();
        public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 9, 12, 0, 0, TimeSpan.Zero));
        public FatigueService Fatigue { get; }
        public MazeAdmission Admission { get; }
        public MazeResults Results { get; }
        public Entity Player { get; }
        public PersonDirty Dirty { get; } = new(7);

        public FatigueFixture(short common, short bonus, DateTime? refilled)
        {
            var mazes = new ReadOnlyCollection<MazeInfoRow>([MazeRow(Maze, level: 6, fatigue: 9)]);

            Fatigue = new FatigueService(World, mazes, Time, Options.Create(new DailyMissionOptions()), NullLogger<FatigueService>.Instance);

            var progress = new SharePointManager(World, new PersonStateStore(null!), SharePointCatalog.Of([]), null!, NullLogger<SharePointManager>.Instance);

            Admission = new MazeAdmission(World, mazes, Fatigue, Scoring(None<MazePlayScoreRow>(), None<MazeRuleRow>()), progress);
            Results = new MazeResults(World, fatigue: Fatigue);

            Player = World.Create(
                Component.GetComponentType(typeof(FatiguePointsComponent)),
                Component.GetComponentType(typeof(LevelComponent)),
                Component.GetComponentType(typeof(DirtyComponent)),
                Component.GetComponentType(typeof(ServerSessionComponent)));

            World.Set(Player, new FatiguePointsComponent { Common = common, Bonus = bonus, RefilledAt = refilled });
            World.Set(Player, new LevelComponent(10, 0));
            World.Set(Player, new DirtyComponent(Dirty));
            World.Set(Player, new ServerSessionComponent(new RecordingConnection()));
        }

        public FatiguePointsComponent Points => World.Get<FatiguePointsComponent>(Player);

        public void Dispose()
        {
            World.Dispose();
        }
    }

    [TestMethod]
    public void EnteringTakesTheFatigueOfTheMazeAndIsRefusedWithoutIt()
    {
        var today = new DateTime(2026, 11, 9, 11, 30, 0, DateTimeKind.Utc);

        using var f = new FatigueFixture(common: 10, bonus: 2, refilled: today);

        Assert.IsNull(f.Admission.Check(f.Player, Maze), "12 points pay for a maze of 9");

        // Half of 9, the saved points first; the other half at the clear.
        f.Results.Start(f.Player, Maze);

        Assert.AreEqual(new FatiguePointsComponent { Common = 8, Bonus = 0, RefilledAt = today }, f.Points);
        Assert.IsTrue(f.Dirty.Sections.HasFlag(PersonSections.Currencies), "the flush writes the fatigue with the row");

        f.Results.Complete(f.Player);

        Assert.AreEqual(4, f.Points.Common);
        Assert.AreEqual(FatigueService.NotEnough, f.Admission.Check(f.Player, Maze), "four points do not pay for nine");

        f.World.Set(f.Player, new LevelComponent(5, 0));
        f.World.Set(f.Player, f.Points with { Common = 200 });

        Assert.AreEqual(MazeAdmission.LevelTooLow, f.Admission.Check(f.Player, Maze), "the level is checked before the fatigue");
    }

    [TestMethod]
    public void FatigueFillsUpAtTheStartOfEachGameDayAndSavesWhatWasLeft()
    {
        // Filled yesterday after the start of the day (11:00), 120 points left.
        using var f = new FatigueFixture(common: 120, bonus: 30, refilled: new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc));

        Assert.IsTrue(f.Fatigue.Refresh(f.Player, out var today));
        Assert.AreEqual(FatigueService.MaxCommon, today.Common, "the points of the day fill up");
        Assert.AreEqual(30 + 60, today.Bonus, "half of what was left is saved");

        f.Fatigue.Spend(f.Player, 150);

        Assert.IsTrue(f.Fatigue.Refresh(f.Player, out var same));
        Assert.AreEqual(140, same.Common, "not again on the same day");

        // Four whole days away and more: half of what was left, a day's worth for each whole day missed, up to the limit.
        f.Time.Advance(TimeSpan.FromDays(5));

        Assert.IsTrue(f.Fatigue.Refresh(f.Player, out var later));
        Assert.AreEqual(FatigueService.MaxCommon, later.Common);
        Assert.AreEqual(FatigueService.MaxBonus, later.Bonus, "saved points stop at their limit");
    }

    [TestMethod]
    public async Task AClearByRankCountsForTheAchievementOfItsRank()
    {
        using var world = World.Create();

        const int achievement = 4003701;
        var catalog = AchievementCatalog.Of(
            [new AchievementRule(achievement, 1, 37, (ushort)AchievementType.MazeClearedRankSS, 0, 1, 0, 0, 0, [], [])],
            [new AchievementBegin(achievement, 1)]);
        var store = new PersonStateStore(new EmptyStore());
        var achievements = new AchievementManager(world, store, catalog, null!, null!, new ReadOnlyCollection<MonsterRow>([]), NullLogger<AchievementManager>.Instance);

        var player = world.Create(
            Component.GetComponentType(typeof(LevelComponent)),
            Component.GetComponentType(typeof(AchievementComponent)),
            Component.GetComponentType(typeof(ServerSessionComponent)));

        world.Set(player, new LevelComponent(10, 0));
        world.Set(player, new ServerSessionComponent(new RecordingConnection()));

        await achievements.EnterAsync(player, 7, CancellationToken.None);

        achievements.OnMazeCleared(player, Maze, seconds: 600, kind: 0, rank: 4);

        Assert.AreEqual(0, world.Get<AchievementComponent>(player).Book!.Counters.Single().Count, "a clear at S is not one at SS");

        achievements.OnMazeCleared(player, Maze, seconds: 600, kind: 0, rank: 5);

        Assert.AreEqual(1, world.Get<AchievementComponent>(player).Book!.Counters.Single().Count);
    }

    /// <summary>
    /// A store with nothing in it: every person enters for the first time.
    /// </summary>
    private sealed class EmptyStore : IPersonStore
    {
        public Task<List<ItemPersistent>> LoadItemsAsync(int person, CancellationToken cancellationToken = default) => Task.FromResult(new List<ItemPersistent>());

        public Task<long[]> ReserveSerialsAsync(int count, CancellationToken cancellationToken = default) => Task.FromResult(new long[count]);

        public Task<string?> LoadDocumentAsync(int person, PersonStateKind kind, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);

        public Task SaveChangesAsync(IReadOnlyList<PersonChanges> batch, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
