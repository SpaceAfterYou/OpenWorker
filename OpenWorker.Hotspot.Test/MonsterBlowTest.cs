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
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Lua;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;
using OpenWorker.Navigation;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What the blow of a monster does to the players it reaches.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MonsterBlowTest
{
    private const int Monster = 1000;

    /// <summary>
    /// A monster of the client's tables that shoots: one shot, 0.93 s into the skill, at 3000 a second.
    /// </summary>
    private const string ShooterFile = "MOB_0215_Intension Summoner";

    private const int Shot = 1303043072;

    /// <summary>
    /// A monster whose shot follows its target: one shot, 1.0 s into the skill.
    /// </summary>
    private const string GuidedShooterFile = "MOB_9066_DignitySnasher";

    private const int GuidedShot = 1302050241;

    /// <summary>
    /// A monster that lays eight traps round itself, 300 away, each a circle of 150 that hits every half second for 4 s.
    /// </summary>
    private const string TrapperFile = "MOB_0211_Hell Collector";

    private const int TrapSkill = 1303053064;

    /// <summary>
    /// Where the trap that lies to the right of the monster is, from the monster that looks along -Y.
    /// </summary>
    private static readonly Vector3 RightTrap = new(300f, 0f, 0f);

    private sealed class Arena(World world, LuaMaze maze, Entity player, MonsterBody body, RecordingConnection connection, FakeTimeProvider time, SimulationClock clock)
    {
        public LuaMaze Maze { get; } = maze;
        public Entity Player { get; } = player;
        public MonsterBody Body { get; } = body;

        public void Advance(float seconds)
        {
            for (var step = 0; step < seconds / SimulationClock.Period.TotalSeconds; step++)
            {
                time.Advance(SimulationClock.Period);
                clock.Tick();
                Maze.Creatures.Step(SimulationClock.Period);
            }
        }

        public void Place(Vector3 position)
        {
            world.Set(Player, world.Get<WorldComponent>(Player) with { Position = position });
        }

        public PlayerFrame[] Hits() =>
            connection.Frames.Select(PlayerFrame.Open).Where(e => e.Is(GroupOpcode.Skill, (byte)SkillOpcode.ActionSkillBt)).ToArray();

        public PlayerFrame[] Shots() =>
            connection.Frames.Select(PlayerFrame.Open).Where(e => e.Is(GroupOpcode.Skill, (byte)SkillOpcode.ProjectileBt)).ToArray();

        /// <summary>
        /// Waits for the hits told so far to come out, then counts them.
        /// </summary>
        public async Task<int> HitsAsync(int atLeast)
        {
            await MetricAssertions.UntilAsync(() => Hits().Length >= atLeast, $"{atLeast} hits are told.");
            await Task.Delay(100);

            return Hits().Length;
        }
    }

    /// <summary>
    /// The skills of the client's tables, or null when the variables of the test data are not set.
    /// </summary>
    private static (ReadOnlyCollection<SkillRow> Skills, string Actions)? Data(params int[] ids)
    {
        var tables = Environment.GetEnvironmentVariable("OPENWORKER_TEST_RES");
        var actions = Environment.GetEnvironmentVariable("OPENWORKER_TEST_ACTION");

        if (string.IsNullOrEmpty(tables) || string.IsNullOrEmpty(actions))
        {
            return null;
        }

        using var reader = new BinaryReader(File.OpenRead(Path.Join(tables, "tb_Skill.res")));
        var rows = new SkillRow[reader.ReadInt32()];

        for (var index = 0; index < rows.Length; index++)
        {
            rows[index] = new SkillRow(reader);
        }

        return (new ReadOnlyCollection<SkillRow>([.. ids.Select(id => rows.First(e => e.Id == id))]), actions);
    }

    private static async Task WithArenaAsync(Func<Arena, Task> test, MonsterRow? row = null, ReadOnlyCollection<SkillRow>? skills = null, string actions = "action")
    {
        var folder = Directory.CreateTempSubdirectory();
        var world = World.Create();

        try
        {
            File.WriteAllText(Path.Join(folder.FullName, "Ai_Still.lua"),
                "function Initial_Normal(self, monster)\n  local ai = monster:GetAi()\n  ai:SetAiCheckTime(0.1)\n  ai:SetFindEnemyInfo(0, 1.0, 100.0)\nend\n");

            var time = new FakeTimeProvider();
            var clock = TestMazes.Clock(time);
            var monsters = new ReadOnlyCollection<MonsterRow>([row ?? TestRows.Build<MonsterRow>(("Id", Monster), ("AiScript", "Ai_Still"))]);

            skills ??= TestMazes.None<SkillRow>();

            var combat = new CombatTables(monsters, TestMazes.None<StatusRow>(), skills, TestMazes.None<CharacterInfoRow>());
            var services = new CreatureServices(new AiScriptLoader(NullLogger<AiScriptLoader>.Instance, folder.FullName), new MonsterBehaviorTables(monsters, skills),
                new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance) { Folder = actions }, clock, new NavMeshProvider());
            var batch = TestMazes.Map(PartyMazeTest.MapWithMonster(Monster));
            var connection = new RecordingConnection();
            var player = world.Create(
                new ActorComponent(100, ActorType.User),
                new ServerSessionComponent(connection),
                new WorldComponent { Location = 0, Position = Vector3.Zero, Rotation = 0f, Jump = 0 },
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

    /// <summary>
    /// A blow without damage neither hurts nor throws: the clients are told nothing of it, and only a blow that deals damage
    /// is a hit.
    /// </summary>
    [TestMethod]
    public Task ABlowWithoutDamageIsNoHit() => WithArenaAsync(async arena =>
    {
        var blow = new AttackTrigger(1, 0, 0, 0, 0f, 360f, Vector3.Zero, 0f, 0f, 500f, 500f, 0f, DamageRate: 0f, ReactionType: 1);

        Assert.AreEqual(0, arena.Maze.Creatures.HitPlayers(arena.Body, 0, blow, [arena.Player]), "A blow without damage strikes nobody.");
        Assert.AreNotEqual(0, arena.Maze.Creatures.HitPlayers(arena.Body, 0, blow with { DamageRate = 1f }, [arena.Player]));

        await MetricAssertions.UntilAsync(() => arena.Hits().Length > 0, "The blow with damage is told.");

        Assert.HasCount(1, arena.Hits(), "Only the blow with damage is told.");
    });

    /// <summary>
    /// A shot the monster has let go flies on when the monster is thrown off balance: only what it had still to deal is lost.
    /// </summary>
    [TestMethod]
    public async Task AShotOnItsWayFliesOnWhenTheMonsterIsThrownOffBalance()
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

        var skills = new ReadOnlyCollection<SkillRow>([rows.First(e => e.Id == Shot)]);
        var shooter = TestRows.Build<MonsterRow>(("Id", Monster), ("AiScript", "Ai_Still"), ("ActionFile", ShooterFile), ("Skill1", Shot));

        await WithArenaAsync(async arena =>
        {
            var aim = arena.Body.Position + new Vector3(1000f, 0f, 0f);

            arena.Place(aim);

            Assert.IsTrue(arena.Body.UseSkill(arena.Body.GetSkill(0)!, new AiTarget(100, aim, 40f, 0f, 1, 1, false)));

            // The shot leaves at 0.93 s and needs a third of a second to come.
            arena.Advance(1.0f);
            arena.Body.Stagger(1f);
            arena.Advance(1.0f);

            await MetricAssertions.UntilAsync(() => arena.Hits().Length > 0, "The shot lands.");
        }, shooter, skills, actions!);
    }

    /// <summary>
    /// A trap of a monster stays for its life and hits whoever is in it at once and again every interval while it stays; one
    /// who has left it is hit no more.
    /// </summary>
    [TestMethod]
    public async Task ATrapHitsWhoStaysInItEveryIntervalAndNotWhoLeft()
    {
        if (Data(TrapSkill) is not var (skills, actions))
        {
            Assert.Inconclusive("OPENWORKER_TEST_RES and OPENWORKER_TEST_ACTION are not set.");

            return;
        }

        var trapper = TestRows.Build<MonsterRow>(("Id", Monster), ("AiScript", "Ai_Still"), ("ActionFile", TrapperFile), ("Skill1", TrapSkill));
        var laid = new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance) { Folder = actions }
            .GetTimeline(TrapSkill, TrapperFile).Hits.First(e => e.Trigger.IsTrap && e.Trigger.Center == RightTrap).Time;

        await WithArenaAsync(async arena =>
        {
            var ahead = arena.Body.Position + new Vector3(0f, -1000f, 0f);

            Assert.IsTrue(arena.Body.UseSkill(arena.Body.GetSkill(0)!, new AiTarget(100, ahead, 40f, 0f, 1, 1, false)));

            arena.Place(arena.Body.Position + TrapLayout.Turn(RightTrap, arena.Body.Yaw));
            arena.Advance(laid - 0.1f);

            Assert.AreEqual(0, await arena.HitsAsync(0), "Nothing hits before the trap is laid.");

            arena.Advance(0.2f);

            Assert.AreEqual(1, await arena.HitsAsync(1), "The trap hits at once who is in it.");

            arena.Advance(1.2f);

            Assert.AreEqual(3, await arena.HitsAsync(3), "It hits again every half second.");

            arena.Place(arena.Body.Position + new Vector3(0f, 3000f, 0f));
            arena.Advance(4f);

            Assert.AreEqual(3, await arena.HitsAsync(3), "Who has left is hit no more, and the burst at its end is far.");
        }, trapper, skills, actions);
    }

    /// <summary>
    /// The clients fly the shots of a monster themselves: only a shot that follows its target is told to them.
    /// </summary>
    [TestMethod]
    public async Task OnlyAShotThatFollowsItsTargetIsTold()
    {
        if (Data(Shot, GuidedShot) is not var (skills, actions))
        {
            Assert.Inconclusive("OPENWORKER_TEST_RES and OPENWORKER_TEST_ACTION are not set.");

            return;
        }

        foreach (var (file, skill, told) in new[] { (ShooterFile, Shot, false), (GuidedShooterFile, GuidedShot, true) })
        {
            var shooter = TestRows.Build<MonsterRow>(("Id", Monster), ("AiScript", "Ai_Still"), ("ActionFile", file), ("Skill1", skill));

            await WithArenaAsync(async arena =>
            {
                var aim = arena.Body.Position + new Vector3(0f, -1000f, 0f);

                Assert.IsTrue(arena.Body.UseSkill(arena.Body.GetSkill(0)!, new AiTarget(100, aim, 40f, 0f, 1, 1, false)));

                arena.Advance(1.5f);

                await Task.Delay(100);

                Assert.AreEqual(told, arena.Shots().Length > 0, $"The shot of {file} is told: {told}.");
            }, shooter, skills, actions);
        }
    }
}
