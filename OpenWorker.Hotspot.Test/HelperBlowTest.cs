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
/// The blows of the monsters of a maze on the helpers of the players follow the rules they follow on the players.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class HelperBlowTest
{
    /// <summary>
    /// Mirium, the first helper a person gets.
    /// </summary>
    private const int Mirium = 10105166;

    private sealed record Arena(World World, LuaMaze Maze, Entity Owner, MonsterBody Monster, SummonedHelper Helper, RecordingConnection Connection)
    {
        public void Advance(float seconds)
        {
            for (var step = 0; step < seconds / 0.05f; step++)
            {
                Maze.Creatures.Step(TimeSpan.FromMilliseconds(50));
            }
        }

        /// <summary>
        /// Closes the owner's connection once what it was told is sent, so none of it is still on its way.
        /// </summary>
        public async Task<PlayerFrame[]> SentAsync()
        {
            var session = World.Get<ServerSessionComponent>(Owner);

            session.Disconnect();
            await session.Closed.WaitAsync(TimeSpan.FromSeconds(30));

            return Connection.Frames.Select(PlayerFrame.Open).ToArray();
        }

        public AiTarget HelperTarget()
        {
            var place = World.Get<WorldComponent>(Helper.Entity);

            return new AiTarget(Helper.Actor, place.Position, 40f, place.Rotation, 1000, 1000, false);
        }
    }

    private static async Task WithHelperAsync(Func<Arena, Task> test)
    {
        var world = World.Create();

        try
        {
            var connection = new RecordingConnection();
            var owner = Player(world, connection);
            var monster = TestTables.Load<MonsterRow>("tb_Monster").First(e => e.Type == 0 && e.Level is > 1 and < 20 && e.Faction != 10 && e.Skill1 != 0 &&
                !string.IsNullOrEmpty(e.AiScript));
            var batch = TestMazes.Map(PartyMazeTest.MapWithMonster(monster.Id));
            var maze = TestMazes.Create(world, owner, batch: batch, creatures: Creatures(world, batch, out var stats));

            maze.SpawnSector(1);

            var body = maze.Creatures.BodyOf(maze.Creatures.FindMonsters((_, _) => true).Single());
            var helper = maze.SummonHelper(owner, stats.Combat(Mirium, 20, []), new HelperValue { Helper = Mirium, Summoned = true })!;

            if (body is null || helper.Body is null)
            {
                Assert.Inconclusive("OPENWORKER_TEST_AI is not set.");
            }

            await test(new Arena(world, maze, owner, body, helper, connection));
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A helper that lies on the ground is waited for, as a player is: a monster does not start a skill on it that is not for one
    /// that lies.
    /// </summary>
    [TestMethod]
    public Task AMonsterWaitsWithASkillNotForALyingHelper() =>
        WithHelperAsync(arena =>
        {
            var skill = arena.Monster.GetSkill(0)! with { ForLying = false };
            var place = arena.World.Get<WorldComponent>(arena.Helper.Entity).Position;
            // A grade above any rank throws whatever the limit of the helper says.
            var push = new AttackReaction(300f, 150f, 0f, 1000f, AttackReaction.StraightAlongAttacker, 0f, 0f, 0f, 0f, 0f, 0, 10, false, 1, false);

            arena.Helper.Body!.ReceiveBlow(new Blow(push, ReactionKind.KnockDown, 1f, 90f, place, place, 0f, true), DefenseType.Normal, true, new BlowRolls(0, 0));

            for (var waited = 0f; !arena.Helper.Body.IsLying && waited < 5f; waited += 0.05f)
            {
                arena.Advance(0.05f);
            }

            Assert.IsTrue(arena.Helper.Body.IsLying, "The helper lies.");
            Assert.IsFalse(arena.Monster.UseSkill(skill, arena.HelperTarget()));
            Assert.IsTrue(arena.Monster.UseSkill(skill with { ForLying = true }, arena.HelperTarget()));

            return Task.CompletedTask;
        });

    /// <summary>
    /// A trap with a reaction and no damage throws the helpers it reaches and deals them nothing, as it does the players.
    /// </summary>
    [TestMethod]
    public Task ATrapWithAReactionAndNoDamageThrowsAHelper() =>
        WithHelperAsync(async arena =>
        {
            var place = arena.World.Get<WorldComponent>(arena.Helper.Entity).Position;
            var health = arena.World.Get<CreatureComponent>(arena.Helper.Entity).Health;
            var trap = new AttackTrigger(1, 0, 0, 0, 0f, 360f, Vector3.Zero, 0, 0, 300, 300, 0, DamageRate: 0f, ReactionType: (int)ReactionKind.KnockDown);
            var area = new AttackArea(trap, place, 0f);

            arena.Maze.Helpers.HitBy(arena.Monster, 0, trap, area, null, false, true);

            var told = (await arena.SentAsync()).Count(e => e.Is(GroupOpcode.Skill, (byte)SkillOpcode.ActionSkillBt));

            Assert.AreEqual(1, told, "The helper is told hit.");
            Assert.AreEqual(health, arena.World.Get<CreatureComponent>(arena.Helper.Entity).Health);
        });

    private static LuaCreatureManager Creatures(World world, OpenWorker.Batch.VBatchFile batch, out HelperStats stats)
    {
        var monsters = TestTables.Load<MonsterRow>("tb_Monster");
        var skills = TestTables.Load<SkillRow>("tb_Skill");
        var statuses = TestTables.Load<StatusRow>("tb_Status");
        var combat = new CombatTables(monsters, statuses, skills, TestTables.Load<CharacterInfoRow>("tb_Character_Info"));
        var scripts = Environment.GetEnvironmentVariable("OPENWORKER_TEST_AI") ?? Path.Join(Path.GetTempPath(), "no-scripts");
        var services = new CreatureServices(
            new AiScriptLoader(NullLogger<AiScriptLoader>.Instance, scripts),
            new MonsterBehaviorTables(monsters, skills, TestTables.Load<FactionRow>("tb_Faction")),
            new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance) { Folder = Environment.GetEnvironmentVariable("OPENWORKER_TEST_ACTION") ?? "action" },
            TestMazes.Clock(),
            new NavMeshProvider());

        stats = new HelperStats(HelperCatalog.From([Mirium]), monsters, statuses);

        return new LuaCreatureManager(world, batch, combat, services);
    }

    private static Entity Player(World world, RecordingConnection connection)
    {
        var life = new AbilityComponentEntry { Current = 1000, Max = 1000 };

        return world.Create(
            new ActorComponent(1, ActorType.User),
            new ServerSessionComponent(connection),
            new WorldComponent { Location = 0, Position = new Vector3(40000, 23000, 150), Rotation = 0f, Jump = 0 },
            new LevelComponent(20, 0),
            new PersonInfoComponent { Name = "Member", Hero = Hero.Haru },
            new AbilityComponent { Health = life, SoulGain = life, SoulVapor = life, Stamina = life, SuperArmor = life, Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f } });
    }
}
