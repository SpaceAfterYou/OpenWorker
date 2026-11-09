using Arch.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.System.Requests;
using OpenWorker.Persistence;
using OpenWorker.Server.Common.Services;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What an account keeps that a district or a maze changes, against a real Postgres (OPENWORKER_TEST_POSTGRES): the next entry
/// must find it there.
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

    private static async Task<(int Person, int Account)> PersonAsync(string name)
    {
        await using var database = await Factory.CreateDbContextAsync();

        var person = new PersonPersistent
        {
            Name = name,
            Hero = Hero.Haru,
            Level = 1,
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
}
