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
    /// A monster whose skill goes on, every time, with the next of its combo once its end animation may be cut, 1.77 s in.
    /// </summary>
    private const string ComboFile = "MOB_9024_BloodyScar";

    private const int ComboSkill = 1303044070;
    private const int ComboNext = 1303044071;

    /// <summary>
    /// A monster whose skill brings in two small puppets 4 s in, each somewhere in a square of 302 round a point 210 ahead of it,
    /// one to the left and one to the right.
    /// </summary>
    private const string SummonerFile = "MOB_0023_Bloodypuppet";

    private const int SummonSkill = 1301040044;
    private const int Summoned = 30104001;

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

        public World World => world;

        public int Shown() =>
            connection.Frames.Select(PlayerFrame.Open).Count(e => e.Is(GroupOpcode.World, (byte)WorldOpcode.OtherInfosMonster));

        public PlayerFrame[] Starts() =>
            connection.Frames.Select(PlayerFrame.Open).Where(e => e.Is(GroupOpcode.Skill, (byte)SkillOpcode.ActiveSkillBt)).ToArray();

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

        /// <summary>
        /// The client of the player says it plays a motion.
        /// </summary>
        public void Tell(short motion, short step)
        {
            world.Set(Player, PlayerStance.Told(world.Get<PlayerPostureComponent>(Player), motion, step));
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

    private static async Task WithArenaAsync(Func<Arena, Task> test, MonsterRow? row = null, ReadOnlyCollection<SkillRow>? skills = null, string actions = "action", SkillChains? chains = null, MonsterRow? other = null)
    {
        var folder = Directory.CreateTempSubdirectory();
        var world = World.Create();

        try
        {
            File.WriteAllText(Path.Join(folder.FullName, "Ai_Still.lua"),
                "function Initial_Normal(self, monster)\n  local ai = monster:GetAi()\n  ai:SetAiCheckTime(0.1)\n  ai:SetFindEnemyInfo(0, 1.0, 100.0)\nend\n");

            var time = new FakeTimeProvider();
            var clock = TestMazes.Clock(time);
            var monsters = new ReadOnlyCollection<MonsterRow>([row ?? TestRows.Build<MonsterRow>(("Id", Monster), ("AiScript", "Ai_Still")), .. other is { } more ? [more] : Array.Empty<MonsterRow>()]);

            skills ??= TestMazes.None<SkillRow>();

            var combat = new CombatTables(monsters, TestMazes.None<StatusRow>(), skills, TestMazes.None<CharacterInfoRow>());
            var services = new CreatureServices(new AiScriptLoader(NullLogger<AiScriptLoader>.Instance, folder.FullName), new MonsterBehaviorTables(monsters, skills),
                new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance) { Folder = actions }, clock, new NavMeshProvider(), chains: chains);
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

            var monster = maze.Creatures.FindMonsters((_, _) => true).First();

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
    /// A trap with a reaction and a buff but no damage still hits: the player is told the reaction and no damage, and keeps its life.
    /// One with neither damage nor a reaction hits nobody.
    /// </summary>
    [TestMethod]
    public Task ATrapWithAReactionAndNoDamageHits() => WithArenaAsync(async arena =>
    {
        var trap = new AttackTrigger(1, 0, 0, 0, 0f, 360f, Vector3.Zero, 0f, 0f, 500f, 500f, 0f, DamageRate: 0f, ReactionType: 1, Buff: 1);
        var place = arena.Body.Position + new Vector3(300f, 0f, 0f);

        Assert.AreEqual(0, arena.Maze.Creatures.HitPlayers(arena.Body.Entity, 0, trap with { ReactionType = 0 }, [arena.Player], place));
        Assert.AreNotEqual(0, arena.Maze.Creatures.HitPlayers(arena.Body.Entity, 0, trap, [arena.Player], place));

        Assert.AreEqual(1, await arena.HitsAsync(1), "Only the trap with a reaction is told.");

        var body = arena.Hits()[0].Body;

        Assert.AreEqual(1, body[1 + 4], "The reaction.");
        Assert.AreEqual(0, BinaryPrimitives.ReadInt32LittleEndian(body.AsSpan(1 + 6)), "No damage.");
        Assert.AreEqual(100000, BinaryPrimitives.ReadInt32LittleEndian(body.AsSpan(1 + 10)), "The life is whole.");
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
    /// The hit of a trap is told from the place of the trap, facing the way the monster faces: one target after the count, its
    /// 37 bytes, then the actor of the monster, then the place and the facing.
    /// </summary>
    [TestMethod]
    public async Task AHitOfATrapIsToldFromTheTrap()
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

            var trap = arena.Body.Position + TrapLayout.Turn(RightTrap, arena.Body.Yaw);

            arena.Place(trap);
            arena.Advance(laid + 0.1f);

            Assert.AreEqual(1, await arena.HitsAsync(1));

            var body = arena.Hits()[0].Body;
            const int at = 1 + 37 + 4;
            var told = new Vector3(BinaryPrimitives.ReadSingleLittleEndian(body.AsSpan(at)), BinaryPrimitives.ReadSingleLittleEndian(body.AsSpan(at + 4)), 0f);

            // The skill carries the monster a little before it lays the trap, which lies 300 to its right.
            Assert.AreEqual(1, body[0]);
            Assert.AreEqual(trap.X, told.X, 1f);
            Assert.AreEqual(trap.Y, told.Y, 30f);
            Assert.AreEqual(RightTrap.Length(), AiMath.Distance2D(told, arena.Body.Position), 30f, "Not the place of the monster.");
            Assert.AreEqual(arena.Body.Yaw, BinaryPrimitives.ReadSingleLittleEndian(body.AsSpan(at + 12)), 0.01f);
        }, trapper, skills, actions);
    }

    /// <summary>
    /// The monster that lays traps, as heavy as a player, so that its throws lift one the whole way.
    /// </summary>
    private static MonsterRow Thrower()
    {
        return TestRows.Build<MonsterRow>(("Id", Monster), ("AiScript", "Ai_Still"), ("ActionFile", TrapperFile), ("Skill1", TrapSkill),
            ("WeightRank", (byte)PlayerVitals.WeightRank));
    }

    /// <summary>
    /// A player a monster has thrown is in the air, then lies on the ground, then is up again when the time on the ground is over.
    /// </summary>
    [TestMethod]
    public async Task AThrownPlayerIsInTheAirThenLyingThenUpByTheTime()
    {
        if (Data(TrapSkill) is not var (skills, actions))
        {
            Assert.Inconclusive("OPENWORKER_TEST_RES and OPENWORKER_TEST_ACTION are not set.");

            return;
        }

        var trapper = Thrower();

        await WithArenaAsync(async arena =>
        {
            var creatures = arena.Maze.Creatures;

            arena.Place(arena.Body.Position + new Vector3(0f, -200f, 0f));

            Assert.IsFalse(creatures.IsInTheAir(arena.Player) || creatures.IsLying(arena.Player), "Nobody has thrown the player yet.");
            Assert.IsTrue(arena.Maze.KnockDown(arena.Player).Hit, "The monster throws the player.");
            Assert.AreEqual(1, await arena.HitsAsync(1), "The throw is told.");
            Assert.IsTrue(creatures.IsInTheAir(arena.Player) && !creatures.IsLying(arena.Player), "Thrown up, the player is in the air.");

            arena.Advance(1.3f);

            Assert.IsTrue(!creatures.IsInTheAir(arena.Player) && creatures.IsLying(arena.Player), "Landed, the player lies.");

            arena.Advance(2.8f);

            Assert.IsTrue(creatures.IsLying(arena.Player), "Three seconds on the ground and the fall before them.");

            arena.Advance(0.9f);

            Assert.IsFalse(creatures.IsInTheAir(arena.Player) || creatures.IsLying(arena.Player), "Up again by the time.");
        }, trapper, skills, actions);
    }

    /// <summary>
    /// The command's throw is a blow of a skill of the monster's own that throws, so that the clients play it; a monster without
    /// one throws nobody.
    /// </summary>
    [TestMethod]
    public async Task TheCommandThrowsWithABlowOfTheMonstersOwnSkill()
    {
        if (Data(TrapSkill) is not var (skills, actions))
        {
            Assert.Inconclusive("OPENWORKER_TEST_RES and OPENWORKER_TEST_ACTION are not set.");

            return;
        }

        await WithArenaAsync(arena =>
        {
            arena.Place(arena.Body.Position + new Vector3(0f, -200f, 0f));

            var refused = arena.Maze.KnockDown(arena.Player);

            Assert.AreEqual((Monster, 0, false), (refused.Monster, refused.Skill, refused.Hit), "No skill of its own throws.");
            Assert.IsFalse(arena.Maze.Creatures.IsInTheAir(arena.Player));
            Assert.IsEmpty(arena.Hits());

            return Task.CompletedTask;
        });

        var trapper = Thrower();

        await WithArenaAsync(async arena =>
        {
            arena.Place(arena.Body.Position + new Vector3(0f, -200f, 0f));

            var thrown = arena.Maze.KnockDown(arena.Player);

            Assert.AreEqual((TrapSkill, true), (thrown.Skill, thrown.Hit));
            Assert.AreNotEqual(0, thrown.Trigger, "An attack the skill really has.");
            Assert.AreEqual(1, await arena.HitsAsync(1));
            Assert.IsTrue(arena.Maze.Creatures.IsInTheAir(arena.Player));
        }, trapper, skills, actions);
    }

    /// <summary>
    /// While a player a monster has thrown lies on the ground, a blow that is not for one passes over it, and a monster waits with a
    /// skill that is not for one; once its client gets it up, it is hit.
    /// </summary>
    [TestMethod]
    public async Task APlayerOnTheGroundIsNotHitByWhatIsNotForOne()
    {
        if (Data(TrapSkill) is not var (skills, actions))
        {
            Assert.Inconclusive("OPENWORKER_TEST_RES and OPENWORKER_TEST_ACTION are not set.");

            return;
        }

        var trapper = Thrower();
        var laid = new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance) { Folder = actions }
            .GetTimeline(TrapSkill, TrapperFile).Hits.First(e => e.Trigger.IsTrap && e.Trigger.Center == RightTrap).Time;

        await WithArenaAsync(async arena =>
        {
            var ahead = arena.Body.Position + new Vector3(0f, -1000f, 0f);
            var player = new AiTarget(100, ahead, 40f, 0f, 1, 1, false);
            var skill = arena.Body.GetSkill(0)!;

            arena.Place(arena.Body.Position + TrapLayout.Turn(RightTrap, arena.Body.Yaw));
            Assert.IsTrue(arena.Maze.KnockDown(arena.Player).Hit);
            Assert.AreEqual(1, await arena.HitsAsync(1), "The throw.");

            arena.Advance(1.3f);

            Assert.IsFalse(arena.Body.UseSkill(skill, player), "The monster waits while its target lies.");
            Assert.IsTrue(arena.Body.UseSkill(skill with { ForLying = true }, player), "A skill for one on the ground starts.");

            arena.Advance(laid + 0.2f);

            Assert.AreEqual(1, await arena.HitsAsync(1), "The trap passes over the player on the ground.");

            arena.Tell(MotionClass.DownUp, PlayerStance.Rising);
            arena.Advance(0.2f);

            Assert.AreEqual(2, await arena.HitsAsync(2), "Up again, the player is hit.");
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

    /// <summary>
    /// A monster goes on with the next skill of its combo, as the table of the server says, once the end animation of its skill may
    /// be cut short: the clients are told the second skill then. Without the table it makes no combo.
    /// </summary>
    [TestMethod]
    public async Task AMonsterGoesOnWithTheNextSkillOfItsComboOnceItsEndMayBeCut()
    {
        var server = Environment.GetEnvironmentVariable("OPENWORKER_TEST_SERVER_RES");

        if (string.IsNullOrEmpty(server) || Data(ComboSkill, ComboNext) is not var (skills, actions))
        {
            Assert.Inconclusive("OPENWORKER_TEST_RES, OPENWORKER_TEST_ACTION and OPENWORKER_TEST_SERVER_RES are not set.");

            return;
        }

        using var reader = new BinaryReader(File.OpenRead(Path.Join(server, "tb_Skill.res")));
        var rows = new SkillAggroRow[reader.ReadInt32()];

        for (var index = 0; index < rows.Length; index++)
        {
            rows[index] = new SkillAggroRow(reader);
        }

        var comboAt = new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance) { Folder = actions }.GetTimeline(ComboSkill, ComboFile).ComboAt;

        Assert.IsNotNull(comboAt);

        var scar = TestRows.Build<MonsterRow>(("Id", Monster), ("AiScript", "Ai_Still"), ("ActionFile", ComboFile), ("Skill1", ComboSkill));

        foreach (var chains in new[] { new SkillChains(new ReadOnlyCollection<SkillAggroRow>(rows)), null })
        {
            await WithArenaAsync(async arena =>
            {
                var aim = arena.Body.Position + new Vector3(0f, -3000f, 0f);

                Assert.IsTrue(arena.Body.UseSkill(arena.Body.GetSkill(0)!, new AiTarget(100, aim, 40f, 0f, 1, 1, false)));

                arena.Advance(comboAt.Value - 0.1f);

                await Task.Delay(100);

                Assert.HasCount(1, arena.Starts(), "Not before the end may be cut.");

                arena.Advance(0.2f);

                await MetricAssertions.UntilAsync(() => arena.Starts().Length >= (chains is null ? 1 : 2), "The skills are told.");
                await Task.Delay(100);

                var told = arena.Starts().Select(e => BinaryPrimitives.ReadInt32LittleEndian(e.Body.AsSpan(4))).ToArray();

                CollectionAssert.AreEqual(chains is null ? new[] { ComboSkill } : new[] { ComboSkill, ComboNext }, told);
                Assert.IsTrue(arena.Body.IsAttacking, "The monster plays on.");
            }, scar, skills, actions, chains);
        }
    }

    /// <summary>
    /// An attack that brings in a monster brings it in at its moment, somewhere round the centre of the attack's space, in the sector of
    /// the monster, and the clients are shown it.
    /// </summary>
    [TestMethod]
    public async Task AnAttackThatBringsInAMonsterBringsItRoundTheCentreOfItsSpace()
    {
        if (Data(SummonSkill) is not var (skills, actions))
        {
            Assert.Inconclusive("OPENWORKER_TEST_RES and OPENWORKER_TEST_ACTION are not set.");

            return;
        }

        var summoner = TestRows.Build<MonsterRow>(("Id", Monster), ("AiScript", "Ai_Still"), ("ActionFile", SummonerFile), ("Skill1", SummonSkill));
        var puppet = TestRows.Build<MonsterRow>(("Id", Summoned), ("AiScript", "Ai_Still"));
        var at = new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance) { Folder = actions }.GetTimeline(SummonSkill, SummonerFile).Summons;

        Assert.HasCount(2, at);

        await WithArenaAsync(async arena =>
        {
            var start = arena.Body.Position;
            var aim = start + new Vector3(0f, -3000f, 0f);

            Assert.IsTrue(arena.Body.UseSkill(arena.Body.GetSkill(0)!, new AiTarget(100, aim, 40f, 0f, 1, 1, false)));

            arena.Advance(at[0].Time - 0.1f);

            Assert.HasCount(1, arena.Maze.Creatures.FindMonsters((_, _) => true), "Nobody is brought in before the moment.");

            arena.Advance(0.2f);

            var brought = arena.Maze.Creatures.FindMonsters((_, _) => true).Where(e => e != arena.Body.Entity).ToArray();

            Assert.HasCount(2, brought);

            // The left one is brought in round the left point, the right one round the right point.
            var places = brought.Select(e => arena.World.Get<WorldComponent>(e).Position).OrderBy(e => e.X).ToArray();
            var centers = at.Select(e => arena.Body.Position + TrapLayout.Turn(e.Trigger.Center with { Z = 0f }, arena.Body.Yaw)).OrderBy(e => e.X).ToArray();

            for (var i = 0; i < 2; i++)
            {
                Assert.IsLessThanOrEqualTo(at[i].Trigger.Radius + 1f, MathF.Abs(places[i].X - centers[i].X));
                Assert.IsLessThanOrEqualTo(at[i].Trigger.Radius + 1f, MathF.Abs(places[i].Y - centers[i].Y));
            }

            await MetricAssertions.UntilAsync(() => arena.Shown() >= 3, "The clients are shown them, after the monster itself.");
        }, summoner, skills, actions, other: puppet);
    }
}
