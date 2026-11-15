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

    [TestMethod]
    public async Task AMonsterWithPartsKeepsItsLifeAndIsChangedWhenAPartBreaks()
    {
        var actionFolder = Folder("OPENWORKER_TEST_ACTION");
        var aiFolder = Folder("OPENWORKER_TEST_AI");
        var monsters = TestTables.Load<MonsterRow>("tb_Monster");
        var skills = TestTables.Load<SkillRow>("tb_Skill");
        var time = new FakeTimeProvider();
        var clock = TestMazes.Clock(time);
        var combat = new CombatTables(monsters, TestTables.Load<StatusRow>("tb_Status"), skills, TestTables.Load<CharacterInfoRow>("tb_Character_Info"));
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

            var machina = creatures.Living.Single();
            var place = world.Get<WorldComponent>(machina).Position;
            var health = world.Get<CreatureComponent>(machina).Health;
            var actor = world.Get<ActorComponent>(machina).Identifier;

            Assert.AreEqual((int)(health * 0.5f), creatures.PartsOf(machina).First.MaxHealth, "each hand has half the life of the monster");

            // A blow around the left hand, where the animation the monster stands in puts it.
            var hand = actions.GetAnimation(MachinaFile, "N_Stand")!.Bones!.At(0, 0f);
            var area = new AttackArea(Circle with { Radius = 100f, Height = 2000f, Center = new Vector3(0, 0, -500) }, place + hand with { Z = 0f }, 0f);

            CollectionAssert.AreEqual(new[] { machina }, creatures.MonstersStruck(area));
            Assert.AreEqual((byte)1, creatures.StruckPart(machina));

            Assert.IsTrue(creatures.TakeOnParts(machina, 1, 10_000_000));
            Assert.AreEqual(health, world.Get<CreatureComponent>(machina).Health, "the life of the monster is not touched");

            for (var passed = TimeSpan.Zero; passed < TimeSpan.FromSeconds(30) && world.IsAlive(machina); passed += SimulationClock.Period)
            {
                time.Advance(SimulationClock.Period);
                clock.Tick();
            }

            Assert.IsFalse(world.IsAlive(machina), "the monster is changed once the animation of the break is over");

            var changed = creatures.Living.Single();

            Assert.AreEqual(RightHanded, world.Get<CreatureComponent>(changed).Identifier);
            Assert.AreEqual(place, world.Get<WorldComponent>(changed).Position);
            Assert.AreEqual(Math.Min(health, (int)world.Get<MonsterCombatComponent>(changed).Stats.MaxHealth), world.Get<CreatureComponent>(changed).Health);
            Assert.IsGreaterThan(0, creatures.PartsOf(changed).First.Health, "the hand left has its own life");

            await QueueMarker.SettleAsync(world.Get<ServerSessionComponent>(player), connection);

            var frames = connection.Frames.Select(PlayerFrame.Open).ToArray();
            var parts = frames.Where(e => e.Is(GroupOpcode.Monster, (byte)MonsterOpcode.PartsHp)).Select(e => e.Actor).ToArray();

            CollectionAssert.AreEqual(new[] { actor, world.Get<ActorComponent>(changed).Identifier }, parts, "the parts of the hit monster, then of the new one");
            Assert.IsTrue(frames.Any(e => e.Is(GroupOpcode.World, (byte)WorldOpcode.OutInfoMonster) && e.Actors.SequenceEqual(new[] { actor })), "the first goes");

            maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
