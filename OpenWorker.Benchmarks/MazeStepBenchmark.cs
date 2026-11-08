using System.Collections.ObjectModel;
using Arch.Core;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Batch;
using OpenWorker.Batch.Actions;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Batch.Enums;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot;
using OpenWorker.Lua;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;
using OpenWorker.Navigation;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Benchmarks;

/// <summary>
/// One step of the simulation of a maze with <see cref="Monsters"/> monsters that think and <see cref="Timers"/> timers
/// of its script waiting: what the world loop does every 50 ms for one maze. The monsters stand round a player who
/// cannot die, so they see him, walk and fight. It reads the client data: OPENWORKER_TEST_RES (tables),
/// OPENWORKER_TEST_AI (monster scripts) and OPENWORKER_BENCH_BATCH (a map, M04_RSQUARE_EP_01.vbatch by default from the
/// stand data).
/// </summary>
[MemoryDiagnoser]
#pragma warning disable CA1001 // BenchmarkDotNet derives from the class and calls the cleanup, which disposes the clock.
public class MazeStepBenchmark
#pragma warning restore CA1001
{
    private sealed class SilentTimers : IMazeTimerHost
    {
        public ValueTask CallAsync(string function, int first, int second, int third) => ValueTask.CompletedTask;

        public ValueTask CompleteAsync(int timer) => ValueTask.CompletedTask;

        public void ShowTimer(int value, float time, float next)
        {
        }
    }

    [Params(10, 100)]
    public int Monsters { get; set; }

    [Params(0, 100)]
    public int Timers { get; set; }

    private World _world = null!;
    private SimulationClock _clock = null!;
    private LuaCreatureManager _creatures = null!;
    private MazeTimers _timers = null!;
    private ServerSessionComponent _session;

    [GlobalSetup]
    public void Setup()
    {
        var tables = Environment.GetEnvironmentVariable("OPENWORKER_TEST_RES") ?? throw new InvalidOperationException("OPENWORKER_TEST_RES is not set.");
        var scripts = Environment.GetEnvironmentVariable("OPENWORKER_TEST_AI") ?? throw new InvalidOperationException("OPENWORKER_TEST_AI is not set.");
        var map = Environment.GetEnvironmentVariable("OPENWORKER_BENCH_BATCH")
            ?? Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache/openworker/stand/client_batch/M04_RSQUARE_EP_01.vbatch");

        var monsters = Load<MonsterRow>(tables, "tb_Monster");
        var skills = Load<SkillRow>(tables, "tb_Skill");
        var combat = new CombatTables(monsters, Load<StatusRow>(tables, "tb_Status"), skills, Load<CharacterInfoRow>(tables, "tb_Character_Info"));
        var behaviors = new MonsterBehaviorTables(monsters, skills);
        var loader = new AiScriptLoader(NullLogger<AiScriptLoader>.Instance, scripts);
        var batch = VBatchFile.CreateFromPath(map);

        _world = World.Create();
        _clock = new SimulationClock(new WorldLoop(NullLogger<WorldLoop>.Instance), NullLogger<SimulationClock>.Instance);

        var services = new CreatureServices(loader, behaviors, new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance), _clock, new NavMeshProvider());

        // A box that shows its monsters at once, all of which think.
        var box = batch.EventBox.MonsterSpawns.First(e =>
            e.CreationCondition == CreationConditionType.Immediate &&
            e.MonsterList.Any(m => m.Id != 0) &&
            e.MonsterList.Where(m => m.Id != 0).All(m => m.Type == MonsterSpawnType.Monster && Thinks(services, m.Id)));

        _session = new ServerSessionComponent(Stream.Null);
        _creatures = new LuaCreatureManager(_world, batch, combat, services, NullLogger.Instance);

        var player = Player(box.GetPosition());

        _creatures.Attach(player);

        while (_creatures.FindMonsters((_, _) => true).Length < Monsters)
        {
            _creatures.Create(player, box.Id);
        }

        _timers = new MazeTimers(new SilentTimers());

        for (var timer = 0; timer < Timers; timer++)
        {
            // Long enough that none runs out while it is measured.
            _timers.AddPlain(timer, 1_000_000f);
        }

        // The monsters come to life and the timers start before the measured steps.
        for (var step = 0; step < 20; step++)
        {
            Step();
        }
    }

    private static bool Thinks(CreatureServices services, int monster)
    {
        return services.Behaviors.Get(monster) is { } behavior && !ReferenceEquals(services.Scripts.Get(behavior.AiScript), AiProfile.Empty);
    }

    private Entity Player(System.Numerics.Vector3 position)
    {
        var player = _world.Create();
        var life = new AbilityComponentEntry { Current = int.MaxValue / 2, Max = int.MaxValue / 2 };

        _world.Add(player, new ActorComponent { Identifier = 1_000_000, Type = ActorType.User });
        _world.Add(player, new WorldComponent { Location = 0, Position = position, Rotation = 0f, Jump = 0 });
        _world.Add(player, new PersonInfoComponent { Name = "bench", Hero = Hero.Haru });
        _world.Add(player, new AbilityComponent
        {
            Health = life,
            SoulGain = life,
            SoulVapor = life,
            Stamina = life,
            SuperArmor = life,
            Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f }
        });
        _world.Add(player, _session);

        return player;
    }

    /// <summary>
    /// What the maze server does in 50 ms for this maze: the clock steps the monsters, and the loop of the timers of the
    /// script counts them down.
    /// </summary>
    [Benchmark]
    public void Step()
    {
        _clock.Tick();

        var counted = _timers.UpdateAsync(0.05f);

        // Every timer waits for a long time and nothing is called, so the round is over at once.
        if (!counted.IsCompletedSuccessfully)
        {
            throw new InvalidOperationException("A round of the timers waited.");
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _creatures.Close();
        _session.Disconnect();

        World.Destroy(_world);

        _clock.Dispose();
    }

    private static ReadOnlyCollection<T> Load<T>(string folder, string table) where T : struct
    {
        using var reader = new BinaryReader(File.OpenRead(Path.Join(folder, $"{table}.res")));

        var rows = new T[reader.ReadInt32()];

        for (var index = 0; index < rows.Length; index++)
        {
            rows[index] = (T)Activator.CreateInstance(typeof(T), reader)!;
        }

        return new ReadOnlyCollection<T>(rows);
    }
}
