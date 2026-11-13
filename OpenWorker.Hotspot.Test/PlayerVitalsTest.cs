using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Lua.Managers;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

[TestClass]
[DoNotParallelize]
public sealed class PlayerVitalsTest
{
    private const string TablesVariable = "OPENWORKER_TEST_RES";

    private static ReadOnlyCollection<T> Load<T>(string folder, string table)
    {
        using var reader = new BinaryReader(File.OpenRead(Path.Join(folder, $"{table}.res")));

        var rows = new T[reader.ReadInt32()];

        for (var index = 0; index < rows.Length; index++)
        {
            rows[index] = (T)Activator.CreateInstance(typeof(T), reader)!;
        }

        return new ReadOnlyCollection<T>(rows);
    }

    private static CombatTables Tables()
    {
        var folder = Environment.GetEnvironmentVariable(TablesVariable);

        if (folder is null)
        {
            return new CombatTables(new ReadOnlyCollection<MonsterRow>([]), new ReadOnlyCollection<StatusRow>([]), new ReadOnlyCollection<SkillRow>([]), new ReadOnlyCollection<CharacterInfoRow>([]));
        }

        return new CombatTables(
            Load<MonsterRow>(folder, "tb_Monster"),
            Load<StatusRow>(folder, "tb_Status"),
            Load<SkillRow>(folder, "tb_Skill"),
            Load<CharacterInfoRow>(folder, "tb_Character_Info"));
    }

    private static (World World, Entity Player, PlayerVitals Vitals) Create()
    {
        var world = World.Create();
        var player = world.Create(
            new AbilityComponent
            {
                Health = new AbilityComponentEntry { Current = 100, Max = 100 },
                SoulGain = new AbilityComponentEntry { Current = 0, Max = 0 },
                SoulVapor = new AbilityComponentEntry { Current = 0, Max = 0 },
                Stamina = new AbilityComponentEntry { Current = 0, Max = 0 },
                SuperArmor = new AbilityComponentEntry { Current = 0, Max = 0 },
                Speed = new SpeedComponentEntry { Attack = 1, Move = 1 }
            },
            new PersonInfoComponent { Name = "Haru", Hero = Hero.Haru },
            new LevelComponent(25, 0),
            new WorldComponent { Location = 1, Position = new Vector3(1, 2, 3), Rotation = 0, Jump = 0 });

        return (world, player, new PlayerVitals(world, Tables(), () => TimeSpan.Zero));
    }

    [TestMethod]
    public void AnEnteringPlayerGetsTheLifeOfTheirClass()
    {
        var (world, player, vitals) = Create();

        try
        {
            vitals.Initialize(player);

            Assert.AreEqual(vitals.MaxHealth(player), vitals.Health(player));

            if (Environment.GetEnvironmentVariable(TablesVariable) is not null)
            {
                Assert.IsGreaterThan(1150, vitals.MaxHealth(player), "A character of level 25 has more life than one of level 1.");
            }
        }

        finally
        {
            World.Destroy(world);
        }
    }

    [TestMethod]
    public void LifeNeverGoesBelowZeroAndAFallenPlayerIsNotAlive()
    {
        var (world, player, vitals) = Create();

        try
        {
            Assert.IsTrue(vitals.IsAlive(player));
            Assert.AreEqual(60, vitals.Hurt(player, 40));
            Assert.AreEqual(0, vitals.Hurt(player, 1000));
            Assert.IsFalse(vitals.IsAlive(player));
            Assert.AreEqual(0, vitals.Hurt(player, 10));
        }

        finally
        {
            World.Destroy(world);
        }
    }

    [TestMethod]
    public void ARevivedPlayerHasAllTheirLifeAndFewSecondsOfPeace()
    {
        var (world, player, vitals) = Create();

        try
        {
            vitals.Hurt(player, 1000);
            Assert.IsFalse(vitals.IsAlive(player));
            Assert.IsFalse(vitals.IsProtected(player));

            vitals.Restore(player);
            vitals.Protect(player);

            Assert.IsTrue(vitals.IsAlive(player));
            Assert.AreEqual(100, vitals.Health(player));
            Assert.IsTrue(vitals.IsProtected(player));

            vitals.Forget(player);

            Assert.IsFalse(vitals.IsProtected(player));
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
