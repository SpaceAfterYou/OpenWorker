using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Modules.CashShop;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Modules.Shop.Types;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Persistence;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The cash shop: where a price comes from, the periods limited products are counted in, the draw of a gacha, and the
/// purchase that takes the balance, counts the limits and writes the letters in one transaction (against a real Postgres,
/// OPENWORKER_TEST_POSTGRES).
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class CashShopTest
{
    private const string Variable = "OPENWORKER_TEST_POSTGRES";

    private const int Ticket = 837_000_044;
    private const int Pack = 837_000_045;
    private const int CashKind = 1;

    private static ItemCatalog Items()
    {
        return new ItemCatalog(
            new ReadOnlyCollection<ItemRow>(
            [
                TestRows.Build<ItemRow>(("Id", Ticket), ("Classify", CashKind), ("Stack", (short)99)),
                TestRows.Build<ItemRow>(("Id", Pack), ("Classify", CashKind), ("Stack", (short)99))
            ]),
            new ReadOnlyCollection<ItemClassifyRow>([TestRows.Build<ItemClassifyRow>(("Id", CashKind), ("Group", (byte)18), ("StorageKind", (byte)13))]),
            new([]),
            new([]));
    }

    private static CashBillingRow ServerPrices(ushort id, params (int Item, int Count, int Price)[] choices)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(id);
        writer.Write((short)0);

        foreach (var column in new Func<(int Item, int Count, int Price), int>[] { e => e.Item, e => e.Count, e => e.Price, e => e.Price })
        {
            for (var index = 0; index < CashBillingRow.Choices; index++)
            {
                writer.Write(index < choices.Length ? column(choices[index]) : 0);
            }
        }

        for (var index = 1; index < CashBillingRow.Choices; index++)
        {
            writer.Write((short)0);
        }

        writer.Flush();
        stream.Position = 0;

        return new CashBillingRow(new BinaryReader(stream));
    }

    /// <summary>
    /// The client's table says what a product gives, the server's what it costs: a choice is sold while the two tables agree on
    /// its item and count and the price is not 0, and the choices end at the first that is not, as the client counts them.
    /// </summary>
    [TestMethod]
    public void AChoiceIsSoldWhileBothTablesAgreeOnItAndItHasAPrice()
    {
        var catalog = new CashShopCatalog(
            new ReadOnlyCollection<CashShopRow>([TestRows.Build<CashShopRow>(("Id", 9), ("Item", Ticket), ("SellActive", (byte)1), ("Billing", (short)9))]),
            new ReadOnlyCollection<CashBillingInfoRow>([TestRows.Build<CashBillingInfoRow>(
                ("Id", (short)9), ("Item1", Ticket), ("Count1", 1), ("Item2", Pack), ("Count2", 5), ("Item3", Ticket), ("Count3", 10))]),
            new ReadOnlyCollection<CashBillingRow>([ServerPrices(9, (Ticket, 1, 300), (Pack, 6, 1000), (Ticket, 10, 2500))]),
            Items());

        Assert.IsTrue(catalog.TryGet(9, out var product));
        Assert.HasCount(1, product.Choices, "the second choice gives another count in each table, so it and all after it are not sold");
        Assert.AreEqual(300, product.Choices[0].Price);
        Assert.AreEqual(300, catalog.Listing[0].CashInfoList[0].Price);
    }

    /// <summary>
    /// A day of the counts starts at 09:00, a week on Tuesday at 09:00, a month on its first day at 09:00; a count for good has
    /// one period.
    /// </summary>
    [TestMethod]
    public void LimitedProductsAreCountedInPeriodsThatStartAtNine()
    {
        var offset = TimeSpan.FromHours(9);
        var mondayMorning = new DateTimeOffset(2026, 11, 2, 8, 0, 0, offset);

        Assert.AreEqual(new DateTimeOffset(2026, 11, 1, 9, 0, 0, offset).UtcDateTime, CashLimits.PeriodOf(CashLimitKind.AccountDay, mondayMorning));
        Assert.AreEqual(new DateTimeOffset(2026, 10, 27, 9, 0, 0, offset).UtcDateTime, CashLimits.PeriodOf(CashLimitKind.CharacterWeek, mondayMorning));
        Assert.AreEqual(new DateTimeOffset(2026, 11, 3, 9, 0, 0, offset).UtcDateTime,
            CashLimits.PeriodOf(CashLimitKind.CharacterWeek, new DateTimeOffset(2026, 11, 3, 9, 0, 0, offset)));
        Assert.AreEqual(new DateTimeOffset(2026, 10, 1, 9, 0, 0, offset).UtcDateTime,
            CashLimits.PeriodOf(CashLimitKind.AccountMonth, new DateTimeOffset(2026, 11, 1, 8, 59, 0, offset)));
        Assert.AreEqual(CashLimits.Always, CashLimits.PeriodOf(CashLimitKind.Character, mondayMorning));
    }

    /// <summary>
    /// A roll falls on the first chance it does not pass, the chances taken in order, for the group and then for the item.
    /// </summary>
    [TestMethod]
    public void AGachaGivesTheItemTheRollFallsOn()
    {
        static T Read<T>(Action<BinaryWriter> write) where T : struct
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            write(writer);
            writer.Flush();
            stream.Position = 0;

            return (T)Activator.CreateInstance(typeof(T), new BinaryReader(stream))!;
        }

        var chances = Read<GachaGroupRow>(writer =>
        {
            writer.Write((ushort)1);
            writer.Write((short)0);
            writer.Write((ushort)7);
            writer.Write((byte)1);

            for (var index = 0; index < GachaGroupRow.Groups; index++)
            {
                writer.Write((ushort)(index < 2 ? 5000 : 0));
            }
        });

        var gacha = Read<GachaRow>(writer =>
        {
            writer.Write((ushort)11);
            writer.Write(Ticket);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write(new byte[8]);
            writer.Write((ushort)7);

            for (var index = 0; index < GachaGroupRow.Groups; index++)
            {
                writer.Write((ushort)(index switch { 0 => 100, 1 => 200, _ => 0 }));
            }
        });

        GachaDrawRow Draw(ushort id, int item, byte type) => Read<GachaDrawRow>(writer =>
        {
            writer.Write(id);

            for (var index = 0; index < GachaDrawRow.Items; index++)
            {
                writer.Write(index < 2 ? item + index : 0);
            }

            for (var index = 0; index < GachaDrawRow.Items; index++)
            {
                writer.Write((ushort)(index < 2 ? 5000 : 0));
            }

            for (var index = 0; index < GachaDrawRow.Items; index++)
            {
                writer.Write(type);
            }
        });

        var rolls = new Queue<int>([5001, 5000, 1, 10_000]);

        var draws = new CashShopGacha(
            new ReadOnlyCollection<GachaGroupRow>([chances]),
            new ReadOnlyCollection<GachaRow>([gacha]),
            new ReadOnlyCollection<GachaDrawRow>([Draw(100, Ticket, 1), Draw(200, Ticket, 2)]),
            Items(),
            (_, _) => rolls.Dequeue());

        var results = draws.Draw(gacha, 2);

        Assert.IsNotNull(results);
        Assert.AreEqual(new GachaResultValue { Item = Ticket, Type = 2 }, results[0], "5001 passes the first group; 5000 is the first item");
        Assert.AreEqual(new GachaResultValue { Item = Pack, Type = 1 }, results[1], "1 is the first group; 10000 the second item");
    }

    /// <summary>
    /// A purchase takes the price, adds to the count of a limited product and writes the letter at once; one that the balance
    /// or the limit refuses changes none of them.
    /// </summary>
    [TestMethod]
    public async Task APurchaseTakesTheBalanceCountsAndWritesTheLetterOrNothing()
    {
        var connection = Environment.GetEnvironmentVariable(Variable);

        if (string.IsNullOrEmpty(connection))
        {
            Assert.Inconclusive($"{Variable} is not set.");
        }

        await using var services = new ServiceCollection()
            .AddPooledDbContextFactory<PersistenceContext>(builder => builder
                .UseNpgsql(connection + ";Database=openworker_test_" + Guid.NewGuid().ToString("N"))
                .UsePersistenceDefaults())
            .BuildServiceProvider();

        var factory = services.GetRequiredService<IDbContextFactory<PersistenceContext>>();

        await using (var context = await factory.CreateDbContextAsync())
        {
            await context.Database.MigrateAsync();
        }

        try
        {
            int person, account;

            await using (var database = await factory.CreateDbContextAsync())
            {
                var row = new PersonPersistent
                {
                    Name = "buyer",
                    Hero = Hero.Haru,
                    Level = 1,
                    DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
                    EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0,
                    Location = 10003, PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0,
                    Account = new AccountPersistent { Username = "buyer", PasswordHash = [], SaltHash = [] },
                    Gate = new GatePersistent { Name = "buyer" }
                };

                database.Add(row);
                await database.SaveChangesAsync();

                (person, account) = (row.Id, row.Account.Id);
            }

            var store = new CashShopStore(factory);
            var buyer = new CashBuyer(person, account, "buyer", (byte)Hero.Haru);
            var order = new CashOrder(300, [new CashLimitDraft(9, false, CashLimits.Always, 1, 1)],
                [new PostItemPersistent { Item = Ticket, Count = 1, Endurance = 100 }]);

            Assert.AreEqual(1000, await store.GiveAsync(account, 1000, InventoryService.CashLimit));

            var bought = await store.BuyAsync(new CashPurchaseDraft(account, person, order.Price, order.Limits,
                CashShopOrders.Letters(order, buyer, null, DateTime.UtcNow)));

            Assert.AreEqual(new CashPurchaseResult(CashPurchaseOutcome.Done, 700), bought);

            var again = await store.BuyAsync(new CashPurchaseDraft(account, person, order.Price, order.Limits,
                CashShopOrders.Letters(order, buyer, null, DateTime.UtcNow)));

            Assert.AreEqual(new CashPurchaseResult(CashPurchaseOutcome.Limit, 700), again, "the limit of one is reached: the price stays");

            var tooDear = await store.BuyAsync(new CashPurchaseDraft(account, person, 701, [],
                CashShopOrders.Letters(order, buyer, null, DateTime.UtcNow)));

            Assert.AreEqual(new CashPurchaseResult(CashPurchaseOutcome.NotEnough, 700), tooDear);
            Assert.IsNull(await store.GiveAsync(account, -701, InventoryService.CashLimit), "the balance never goes below zero");

            await using var check = await factory.CreateDbContextAsync();

            Assert.AreEqual(1, await check.Posts.CountAsync(e => e.ReceiverId == person), "one letter for the one purchase");
            Assert.AreEqual(1, await check.CashShopPurchases.Where(e => e.AccountId == account).SumAsync(e => e.Count));
        }
        finally
        {
            await using var context = await factory.CreateDbContextAsync();

            await context.Database.EnsureDeletedAsync();
        }
    }

    /// <summary>
    /// The window of the client cannot open before it has the products, so a client from the character selection is given
    /// the banners, the products in parts with the last one marked, the balance and the lists at the entry, while the cash shop
    /// is open; a closed shop gives nothing, and a login given them is not given them again at its next entry.
    /// </summary>
    [TestMethod]
    public async Task AClientFromTheCharacterSelectionIsGivenTheShopAtTheEntryOncePerLogin()
    {
        using var world = World.Create();
        var (window, contents) = Window(world, new MemoryCashShopLogins());
        var connection = new RecordingConnection();
        var player = Player(world, connection, 3);

        await window.EnterAsync(player, true, CancellationToken.None);

        contents[ServerContentIndex.CashShop] = true;

        await window.EnterAsync(player, true, CancellationToken.None);
        await window.EnterAsync(player, false, CancellationToken.None);

        var sent = Drain(world, player, connection);

        CollectionAssert.AreEqual(ShopBurst, sent.Select(e => e.Opcode).ToArray(), "only the entry from the character selection with the shop open");
        Assert.AreEqual((0, CashShopCatalog.PartSize), (sent[1].Body[0], BitConverter.ToInt32(sent[1].Body, 1)));
        Assert.AreEqual((1, 1), (sent[2].Body[0], BitConverter.ToInt32(sent[2].Body, 1)));
        Assert.AreEqual(1234, BitConverter.ToInt32(sent[3].Body));
    }

    /// <summary>
    /// A login that came in while the shop was closed has no products: once the shop has opened, its next entry into a
    /// district gives them, though it does not come from the character selection.
    /// </summary>
    [TestMethod]
    public async Task ALoginThatCameWhileTheShopWasClosedIsGivenItAtItsNextEntry()
    {
        using var world = World.Create();
        var (window, contents) = Window(world, new MemoryCashShopLogins());
        var connection = new RecordingConnection();
        var player = Player(world, connection, 3);

        await window.EnterAsync(player, true, CancellationToken.None);

        contents[ServerContentIndex.CashShop] = true;

        await window.EnterAsync(player, false, CancellationToken.None);

        CollectionAssert.AreEqual(ShopBurst, Drain(world, player, connection).Select(e => e.Opcode).ToArray());
    }

    /// <summary>
    /// When the shop opens, the notice that enables the entry of the menu reaches a player only after the products: a login
    /// without them is given them first, one that has them gets the notice alone.
    /// </summary>
    [TestMethod]
    public async Task TheShopIsQueuedBeforeTheNoticeThatItHasOpened()
    {
        using var world = World.Create();
        var logins = new MemoryCashShopLogins();
        var (window, contents) = Window(world, logins);
        var lacking = new RecordingConnection();
        var given = new RecordingConnection();
        var players = new[] { Player(world, lacking, 3), Player(world, given, 4) };

        await logins.GiveAsync(world.Get<ClaimsComponent>(players[1]).Key);

        contents[ServerContentIndex.CashShop] = true;

        await window.OpenAsync(players, new SystemServerOptionUpdateResponse { Contents = contents }, CancellationToken.None);

        var notice = MessageOpcode.From(GroupOpcode.System, SystemOpcode.ServerOptionUpdate);

        CollectionAssert.AreEqual(ShopBurst.Append(notice).ToArray(), Wait(lacking, 6).Select(e => e.Opcode).ToArray());
        CollectionAssert.AreEqual(new[] { notice }, Wait(given, 1).Select(e => e.Opcode).ToArray());
    }

    private static readonly MessageOpcode[] ShopBurst =
    [
        MessageOpcode.From(GroupOpcode.Shop, ShopOpcode.BannerLoad),
        MessageOpcode.From(GroupOpcode.Shop, ShopOpcode.CashLoad),
        MessageOpcode.From(GroupOpcode.Shop, ShopOpcode.CashLoad),
        MessageOpcode.From(GroupOpcode.Item, ItemOpcode.UpdateCash),
        MessageOpcode.From(GroupOpcode.Shop, ShopOpcode.CashSetLoad)
    ];

    /// <summary>
    /// A window over two parts of products, the second of one, with a balance of 1234, and the server's contents it reads.
    /// </summary>
    private static (CashShopWindow Window, ServerContent Contents) Window(World world, ICashShopLogins logins)
    {
        var rows = Enumerable.Range(1, CashShopCatalog.PartSize + 1)
            .Select(e => TestRows.Build<CashShopRow>(("Id", e), ("Item", Ticket), ("SellActive", (byte)1), ("Billing", (short)9)))
            .ToList();

        var catalog = new CashShopCatalog(
            new ReadOnlyCollection<CashShopRow>(rows),
            new ReadOnlyCollection<CashBillingInfoRow>([TestRows.Build<CashBillingInfoRow>(("Id", (short)9), ("Item1", Ticket), ("Count1", 1))]),
            new ReadOnlyCollection<CashBillingRow>([ServerPrices(9, (Ticket, 1, 300))]),
            Items());

        var contents = new ServerContent();

        return (new CashShopWindow(world, catalog, new Balances(1234), logins, contents, NullLogger<CashShopWindow>.Instance), contents);
    }

    /// <summary>
    /// A window with nothing to sell, for a test that only needs one to exist.
    /// </summary>
    internal static CashShopWindow EmptyWindow(World world, ServerContent contents)
    {
        return new CashShopWindow(
            world,
            new CashShopCatalog(new ReadOnlyCollection<CashShopRow>([]), new ReadOnlyCollection<CashBillingInfoRow>([]), new ReadOnlyCollection<CashBillingRow>([]), Items()),
            new Balances(0),
            new MemoryCashShopLogins(),
            contents,
            NullLogger<CashShopWindow>.Instance);
    }

    private static Entity Player(World world, RecordingConnection connection, int account)
    {
        return world.Create(new ActorComponent(account + 4, ActorType.User), new ClaimsComponent(account, 0), new ServerSessionComponent(connection));
    }

    /// <summary>
    /// The frames sent to the player so far: a marker is queued after them, and they are all written once it is.
    /// </summary>
    private static PlayerFrame[] Drain(World world, Entity player, RecordingConnection connection)
    {
        var marker = MessageOpcode.From(GroupOpcode.System, SystemOpcode.ServerOptionUpdate);

        world.Get<ServerSessionComponent>(player).Send(new SystemServerOptionUpdateResponse { Contents = new ServerContent() });

        var until = DateTime.UtcNow.AddSeconds(5);

        while (!connection.Frames.Select(PlayerFrame.Open).Any(e => e.Opcode.Equals(marker)) && DateTime.UtcNow < until)
        {
            Thread.Sleep(10);
        }

        return connection.Frames.Select(PlayerFrame.Open).TakeWhile(e => !e.Opcode.Equals(marker)).ToArray();
    }

    private static PlayerFrame[] Wait(RecordingConnection connection, int count)
    {
        var until = DateTime.UtcNow.AddSeconds(5);

        while (connection.Frames.Length < count && DateTime.UtcNow < until)
        {
            Thread.Sleep(10);
        }

        Thread.Sleep(50);

        return connection.Frames.Select(PlayerFrame.Open).ToArray();
    }

    private sealed class MemoryCashShopLogins : ICashShopLogins
    {
        private readonly HashSet<long> _given = [];

        public Task<bool> IsGivenAsync(long session, CancellationToken cancellationToken = default) => Task.FromResult(_given.Contains(session));

        public Task GiveAsync(long session, CancellationToken cancellationToken = default)
        {
            _given.Add(session);

            return Task.CompletedTask;
        }
    }

    private sealed class Balances(long balance) : ICashShopStore
    {
        public Task<long> BalanceAsync(int account, CancellationToken cancellationToken = default) => Task.FromResult(balance);

        public Task<List<CashShopSetPersistent>> ListSetsAsync(int person, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<CashShopSetPersistent>());

        public Task<long?> GiveAsync(int account, long amount, long limit, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CashPurchaseResult> BuyAsync(CashPurchaseDraft draft, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CashReceiver?> FindPersonAsync(string name, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveSetAsync(CashShopSetPersistent list, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> DeleteSetAsync(int person, byte setNo, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
