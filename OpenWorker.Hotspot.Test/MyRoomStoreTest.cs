using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Persistence;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The rooms in a real Postgres: set OPENWORKER_TEST_POSTGRES to a connection string without a database. The run makes a
/// database of its own from the model and drops it after.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MyRoomStoreTest
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

        await context.Database.EnsureCreatedAsync();
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

    [TestMethod]
    public async Task AnAccountMakesOneRoomPerGateAndItsFurnitureIsKeptPieceByPiece()
    {
        int account, person;
        const short gate = 1;

        await using (var database = await Factory.CreateDbContextAsync())
        {
            var owner = new AccountPersistent { Username = "room", PasswordHash = [], SaltHash = [] };
            var place = new GatePersistent { Id = gate, Name = "Test" };
            var character = new PersonPersistent
            {
                Name = "Roomer", Hero = Hero.Haru, DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
                EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0, Location = 10003,
                PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0, Account = owner, Gate = place
            };

            database.AddRange(owner, place, character);

            await database.SaveChangesAsync();

            (account, person) = (owner.Id, character.Id);
        }

        var store = new MyRoomStore(Factory);
        var room = new MyRoomRecord(account, gate, 1, 1, 0, "Roomer");

        Assert.AreEqual(account, await store.AccountOfAsync(person, gate));
        Assert.IsNull(await store.AccountOfAsync(person, gate + 1), "the character is not of that gate");

        // Two servers that make the room at once make it once.
        var made = await Task.WhenAll(store.CreateAsync(room), store.CreateAsync(room with { Name = "Other" }));

        Assert.AreEqual(1, made.Count(e => e));
        Assert.IsNotNull(await store.FindAsync(account, gate));

        await store.SetupAsync(account, gate, 1, "Closed");

        Assert.AreEqual(room with { OpenLevel = 1, Name = "Closed" }, await store.FindAsync(account, gate));

        Assert.IsTrue(await store.PlaceAsync(account, gate, new MyRoomPiece(500, 880000001, 0, 10)));
        Assert.IsTrue(await store.PlaceAsync(account, gate, new MyRoomPiece(501, 880000002, 1, 11)));
        Assert.IsFalse(await store.PlaceAsync(account, gate, new MyRoomPiece(500, 880000001, 0, 12)), "a serial stands once");

        await store.MoveAsync(account, gate, [new MyRoomPiece(501, 880000002, 3, 42), new MyRoomPiece(999, 1, 0, 0)]);

        Assert.IsTrue(await store.RemoveAsync(account, gate, 500));
        Assert.IsFalse(await store.RemoveAsync(account, gate, 500), "of two removals one gives the item back");

        CollectionAssert.AreEqual(new[] { new MyRoomPiece(501, 880000002, 3, 42) }, (await store.FurnitureAsync(account, gate)).ToArray());
    }
}
