using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.DistrictServer.Server;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Gameplay.Modules.Maze.Components;
using OpenWorker.Gameplay.Modules.Maze.Components;
using OpenWorker.Gameplay.Modules.Maze.Extensions;
using OpenWorker.Gameplay.Modules.Npcs;
using OpenWorker.Gameplay.Modules.Npcs.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Gameplay.Modules.Progress.Components;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Gameplay.Modules.Quests.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Gameplay.Modules.Skill;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Requests;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Persistence;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The write-behind against a real Postgres, through the stores, the writer and the flush service of the servers. Each run
/// migrates a database of its own and drops it after. Set OPENWORKER_TEST_POSTGRES to a connection string without a
/// database, for example <c>Host=localhost;Port=5432;Username=postgres;Password=postgres</c>.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PersistenceTest
{
    private const string Variable = "OPENWORKER_TEST_POSTGRES";

    // The district of the place that is kept.
    private const short District = 10003;

    private static IDbContextFactory<PersistenceContext> Factory { get; set; } = null!;

    private static ServiceProvider? Services { get; set; }

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        var connection = Environment.GetEnvironmentVariable(Variable);

        if (string.IsNullOrEmpty(connection))
        {
            Assert.Inconclusive($"{Variable} is not set.");
        }

        var database = "openworker_test_" + Guid.NewGuid().ToString("N");

        Services = new ServiceCollection()
            .AddPooledDbContextFactory<PersistenceContext>(builder => builder
                .UseNpgsql(connection + ";Database=" + database)
                .UsePersistenceDefaults())
            .BuildServiceProvider();

        Factory = Services.GetRequiredService<IDbContextFactory<PersistenceContext>>();

        await using var context = await Factory.CreateDbContextAsync();

        await context.Database.MigrateAsync();
    }

    [ClassCleanup]
    public static async Task CleanupAsync()
    {
        if (Services is null)
        {
            return;
        }

        await using (var context = await Factory.CreateDbContextAsync())
        {
            await context.Database.EnsureDeletedAsync();
        }

        await Services.DisposeAsync();
    }

    /// <summary>
    /// One server of the game, as far as the write-behind goes: the world, its loop, the real stores, the writer and the
    /// flush service.
    /// </summary>
    private sealed class Server : IAsyncDisposable
    {
        public World World { get; } = World.Create();
        public WorldLoop Loop { get; } = new(NullLogger<WorldLoop>.Instance);
        public InventoryPersistence Inventories { get; }
        public InventoryService Items { get; }
        public PersonSkills Skills { get; }
        public PersonStateQuestStore Quests { get; }
        public PersonStateStore States { get; }
        public PersistenceWriter Writer { get; }
        public PersistenceFlushService Flush { get; }
        public PersonFlush Close { get; }

        public Server()
        {
            var catalog = new ItemCatalog(Empty<ItemRow>(), Empty<ItemClassifyRow>(), Empty<InvenSlotExtendRow>(), Empty<BankSlotExtendRow>());
            var store = new PersonStore(Factory);
            var states = new PersonStateStore(store);

            States = states;

            Inventories = new InventoryPersistence(World, store, new InventoryFactory(catalog, Empty<CreateClothRow>(), new LocalItemSerials()),
                NullLogger<InventoryPersistence>.Instance, states);
            Items = new InventoryService(World, catalog, new LocalItemSerials());
            Skills = new PersonSkills(World, states, Empty<CharacterInfoRow>());
            Quests = new PersonStateQuestStore(states);

            var snapshots = new PersonSnapshots(
                World,
                [
                    new SkillDocumentSource(), QuestManager.Document, AkashicRecordBook.Document, AchievementManager.Document,
                    SharePointManager.Document, NpcCreditManager.Document, MazeEntryLimits.Document
                ],
                [new PersonPlaceStore(Options.Create(new InstanceOptions { Location = District }))]);

            Writer = new PersistenceWriter(store, Loop, snapshots, NullLogger<PersistenceWriter>.Instance);
            Flush = new PersistenceFlushService(Loop, snapshots, Writer, new DatabaseItemSerials(store),
                Options.Create(new PersistenceOptions()), new FakeTimeProvider(), NullLogger<PersistenceFlushService>.Instance);
            Close = new PersonFlush(snapshots, Writer);
        }

        public Task StartAsync() => Writer.StartAsync(CancellationToken.None);

        /// <summary>
        /// The player enters: what the district and the maze load for it.
        /// </summary>
        public Task<Entity> EnterAsync(int account, int person) => Loop.InvokeAsync(async () =>
        {
            var entry = (await Factory.LoadPersonAtEntryAsync(account, person, CancellationToken.None))!;

            var player = World.Create(
                Component.GetComponentType(typeof(ActorComponent)),
                Component.GetComponentType(typeof(WorldComponent)),
                Component.GetComponentType(typeof(InventoryComponent)),
                Component.GetComponentType(typeof(LevelComponent)),
                Component.GetComponentType(typeof(CurrencyComponent)),
                Component.GetComponentType(typeof(DirtyComponent)),
                Component.GetComponentType(typeof(QuestComponent)),
                Component.GetComponentType(typeof(AchievementComponent)),
                Component.GetComponentType(typeof(SharePointComponent)),
                Component.GetComponentType(typeof(NpcCreditComponent)),
                Component.GetComponentType(typeof(MazeEntryComponent)),
                Component.GetComponentType(typeof(SkillPointComponent)),
                Component.GetComponentType(typeof(SkillLearnedComponent)),
                Component.GetComponentType(typeof(SkillDeckComponent)),
                Component.GetComponentType(typeof(SkillDeckBonusComponent)),
                Component.GetComponentType(typeof(FatiguePointsComponent)));

            World.Set(player, new ActorComponent(person, ActorType.User));
            World.Set(player, entry.Person.ToFatiguePointsComponent());
            World.Set(player, new WorldComponent
            {
                Location = entry.Person.Location,
                Position = new Vector3(entry.Person.PositionX, entry.Person.PositionY, entry.Person.PositionZ),
                Rotation = entry.Person.RotationX,
                Jump = 0
            });

            await Inventories.LoadAsync(player, entry.Person);
            await Skills.LoadAsync(player, person, entry.Person.Hero, CancellationToken.None);

            World.Set(player, new QuestComponent { Person = person, Book = await Quests.LoadAsync(person, CancellationToken.None) ?? new QuestBook() });

            World.Set(player, new AchievementComponent
            {
                Person = person,
                Book = await States.LoadAsync<AchievementBook>(person, PersonStateKind.Achievements, CancellationToken.None) ?? new AchievementBook()
            });

            World.Set(player, new SharePointComponent
            {
                Person = person,
                Book = await States.LoadAsync<SharePointBook>(person, PersonStateKind.SharePoints, CancellationToken.None) ?? new SharePointBook()
            });

            World.Set(player, new NpcCreditComponent
            {
                Person = person,
                Book = await States.LoadAsync<NpcCreditBook>(person, PersonStateKind.NpcCredit, CancellationToken.None) ?? new NpcCreditBook()
            });

            World.Set(player, new MazeEntryComponent
            {
                Person = person,
                Book = await States.LoadAsync<MazeEntryBook>(person, PersonStateKind.MazeEntry, CancellationToken.None) ?? new MazeEntryBook()
            });

            return player;
        }).Unwrap();

        public Task RunAsync(Action action) => Loop.InvokeAsync(action);

        /// <summary>
        /// The connection closes: the flush of the person, as the close handler runs it on the loop.
        /// </summary>
        public Task LeaveAsync(Entity player) => Loop.InvokeAsync(() => Close.OnSessionClosedAsync(player));

        public async ValueTask DisposeAsync()
        {
            await Writer.StopAsync(CancellationToken.None);

            Loop.Dispose();
            World.Dispose();
        }
    }

    private static ReadOnlyCollection<T> Empty<T>() => new([]);

    private static async Task<(int Account, int Person)> CreatePersonAsync(string name)
    {
        await using var database = await Factory.CreateDbContextAsync();

        var account = new AccountPersistent { Username = name, PasswordHash = [], SaltHash = [] };
        var gate = new GatePersistent { Name = name };

        var person = new PersonPersistent
        {
            Name = name,
            Hero = Hero.Haru,
            DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
            EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0,
            Location = District, PositionX = 1, PositionY = 2, PositionZ = 3, RotationX = 0,
            FatiguePointCommon = 200,
            Account = account,
            Gate = gate
        };

        database.AddRange(account, gate, person);

        await database.SaveChangesAsync();

        return (account.Id, person.Id);
    }

    private static async Task<PersonPersistent> RowAsync(int person)
    {
        await using var database = await Factory.CreateDbContextAsync();

        return await database.Persons.FirstAsync(e => e.Id == person);
    }

    // When the fatigue was filled: today, to the microsecond the database keeps.
    private static readonly DateTime Refilled = new(DateTime.UtcNow.Ticks / 10 * 10, DateTimeKind.Utc);

    [TestMethod]
    public async Task ALogoutAndANewLoginKeepWhatThePlayerHad()
    {
        var (account, person) = await CreatePersonAsync("keeper");

        await using (var first = new Server())
        {
            await first.StartAsync();

            var player = await first.EnterAsync(account, person);

            // A round of the flush in the middle of the play, then more changes that only the logout writes.
            await first.RunAsync(() =>
            {
                var inventory = first.World.Get<InventoryComponent>(player).Value!;

                inventory.Place(StorageGroup.Common, 3, new ItemInstance { Serial = 900_001, Item = 710105001, Count = 7 });

                var wallet = first.World.Get<CurrencyComponent>(player);

                wallet[ShopCurrency.Gold] = 1_500;
                wallet[ShopCurrency.Cash] = 25;
                first.World.Set(player, new FatiguePointsComponent { Common = 120, Bonus = 30, RefilledAt = Refilled });
                first.World.MarkDirty(player, PersonSections.Currencies);
            });

            Assert.IsTrue(await first.Flush.FlushAsync());

            await first.RunAsync(() =>
            {
                var inventory = first.World.Get<InventoryComponent>(player).Value!;

                inventory.At(StorageGroup.Common, 3)!.Count = 9;
                inventory.Touch(inventory.At(StorageGroup.Common, 3)!);

                inventory.AkashicRecords.Records.Add(new AkashicRecord { Card = 381000101, Position = 139, Exp = 40 });
                first.World.MarkDirty(player, PersonSections.AkashicRecords);

                first.World.Set(player, new LevelComponent(12, 3_400));
                first.World.MarkDirty(player, PersonSections.Level);

                var learned = first.World.Get<SkillLearnedComponent>(player);
                first.World.Set(player, learned with { Values = [.. learned.Values, new SkillInfoValue { Skill = 4242, Divergence = 2 }] });
                first.World.Set(player, new SkillPointComponent { TotalSkillPoint = 40, FreeSkillPoint = 3 });
                first.World.MarkDirty(player, PersonSections.Skills);

                var quests = first.World.Get<QuestComponent>(player);
                quests.Book!.Episodes.Add(new QuestEpisodeState { Episode = 100, Helper = true });
                first.World.MarkDirty(player, PersonSections.Quests);

                var achievements = first.World.Get<AchievementComponent>(player).Book!;
                achievements.Counters.Add(new AchievementCounter { Type = (ushort)AchievementType.MazeCleared, Target = 0, Achievement = 4002102, Count = 1 });
                achievements.SetRewarded(41);

                var sharePoints = first.World.Get<SharePointComponent>(player).Book!;
                sharePoints.Districts.Add(new DistrictProgress { District = District, Point = 16 });
                sharePoints.Groups.Add(new MazeGroupProgress { Group = 32, Maze = 21211, District = District, Clears = 1, Point = 16, Step = 1 });
                sharePoints.Record(21211, 4, 300);
                first.World.MarkDirty(player, PersonSections.Progress);

                first.World.Set(player, first.World.Get<WorldComponent>(player) with { Position = new Vector3(50, 60, 70), Rotation = 90 });
            });

            await first.LeaveAsync(player);
        }

        await using var second = new Server();

        await second.StartAsync();

        var back = await second.EnterAsync(account, person);

        await second.RunAsync(() =>
        {
            var world = second.World;

            var item = world.Get<InventoryComponent>(back).Value!.At(StorageGroup.Common, 3);

            Assert.IsNotNull(item, "the item is where it was put");
            Assert.AreEqual(710105001, item.Item);
            Assert.AreEqual((short)9, item.Count, "the change after the round, written by the logout");

            var card = world.Get<InventoryComponent>(back).Value!.AkashicRecords.Records.Single();

            Assert.AreEqual((381000101, 139, 40), (card.Card, card.Position, card.Exp), "the card of the akashic record book");

            var wallet = world.Get<CurrencyComponent>(back);

            Assert.AreEqual(1_500, wallet.Gold);
            Assert.AreEqual(25, wallet.Cash, "a currency of the wallet without a column of its own");

            Assert.AreEqual(new LevelComponent(12, 3_400), world.Get<LevelComponent>(back));

            Assert.AreEqual((short)3, world.Get<SkillPointComponent>(back).FreeSkillPoint);
            Assert.IsTrue(world.Get<SkillLearnedComponent>(back).Values.Any(e => e is { Skill: 4242, Divergence: 2 }));

            Assert.AreEqual(100, world.Get<QuestComponent>(back).Book!.Episodes.Single().Episode);

            var achievements = world.Get<AchievementComponent>(back).Book!;

            Assert.AreEqual((4002102, 1L), achievements.Counters.Select(e => (e.Achievement, e.Count)).Single(), "the counter of the achievements");
            Assert.IsTrue(achievements.IsRewarded(41), "the reward taken");

            var sharePoints = world.Get<SharePointComponent>(back).Book!;

            Assert.AreEqual(16, sharePoints.FindDistrict(District)!.Point, "the points of the district");
            Assert.AreEqual((byte)1, sharePoints.FindGroup(32)!.Step, "the step of the group");
            Assert.AreEqual((1, (byte)4, 300), (sharePoints.FindMaze(21211)!.Clears, sharePoints.FindMaze(21211)!.BestRank, sharePoints.FindMaze(21211)!.BestSeconds), "the record of the maze");

            Assert.AreEqual(new FatiguePointsComponent { Common = 120, Bonus = 30, RefilledAt = Refilled }, world.Get<FatiguePointsComponent>(back), "the fatigue, with the row");

            Assert.AreEqual(new Vector3(50, 60, 70), world.Get<WorldComponent>(back).Position, "the place in the district");
        });
    }

    [TestMethod]
    public async Task AnItemMovedToTheBankAndBackAndTheCreditWithNpcsSurviveAFlush()
    {
        var (account, person) = await CreatePersonAsync("banker");

        static MoveItemValue Slot(StorageGroup storage, short index) => new() { Storage = storage, Index = index };

        // Into the bank, with credit gathered at a group of NPCs.
        await using (var first = new Server())
        {
            await first.StartAsync();

            var player = await first.EnterAsync(account, person);

            await first.RunAsync(() =>
            {
                var inventory = first.World.Get<InventoryComponent>(player).Value!;

                inventory.Place(StorageGroup.Common, 2, new ItemInstance { Serial = 900_201, Item = 710105001, Count = 4 });
                first.Items.Move(player, Slot(StorageGroup.Common, 2), Slot(StorageGroup.CommonStorage, 5));

                // A worn item goes to the bag before the bank.
                inventory.Place(StorageGroup.AbilityEquip, 0, new ItemInstance { Serial = 900_202, Item = 710105002, Count = 1 });
                first.Items.Move(player, Slot(StorageGroup.AbilityEquip, 0), Slot(StorageGroup.CommonStorage, 6));

                Assert.AreEqual(900_202, inventory.At(StorageGroup.AbilityEquip, 0)?.Serial, "a worn item stays out of the bank");

                first.World.Get<NpcCreditComponent>(player).Book!.Groups.Add(new NpcCreditEntry { Group = 1001, Grade = 2, Point = 7, DayPoint = 3 });

                var entries = first.World.Get<MazeEntryComponent>(player).Book!;

                entries.Day = new DateTime(2026, 11, 9, 11, 0, 0, DateTimeKind.Utc);
                entries.Mazes[21061] = 2;
                entries.Groups[100] = 1;
                entries.FreeRevives = 2;

                first.World.MarkDirty(player, PersonSections.MiscState);
            });

            await first.LeaveAsync(player);
        }

        // Out of the bank again.
        await using (var second = new Server())
        {
            await second.StartAsync();

            var player = await second.EnterAsync(account, person);

            await second.RunAsync(() =>
            {
                var inventory = second.World.Get<InventoryComponent>(player).Value!;

                Assert.AreEqual(900_201, inventory.At(StorageGroup.CommonStorage, 5)?.Serial, "the item is in the bank");
                Assert.IsNull(inventory.At(StorageGroup.Common, 2));

                var credit = second.World.Get<NpcCreditComponent>(player).Book!.Find(1001);

                Assert.AreEqual((byte)2, credit?.Grade, "the credit with the group");
                Assert.AreEqual(7, credit?.Point);

                var entries = second.World.Get<MazeEntryComponent>(player).Book!;

                Assert.AreEqual((2, 1, 2), (entries.Mazes[21061], entries.Groups[100], entries.FreeRevives), "the entries into the mazes today");

                second.Items.Move(player, Slot(StorageGroup.CommonStorage, 5), Slot(StorageGroup.Common, 0));
            });

            Assert.IsTrue(await second.Flush.FlushAsync());

            await second.LeaveAsync(player);
        }

        await using var third = new Server();

        await third.StartAsync();

        var back = await third.EnterAsync(account, person);

        await third.RunAsync(() =>
        {
            var inventory = third.World.Get<InventoryComponent>(back).Value!;

            Assert.AreEqual((short)4, inventory.At(StorageGroup.Common, 0)?.Count, "the item is back in the bag");
            Assert.IsNull(inventory.At(StorageGroup.CommonStorage, 5), "and no longer in the bank");
        });
    }

    [TestMethod]
    public async Task SerialsAskedForAheadAreReadyWithoutWaitingForTheDatabase()
    {
        var serials = new DatabaseItemSerials(new PersonStore(Factory));

        // More than one block: a creation of many items at once still finds them all.
        await serials.EnsureAsync(600);

        var taken = Enumerable.Range(0, 600).Select(_ => serials.NextSerial()).ToHashSet();

        Assert.HasCount(600, taken, "every serial is given once");
    }

    [TestMethod]
    public async Task AFlushWritesTheDirtyPlayerAndNotTheCleanOne()
    {
        var (dirtyAccount, dirtyPerson) = await CreatePersonAsync("dirty");
        var (cleanAccount, cleanPerson) = await CreatePersonAsync("clean");

        await using var server = new Server();

        await server.StartAsync();

        var dirty = await server.EnterAsync(dirtyAccount, dirtyPerson);
        var clean = await server.EnterAsync(cleanAccount, cleanPerson);

        // What entering marks (the first items of a person that had none) is written once.
        Assert.IsTrue(await server.Flush.FlushAsync());

        var before = await RowAsync(cleanPerson);

        await server.RunAsync(() =>
        {
            server.World.Set(dirty, new LevelComponent(30, 0));
            server.World.MarkDirty(dirty, PersonSections.Level);
        });

        Assert.IsTrue(await server.Flush.FlushAsync());

        Assert.AreEqual((byte)30, (await RowAsync(dirtyPerson)).Level);
        Assert.AreEqual(before.UpdatedAt, (await RowAsync(cleanPerson)).UpdatedAt, "nothing is written for a player that changed nothing");

        // The mask was taken: a round with no change writes nothing.
        Assert.AreEqual(0, await server.Loop.InvokeAsync(() => server.World.DirtyOf(dirty)!.Sections == PersonSections.None ? 0 : 1));
        Assert.IsTrue(await server.Loop.InvokeAsync(() => server.World.IsAlive(clean)));
    }

    /// <summary>
    /// Items put on a helper, swapped and taken off are written to a storage of their own where one place holds one item:
    /// the flush writes two items that trade a place without breaking the unique index, and a new entry finds each where it
    /// was left. The index refuses a second item at a taken place.
    /// </summary>
    [TestMethod]
    public async Task AHelperPlaceHoldsOneItemThroughSwapsFlushesAndANewEntry()
    {
        var (account, person) = await CreatePersonAsync("outfitter");
        var place = Inventory.HelperPlace(100, 0);

        await using (var first = new Server())
        {
            await first.StartAsync();

            var player = await first.EnterAsync(account, person);
            var items = new InventoryService(first.World, new ItemCatalog(Empty<ItemRow>(), Empty<ItemClassifyRow>(), Empty<InvenSlotExtendRow>(),
                Empty<BankSlotExtendRow>()), new LocalItemSerials());

            ItemInstance weapon = null!, other = null!;

            await first.RunAsync(() =>
            {
                var inventory = first.World.Get<InventoryComponent>(player).Value!;

                weapon = new ItemInstance { Serial = 900_101, Item = 360010102, Count = 1 };
                other = new ItemInstance { Serial = 900_102, Item = 360010103, Count = 1 };

                inventory.Place(StorageGroup.Common, 4, weapon);
                inventory.Place(StorageGroup.Common, 5, other);

                Assert.IsTrue(items.PutOnHelper(player, weapon, place, swap: false));
            });

            Assert.IsTrue(await first.Flush.FlushAsync());

            // The other item takes the place of the worn one, which goes to the bag slot it left.
            await first.RunAsync(() => Assert.IsTrue(items.PutOnHelper(player, other, place, swap: true)));

            Assert.IsTrue(await first.Flush.FlushAsync());

            // Taken off and the first one put back on, written by the logout in one go.
            await first.RunAsync(() =>
            {
                Assert.IsTrue(items.TakeOffHelper(player, place, StorageGroup.Common, 6));
                Assert.IsTrue(items.PutOnHelper(player, weapon, place, swap: false));
            });

            await first.LeaveAsync(player);
        }

        await using (var second = new Server())
        {
            await second.StartAsync();

            var back = await second.EnterAsync(account, person);

            await second.RunAsync(() =>
            {
                var inventory = second.World.Get<InventoryComponent>(back).Value!;

                Assert.AreEqual(900_101, inventory.WornByHelper(place)?.Serial, "the helper wears the item put back on");
                Assert.AreEqual(900_102, inventory.At(StorageGroup.Common, 6)?.Serial, "the item taken off is in the bag");
                Assert.IsNull(inventory.At(StorageGroup.Common, 4));
                Assert.IsNull(inventory.At(StorageGroup.Common, 5));
            });
        }

        await using var database = await Factory.CreateDbContextAsync();

        database.Items.Add(new ItemPersistent { Id = 900_103, PersonId = person, Storage = ItemPersistent.HelperStorage, Slot = place, Item = 360010104, Count = 1 });

        var refused = await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());

        Assert.AreEqual(Npgsql.PostgresErrorCodes.UniqueViolation, (refused.InnerException as Npgsql.PostgresException)?.SqlState);
    }
}
