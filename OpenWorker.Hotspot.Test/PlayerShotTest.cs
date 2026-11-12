using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Lua;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What a player sets going with a skill, with the client's tables and action files: a player who leaves lets go of what it carries.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PlayerShotTest
{
    /// <summary>
    /// A skill of Haru that carries away what stands before it, and a monster light enough to be carried.
    /// </summary>
    private const int PierceStep = 12000411;

    private const int LightMonster = 30103001;

    /// <summary>
    /// A player who leaves the maze while it carries monsters lets them go where they stand: nothing steps the carry of a player
    /// who is gone, and they would stand still for good.
    /// </summary>
    [TestMethod]
    public void APlayerWhoLeavesLetsGoOfWhatItCarries()
    {
        var world = World.Create();

        try
        {
            var arena = new Arena(world, Hero.Haru, 120f, LightMonster).Open();
            var stays = Arena.Person(world, 2, Hero.Haru, new RecordingConnection());

            Assert.IsTrue(arena.Maze.Join(stays));

            var body = arena.Creatures.BodyOf(arena.Monster)!;

            arena.Maze.StartSkill(arena.Player, PierceStep, default);

            for (var step = 0; step < 40 && !body.IsCarried; step++)
            {
                arena.Run(SimulationClock.Period.TotalSeconds);
            }

            Assert.IsTrue(body.IsCarried, "The skill carries the monster in front of the player.");
            Assert.IsFalse(arena.Maze.Leave(arena.Player), "The maze goes on for the one who stays.");
            Assert.IsFalse(body.IsCarried, "The monster is let go.");

            arena.Maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A maze of the client's tables and files with one monster, and a player of the hero who stands the given distance behind it,
    /// facing it along yaw 0.
    /// </summary>
    private sealed class Arena(World world, Hero hero, float distance, int monster = 0)
    {
        public FakeTimeProvider Time { get; } = new();
        public RecordingConnection Connection { get; } = new();
        public SimulationClock Clock { get; private set; } = null!;
        public Entity Player { get; private set; }
        public Entity Monster { get; private set; }
        public LuaCreatureManager Creatures { get; private set; } = null!;
        public LuaMaze Maze { get; private set; } = null!;

        public Arena Open()
        {
            var folder = Environment.GetEnvironmentVariable("OPENWORKER_TEST_ACTION");
            var skills = TestTables.Load<SkillRow>("tb_Skill");

            if (string.IsNullOrEmpty(folder))
            {
                Assert.Inconclusive("OPENWORKER_TEST_ACTION is not set.");
            }

            var heroes = TestTables.Load<CharacterInfoRow>("tb_Character_Info");
            var monsters = TestTables.Load<MonsterRow>("tb_Monster");
            var tables = new CombatTables(monsters, TestTables.Load<StatusRow>("tb_Status"), skills, heroes);
            var kind = monster != 0 ? monster : monsters.First(e => e.Level is > 1 and < 20 && e.Type == 0 && e.Rank <= 6 && tables.GetMonster(e.Id).Stats.MaxHealth >= 100f).Id;
            var actions = new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance) { Folder = folder };

            Clock = TestMazes.Clock(Time);
            Player = Person(world, 1, hero, Connection);

            // Only a monster with a mind has a body that can be carried.
            var minds = monster != 0 ? Environment.GetEnvironmentVariable("OPENWORKER_TEST_AI") : null;

            if (monster != 0 && string.IsNullOrEmpty(minds))
            {
                Assert.Inconclusive("OPENWORKER_TEST_AI is not set.");
            }

            var services = minds is null
                ? new CreatureServices(null!, new Gameplay.Ai.MonsterBehaviorTables(TestMazes.None<MonsterRow>(), TestMazes.None<SkillRow>()), actions, Clock,
                    new Navigation.NavMeshProvider())
                : new CreatureServices(new AiScriptLoader(NullLogger<AiScriptLoader>.Instance, minds),
                    new Gameplay.Ai.MonsterBehaviorTables(monsters, skills, TestTables.Load<FactionRow>("tb_Faction")), actions, Clock, new Navigation.NavMeshProvider());
            var batch = TestMazes.Map(PartyMazeTest.MapWithMonster(kind));

            Creatures = new LuaCreatureManager(world, batch, tables, services);
            Maze = TestMazes.Create(world, Player, clock: Clock, batch: batch, creatures: Creatures, actions: actions, tables: tables);

            Assert.IsTrue(Maze.TryStart());
            Maze.MarkLoaded(Player);
            Maze.SpawnSector(1);

            Monster = Creatures.FindMonsters((_, _) => true).Single();

            var target = world.Get<WorldComponent>(Monster).Position;

            world.Set(Player, world.Get<WorldComponent>(Player) with { Position = target + new Vector3(0f, distance, 0f), Rotation = 0f });

            return this;
        }

        public static Entity Person(World world, int person, Hero hero, RecordingConnection connection)
        {
            var life = new AbilityComponentEntry { Current = 100, Max = 100 };

            return world.Create(
                new ActorComponent(person, ActorType.User),
                new ServerSessionComponent(connection),
                new WorldComponent { Location = 0, Position = Vector3.Zero, Rotation = 0f, Jump = 0 },
                new LevelComponent(20, 0),
                new PersonInfoComponent { Name = "Shooter" + person, Hero = hero },
                new AbilityComponent { Health = life, SoulGain = life, SoulVapor = life, Stamina = life, SuperArmor = life, Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f } });
        }

        public void Run(double seconds)
        {
            for (var tick = 0; tick < seconds / SimulationClock.Period.TotalSeconds; tick++)
            {
                Time.Advance(SimulationClock.Period);
                Clock.Tick();
            }
        }
    }
}
