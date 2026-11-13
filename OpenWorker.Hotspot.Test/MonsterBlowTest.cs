using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
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
    /// A blow without damage neither hurts nor throws: the clients are told nothing of it, and only a blow that deals damage
    /// is a hit.
    /// </summary>
    [TestMethod]
    public async Task ABlowWithoutDamageIsNoHit()
    {
        var folder = Directory.CreateTempSubdirectory();
        var world = World.Create();

        try
        {
            File.WriteAllText(Path.Join(folder.FullName, "Ai_Still.lua"),
                "function Initial_Normal(self, monster)\n  local ai = monster:GetAi()\n  ai:SetAiCheckTime(0.1)\n  ai:SetFindEnemyInfo(0, 1.0, 100.0)\nend\n");

            var clock = TestMazes.Clock();
            var monsters = new ReadOnlyCollection<MonsterRow>([TestRows.Build<MonsterRow>(("Id", Monster), ("AiScript", "Ai_Still"))]);
            var skills = TestMazes.None<SkillRow>();
            var combat = new CombatTables(monsters, TestMazes.None<StatusRow>(), skills, TestMazes.None<CharacterInfoRow>());
            var services = new CreatureServices(new AiScriptLoader(NullLogger<AiScriptLoader>.Instance, folder.FullName), new MonsterBehaviorTables(monsters, skills),
                new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance), clock, new NavMeshProvider());
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
                    Health = new AbilityComponentEntry { Current = 1000, Max = 1000 },
                    SoulGain = default,
                    SoulVapor = default,
                    Stamina = default,
                    SuperArmor = default,
                    Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f }
                });
            var maze = TestMazes.Create(world, player, batch: batch, creatures: new LuaCreatureManager(world, batch, combat, services), tables: combat, clock: clock);

            maze.SpawnSector(1);

            var body = maze.Creatures.BodyOf(maze.Creatures.FindMonsters((_, _) => true).Single())!;
            var blow = new AttackTrigger(1, 0, 0, 0, 0f, 360f, Vector3.Zero, 0f, 0f, 500f, 500f, 0f, DamageRate: 0f, ReactionType: 1);

            Assert.AreEqual(0, maze.Creatures.HitPlayers(body, 0, blow, [player]), "A blow without damage strikes nobody.");
            Assert.AreNotEqual(0, maze.Creatures.HitPlayers(body, 0, blow with { DamageRate = 1f }, [player]));

            await MetricAssertions.UntilAsync(() => Hits(connection).Length > 0, "The blow with damage is told.");

            Assert.HasCount(1, Hits(connection), "Only the blow with damage is told.");
        }

        finally
        {
            World.Destroy(world);
            folder.Delete(true);
        }
    }

    private static PlayerFrame[] Hits(RecordingConnection connection) =>
        connection.Frames.Select(PlayerFrame.Open).Where(e => e.Is(GroupOpcode.Skill, (byte)SkillOpcode.ActionSkillBt)).ToArray();
}
