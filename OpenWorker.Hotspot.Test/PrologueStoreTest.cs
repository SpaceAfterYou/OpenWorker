using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Persistence;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The prologue flag of a character in a real Postgres: set OPENWORKER_TEST_POSTGRES to a connection string without a
/// database. The run migrates a database of its own and drops it after.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PrologueStoreTest
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

    private static PersonPersistent Person(string name, AccountPersistent account, GatePersistent gate, bool? pending) => new()
    {
        Name = name, Hero = Hero.Haru, DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
        EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0, Location = 10003,
        PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0, Account = account, Gate = gate,
        ProloguePending = pending ?? default
    };

    [TestMethod]
    public async Task FinishingThePrologueWritesItOnceAndTheNextSelectionGoesToTheDistrict()
    {
        int fresh, old, account;
        const short gate = 1;

        await using (var database = await Factory.CreateDbContextAsync())
        {
            var owner = new AccountPersistent { Username = "prologue", PasswordHash = [], SaltHash = [] };
            var place = new GatePersistent { Id = gate, Name = "Test" };

            // A character made before the prologue existed has the flag the column starts with.
            var made = Person("Fresh", owner, place, true);
            var before = Person("Old", owner, place, null);

            database.AddRange(owner, place, made, before);

            await database.SaveChangesAsync();

            (fresh, old, account) = (made.Id, before.Id, owner.Id);
        }

        var store = new PrologueStore(Factory);

        Assert.IsTrue(await store.IsPendingAsync(fresh, account, gate));
        Assert.IsFalse(await store.IsPendingAsync(old, account, gate), "a character made before the prologue has played it");
        Assert.IsFalse(await store.IsPendingAsync(fresh, account + 1, gate), "another account does not own it");

        var first = store.FinishAsync(fresh);
        var second = store.FinishAsync(fresh);

        CollectionAssert.AreEquivalent(new[] { true, false }, await Task.WhenAll(first, second), "two servers finish it once");
        Assert.IsFalse(await store.IsPendingAsync(fresh, account, gate));
        Assert.IsFalse(await store.FinishAsync(old));
    }
}
