using System.Buffers.Binary;
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
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Lua;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The traps a maze script lays on the map: they belong to the scene attacker of the maze, hit as its blows, and live as long as
/// their attack event or the script says.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MapTrapTest
{
    /// <summary>
    /// A trap that lingers hits what comes into it at once, then again at every interval while it stays, and not past its life.
    /// </summary>
    [TestMethod]
    public void ALingeringTrapHitsAtOnceThenEveryIntervalUntilItsLifeIsOver()
    {
        var world = World.Create();

        try
        {
            var target = world.Create();
            var place = new Vector3(50f, 0f, 0f);
            var projectile = new ProjectileInfo(Vector3.Zero, 0f, 1f, false, 0, 10f, true, 0.375f, false, false, 0, Vector3.Zero, false, 0);
            var trigger = new AttackTrigger(1, 0, 2, 0, 0f, 360f, Vector3.Zero, 0f, 0f, 100f, 100f, 0f, Projectile: projectile);
            var trap = new MapTrap(1, trigger, Vector3.Zero, null, 1);
            var hits = new List<int>();

            for (var step = 1; step <= 10; step++)
            {
                if (trap.Step(0.125f, area => area.Touches(place, 0f, 0f) ? [target] : []).Count > 0)
                {
                    hits.Add(step);
                }
            }

            CollectionAssert.AreEqual(new[] { 1, 4, 7 }, hits, "Every 0.375 s from the first touch, and its core is not touched at its end.");
            Assert.IsFalse(trap.Active);
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// The client is shown the scene attacker of the maze, then the trap as its projectile; a player in the trap takes its blow as
    /// from a monster.
    /// </summary>
    [TestMethod]
    public async Task AScriptTrapIsTheSceneAttackersAndHitsAPlayerAsAMonsterDoes()
    {
        const int attacker = 10000209;
        const int skill = 40952;

        var world = World.Create();

        try
        {
            var arena = new Arena(world, attacker);

            new LuaGameHelper(NullLogger<LuaGameHelper>.Instance).CreateTrap(arena.Maze, new VVector3 { X = 30f }, skill);
            arena.Run(1f);

            Assert.IsLessThan(100, world.Get<AbilityComponent>(arena.Player).Health.Current, "The trap hurts the player standing in it.");

            await QueueMarker.SettleAsync(world.Get<ServerSessionComponent>(arena.Player), arena.Connection);

            var frames = arena.Connection.Frames.Select(PlayerFrame.Open).ToArray();
            var shown = Array.FindIndex(frames, e => e.Is(GroupOpcode.World, (byte)WorldOpcode.OtherInfosMonster) && Holds(e.Body, attacker));
            var laid = Array.FindIndex(frames, e => e.Is(GroupOpcode.Skill, (byte)SkillOpcode.ProjectileBt));

            Assert.IsGreaterThanOrEqualTo(0, shown, "The scene attacker is shown.");
            Assert.IsGreaterThan(shown, laid, "Then its trap.");
            var owner = BinaryPrimitives.ReadInt32LittleEndian(frames[shown].Body.AsSpan(2));

            Assert.AreEqual(new ActorComponent(owner).Identifier, frames[laid].Actor);
            Assert.IsTrue(frames.Any(e => e.Is(GroupOpcode.Skill, (byte)SkillOpcode.ActionSkillBt) && Holds(e.Body, owner)), "The blow is the scene attacker's.");

            arena.Maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A number holds the first trap laid under it; the script ends that trap by the number.
    /// </summary>
    [TestMethod]
    public void TheScriptEndsTheTrapOfANumber()
    {
        const int attacker = 10001001;
        const int skill = 40900;

        var world = World.Create();

        try
        {
            var arena = new Arena(world, attacker);
            var helper = new LuaGameHelper(NullLogger<LuaGameHelper>.Instance);
            var first = helper.CreateTrapLoop(arena.Maze, new VVector3 { X = 30f }, skill, 3).Trap;
            var second = helper.CreateTrapLoop(arena.Maze, new VVector3 { X = 60f }, skill, 3).Trap;

            Assert.IsNotNull(first);
            Assert.IsNotNull(second);

            arena.Maze.SetTrapLifeTime(3, 0.1f);
            arena.Run(1f);

            Assert.IsFalse(first.Active);
            Assert.IsTrue(second.Active, "A number already held does not take a second trap.");

            arena.Maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    private static bool Holds(byte[] body, int value)
    {
        Span<byte> bytes = stackalloc byte[4];

        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);

        return body.AsSpan().IndexOf(bytes) >= 0;
    }

    /// <summary>
    /// A started maze of the client's tables and files whose scene attacker is the monster given, and one player at its origin
    /// who has loaded it.
    /// </summary>
    private sealed class Arena
    {
        private readonly FakeTimeProvider _time = new();
        private readonly SimulationClock _clock;

        public Arena(World world, int attacker)
        {
            var folder = Environment.GetEnvironmentVariable("OPENWORKER_TEST_ACTION");

            if (string.IsNullOrEmpty(folder))
            {
                Assert.Inconclusive("OPENWORKER_TEST_ACTION is not set.");
            }

            var monsters = TestTables.Load<MonsterRow>("tb_Monster");
            var skills = TestTables.Load<SkillRow>("tb_Skill");
            var tables = new CombatTables(monsters, TestTables.Load<StatusRow>("tb_Status"), skills, TestTables.Load<CharacterInfoRow>("tb_Character_Info"));
            var actions = new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance) { Folder = folder };
            var batch = TestMazes.Map();

            _clock = TestMazes.Clock(_time);

            var services = new CreatureServices(null!, new MonsterBehaviorTables(monsters, skills), actions, _clock, new Navigation.NavMeshProvider());
            var life = new AbilityComponentEntry { Current = 100, Max = 100 };

            Player = world.Create(
                new ActorComponent(1, ActorType.User),
                new ServerSessionComponent(Connection),
                new WorldComponent { Location = 0, Position = Vector3.Zero, Rotation = 0f, Jump = 0 },
                new LevelComponent(20, 0),
                new PersonInfoComponent { Name = "Walker", Hero = Hero.Haru },
                new InventoryComponent(new Inventory(1)),
                new AbilityComponent { Health = life, SoulGain = life, SoulVapor = life, Stamina = life, SuperArmor = life, Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f } });

            Maze = TestMazes.Create(world, Player, clock: _clock, batch: batch, creatures: new LuaCreatureManager(world, batch, tables, services), actions: actions, tables: tables,
                sceneAttacker: attacker);

            Assert.IsTrue(Maze.TryStart());
            Maze.MarkLoaded(Player);
        }

        public RecordingConnection Connection { get; } = new();

        public Entity Player { get; }

        public LuaMaze Maze { get; }

        public void Run(float seconds)
        {
            for (var tick = 0; tick < seconds * 20; tick++)
            {
                _time.Advance(SimulationClock.Period);
                _clock.Tick();
            }
        }
    }
}
