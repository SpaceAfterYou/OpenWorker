using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Persistence;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The orbs of friends against a real Postgres (OPENWORKER_TEST_POSTGRES): what the database holds to, so that every server
/// sees the same orbs and uses.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class HelperSupportTest
{
    private const string Variable = "OPENWORKER_TEST_POSTGRES";

    private static IDbContextFactory<PersistenceContext> Factory { get; set; } = null!;

    private static ServiceProvider? Services { get; set; }

    private static readonly DateTime Now = new(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

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

    private static async Task<int> PersonAsync(string name, byte level)
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

        return person.Id;
    }

    private static async Task BefriendAsync(int one, int other)
    {
        await using var database = await Factory.CreateDbContextAsync();

        database.Friends.Add(new FriendPersistent { OwnerId = one, TargetId = other, Kind = FriendKind.Friend });
        database.Friends.Add(new FriendPersistent { OwnerId = other, TargetId = one, Kind = FriendKind.Friend });

        await database.SaveChangesAsync();
    }

    /// <summary>
    /// An orb is listed to the friends of the one who offers it and to nobody else, at the value it gives the reader: cut down
    /// for a reader more than five levels below. A friend uses it once, and it leaves the friend's list; nobody uses it after
    /// its day.
    /// </summary>
    [TestMethod]
    public async Task AnOrbIsListedToFriendsAndUsedOncePerFriendWhileItsDayLasts()
    {
        var supporter = await PersonAsync("supporter", 60);
        var friend = await PersonAsync("friend", 10);
        var late = await PersonAsync("late", 10);
        var stranger = await PersonAsync("stranger", 10);

        await BefriendAsync(supporter, friend);
        await BefriendAsync(supporter, late);

        Assert.AreEqual(SupportRegisterOutcome.Registered, await Factory.RegisterSupportAsync(supporter, 0, 120f, Now.AddDays(1), Now));

        var offer = (await Factory.SupportOffersAsync(friend, Now)).Single();

        Assert.AreEqual((supporter, (byte)60, 120f), (offer.Friend, offer.Level, offer.Value));
        Assert.AreEqual(20f, HelperSupports.Scaled(offer.Value, offer.Level, 10), 0.001f, "a friend 50 levels above gives a sixth");
        Assert.AreEqual(120f, HelperSupports.Scaled(offer.Value, 15, 10), "five levels above is not cut");
        Assert.IsEmpty(await Factory.SupportOffersAsync(stranger, Now), "a stranger does not see the orb");

        Assert.AreEqual(SupportUseOutcome.Missing, (await Factory.UseSupportAsync(stranger, supporter, 0, Now)).Outcome, "nor uses it");
        Assert.AreEqual(SupportUseOutcome.Used, (await Factory.UseSupportAsync(friend, supporter, 0, Now)).Outcome);
        Assert.AreEqual(SupportUseOutcome.AlreadyUsed, (await Factory.UseSupportAsync(friend, supporter, 0, Now)).Outcome, "once per friend");
        Assert.IsEmpty(await Factory.SupportOffersAsync(friend, Now), "a used orb leaves the list");

        Assert.IsEmpty(await Factory.SupportOffersAsync(late, Now.AddDays(1)), "the orb is gone at the end of its day");
        Assert.AreEqual(SupportUseOutcome.Expired, (await Factory.UseSupportAsync(late, supporter, 0, Now.AddDays(1))).Outcome);
    }

    /// <summary>
    /// The reward for offering an orb is taken once. A new orb is offered only after the day of the last one is over and its
    /// reward taken, and the friends who used the last one can use the new one.
    /// </summary>
    [TestMethod]
    public async Task TheRewardIsTakenOnceAndANewOrbWaitsForTheEndOfTheLast()
    {
        var supporter = await PersonAsync("giver", 30);
        var friend = await PersonAsync("taker", 30);

        await BefriendAsync(supporter, friend);

        Assert.AreEqual(SupportRegisterOutcome.Registered, await Factory.RegisterSupportAsync(supporter, 1, 50f, Now.AddDays(1), Now));
        Assert.AreEqual(SupportUseOutcome.Used, (await Factory.UseSupportAsync(friend, supporter, 0, Now)).Outcome);

        var later = Now.AddDays(1);

        Assert.AreEqual(SupportRegisterOutcome.RewardWaiting, await Factory.RegisterSupportAsync(supporter, 0, 60f, later.AddDays(1), later),
            "a reward still to take holds the next orb back, even after the day");

        Assert.IsTrue(await Factory.ClaimSupportRewardAsync(supporter, Now));
        Assert.IsFalse(await Factory.ClaimSupportRewardAsync(supporter, Now), "the reward is given once");

        Assert.AreEqual(SupportRegisterOutcome.StillValid, await Factory.RegisterSupportAsync(supporter, 0, 60f, Now.AddDays(2), Now.AddHours(1)));
        Assert.AreEqual(SupportRegisterOutcome.Registered, await Factory.RegisterSupportAsync(supporter, 0, 60f, later.AddDays(1), later));

        Assert.AreEqual(SupportUseOutcome.Used, (await Factory.UseSupportAsync(friend, supporter, 0, later)).Outcome, "a new orb, a new use");
        Assert.AreEqual(HelperSupportPersistent.RewardWaiting, (await Factory.SupportOfAsync(supporter))!.Reward);
    }
}
