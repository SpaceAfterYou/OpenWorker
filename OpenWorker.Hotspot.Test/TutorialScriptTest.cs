using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using Lua;
using Lua.Standard;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Batch;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Gameplay.Modules.Quests.Components;
using OpenWorker.Lua;
using OpenWorker.Lua.Enums;
using OpenWorker.MazeServer.Services;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The tutorial scripts hear what they wait for: the quests of the player, the skills it uses, the townsfolk that reach their
/// points. The tutorial scripts of the client run to their end against the maze.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class TutorialScriptTest
{
    private const short Tutorial = 21112;

    private static Entity Person(World world, int identifier) => world.Create(
        new ActorComponent { Identifier = identifier, Type = ActorType.User },
        new PersonInfoComponent { Name = "Tester", Hero = Hero.Haru });

    private static Entity Player(World world)
    {
        var player = Person(world, 42);

        world.Add(player, new WorldComponent { Location = Tutorial, Position = Vector3.Zero, Rotation = 0f, Jump = 0 });
        world.Add(player, new ServerSessionComponent(Stream.Null));

        return player;
    }

    private static void Tick(FakeTimeProvider time, SimulationClock clock, int ticks)
    {
        for (var tick = 0; tick < ticks; tick++)
        {
            time.Advance(SimulationClock.Period);
            clock.Tick();
        }
    }

    [TestMethod]
    public async Task ScriptHearsTheQuestsOfThePlayerInTheOrderTheyChange()
    {
        var world = World.Create();

        try
        {
            var time = new FakeTimeProvider();
            var clock = TestMazes.Clock(time);
            var state = LuaState.Create();

            await state.DoStringAsync("""
                heard = {}
                function OnUpdateQuest(user, kind, id, maze) heard[#heard + 1] = kind * 10000000 + id end
                """);

            var quest = new QuestDefinition(100007, 4, QuestContents.Main, 1, 0, [], true, true, [], [], QuestRewardDefinition.None, [1000071, 1000072]);
            var catalog = QuestCatalog.From([quest], []);
            var book = new QuestBook();
            var player = Player(world);

            world.Add(player, new QuestComponent { Person = 42, Book = book });

            var maze = TestMazes.Create(world, player, clock: clock, state: state);

            world.Add(player, maze);
            maze.FollowQuests(catalog);
            Assert.IsTrue(maze.TryStart());

            book.Episodes.Add(new QuestEpisodeState { Episode = 100007 });
            Tick(time, clock, 1);

            book.Episodes[0].Done = 1;
            Tick(time, clock, 1);

            // The last condition and the end of the episode come in one step.
            book.Episodes.Clear();
            book.SetFinished(quest.CompleteBit, true);
            Tick(time, clock, 1);

            var heard = state.Environment["heard"].Read<LuaTable>();
            var list = Enumerable.Range(1, heard.ArrayLength).Select(i => (long)heard[i].Read<double>()).ToArray();

            CollectionAssert.AreEqual(new long[]
            {
                (long)QuestUpdateType.AcceptQuest * 10000000 + 100007,
                (long)QuestUpdateType.CompleteCondition * 10000000 + 1000071,
                (long)QuestUpdateType.CompleteCondition * 10000000 + 1000072,
                (long)QuestUpdateType.CompleteQuest * 10000000 + 100007
            }, list);

            maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// The maze scripts and the maps of the stand: the scripts lie next to the AI scripts (<c>OPENWORKER_TEST_AI</c>), the
    /// maps in <c>client_batch</c> next to them.
    /// </summary>
    private static (string Scripts, string Maps) TutorialData()
    {
        var ai = Environment.GetEnvironmentVariable("OPENWORKER_TEST_AI");
        var scripts = string.IsNullOrEmpty(ai) ? null : Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(ai));
        var maps = scripts is null ? null : Path.Join(Path.GetDirectoryName(scripts), "client_batch");

        if (scripts is null || maps is null || !File.Exists(Path.Join(scripts, "T02_TUTORIAL.lua")) || !File.Exists(Path.Join(maps, "T02_TUTORIAL.vbatch")))
        {
            Assert.Inconclusive("OPENWORKER_TEST_AI is not set to the AI folder of a stand with the tutorial scripts and maps.");
        }

        return (scripts, maps);
    }

    private static async Task<(LuaMaze Maze, LuaState State, Entity Player)> TutorialAsync(World world, SimulationClock clock, string name)
    {
        var (scripts, maps) = TutorialData();
        var state = LuaState.Create();

        state.OpenStandardLibraries();
        LuaCompatibility.Apply(state);

        Assert.IsTrue(await new MazeScripts(scripts).RunAsync(state, name, CancellationToken.None));

        // What the test reads of the script: the points the townsman reached, the timers and the skills it heard of.
        await state.DoStringAsync("""
            reached, timers, skills = {}, {}, 0
            local way, timer, skill = OnNpcWayPoint, OnTimerComplete, OnActionSkill
            if way then function OnNpcWayPoint(npc, point, maze) reached[#reached + 1] = point; way(npc, point, maze) end end
            if timer then function OnTimerComplete(id, maze) timers[#timers + 1] = id; timer(id, maze) end end
            if skill then function OnActionSkill(id, maze) skills = skills + 1; skill(id, maze) end end
            """);

        var player = Player(world);
        var quests = TestMazes.Quests(world);
        var reports = new MazeReporter(null!, null!, null!, null!, quests, NullLogger<MazeReporter>.Instance);
        var maze = TestMazes.Create(world, player, clock: clock, state: state, batch: VBatchFile.CreateFromPath(Path.Join(maps, name + ".vbatch")),
            quests: quests, reports: reports);

        world.Add(player, maze);
        Assert.IsTrue(maze.TryStart());

        maze.SpawnSector(10001);

        await state.OnEnterPlayerAsync(world.Get<ActorComponent>(player), maze);

        return (maze, state, player);
    }

    private static double[] Numbers(LuaState state, string name)
    {
        var table = state.Environment[name].Read<LuaTable>();

        return Enumerable.Range(1, table.ArrayLength).Select(i => table[i].Read<double>()).ToArray();
    }

    [TestMethod]
    public async Task FirstTutorialScriptRunsToTheEndOfItsFight()
    {
        var world = World.Create();

        try
        {
            var time = new FakeTimeProvider();
            var clock = TestMazes.Clock(time);
            var (maze, state, player) = await TutorialAsync(world, clock, "T01_TUTORIAL");

            // The player walks into the box that starts the fight; the monsters come after the cutscene.
            await state.OnExecuteLuaFunction("LuaFunction", 1005, maze);
            Tick(time, clock, 14 * 20);

            CollectionAssert.Contains(Numbers(state, "timers"), 78d);
            Assert.AreEqual(4, maze.Creatures.FindMonsters((_, _) => true).Length);

            // Four of them fall, and eight more come.
            for (var box = 20001; box <= 20004; box++)
            {
                await state.OnDie(30101021, box, maze);
            }

            Tick(time, clock, 2);

            Assert.AreEqual(12, maze.Creatures.FindMonsters((_, _) => true).Length);
            Assert.IsTrue(world.Has<LuaMaze>(player), "the script has not failed");

            maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    [TestMethod]
    public async Task SecondTutorialScriptRunsToItsLastSector()
    {
        var world = World.Create();

        try
        {
            var time = new FakeTimeProvider();
            var clock = TestMazes.Clock(time);
            var (maze, state, player) = await TutorialAsync(world, clock, "T02_TUTORIAL");
            var actor = (int)world.Get<ActorComponent>(player).Identifier;

            // The guide takes the player through the first sector once the player takes her quest.
            await state.OnUpdateQuest(actor, QuestUpdateType.AcceptQuest, 100005, maze);
            Tick(time, clock, 5 * 60 * 20);

            CollectionAssert.AreEqual(new[] { 4101d, 4102d, 4103d, 4104d, 4105d, 4106d, 4107d, 4109d, 4110d, 4111d, 4115d }, Numbers(state, "reached"));

            // A skill the tutorial asks for.
            maze.StartSkill(12001211, default);
            Assert.AreEqual(1d, state.Environment["skills"].Read<double>());

            // Each sector ends by what the client reports: a quest condition, an object, a box, two talks.
            await state.OnUpdateQuest(actor, QuestUpdateType.CompleteCondition, 1000071, maze);
            await state.OnInteractionObject(7011, maze);
            await state.OnExecuteLuaFunction("LuaFunction", 10061, maze);
            await state.CheckConditionAsync(0, 911202, maze);
            await state.CheckConditionAsync(0, 921604, maze);
            Tick(time, clock, 20);

            foreach (var sector in new[] { 10001, 10002, 10006, 10010, 10011 })
            {
                Assert.IsTrue(maze.Progress.IsSectorCleared(sector), $"sector {sector}");
            }

            Assert.IsTrue(world.Has<LuaMaze>(player), "the script has not failed");

            maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
