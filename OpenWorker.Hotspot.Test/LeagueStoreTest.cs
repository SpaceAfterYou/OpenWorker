using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Persistence;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The growth of a league against a real Postgres. Each run migrates a database of its own and drops it after. Set
/// OPENWORKER_TEST_POSTGRES to a connection string without a database. The levels are made here.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class LeagueStoreTest
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

    private static async Task<int> CreatePersonAsync(string name)
    {
        await using var database = await Factory.CreateDbContextAsync();

        var person = new PersonPersistent
        {
            Name = name,
            Hero = Hero.Haru,
            DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
            EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0,
            Location = 10003, PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0,
            FatiguePointCommon = 200,
            Level = 31,
            Account = new AccountPersistent { Username = name, PasswordHash = [], SaltHash = [] },
            Gate = new GatePersistent { Name = name }
        };

        database.Add(person);

        await database.SaveChangesAsync();

        return person.Id;
    }

    private static LeagueInfoRow Level(int level, int exp, int members)
    {
        var bytes = new byte[6 * sizeof(int)];

        BitConverter.TryWriteBytes(bytes, level);
        BitConverter.TryWriteBytes(bytes.AsSpan(sizeof(int)), level);
        BitConverter.TryWriteBytes(bytes.AsSpan(2 * sizeof(int)), exp);
        BitConverter.TryWriteBytes(bytes.AsSpan(3 * sizeof(int)), members);

        return new LeagueInfoRow(new BinaryReader(new MemoryStream(bytes)));
    }

    [TestMethod]
    public async Task ReachingTheExperienceOfTheNextLevelRaisesTheLevelAndTheRoomForMembers()
    {
        var levels = new LeagueLevels(new ReadOnlyCollection<LeagueInfoRow>([Level(1, 0, 20), Level(2, 1000, 25), Level(3, 2000, 30)]));

        var master = await CreatePersonAsync("growmaster");
        var (_, _, league) = await Factory.CreateAsync("Growing", master, LeagueServiceBase.DefaultAuth);

        Assert.IsNotNull(league);
        Assert.AreEqual(20, levels.MemberLimit(league.Rating));

        var first = await Factory.GrowAsync(league.Id, master, 600, 12, levels.Next);

        Assert.AreEqual((byte)1, first!.League.Rating);
        Assert.AreEqual(600, first.League.Exp);

        // 2100 in all: 1000 goes to the second level, the rest is short of the 2000 the third asks for.
        var second = await Factory.GrowAsync(league.Id, master, 1500, 30, levels.Next);

        Assert.AreEqual((byte)1, second!.LevelBefore);
        Assert.AreEqual((byte)2, second.League.Rating);
        Assert.AreEqual(1100, second.League.Exp);
        Assert.AreEqual(42, second.League.Money);
        Assert.AreEqual(1, second.League.Rank);
        Assert.AreEqual(25, levels.MemberLimit(second.League.Rating));

        // The member keeps the whole of what they brought, and the list shows the level of the person.
        var member = await Factory.MemberAsync(master);

        Assert.AreEqual(2100, member!.Exp);
        Assert.AreEqual((byte)31, member.Level);
    }
}
