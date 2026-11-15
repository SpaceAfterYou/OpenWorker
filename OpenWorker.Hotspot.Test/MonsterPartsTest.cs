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
using OpenWorker.Gameplay.Modules.Helpers;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Modules.Helpers.Types;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Helpers;
using OpenWorker.Lua.Managers;
using OpenWorker.Navigation;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The parts of a monster: the hit bones a blow lands on, the life of the parts, and what a broken part turns the monster into.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MonsterPartsTest
{
    /// <summary>
    /// The Deus Ex Machina with both hands: its left hand is part 1, its right hand part 2.
    /// </summary>
    private const int Machina = 30404411;

    /// <summary>
    /// The Deus Ex Machina that has lost its left hand.
    /// </summary>
    private const int RightHanded = 30404412;

    /// <summary>
    /// The body of the Deus Ex Machina, both hands lost.
    /// </summary>
    private const int Body = 30404414;

    private const string MachinaFile = "MOB_9037_DeusExMachina_All";

    private static readonly AttackTrigger Circle = new(0, 0, 0, 0, 0f, 360f, Vector3.Zero, 0, 0, 450, 200, 0);

    private static string Folder(string variable)
    {
        var folder = Environment.GetEnvironmentVariable(variable);

        if (string.IsNullOrEmpty(folder))
        {
            Assert.Inconclusive($"{variable} is not set.");
        }

        return folder;
    }

    [TestMethod]
    public void ABlowLandsOnThePartOfTheNearestBoneUnlessTheCylinderIsNearer()
    {
        var area = new AttackArea(Circle, Vector3.Zero, 0f);
        BonePlace[] bones = [new(new Vector3(0, 460, 50), 30, 2), new(new Vector3(0, 300, 50), 30, 1)];

        Assert.IsTrue(area.Strikes(new Vector3(0, 2000, 0), 50, 200, bones, out var part), "the bones are hit where the cylinder is not");
        Assert.AreEqual((byte)1, part);

        Assert.IsTrue(area.Strikes(new Vector3(0, 100, 0), 50, 200, bones, out part));
        Assert.AreEqual((byte)0, part, "the cylinder is nearer than any bone");

        Assert.IsFalse(area.Strikes(new Vector3(0, 0, 1000), 50, 200, [], out _), "a body above the area without bones is not hit");
        Assert.IsTrue(area.Strikes(new Vector3(0, 0, 1000), 50, 200, [new(new Vector3(0, 100, 100), 30, 2)], out part), "its bones still are");
        Assert.AreEqual((byte)2, part);
    }

    [TestMethod]
    public void TheHitBonesOfAMonsterComeFromTheFileBesideItsActionFileAndItsAnimations()
    {
        var actions = new SkillActionProvider(TestTables.Load<SkillRow>("tb_Skill"), NullLogger<SkillActionProvider>.Instance) { Folder = Folder("OPENWORKER_TEST_ACTION") };

        var body = actions.GetHitBody(MachinaFile);

        Assert.IsNotNull(body);
        CollectionAssert.AreEqual(new byte[] { 1, 1, 2, 2, 2, 1 }, body.Bones.Select(e => e.Part).ToArray());
        Assert.IsTrue(body.HasCylinder);

        var stand = actions.GetAnimation(MachinaFile, "N_Stand")?.Bones;

        Assert.IsNotNull(stand);
        Assert.AreEqual(body.Bones.Count, stand.Bones);
        Assert.IsGreaterThan(0, stand.Count);
        Assert.IsGreaterThan(300f, stand.At(0, 0f).X, "the left hand is on the left of the model");
        Assert.IsLessThan(-300f, stand.At(2, 0f).X, "the right hand on the right");
    }

    [TestMethod]
    public void APartTakesTheBlowsOnItsBonesAndItsBreakChangesTheMonster()
    {
        var rules = new MonsterPartRules(TestTables.Load<MonsterRow>("tb_Monster"), TestTables.Load<MonsterBrokenPartsRow>("tb_Monster_Broken_Parts"));

        var parts = rules.Create(Machina, 10_000);

        Assert.AreEqual(new MonsterPart(30403041, 1, 5_000, 5_000), parts.First);
        Assert.AreEqual(new MonsterPart(30403042, 2, 5_000, 5_000), parts.Second);
        Assert.IsFalse(rules.Create(Body, 10_000).Any, "the body has no part");

        parts = MonsterPartRules.Hit(parts, 0, 3_000, out var struck);

        Assert.IsFalse(struck, "a blow on no part leaves the parts");

        parts = MonsterPartRules.Hit(parts, 1, 3_000, out struck);

        Assert.IsTrue(struck);
        Assert.AreEqual(2_000, parts.First.Health);
        Assert.AreEqual(5_000, parts.Second.Health);
        Assert.IsNull(rules.BreakOf(parts));

        parts = MonsterPartRules.Hit(parts, 1, 9_000, out _);

        Assert.AreEqual(0, parts.First.Health);
        Assert.AreEqual(new PartBreak(RightHanded, "B_Death_Left"), rules.BreakOf(parts), "the left hand off, the right one stands");

        var right = MonsterPartRules.Hit(rules.Create(RightHanded, 10_000), 2, 99_999, out _);

        Assert.AreEqual(new PartBreak(Body, "B_Death_Left_Right"), rules.BreakOf(right), "the last hand off leaves the body");
    }

    /// <summary>
    /// A maze with the real tables, files and minds, a player in it and the Deus Ex Machina with both hands, standing still.
    /// </summary>
    private sealed record Stage(World World, LuaMaze Maze, LuaCreatureManager Creatures, SkillActionProvider Actions, Entity Player, RecordingConnection Connection,
        Entity Machina, FakeTimeProvider Time, SimulationClock Clock, HelperStats Helpers)
    {
        public void Advance(TimeSpan span)
        {
            for (var passed = TimeSpan.Zero; passed < span; passed += SimulationClock.Period)
            {
                Time.Advance(SimulationClock.Period);
                Clock.Tick();
            }
        }

        /// <summary>
        /// An area of a blow around the left hand, where the animation the monster stands in puts it.
        /// </summary>
        public AttackArea AroundTheLeftHand()
        {
            var hand = Actions.GetAnimation(MachinaFile, "N_Stand")!.Bones!.At(0, 0f);

            return new AttackArea(Circle with { Radius = 100f, Height = 2000f, Center = new Vector3(0, 0, -500) }, World.Get<WorldComponent>(Machina).Position + hand with { Z = 0f }, 0f);
        }
    }

    private static async Task WithMachinaAsync(Func<Stage, Task> test)
    {
        var actionFolder = Folder("OPENWORKER_TEST_ACTION");
        var aiFolder = Folder("OPENWORKER_TEST_AI");
        var monsters = TestTables.Load<MonsterRow>("tb_Monster");
        var skills = TestTables.Load<SkillRow>("tb_Skill");
        var statuses = TestTables.Load<StatusRow>("tb_Status");
        var time = new FakeTimeProvider();
        var clock = TestMazes.Clock(time);
        var combat = new CombatTables(monsters, statuses, skills, TestTables.Load<CharacterInfoRow>("tb_Character_Info"));
        var actions = new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance) { Folder = actionFolder };
        var services = new CreatureServices(
            new AiScriptLoader(NullLogger<AiScriptLoader>.Instance, aiFolder),
            new MonsterBehaviorTables(monsters, skills, TestTables.Load<FactionRow>("tb_Faction")),
            actions,
            clock,
            new NavMeshProvider(),
            parts: new MonsterPartRules(monsters, TestTables.Load<MonsterBrokenPartsRow>("tb_Monster_Broken_Parts")));
        var world = World.Create();

        try
        {
            var connection = new RecordingConnection();
            var life = new AbilityComponentEntry { Current = 100_000, Max = 100_000 };
            var player = world.Create(
                new ActorComponent(1, ActorType.User),
                new ServerSessionComponent(connection),
                new WorldComponent { Location = 0, Position = new Vector3(1000f, 1000f, 0f), Rotation = 0f, Jump = 0 },
                new LevelComponent(60, 0),
                new PersonInfoComponent { Name = "Master", Hero = Hero.Haru },
                new AbilityComponent { Health = life, SoulGain = life, SoulVapor = life, Stamina = life, SuperArmor = life, Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f } });
            var batch = TestMazes.Map();
            var creatures = new LuaCreatureManager(world, batch, combat, services);
            var maze = TestMazes.Create(world, player, clock: clock, batch: batch, creatures: creatures, tables: combat, actions: actions);

            maze.MarkLoaded(player);
            maze.TryStart();

            Assert.IsTrue(maze.CallMonsters(player, Machina, 1, still: true));

            var helpers = new HelperStats(HelperCatalog.From([Mirium]), monsters, statuses);

            await test(new Stage(world, maze, creatures, actions, player, connection, creatures.Living.Single(), time, clock, helpers));

            maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// Mirium, the first helper a person gets.
    /// </summary>
    private const int Mirium = 10105166;

    [TestMethod]
    public Task AMonsterWithPartsKeepsItsLifeAndIsChangedWhenAPartBreaks() =>
        WithMachinaAsync(async stage =>
        {
            var (world, creatures, machina) = (stage.World, stage.Creatures, stage.Machina);
            var place = world.Get<WorldComponent>(machina).Position;
            var health = world.Get<CreatureComponent>(machina).Health;
            var actor = world.Get<ActorComponent>(machina).Identifier;

            Assert.AreEqual((int)(health * 0.5f), creatures.PartsOf(machina).First.MaxHealth, "each hand has half the life of the monster");

            CollectionAssert.AreEqual(new[] { machina }, creatures.MonstersStruck(stage.AroundTheLeftHand()));
            Assert.AreEqual((byte)1, creatures.StruckPart(machina));

            Assert.IsTrue(creatures.TakeOnParts(machina, 1, 10_000_000));
            Assert.AreEqual(health, world.Get<CreatureComponent>(machina).Health, "the life of the monster is not touched");

            for (var passed = TimeSpan.Zero; passed < TimeSpan.FromSeconds(30) && world.IsAlive(machina); passed += SimulationClock.Period)
            {
                stage.Advance(SimulationClock.Period);
            }

            Assert.IsFalse(world.IsAlive(machina), "the monster is changed once the animation of the break is over");

            var changed = creatures.Living.Single();

            Assert.AreEqual(RightHanded, world.Get<CreatureComponent>(changed).Identifier);
            Assert.AreEqual(place, world.Get<WorldComponent>(changed).Position);
            Assert.AreEqual(Math.Min(health, (int)world.Get<MonsterCombatComponent>(changed).Stats.MaxHealth), world.Get<CreatureComponent>(changed).Health);
            Assert.IsGreaterThan(0, creatures.PartsOf(changed).First.Health, "the hand left has its own life");

            await QueueMarker.SettleAsync(world.Get<ServerSessionComponent>(stage.Player), stage.Connection);

            var frames = stage.Connection.Frames.Select(PlayerFrame.Open).ToArray();
            var parts = frames.Where(e => e.Is(GroupOpcode.Monster, (byte)MonsterOpcode.PartsHp)).Select(e => e.Actor).ToArray();

            CollectionAssert.AreEqual(new[] { actor, world.Get<ActorComponent>(changed).Identifier }, parts, "the parts of the hit monster, then of the new one");
            Assert.IsTrue(frames.Any(e => e.Is(GroupOpcode.World, (byte)WorldOpcode.OutInfoMonster) && e.Actors.SequenceEqual(new[] { actor })), "the first goes");
        });

    /// <summary>
    /// A blow of a helper lands on a part as a blow of a player does: the part it found when it struck takes the damage when the
    /// strike is dealt, a step later, and the life of the monster is not touched.
    /// </summary>
    [TestMethod]
    public Task AHelperBlowLandsOnThePartItStruck() =>
        WithMachinaAsync(stage =>
        {
            var helper = stage.Maze.SummonHelper(stage.Player, stage.Helpers.Combat(Mirium, 60, []), new HelperValue { Helper = Mirium, Summoned = true });

            if (helper?.Body is not { } body || body.GetSkill(0) is not { } skill)
            {
                Assert.Inconclusive("Mirium has no mind or skill here.");

                return Task.CompletedTask;
            }

            var health = stage.World.Get<CreatureComponent>(stage.Machina).Health;
            var hand = stage.Creatures.PartsOf(stage.Machina).First.Health;
            var parts = new List<byte>();
            var struck = stage.Maze.Helpers.EnemiesStruck(body, stage.AroundTheLeftHand(), parts);

            CollectionAssert.AreEqual(new[] { stage.Machina }, struck);
            CollectionAssert.AreEqual(new byte[] { 1 }, parts, "the blow is found on the left hand");

            var trigger = new AttackTrigger(1, 0, 0, 0, 0f, 360f, Vector3.Zero, 0, 0, 100, 100, 0, DamageRate: 10f, ReactionType: 0);

            // A blow may miss: a few are dealt.
            for (var blow = 0; blow < 10 && stage.Creatures.PartsOf(stage.Machina).First.Health == hand; blow++)
            {
                stage.Maze.Helpers.Queue(new HelperStrike(helper, skill.Id, trigger, struck, [.. parts]));
                stage.Advance(SimulationClock.Period * 2);
            }

            Assert.IsLessThan(hand, stage.Creatures.PartsOf(stage.Machina).First.Health, "the hand took the blow");
            Assert.AreEqual(health, stage.World.Get<CreatureComponent>(stage.Machina).Health, "the monster did not");

            return Task.CompletedTask;
        });

    /// <summary>
    /// A shot and a link of a chain that run into a bone of a monster land on the part of that bone, wherever its cylinder is.
    /// </summary>
    [TestMethod]
    public void AShotAndALinkOfAChainLandOnThePartOfTheBoneTheyRunInto()
    {
        // The cylinder is far ahead; the bone of part 2 reaches out to 400 in front of the shooter.
        var target = new FlightTarget(7, new Vector3(0, -2000, 0), 50, 200, [new BonePlace(new Vector3(0, -400, 50), 60, 2)]);
        var flight = new ProjectileInfo(Vector3.Zero, 1000f, 1f, false, 1, 10f, false, 0f, false, false, 0, Vector3.Zero, false, 0);
        var shot = new AttackTrigger(1, 0, 0, 0, 0f, 360f, Vector3.Zero, 0, 0, 100, 100, 0, Projectile: flight);
        var projectile = ProjectileFlight.LaunchByFacing(shot, new Vector3(0, 0, 50), Vector3.UnitX, 0f);
        var explosions = new List<Explosion>();

        for (var i = 0; i < 20 && explosions.Count == 0; i++)
        {
            projectile.Advance(0.05f, [target], explosions);
        }

        Assert.AreEqual(new Explosion(explosions.Single().Position, 7, 2), explosions.Single());
        Assert.IsLessThan(500f, -explosions.Single().Position.Y, "it stopped at the bone, not at the cylinder");

        var chain = ChainLightning.Fire(shot with { Radius = 1000f, Height = 200f, Projectile = flight with { Speed = 3500f }, Chain = new ChainInfo(ChainInfo.OneByOne, 3f, 1, 0.5f, Vector3.Zero, 300f, 0f, false) },
            Vector3.Zero, 0f, [target]);
        var hits = new List<ChainHit>();

        for (var i = 0; i < 20 && hits.Count == 0; i++)
        {
            chain.Advance(0.05f, [target], Vector3.Zero, 0f, hits, []);
        }

        Assert.AreEqual(new ChainHit(7, 0, 2), hits.Single());
    }
}
