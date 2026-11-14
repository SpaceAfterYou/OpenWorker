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
using OpenWorker.Gameplay.Modules.Drops;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Gameplay.Modules.UnityPoints;
using OpenWorker.Hotspot.Modules.Chat.Responses;
using OpenWorker.Hotspot.Modules.Drops;
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
    private const int FeverItem = 650000001;
    private const string FeverItemText = "650000001";

    /// <summary>
    /// A started maze with one player in it, and a person outside every maze.
    /// </summary>
    private sealed class Stage : IDisposable
    {
        private readonly ServiceProvider _services;

        // A server that keeps nothing on the ground.
        private readonly ServiceProvider _district;

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

            var items = new ItemCatalog(new ReadOnlyCollection<ItemRow>([TestRows.Build<ItemRow>(("Id", FeverItem))]), TestMazes.None<ItemClassifyRow>(),
                TestMazes.None<InvenSlotExtendRow>(), TestMazes.None<BankSlotExtendRow>());
            var serials = new LocalItemSerials();

            Drops = new DropService(World, clock, new InventoryService(World, items, serials), items, serials, TestMazes.None<DropRow>(),
                TestMazes.None<DropGroupRow>(), TestMazes.None<DropRateMobRow>(), monsters);

            _services = new ServiceCollection().AddSingleton(World).AddSingleton(items).AddSingleton(Drops).BuildServiceProvider();
            _district = new ServiceCollection().AddSingleton(World).AddSingleton(items).BuildServiceProvider();
        }

        public World World { get; } = World.Create();
        public RecordingConnection Connection { get; } = new();
        public Entity Player { get; }
        public Entity Outsider { get; }
        public LuaCreatureManager Creatures { get; }
        public LuaMaze Maze { get; }
        public DropService Drops { get; }

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

        public bool RunInDistrict<T>(Func<IServiceProvider, T> make, Entity who, params string[] tokens) where T : ICommand
        {
            return make(_district).TryExecute(who, tokens).AsTask().GetAwaiter().GetResult();
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
            _district.Dispose();
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
    /// Monsters of a row the table has come where the player stands, as many as asked up to 50, still when asked.
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

        Assert.IsFalse(stage.Run(e => new MonsterCommand(e), stage.Player, MonsterText, "2", "3"), "a word past the count that is not still");
        Assert.IsTrue(stage.Run(e => new MonsterCommand(e), stage.Player, MonsterText, "still"));
        Assert.IsTrue(stage.Run(e => new MonsterCommand(e), stage.Player, MonsterText, "2", "still"));
        Assert.AreEqual(6, stage.Creatures.Living.Count);
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

    /// <summary>
    /// An item the client has falls at the feet of the player as a drop of a kill, only on the server that keeps the ground and
    /// only in a maze.
    /// </summary>
    [TestMethod]
    public void AnItemFallsAtTheFeetOfThePlayerAsADrop()
    {
        using var stage = new Stage();

        Assert.IsFalse(stage.Run(e => new DropCommand(e), stage.Outsider, FeverItemText), "outside a maze");
        Assert.IsFalse(stage.RunInDistrict(e => new DropCommand(e), stage.Player, FeverItemText), "a server without the ground");
        Assert.IsFalse(stage.Run(e => new DropCommand(e), stage.Player, "650000009"), "an item the client lacks");
        Assert.IsFalse(stage.Run(e => new DropCommand(e), stage.Player, FeverItemText, "0"));
        Assert.IsEmpty(stage.Drops.On(stage.Player));

        Assert.IsTrue(stage.Run(e => new DropCommand(e), stage.Player, FeverItemText, "2"));

        var dropped = stage.Drops.On(stage.Player).Single();

        Assert.AreEqual(FeverItem, dropped.ObjectId);
        Assert.AreEqual(2, dropped.Count);
        Assert.IsLessThanOrEqualTo(90f, Vector3.Distance(stage.World.Get<WorldComponent>(stage.Player).Position, dropped.Position), "at the feet");
        Assert.AreEqual(1, stage.Told(new DropInfosResponse { DropGroup = 0, Objects = [] }.Opcode), "the client is told as of a kill's drop");
    }
}
