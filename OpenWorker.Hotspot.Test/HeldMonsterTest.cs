using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Gameplay.Buffs;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Lua;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;
using OpenWorker.Navigation;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A monster a stun holds stands where the stun found it: a walk it was on is cut there and the clients are told that place,
/// nothing starts a walk while the stun lasts, and the monster walks again when it is over.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class HeldMonsterTest
{
    private const int Sector = 1;
    private const int Monster = 1000;
    private const ushort Stun = 900;
    private const float StunSeconds = 1f;
    private static readonly float Step = (float)SimulationClock.Period.TotalSeconds;

    private sealed class Arena(World world, LuaMaze maze, Entity monster, MonsterBody body, RecordingConnection connection, FakeTimeProvider time, SimulationClock clock) : IDisposable
    {
        public World World { get; } = world;
        public LuaMaze Maze { get; } = maze;
        public Entity Monster { get; } = monster;
        public MonsterBody Body { get; } = body;
        public RecordingConnection Connection { get; } = connection;
        public Vector3 Position => World.Get<WorldComponent>(Monster).Position;
        public Vector3 Far => Position + new Vector3(2000f, 0f, 0f);

        public void Advance(int steps)
        {
            for (var index = 0; index < steps; index++)
            {
                time.Advance(SimulationClock.Period);
                clock.Tick();
                Maze.Buffs.Step(Step);
                Maze.Creatures.Step(SimulationClock.Period);
            }
        }

        public void Dispose()
        {
            World.Destroy(World);
        }
    }

    private static Arena Open(DirectoryInfo folder)
    {
        // A monster that looks for nobody: only the test makes it walk.
        File.WriteAllText(Path.Join(folder.FullName, "Ai_Still.lua"),
            "function Initial_Normal(self, monster)\n  local ai = monster:GetAi()\n  ai:SetAiCheckTime(0.1)\n  ai:SetFindEnemyInfo(0, 1.0, 100.0)\nend\n");

        var world = World.Create();
        var time = new FakeTimeProvider();
        var clock = TestMazes.Clock(time);
        var monsters = new ReadOnlyCollection<MonsterRow>([TestRows.Build<MonsterRow>(("Id", Monster), ("AiScript", "Ai_Still"))]);
        var skills = TestMazes.None<SkillRow>();
        var combat = new CombatTables(monsters, TestMazes.None<StatusRow>(), skills, TestMazes.None<CharacterInfoRow>());
        var services = new CreatureServices(new AiScriptLoader(NullLogger<AiScriptLoader>.Instance, folder.FullName), new MonsterBehaviorTables(monsters, skills),
            new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance), clock, new NavMeshProvider());
        var batch = TestMazes.Map(PartyMazeTest.MapWithMonster(Monster));
        var buffs = new ReadOnlyCollection<BuffRow>(
        [
            TestRows.Build<BuffRow>(("Id", Stun), ("AllowedPlaces", 10), ("Kind", (byte)BuffKind.Debuff), ("Effect", (ushort)BuffEffect.Stun), ("Group", Stun),
                ("SuccessRate", 10000), ("Grade", 1), ("DurationMilliseconds", (int)(StunSeconds * 1000)), ("OverlapCount", 1))
        ]);
        var connection = new RecordingConnection();
        var player = world.Create(
            new ActorComponent(100, ActorType.User),
            new ServerSessionComponent(connection),
            new WorldComponent { Location = 0, Position = Vector3.Zero, Rotation = 0f, Jump = 0 },
            new LevelComponent(10, 0),
            new PersonInfoComponent { Name = "Haru", Hero = Hero.Haru },
            new AbilityComponent
            {
                Health = new AbilityComponentEntry { Current = 100, Max = 100 },
                SoulGain = default,
                SoulVapor = default,
                Stamina = default,
                SuperArmor = default,
                Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f }
            });
        var maze = TestMazes.Create(world, player, batch: batch, creatures: new LuaCreatureManager(world, batch, combat, services), buffs: new BuffManager(buffs),
            tables: combat, clock: clock);

        maze.SpawnSector(Sector);

        var monster = maze.Creatures.FindMonsters((_, _) => true).Single();

        world.Set(monster, world.Get<CreatureComponent>(monster) with { Health = 1000 });

        return new Arena(world, maze, monster, maze.Creatures.BodyOf(monster)!, connection, time, clock);
    }

    private static async Task WithArenaAsync(Func<Arena, Task> test)
    {
        var folder = Directory.CreateTempSubdirectory();

        try
        {
            using var arena = Open(folder);

            await test(arena);
        }

        finally
        {
            folder.Delete(true);
        }
    }

    private static PlayerFrame[] Stops(Arena arena) => arena.Connection.Frames.Select(PlayerFrame.Open)
        .Where(e => e.Is(GroupOpcode.Move, (byte)MoveOpcode.StopBt) && e.Actor == ActorOf(arena)).ToArray();

    private static int ActorOf(Arena arena) => arena.World.Get<ActorComponent>(arena.Monster).Identifier;

    /// <summary>
    /// A stun that comes while the monster walks cuts the walk where the monster is, tells the clients that place, and the
    /// monster stays there.
    /// </summary>
    [TestMethod]
    public Task AWalkInProgressIsCutWhereTheHoldFindsTheMonster() => WithArenaAsync(async arena =>
    {
        var start = arena.Position;

        Assert.IsTrue(arena.Body.MoveTo(arena.Far, run: true));

        arena.Advance(20);

        var held = arena.Position;

        Assert.IsGreaterThan(10f, Vector3.Distance(start, held), "The monster walked before the stun.");

        var stopsBefore = Stops(arena).Length;

        Assert.IsTrue(arena.Maze.Buffs.Give(arena.Monster, Stun, arena.Monster));
        Assert.IsFalse(arena.Body.IsMoving, "The walk is cut.");

        await MetricAssertions.UntilAsync(() => Stops(arena).Length > stopsBefore, "The clients are told the monster stops.");

        Assert.AreEqual(held.X, Stops(arena)[^1].X, 0.01f, "They are told the place the stun found it at.");

        arena.Advance(10);

        Assert.AreEqual(held, arena.Position, "A held monster does not move.");
    });

    /// <summary>
    /// While the stun lasts nothing starts a walk or an attack: the mind and anything else that asks are refused.
    /// </summary>
    [TestMethod]
    public Task NoWalkStartsWhileTheMonsterIsHeld() => WithArenaAsync(arena =>
    {
        var held = arena.Position;

        Assert.IsTrue(arena.Maze.Buffs.Give(arena.Monster, Stun, arena.Monster));

        Assert.IsFalse(arena.Body.CanMove);
        Assert.IsFalse(arena.Body.CanAttack);
        Assert.IsFalse(arena.Body.MoveTo(arena.Far, run: true), "A walk is refused.");
        Assert.IsFalse(arena.Body.UseSkill(new AiSkill(1, 0f, 1000f, 0f, 0, 0f, 360f, true, false), new AiTarget(0, arena.Far, 0f, 0f, 1, 1, false)), "An attack is refused.");

        arena.Advance(10);

        Assert.IsFalse(arena.Body.IsMoving);
        Assert.AreEqual(held, arena.Position);

        return Task.CompletedTask;
    });

    /// <summary>
    /// When the stun is over the monster can walk again.
    /// </summary>
    [TestMethod]
    public Task TheWalkCanStartAgainWhenTheHoldEnds() => WithArenaAsync(arena =>
    {
        var held = arena.Position;

        Assert.IsTrue(arena.Maze.Buffs.Give(arena.Monster, Stun, arena.Monster));

        arena.Advance((int)(StunSeconds / Step) + 2);

        Assert.IsFalse(arena.Maze.Buffs.IsHeld(arena.Monster), "The stun is over.");
        Assert.IsTrue(arena.Body.MoveTo(arena.Far, run: true));

        arena.Advance(10);

        Assert.IsGreaterThan(10f, Vector3.Distance(held, arena.Position), "The monster walks.");

        return Task.CompletedTask;
    });
}
