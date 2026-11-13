using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Batch.Actions;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.MazeEventCommands;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Gameplay.Modules.UnityPoints;
using OpenWorker.Hotspot.Modules.Chat.Responses;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Lua;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;
using OpenWorker.Navigation;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The commands that start in a maze what its maps and scripts never start: each refuses outside a maze and a number it cannot
/// use, and does what it says inside one.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MazeEventCommandsTest
{
    private const int Monster = 20000;
    private const int UnityEvent = 40101;
    private const string MonsterText = "20000";
    private const string UnityEventText = "40101";

    /// <summary>
    /// A started maze with one player in it, and a person outside every maze.
    /// </summary>
    private sealed class Stage : IDisposable
    {
        private readonly ServiceProvider _services;

        public Stage()
        {
            var monsters = new ReadOnlyCollection<MonsterRow>([TestRows.Build<MonsterRow>(("Id", Monster))]);
            var skills = TestMazes.None<SkillRow>();
            var tables = new CombatTables(monsters, TestMazes.None<StatusRow>(), skills, TestMazes.None<CharacterInfoRow>());
            var batch = TestMazes.Map();
            var actions = new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance);
            var clock = TestMazes.Clock(new FakeTimeProvider());
            var creatureServices = new CreatureServices(new AiScriptLoader(NullLogger<AiScriptLoader>.Instance, Path.Combine(Path.GetTempPath(), "no-such-folder")),
                new MonsterBehaviorTables(monsters, skills), actions, clock, new NavMeshProvider());
            var unity = new UnityEvents(new ReadOnlyCollection<UnityEventRow>([TestRows.Build<UnityEventRow>(("Id", (ushort)UnityEvent))]));

            Player = Person(1, Connection);
            Outsider = Person(2, new RecordingConnection());
            Creatures = new LuaCreatureManager(World, batch, tables, creatureServices);
            Maze = TestMazes.Create(World, Player, clock: clock, batch: batch, creatures: Creatures, actions: actions, tables: tables, unity: unity);

            Maze.MarkLoaded(Player);
            Maze.TryStart();
            World.Add(Player, Maze);

            _services = new ServiceCollection().AddSingleton(World).BuildServiceProvider();
        }

        public World World { get; } = World.Create();
        public RecordingConnection Connection { get; } = new();
        public Entity Player { get; }
        public Entity Outsider { get; }
        public LuaCreatureManager Creatures { get; }
        public LuaMaze Maze { get; }

        private Entity Person(int actor, RecordingConnection connection)
        {
            var life = new AbilityComponentEntry { Current = 100, Max = 100 };

            return World.Create(
                new ActorComponent(actor, ActorType.User),
                new ServerSessionComponent(connection),
                new WorldComponent { Location = 0, Position = new Vector3(500f, 600f, 0f), Rotation = 90f, Jump = 0 },
                new PersonInfoComponent { Name = "Master", Hero = Hero.Haru },
                new AbilityComponent { Health = life, SoulGain = life, SoulVapor = life, Stamina = life, SuperArmor = life, Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f } });
        }

        public bool Run<T>(Func<IServiceProvider, T> make, Entity who, params string[] tokens) where T : ICommand
        {
            return make(_services).TryExecute(who, tokens).AsTask().GetAwaiter().GetResult();
        }

        /// <summary>
        /// How many times the player was sent the message of that opcode so far, once a notice sent after them has arrived.
        /// </summary>
        public int Told(MessageOpcode opcode)
        {
            var sentinel = new ChatNoticeResponse { Message = "end" };
            var before = Count(sentinel.Opcode);

            World.Get<ServerSessionComponent>(Player).Send(sentinel);

            var deadline = DateTime.UtcNow.AddSeconds(2);

            while (Count(sentinel.Opcode) <= before && DateTime.UtcNow < deadline)
            {
                Thread.Sleep(5);
            }

            return Count(opcode);
        }

        private int Count(MessageOpcode opcode) => Connection.Frames.Select(PlayerFrame.Open).Count(e => e.Opcode.Value == opcode.Value);

        public void Dispose()
        {
            Maze.Close();
            _services.Dispose();
            World.Destroy(World);
        }
    }

    /// <summary>
    /// The unity mode starts only for an event its table has.
    /// </summary>
    [TestMethod]
    public void TheUnityModeStartsOnlyForAnEventOfTheTable()
    {
        using var stage = new Stage();

        Assert.IsFalse(stage.Run(e => new UnityStartCommand(e), stage.Outsider, UnityEventText), "outside a maze");
        Assert.IsFalse(stage.Run(e => new UnityStartCommand(e), stage.Player, "40102"), "an event the table lacks");
        Assert.IsFalse(stage.Run(e => new UnityStartCommand(e), stage.Player, "70000"), "past the numbers of the table");
        Assert.IsFalse(stage.Maze.HasMode);

        Assert.IsTrue(stage.Run(e => new UnityStartCommand(e), stage.Player, UnityEventText));
        Assert.IsTrue(stage.Maze.HasMode);
    }

    /// <summary>
    /// Monsters of a row the table has come where the player stands, as many as asked up to 50.
    /// </summary>
    [TestMethod]
    public void MonstersOfARowComeWhereThePlayerStands()
    {
        using var stage = new Stage();

        Assert.IsFalse(stage.Run(e => new MonsterCommand(e), stage.Outsider, MonsterText), "outside a maze");
        Assert.IsFalse(stage.Run(e => new MonsterCommand(e), stage.Player, "20001"), "a row the table lacks");
        Assert.IsFalse(stage.Run(e => new MonsterCommand(e), stage.Player, MonsterText, "51"), "too many");
        Assert.IsFalse(stage.Run(e => new MonsterCommand(e), stage.Player, MonsterText, "0"), "none");
        Assert.AreEqual(0, stage.Creatures.Living.Count);

        Assert.IsTrue(stage.Run(e => new MonsterCommand(e), stage.Player, MonsterText, "3"));

        var called = stage.Creatures.Living;

        Assert.AreEqual(3, called.Count);
        Assert.IsTrue(called.All(e => stage.World.Get<CreatureComponent>(e).Identifier == Monster));
        Assert.AreEqual(stage.World.Get<WorldComponent>(stage.Player).Position, stage.World.Get<WorldComponent>(called[0]).Position, "the first stands where the player does");
        Assert.AreEqual(1, stage.Told(new WorldOtherInfosMonsterResponse { Monsters = [] }.Opcode), "the clients are shown them at once");
    }

    /// <summary>
    /// The countdown is shown for a time above zero and up to a day, and hidden by a time below zero.
    /// </summary>
    [TestMethod]
    public void TheCountdownIsShownForATimeAndHiddenBelowZero()
    {
        using var stage = new Stage();
        var timer = new MazeTimerResponse().Opcode;

        Assert.IsFalse(stage.Run(e => new CasualTimerCommand(e), stage.Outsider, "60"), "outside a maze");
        Assert.IsFalse(stage.Run(e => new CasualTimerCommand(e), stage.Player, "0"));
        Assert.IsFalse(stage.Run(e => new CasualTimerCommand(e), stage.Player, "NaN"));
        Assert.IsFalse(stage.Run(e => new CasualTimerCommand(e), stage.Player, "86401"));
        Assert.IsFalse(stage.Run(e => new CasualTimerCommand(e), stage.Player, "60", "-5"), "added seconds below zero");
        Assert.AreEqual(0, stage.Told(timer));

        Assert.IsTrue(stage.Run(e => new CasualTimerCommand(e), stage.Player, "60", "10"));
        Assert.IsTrue(stage.Run(e => new CasualTimerCommand(e), stage.Player, "-1"));
        Assert.AreEqual(2, stage.Told(timer));
    }

    /// <summary>
    /// The player takes a monster of a row the table has and lets it go with 0; with no monster there is nothing to let go.
    /// </summary>
    [TestMethod]
    public void APlayerControlsAMonsterOfARowAndLetsItGo()
    {
        using var stage = new Stage();
        var control = new MonsterControlResponse().Opcode;

        Assert.IsFalse(stage.Run(e => new MonsterControlCommand(e), stage.Outsider, MonsterText), "outside a maze");
        Assert.IsFalse(stage.Run(e => new MonsterControlCommand(e), stage.Player, "20001"), "a row the table lacks");
        Assert.IsFalse(stage.Run(e => new MonsterControlCommand(e), stage.Player, "0"), "no monster to let go");
        Assert.IsFalse(stage.Run(e => new MonsterControlCommand(e), stage.Player, "-1"));

        Assert.IsTrue(stage.Run(e => new MonsterControlCommand(e), stage.Player, MonsterText));
        Assert.IsTrue(stage.Run(e => new MonsterControlCommand(e), stage.Player, "0"));
        Assert.IsFalse(stage.Run(e => new MonsterControlCommand(e), stage.Player, "0"), "let go already");
        Assert.AreEqual(2, stage.Told(control), "everybody is told of the take and of the release");
    }
}
