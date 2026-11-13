using System.Buffers.Binary;
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
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;
using OpenWorker.Navigation;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A skill carries the monster that plays it as far as its animations do, on the ground, and the monster deals its blows and
/// walks on from where the skill has taken it.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class SkillMovementTest
{
    private const int Monster = 1000;
    private const int Person = 100;

    /// <summary>
    /// The dash of the imp puppet: 1.73 s of animation, a tenth of a second of it played ten times slower, that carry it 684 units
    /// forward, with blows of radius 120.
    /// </summary>
    private const int Dash = 1301030011;

    private const string PuppetFile = "MOB_0085_ImpPuppet";
    private const float DashWay = 684f;

    /// <summary>
    /// The ground ends along a line of Y: there is ground only on its far side.
    /// </summary>
    private sealed class GroundEndingAt(float edge) : INavigation
    {
        public bool TryGetFloor(Vector3 position, out float height)
        {
            height = position.Z;

            return position.Y >= edge;
        }

        public bool TryFindPath(Vector3 from, Vector3 target, float radius, List<Vector3> path) => false;

        public bool CanSee(Vector3 from, Vector3 target) => true;

        public Vector3 Clamp(Vector3 from, Vector3 target, float radius, out bool reached)
        {
            reached = target.Y >= edge + radius;

            return reached ? target : target with { Y = Math.Min(from.Y, edge + radius) };
        }
    }

    private sealed class Arena(World world, LuaMaze maze, Entity player, MonsterBody body, RecordingConnection connection, FakeTimeProvider time, SimulationClock clock)
    {
        public LuaMaze Maze { get; } = maze;
        public MonsterBody Body { get; } = body;
        public Vector3 Start { get; } = body.Position;

        public void Advance(float seconds)
        {
            for (var step = 0; step < seconds / SimulationClock.Period.TotalSeconds; step++)
            {
                time.Advance(SimulationClock.Period);
                clock.Tick();
                Maze.Creatures.Step(SimulationClock.Period);
            }
        }

        /// <summary>
        /// Puts the player ahead of the monster, at the height of the monster, and has the monster dash at it.
        /// </summary>
        public void DashAt(float ahead)
        {
            var place = Start + new Vector3(0f, -ahead, 0f);

            world.Set(player, world.Get<WorldComponent>(player) with { Position = place });

            Assert.IsTrue(Body.UseSkill(Body.GetSkill(0)!, new AiTarget(Person, place, PlayerVitals.BodyRadius, 0f, 1, 1, false)));
        }

        public PlayerFrame[] Frames(MoveOpcode command) =>
            connection.Frames.Select(PlayerFrame.Open).Where(e => e.Is(GroupOpcode.Move, (byte)command)).ToArray();

        public int Hits() =>
            connection.Frames.Select(PlayerFrame.Open).Count(e => e.Is(GroupOpcode.Skill, (byte)SkillOpcode.ActionSkillBt));
    }

    private static async Task WithPuppetAsync(Func<Arena, Task> test)
    {
        var tables = Environment.GetEnvironmentVariable("OPENWORKER_TEST_RES");
        var actions = Environment.GetEnvironmentVariable("OPENWORKER_TEST_ACTION");

        if (string.IsNullOrEmpty(tables) || string.IsNullOrEmpty(actions))
        {
            Assert.Inconclusive("OPENWORKER_TEST_RES and OPENWORKER_TEST_ACTION are not set.");
        }

        using var reader = new BinaryReader(File.OpenRead(Path.Join(tables, "tb_Skill.res")));
        var rows = new SkillRow[reader.ReadInt32()];

        for (var index = 0; index < rows.Length; index++)
        {
            rows[index] = new SkillRow(reader);
        }

        var skills = new ReadOnlyCollection<SkillRow>([rows.First(e => e.Id == Dash)]);
        var folder = Directory.CreateTempSubdirectory();
        var world = World.Create();

        try
        {
            File.WriteAllText(Path.Join(folder.FullName, "Ai_Still.lua"), "function Initial_Normal(self, monster)\n  local ai = monster:GetAi()\n  ai:SetAiCheckTime(0.1)\nend\n");

            var time = new FakeTimeProvider();
            var clock = TestMazes.Clock(time);
            var monsters = new ReadOnlyCollection<MonsterRow>([TestRows.Build<MonsterRow>(("Id", Monster), ("AiScript", "Ai_Still"), ("ActionFile", PuppetFile), ("Skill1", Dash))]);
            var combat = new CombatTables(monsters, TestMazes.None<StatusRow>(), skills, TestMazes.None<CharacterInfoRow>());
            var services = new CreatureServices(new AiScriptLoader(NullLogger<AiScriptLoader>.Instance, folder.FullName), new MonsterBehaviorTables(monsters, skills),
                new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance) { Folder = actions! }, clock, new NavMeshProvider());
            var batch = TestMazes.Map(PartyMazeTest.MapWithMonster(Monster));
            var connection = new RecordingConnection();
            var player = world.Create(
                new ActorComponent(Person, ActorType.User),
                new ServerSessionComponent(connection),
                new WorldComponent { Location = 0, Position = new Vector3(0f, 0f, -10000f), Rotation = 0f, Jump = 0 },
                new LevelComponent(10, 0),
                new PersonInfoComponent { Name = "Haru", Hero = Hero.Haru },
                new AbilityComponent
                {
                    Health = new AbilityComponentEntry { Current = 100000, Max = 100000 },
                    SoulGain = default,
                    SoulVapor = default,
                    Stamina = default,
                    SuperArmor = default,
                    Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f }
                });
            var maze = TestMazes.Create(world, player, batch: batch, creatures: new LuaCreatureManager(world, batch, combat, services), tables: combat, clock: clock);

            maze.SpawnSector(1);

            var monster = maze.Creatures.FindMonsters((_, _) => true).Single();

            world.Set(monster, world.Get<CreatureComponent>(monster) with { Health = 100000 });

            await test(new Arena(world, maze, player, maze.Creatures.BodyOf(monster)!, connection, time, clock));
        }

        finally
        {
            World.Destroy(world);
            folder.Delete(true);
        }
    }

    private static Vector2 PlaceOf(PlayerFrame frame, int offset) =>
        new(BinaryPrimitives.ReadSingleLittleEndian(frame.Body.AsSpan(offset)), BinaryPrimitives.ReadSingleLittleEndian(frame.Body.AsSpan(offset + 4)));

    /// <summary>
    /// On open ground the dash carries the monster the whole way of its animation, straight ahead.
    /// </summary>
    [TestMethod]
    public Task ADashCarriesTheMonsterTheWayOfItsAnimation() => WithPuppetAsync(arena =>
    {
        arena.DashAt(5000f);
        arena.Advance(3.5f);

        Assert.AreEqual(arena.Start.X, arena.Body.Position.X, 1f);
        Assert.AreEqual(arena.Start.Y - DashWay, arena.Body.Position.Y, 2f);

        return Task.CompletedTask;
    });

    /// <summary>
    /// Where the ground ends the dash ends, a radius before the edge, and the clients are told where the monster stands.
    /// </summary>
    [TestMethod]
    public Task ADashEndsARadiusBeforeTheEdgeOfTheGround() => WithPuppetAsync(arena =>
    {
        var edge = arena.Start.Y - 300f;
        var stop = edge + Math.Min(arena.Body.Radius, 100f);

        arena.Maze.Creatures.Navigation = new GroundEndingAt(edge);
        arena.DashAt(5000f);
        arena.Advance(3.5f);

        Assert.AreEqual(stop, arena.Body.Position.Y, 1f);

        var told = arena.Frames(MoveOpcode.IgnoreMotionDelta);

        Assert.HasCount(1, told, "The cut is told once.");
        Assert.AreEqual(stop, PlaceOf(told[0], 4).Y, 1f);

        return Task.CompletedTask;
    });

    /// <summary>
    /// A blow of the dash is judged where the dash has carried the monster by its moment: a player 560 ahead, far out of reach of
    /// the place the dash began at, is hit.
    /// </summary>
    [TestMethod]
    public Task ABlowOfTheDashIsJudgedFromWhereTheDashHasCarriedTheMonster() => WithPuppetAsync(async arena =>
    {
        arena.DashAt(560f);
        arena.Advance(3.5f);

        await MetricAssertions.UntilAsync(() => arena.Hits() > 0, "The player ahead is hit.");
    });

    /// <summary>
    /// A monster runs no further into the player it fights: it stops before it and the clients are told where.
    /// </summary>
    [TestMethod]
    public Task ADashStopsBeforeThePlayerTheMonsterFights() => WithPuppetAsync(arena =>
    {
        arena.Body.Machine.OnStruck(Person);
        arena.DashAt(400f);

        // Before the end of the skill, after which the monster may walk to the player.
        arena.Advance(2.5f);

        var reach = Math.Min(arena.Body.Radius, 100f) + PlayerVitals.BodyRadius + 10f;
        var player = arena.Start.Y - 400f;

        Assert.IsGreaterThan(reach, arena.Body.Position.Y - player);
        Assert.IsLessThan(reach + 40f, arena.Body.Position.Y - player);
        Assert.HasCount(1, arena.Frames(MoveOpcode.IgnoreMotionDelta));

        return Task.CompletedTask;
    });

    /// <summary>
    /// A flinch ends the skill and its move where the monster is.
    /// </summary>
    [TestMethod]
    public Task AFlinchEndsTheMoveWhereTheMonsterIs() => WithPuppetAsync(arena =>
    {
        arena.DashAt(5000f);
        arena.Advance(1.6f);
        arena.Body.Stagger(1f);

        var flinched = arena.Body.Position;

        arena.Advance(2f);

        Assert.IsGreaterThan(100f, arena.Start.Y - flinched.Y, "The dash had carried the monster before the flinch.");
        Assert.AreEqual(flinched, arena.Body.Position);

        return Task.CompletedTask;
    });

    /// <summary>
    /// The walk after the skill starts where the skill has left the monster.
    /// </summary>
    [TestMethod]
    public Task TheWalkAfterTheSkillStartsWhereTheSkillLeftTheMonster() => WithPuppetAsync(arena =>
    {
        arena.DashAt(5000f);
        arena.Advance(3.5f);

        var after = arena.Body.Position;

        Assert.IsTrue(arena.Body.MoveTo(after + new Vector3(0f, -300f, 0f), false));
        arena.Advance(1f);

        var walk = arena.Frames(MoveOpcode.MoveBt);

        Assert.IsNotEmpty(walk);
        Assert.AreEqual(after.Y, PlaceOf(walk[0], 4 + 8).Y, 1f);
        Assert.IsGreaterThan(DashWay - 10f, arena.Start.Y - PlaceOf(walk[0], 4 + 8).Y, "Not from where the skill began.");

        return Task.CompletedTask;
    });
}
