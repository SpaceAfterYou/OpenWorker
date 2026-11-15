using System.Globalization;
using System.Numerics;
using System.Text;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Channel;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Channels.Components;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Modules.Movement.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua;
using OpenWorker.MazeServer.Services;
using OpenWorker.UpdateContent.Res.Rows;
using StackExchange.Redis;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The open maze of the world (District 6): everybody of a channel shares one instance, and its monsters come back after the time
/// of their box. The Redis rule needs Redis: set OPENWORKER_TEST_REDIS.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class WorldMazeTest
{
    private const short Gate = 1;
    private const short District6 = 30021;

    private static readonly (Guid Server, string Host, short Port) Unregistered = (Guid.Empty, "maze", 100);

    private static ConnectionMultiplexer? _redis;

    private static async Task<IConnectionMultiplexer> RedisAsync()
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
        _redis = null;
    }

    /// <summary>
    /// The first player of a channel opens the instance, the next ones of the channel are sent into it until it is full; the
    /// record follows the instance that holds it, and the next player after it ended opens a new one.
    /// </summary>
    [TestMethod]
    public async Task EverybodyOfAChannelIsSentIntoOneInstanceUntilItIsFull()
    {
        var redis = await RedisAsync();
        var directory = new WorldMazeDirectory(redis);
        var channel = PartyTest.People(1)[0];

        var first = await directory.RouteAsync(Gate, District6, channel, Unregistered, capacity: 2);
        var second = await directory.RouteAsync(Gate, District6, channel, Unregistered, capacity: 2);

        Assert.IsNotNull(first);
        Assert.AreEqual(first.Value.Instance, second?.Instance, "the second player of the channel is sent where the first is");
        Assert.AreEqual(2, second?.Players);
        Assert.IsNull(await directory.RouteAsync(Gate, District6, channel, Unregistered, capacity: 2), "a full instance takes nobody");

        var other = await directory.RouteAsync(Gate, District6, channel + 1, Unregistered, capacity: 2);

        Assert.AreNotEqual(first.Value.Instance, other?.Instance, "another channel has an instance of its own");

        Assert.IsFalse(await directory.HoldAsync(Gate, District6, channel, Guid.NewGuid(), Unregistered, 0), "another instance does not take the record");
        Assert.IsTrue(await directory.HoldAsync(Gate, District6, channel, first.Value.Instance, Unregistered, 1), "the instance renews its record");
        Assert.AreEqual(first.Value.Instance, (await directory.RouteAsync(Gate, District6, channel, Unregistered, capacity: 2))?.Instance,
            "the renewed count lets one more in");

        await directory.ReleaseAsync(Gate, District6, channel, Guid.NewGuid());

        Assert.IsNull(await directory.RouteAsync(Gate, District6, channel, Unregistered, capacity: 2),
            "the release of another instance leaves the record, full again");

        await directory.ReleaseAsync(Gate, District6, channel, first.Value.Instance);

        Assert.AreNotEqual(first.Value.Instance, (await directory.RouteAsync(Gate, District6, channel, Unregistered, capacity: 2))?.Instance,
            "the next player after the instance ended opens a new one");

        await directory.ReleaseAsync(Gate, District6, channel + 1, other!.Value.Instance);
    }

    private const int Sector = 20001;
    private const int Returning = 101;
    private const int Staying = 102;
    private const int Monster = 5000;

    private static string Vector(Vector3 v) => string.Create(CultureInfo.InvariantCulture, $"{v.X:0.000},{v.Y:0.000},{v.Z:0.000}");

    private static string Basic(int id, Vector3 low, Vector3 high) =>
        $"""<m_ID type="0" value="{id}" /> <m_iGenerateID type="0" value="{id}" /> <iLayerBitmask type="4" value="Common" /> <m_vPosTopLeft type="7" value="{Vector(low)}" /> <m_vPosBottomRight type="7" value="{Vector(high)}" /> <m_fRotation type="2" value="0.0000" /> <m_vSize type="7" value="{Vector(high - low)}" /> <m_bShowCustomEntity type="5" value="TRUE" /> <m_ColorLDR type="132" value="0,0,0,255" />""";

    private static string SectorBox() =>
        $"""<VSectorBox boxtype="eEventBoxType_Sector"> {Basic(Sector, Vector3.Zero, new Vector3(100000f))} <m_eType type="4" value="General" /> <m_szEnterLua type="12" value="" /> <m_eClearType type="4" value="None" /> <m_iClearKillRatio type="0" value="0" /> <m_szClearLua type="12" value="" /> <m_iSectorTitle type="0" value="0" /> <m_iSectorExitType type="4" value="Portal" /> <m_iSectorExitID type="0" value="0" /> <m_iRelativeSectorID type="0" value="0" /> <m_iConditionKillRatio_1Step type="0" value="0" /> <m_iConditionKillRatio_2Step type="0" value="0" /> <m_iConditionKillRatio_3Step type="0" value="0" /> <m_iConditionKillRatio_4Step type="0" value="0" /> <m_iConditionKillRatio_5Step type="0" value="0" /> </VSectorBox>""";

    /// <summary>
    /// A box that brings one monster in at once, with the respawn time given in milliseconds.
    /// </summary>
    private static string SpawnBox(int id, float respawn)
    {
        var text = new StringBuilder($"""<VMonsterSpawnBox boxtype="eEventBoxType_MonsterSpawn"> {Basic(id, new Vector3(500f, 500f, 0f), new Vector3(600f, 600f, 100f))} """);

        for (var slot = 1; slot <= 10; slot++)
        {
            var monster = slot == 1 ? Monster : 0;

            text.Append(CultureInfo.InvariantCulture, $"""<m_eType{slot} type="4" value="Monster" /> <m_iMonsterID{slot} type="0" value="{monster}" /> <m_iChance{slot} type="0" value="{(monster == 0 ? 0 : 10000)}" /> """);
        }

        text.Append(CultureInfo.InvariantCulture, $"""<m_eCreationPositionType type="4" value="Center" /> <m_eMoveType type="4" value="Free" /> <m_eCreationCondition type="4" value="Immediate" /> <m_CreationEffectFile type="12" value="" /> <m_fWaitCreationDelayTime type="2" value="0.0000" /> <m_fWaitCreationSequenceTime type="2" value="0.0000" /> <m_iWaitCreationMaxWave type="0" value="1" /> <m_iMaxEntityCount type="0" value="1" /> <m_eWaitCreationSequenceType type="4" value="All" /> <m_iWaypoint type="0" value="0" /> <m_iAggroGroupID type="0" value="0" /> <m_iAggroDistance type="0" value="0" /> <m_iAggroMaxCount type="0" value="0" /> <m_fLookRatio type="2" value="1.0000" /> <m_bShowSight type="5" value="FALSE" /> <m_szObjectKey type="12" value="" /> <m_eScriptType type="4" value="None" /> <m_iCheckScriptHP1 type="0" value="0" /> <m_iCheckScriptHP2 type="0" value="0" /> <m_iCheckScriptHP3 type="0" value="0" /> <m_iCheckScriptHP4 type="0" value="0" /> <m_iCheckScriptHP5 type="0" value="0" /> <m_iSectorID type="0" value="{Sector}" /> <m_ChangeSpawnAction type="12" value="" /> <m_ProtectionTarget type="0" value="0" /> <m_RespawnTime type="2" value="{respawn:0.0000}" /> <m_iStep type="0" value="1" /> <m_eRespawnType type="4" value="None" /> <m_iRespawnCondition type="0" value="0" /> <m_iGroupID type="0" value="0" /> </VMonsterSpawnBox>""");

        return text.ToString();
    }

    private static string StartBox(int id, Vector3 low) =>
        $"""<VStartEventBox boxtype="eEventBoxType_Start"> {Basic(id, low, low + new Vector3(10f))} <m_eSpawnType type="4" value="Random" /> </VStartEventBox>""";

    private static string Map() =>
        "<root><Batchs eventtype=\"EventBox\">" + SectorBox() + SpawnBox(Returning, 1000f) + SpawnBox(Staying, 0f) +
        StartBox(FirstStart, new Vector3(1000f, 1000f, 0f)) + StartBox(SecondStart, new Vector3(5000f, 5000f, 0f)) +
        "</Batchs><Batchs eventtype=\"EventPoint\"/></root>";

    private const int FirstStart = 3002101;
    private const int SecondStart = 3002102;

    /// <summary>
    /// A player who gets up at its safe place stands at the start box it came in by.
    /// </summary>
    [TestMethod]
    public void TheSafePlaceIsTheStartBoxThePlayerCameInBy()
    {
        var world = World.Create();

        try
        {
            var player = world.Create(
                new ActorComponent(1, ActorType.User),
                new ServerSessionComponent(new RecordingConnection()),
                new WorldComponent { Location = 0, Position = new Vector3(100f, 100f, 0f), Rotation = 0f, Jump = SecondStart });
            var maze = TestMazes.Create(world, player, map: Map(), channel: 1);

            maze.ReturnToStart(player);

            var place = world.Get<WorldComponent>(player).Position;

            Assert.IsTrue(place.X is >= 5000f and <= 5010f && place.Y is >= 5000f and <= 5010f, $"stands in the box it came in by, at {place}");

            maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// What the map brings comes out as the first player arrives; a monster of a box with a respawn time comes back that many
    /// milliseconds of game time after it falls, one of a box without stays dead, and the sector never clears.
    /// </summary>
    [TestMethod]
    public async Task AMonsterOfAnOpenMazeComesBackAfterTheTimeOfItsBox()
    {
        var world = World.Create();

        try
        {
            var time = new FakeTimeProvider();
            var clock = TestMazes.Clock(time);
            var player = world.Create(
                new ActorComponent(1, ActorType.User),
                new ServerSessionComponent(new RecordingConnection()),
                new WorldComponent { Location = 0, Position = new Vector3(100f, 100f, 0f), Rotation = 0f, Jump = 0 });
            var maze = TestMazes.Create(world, player, clock: clock, map: Map(), channel: 1);

            int Living() => maze.Creatures.FindMonsters((_, _) => true).Length;

            void Run(TimeSpan span)
            {
                for (var passed = TimeSpan.Zero; passed < span; passed += SimulationClock.Period)
                {
                    time.Advance(SimulationClock.Period);
                    clock.Tick();
                }
            }

            Assert.IsTrue(maze.TryStart());
            Assert.AreEqual(2, Living(), "every box brings its monster at once");

            await maze.ClearSectorAsync(Sector);

            Assert.AreEqual(0, Living());
            Assert.AreEqual(0, maze.Progress.Cleared.Count, "nothing clears in an open maze");
            Assert.IsFalse(maze.IsComplete);

            Run(TimeSpan.FromMilliseconds(900));

            Assert.AreEqual(0, Living(), "the monster is not back before its time");

            Run(TimeSpan.FromMilliseconds(200));

            var back = maze.Creatures.FindMonsters((_, _) => true);

            Assert.HasCount(1, back, "the monster of the box with a respawn time is back, the other stays dead");
            Assert.AreEqual(Monster, world.Get<CreatureComponent>(back[0]).Identifier);
            Assert.AreEqual(Returning, world.Get<NonPlayableCreatureComponent>(back[0]).SpawnBox);

            maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    private sealed class Party(params int[] members) : IPartyRoster
    {
        public ValueTask<IReadOnlyList<int>> MembersAsync(int person) =>
            ValueTask.FromResult<IReadOnlyList<int>>(members.Contains(person) ? members : []);

        public ValueTask<PartyView?> PartyAsync(int person) =>
            ValueTask.FromResult(members.Contains(person) ? new PartyView(1, members[0], members) : null);
    }

    /// <summary>
    /// Where the persons are, as the servers wrote it.
    /// </summary>
    private sealed class Presence(params PresenceRecord[] records) : IPresenceStore
    {
        public ValueTask SetAsync(PresenceRecord record, TimeSpan lifetime) => ValueTask.CompletedTask;

        public ValueTask RemoveAsync(PresenceRecord record) => ValueTask.CompletedTask;

        public ValueTask<PresenceRecord?> GetAsync(int person) => ValueTask.FromResult(records.FirstOrDefault(e => e.Person == person));

        public ValueTask<PresenceRecord?> GetByNameAsync(string name) => ValueTask.FromResult<PresenceRecord?>(null);

        public ValueTask<IReadOnlyList<PresenceRecord?>> GetManyAsync(IReadOnlyList<int> persons) =>
            ValueTask.FromResult<IReadOnlyList<PresenceRecord?>>(persons.Select(e => records.FirstOrDefault(r => r.Person == e)).ToArray());
    }

    /// <summary>
    /// A member of a party goes to the channel where another member already plays the open maze, not to its own.
    /// </summary>
    [TestMethod]
    public async Task APartyMemberGoesToTheChannelWhereAMemberAlreadyPlays()
    {
        var redis = await RedisAsync();
        var directory = new WorldMazeDirectory(redis);
        var world = World.Create();

        // A gate of its own, so the records of the other tests do not meet these.
        const short gate = Gate + 40;

        try
        {
            var channels = new ServiceChannels(Options.Create(new ChannelOptions { From = 1, To = 3 }), world);
            var player = world.Create(new ActorComponent(1, ActorType.User), new ChannelMemberComponent(0));
            var member = new PresenceRecord(Guid.NewGuid(), 2, "Member", 1, 20, District6, 3, true, 0, 0);
            var routes = new WorldMazeRoutes(world, new WorldMazes(TestMazes.None<MazeInfoRow>()), directory,
                Options.Create(new GateOptions { Id = gate }), NullLogger<WorldMazeRoutes>.Instance, channels, new Party(1, 2), new Presence(member));

            var routed = await routes.RouteAsync(player, District6, Unregistered);

            Assert.AreEqual(3, routed?.Channel, "the member goes where the other member plays");

            await directory.ReleaseAsync(gate, District6, 3, routed!.Value.Route.Instance);
        }

        finally
        {
            World.Destroy(world);
        }
    }

    private sealed class RecordingReporter : MazeReporter
    {
        public RecordingReporter() : base(null!, null!, null!, null!, null!, null!)
        {
        }

        public List<Entity> Killed { get; } = [];

        public List<Entity> PartyKilled { get; } = [];

        public List<int> Cubes { get; } = [];

        public override ValueTask MonsterKilledAsync(Entity player, int monster, uint conditions)
        {
            Killed.Add(player);

            return ValueTask.CompletedTask;
        }

        public override ValueTask PartyMonsterKilledAsync(Entity player, int monster)
        {
            PartyKilled.Add(player);

            return ValueTask.CompletedTask;
        }

        public override void RegisterCube(Entity player, int box, int interaction, TimeSpan duration)
        {
            Cubes.Add(box);
        }
    }

    private static Entity Player(World world, int person, Vector3 position, RecordingConnection? connection = null) =>
        world.Create(
            new ActorComponent(person, ActorType.User),
            new ServerSessionComponent(connection ?? new RecordingConnection()),
            new WorldComponent { Location = District6, Position = position, Rotation = 0f, Jump = 0 });

    /// <summary>
    /// A kill in an open maze counts in full for the killer, and for the quests of the members of its party in the instance;
    /// the others in the instance hear nothing of it.
    /// </summary>
    [TestMethod]
    public async Task AKillInAnOpenMazeCountsForTheQuestsOfTheKillersPartyInIt()
    {
        var world = World.Create();

        try
        {
            var killer = Player(world, 1, Vector3.Zero);
            var member = Player(world, 2, Vector3.Zero);
            var stranger = Player(world, 3, Vector3.Zero);
            var reports = new RecordingReporter();
            var maze = TestMazes.Create(world, killer, reports: reports, channel: 1);

            Assert.IsTrue(maze.Join(member));
            Assert.IsTrue(maze.Join(stranger));

            maze.PartyOf = _ => ValueTask.FromResult<IReadOnlyList<Entity>>([killer, member]);

            await maze.ReportKillAsync(killer, 4242, 0);

            CollectionAssert.AreEqual(new[] { killer }, reports.Killed, "the kill is the killer's");
            CollectionAssert.AreEqual(new[] { member }, reports.PartyKilled, "the quests of its party in the instance count it");

            maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    private const string CubeMap =
        """<root><Batchs eventtype="EventBox"><VInterActionBox boxtype="eEventBoxType_InteractionObjectBox"> <m_ID type="0" value="101" /> <m_iGenerateID type="0" value="101" /> <iLayerBitmask type="4" value="Common" /> <m_vPosTopLeft type="7" value="0.000,0.000,0.000" /> <m_vPosBottomRight type="7" value="100.000,100.000,100.000" /> <m_fRotation type="2" value="0.0000" /> <m_vSize type="7" value="100.000,100.000,100.000" /> <m_bShowCustomEntity type="5" value="TRUE" /> <m_ColorLDR type="132" value="0,0,0,255" /> <m_iInteractionID type="0" value="50050" /> <m_sObjectKey type="12" value="UpTown_VaccumCube_1" /> </VInterActionBox></Batchs><Batchs eventtype="EventPoint"/></root>""";

    /// <summary>
    /// The open maze of the world shows none of the cubes its map places; another maze shows them.
    /// </summary>
    [TestMethod]
    public void AnOpenMazeShowsNoCubes()
    {
        var world = World.Create();

        try
        {
            var player = Player(world, 1, Vector3.Zero);
            var reports = new RecordingReporter();
            var open = TestMazes.Create(world, player, reports: reports, map: CubeMap, channel: 1);
            var other = TestMazes.Create(world, player, reports: reports, map: CubeMap);

            open.RegisterCubes(player);

            Assert.IsEmpty(reports.Cubes, "no cube in the open maze");

            other.RegisterCubes(player);

            CollectionAssert.AreEqual(new[] { 101 }, reports.Cubes, "the same map elsewhere has its cube");

            open.Close();
            other.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// In an open maze only the players around see each other: a far one sees nothing of a player and is not told it came; when
    /// it comes near, both are told about each other, and from then on it sees what the other does.
    /// </summary>
    [TestMethod]
    public async Task InAnOpenMazeOnlyThePlayersAroundSeeEachOther()
    {
        var world = World.Create();

        try
        {
            const float cell = WorldOptions.DefaultVisibilityRadius;

            var provider = ChannelMembers.Provider();
            var nearConnection = new RecordingConnection();
            var farConnection = new RecordingConnection();
            var near = Placed(world, provider, nearConnection, 1, new Vector3(100f, 100f, 0f));
            var far = Placed(world, provider, farConnection, 2, new Vector3(cell * 4f, 100f, 0f));
            var maze = TestMazes.Create(world, near, channel: 1);
            var sight = new WorldMazeSight(world);
            var audience = new MazeAudience(world, sight);

            Assert.IsTrue(maze.Join(far));

            world.Add(near, maze);
            world.Add(far, maze);

            sight.Arrive(near, maze);
            sight.Arrive(far, maze);

            var came = default(WorldInInfoPcResponse).Opcode.Value;
            var acted = new MovementTransportOffResponse { Actor = new ActorValue(0, ActorType.User) }.Opcode.Value;

            audience.Nearby(near, new MovementTransportOffResponse { Actor = new ActorValue(1, ActorType.User) });
            audience.Nearby(far, new MovementTransportOffResponse { Actor = new ActorValue(2, ActorType.User) });

            await MetricAssertions.UntilAsync(() => Opcodes(farConnection).Contains(acted), "the far player hears itself");

            Assert.IsFalse(Opcodes(farConnection).Contains(came), "the far player is not told about the other");
            Assert.AreEqual(1, Opcodes(farConnection).Count(e => e == acted), "the far player does not hear the other");

            world.Set(far, world.Get<WorldComponent>(far) with { Position = new Vector3(cell + 100f, 100f, 0f) });
            audience.Relocate(far, new Vector3(cell + 100f, 100f, 0f));

            await MetricAssertions.UntilAsync(() => Opcodes(nearConnection).Contains(came) && Opcodes(farConnection).Contains(came),
                "the two come into sight of each other");

            audience.Nearby(far, new MovementTransportOffResponse { Actor = new ActorValue(2, ActorType.User) });

            await MetricAssertions.UntilAsync(() => Opcodes(nearConnection).Count(e => e == acted) == 2, "the near player sees what the other does now");

            maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    private static Entity Placed(World world, OpenWorker.DependencyInjection.ArchComponentProvider provider, RecordingConnection connection, int person, Vector3 position)
    {
        var player = ChannelMembers.Create(world, provider, connection, person);

        world.Set(player, new ActorComponent(person, ActorType.User));
        world.Set(player, new WorldComponent { Location = District6, Position = position, Rotation = 0f, Jump = 0 });

        return player;
    }

    private static short[] Opcodes(RecordingConnection connection) =>
        connection.Frames.Select(PlayerFrame.Open).Select(e => e.Opcode.Value).ToArray();
}
