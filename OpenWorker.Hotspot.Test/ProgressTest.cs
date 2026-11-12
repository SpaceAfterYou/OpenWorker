using System.Buffers.Binary;
using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Gameplay.Modules.Progress.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The achievements and the share points of a person, against the item tables of the client: set OPENWORKER_TEST_RES to the
/// folder with the *.res files. The achievements, districts and groups are made here.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ProgressTest
{
    private const int District = 10003;
    private const int Group = 32;
    private const int Maze = 21211;
    private const int FirstAchievement = 4002101;

    /// <summary>
    /// A store with nothing in it: every person enters for the first time.
    /// </summary>
    private sealed class EmptyStore : IPersonStore
    {
        public Task<List<ItemPersistent>> LoadItemsAsync(int person, CancellationToken cancellationToken = default) => Task.FromResult(new List<ItemPersistent>());

        public Task<long[]> ReserveSerialsAsync(int count, CancellationToken cancellationToken = default) => Task.FromResult(new long[count]);

        public Task<string?> LoadDocumentAsync(int person, PersonStateKind kind, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);

        public Task<Dictionary<PersonStateKind, string>> LoadDocumentsAsync(int person, CancellationToken cancellationToken = default) => Task.FromResult(new Dictionary<PersonStateKind, string>());

        public Task SaveChangesAsync(IReadOnlyList<PersonChanges> batch, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class Fixture : IDisposable
    {
        private readonly RecordingConnection _connection = new();

        public World World { get; } = World.Create();
        public InventoryService Inventory { get; }
        public AchievementManager Achievements { get; }
        public SharePointManager SharePoints { get; }
        public Entity Player { get; }
        public PersonDirty Dirty { get; } = new(7);

        /// <summary>The item of every reward: a stack of the common bag the new person does not have.</summary>
        public int Item { get; }

        public Fixture()
        {
            var items = TestTables.Load<ItemRow>("tb_Item");
            var catalog = new ItemCatalog(items, TestTables.Load<ItemClassifyRow>("tb_Item_Classify"), TestTables.Load<InvenSlotExtendRow>("tb_Inven_Slot_Extend"),
                TestTables.Load<BankSlotExtendRow>("tb_Bank_Slot_Extend"));
            var serials = new LocalItemSerials();
            var store = new PersonStateStore(new EmptyStore());

            Inventory = new InventoryService(World, catalog, serials);

            var person = new PersonPersistent
            {
                Id = 7,
                Name = "Test",
                Hero = Hero.Haru,
                DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
                EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0,
                Location = 0, PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0,
                Account = null!, Gate = null!
            };

            var bag = new InventoryFactory(catalog, TestTables.Load<CreateClothRow>("tb_Create_Cloth"), serials).CreateNew(person);

            bag.Dirty = Dirty;
            Item = items.First(e => e.Stack >= 10 && catalog.HomeOf(e.Id) == StorageGroup.Common && bag.Items.All(x => x.Item != e.Id)).Id;

            var rewards = new ProgressItems(World, Inventory, new ReadOnlyCollection<QuestRewardRow>([]));

            // A district whose reward asks for 20 points, and a group of it whose clears of the first episode add 16, with a
            // step at 7, 18, 33, 51 and 73 points.
            var sharePoints = SharePointCatalog.Of(
            [
                new SharePointRule(District, false, 0, new int[5], [20, 0, 0, 0, 0], [new ProgressItem(1, Item, 1), default, default, default, default]),
                new SharePointRule(Group, true, District, [1, 16, 16, 16, 16], [7, 18, 33, 51, 73], Enumerable.Repeat(new ProgressItem(1, Item, 2), 5).ToArray())
            ]);

            // A chain of two achievements of maze clears: one clear, then two.
            var achievements = AchievementCatalog.Of(
                [
                    new AchievementRule(FirstAchievement, 1, 41, (ushort)AchievementType.MazeCleared, 0, 1, 100, 0, 0, [], [new ProgressItem(1, Item, 3)]),
                    new AchievementRule(FirstAchievement + 1, 1, 42, (ushort)AchievementType.MazeCleared, 0, 2, 200, 0, 0, [], [])
                ],
                [new AchievementBegin(FirstAchievement, 1)]);

            Achievements = new AchievementManager(World, store, achievements, rewards, Inventory, new ReadOnlyCollection<MonsterRow>([]),
                NullLogger<AchievementManager>.Instance);
            SharePoints = new SharePointManager(World, store, sharePoints, rewards, NullLogger<SharePointManager>.Instance);

            Player = World.Create(
                Component.GetComponentType(typeof(ActorComponent)),
                Component.GetComponentType(typeof(InventoryComponent)),
                Component.GetComponentType(typeof(PersonInfoComponent)),
                Component.GetComponentType(typeof(CurrencyComponent)),
                Component.GetComponentType(typeof(LevelComponent)),
                Component.GetComponentType(typeof(DirtyComponent)),
                Component.GetComponentType(typeof(AchievementComponent)),
                Component.GetComponentType(typeof(SharePointComponent)),
                Component.GetComponentType(typeof(ServerSessionComponent)));

            World.Set(Player, new ActorComponent(7, ActorType.User));
            World.Set(Player, new InventoryComponent(bag));
            World.Set(Player, new PersonInfoComponent { Name = "Test", Hero = Hero.Haru });
            World.Set(Player, new CurrencyComponent());
            World.Set(Player, new LevelComponent(10, 0));
            World.Set(Player, new DirtyComponent(Dirty));
            World.Set(Player, new ServerSessionComponent(_connection));
        }

        public long Count() => Inventory.CountOf(Player, Item);

        public long Gold() => World.Get<CurrencyComponent>(Player).Gold;

        public SharePointBook SharePointBook => World.Get<SharePointComponent>(Player).Book!;

        /// <summary>The answers of 03/63 so far: the identifier, the step and the result of each.</summary>
        public async Task<(int Id, byte Step, byte Result)[]> SharePointAnswersAsync(int count)
        {
            await MetricAssertions.UntilAsync(() => Received(CharacterOpcode.GetRewardSharePoint).Length >= count, "The player gets the answers of the share points.");

            return Received(CharacterOpcode.GetRewardSharePoint)
                .Select(e => (BinaryPrimitives.ReadInt32LittleEndian(e.Body), e.Body[4], e.Body[5]))
                .ToArray();
        }

        public PlayerFrame[] Received(CharacterOpcode command) =>
            _connection.Frames.Select(PlayerFrame.Open).Where(e => e.Is(GroupOpcode.Character, (byte)command)).ToArray();

        public void Dispose()
        {
            World.Dispose();
        }
    }

    [TestMethod]
    public async Task AClearedMazeGrowsItsGroupAndTheStepItReachedIsRewardedOnce()
    {
        using var f = new Fixture();

        await f.SharePoints.EnterDistrictAsync(f.Player, 7, District, CancellationToken.None);

        f.SharePoints.OnMazeCleared(f.Player, Maze, Group, episode: 1);

        var group = f.SharePointBook.FindGroup(Group)!;

        Assert.AreEqual(16, group.Point, "the first episode adds its points");
        Assert.AreEqual(16, f.SharePointBook.FindDistrict(District)!.Point, "and so does the district of the group");
        Assert.IsTrue(f.Dirty.Sections.HasFlag(PersonSections.Progress), "the flush writes the points");

        await MetricAssertions.UntilAsync(() => f.Received(CharacterOpcode.UpdateSharePoint).Length == 2, "the district entered and the clear are told");

        f.SharePoints.TakeReward(f.Player, 1, Group);
        f.SharePoints.TakeReward(f.Player, 1, Group);

        CollectionAssert.AreEqual(new[] { (Group, (byte)1, (byte)SharePointResult.Given), (Group, (byte)1, (byte)SharePointResult.NotReached) },
            await f.SharePointAnswersAsync(2));
        Assert.AreEqual(2, f.Count(), "the first step, the one 16 points reach, is given once");
        Assert.AreEqual((byte)1, group.Step);
    }

    [TestMethod]
    public async Task TheRewardOfADistrictIsGivenOnce()
    {
        using var f = new Fixture();

        await f.SharePoints.EnterDistrictAsync(f.Player, 7, District, CancellationToken.None);

        f.SharePoints.TakeReward(f.Player, 0, District);

        f.SharePoints.OnMazeCleared(f.Player, Maze, Group, episode: 1);
        f.SharePoints.OnMazeCleared(f.Player, Maze, Group, episode: 1);

        Assert.AreEqual(20, f.SharePointBook.FindDistrict(District)!.Point, "the district stops at the point of its reward");

        f.SharePoints.TakeReward(f.Player, 0, District);
        f.SharePoints.TakeReward(f.Player, 0, District);

        CollectionAssert.AreEqual(
            new[]
            {
                (District, (byte)0, (byte)SharePointResult.NotReached),
                (District, (byte)1, (byte)SharePointResult.Given),
                (District, (byte)1, (byte)SharePointResult.NotReached)
            },
            await f.SharePointAnswersAsync(3));
        Assert.AreEqual(1, f.Count());
    }

    [TestMethod]
    public async Task AnAchievementGivesItsRewardOnceWhenItsCounterReachesItsCount()
    {
        using var f = new Fixture();

        await f.Achievements.EnterAsync(f.Player, 7, CancellationToken.None);

        Assert.IsFalse(f.Achievements.TakeReward(f.Player, FirstAchievement).Given, "nothing is counted yet");

        f.Achievements.OnMazeCleared(f.Player, Maze, seconds: 600, kind: 0);

        var counter = f.World.Get<AchievementComponent>(f.Player).Book!.Counters.Single();

        Assert.AreEqual(FirstAchievement + 1, counter.Achievement, "the counter moves on to the next achievement of the chain");
        Assert.AreEqual(1, counter.Count);
        await MetricAssertions.UntilAsync(() => f.Received(CharacterOpcode.AchieveUpdate).Length == 1, "the client is told");

        var first = f.Achievements.TakeReward(f.Player, FirstAchievement);
        var second = f.Achievements.TakeReward(f.Player, FirstAchievement);

        Assert.IsTrue(first.Given);
        Assert.AreEqual(AchievementReward.Silent, second, "a reward taken is not given again, and gets no answer");
        Assert.IsFalse(f.Achievements.TakeReward(f.Player, FirstAchievement + 1).Given, "the next one is not reached");
        Assert.AreEqual(100, f.Gold());
        Assert.AreEqual(3, f.Count());

        // Gold that would overflow the wallet is refused with the client's code, before anything is given.
        f.Achievements.OnMazeCleared(f.Player, Maze, seconds: 600, kind: 0);
        f.World.Get<CurrencyComponent>(f.Player)[ShopCurrency.Gold] = InventoryService.WalletLimit;

        Assert.AreEqual(AchievementManager.GoldOverflow, f.Achievements.TakeReward(f.Player, FirstAchievement + 1).Error);
        Assert.AreEqual(3, f.Count(), "nothing of the refused reward is given");
    }
}
