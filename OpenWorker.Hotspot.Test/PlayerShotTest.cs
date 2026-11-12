using System.Buffers.Binary;
using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Modules.Skill.Requests;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Lua;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;
using OpenWorker.UpdateContent.Res.Rows;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What a player fires with a skill is bounded by the skill, with the client's tables and action files: a projectile leaves
/// from where the server has the player, a skill fires no more shots or chains than it lets go, a player who is down fires
/// nothing, a trap dragged with the mouse stays within the reach of the mouse, and a player who leaves lets go of what it carries.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PlayerShotTest
{
    /// <summary>
    /// A skill of Haru that throws one projectile (attack 1701 of its only animation).
    /// </summary>
    private const int Flying = 12000911;

    private const short FlyingAttack = 1701;

    /// <summary>
    /// A skill of Haru that charges, and a shot of the end of its third level.
    /// </summary>
    private const int ChargedSkill = 12000632;

    private const short ChargedShot = 1853;

    /// <summary>
    /// A skill of Stella whose loop, played while the key is held, lets its chains go as it begins (attack 1201 among them).
    /// </summary>
    private const int Chain = 52000711;

    private const short ChainAttack = 1201;

    /// <summary>
    /// A skill of Haru that carries away what stands before it, and a monster light enough to be carried.
    /// </summary>
    private const int PierceStep = 12000411;

    private const int LightMonster = 30103001;

    /// <summary>
    /// The shot of a skill leaves from where the server has the player, whatever place the client names, the way the client aims.
    /// </summary>
    [TestMethod]
    public async Task AShotLeavesFromWhereTheServerHasThePlayer()
    {
        var world = World.Create();

        try
        {
            var arena = new Arena(world, Hero.Haru, 350f).Open();
            var player = world.Get<WorldComponent>(arena.Player).Position;

            arena.Maze.StartSkill(arena.Player, Flying, default);

            Assert.AreEqual(ClaimVerdict.Taken, arena.Maze.FireProjectile(arena.Player, Shot(Flying, FlyingAttack, player + new Vector3(5000f, 5000f, 0f))));

            await MetricAssertions.UntilAsync(() => Frames(arena.Connection, SkillOpcode.ProjectileBt).Length == 1, "The others see the shot.");

            var body = Frames(arena.Connection, SkillOpcode.ProjectileBt).Single().Body;
            var start = new Vector2(BinaryPrimitives.ReadSingleLittleEndian(body.AsSpan(4 + 4 + 2)), BinaryPrimitives.ReadSingleLittleEndian(body.AsSpan(4 + 4 + 2 + 4)));

            Assert.IsLessThan(500f, Vector2.Distance(start, new Vector2(player.X, player.Y)), $"The shot leaves from the player, not from the place the client named: {start}.");

            arena.Maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A skill fires the shots its animations let go and no more: the next one is refused and counts against the error budget,
    /// the ones the client sends in the same second only do nothing. A chain is bounded the same way.
    /// </summary>
    [TestMethod]
    public void ASkillFiresNoMoreShotsThanItLetsGo()
    {
        var world = World.Create();

        try
        {
            var arena = new Arena(world, Hero.Haru, 350f).Open();
            var place = world.Get<WorldComponent>(arena.Player).Position;

            arena.Maze.StartSkill(arena.Player, Flying, default);

            Assert.AreEqual(ClaimVerdict.Taken, arena.Maze.FireProjectile(arena.Player, Shot(Flying, FlyingAttack, place)));
            Assert.AreEqual(ClaimVerdict.Refused, arena.Maze.FireProjectile(arena.Player, Shot(Flying, FlyingAttack, place)), "The animation let one shot go.");
            Assert.AreEqual(ClaimVerdict.Dropped, arena.Maze.FireProjectile(arena.Player, Shot(Flying, FlyingAttack, place)), "The fault is counted once a second.");
            Assert.AreEqual(ClaimVerdict.Dropped, arena.Maze.FireProjectile(arena.Player, Shot(Flying, 99, place)), "The skill has no such attack.");

            arena.Run(1.2f);
            arena.Maze.StartSkill(arena.Player, Flying + 1, default);

            Assert.AreEqual(ClaimVerdict.Dropped, arena.Maze.FireProjectile(arena.Player, Shot(Flying, FlyingAttack, place)), "Another skill has cut it.");

            arena.Maze.Close();

            var stella = new Arena(world, Hero.Stella, 350f).Open();

            stella.Maze.StartSkill(stella.Player, Chain, default);
            stella.Maze.PressKey(stella.Player, true);

            Assert.AreEqual(ClaimVerdict.Refused, stella.Maze.FireChain(stella.Player, ChainOf(Chain, ChainAttack)), "The loop that lets the chain go has not begun.");

            stella.Run(1.2f);

            Assert.AreEqual(ClaimVerdict.Taken, stella.Maze.FireChain(stella.Player, ChainOf(Chain, ChainAttack)));
            Assert.AreEqual(ClaimVerdict.Refused, stella.Maze.FireChain(stella.Player, ChainOf(Chain, ChainAttack)), "The loop let one chain go.");

            stella.Maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A player who is down fires nothing, lets no chain go and drags no trap: the requests do nothing and are no fault.
    /// </summary>
    [TestMethod]
    public void APlayerWhoIsDownFiresNothing()
    {
        var world = World.Create();

        try
        {
            var arena = new Arena(world, Hero.Haru, 350f).Open();
            var place = world.Get<WorldComponent>(arena.Player).Position;

            arena.Maze.StartSkill(arena.Player, Flying, default);

            var ability = world.Get<AbilityComponent>(arena.Player);

            world.Set(arena.Player, ability with { Health = ability.Health with { Current = 0 } });

            Assert.AreEqual(ClaimVerdict.Dropped, arena.Maze.FireProjectile(arena.Player, Shot(Flying, FlyingAttack, place)));
            Assert.AreEqual(ClaimVerdict.Dropped, arena.Maze.FireChain(arena.Player, ChainOf(Flying, FlyingAttack)));
            Assert.AreEqual(ClaimVerdict.Dropped, arena.Maze.MoveTrap(arena.Player, place));

            arena.Maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A trap dragged with the mouse goes where the client puts it within the reach of the mouse from where the server has the
    /// player; a place beyond is refused, and counts against the error budget once a second.
    /// </summary>
    [TestMethod]
    public void ATrapIsDraggedOnlyWithinTheReachOfTheMouse()
    {
        var world = World.Create();

        try
        {
            var arena = new Arena(world, Hero.Erwin, 350f).Open();
            var place = world.Get<WorldComponent>(arena.Player).Position;

            Assert.AreEqual(ClaimVerdict.Taken, arena.Maze.MoveTrap(arena.Player, place + new Vector3(2400f, 2400f, 0f)));
            Assert.AreEqual(ClaimVerdict.Refused, arena.Maze.MoveTrap(arena.Player, place + new Vector3(3000f, 3000f, 0f)));
            Assert.AreEqual(ClaimVerdict.Dropped, arena.Maze.MoveTrap(arena.Player, place + new Vector3(float.MaxValue, 0f, 0f)), "The fault is counted once a second.");

            arena.Run(1.1f);

            Assert.AreEqual(ClaimVerdict.Refused, arena.Maze.MoveTrap(arena.Player, place + new Vector3(float.MaxValue, 0f, 0f)));

            arena.Maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

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
    /// A skill that charges ends with the animation of the level it reached, and the shots of that animation are the skill's.
    /// </summary>
    [TestMethod]
    public void TheShotsOfAChargedEndAreTheSkills()
    {
        var folder = Environment.GetEnvironmentVariable("OPENWORKER_TEST_ACTION");

        if (string.IsNullOrEmpty(folder))
        {
            Assert.Inconclusive("OPENWORKER_TEST_ACTION is not set.");
        }

        var actions = new SkillActionProvider(TestTables.Load<SkillRow>("tb_Skill"), NullLogger<SkillActionProvider>.Instance) { Folder = folder };

        Assert.IsTrue(actions.TryGetProjectile(ChargedSkill, Hero.Haru, ChargedShot, out _));
    }

    private static SkillProjectileRequest Shot(int skill, short attack, Vector3 place)
    {
        Span<byte> bytes = stackalloc byte[4 + 2 + (12 * 3) + 4 + 4 + 2 + 2 + 4 + 4];

        var writer = new PacketWriter(bytes);

        writer.WriteInt32(skill);
        writer.WriteInt16(attack);
        writer.WriteSingle(place.X);
        writer.WriteSingle(place.Y);
        writer.WriteSingle(place.Z);

        // The camera looks the way the player does, along yaw 0.
        writer.WriteSingle(180f);
        writer.WriteSingle(0f);
        writer.WriteSingle(0f);
        writer.WriteSingle(-1f);
        writer.WriteSingle(0f);
        writer.WriteSingle(0f);
        writer.WriteSingle(0f);
        writer.WriteSingle(0f);
        writer.WriteInt16(0);
        writer.WriteInt16(0);
        writer.WriteUInt32(0);
        writer.WriteInt32(0);

        var reader = new PacketReader(bytes);

        return SkillProjectileRequest.Read(ref reader);
    }

    private static SkillProjectileValue ChainOf(int skill, short attack) => default(SkillProjectileValue) with { Skill = skill, TriggerIndex = attack };

    private static PlayerFrame[] Frames(RecordingConnection connection, SkillOpcode command) =>
        connection.Frames.Select(PlayerFrame.Open).Where(e => e.Is(GroupOpcode.Skill, (byte)command)).ToArray();

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
