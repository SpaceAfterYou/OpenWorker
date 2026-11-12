using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Exchange;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Response.Person.Enums;
using OpenWorker.Hotspot.Modules.Exchange.Requests;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Persistence;
using OpenWorker.Server.Common.Services;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The exchange against a real Postgres: the store, and the handlers over it on one server and across two. Each run migrates
/// a database of its own and drops it after. Set OPENWORKER_TEST_POSTGRES to a connection string without a database.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ExchangeTest
{
    private const string Variable = "OPENWORKER_TEST_POSTGRES";

    // An item that does not stack, of a kind whose fee is 3000, the only rows of the tables of the test.
    private const int Sword = 900_000_001;
    private const int Kind = 110101;
    private const long Fee = 3000;

    private static IDbContextFactory<PersistenceContext> Factory { get; set; } = null!;

    private static ServiceProvider? Services { get; set; }

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        var connection = Environment.GetEnvironmentVariable(Variable);

        if (string.IsNullOrEmpty(connection))
        {
            Assert.Inconclusive("OPENWORKER_TEST_POSTGRES is not set.");
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
            Location = 10003, PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0,
            FatiguePointCommon = 200,
            Account = account,
            Gate = gate
        };

        database.AddRange(account, gate, person);

        await database.SaveChangesAsync();

        return (account.Id, person.Id);
    }

    private static ExchangeListingDraft Draft(int seller, short count = 1, byte upgrade = 0)
    {
        return new ExchangeListingDraft(seller, "seller", 1, new PostItemPersistent { Item = Sword, Count = count, Upgrade = upgrade }, 11, 1, 0, 1, 3);
    }

    private static (PostPersistent, PostPersistent) Letters(ExchangeListingPersistent listing, short count, int buyer)
    {
        return ExchangeListings.SaleLetters(listing, count, buyer, "buyer", 1, DateTime.UtcNow);
    }

    #region Tables of the test

    private static ItemCatalog Catalog()
    {
        var item = new byte[1024];

        BitConverter.TryWriteBytes(item, Sword);
        BitConverter.TryWriteBytes(item.AsSpan(4), Kind);

        // The kind: group 11, sub group 1, the common bag.
        var kind = new byte[1024];

        BitConverter.TryWriteBytes(kind, Kind);
        kind[4] = 11;
        kind[7] = 1;

        return new ItemCatalog(
            new ReadOnlyCollection<ItemRow>([new ItemRow(new BinaryReader(new MemoryStream(item)))]),
            new ReadOnlyCollection<ItemClassifyRow>([new ItemClassifyRow(new BinaryReader(new MemoryStream(kind)))]),
            new([]),
            new([]));
    }

    private static ReadOnlyCollection<ItemClassifyFeeRow> Fees()
    {
        var row = new byte[19];

        BitConverter.TryWriteBytes(row, Kind);
        BitConverter.TryWriteBytes(row.AsSpan(15), (ushort)Fee);

        return new ReadOnlyCollection<ItemClassifyFeeRow>([new ItemClassifyFeeRow(new BinaryReader(new MemoryStream(row)))]);
    }

    private static void Equip(TestServer server, TestPlayer player, int account, ItemCatalog catalog, long gold)
    {
        var factory = new InventoryFactory(catalog, new([]), new LocalItemSerials());

        var person = new PersonPersistent
        {
            Id = player.Person, Name = player.Name, Hero = Hero.Lily,
            DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
            EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0,
            Location = 0, PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0,
            Account = null!, Gate = null!
        };

        server.World.Add(player.Entity,
            new InventoryComponent(factory.CreateNew(person)),
            new CurrencyComponent { Gold = gold },
            new ClaimsComponent(account, 0),
            new TradePasswordComponent { Password = "hash", State = PasswordProtectionState.Authenticated });
    }

    private static ExchangeService Service(TestServer server, ItemCatalog catalog, IExchangeStore? store = null)
    {
        var inventory = new InventoryService(server.World, catalog, new LocalItemSerials());

        return new ExchangeService(server.World, store ?? new ExchangeStore(Factory), new PostStore(Factory), inventory, server.Network,
            NoPersonFlush.Instance, Fees(), NullLogger<ExchangeService>.Instance);
    }

    /// <summary>
    /// The store of the exchange with a purchase that fails the way a lost connection does.
    /// </summary>
    public class PurchaseFails : System.Reflection.DispatchProxy
    {
        public IExchangeStore Inner { get; set; } = null!;

        protected override object? Invoke(System.Reflection.MethodInfo? method, object?[]? args)
        {
            if (method!.Name == nameof(IExchangeStore.BuyAsync))
            {
                return Task.FromException<ExchangeBuyResult>(new TimeoutException("the database does not answer"));
            }

            return method.Invoke(Inner, args);
        }
    }

    /// <summary>
    /// The database fails in the middle of a purchase: nothing was sold, so the buyer keeps its gold.
    /// </summary>
    [TestMethod]
    public async Task APurchaseTheDatabaseFailsCostsTheBuyerNothing()
    {
        var (_, seller) = await CreatePersonAsync("failseller");
        var (buyerAccount, buyer) = await CreatePersonAsync("failbuyer");

        var real = new ExchangeStore(Factory);
        var waiting = await real.AddWaitingAsync(Draft(seller, upgrade: 0), 10);
        var listing = (await real.PutOnSaleAsync(waiting.Listing!.Serial, seller, 2_000, 9, DateTime.UtcNow.AddHours(1), 10)).Listing!;

        var failing = System.Reflection.DispatchProxy.Create<IExchangeStore, PurchaseFails>();

        ((PurchaseFails)(object)failing).Inner = real;

        var catalog = Catalog();

        using var server = new TestServer(new MemoryCommunity());
        using var player = server.Join(buyer, "failbuyer");

        Equip(server, player, buyerAccount, catalog, 5_000);

        var service = Service(server, catalog, failing);

        try
        {
            Handle(server, player, c => service.OnHandleAsync(c, new ExchangeItemBuyRequest { Exchange = listing.Id, Item = Sword, Count = 1 }));
        }
        catch (Exception e) when (e is TimeoutException || e.InnerException is TimeoutException || e is AggregateException)
        {
            // The failure of the store goes on to the error budget of the session; the gold is what is asked here.
        }

        Assert.AreEqual(5_000, server.World.Get<CurrencyComponent>(player.Entity).Gold);
        Assert.AreEqual((short)1, (await real.FindAsync(listing.Id))!.Count, "the listing is untouched");

        // The tests of this class share one exchange: the listing leaves it again.
        await real.RecallAsync(listing.Serial, seller, e => ExchangeListings.ReturnLetter(e, DateTime.UtcNow));
    }

    private static void Handle(TestServer server, TestPlayer player, Func<ServiceHandleContext, ValueTask> handle) =>
        server.Run(async () => await handle(new ServiceHandleContext(player.Entity, CancellationToken.None)));

    private static int Result(ReceivedFrame frame) => BitConverter.ToInt32(frame.Content);

    #endregion

    /// <summary>
    /// The last item of a listing, bought by two buyers at the same moment: one statement decides, so one gets it and the
    /// other is refused; one sale, one pair of letters, and the listing is gone.
    /// </summary>
    [TestMethod]
    public async Task TwoBuyersOfTheLastItemNeverBothGetIt()
    {
        var store = new ExchangeStore(Factory);

        var (_, seller) = await CreatePersonAsync("lastseller");
        var (_, first) = await CreatePersonAsync("lastbuyer1");
        var (_, second) = await CreatePersonAsync("lastbuyer2");

        var waiting = await store.AddWaitingAsync(Draft(seller), 10);
        var listing = (await store.PutOnSaleAsync(waiting.Listing!.Serial, seller, 500, 9, DateTime.UtcNow.AddHours(1), 10)).Listing!;

        var buys = await Task.WhenAll(
            Task.Run(() => store.BuyAsync(listing.Id, first, Sword, 1, 500, DateTime.UtcNow, (l, c) => Letters(l, c, first))),
            Task.Run(() => store.BuyAsync(listing.Id, second, Sword, 1, 500, DateTime.UtcNow, (l, c) => Letters(l, c, second))));

        Assert.AreEqual(1, buys.Count(e => e.Outcome == ExchangeOutcome.Done));
        Assert.IsTrue(buys.Any(e => e.Outcome is ExchangeOutcome.Missing or ExchangeOutcome.NotEnough), "the second buyer is refused");

        await using var database = await Factory.CreateDbContextAsync();

        Assert.IsFalse(await database.ExchangeListings.AnyAsync(e => e.Id == listing.Id), "a listing sold out is gone");
        Assert.AreEqual(1, await database.ExchangeSales.CountAsync(e => e.Item == Sword && e.PriceOne == 500));
        Assert.AreEqual(1, await database.Posts.CountAsync(e => e.ReceiverId == seller), "one letter with the gold");
        Assert.AreEqual(1, await database.Posts.CountAsync(e => e.ReceiverId == first || e.ReceiverId == second), "one letter with the item");
    }

    /// <summary>
    /// The limit of the waiting listings of a seller is counted under the row of the seller: twelve at once with a limit of
    /// ten store ten.
    /// </summary>
    [TestMethod]
    public async Task TheLimitOfWaitingListingsHoldsForRequestsAtOnce()
    {
        var store = new ExchangeStore(Factory);

        var (_, seller) = await CreatePersonAsync("limitseller");

        var results = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => Task.Run(() => store.AddWaitingAsync(Draft(seller), 10))));

        Assert.AreEqual(10, results.Count(e => e.Outcome == ExchangeOutcome.Done));
        Assert.AreEqual(2, results.Count(e => e.Outcome == ExchangeOutcome.Full));
        Assert.HasCount(10, await store.ListMineAsync(seller));
    }

    /// <summary>
    /// The entry lists of a new player are empty; the list of the items it keeps an eye on takes ten and refuses the eleventh
    /// with the original's code.
    /// </summary>
    [TestMethod]
    public async Task TheEntryListsAreEmptyAtFirstAndTheInterestListHoldsTen()
    {
        var (account, person) = await CreatePersonAsync("watcher");

        var catalog = Catalog();

        using var server = new TestServer(new MemoryCommunity());
        using var player = server.Join(person, "watcher");

        Equip(server, player, account, catalog, 0);

        var service = Service(server, catalog);

        Handle(server, player, c => service.OnHandleAsync(c, new ExchangeInterestListRequest { Person = person }));
        Handle(server, player, c => service.OnHandleAsync(c, new ExchangeMyListRequest { Person = person }));

        var entry = player.Read();

        Assert.AreEqual((byte)ExchangeOpcode.InterestList, entry[0].Command);
        CollectionAssert.AreEqual(BitConverter.GetBytes(person).Concat(new byte[2]).ToArray(), entry[0].Content, "the person, no items");
        Assert.AreEqual((byte)ExchangeOpcode.MyList, entry[1].Command);
        CollectionAssert.AreEqual(new byte[2], entry[1].Content, "no listings");

        // The test has one item; the list counts rows, so the other nine are written as the store would.
        var store = new ExchangeStore(Factory);

        for (var item = 1; item < ExchangeRules.InterestLimit; item++)
        {
            Assert.AreEqual(ExchangeInterestOutcome.Added, await store.AddInterestAsync(person, item, ExchangeRules.InterestLimit));
        }

        Handle(server, player, c => service.OnHandleAsync(c, new ExchangeInterestItemRequest { Person = person, Item = Sword, Add = 1 }));

        Assert.AreEqual(0, BitConverter.ToInt32(player.Read().Single().Content, 8), "the tenth is taken");
        Assert.AreEqual(ExchangeInterestOutcome.Full, await store.AddInterestAsync(person, 99, ExchangeRules.InterestLimit));
        Assert.HasCount(ExchangeRules.InterestLimit, await store.ListInterestsAsync(person));
    }

    /// <summary>
    /// An item given to the exchange leaves the bag and waits; put on sale it costs the fee of its kind and gets an end; taken
    /// back it comes home by letter with what was done to it, and the listing leaves the list.
    /// </summary>
    [TestMethod]
    public async Task AnItemGoesToTheExchangeOnSaleAndComesBackByLetter()
    {
        var (account, person) = await CreatePersonAsync("lister");

        var catalog = Catalog();

        using var server = new TestServer(new MemoryCommunity());
        using var player = server.Join(person, "lister");

        Equip(server, player, account, catalog, 10_000);

        var bag = server.World.Get<InventoryComponent>(player.Entity).Value!;

        bag.Place(StorageGroup.Common, 3, new ItemInstance { Serial = 77, Item = Sword, Count = 1, Upgrade = 7 });

        var service = Service(server, catalog);

        Handle(server, player, c => service.OnHandleAsync(c, new ExchangeWaitRegisterRequest
        {
            Item = Sword, Count = 1, Storage = (byte)StorageGroup.Common, Slot = 3
        }));

        Assert.IsNull(bag.At(StorageGroup.Common, 3), "the sword left the bag");

        var given = player.Read().Where(e => e.Group == (byte)GroupOpcode.Exchange).ToList();

        Assert.AreEqual(0, Result(given.Single(e => e.Command == (byte)ExchangeOpcode.WaitRegister)));

        var mine = await new ExchangeStore(Factory).ListMineAsync(person);
        var serial = mine.Single().Serial;

        Assert.AreEqual(ExchangeListingState.Waiting, mine.Single().State);

        Handle(server, player, c => service.OnHandleAsync(c, new ExchangeSellRegisterRequest { Serial = serial, Price = 1_000 }));

        var sold = player.Read().Where(e => e.Group == (byte)GroupOpcode.Exchange).ToList();

        Assert.AreEqual(0, Result(sold.Single(e => e.Command == (byte)ExchangeOpcode.SellRegister)));
        Assert.AreEqual(10_000 - Fee, server.World.Get<CurrencyComponent>(player.Entity).Gold, "the fee of the kind is paid");

        var onSale = (await new ExchangeStore(Factory).ListMineAsync(person)).Single();

        Assert.AreEqual(ExchangeListingState.OnSale, onSale.State);
        Assert.AreEqual(ExchangeRules.SaleRate, onSale.SaleRate);
        Assert.IsTrue(onSale.ExpiresAt > DateTime.UtcNow.AddHours(23), "a sale runs a day");

        Handle(server, player, c => service.OnHandleAsync(c, new ExchangeItemRecallRequest { Serial = serial }));

        var back = player.Read();
        var recall = back.Single(e => e.Group == (byte)GroupOpcode.Exchange);

        Assert.AreEqual(0, Result(recall));
        Assert.AreEqual(serial, BitConverter.ToInt64(recall.Content, 4));
        Assert.IsTrue(back.Any(e => e.Group == (byte)GroupOpcode.Post && e.Command == (byte)PostOpcode.Recv), "the letter is announced");

        var letter = (await new PostStore(Factory).ListReceivedAsync(person, saved: false)).Single();

        Assert.AreEqual(ExchangeListings.TakenBack, letter.SubType);
        Assert.AreEqual(7, letter.Items.Single().Upgrade, "the item comes back as it went");
        Assert.IsEmpty(await new ExchangeStore(Factory).ListMineAsync(person));
    }

    /// <summary>
    /// A buyer on one server buys the listing of a seller on another: the buyer pays and gets the item by letter, the seller
    /// is told on its own server of the listing sold out and of the letter with the gold less the commission. The listing is
    /// gone for the next buyer, whose gold comes back.
    /// </summary>
    [TestMethod]
    public async Task ABuyerOnOneServerPaysAndTheSellerOnAnotherIsTold()
    {
        var (sellerAccount, seller) = await CreatePersonAsync("farseller");
        var (buyerAccount, buyer) = await CreatePersonAsync("farbuyer");

        var store = new ExchangeStore(Factory);
        var waiting = await store.AddWaitingAsync(Draft(seller, upgrade: 5), 10);
        var listing = (await store.PutOnSaleAsync(waiting.Listing!.Serial, seller, 2_000, 9, DateTime.UtcNow.AddHours(1), 10)).Listing!;

        var catalog = Catalog();
        var community = new MemoryCommunity();

        using var near = new TestServer(community);
        using var far = new TestServer(community);
        using var sellerPlayer = near.Join(seller, "farseller");
        using var buyerPlayer = far.Join(buyer, "farbuyer");

        Equip(near, sellerPlayer, sellerAccount, catalog, 0);
        Equip(far, buyerPlayer, buyerAccount, catalog, 5_000);

        var service = Service(far, catalog);

        Handle(far, buyerPlayer, c => service.OnHandleAsync(c, new ExchangeItemBuyRequest { Exchange = listing.Id, Item = Sword, Count = 1 }));

        var bought = buyerPlayer.Read();

        Assert.AreEqual(0, Result(bought.Single(e => e.Group == (byte)GroupOpcode.Exchange)));
        Assert.IsTrue(bought.Any(e => e.Group == (byte)GroupOpcode.Post && e.Command == (byte)PostOpcode.Recv));
        Assert.AreEqual(3_000, far.World.Get<CurrencyComponent>(buyerPlayer.Entity).Gold);

        var told = sellerPlayer.Read();
        var mine = told.Single(e => e.Group == (byte)GroupOpcode.Exchange);

        Assert.AreEqual((byte)ExchangeOpcode.MyList, mine.Command);
        Assert.AreEqual(1, BitConverter.ToInt16(mine.Content), "one listing changed");
        Assert.IsTrue(told.Any(e => e.Group == (byte)GroupOpcode.Post && e.Command == (byte)PostOpcode.Recv), "the seller is told of the gold");

        var gold = (await new PostStore(Factory).ListReceivedAsync(seller, saved: false)).Single();

        Assert.AreEqual(ExchangeRules.SellerGets(2_000, 9), gold.Money);
        Assert.AreEqual(1_820, gold.Money, "the exchange keeps nine in a hundred");
        Assert.AreEqual(5, (await new PostStore(Factory).ListReceivedAsync(buyer, saved: false)).Single().Items.Single().Upgrade);

        // Sold out: the next buyer finds nothing, and keeps its gold.
        Handle(far, buyerPlayer, c => service.OnHandleAsync(c, new ExchangeItemBuyRequest { Exchange = listing.Id, Item = Sword, Count = 1 }));

        Assert.AreEqual(ExchangeCodes.Gone, Result(buyerPlayer.Read().Single()));
        Assert.AreEqual(3_000, far.World.Get<CurrencyComponent>(buyerPlayer.Entity).Gold);
    }

    /// <summary>
    /// A sale that has ended is not found by a search and not bought; taken back, it comes with the letter of an ended sale.
    /// </summary>
    [TestMethod]
    public async Task AnEndedSaleIsNotFoundNorBoughtAndComesBackAsEnded()
    {
        var store = new ExchangeStore(Factory);

        var (_, seller) = await CreatePersonAsync("endseller");
        var (_, buyer) = await CreatePersonAsync("endbuyer");

        var waiting = await store.AddWaitingAsync(Draft(seller), 10);
        var listing = (await store.PutOnSaleAsync(waiting.Listing!.Serial, seller, 700, 9, DateTime.UtcNow.AddSeconds(-1), 10)).Listing!;

        var search = ExchangeRules.Filter(Sword, -1, -1, -1, -1, -1, -1, -1, -1, -1);
        var (total, _) = await store.SearchAsync(search, 1, ExchangeRules.PageSize, DateTime.UtcNow);

        Assert.AreEqual(0, total);

        var buy = await store.BuyAsync(listing.Id, buyer, Sword, 1, 700, DateTime.UtcNow, (l, c) => Letters(l, c, buyer));

        Assert.AreEqual(ExchangeOutcome.Expired, buy.Outcome);

        var recall = await store.RecallAsync(listing.Serial, seller, e => ExchangeListings.ReturnLetter(e, DateTime.UtcNow));

        Assert.AreEqual(ExchangeOutcome.Done, recall.Outcome);
        Assert.AreEqual(ExchangeListings.Ended, recall.Letter!.SubType);
        Assert.AreEqual(1, recall.Letter.Items.Single().Count);
    }
}
