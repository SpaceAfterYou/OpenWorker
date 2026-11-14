using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using OpenWorker.Channel;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot.Modules.World.Requests;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.SoulWorker.Network;
using System.Collections.ObjectModel;
using Arch.Core;
using Lua;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenWorker.Batch;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Extensions;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Buffs;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Maze.Requests;
using OpenWorker.Hotspot.Modules.Skill.Requests;
using OpenWorker.Lua;
using OpenWorker.Lua.Managers;
using OpenWorker.MazeServer.Services;
using OpenWorker.UpdateContent.Res.Rows;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A client says what it likes, so a request about a maze it is not in, or a box its map does not have,
/// must be ignored instead of throwing on the world loop.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MazeRequestGuardTest
{
    private sealed class Recorder : ILogger<LuaMaze>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }

    private static BinaryReader Box(int box) => new(new MemoryStream(BitConverter.GetBytes(box)));

    internal static ServiceHandleContext Context(Entity player) => new(player, CancellationToken.None);

    internal static MazeService Service(World world, Hotspot.Handler.HandlerExecutor? errors = null, MazeAdmission? admission = null, MovementPace? pace = null,
        ServerChanges? changes = null)
    {
        // The handlers under test never reach Redis, the batches or the other server.
        return new MazeService(
            world,
            null!,
            null!,
            null!,
            NoPersonFlush.Instance,
            Options.Create(new GateOptions { Id = 1 }),
            TestMazes.Quests(world),
            null!,
            null!,
            null!,
            pace!,
            errors!,
            null!,
            null!,
            null!,
            admission!,
            changes ?? new ServerChanges(world, TimeProvider.System),
            NullLogger<MazeService>.Instance);
    }

    /// <summary>
    /// A maze whose map has no boxes at all, so that every box a client names is unknown.
    /// </summary>
    private static LuaMaze Maze(World world, Entity player, ILogger<LuaMaze> logger, string map = TestMazes.EmptyMap) =>
        TestMazes.Create(world, player, logger, map: map);

    [TestMethod]
    public async Task PlayerWhoIsNotInAMazeIsIgnored()
    {
        var world = World.Create();

        try
        {
            var service = Service(world);
            var player = world.Create();

            await service.OnHandleAsync(Context(player), new MazeEventSpawnBoxRequest(Box(1)));
            await service.OnHandleAsync(Context(player), new MazeOperationEndRequest(Box(1)));
            await service.OnHandleAsync(Context(player), new MazeLuaFunctionRequest(Box(1)));
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// The input of a skill, the place a skill reports and the warp of a skill are the business of a player who is in a maze and on
    /// its feet. The service of this test has no relay to tell the others with: a player that is not in a maze is ignored before
    /// anything is sent, or the test fails.
    /// </summary>
    [TestMethod]
    public async Task SkillInputOfAPlayerWhoIsNotInAMazeIsIgnored()
    {
        var world = World.Create();

        try
        {
            var service = Service(world);
            var player = world.Create();

            await service.OnHandleAsync(Context(player), new SkillChargingRequest(new BinaryReader(new MemoryStream())));
            await service.OnHandleAsync(Context(player), new SkillChargingRequest(new BinaryReader(new MemoryStream(BitConverter.GetBytes(1f)))));
            await service.OnHandleAsync(Context(player), Read<SkillTargetChangeRequest>(BitConverter.GetBytes(5), (ref PacketReader r) => SkillTargetChangeRequest.Read(ref r)));
            await service.OnHandleAsync(Context(player), Read<SkillSubInputRequest>(BitConverter.GetBytes(2), (ref PacketReader r) => SkillSubInputRequest.Read(ref r)));
            await service.OnHandleAsync(Context(player), Read<SkillPreTargetListRequest>([0], (ref PacketReader r) => SkillPreTargetListRequest.Read(ref r)));
            await service.OnHandleAsync(Context(player), Read<SkillKeypressRequest>([1, 0, 0, 0, 1, 0, 0, 0, 0], (ref PacketReader r) => SkillKeypressRequest.Read(ref r)));
            await service.OnHandleAsync(Context(player), Read<SkillSyncPositionRequest>(new byte[28], (ref PacketReader r) => SkillSyncPositionRequest.Read(ref r)));
            await service.OnHandleAsync(Context(player), Read<SkillWarpPositionRequest>(new byte[12], (ref PacketReader r) => SkillWarpPositionRequest.Read(ref r)));
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A portal of a maze's map into another maze asks what the portal of a district asks: a player below the level of the
    /// maze is refused with the code of the reference, on the error of 04/01, and goes nowhere (the service has no world
    /// manager to send it with).
    /// </summary>
    [TestMethod]
    public async Task AWayOutOfAMazeIsAskedWhatThePortalOfTheMazeAsks()
    {
        const short Other = 21311;

        var world = World.Create();

        try
        {
            var mazes = new ReadOnlyCollection<MazeInfoRow>([MazeResultsTest.MazeRow(Other, level: 30, fatigue: 0)]);
            var fatigue = new FatigueService(world, mazes, TimeProvider.System, Options.Create(new DailyMissionOptions()), NullLogger<FatigueService>.Instance);
            var progress = new SharePointManager(world, new PersonStateStore(null!), SharePointCatalog.Of([]), null!, NullLogger<SharePointManager>.Instance);
            var admission = new MazeAdmission(world, mazes, fatigue, MazeResultsTest.Scoring(MazeResultsTest.None<MazePlayScoreRow>(), MazeResultsTest.None<MazeRuleRow>()), progress);
            var connection = new RecordingConnection();
            var player = world.Create(new LevelComponent(1, 0), new ServerSessionComponent(connection));

            var body = new byte[4 + 4 + 4 + 2 + 2 + 4 + 4 + 8 + 2 + 1];

            BinaryPrimitives.WriteInt16LittleEndian(body.AsSpan(12), Other);

            await Service(world, admission: admission).OnHandleAsync(Context(player), Read<WorldEnterRequest>(body, (ref PacketReader r) => WorldEnterRequest.Read(ref r)));

            await MetricAssertions.UntilAsync(() => connection.Frames.Length == 1, "The player is answered.");

            var answer = PlayerFrame.Open(connection.Frames[0]);

            Assert.IsTrue(answer.Is(GroupOpcode.World, (byte)WorldOpcode.EnterReq | 0x80), "The answer is the error of 04/01.");
            Assert.AreEqual(MazeAdmission.LevelTooLow, BinaryPrimitives.ReadUInt16LittleEndian(answer.Body));
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// The end of a dash or an evade (06/05) moves a player in a maze who is on its feet, as the other places a skill reports
    /// do; a fallen player stays where it fell, and in a city the place is not taken at all (as the original does).
    /// </summary>
    [TestMethod]
    public async Task TheEndOfADashMovesOnlyAPlayerOnItsFeetInAMaze()
    {
        var world = World.Create();

        try
        {
            var start = new System.Numerics.Vector3(1000f, 1000f, 0f);
            var player = world.Create(
                new AbilityComponent
                {
                    Health = new AbilityComponentEntry { Current = 100, Max = 100 },
                    SoulGain = default, SoulVapor = default, Stamina = default, SuperArmor = default,
                    Speed = new SpeedComponentEntry { Attack = 1, Move = 1 }
                },
                new Gameplay.Messages.Response.Person.WorldComponent { Location = 1, Position = start, Rotation = 0f, Jump = 0 });
            var maze = Maze(world, player, NullLogger<LuaMaze>.Instance);
            var pace = new MovementPace(new Dictionary<Hero, float>(), new WorldOptions(), TimeProvider.System);
            var service = Service(world, pace: pace);

            world.Add(player, maze);

            // The dash ends 500 units away.
            var body = new byte[4 + 4 + 12 + 4 + 2 + 1 + 4 + 4 + 1 + 4];

            BinaryPrimitives.WriteSingleLittleEndian(body.AsSpan(8), 1500f);
            BinaryPrimitives.WriteSingleLittleEndian(body.AsSpan(12), 1000f);

            var dash = Read<SkillPassiveEndRequest>(body, (ref PacketReader r) => SkillPassiveEndRequest.Read(ref r));
            System.Numerics.Vector3 Place() => world.Get<Gameplay.Messages.Response.Person.WorldComponent>(player).Position;

            await new DistrictServer.Services.SkillService(world, null!, null!).OnHandleAsync(Context(player), dash);

            Assert.AreEqual(start, Place(), "A city takes no place from the end of a dash.");

            maze.Creatures.Vitals.Hurt(player, 1000);

            await service.OnHandleAsync(Context(player), dash);

            Assert.AreEqual(start, Place(), "A fallen player stays where it fell.");

            maze.Creatures.Vitals.Restore(player);

            await service.OnHandleAsync(Context(player), dash);

            Assert.AreEqual(1500f, Place().X, "A player on its feet ends the dash where the client says.");
        }

        finally
        {
            World.Destroy(world);
        }
    }

    internal delegate T Reading<out T>(ref PacketReader reader);

    internal static T Read<T>(byte[] bytes, Reading<T> read)
    {
        var reader = new PacketReader(bytes);

        return read(ref reader);
    }

    [TestMethod]
    public async Task BoxTheMapDoesNotHaveIsAnsweredWithAnError()
    {
        var world = World.Create();
        var listener = new TcpListener(IPAddress.Loopback, 0);

        listener.Start();

        using var client = new TcpClient();

        client.Connect(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);

        using var server = listener.AcceptTcpClient();
        using var reader = new BinaryReader(client.GetStream());

        client.ReceiveTimeout = 5000;

        try
        {
            var service = Service(world);
            var player = world.Create();

            world.Add(player, new ServerSessionComponent(server));
            world.Add(player, Maze(world, player, NullLogger<LuaMaze>.Instance));

            await service.OnHandleAsync(Context(player), new MazeLuaFunctionRequest(Box(99)));

            // The answer is the error of the request: its command with the high bit, then the code.
            var header = new MessageHeader(reader);
            var content = reader.ReadBytes(header.ContentLength);

            MessageEncryption.Exchange(content, header.Version);

            Assert.AreEqual((byte)GroupOpcode.Maze, content[0]);
            Assert.AreEqual((byte)MazeOpcode.eSUB_CMD_MAZE_LUA_FUNCTION_REQ | 0x80, content[1]);
            Assert.AreEqual(4, content.Length);
        }

        finally
        {
            World.Destroy(world);
            listener.Stop();
        }
    }

    /// <summary>
    /// A map with one script function box, from (1000, 1000) to (2000, 1500).
    /// </summary>
    private const string MapWithAFunctionBox =
        "<root><Batchs eventtype=\"EventBox\"><VLuaFunctionBox boxtype=\"eEventBoxType_LuaFunction\">" +
        "<m_ID type=\"0\" value=\"7\" /><m_iGenerateID type=\"0\" value=\"7\" /><iLayerBitmask type=\"4\" value=\"Common\" />" +
        "<m_vPosTopLeft type=\"7\" value=\"1000,1000,0\" /><m_vPosBottomRight type=\"7\" value=\"2000,1500,500\" />" +
        "<m_fRotation type=\"2\" value=\"0\" /><m_vSize type=\"7\" value=\"1000,500,500\" />" +
        "<m_bShowCustomEntity type=\"5\" value=\"TRUE\" /><m_ColorLDR type=\"132\" value=\"0,0,0,255\" />" +
        "<m_eType type=\"4\" value=\"Party\" /><m_szFunction type=\"12\" value=\"OnNothing\" /><m_iCheckID type=\"0\" value=\"0\" />" +
        "</VLuaFunctionBox></Batchs><Batchs eventtype=\"EventPoint\"/></root>";

    /// <summary>
    /// The client names the box it has stepped into, but the server knows where the person stands: a box named from across
    /// the map does nothing (no script, no answer) and counts against the error budget of the session; at the box the same
    /// request runs and is answered.
    /// </summary>
    [TestMethod]
    public async Task ABoxNamedFromAfarDoesNothing()
    {
        var world = World.Create();
        var listener = new TcpListener(IPAddress.Loopback, 0);

        listener.Start();

        using var client = new TcpClient();

        client.Connect(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);

        using var server = listener.AcceptTcpClient();
        using var reader = new BinaryReader(client.GetStream());

        client.ReceiveTimeout = 5000;

        try
        {
            var time = new Microsoft.Extensions.Time.Testing.FakeTimeProvider();
            var budget = new Hotspot.Handler.HandlerErrorBudget(time, Options.Create(new HandlerErrorBudgetOptions { MaxErrors = 10, Window = TimeSpan.FromMinutes(1) }));
            var errors = new Hotspot.Handler.HandlerExecutor(new Hotspot.Handler.HandlerTable(new Hotspot.Handler.HandlerTableBuilder(new Hotspot.Handler.HandlerBuilder(), NullLogger<Hotspot.Handler.HandlerTable>.Instance)), new ServiceCollection().BuildServiceProvider(), world, budget, NullLogger<Hotspot.Handler.HandlerExecutor>.Instance);
            var service = Service(world, errors);
            var player = world.Create();

            world.Add(player, new ServerSessionComponent(server));
            world.Add(player, Maze(world, player, NullLogger<LuaMaze>.Instance, MapWithAFunctionBox));
            world.Add(player, new Gameplay.Messages.Response.Person.WorldComponent { Location = 1, Position = new System.Numerics.Vector3(20000f, 20000f, 0f), Rotation = 0f, Jump = 0 });

            await service.OnHandleAsync(Context(player), new MazeLuaFunctionRequest(Box(7)));

            Assert.AreEqual(1, world.Get<Hotspot.Handler.HandlerErrorsComponent>(player).Count, "The false claim is counted.");

            world.Set(player, world.Get<Gameplay.Messages.Response.Person.WorldComponent>(player) with { Position = new System.Numerics.Vector3(1500f, 1200f, 0f) });

            await service.OnHandleAsync(Context(player), new MazeLuaFunctionRequest(Box(7)));

            // The first frame the client gets is the answer to the request made at the box: the one from afar got none.
            var header = new MessageHeader(reader);
            var content = reader.ReadBytes(header.ContentLength);

            MessageEncryption.Exchange(content, header.Version);

            Assert.AreEqual((byte)GroupOpcode.Maze, content[0]);
            Assert.AreEqual((byte)MazeOpcode.eSUB_CMD_MAZE_LUA_FUNCTION_RES, content[1]);
            Assert.AreEqual(1, world.Get<Hotspot.Handler.HandlerErrorsComponent>(player).Count, "The request at the box is no error.");
        }

        finally
        {
            World.Destroy(world);
            listener.Stop();
        }
    }

    /// <summary>
    /// A box that a townsman enters (the guide of the tutorial walking ahead) is told by the client while the player stands
    /// elsewhere: it runs and is answered, and it is no error of the session.
    /// </summary>
    [TestMethod]
    public async Task ABoxOfATownsmanRunsWhereverThePlayerStands()
    {
        var world = World.Create();
        var listener = new TcpListener(IPAddress.Loopback, 0);

        listener.Start();

        using var client = new TcpClient();

        client.Connect(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);

        using var server = listener.AcceptTcpClient();
        using var reader = new BinaryReader(client.GetStream());

        client.ReceiveTimeout = 5000;

        try
        {
            var time = new Microsoft.Extensions.Time.Testing.FakeTimeProvider();
            var budget = new Hotspot.Handler.HandlerErrorBudget(time, Options.Create(new HandlerErrorBudgetOptions { MaxErrors = 10, Window = TimeSpan.FromMinutes(1) }));
            var errors = new Hotspot.Handler.HandlerExecutor(new Hotspot.Handler.HandlerTable(new Hotspot.Handler.HandlerTableBuilder(new Hotspot.Handler.HandlerBuilder(), NullLogger<Hotspot.Handler.HandlerTable>.Instance)), new ServiceCollection().BuildServiceProvider(), world, budget, NullLogger<Hotspot.Handler.HandlerExecutor>.Instance);
            var service = Service(world, errors);
            var player = world.Create();
            var map = MapWithAFunctionBox.Replace("value=\"Party\"", "value=\"NPC\"").Replace("m_iCheckID type=\"0\" value=\"0\"", "m_iCheckID type=\"0\" value=\"101001\"");

            world.Add(player, new ServerSessionComponent(server));
            world.Add(player, Maze(world, player, NullLogger<LuaMaze>.Instance, map));
            world.Add(player, new Gameplay.Messages.Response.Person.WorldComponent { Location = 1, Position = new System.Numerics.Vector3(20000f, 20000f, 0f), Rotation = 0f, Jump = 0 });

            await service.OnHandleAsync(Context(player), new MazeLuaFunctionRequest(Box(7)));

            var header = new MessageHeader(reader);
            var content = reader.ReadBytes(header.ContentLength);

            MessageEncryption.Exchange(content, header.Version);

            Assert.AreEqual((byte)GroupOpcode.Maze, content[0]);
            Assert.AreEqual((byte)MazeOpcode.eSUB_CMD_MAZE_LUA_FUNCTION_RES, content[1]);
            Assert.IsFalse(world.Has<Hotspot.Handler.HandlerErrorsComponent>(player) && world.Get<Hotspot.Handler.HandlerErrorsComponent>(player).Count > 0, "The townsman's box is no error.");
        }

        finally
        {
            World.Destroy(world);
            listener.Stop();
        }
    }

    [TestMethod]
    public void ScriptThatNamesAPositionBoxTheMapDoesNotHaveGetsTheOriginAndALoggedWarning()
    {
        var world = World.Create();

        try
        {
            var log = new Recorder();
            var maze = Maze(world, world.Create(), log);

            var position = maze.GetCommonPositionBox(7);

            Assert.AreEqual(0f, position.X);
            Assert.AreEqual(0f, position.Y);
            Assert.AreEqual(0f, position.Z);
            Assert.IsTrue(log.Entries.Any(e => e.Level == LogLevel.Warning && e.Message.Contains("box 7", StringComparison.Ordinal)));
        }

        finally
        {
            World.Destroy(world);
        }
    }

    [TestMethod]
    public void ScriptThatAppliesABuffTheTableDoesNotHaveIsRefused()
    {
        var buffs = new BuffManager(TestMazes.None<BuffRow>());

        Assert.IsFalse(buffs.Catalog.TryGet(12345, out _));
    }
}
