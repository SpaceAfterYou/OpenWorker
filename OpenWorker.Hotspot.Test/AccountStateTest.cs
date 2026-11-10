using System.Buffers.Binary;
using Arch.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Login;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot.Messages.Response.Person.Enums;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Wire;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.System.Requests;
using OpenWorker.Persistence;
using OpenWorker.Server.Common.Services;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What the rows of accounts and persons keep for a district or a maze, against a real Postgres (OPENWORKER_TEST_POSTGRES): what
/// a server changes, the next entry must find there, and what the others are shown of a person away comes from there.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class AccountStateTest
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

        Services = new ServiceCollection()
            .AddPooledDbContextFactory<PersistenceContext>(builder => builder
                .UseNpgsql(connection + ";Database=openworker_test_" + Guid.NewGuid().ToString("N"))
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

    private static async Task<(int Person, int Account)> PersonAsync(string name, byte level = 1)
    {
        await using var database = await Factory.CreateDbContextAsync();

        var person = new PersonPersistent
        {
            Name = name,
            Hero = Hero.Haru,
            Level = level,
            DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
            EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0,
            Location = 10003, PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0,
            Account = new AccountPersistent { Username = name, PasswordHash = [], SaltHash = [] },
            Gate = new GatePersistent { Name = name }
        };

        database.Add(person);
        await database.SaveChangesAsync();

        return (person.Id, person.Account.Id);
    }

    /// <summary>
    /// The options window of the game saves the options of the account, as the gate does: the next entry, which reads them
    /// from the account, finds them, and the others see the new choice of who may whisper at once.
    /// </summary>
    [TestMethod]
    public async Task OptionsChangedInTheGameComeBackAtTheNextEntry()
    {
        var (person, account) = await PersonAsync("options");
        var community = new MemoryCommunity();

        using var server = new TestServer(community);
        using var player = server.Join(person, "options");

        server.World.Add(player.Entity, new ClaimsComponent(account, 1));

        var options = Enumerable.Repeat((byte)'0', 64).ToArray();
        options[0] = (byte)'5';
        options[PersonOptions.Whisper] = (byte)('0' + PersonOptions.Nobody);

        var handler = new SystemService(server.World, TimeProvider.System, Factory, server.Network);

        server.Run(() => handler.OnHandleAsync(
            new ServiceHandleContext(player.Entity, CancellationToken.None),
            new SystemOptionUpdateRequest(new BinaryReader(new MemoryStream(options)))).AsTask());

        CollectionAssert.AreEqual(options, await Factory.LoadPersonOptionsAsync(account, CancellationToken.None));
        Assert.AreEqual(PersonOptions.Nobody, (await community.GetAsync(person))!.Whisper);
    }

    /// <summary>
    /// The trade password is kept hashed on the account and checked: a wrong one is refused with the code of the account
    /// database and the state goes back to "not given", the right one is accepted; at the next entry the account has a
    /// password the player has not given yet.
    /// </summary>
    [TestMethod]
    public async Task AWrongTradePasswordIsRefusedAndTheRightOneAccepted()
    {
        var (person, account) = await PersonAsync("trader");

        using var server = new TestServer(new MemoryCommunity());
        using var player = server.Join(person, "trader");

        server.World.Add(player.Entity, new ClaimsComponent(account, 1));
        server.World.Add(player.Entity, new TradePasswordComponent { Password = string.Empty, State = PasswordProtectionState.None });

        var handler = new TradePasswordService(server.World, Factory, new MemoryTradePasswordStates());

        (int Error, PasswordProtectionState State) Ask(PasswordCheckType type, string password)
        {
            player.Read(50);

            server.Run(() => handler.OnHandleAsync(new ServiceHandleContext(player.Entity, CancellationToken.None), Request(type, password)).AsTask());

            var answer = player.Read().Single(e => e.Group == (byte)GroupOpcode.Character && e.Command == (byte)CharacterOpcode.TradePassword);

            return (BinaryPrimitives.ReadInt32LittleEndian(answer.Content), (PasswordProtectionState)answer.Content[4]);
        }

        Assert.AreEqual((LoginRules.TradePasswordInvalid, PasswordProtectionState.None), Ask(PasswordCheckType.Create, "1289"), "8 and 9 are not on the pad.");
        Assert.AreEqual((0, PasswordProtectionState.Authenticated), Ask(PasswordCheckType.Create, "1234"));

        await using (var database = await Factory.CreateDbContextAsync())
        {
            var stored = await database.Accounts.Where(e => e.Id == account).Select(e => e.TradePassword).SingleAsync();

            Assert.IsFalse(string.IsNullOrEmpty(stored));
            Assert.AreNotEqual("1234", stored, "The password is kept hashed.");

            // The next entry: the account has a password the player has not given on this server.
            var entry = await Factory.LoadPersonAtEntryAsync(account, person, CancellationToken.None);

            server.World.Set(player.Entity, new TradePasswordComponent { Password = entry!.TradePassword, State = PasswordProtectionState.UnAuthenticated });
        }

        // A password that is not on the pad is no attempt; a wrong one takes the password back and is told as the account database does.
        Assert.AreEqual((LoginRules.TradePasswordInvalid, PasswordProtectionState.UnAuthenticated), Ask(PasswordCheckType.Check, "1289"));
        Assert.AreEqual((TradePasswordService.WrongPassword, PasswordProtectionState.UnAuthenticated), Ask(PasswordCheckType.Check, "1235"));
        Assert.AreEqual((0, PasswordProtectionState.Authenticated), Ask(PasswordCheckType.Check, "1234"));
    }

    /// <summary>
    /// A person who is not online is shown at the level its row keeps, in the list of friends, the block list and a search,
    /// instead of one level for everybody.
    /// </summary>
    [TestMethod]
    public async Task APersonAwayIsShownAtTheLevelOfItsRow()
    {
        var (owner, _) = await PersonAsync("lister");
        var (friend, _) = await PersonAsync("farfriend", 42);
        var (blocked, _) = await PersonAsync("farblocked", 17);

        Assert.AreEqual(InviteOutcome.Sent, await Factory.InviteAsync(owner, friend));
        Assert.AreEqual(AnswerOutcome.Befriended, await Factory.AnswerAsync(friend, owner, accept: true));
        Assert.AreEqual(BlockOutcome.Done, await Factory.BlockAsync(owner, blocked));

        var entry = (await Factory.ListAsync(owner)).Single(e => e.Person == friend);

        Assert.AreEqual((byte)42, FriendBook.ToValue(entry, online: null).Level);
        Assert.AreEqual((byte)17, (await Factory.BlockedAsync(owner)).Single().Level);
        Assert.AreEqual((byte)42, (await Factory.SearchAsync("farfriend", 0, 10)).Single().Level);
    }

    private static PersonTradePasswordRequest Request(PasswordCheckType type, string password)
    {
        var bytes = new byte[1 + 2 + password.Length];

        bytes[0] = (byte)type;
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(1), (ushort)password.Length);
        System.Text.Encoding.ASCII.GetBytes(password, bytes.AsSpan(3));

        var reader = new PacketReader(bytes);

        return PersonTradePasswordRequest.Read(ref reader);
    }
}
