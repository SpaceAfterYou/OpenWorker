using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Persistence;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The end of a ranking period in a real Postgres: set OPENWORKER_TEST_POSTGRES to a connection string without a database.
/// The run makes a database of its own from the model and drops it after.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class RankingPeriodStoreTest
{
    private const string Variable = "OPENWORKER_TEST_POSTGRES";
    private const int Map = 21011;

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

    /// <summary>
    /// A period ends once: its places in order with their accounts, the letters written by one of two servers that end it at
    /// the same moment, and only the results recorded before its end taken off the list.
    /// </summary>
    [TestMethod]
    public async Task APeriodEndsOnceAndKeepsTheResultsRecordedAfterIt()
    {
        var end = new DateTime(2026, 11, 16, 0, 0, 0, DateTimeKind.Utc);
        var (slow, fast, late) = (await PersonAsync("Slow"), await PersonAsync("Fast"), await PersonAsync("Late"));

        await Factory.SubmitRankingAsync(RankingKind.TimeAttack, Map, slow.Person, 500, 0, end.AddDays(-2));
        await Factory.SubmitRankingAsync(RankingKind.TimeAttack, Map, fast.Person, 300, 0, end.AddDays(-1));
        await Factory.SubmitRankingAsync(RankingKind.TimeAttack, Map, late.Person, 100, 0, end.AddMinutes(1));

        var places = await Factory.PeriodPlacesAsync(RankingKind.TimeAttack, Map, end, 100);

        CollectionAssert.AreEqual(new[] { new RankingPlace(1, fast.Person, fast.Account), new RankingPlace(2, slow.Person, slow.Account) }, places);

        PostAccountDraft Letter(int account) => new(account, "SoulWorker", "Reward", "Text", 0, [new PostItemPersistent { Item = 1, Count = 1 }], 4, 9);

        var ended = await Task.WhenAll(
            Factory.EndPeriodAsync(RankingKind.TimeAttack, Map, end, [Letter(fast.Account)], end),
            Factory.EndPeriodAsync(RankingKind.TimeAttack, Map, end, [Letter(fast.Account)], end));

        Assert.AreEqual(1, ended.Count(e => e is not null), "one of the two ends it");
        Assert.IsNull(await Factory.PeriodPlacesAsync(RankingKind.TimeAttack, Map, end, 100));

        await using var database = await Factory.CreateDbContextAsync();

        Assert.AreEqual(1, await database.Posts.CountAsync(e => e.ReceiverAccountId == fast.Account));
        CollectionAssert.AreEqual(new[] { late.Person }, await database.RankingEntries.Select(e => e.PersonId).ToArrayAsync());
    }

    private static async Task<(int Person, int Account)> PersonAsync(string name)
    {
        await using var database = await Factory.CreateDbContextAsync();

        var account = new AccountPersistent { Username = name, PasswordHash = [], SaltHash = [] };
        var person = new PersonPersistent
        {
            Name = name, Hero = Hero.Haru, DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
            EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0, Location = 10003,
            PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0, Account = account,
            Gate = new GatePersistent { Id = (short)(name.Length * 10 + name[0]), Name = name }
        };

        database.AddRange(account, person);

        await database.SaveChangesAsync();

        return (person.Id, account.Id);
    }
}
