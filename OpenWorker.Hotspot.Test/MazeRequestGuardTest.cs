using System.Net;
using System.Net.Sockets;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.SoulWorker.Network;
using System.Collections.ObjectModel;
using Arch.Core;
using Lua;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenWorker.Batch;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Extensions;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Maze.Requests;
using OpenWorker.Lua;
using OpenWorker.Lua.Managers;
using OpenWorker.MazeServer.Services;
using OpenWorker.UpdateContent.Res.Rows;
using OpenWorker.Gameplay.Modules.Persistence;

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

    private static ServiceHandleContext Context(Entity player) => new(player, CancellationToken.None);

    private static MazeService Service(World world)
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
            NullLogger<MazeService>.Instance);
    }

    /// <summary>
    /// A maze whose map has no boxes at all, so that every box a client names is unknown.
    /// </summary>
    private static LuaMaze Maze(World world, Entity player, ILogger<LuaMaze> logger) => TestMazes.Create(world, player, logger);

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
        var world = World.Create();

        try
        {
            var buffs = new BuffManager(world, TestMazes.None<BuffRow>());

            Assert.IsFalse(buffs.Apply(world.Create(), default, 12345));
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
