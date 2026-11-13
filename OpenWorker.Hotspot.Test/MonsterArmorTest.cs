using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Channels.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The super armor of a monster of a maze, hit by a player: it holds and shows no reaction until its gauge is empty, breaks, and
/// then lets everything through.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MonsterArmorTest
{
    private const int Monster = 99001;

    internal static Entity Player(World world)
    {
        var life = new AbilityComponentEntry { Current = 100, Max = 100 };

        return world.Create(
            new ActorComponent(1, ActorType.User),
            new ServerSessionComponent(new RecordingConnection()),
            new WorldComponent { Location = 0, Position = Vector3.Zero, Rotation = 0f, Jump = 0 },
            new LevelComponent(20, 0),
            new PersonInfoComponent { Name = "Fighter", Hero = Hero.Haru },
            new AbilityComponent { Health = life, SoulGain = life, SoulVapor = life, Stamina = life, SuperArmor = life, Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f } });
    }

    private static Blow Strike(short hardness)
    {
        var push = new AttackReaction(120f, 0f, 0f, 0f, AttackReaction.StraightAlongAttacker, 0f, 0f, 0f, 0f, 0f, 0, 0, false, hardness, false);

        return new Blow(push, ReactionKind.Flinch, 1f, 90f, Vector3.Zero, Vector3.Zero, 0f, false);
    }

    /// <summary>
    /// A blow that is not hard lowers the gauge and moves nobody while the armor holds; the blow that empties the gauge breaks the
    /// armor and pushes the monster like any other, and the hits that follow find it broken.
    /// </summary>
    [TestMethod]
    public void AnArmorHoldsUntilItsGaugeIsEmptyAndThenBreaks()
    {
        var world = World.Create();

        try
        {
            var maze = TestMazes.Create(world, Player(world), map: PartyMazeTest.MapWithMonster(Monster));

            maze.SpawnSector(1);

            var monster = maze.Creatures.FindMonsters((_, _) => true).Single();
            var armor = world.Get<MonsterCombatComponent>(monster) with { Defense = DefenseType.SuperArmor, MaxSuperArmor = 1000f, SuperArmor = 1000f };

            world.Set(monster, armor);

            var held = maze.Creatures.ReceiveBlow(monster, Strike(1), DefenseType.SuperArmor, 400, 100f, true);

            Assert.AreEqual((byte)DefenseType.SuperArmor, held.Defense);
            Assert.AreEqual(600f, held.SuperArmor);

            var place = world.Get<WorldComponent>(monster).Position;

            Assert.AreEqual(new Vector3(place.X, place.Y, 0f), held.ExtraMove, "A monster whose armor holds is told to stay where it is.");
            Assert.IsNull(world.Get<MonsterCombatComponent>(monster).SuperArmorBrokenUntil);

            var broken = maze.Creatures.ReceiveBlow(monster, Strike(1), DefenseType.SuperArmor, 700, 100f, true);

            Assert.AreEqual((byte)DefenseType.Normal, broken.Defense, "The armor is gone.");
            Assert.AreEqual(0f, broken.SuperArmor);
            Assert.IsNotNull(world.Get<MonsterCombatComponent>(monster).SuperArmorBrokenUntil, "The armor takes its time to come back.");

            var after = maze.Creatures.ReceiveBlow(monster, Strike(1), DefenseType.SuperArmor, 100, 100f, true);

            Assert.AreEqual((byte)DefenseType.Normal, after.Defense, "A broken armor takes nothing more.");
            Assert.AreEqual(0f, after.SuperArmor);
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// An armor that broke is whole again for the first blow that lands after its time is up, before the maze steps: the
    /// clients count that time themselves and break an armor they hold whole on a hit that says it is empty. The time is the
    /// maze's: while its clock stands, the armor stays broken.
    /// </summary>
    [TestMethod]
    public void ABlowAfterTheTimeOfABrokenArmorFindsItWhole()
    {
        var world = World.Create();

        try
        {
            var time = new FakeTimeProvider();
            var maze = TestMazes.Create(world, Player(world), map: PartyMazeTest.MapWithMonster(Monster), clock: TestMazes.Clock(time));

            maze.SpawnSector(1);

            var monster = maze.Creatures.FindMonsters((_, _) => true).Single();

            world.Set(monster, world.Get<MonsterCombatComponent>(monster) with { Defense = DefenseType.SuperArmor, MaxSuperArmor = 1000f, SuperArmor = 1000f, SuperArmorRecovery = 1 });

            var broken = maze.Creatures.ReceiveBlow(monster, Strike(1), DefenseType.SuperArmor, 1000, 100f, true);

            Assert.AreEqual((byte)DefenseType.Normal, broken.Defense);

            Thread.Sleep(50);

            Assert.AreEqual((byte)DefenseType.Normal, maze.Creatures.ReceiveBlow(monster, Strike(1), DefenseType.SuperArmor, 400, 100f, true).Defense,
                "The clock of the maze has not moved.");

            time.Advance(TimeSpan.FromMilliseconds(50));

            var held = maze.Creatures.ReceiveBlow(monster, Strike(1), DefenseType.SuperArmor, 400, 100f, true);

            Assert.AreEqual((byte)DefenseType.SuperArmor, held.Defense, "The armor that came back holds.");
            Assert.AreEqual(600f, held.SuperArmor);
            Assert.IsNull(world.Get<MonsterCombatComponent>(monster).SuperArmorBrokenUntil);
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A hard blow breaks an armor that is full.
    /// </summary>
    [TestMethod]
    public void AHardBlowBreaksAFullArmor()
    {
        var world = World.Create();

        try
        {
            var maze = TestMazes.Create(world, Player(world), map: PartyMazeTest.MapWithMonster(Monster));

            maze.SpawnSector(1);

            var monster = maze.Creatures.FindMonsters((_, _) => true).Single();

            world.Set(monster, world.Get<MonsterCombatComponent>(monster) with { Defense = DefenseType.SuperArmor, MaxSuperArmor = 1000f, SuperArmor = 1000f });

            var effect = maze.Creatures.ReceiveBlow(monster, Strike(3), DefenseType.SuperArmor, 1, 0f, true);

            Assert.AreEqual((byte)DefenseType.Normal, effect.Defense);
            Assert.AreEqual(0f, effect.SuperArmor);
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
