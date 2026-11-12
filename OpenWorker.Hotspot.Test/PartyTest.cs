using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Types;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot.Modules.Party.Enums;
using StackExchange.Redis;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Parties, the recruitment board and the auto matching, kept in Redis where every server sees them. Needs Redis: set
/// OPENWORKER_TEST_REDIS, for example redis://127.0.0.1:6379. Each test takes person numbers of its own, so runs do not meet.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PartyTest
{
    private static ConnectionMultiplexer? _redis;

    public TestContext TestContext { get; set; } = null!;

    internal static async Task<IConnectionMultiplexer> RedisAsync()
    {
        var connection = Environment.GetEnvironmentVariable("OPENWORKER_TEST_REDIS");

        if (string.IsNullOrEmpty(connection))
        {
            Assert.Inconclusive("OPENWORKER_TEST_REDIS is not set.");
        }

        return _redis ??= await ConnectionMultiplexer.ConnectAsync(new RedisOptions { ConnectionString = connection }.ToConfigurationOptions());
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        _redis?.Dispose();
    }

    /// <summary>
    /// Numbers no other test or run uses.
    /// </summary>
    internal static int[] People(int count)
    {
        var first = Random.Shared.Next(10_000_000, 400_000_000);

        return Enumerable.Range(first, count).ToArray();
    }

    internal static PartyMemberCard Card(int person) => new(person, "P" + person, 1, 25);

    [TestMethod]
    public async Task AnAcceptedInvitationFormsAPartyThatTheRosterOfAnotherServerSees()
    {
        var redis = await RedisAsync();
        var here = new PartyBook(redis, TimeProvider.System);
        var there = new PartyBook(redis, TimeProvider.System);
        var (master, member) = People(2) switch { var e => (e[0], e[1]) };

        Assert.IsNull(await here.InviteAsync(member, new PartyInvite(master, "P" + master)));
        Assert.AreEqual(master, (await there.TakeInviteAsync(member))!.Inviter);

        var (outcome, id) = await there.JoinAsync(Card(master), Card(member));

        Assert.AreEqual(PartyJoinOutcome.Created, outcome);

        IPartyRoster roster = here;
        var view = await roster.PartyAsync(member);

        Assert.IsNotNull(view);
        Assert.AreEqual(id, view.Id);
        Assert.AreEqual(master, view.Master);
        CollectionAssert.AreEquivalent(new[] { master, member }, view.Members.ToArray());
        CollectionAssert.AreEquivalent(new[] { master, member }, (await roster.MembersAsync(master)).ToArray());
    }

    [TestMethod]
    public async Task AMasterWhoLeftCannotTakeThePartyBackWithTheNumberKeptForIt()
    {
        var redis = await RedisAsync();
        var book = new PartyBook(redis, TimeProvider.System);
        var people = People(4);
        var master = people[3];

        var (_, party) = await book.JoinAsync(Card(master), Card(people[0]));

        await book.JoinAsync(Card(master), Card(people[1]));
        await book.LeaveAsync(master);

        // The recruitment of the old master still carries the number of the party that goes on without it.
        var again = await book.JoinAsync(Card(master), Card(people[2]), keep: party);

        Assert.AreEqual(PartyJoinOutcome.NotMaster, again.Outcome);
        Assert.AreEqual(people[0], (await book.OfAsync(people[1]))!.Master, "the party keeps its new master");
        Assert.IsNull(await book.OfAsync(master));
        Assert.IsNull(await book.OfAsync(people[2]));
    }

    [TestMethod]
    public async Task TheMasterWhoLeavesHandsThePartyOverAndTheLastButOneOutEndsIt()
    {
        var redis = await RedisAsync();
        var book = new PartyBook(redis, TimeProvider.System);
        var people = People(3);
        var master = people[2];

        Assert.AreEqual(PartyJoinOutcome.Created, (await book.JoinAsync(Card(master), Card(people[0]))).Outcome);
        Assert.AreEqual(PartyJoinOutcome.Joined, (await book.JoinAsync(Card(master), Card(people[1]))).Outcome);

        var left = await book.LeaveAsync(master);

        Assert.IsTrue(left.Left);
        Assert.IsFalse(left.Deleted);
        Assert.AreEqual(people[0], left.Master, "The member with the lowest number takes over.");
        Assert.AreEqual(people[0], (await book.OfAsync(people[1]))!.Master);
        Assert.IsNull(await book.OfAsync(master));

        var last = await book.LeaveAsync(people[1]);

        Assert.IsTrue(last.Deleted, "A party of one is no party.");
        Assert.IsNull(await book.OfAsync(people[0]));
        Assert.IsNull(await book.OfAsync(people[1]));
    }

    [TestMethod]
    public async Task AKickTellsTheKickedAndTheRestOnEveryServer()
    {
        var redis = await RedisAsync();
        var people = People(3);
        var community = new MemoryCommunity();

        using var first = new TestServer(community);
        using var second = new TestServer(community);

        var book = new PartyBook(redis, TimeProvider.System);
        var notifier = Notifier(first, book);

        using var master = first.Join(people[0], "Master");
        using var kicked = second.Join(people[1], "Kicked");
        using var stays = second.Join(people[2], "Stays");

        await book.JoinAsync(Card(people[0]), Card(people[1]));
        await book.JoinAsync(Card(people[0]), Card(people[2]));

        var party = (await book.OfAsync(people[0]))!.Id;

        first.Run(() => notifier.LeaveAsync(people[1], kicked: true));

        foreach (var player in new[] { master, kicked, stays })
        {
            var frames = player.Read();
            var leave = frames.Single(e => e.Group == (byte)GroupOpcode.Party && e.Command == (byte)PartyOpcode.Leave);

            Assert.AreEqual(party, BitConverter.ToInt32(leave.Content, 0));
            Assert.AreEqual(people[1], ((ActorValue)BitConverter.ToInt32(leave.Content, 4)).Identifier);
            Assert.AreEqual(1, leave.Content[8], "The client tells a kick from a leave by this flag.");
        }

        CollectionAssert.AreEquivalent(new[] { people[0], people[2] }, (await book.OfAsync(people[2]))!.Persons.ToArray());
    }

    /// <summary>
    /// A member who stays on the server and changes what the party shows of them, the level or the channel, is shown to the
    /// others anew, and the party keeps the new card.
    /// </summary>
    [TestMethod]
    public async Task AMemberWhoChangesLevelIsShownToTheOthersAnew()
    {
        var redis = await RedisAsync();
        var people = People(2);
        var community = new MemoryCommunity();

        using var first = new TestServer(community);
        using var second = new TestServer(community);

        var book = new PartyBook(redis, TimeProvider.System);
        var notifier = Notifier(first, book);

        using var changed = first.Join(people[0], "Changed");
        using var other = second.Join(people[1], "Other");

        await book.JoinAsync(Card(people[0]), Card(people[1]));
        first.World.Add(changed.Entity, new LevelComponent(30, 0));
        changed.Read();
        other.Read();

        var record = (await first.Network.FindAsync(people[0]))!;

        first.Run(() => notifier.OnChangedAsync(changed.Entity, record).AsTask());

        Assert.IsTrue(other.Read().Any(e => e.Group == (byte)GroupOpcode.Party && e.Command == (byte)PartyOpcode.UpdateMemberInfo), "The others are told");
        Assert.IsFalse(changed.Read().Any(e => e.Group == (byte)GroupOpcode.Party && e.Command == (byte)PartyOpcode.UpdateMemberInfo), "The member is not told of themself");
        Assert.AreEqual(30, (await book.OfAsync(people[1]))!.Members.Single(e => e.Person == people[0]).Card.Level);
    }

    [TestMethod]
    public async Task AnAcceptedApplicationPutsTheApplicantIntoThePartyOfTheRecruitment()
    {
        var redis = await RedisAsync();
        var people = People(2);
        var (master, applicant) = (people[0], people[1]);

        using var server = new TestServer(new MemoryCommunity());
        using var services = Parties(server, redis, TimeProvider.System);

        var book = services.GetRequiredService<PartyBook>();
        var board = services.GetRequiredService<PartyRecruitBoard>();
        var notifier = services.GetRequiredService<PartyNotifier>();

        // A master alone puts up a recruitment under a number kept for the party to come.
        var id = await book.ReserveAsync();
        await board.PutAsync(id, master, "P" + master, new PartyRecruitTerms("Come", 1, 100, 0, 10003));

        var (outcome, applications) = await board.ApplyAsync(id, Card(applicant));

        Assert.AreEqual(PartyApplyError.Success, outcome);
        Assert.AreEqual(1, applications);
        Assert.AreEqual(PartyApplyError.Overlap, (await board.ApplyAsync(id, Card(applicant))).Outcome);

        var joined = PartyJoinOutcome.Full;
        server.Run(async () => joined = await notifier.JoinAsync(Card(master), Card(applicant), keep: id));

        Assert.AreEqual(PartyJoinOutcome.Created, joined);

        var party = await book.OfAsync(applicant);

        Assert.IsNotNull(party);
        Assert.AreEqual(id, party.Id, "The party is the one the board showed.");
        Assert.AreEqual(master, party.Master);
        Assert.IsEmpty(await board.AppliedAsync(applicant), "Who is in a party applies nowhere.");
    }

    [TestMethod]
    public async Task ARecruitmentExpiresAndOnlyOneServerTakesItDown()
    {
        var redis = await RedisAsync();
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var here = new PartyRecruitBoard(redis, time);
        var there = new PartyRecruitBoard(redis, time);
        var master = People(1)[0];
        var id = await new PartyBook(redis, time).ReserveAsync();

        await here.PutAsync(id, master, "P" + master, new PartyRecruitTerms("Come", 1, 100, 0, 10003));

        Assert.IsNotNull(await there.OfAsync(master));

        time.Advance(PartyRules.RecruitLifetime + TimeSpan.FromSeconds(1));

        Assert.IsNull(await there.OfAsync(master));
        Assert.IsFalse((await there.ListAsync()).Any(e => e.Id == id));

        var taken = (await here.ExpireAsync()).Concat(await there.ExpireAsync()).Where(e => e.Id == id).ToArray();

        Assert.HasCount(1, taken);
        Assert.AreEqual(master, taken[0].Master);
    }

    [TestMethod]
    public async Task AnApplicationWhoseTimeIsUpHoldsNoPlaceOnARenewedRecruitment()
    {
        var redis = await RedisAsync();
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var board = new PartyRecruitBoard(redis, time);
        var people = People(PartyRules.MaxApplicants + 2);
        var master = people[0];
        var terms = new PartyRecruitTerms("Come", 1, 100, 0, 10003);
        var id = await new PartyBook(redis, time).ReserveAsync();

        await board.PutAsync(id, master, "P" + master, terms);

        foreach (var person in people.Skip(1).Take(PartyRules.MaxApplicants))
        {
            Assert.AreEqual(PartyApplyError.Success, (await board.ApplyAsync(id, Card(person))).Outcome);
        }

        Assert.AreEqual(PartyApplyError.PartyCount, (await board.ApplyAsync(id, Card(people[^1]))).Outcome, "The recruitment is full.");

        // The master renews the recruitment just before it ends; the applications run out after that.
        time.Advance(PartyRules.RecruitLifetime - TimeSpan.FromSeconds(5));
        await board.PutAsync(id, master, "P" + master, terms);
        time.Advance(TimeSpan.FromSeconds(10));

        Assert.AreEqual(PartyApplyError.Success, (await board.ApplyAsync(id, Card(people[^1]))).Outcome, "A new person takes a freed place.");
        Assert.AreEqual(PartyApplyError.Success, (await board.ApplyAsync(id, Card(people[1]))).Outcome, "Who applied before applies again.");

        await board.TakeAsync(id);
    }

    /// <summary>
    /// Who has blocked a person, or been blocked by them, is not put in the same group with them; the person goes to another
    /// group, or starts one.
    /// </summary>
    [TestMethod]
    public async Task TheAutoMatchingKeepsApartPeopleWhoHaveBlockedEachOther()
    {
        var redis = await RedisAsync();
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var people = People(3);
        var maze = (short)Random.Shared.Next(20_000, 30_000);

        using var server = new TestServer(new MemoryCommunity());

        var matching = new PartyMatching(server.Network, Notifier(server, new PartyBook(redis, time)), redis, time, relations: new Blocks(people[0], people[1]));
        var players = people.Select(e => server.Join(e, "P" + e)).ToArray();

        try
        {
            foreach (var player in players)
            {
                server.Run(() => matching.EnterAsync(player.Entity, maze, PartyRules.MatchingLimit(2)));
            }

            var groups = new List<int>();

            foreach (var person in people)
            {
                groups.Add((await matching.OfAsync(person))!.Id);
            }

            Assert.AreNotEqual(groups[0], groups[1], "The one who blocked and the one blocked wait in different groups");
            Assert.AreEqual(groups[0], groups[2], "Whoever blocked nobody joins the first group there is");
        }

        finally
        {
            foreach (var person in people)
            {
                await matching.ExitAsync(person, OpenWorker.Hotspot.Modules.Party.Enums.PartyMatchingExit.None);
            }

            foreach (var player in players)
            {
                player.Dispose();
            }
        }
    }

    /// <summary>
    /// Who waits in a group is asked whether they go, and the answer is kept with the group; somebody who waits in no
    /// group is given none.
    /// </summary>
    [TestMethod]
    public async Task TheAnswerOfAMemberToTheMatchingCheckIsKeptWithItsGroup()
    {
        var redis = await RedisAsync();
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var people = People(3);
        var maze = (short)Random.Shared.Next(20_000, 30_000);

        using var server = new TestServer(new MemoryCommunity());

        var matching = new PartyMatching(server.Network, Notifier(server, new PartyBook(redis, time)), redis, time);
        var players = people.Take(2).Select(e => server.Join(e, "P" + e)).ToArray();

        try
        {
            foreach (var player in players)
            {
                server.Run(() => matching.EnterAsync(player.Entity, maze, PartyRules.MatchingLimit(2)));
            }

            await matching.CheckAsync(people[0], true);
            await matching.CheckAsync(people[1], false);
            await matching.CheckAsync(people[2], true);

            var group = (await matching.OfAsync(people[0]))!;

            Assert.AreEqual(1, group.Members.Single(e => e.Card.Person == people[0]).Answer, "The one who goes");
            Assert.AreEqual(2, group.Members.Single(e => e.Card.Person == people[1]).Answer, "The one who does not");
            Assert.HasCount(2, group.Members, "An answer makes nobody a member");
        }

        finally
        {
            foreach (var person in people)
            {
                await matching.ExitAsync(person, PartyMatchingExit.None);
            }

            foreach (var player in players)
            {
                player.Dispose();
            }
        }
    }

    private sealed class Blocks(int blocker, int blocked) : IPersonRelations
    {
        public ValueTask<bool> IsFriendAsync(int person, int other) => ValueTask.FromResult(false);

        public ValueTask<bool> IsBlockedAsync(int person, int other) => ValueTask.FromResult(person == blocker && other == blocked);
    }

    [TestMethod]
    public async Task TheAutoMatchingMakesAPartyOfFourWithTheLeaderAsMaster()
    {
        var redis = await RedisAsync();
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var people = People(PartyRules.MaxMembers);
        var maze = (short)Random.Shared.Next(20_000, 30_000);

        using var server = new TestServer(new MemoryCommunity());
        using var services = Parties(server, redis, time);

        var matching = services.GetRequiredService<PartyMatching>();
        var players = people.Select(e => server.Join(e, "P" + e)).ToArray();

        try
        {
            foreach (var player in players)
            {
                server.Run(() => matching.EnterAsync(player.Entity, maze, PartyRules.MatchingLimit(3)));
            }

            var groups = new List<PartyMatchingGroup?>();
            server.Run(async () => groups.AddRange(await Task.WhenAll(people.Select(matching.OfAsync))));

            Assert.AreEqual(1, groups.Select(e => e?.Id).Distinct().Count(), "All four wait in one group.");

            // A full group names its leader at once, and becomes a party when the wait is over.
            server.Run(() => matching.TickAsync());
            time.Advance(PartyRules.MatchingStart);
            server.Run(() => matching.TickAsync());

            var party = await services.GetRequiredService<PartyBook>().OfAsync(people[0]);

            Assert.IsNotNull(party);
            Assert.HasCount(PartyRules.MaxMembers, party.Members);
            Assert.AreEqual(people.Min(), party.Master, "Of equal levels the lowest number leads.");
            Assert.AreEqual(PartyType.Auto, party.Type);
        }

        finally
        {
            foreach (var player in players)
            {
                player.Dispose();
            }
        }
    }

    internal static ServiceProvider Parties(TestServer server, IConnectionMultiplexer redis, TimeProvider time)
    {
        var services = new ServiceCollection();

        services.AddSingleton(server.World);
        services.AddSingleton(server.Network);
        services.AddSingleton(redis);
        services.AddSingleton(time);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<PartyBook>();
        services.AddSingleton<PartyNotifier>();
        services.AddSingleton<PartyRecruitBoard>();
        services.AddSingleton<PartyAwaiterBoard>();
        services.AddSingleton<PartyRecruitNotifier>();
        services.AddSingleton<IPartyJoinListener>(e => e.GetRequiredService<PartyRecruitNotifier>());
        services.AddSingleton<PartyMatching>();

        return services.BuildServiceProvider();
    }

    internal static PartyNotifier Notifier(TestServer server, PartyBook book, TimeProvider? time = null)
    {
        return new PartyNotifier(
            server.World,
            server.Network,
            book,
            time ?? TimeProvider.System,
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<PartyNotifier>.Instance);
    }
}
