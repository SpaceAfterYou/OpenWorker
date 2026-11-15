using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Movement;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Where a monster takes the player it fights to be.
/// </summary>
[TestClass]
public sealed class MonsterAimTest
{
    private const int Person = 100;

    /// <summary>
    /// A player that runs is seen where it has run to since its last move, as far as the end of the run; a player put anywhere
    /// since is seen where it was put.
    /// </summary>
    [TestMethod]
    public void AMonsterSeesARunningPlayerWhereItHasRunTo()
    {
        var world = World.Create();

        try
        {
            var time = new FakeTimeProvider();
            var clock = TestMazes.Clock(time);
            var player = world.Create(
                new ActorComponent(Person, ActorType.User),
                new WorldComponent { Location = 0, Position = Vector3.Zero, Rotation = 0f, Jump = 0 },
                new AbilityComponent
                {
                    Health = new AbilityComponentEntry { Current = 100, Max = 100 },
                    SoulGain = default,
                    SoulVapor = default,
                    Stamina = default,
                    SuperArmor = default,
                    Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f }
                });
            var maze = TestMazes.Create(world, player, clock: clock);

            world.Add(player, new PlayerRunComponent(Vector3.Zero, new Vector2(300f, 0f), 400f, clock.Time));

            Advance(time, clock, 0.5f);

            var seen = maze.Creatures.FindPlayer(Person)!.Value;

            Assert.AreEqual(200f, seen.Position.X, 1f, "Half a second at 400.");
            Assert.IsTrue(seen.IsMoving);

            Advance(time, clock, 0.5f);

            Assert.AreEqual(300f, maze.Creatures.FindPlayer(Person)!.Value.Position.X, 0.01f, "No farther than the end of the run.");

            world.Set(player, world.Get<WorldComponent>(player) with { Position = new Vector3(-50f, 0f, 0f) });

            Assert.AreEqual(-50f, maze.Creatures.FindPlayer(Person)!.Value.Position.X, "Put somewhere since.");
        }

        finally
        {
            World.Destroy(world);
        }
    }

    private static void Advance(FakeTimeProvider time, SimulationClock clock, float seconds)
    {
        for (var step = 0; step < seconds / SimulationClock.Period.TotalSeconds - 0.001; step++)
        {
            time.Advance(SimulationClock.Period);
            clock.Tick();
        }
    }
}
