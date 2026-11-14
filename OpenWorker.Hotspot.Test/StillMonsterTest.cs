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
/// A monster a command calls still stands where it was called and never attacks, while one called the usual way fights; the
/// armor of the still one comes back all the same.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class StillMonsterTest
{
    /// <summary>
    /// The Discus Puppet: a monster with a super armor gauge and a mind.
    /// </summary>
    private const int Discus = 30103005;

    /// <summary>
    /// The bit the wire sets on the actor of a monster.
    /// </summary>
    private const int MonsterActors = 1 << 30;

    [TestMethod]
    public async Task AStillMonsterNeitherWalksNorAttacksAndItsArmorComesBack()
    {
        var folder = Environment.GetEnvironmentVariable("OPENWORKER_TEST_AI");

        if (string.IsNullOrEmpty(folder))
        {
            Assert.Inconclusive("OPENWORKER_TEST_AI is not set.");
        }

        var monsters = TestTables.Load<MonsterRow>("tb_Monster");
        var skills = TestTables.Load<SkillRow>("tb_Skill");
        var time = new FakeTimeProvider();
        var clock = TestMazes.Clock(time);
        var combat = new CombatTables(monsters, TestTables.Load<StatusRow>("tb_Status"), skills, TestTables.Load<CharacterInfoRow>("tb_Character_Info"));
        var services = new CreatureServices(
            new AiScriptLoader(NullLogger<AiScriptLoader>.Instance, folder),
            new MonsterBehaviorTables(monsters, skills, TestTables.Load<FactionRow>("tb_Faction")),
            new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance),
            clock,
            new NavMeshProvider());
        var world = World.Create();

        try
        {
            var connection = new RecordingConnection();
            var life = new AbilityComponentEntry { Current = 100_000, Max = 100_000 };
            var player = world.Create(
                new ActorComponent(1, ActorType.User),
                new ServerSessionComponent(connection),
                new WorldComponent { Location = 0, Position = new Vector3(1000f, 1000f, 0f), Rotation = 0f, Jump = 0 },
                new LevelComponent(20, 0),
                new PersonInfoComponent { Name = "Master", Hero = Hero.Haru },
                new AbilityComponent { Health = life, SoulGain = life, SoulVapor = life, Stamina = life, SuperArmor = life, Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f } });
            var batch = TestMazes.Map();
            var creatures = new LuaCreatureManager(world, batch, combat, services);
            var maze = TestMazes.Create(world, player, clock: clock, batch: batch, creatures: creatures, tables: combat);

            maze.MarkLoaded(player);
            maze.TryStart();

            Assert.IsTrue(maze.CallMonsters(player, Discus, 1, still: true));

            var still = creatures.Living.Single();
            var place = world.Get<WorldComponent>(still).Position;
            var full = world.Get<MonsterCombatComponent>(still).MaxSuperArmor;

            Assert.IsGreaterThan(0f, full);

            Assert.IsTrue(maze.CallMonsters(player, Discus, 1));

            var free = creatures.Living.Single(e => e != still);
            var freeAt = world.Get<WorldComponent>(free).Position;

            // Its armor broken, to come back in a second.
            world.Set(still, world.Get<MonsterCombatComponent>(still) with { SuperArmor = 0f, SuperArmorBrokenUntil = clock.Time + TimeSpan.FromSeconds(1) });

            for (var passed = TimeSpan.Zero; passed < TimeSpan.FromSeconds(10); passed += SimulationClock.Period)
            {
                time.Advance(SimulationClock.Period);
                clock.Tick();
            }

            await QueueMarker.SettleAsync(world.Get<ServerSessionComponent>(player), connection);

            var skillsOf = connection.Frames.Select(PlayerFrame.Open)
                .Where(e => e.Is(GroupOpcode.Skill, (byte)SkillOpcode.ActiveSkillBt))
                .Select(e => BitConverter.ToInt32(e.Body, 0) & ~MonsterActors)
                .ToArray();
            var stillActor = world.Get<ActorComponent>(still).Identifier;
            var freeActor = world.Get<ActorComponent>(free).Identifier;

            Assert.IsTrue(skillsOf.Contains(freeActor) || world.Get<WorldComponent>(free).Position != freeAt, "the one called the usual way fights");
            Assert.DoesNotContain(stillActor, skillsOf, "the still one never attacks");
            Assert.AreEqual(place, world.Get<WorldComponent>(still).Position, "nor walks");
            Assert.AreEqual(full, world.Get<MonsterCombatComponent>(still).SuperArmor, "its armor comes back");

            maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
