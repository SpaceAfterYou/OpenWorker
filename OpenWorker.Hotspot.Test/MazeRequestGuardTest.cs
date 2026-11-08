using System.Collections.ObjectModel;
using Arch.Core;
using Lua;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Batch;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Maze.Requests;
using OpenWorker.Lua;
using OpenWorker.Lua.Managers;
using OpenWorker.MazeServer.Services;
using OpenWorker.UpdateContent.Res.Rows;

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

    private static ReadOnlyCollection<T> None<T>() => new([]);

    private static BinaryReader Box(int box) => new(new MemoryStream(BitConverter.GetBytes(box)));

    private static ServiceHandleContext Context(Entity player) => new(player, CancellationToken.None);

    private static MazeService Service(World world)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Gate:Id"] = "1" })
            .Build();

        // The handlers under test never reach Redis, the batches or the other server.
        return new MazeService(
            world,
            null!,
            null!,
            null!,
            configuration,
            new QuestManager(world, None<QuestEpisodeRow>(), None<QuestConditionRow>()),
            NullLogger<MazeService>.Instance);
    }

    /// <summary>
    /// A maze whose map has no boxes at all, so that every box a client names is unknown.
    /// </summary>
    private static LuaMaze Maze(World world, Entity player, ILogger<LuaMaze> logger)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.vbatch");

        File.WriteAllText(path, "<root><Batchs eventtype=\"EventBox\"/><Batchs eventtype=\"EventPoint\"/></root>");

        try
        {
            var batch = VBatchFile.CreateFromPath(path);
            var tables = new CombatTables(None<MonsterRow>(), None<StatusRow>(), None<SkillRow>(), None<CharacterInfoRow>());
            var quests = new QuestManager(world, None<QuestEpisodeRow>(), None<QuestConditionRow>());

            return new LuaMaze(
                LuaState.Create(),
                world,
                player,
                new LuaCreatureManager(world, batch, tables),
                new BuffManager(world, None<BuffRow>()),
                quests,
                batch,
                None<InteractionObjectRow>(),
                new SkillActionProvider(None<SkillRow>(), NullLogger<SkillActionProvider>.Instance),
                tables,
                logger);
        }

        finally
        {
            File.Delete(path);
        }
    }

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
    public async Task BoxesTheMapDoesNotHaveAreIgnored()
    {
        var world = World.Create();

        try
        {
            var service = Service(world);

            // The player has no session on purpose: an answer to a request that is ignored would throw.
            var player = world.Create();

            world.Add(player, Maze(world, player, NullLogger<LuaMaze>.Instance));

            await service.OnHandleAsync(Context(player), new MazeEventSpawnBoxRequest(Box(99)));
            await service.OnHandleAsync(Context(player), new MazeLuaFunctionRequest(Box(99)));
        }

        finally
        {
            World.Destroy(world);
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
            var buffs = new BuffManager(world, None<BuffRow>());

            Assert.IsFalse(buffs.Apply(world.Create(), default, 12345));
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
