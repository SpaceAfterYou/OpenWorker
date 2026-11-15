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
using OpenWorker.Lua;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;
using OpenWorker.Navigation;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Monsters of the real tables and minds round a player who stands still keep fighting him: each keeps choosing what to do,
/// and every skill one chooses it plays, whatever way it looks when it chooses.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MonsterAttackRateTest
{
    /// <summary>
    /// The Imp Puppet: a small monster whose script sends it walking round its target between its blows.
    /// </summary>
    private const int Imp = 30103001;

    private const int Count = 6;

    private const int Seed = 7;

    /// <summary>
    /// The bit the wire sets on the actor of a monster.
    /// </summary>
    private const int MonsterActors = 1 << 30;

    [TestMethod]
    public async Task SixImpsRoundAStandingPlayerKeepChoosingAndPlayEverySkillTheyChoose()
    {
        var folder = Environment.GetEnvironmentVariable("OPENWORKER_TEST_AI");
        var actionFolder = Environment.GetEnvironmentVariable("OPENWORKER_TEST_ACTION");

        if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(actionFolder))
        {
            Assert.Inconclusive("OPENWORKER_TEST_AI or OPENWORKER_TEST_ACTION is not set.");
        }

        var monsters = TestTables.Load<MonsterRow>("tb_Monster");
        var skills = TestTables.Load<SkillRow>("tb_Skill");
        var time = new FakeTimeProvider();
        var clock = TestMazes.Clock(time);
        var combat = new CombatTables(monsters, TestTables.Load<StatusRow>("tb_Status"), skills, TestTables.Load<CharacterInfoRow>("tb_Character_Info"));
        var services = new CreatureServices(
            new AiScriptLoader(NullLogger<AiScriptLoader>.Instance, folder),
            new MonsterBehaviorTables(monsters, skills, TestTables.Load<FactionRow>("tb_Faction")),
            new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance) { Folder = actionFolder },
            clock,
            new NavMeshProvider());
        var world = World.Create();

        try
        {
            var connection = new RecordingConnection();
            var life = new AbilityComponentEntry { Current = 100_000_000, Max = 100_000_000 };
            var player = world.Create(
                new ActorComponent(1, ActorType.User),
                new ServerSessionComponent(connection),
                new WorldComponent { Location = 0, Position = new Vector3(1000f, 1000f, 0f), Rotation = 0f, Jump = 0 },
                new LevelComponent(60, 0),
                new PersonInfoComponent { Name = "Master", Hero = Hero.Haru },
                new AbilityComponent { Health = life, SoulGain = life, SoulVapor = life, Stamina = life, SuperArmor = life, Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f } });
            var batch = TestMazes.Map();
            var creatures = new LuaCreatureManager(world, batch, combat, services) { Random = new Random(Seed) };
            var maze = TestMazes.Create(world, player, clock: clock, batch: batch, creatures: creatures, tables: combat);

            maze.MarkLoaded(player);
            maze.TryStart();

            Assert.IsTrue(maze.CallMonsters(player, Imp, Count));

            var counting = false;
            var decisions = new Dictionary<int, int>();
            var chosen = 0;
            var finished = 0;
            var choices = new HashSet<int>();

            foreach (var imp in creatures.Living)
            {
                var id = imp.Id;

                creatures.BodyOf(imp)!.Machine.StateChanged += (old, state) =>
                {
                    if (!counting)
                    {
                        return;
                    }

                    if (IsSkill(old) && choices.Remove(id))
                    {
                        finished++;
                    }

                    if (state == AiState.Attack)
                    {
                        decisions[id] = decisions.GetValueOrDefault(id) + 1;
                    }
                    else if (IsSkill(state))
                    {
                        chosen++;
                        choices.Add(id);
                    }
                };
            }

            // The player steps off a little and stands; the imps come up to him, then fifteen seconds of the fight are counted.
            world.Set(player, world.Get<WorldComponent>(player) with { Position = new Vector3(1500f, 1000f, 0f) });

            Run(time, clock, TimeSpan.FromSeconds(5));

            await QueueMarker.SettleAsync(world.Get<ServerSessionComponent>(player), connection);

            var before = connection.Frames.Length;

            counting = true;

            Run(time, clock, TimeSpan.FromSeconds(15));

            await QueueMarker.SettleAsync(world.Get<ServerSessionComponent>(player), connection);

            var played = connection.Frames.Skip(before).Select(PlayerFrame.Open)
                .Count(e => e.Is(GroupOpcode.Skill, (byte)SkillOpcode.ActiveSkillBt) && (BitConverter.ToInt32(e.Body, 0) & MonsterActors) != 0);

            Assert.HasCount(Count, decisions, "each imp keeps choosing what to do");
            Assert.IsGreaterThanOrEqualTo(4, decisions.Values.Min(), "every two or three seconds at least");
            Assert.IsGreaterThanOrEqualTo(finished, played, "every skill an imp chose and is done with was played");
            Assert.IsGreaterThanOrEqualTo(1, played, $"skills {played}, chosen {chosen}, decisions {decisions.Values.Sum()}");

            maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    private static bool IsSkill(AiState state) => state is >= AiState.AttackSkill1 and <= AiState.AttackSkill10;

    private static void Run(FakeTimeProvider time, SimulationClock clock, TimeSpan span)
    {
        for (var passed = TimeSpan.Zero; passed < span; passed += SimulationClock.Period)
        {
            time.Advance(SimulationClock.Period);
            clock.Tick();
        }
    }
}
