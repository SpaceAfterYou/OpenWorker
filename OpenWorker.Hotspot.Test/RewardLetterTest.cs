using Arch.Core;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Post;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What a reward does when the bags have no room: refused, given in part with the rest by letter, or sent by letter whole,
/// as its source says, and given once whatever the database answers. Set OPENWORKER_TEST_RES to the client tables.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class RewardLetterTest
{
    // Cloth, stacks of 50, and a gear that does not stack; both live in the common bag.
    private const int Material = 710105001;
    private const int Gear = 521;

    private const int Account = 31;

    [TestMethod]
    public async Task WhatFitsIsGivenAndTheRestGoesByLetter()
    {
        using var f = new Fixture();

        f.FillCommonBag(leaveFree: 1);

        Assert.IsTrue(await f.Granter.TryGrantAsync(f.Player, new Reward([new RewardItem(Gear, 1), new RewardItem(Gear, 1)], Gold: 70), RewardSource.Attendance, CancellationToken.None));

        Assert.AreEqual(1, f.Inventory.CountOf(f.Player, Gear), "the one that fits is in the bag");
        Assert.AreEqual(70, f.Inventory.Gold(f.Player), "the gold the purse holds goes into it");

        await MetricAssertions.UntilAsync(() => f.Post.Letters.Count == 1, "the rest goes by letter");

        var letter = f.Post.Letters[0];

        Assert.AreEqual(Account, letter.Account);
        Assert.AreEqual(1, letter.Items.Count);
        Assert.AreEqual(Gear, letter.Items[0].Item);
        Assert.AreEqual(0, letter.Money);
        Assert.AreEqual(GameLetters.AccountKind, letter.MainType);
        Assert.AreEqual(GameLetters.OwnText, letter.SubType, "the client has no row of attendance: the letter shows its own text");

        await MetricAssertions.UntilAsync(() => f.Received(PostOpcode.AccountRecv) == 1, "the person is told of the letter");
    }

    [TestMethod]
    public async Task TheRewardOfADailyMissionGoesByLetterEvenWithRoom()
    {
        using var f = new Fixture();

        Assert.IsTrue(await f.Granter.TryGrantAsync(f.Player, new Reward([new RewardItem(Material, 10)]), RewardSource.DailyMission, CancellationToken.None));

        Assert.AreEqual(0, f.Inventory.CountOf(f.Player, Material), "nothing goes into the bag");

        await MetricAssertions.UntilAsync(() => f.Post.Letters.Count == 1, "the reward goes by letter");

        Assert.AreEqual(Material, f.Post.Letters[0].Items[0].Item);
        Assert.AreEqual((short)10, f.Post.Letters[0].Items[0].Count);
        Assert.AreEqual(GameLetters.DailyMissionReward, f.Post.Letters[0].SubType, "the row of the reward of a daily mission");
    }

    [TestMethod]
    public async Task AQuestRewardThatDoesNotFitIsRefusedAndNotMailed()
    {
        using var f = new Fixture();

        f.FillCommonBag(leaveFree: 0);

        Assert.IsFalse(await f.Granter.TryGrantAsync(f.Player, new Reward([new RewardItem(Gear, 1)], Gold: 70), RewardSource.Quest, CancellationToken.None));

        Assert.AreEqual(0, f.Inventory.CountOf(f.Player, Gear));
        Assert.AreEqual(0, f.Inventory.Gold(f.Player), "a refused reward gives no gold either");
        Assert.AreEqual(0, f.Post.Calls, "the post is not asked");
    }

    [TestMethod]
    public async Task ALetterTheDatabaseRefusesIsKeptAndDeliveredOnceByTheNextTry()
    {
        using var f = new Fixture();

        f.FillCommonBag(leaveFree: 0);
        f.Post.Failures = 1;
        f.Post.DeliveryFailures = 1;

        Assert.IsTrue(await f.Granter.TryGrantAsync(f.Player, new Reward([new RewardItem(Gear, 1)]), RewardSource.PlayTime, CancellationToken.None),
            "the reward is settled at once: the letter is the post's to write");

        await MetricAssertions.UntilAsync(() => f.Post.Deliveries == 1, "the letter is kept pending and its first delivery refused");

        Assert.HasCount(1, f.Post.Pending);

        // The person leaves before the letter is written: the letter is for the account and still goes, untold.
        f.World.Destroy(f.Player);

        await MetricAssertions.UntilAsync(() =>
        {
            f.Time.Advance(TimeSpan.FromMilliseconds(250));

            return f.Post.Letters.Count == 1;
        }, "the pending letter is delivered by the next try");

        f.Time.Advance(TimeSpan.FromMinutes(5));
        await Task.Delay(50);

        Assert.AreEqual(2, f.Post.Deliveries, "it is delivered once and not tried again");
        Assert.HasCount(0, f.Post.Pending);
        Assert.AreEqual(Gear, f.Post.Letters[0].Items.Single().Item, "the item is made when the letter is delivered");
    }

    [TestMethod]
    public async Task ALetterKeptPendingIsDeliveredOnceAtTheNextStart()
    {
        using var f = new Fixture();

        f.FillCommonBag(leaveFree: 0);
        f.Post.Failures = 1;
        f.Post.DeliveryFailures = int.MaxValue;

        Assert.IsTrue(await f.Granter.TryGrantAsync(f.Player, new Reward([new RewardItem(Gear, 1)], Gold: 0), RewardSource.Attendance, CancellationToken.None));

        // Every try fails until the granter gives up: the letter stays pending. Time moves until each next try is made.
        await MetricAssertions.UntilAsync(() => f.Post.Deliveries == 1, "the letter is kept pending and its first delivery refused");

        for (var tries = 2; tries <= 5; tries++)
        {
            var reached = tries;

            await MetricAssertions.UntilAsync(() =>
            {
                f.Time.Advance(TimeSpan.FromSeconds(1));

                return f.Post.Deliveries == reached;
            }, "the delivery is tried again");
        }

        f.Time.Advance(TimeSpan.FromMinutes(5));
        await Task.Delay(50);

        Assert.AreEqual(5, f.Post.Deliveries, "the tries end");
        Assert.HasCount(0, f.Post.Letters);
        Assert.HasCount(1, f.Post.Pending, "the reward is kept, not lost");

        // The next start of a district: another granter over the same post.
        f.Post.DeliveryFailures = 0;

        var next = new InventoryRewardGranter(f.Inventory, f.World, f.Post, f.Time);

        Assert.AreEqual(1, await next.DeliverPendingAsync(DateTime.MaxValue, CancellationToken.None));
        Assert.AreEqual(0, await next.DeliverPendingAsync(DateTime.MaxValue, CancellationToken.None), "a delivered letter is not delivered again");
        Assert.HasCount(1, f.Post.Letters);
        Assert.AreEqual(Gear, f.Post.Letters[0].Items.Single().Item);
    }

    private sealed class Fixture : IDisposable
    {
        public World World { get; } = World.Create();
        public FakeTimeProvider Time { get; } = new();
        public RecordingPost Post { get; } = new();
        public InventoryService Inventory { get; }
        public InventoryRewardGranter Granter { get; }
        public Entity Player { get; }

        private readonly RecordingConnection _connection = new();

        public Fixture()
        {
            var catalog = new ItemCatalog(
                TestTables.Load<ItemRow>("tb_Item"),
                TestTables.Load<ItemClassifyRow>("tb_Item_Classify"),
                TestTables.Load<InvenSlotExtendRow>("tb_Inven_Slot_Extend"),
                TestTables.Load<BankSlotExtendRow>("tb_Bank_Slot_Extend"));

            var serials = new LocalItemSerials();

            Inventory = new InventoryService(World, catalog, serials);
            Granter = new InventoryRewardGranter(Inventory, World, Post, Time);

            Player = World.Create(
                Component.GetComponentType(typeof(ActorComponent)),
                Component.GetComponentType(typeof(InventoryComponent)),
                Component.GetComponentType(typeof(LevelComponent)),
                Component.GetComponentType(typeof(PersonInfoComponent)),
                Component.GetComponentType(typeof(CurrencyComponent)),
                Component.GetComponentType(typeof(ClaimsComponent)),
                Component.GetComponentType(typeof(ServerSessionComponent)));

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

            World.Set(Player, new ActorComponent(7, ActorType.User));
            World.Set(Player, new InventoryComponent(new InventoryFactory(catalog, TestTables.Load<CreateClothRow>("tb_Create_Cloth"), serials).CreateNew(person)));
            World.Set(Player, new LevelComponent(1, 0));
            World.Set(Player, new PersonInfoComponent { Name = "Test", Hero = Hero.Haru });
            World.Set(Player, new CurrencyComponent());
            World.Set(Player, new ClaimsComponent(Account, 0));
            World.Set(Player, new ServerSessionComponent(_connection));
        }

        /// <summary>
        /// Fills every free slot of the common bag but <paramref name="leaveFree"/> with whole stacks of cloth.
        /// </summary>
        public void FillCommonBag(int leaveFree)
        {
            var free = Inventory.InventoryOf(Player)!.Find(StorageGroup.Common)!.Slots.Count(e => e is null);

            Assert.IsTrue(Inventory.TryGive(Player, Material, 50 * (free - leaveFree)));
        }

        public int Received(PostOpcode command)
        {
            return _connection.Frames.Select(PlayerFrame.Open).Count(e => e.Is(GroupOpcode.Post, (byte)command));
        }

        public void Dispose()
        {
            World.Dispose();
        }
    }

    /// <summary>
    /// Keeps the letters of the game it is asked to write and the pending ones, and refuses the first writes or deliveries
    /// when told to.
    /// </summary>
    private sealed class RecordingPost : IPostStore
    {
        private long _serial;

        public List<PostAccountDraft> Letters { get; } = [];
        public Dictionary<long, PostAccountDraft> Pending { get; } = [];
        public int Calls { get; private set; }
        public int Failures { get; set; }
        public int Deliveries { get; private set; }
        public int DeliveryFailures { get; set; }

        public Task<PostPersistent> SendToAccountAsync(PostAccountDraft draft, CancellationToken cancellationToken = default)
        {
            Calls++;

            if (Failures > 0)
            {
                Failures--;

                return Task.FromException<PostPersistent>(new InvalidOperationException("The database did not take the letter."));
            }

            Letters.Add(draft);

            return Task.FromResult(Letter(++_serial, draft));
        }

        public Task<PostPersistent> SendPendingToAccountAsync(PostAccountDraft draft, CancellationToken cancellationToken = default)
        {
            Pending[++_serial] = draft;

            return Task.FromResult(Letter(_serial, draft));
        }

        public Task<List<PostPersistent>> ListPendingAsync(DateTime writtenBefore, int limit, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Pending.Select(e => Letter(e.Key, e.Value)).Take(limit).ToList());
        }

        public Task<PostPersistent?> DeliverPendingAsync(long serial, IReadOnlyList<PostItemPersistent> items, CancellationToken cancellationToken = default)
        {
            Deliveries++;

            if (DeliveryFailures > 0)
            {
                DeliveryFailures--;

                return Task.FromException<PostPersistent?>(new InvalidOperationException("The database did not deliver the letter."));
            }

            if (!Pending.Remove(serial, out var draft))
            {
                return Task.FromResult<PostPersistent?>(null);
            }

            var delivered = draft with { Items = items };

            Letters.Add(delivered);

            return Task.FromResult<PostPersistent?>(Letter(serial, delivered));
        }

        private static PostPersistent Letter(long serial, PostAccountDraft draft)
        {
            return new PostPersistent
            {
                Id = serial,
                Kind = PostKind.Account,
                ReceiverAccountId = draft.Account,
                SenderName = draft.SenderName,
                Title = draft.Title,
                Body = draft.Body,
                Money = draft.Money,
                Items = draft.Items.ToList(),
                MainType = draft.MainType,
                SubType = draft.SubType,
                ExpiresAt = DateTime.UtcNow + PostLimits.Lifetime
            };
        }

        public Task<(int Person, int Account)> CountUnreadAsync(int person, int account, CancellationToken cancellationToken = default)
        {
            return Task.FromResult((0, Letters.Count(e => e.Account == account)));
        }

        public Task<PostSendResult> SendAsync(PostDraft draft, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<PostPersistent>> ListReceivedAsync(int person, bool saved, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<PostPersistent>> ListSentAsync(int person, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<PostPersistent>> ListAccountAsync(int account, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PostPersistent?> ReadAsync(long serial, int person, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PostPersistent?> ReadAccountAsync(long serial, int account, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PostPersistent?> ClaimAsync(long serial, int person, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PostPersistent?> ClaimAccountAsync(long serial, int account, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ReleaseAsync(long serial, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PostPersistent?> SaveAsync(long serial, int person, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PostDeleteOutcome> DeleteReceivedAsync(long serial, int person, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PostDeleteOutcome> DeleteSentAsync(long serial, int person, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PostDeleteOutcome> DeleteAccountAsync(long serial, int account, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PostReturnResult> SendBackAsync(long serial, int person, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> SweepAsync(int limit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
