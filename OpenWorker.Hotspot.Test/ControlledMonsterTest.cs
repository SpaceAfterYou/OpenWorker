using System.Buffers.Binary;
using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;
using OpenWorker.Navigation;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A monster the player controls plays its skill as the clients play it.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ControlledMonsterTest
{
    private const string ActionsVariable = "OPENWORKER_TEST_ACTION";

    /// <summary>
    /// The imp puppet: its first skill lunges it forward and lands its blow 0.87 s after the key.
    /// </summary>
    private const int Imp = 30103001;

    /// <summary>
    /// The blow lands at the moment the skill's timeline gives it, from where the skill's moves have carried the monster: a target
    /// just out of reach of the place the key found it is hit, and not before that moment.
    /// </summary>
    [TestMethod]
    public void TheBlowLandsWhenAndWhereTheSkillCarriesTheMonster()
    {
        var folder = Environment.GetEnvironmentVariable(ActionsVariable);
        var monsters = TestTables.Load<MonsterRow>("tb_Monster");

        if (string.IsNullOrEmpty(folder))
        {
            Assert.Inconclusive($"{ActionsVariable} is not set.");
        }

        var skills = TestTables.Load<SkillRow>("tb_Skill");
        var tables = new CombatTables(monsters, TestTables.Load<StatusRow>("tb_Status"), skills, TestTables.Load<CharacterInfoRow>("tb_Character_Info"));
        var actions = new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance) { Folder = folder };
        var time = new FakeTimeProvider();
        var clock = TestMazes.Clock(time);
        var services = new CreatureServices(new AiScriptLoader(NullLogger<AiScriptLoader>.Instance, Path.Combine(Path.GetTempPath(), "no-such-folder")),
            new MonsterBehaviorTables(monsters, skills), actions, clock, new NavMeshProvider());
        var world = World.Create();

        try
        {
            var connection = new RecordingConnection();
            var life = new AbilityComponentEntry { Current = 100, Max = 100 };

            // Yaw 90 looks along +X.
            var player = world.Create(
                new ActorComponent(1, ActorType.User),
                new ServerSessionComponent(connection),
                new WorldComponent { Location = 0, Position = new Vector3(100f, 100f, 0f), Rotation = 90f, Jump = 0 },
                new PersonInfoComponent { Name = "Tamer", Hero = Hero.Haru },
                new AbilityComponent { Health = life, SoulGain = life, SoulVapor = life, Stamina = life, SuperArmor = life, Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f } });
            var batch = TestMazes.Map();
            var creatures = new LuaCreatureManager(world, batch, tables, services);
            var maze = TestMazes.Create(world, player, clock: clock, batch: batch, creatures: creatures, actions: actions, tables: tables);

            maze.MarkLoaded(player);
            maze.TryStart();

            Assert.IsTrue(maze.CallMonsters(player, Imp, 1));

            // The blow reaches 188 ahead of where it is laid, the target's body 40 more: 270 ahead is out of reach of the start.
            var target = creatures.Living.Single();

            world.Set(target, world.Get<WorldComponent>(target) with { Position = new Vector3(370f, 100f, 0f) });

            Assert.IsTrue(maze.ControlMonster(player, Imp));

            var monster = ControlledActor(connection);
            var before = world.Get<CreatureComponent>(target).Health;

            Assert.AreEqual(ClaimVerdict.Taken, maze.ControlAttack(player, monster, 0));

            Advance(time, clock, TimeSpan.FromSeconds(0.8));

            Assert.AreEqual(before, world.Get<CreatureComponent>(target).Health);

            Advance(time, clock, TimeSpan.FromSeconds(0.2));

            Assert.IsLessThan(before, world.Get<CreatureComponent>(target).Health);

            maze.Close();
        }
        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// The monster of the 17/51 sent to the player, once the writer has sent it.
    /// </summary>
    private static int ControlledActor(RecordingConnection connection)
    {
        var opcode = new MonsterControlResponse().Opcode.Value;
        var deadline = DateTime.UtcNow.AddSeconds(2);

        while (!connection.Frames.Select(PlayerFrame.Open).Any(e => e.Opcode.Value == opcode) && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(5);
        }

        var control = connection.Frames.Select(PlayerFrame.Open).Last(e => e.Opcode.Value == opcode);

        return BinaryPrimitives.ReadInt32LittleEndian(control.Body.AsSpan(4, 4));
    }

    private static void Advance(FakeTimeProvider time, SimulationClock clock, TimeSpan span)
    {
        for (var passed = TimeSpan.Zero; passed < span; passed += SimulationClock.Period)
        {
            time.Advance(SimulationClock.Period);
            clock.Tick();
        }
    }
}
