using System.Collections.ObjectModel;
using System.Text;
using Arch.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.DistrictServer.Services;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Post;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Post;
using OpenWorker.Hotspot.Modules.Post.Requests;
using OpenWorker.Hotspot.Modules.Post.Types;
using OpenWorker.Persistence;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The letters against a real Postgres: the store, and the handlers of the district over it. Each run migrates a database
/// of its own and drops it after. Set OPENWORKER_TEST_POSTGRES to a connection string without a database.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PostTest
{
    private const string Variable = "OPENWORKER_TEST_POSTGRES";

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

    private static PostDraft Draft(int sender, string senderName, string receiverName, long money, params PostItemPersistent[] items)
    {
        return new PostDraft(sender, senderName, (byte)Hero.Haru, receiverName, "Title", "Body", money, 100, items);
    }

    [TestMethod]
    public async Task ALetterReachesItsReceiverWhatItCarriesIsTakenOnceAndItIsNotDeletedWithIt()
    {
        var store = new PostStore(Factory);

        var (_, sender) = await CreatePersonAsync("postfrom");
        var (_, receiver) = await CreatePersonAsync("postto");

        var sword = new PostItemPersistent { Item = 111000001, Count = 1, Upgrade = 7 };

        // The name is found whatever its case.
        var sent = await store.SendAsync(Draft(sender, "postfrom", "PostTo", 500, sword));

        Assert.AreEqual(PostSendOutcome.Sent, sent.Outcome);
        Assert.AreEqual(1, sent.Unread);

        var serial = sent.Letter!.Id;
        var box = await store.ListReceivedAsync(receiver, saved: false);

        Assert.AreEqual(serial, box.Single().Id);
        Assert.AreEqual(7, box.Single().Items.Single().Upgrade, "the item travels with what was done to it");
        Assert.AreEqual(serial, (await store.ListSentAsync(sender)).Single().Id);

        // What it carries is not deleted with it.
        Assert.AreEqual(PostDeleteOutcome.HasAttachments, await store.DeleteReceivedAsync(serial, receiver));

        // Somebody else cannot take it; the receiver takes it once.
        Assert.IsNull(await store.ClaimAsync(serial, sender));
        Assert.AreEqual(500, (await store.ClaimAsync(serial, receiver))?.Money);
        Assert.IsNull(await store.ClaimAsync(serial, receiver), "what a letter carries is taken once");

        // A claim the game could not honour is put back and can be made again.
        await store.ReleaseAsync(serial);

        Assert.IsNotNull(await store.ClaimAsync(serial, receiver));

        // Now the receiver deletes it; the sender lists it until they delete it too, and then it is gone.
        Assert.AreEqual(PostDeleteOutcome.Deleted, await store.DeleteReceivedAsync(serial, receiver));
        Assert.IsEmpty(await store.ListReceivedAsync(receiver, saved: false));
        Assert.HasCount(1, await store.ListSentAsync(sender));
        Assert.AreEqual(PostDeleteOutcome.Deleted, await store.DeleteSentAsync(serial, sender));

        await using var database = await Factory.CreateDbContextAsync();

        Assert.IsFalse(await database.Posts.AnyAsync(e => e.Id == serial), "a letter neither side keeps is gone");
    }

    // An item that does not stack, the only row of the item table of the test.
    private const int Sword = 900_000_001;

    /// <summary>
    /// A row of the item table with nothing but its key: an item that lives in the common bag and does not stack.
    /// </summary>
    private static ItemCatalog Catalog()
    {
        var bytes = new byte[1024];

        BitConverter.TryWriteBytes(bytes, Sword);

        var row = new ItemRow(new BinaryReader(new MemoryStream(bytes)));

        return new ItemCatalog(new ReadOnlyCollection<ItemRow>([row]), new([]), new([]), new([]));
    }

    private static void Text(BinaryWriter writer, string value)
    {
        writer.Write((short)(2 * value.Length + 1));
        writer.Write(Encoding.Unicode.GetBytes(value));
        writer.Write((byte)0);
    }

    private static BinaryReader Body(Action<BinaryWriter> write)
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        write(writer);
        writer.Flush();
        stream.Position = 0;

        return new BinaryReader(stream);
    }

    private static BinaryReader Serial(long serial) => Body(w =>
    {
        w.Write(0);
        w.Write(serial);
    });

    [TestMethod]
    public async Task ALetterWithGoldAndAnItemIsSentListedReadAndTakenOnceThroughTheHandlers()
    {
        var (_, from) = await CreatePersonAsync("sender");
        var (toAccount, to) = await CreatePersonAsync("receiver");

        var catalog = Catalog();
        var community = new MemoryCommunity();

        using var server = new TestServer(community);
        using var sender = server.Join(from, "sender");
        using var receiver = server.Join(to, "receiver");

        var inventory = new InventoryService(server.World, catalog, new LocalItemSerials());
        var factory = new InventoryFactory(catalog, new([]), new LocalItemSerials());

        var service = new PostService(server.World, new PostStore(Factory), inventory, server.Network, NoPersonFlush.Instance, TestPersonCommit.Written,
            TimeProvider.System,
            NullLogger<PostService>.Instance);

        foreach (var (player, account) in new[] { (sender, 1), (receiver, toAccount) })
        {
            var person = new PersonPersistent
            {
                Id = player.Person, Name = player.Name, Hero = Hero.Lily,
                DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
                EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0,
                Location = 0, PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0,
                Account = null!, Gate = null!
            };

            server.World.Add(player.Entity, new InventoryComponent(factory.CreateNew(person)), new CurrencyComponent { Gold = 1_000 },
                new ClaimsComponent(account, 0));
        }

        var bag = server.World.Get<InventoryComponent>(sender.Entity).Value!;

        bag.Place(StorageGroup.Common, 3, new ItemInstance { Serial = 77, Item = Sword, Count = 1, Upgrade = 7 });

        void Handle(TestPlayer player, Func<ServiceHandleContext, ValueTask> handle) =>
            server.Run(async () => await handle(new ServiceHandleContext(player.Entity, CancellationToken.None)));

        // The sender writes to the receiver, with 300 gold and the sword.
        Handle(sender, c => service.OnHandleAsync(c, new PostSendRequest(Body(w =>
        {
            w.Write(0);
            Text(w, "receiver");
            Text(w, "Hello");
            Text(w, "A sword for you.");
            w.Write(300L);

            for (var i = 0; i < PostModuleDefines.ItemCount; i++)
            {
                w.Write((byte)(i == 0 ? StorageGroup.Common : StorageGroup.None));
                w.Write((short)(i == 0 ? 3 : -1));
                w.Write((short)(i == 0 ? 1 : 0));
            }
        }))));

        Assert.IsNull(bag.At(StorageGroup.Common, 3), "the sword left with the letter");
        Assert.AreEqual(1_000 - 300 - PostLetters.Postage, server.World.Get<CurrencyComponent>(sender.Entity).Gold);

        var sent = sender.Read().Single(e => e.Group == (byte)GroupOpcode.Post);

        Assert.AreEqual((byte)PostOpcode.Send, sent.Command);
        Assert.AreEqual(0, BitConverter.ToInt32(sent.Content), "the letter went");

        // The receiver is told and finds it in the box.
        var told = receiver.Read().Single();

        Assert.AreEqual((byte)PostOpcode.Recv, told.Command);

        Handle(receiver, c => service.OnHandleAsync(c, new PostRecvListRequest(Body(w => w.Write(0)))));

        var list = receiver.Read().Single();
        using var reader = new BinaryReader(new MemoryStream(list.Content));

        Assert.AreEqual((byte)PostOpcode.RecvList, list.Command);
        Assert.AreEqual(1, reader.ReadByte());

        var letter = new PostValue(reader);

        Assert.AreEqual("sender", letter.Character.Name);
        Assert.AreEqual(300, letter.Money);
        Assert.AreEqual(Sword, letter.Items[0].Item);

        Handle(receiver, c => service.OnHandleAsync(c, new PostReadRequest(Serial(letter.Serial))));

        Assert.AreEqual((byte)PostOpcode.Read, receiver.Read().Single().Command);

        // Taking it gives the gold and the sword as it was sent; taking it again gives nothing.
        Handle(receiver, c => service.OnHandleAsync(c, new PostReceiptRequest(Serial(letter.Serial))));
        Handle(receiver, c => service.OnHandleAsync(c, new PostReceiptRequest(Serial(letter.Serial))));

        var swords = server.World.Get<InventoryComponent>(receiver.Entity).Value!.Items.Where(e => e.Item == Sword).ToList();

        Assert.AreEqual(7, swords.Single().Upgrade);
        Assert.AreNotEqual(77, swords.Single().Serial, "the receiver holds it under a serial of its own");
        Assert.AreEqual(1_300, server.World.Get<CurrencyComponent>(receiver.Entity).Gold);

        var answers = receiver.Read().Where(e => e.Group == (byte)GroupOpcode.Post).ToList();

        Assert.AreEqual((byte)PostOpcode.Receipt, answers[0].Command);
        Assert.AreEqual((byte)PostOpcode.Receipt | 0x80, answers[1].Command, "the second time is refused");
        Assert.AreEqual(PostMessages.Deleted, BitConverter.ToUInt16(answers[1].Content));
    }

    /// <summary>
    /// An item that is bound to its owner, or broken, does not go into a letter: the letter is refused with the text for it, the
    /// item stays in the bag and the sender pays nothing. A free item goes. (The rule is the one of the trade and the shops of
    /// players: <c>ItemBinding.HeldBack</c>.)
    /// </summary>
    [TestMethod]
    public async Task ABoundOrBrokenItemIsNotMailedAndAFreeOneIs()
    {
        var (_, from) = await CreatePersonAsync("bsender");
        var (toAccount, to) = await CreatePersonAsync("breceiver");

        using var server = new TestServer(new MemoryCommunity());
        using var sender = server.Join(from, "bsender");
        using var receiver = server.Join(to, "breceiver");

        var inventory = new InventoryService(server.World, Catalog(), new LocalItemSerials());
        var factory = new InventoryFactory(Catalog(), new([]), new LocalItemSerials());

        var service = new PostService(server.World, new PostStore(Factory), inventory, server.Network, NoPersonFlush.Instance, TestPersonCommit.Written,
            TimeProvider.System,
            NullLogger<PostService>.Instance);

        foreach (var (player, account) in new[] { (sender, 1), (receiver, toAccount) })
        {
            var person = new PersonPersistent
            {
                Id = player.Person, Name = player.Name, Hero = Hero.Lily,
                DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
                EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0,
                Location = 0, PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0,
                Account = null!, Gate = null!
            };

            server.World.Add(player.Entity, new InventoryComponent(factory.CreateNew(person)), new CurrencyComponent { Gold = 1_000 },
                new ClaimsComponent(account, 0));
        }

        var bag = server.World.Get<InventoryComponent>(sender.Entity).Value!;

        bag.Place(StorageGroup.Common, 3, new ItemInstance { Serial = 71, Item = Sword, Count = 1, BindState = ItemBindState.BoundByBroach });
        bag.Place(StorageGroup.Common, 4, new ItemInstance { Serial = 72, Item = Sword, Count = 1, Flag = 2 });
        bag.Place(StorageGroup.Common, 5, new ItemInstance { Serial = 73, Item = Sword, Count = 1 });

        void Send(short slot) => server.Run(async () => await service.OnHandleAsync(
            new ServiceHandleContext(sender.Entity, CancellationToken.None),
            new PostSendRequest(Body(w =>
            {
                w.Write(0);
                Text(w, "breceiver");
                Text(w, "Hello");
                Text(w, "A sword.");
                w.Write(0L);

                for (var i = 0; i < PostModuleDefines.ItemCount; i++)
                {
                    w.Write((byte)(i == 0 ? StorageGroup.Common : StorageGroup.None));
                    w.Write((short)(i == 0 ? slot : -1));
                    w.Write((short)(i == 0 ? 1 : 0));
                }
            }))));

        Send(3);
        Send(4);

        Assert.IsNotNull(bag.At(StorageGroup.Common, 3), "the bound sword stayed");
        Assert.IsNotNull(bag.At(StorageGroup.Common, 4), "the broken sword stayed");
        Assert.AreEqual(1_000, server.World.Get<CurrencyComponent>(sender.Entity).Gold, "nothing was paid");

        var refusals = sender.Read().Where(e => e.Group == (byte)GroupOpcode.Post).ToList();

        Assert.HasCount(2, refusals);
        Assert.IsTrue(refusals.All(e => e.Command == (byte)PostOpcode.Send && BitConverter.ToInt32(e.Content) != 0), "both letters were refused");
        Assert.IsEmpty(receiver.Read().Where(e => e.Group == (byte)GroupOpcode.Post), "the receiver heard nothing");

        Send(5);

        Assert.IsNull(bag.At(StorageGroup.Common, 5), "the free sword went");
    }

    /// <summary>
    /// The gold and the items of a letter are written as gone from the sender before the letter is: when that write fails, the
    /// letter does not go, the sender has them back and is answered. A crash after the write can then only lose them, never
    /// leave them both with the sender and in the letter.
    /// </summary>
    [TestMethod]
    public async Task ALetterWhoseGoldAndItemsAreNotWrittenFirstDoesNotGo()
    {
        var (fromAccount, from) = await CreatePersonAsync("usender");
        await CreatePersonAsync("ureceiver");

        using var server = new TestServer(new MemoryCommunity());
        using var sender = server.Join(from, "usender");

        var inventory = new InventoryService(server.World, Catalog(), new LocalItemSerials());
        var factory = new InventoryFactory(Catalog(), new([]), new LocalItemSerials());
        var commit = new TestPersonCommit(false);

        var service = new PostService(server.World, new PostStore(Factory), inventory, server.Network, NoPersonFlush.Instance, commit,
            TimeProvider.System, NullLogger<PostService>.Instance);

        var person = new PersonPersistent
        {
            Id = from, Name = "usender", Hero = Hero.Lily,
            DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
            EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0,
            Location = 0, PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0,
            Account = null!, Gate = null!
        };

        server.World.Add(sender.Entity, new InventoryComponent(factory.CreateNew(person)), new CurrencyComponent { Gold = 1_000 },
            new ClaimsComponent(fromAccount, 0));

        var bag = server.World.Get<InventoryComponent>(sender.Entity).Value!;

        bag.Place(StorageGroup.Common, 3, new ItemInstance { Serial = 81, Item = Sword, Count = 1, Upgrade = 5 });

        server.Run(async () => await service.OnHandleAsync(
            new ServiceHandleContext(sender.Entity, CancellationToken.None),
            new PostSendRequest(Body(w =>
            {
                w.Write(0);
                Text(w, "ureceiver");
                Text(w, "Hello");
                Text(w, "A sword.");
                w.Write(300L);

                for (var i = 0; i < PostModuleDefines.ItemCount; i++)
                {
                    w.Write((byte)(i == 0 ? StorageGroup.Common : StorageGroup.None));
                    w.Write((short)(i == 0 ? 3 : -1));
                    w.Write((short)(i == 0 ? 1 : 0));
                }
            }))));

        Assert.AreEqual(1, commit.Calls, "what the letter carries was written first");
        Assert.AreEqual(1_000, server.World.Get<CurrencyComponent>(sender.Entity).Gold, "the gold came back");
        Assert.AreEqual(5, bag.Items.Single(e => e.Item == Sword).Upgrade, "the sword came back as it was");

        var answer = sender.Read().Single(e => e.Group == (byte)GroupOpcode.Post);

        Assert.AreEqual((byte)PostOpcode.Send, answer.Command);
        Assert.AreEqual(PostMessages.Deleted, BitConverter.ToInt32(answer.Content), "the sender is answered");

        await using var database = await Factory.CreateDbContextAsync();

        Assert.IsFalse(await database.Posts.AnyAsync(e => e.SenderId == from), "no letter was written");
    }

    /// <summary>
    /// A clock of its own for a test of letters whose time is up, set in a year of its own: the database is shared by the
    /// tests of the class, and a sweep at that time reaches no letter of a test with a later year.
    /// </summary>
    private static FakeTimeProvider Clock(int year) => new(new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero));

    /// <summary>
    /// Letters straight into the table, for a person or an account, sent at the time of the clock.
    /// </summary>
    private static async Task<List<long>> AddLettersAsync(TimeProvider clock, int count, int? person, int? account, PostState state = PostState.None)
    {
        await using var database = await Factory.CreateDbContextAsync();

        var now = clock.GetUtcNow().UtcDateTime;

        var letters = Enumerable.Range(0, count).Select(_ => new PostPersistent
        {
            Kind = person is null ? PostKind.Account : PostKind.Person,
            ReceiverId = person,
            ReceiverAccountId = person is null ? account : null,
            SenderName = "game",
            Title = "Title",
            Body = "Body",
            Money = 10,
            State = state,
            CreatedAt = now,
            UpdatedAt = now,
            ExpiresAt = now + PostLimits.Lifetime
        }).ToList();

        database.Posts.AddRange(letters);

        await database.SaveChangesAsync();

        return letters.Select(e => e.Id).ToList();
    }

    /// <summary>
    /// A letter whose time is up is gone for both sides with what it carries, whether the sweep has run or not: no list
    /// shows it, no count counts it and it cannot be taken. A letter its receiver keeps has no end.
    /// </summary>
    [TestMethod]
    public async Task ALetterWhoseTimeIsUpIsNotListedAndAKeptOneIs()
    {
        var clock = Clock(2001);
        var store = new PostStore(Factory, clock);

        var (_, sender) = await CreatePersonAsync("deadfrom");
        var (account, receiver) = await CreatePersonAsync("deadto");

        var dead = (await store.SendAsync(Draft(sender, "deadfrom", "deadto", 500))).Letter!.Id;
        var kept = (await store.SendAsync(Draft(sender, "deadfrom", "deadto", 500))).Letter!.Id;
        var shared = (await store.SendToAccountAsync(new PostAccountDraft(account, "game", "Title", "Body", 10, []))).Id;

        Assert.IsNotNull(await store.SaveAsync(kept, receiver));

        clock.Advance(PostLimits.Lifetime + TimeSpan.FromSeconds(1));

        Assert.IsEmpty(await store.ListReceivedAsync(receiver, saved: false));
        Assert.IsEmpty(await store.ListSentAsync(sender), "the sender lists it until its time is up");
        Assert.IsEmpty(await store.ListAccountAsync(account));
        Assert.AreEqual((0, 0), await store.CountUnreadAsync(receiver, account));
        Assert.IsNull(await store.ClaimAsync(dead, receiver), "what it carried is gone with it");
        Assert.IsNull(await store.ClaimAccountAsync(shared, account));

        var box = await store.ListReceivedAsync(receiver, saved: true);

        Assert.AreEqual(kept, box.Single().Id, "a kept letter stays");
        Assert.AreEqual(0, PostLetters.RemainTime(box.Single(), clock.GetUtcNow().UtcDateTime), "and shows no end");
        Assert.IsNotNull(await store.ClaimAsync(kept, receiver));
    }

    /// <summary>
    /// The limit of a box counts the letters that live: a box full of letters whose time is up takes a new one.
    /// </summary>
    [TestMethod]
    public async Task ALetterWhoseTimeIsUpTakesNoRoomInTheBox()
    {
        var clock = Clock(2002);
        var store = new PostStore(Factory, clock);

        var (_, sender) = await CreatePersonAsync("fullfrom");
        var (_, receiver) = await CreatePersonAsync("fullto");

        await AddLettersAsync(clock, PostLimits.MaxLetters, receiver, null);

        Assert.AreEqual(PostSendOutcome.Full, (await store.SendAsync(Draft(sender, "fullfrom", "fullto", 0))).Outcome);

        clock.Advance(PostLimits.Lifetime + TimeSpan.FromSeconds(1));

        Assert.AreEqual(PostSendOutcome.Sent, (await store.SendAsync(Draft(sender, "fullfrom", "fullto", 0))).Outcome);
    }
}
