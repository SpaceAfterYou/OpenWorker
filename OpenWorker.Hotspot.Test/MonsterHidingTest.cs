using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
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
/// A monster that has gone from sight.
/// </summary>
[TestClass]
public sealed class MonsterHidingTest
{
    private static readonly ActorValue Monster = new(0x1234, ActorType.Monster);

    /// <summary>
    /// Gone, a monster is out of reach, stands and does not think, except for what its event leaves it.
    /// </summary>
    [TestMethod]
    public void AHiddenMonsterKeepsOnlyWhatItsEventLeavesIt()
    {
        var hiding = new MonsterHiding();

        Assert.IsTrue(hiding.CanBeHit && hiding.CanMove && hiding.CanThink);

        hiding.Begin(new InvisibleEvent(0f, InvisibleEvent.KeepsMind, InvisibleEvent.ForTime, 2000), 10.0);

        Assert.IsFalse(hiding.CanBeHit);
        Assert.IsFalse(hiding.CanMove);
        Assert.IsTrue(hiding.CanThink);

        hiding.Begin(new InvisibleEvent(0f, MonsterHiding.KeepsCollision | MonsterHiding.KeepsMovement, InvisibleEvent.ForTime, 2000), 10.0);

        Assert.IsTrue(hiding.CanBeHit);
        Assert.IsTrue(hiding.CanMove);
        Assert.IsFalse(hiding.CanThink);
    }

    /// <summary>
    /// One who comes in while a monster is gone for a time is told the time that is left; other kinds are told as they were.
    /// </summary>
    [TestMethod]
    public void OneWhoComesInLaterIsToldTheTimeThatIsLeft()
    {
        var hiding = new MonsterHiding();

        hiding.Begin(new InvisibleEvent(0f, 8, InvisibleEvent.ForTime, 2500), 10.0);

        hiding.CountDown(1.0);

        var notice = hiding.Notice(Monster);

        Assert.IsTrue(notice.Invisible);
        Assert.AreEqual((8, InvisibleEvent.ForTime, 1500), (notice.Flags, notice.Type, notice.Value));

        hiding.Begin(new InvisibleEvent(0f, 0, InvisibleEvent.UntilHurt, 300), 10.0);

        Assert.AreEqual(300, hiding.Notice(Monster).Value);
    }

    /// <summary>
    /// A hiding for a time runs out by the animation of the monster, not by the clock: a second of it that begins in a stretch
    /// played at half speed lasts two seconds.
    /// </summary>
    [TestMethod]
    public void AHidingForATimeRunsOutByTheSpeedOfTheAnimation()
    {
        const int skill = 1300000001;
        const int row = 1000;
        const string file = "MOB_Hider";

        var folder = Directory.CreateTempSubdirectory();
        var world = World.Create();

        try
        {
            File.WriteAllText(Path.Join(folder.FullName, "Ai_Still.lua"), "function Initial_Normal(self, monster)\n  local ai = monster:GetAi()\n  ai:SetAiCheckTime(0.1)\nend\n");

            var hide = new InvisibleEvent(0f, 0, InvisibleEvent.ForTime, 1000);
            var animation = new ActionAnimation("B_Skill_01", 3f, [], Hides: [hide], Speeds: [new SpeedEvent(0f, 2f, 0.5f, false)]);
            var skills = new ReadOnlyCollection<SkillRow>([TestRows.Build<SkillRow>(("Id", skill), ("AnimationStart", animation.Name))]);
            var monsters = new ReadOnlyCollection<MonsterRow>([TestRows.Build<MonsterRow>(("Id", row), ("AiScript", "Ai_Still"), ("ActionFile", file), ("Skill1", skill))]);
            var actions = new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance) { Folder = folder.FullName };

            actions.Add(file, ActionFile.Of(animation));

            var time = new FakeTimeProvider();
            var clock = TestMazes.Clock(time);
            var combat = new CombatTables(monsters, TestMazes.None<StatusRow>(), skills, TestMazes.None<CharacterInfoRow>());
            var services = new CreatureServices(new AiScriptLoader(NullLogger<AiScriptLoader>.Instance, folder.FullName), new MonsterBehaviorTables(monsters, skills), actions, clock,
                new NavMeshProvider());
            var batch = TestMazes.Map(PartyMazeTest.MapWithMonster(row));
            var player = world.Create(
                new ActorComponent(100, ActorType.User),
                new ServerSessionComponent(new RecordingConnection()),
                new WorldComponent { Location = 0, Position = new Vector3(0f, 0f, -10000f), Rotation = 0f, Jump = 0 },
                new LevelComponent(10, 0),
                new PersonInfoComponent { Name = "Haru", Hero = Hero.Haru },
                new AbilityComponent
                {
                    Health = new AbilityComponentEntry { Current = 100000, Max = 100000 },
                    SoulGain = default,
                    SoulVapor = default,
                    Stamina = default,
                    SuperArmor = default,
                    Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f }
                });
            var maze = TestMazes.Create(world, player, batch: batch, creatures: new LuaCreatureManager(world, batch, combat, services), tables: combat, clock: clock);

            maze.SpawnSector(1);

            var body = maze.Creatures.BodyOf(maze.Creatures.FindMonsters((_, _) => true).Single())!;

            Assert.IsTrue(body.UseSkill(body.GetSkill(0)!, new AiTarget(100, body.Position + new Vector3(0f, -300f, 0f), 50f, 0f, 1, 1, false)));

            void Advance(float seconds)
            {
                for (var step = 0; step < seconds / SimulationClock.Period.TotalSeconds; step++)
                {
                    time.Advance(SimulationClock.Period);
                    clock.Tick();
                    maze.Creatures.Step(SimulationClock.Period);
                }
            }

            Advance(1.5f);
            Assert.IsTrue(body.IsOutOfReach, "Half a second of the animation has played.");

            Advance(1f);
            Assert.IsFalse(body.IsOutOfReach, "The second of the animation has played.");
        }

        finally
        {
            World.Destroy(world);
            folder.Delete(true);
        }
    }
}
