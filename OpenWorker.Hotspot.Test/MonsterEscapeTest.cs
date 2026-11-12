using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Lua;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;
using OpenWorker.Navigation;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A monster in a maze that is hurt over and over gets away with the skill its script names, and the players are told.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MonsterEscapeTest
{
    private const int Blows = 120;

    [TestMethod]
    public async Task AMonsterHurtOverAndOverEscapesWithTheSkillOfItsScriptAndThePlayersAreTold()
    {
        var folder = Environment.GetEnvironmentVariable("OPENWORKER_TEST_AI");

        if (string.IsNullOrEmpty(folder))
        {
            Assert.Inconclusive("OPENWORKER_TEST_AI is not set.");
        }

        var monsters = TestTables.Load<MonsterRow>("tb_Monster");
        var skills = TestTables.Load<SkillRow>("tb_Skill");
        var scripts = new AiScriptLoader(NullLogger<AiScriptLoader>.Instance, folder);

        // A plain monster whose script escapes every time its points are full, with a skill.
        var monster = monsters.First(e => e.Type == 0 && e.Faction != 10 && !string.IsNullOrEmpty(e.AiScript) && e.AiScript != "0"
            && scripts.Get(e.AiScript).Follower.Escape is { Kind: AiEscape.SkillKind, Chance: 10000 } escape && skills.Any(s => s.Id == escape.Value));
        var escape = scripts.Get(monster.AiScript).Follower.Escape!;
        var world = World.Create();

        try
        {
            var connection = new RecordingConnection();
            var player = world.Create(
                new ActorComponent(1, ActorType.User),
                new ServerSessionComponent(connection),
                new WorldComponent { Location = 0, Position = new Vector3(40000, 23000, 150), Rotation = 0f, Jump = 0 },
                new LevelComponent(20, 0),
                new PersonInfoComponent { Name = "Fighter", Hero = Hero.Haru },
                new AbilityComponent
                {
                    Health = new AbilityComponentEntry { Current = 1000, Max = 1000 },
                    SoulGain = new AbilityComponentEntry { Current = 1000, Max = 1000 },
                    SoulVapor = new AbilityComponentEntry { Current = 1000, Max = 1000 },
                    Stamina = new AbilityComponentEntry { Current = 1000, Max = 1000 },
                    SuperArmor = new AbilityComponentEntry { Current = 1000, Max = 1000 },
                    Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f }
                });
            var combat = new CombatTables(monsters, TestTables.Load<StatusRow>("tb_Status"), skills, TestTables.Load<CharacterInfoRow>("tb_Character_Info"));
            var services = new CreatureServices(
                scripts,
                new MonsterBehaviorTables(monsters, skills, TestTables.Load<FactionRow>("tb_Faction")),
                new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance),
                TestMazes.Clock(),
                new NavMeshProvider());
            var batch = TestMazes.Map(PartyMazeTest.MapWithMonster(monster.Id));
            var maze = TestMazes.Create(world, player, batch: batch, creatures: new LuaCreatureManager(world, batch, combat, services));

            maze.SpawnSector(1);

            var target = maze.Creatures.FindMonsters((_, _) => true).Single();
            var escaped = false;

            for (var blow = 0; blow < Blows && !escaped; blow++)
            {
                maze.Creatures.OnMonsterHit(target, player, 50);
                maze.Creatures.Step(TimeSpan.FromSeconds(0.6));

                // The frames reach the connection through the queue of its writer: what the blow sent has arrived once
                // the marker sent after it has.
                await QueueMarker.SettleAsync(world.Get<ServerSessionComponent>(player), connection);

                escaped = connection.Frames.Select(PlayerFrame.Open).Any(e => e.Is(GroupOpcode.Monster, (byte)MonsterOpcode.EscapeDamageBt));
            }

            var frames = connection.Frames.Select(PlayerFrame.Open).ToArray();

            Assert.IsTrue(escaped, "The players are told the monster has escaped.");
            Assert.IsTrue(frames.Any(e => e.Is(GroupOpcode.Skill, (byte)SkillOpcode.ActiveSkillBt) && BitConverter.ToInt32(e.Body, 4) == escape.Value),
                "The monster plays the skill of its script.");
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
