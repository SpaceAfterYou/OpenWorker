using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Gameplay.Modules.Helpers;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Gameplay.Modules.Quests.Components;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.Hotspot.SoulWorker.Network;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Plays a person through the quest manager on the world loop and reads what the client would be sent. Creating
/// entities registers component types in a registry that does not take two threads at once.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class QuestManagerTest
{
    private const int Npc = 7;
    private const int Wolf = 50;
    private const int Boar = 60;
    private const int Pack = 900;
    private const int Sword = 710001;

    private static QuestConditionDefinition Talk { get; } = new(1001, QuestConditionType.Talk, QuestConditionTarget.Npc, Npc, 0, 1, 0, [], []);

    private static QuestConditionDefinition Hunt { get; } = new(1002, QuestConditionType.Hunt, QuestConditionTarget.Monster, Wolf, 1001, 3, 0, [], []);

    private static QuestConditionDefinition Boars { get; } = new(1010, QuestConditionType.Hunt, QuestConditionTarget.Monster, Boar, 0, 1, 0, [], []);

    private static QuestConditionDefinition Tusks { get; } = new(1020, QuestConditionType.Collect, QuestConditionTarget.Item, 880001, 0, 4, 0, [], []);

    private static QuestConditionDefinition Cleared { get; } = new(1030, QuestConditionType.Clear, QuestConditionTarget.Maze, 21111, 0, 1, 0, [], []);

    private const int SurvivalMaze = 21331;

    private static QuestConditionDefinition Survive { get; } = new(1050, QuestConditionType.Survival, QuestConditionTarget.Npc, 0, 0, 1, SurvivalMaze, [], []);

    private static QuestDefinition Holdout { get; } = new(
        950,
        12,
        QuestContents.Side,
        0,
        0,
        [],
        false,
        false,
        [],
        [],
        QuestRewardDefinition.None,
        [1050]);

    private static QuestDefinition Hard { get; } = new(
        100,
        1,
        QuestContents.Main,
        0,
        0,
        [],
        false,
        false,
        [new QuestItem(Sword, 1)],
        [],
        new QuestRewardDefinition(1000, 50, 7, 3, 100100, 0, 0, false, [new QuestItem(710002, 2)], false, []),
        [1001, 1002]);

    private static QuestDefinition Quick { get; } = new(
        150,
        2,
        QuestContents.Side,
        0,
        0,
        [],
        true,
        false,
        [],
        [],
        new QuestRewardDefinition(0, 5, 0, 0, 0, 0, 0, false, [], false, []),
        [1010]);

    private static QuestDefinition Bound { get; } = new(
        300,
        3,
        QuestContents.Side,
        0,
        0,
        [],
        false,
        true,
        [],
        [],
        QuestRewardDefinition.None,
        [1010]);

    private static QuestDefinition Gathering { get; } = new(
        400,
        4,
        QuestContents.Side,
        0,
        0,
        [],
        false,
        false,
        [],
        [new QuestItem(880001, 4)],
        QuestRewardDefinition.None,
        [1020]);

    /// <summary>
    /// A hunt and a collection in one episode.
    /// </summary>
    private static QuestDefinition Mixed { get; } = new(
        450,
        14,
        QuestContents.Side,
        0,
        0,
        [],
        false,
        false,
        [],
        [],
        QuestRewardDefinition.None,
        [1010, 1020]);

    private static QuestDefinition Late { get; } = new(
        500,
        5,
        QuestContents.Side,
        0,
        0,
        [100],
        false,
        false,
        [],
        [],
        QuestRewardDefinition.None,
        [1030]);

    private static QuestDefinition HighLevel { get; } = new(
        600,
        6,
        QuestContents.Side,
        90,
        0,
        [],
        false,
        false,
        [],
        [],
        QuestRewardDefinition.None,
        [1010]);

    private static QuestConditionDefinition HandIn { get; } = new(1040, QuestConditionType.Talk, QuestConditionTarget.Npc, Npc, 0, 1, 0, [], [new QuestItem(Sword, 1)]);

    private static QuestDefinition Daily { get; } = new(
        700,
        7,
        QuestContents.Repeat,
        0,
        0,
        [],
        false,
        false,
        [],
        [],
        new QuestRewardDefinition(0, 5, 0, 0, 0, 0, 0, false, [], false, []),
        [1010]);

    private static QuestDefinition Errand { get; } = new(
        800,
        8,
        QuestContents.Side,
        0,
        0,
        [],
        false,
        false,
        [],
        [],
        QuestRewardDefinition.None,
        [1040]);

    private static QuestDefinition Prequel { get; } = new(
        910,
        10,
        QuestContents.Side,
        0,
        0,
        [],
        false,
        false,
        [],
        [],
        QuestRewardDefinition.None,
        [1010]) { Links = [920] };

    /// <summary>
    /// Follows <see cref="Prequel"/> by itself.
    /// </summary>
    private static QuestDefinition Sequel { get; } = new(
        920,
        11,
        QuestContents.Side,
        0,
        0,
        [910],
        false,
        false,
        [],
        [],
        QuestRewardDefinition.None,
        [1010]) { StartsByItself = true };

    private static QuestCatalog Catalog { get; } = QuestCatalog.From(
        [Hard, Quick, Bound, Gathering, Mixed, Late, HighLevel, Daily, Errand, Prequel, Sequel, Holdout],
        [Talk, Hunt, Boars, Tusks, Cleared, HandIn, Survive],
        null,
        new Dictionary<int, int[]> { [Wolf] = [Pack] });

    private sealed class MemoryStore : IQuestStore
    {
        public Dictionary<int, string> Documents { get; } = [];

        public ValueTask<QuestBook?> LoadAsync(int person, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(Documents.TryGetValue(person, out var json) ? JsonSerializer.Deserialize<QuestBook>(json) : null);
        }
    }

    private sealed class Granter(bool fits = true) : IRewardGranter
    {
        public List<(Reward Reward, RewardSource Source)> Given { get; } = [];

        public ValueTask<bool> TryGrantAsync(Entity player, Reward reward, RewardSource source, CancellationToken cancellationToken)
        {
            if (fits)
            {
                Given.Add((reward, source));
            }

            return ValueTask.FromResult(fits);
        }
    }

    private sealed class Inventory : IQuestItems
    {
        public Dictionary<int, int> Have { get; } = [];

        public List<QuestItem> Taken { get; } = [];

        public ValueTask<int> CountAsync(Entity player, int item, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Have.GetValueOrDefault(item));

        public ValueTask TakeAsync(Entity player, IReadOnlyList<QuestItem> items, CancellationToken cancellationToken)
        {
            Taken.AddRange(items);

            return ValueTask.CompletedTask;
        }
    }

    private sealed class Titles : IQuestTitles
    {
        public List<int> Granted { get; } = [];

        public ValueTask GrantAsync(Entity player, int title)
        {
            Granted.Add(title);

            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// The character <see cref="Npc"/> stands where <see cref="Reach"/> says.
    /// </summary>
    private sealed class Npcs : IQuestNpcs
    {
        public NpcReach Reach { get; set; } = NpcReach.Near;

        NpcReach IQuestNpcs.Reach(Entity player, int npc) => npc == Npc ? Reach : NpcReach.Unknown;
    }

    private sealed class Harness : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly TcpClient _server;

        public Harness(
            MemoryStore? store = null, Granter? granter = null, Inventory? inventory = null, Hero hero = Hero.Haru, TimeProvider? time = null,
            ReadOnlyCollection<QuestConditionRuleRow>? rules = null)
        {
            _listener.Start();

            Client = new TcpClient();
            Client.Connect(IPAddress.Loopback, ((IPEndPoint)_listener.LocalEndpoint).Port);
            Client.ReceiveTimeout = 2000;

            _server = _listener.AcceptTcpClient();

            Store = store ?? new MemoryStore();
            Granter = granter ?? new Granter();
            Inventory = inventory ?? new Inventory();

            Loop = new WorldLoop(NullLogger<WorldLoop>.Instance);
            World = Arch.Core.World.Create();

            // The stores of helpers and boosters are never reached: the person has neither.
            var helpers = new HelperManager(World, null!, HelperCatalog.From([]));
            var boosters = new BoosterManager(World, null!, null!, TimeProvider.System, NullLogger<BoosterManager>.Instance);

            Manager = new QuestManager(
                World,
                Store,
                Catalog,
                Granter,
                Inventory,
                helpers,
                boosters,
                NullLogger<QuestManager>.Instance,
                npcs: Npcs,
                titles: Titles,
                time: time,
                rules: rules);

            Player = World.Create(
                Component.GetComponentType(typeof(ServerSessionComponent)),
                Component.GetComponentType(typeof(ActorComponent)),
                Component.GetComponentType(typeof(PersonInfoComponent)),
                Component.GetComponentType(typeof(QuestComponent)),
                Component.GetComponentType(typeof(DirtyComponent)));

            World.Set(Player, new ServerSessionComponent(_server));
            World.Set(Player, new ActorComponent(42, ActorType.User));
            World.Set(Player, new PersonInfoComponent { Name = "Tester", Hero = hero });
            World.Set(Player, default(QuestComponent));
            World.Set(Player, new DirtyComponent(new PersonDirty(1)));

            Reader = new BinaryReader(Client.GetStream());
        }

        public TcpClient Client { get; }
        public Titles Titles { get; } = new();
        public Npcs Npcs { get; } = new();
        public MemoryStore Store { get; }
        public Granter Granter { get; }
        public Inventory Inventory { get; }
        public WorldLoop Loop { get; }
        public World World { get; }
        public QuestManager Manager { get; }
        public Entity Player { get; }
        public BinaryReader Reader { get; }

        public Task Run(Func<ValueTask> action) => Loop.InvokeAsync(action);

        public Task Enter(int person = 1) => Run(() => Manager.EnterAsync(Player, person, CancellationToken.None));

        /// <summary>
        /// Waits until the loop has done what it was doing, such as the save that follows a message to the client.
        /// </summary>
        public Task Settle() => Loop.InvokeAsync(() => { });

        public QuestBook Book => World.Get<QuestComponent>(Player).Book!;

        /// <summary>
        /// The book as the flush of the person would write it now: the change has to have marked the quests.
        /// </summary>
        public string Snapshot()
        {
            Assert.IsTrue(World.DirtyOf(Player)!.Sections.HasFlag(PersonSections.Quests), "the change marks the quests for the flush");

            return QuestManager.Document.Snapshot(World, Player)!;
        }

        public QuestBook Saved() => JsonSerializer.Deserialize<QuestBook>(Snapshot())!;

        /// <summary>
        /// The next frame the client gets: group, command and the rest.
        /// </summary>
        public (byte Group, byte Command, byte[] Body) Next()
        {
            var header = new MessageHeader(Reader);
            var content = Reader.ReadBytes(header.ContentLength);

            MessageEncryption.Exchange(content, header.Version);

            return (content[0], content[1], content[2..]);
        }

        public bool Quiet(int milliseconds = 200)
        {
            Client.ReceiveTimeout = milliseconds;

            try
            {
                Next();

                return false;
            }

            catch (IOException)
            {
                return true;
            }

            finally
            {
                Client.ReceiveTimeout = 2000;
            }
        }

        /// <summary>
        /// The next frame, which has to be the refusal of the command with the code.
        /// </summary>
        public void ExpectError(byte command, ushort code)
        {
            var (group, sub, body) = Next();

            Assert.AreEqual(0x15, group);
            Assert.AreEqual(command | 0x80, sub);
            Assert.AreEqual(code, BitConverter.ToUInt16(body));
        }

        public void Dispose()
        {
            Reader.Dispose();
            Client.Dispose();
            _server.Dispose();
            _listener.Stop();
        }
    }

    private static int Int(byte[] body, int offset) => BitConverter.ToInt32(body, offset);

    #region Entering

    [TestMethod]
    public async Task EnteringTellsTheEpisodesInProgressThenTheFinishedOnes()
    {
        using var test = new Harness();

        await test.Enter();

        // The client reads the finished episodes against the ones it already has in progress.
        var list = test.Next();
        var finished = test.Next();

        Assert.AreEqual((0x15, 0x01), (finished.Group, finished.Command));
        Assert.HasCount(128, finished.Body);

        Assert.AreEqual((0x15, 0x02), (list.Group, list.Command));
        Assert.AreEqual((short)0, BitConverter.ToInt16(list.Body));
        Assert.HasCount(2, list.Body);
    }

    [TestMethod]
    public async Task AStoredBookIsLoadedAndTold()
    {
        var store = new MemoryStore();
        var book = new QuestBook { Episodes = [QuestRules.Start(Hard, true)] };
        book.Episodes[0].Steps[1] = 2;
        book.Episodes[0].Done = 1;
        book.SetFinished(77, true);
        store.Documents[1] = JsonSerializer.Serialize(book);

        using var test = new Harness(store);

        await test.Enter();

        var list = test.Next().Body;
        var finished = test.Next();

        Assert.AreEqual((byte)(1 << 5), finished.Body[9]);

        Assert.AreEqual((short)1, BitConverter.ToInt16(list));

        // The episode, the helper flag, the bits of done conditions, the failure flag, then ten conditions.
        Assert.AreEqual(100, Int(list, 2));
        Assert.AreEqual((byte)1, list[6]);
        Assert.AreEqual((short)1, BitConverter.ToInt16(list, 7));
        Assert.AreEqual((byte)0, list[9]);
        Assert.AreEqual(1001, Int(list, 10));
        Assert.AreEqual((byte)0, list[14]);
        Assert.AreEqual(1002, Int(list, 15));
        Assert.AreEqual((byte)2, list[19]);
        Assert.AreEqual(0, Int(list, 20), "an empty slot has no condition");
        Assert.HasCount(2 + 4 + 1 + 2 + 1 + 50, list);
    }

    [TestMethod]
    public async Task ABookTheTablesNoLongerFitIsMendedOnEntering()
    {
        var store = new MemoryStore();
        store.Documents[1] = JsonSerializer.Serialize(new QuestBook { Episodes = [new QuestEpisodeState { Episode = 4242 }] });

        using var test = new Harness(store);

        await test.Enter();
        test.Next();

        Assert.AreEqual((short)0, BitConverter.ToInt16(test.Next().Body));
    }

    #endregion Entering

    #region Taking

    [TestMethod]
    public async Task TakingAnEpisodeIsToldAndSaved()
    {
        using var test = new Harness();

        await test.Enter();
        test.Next();
        test.Next();

        bool taken = false;
        await test.Run(async () => taken = await test.Manager.AcceptAsync(test.Player, 100, CancellationToken.None));

        var accept = test.Next();

        Assert.IsTrue(taken);
        Assert.AreEqual((0x15, 0x03), (accept.Group, accept.Command));
        Assert.AreEqual(100, Int(accept.Body, 0));
        Assert.AreEqual((byte)1, accept.Body[4]);

        Assert.IsTrue(test.Manager.HasActiveEpisode(test.Player, 100));
        Assert.HasCount(1, test.Saved().Episodes);

        // What the episode hands over with it goes through the granter.
        var (reward, source) = test.Granter.Given.Single();

        Assert.AreEqual(RewardSource.Quest, source);
        CollectionAssert.AreEqual(new[] { new RewardItem(Sword, 1) }, reward.Items.ToArray());
    }

    [TestMethod]
    public async Task AnEpisodeCanNotBeTakenTwice()
    {
        using var test = new Harness();

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () =>
        {
            await test.Manager.AcceptAsync(test.Player, 100, CancellationToken.None);
            await test.Manager.AcceptAsync(test.Player, 100, CancellationToken.None);
        });

        Assert.AreEqual(0x03, test.Next().Command);
        test.ExpectError(0x03, QuestErrors.AlreadyTaken);
        Assert.IsTrue(test.Quiet());
    }

    [TestMethod]
    public async Task ARequestThatBreaksARuleIsRefusedWithItsCode()
    {
        using var test = new Harness();

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () =>
        {
            await test.Manager.AcceptAsync(test.Player, 4242, CancellationToken.None);
            await test.Manager.AcceptAsync(test.Player, 500, CancellationToken.None);
            await test.Manager.AcceptAsync(test.Player, 600, CancellationToken.None);
        });

        test.ExpectError(0x03, QuestErrors.UnknownEpisode);
        test.ExpectError(0x03, QuestErrors.NotAvailableYet);
        test.ExpectError(0x03, QuestErrors.LevelTooLow);
        Assert.IsFalse(test.Manager.HasAnyActiveEpisode(test.Player));
    }

    [TestMethod]
    public async Task ARefusedGiftKeepsTheEpisodeAway()
    {
        using var test = new Harness(granter: new Granter(false));

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () => await test.Manager.AcceptAsync(test.Player, 100, CancellationToken.None));

        test.ExpectError(0x03, QuestErrors.ObjectsNotGiven);
        Assert.IsFalse(test.Manager.HasAnyActiveEpisode(test.Player));
    }

    [TestMethod]
    public async Task ItemsThePersonHasCountTowardsACollectingCondition()
    {
        var inventory = new Inventory { Have = { [880001] = 3 } };
        using var test = new Harness(inventory: inventory);

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () => await test.Manager.AcceptAsync(test.Player, 400, CancellationToken.None));

        Assert.AreEqual(0x03, test.Next().Command);

        var update = test.Next();

        Assert.AreEqual(0x07, update.Command);
        Assert.AreEqual((short)1, BitConverter.ToInt16(update.Body));
        Assert.AreEqual(1020, Int(update.Body, 2));
        Assert.AreEqual((byte)3, update.Body[6]);
    }

    /// <summary>
    /// A collecting condition counts the items as they arrive and does not go down when they are sold. It is counted again
    /// from what is held only when the episode is taken and when another condition of the person is done: then it is set to
    /// the items held, down as well as up, unless nothing of the item is held at all.
    /// </summary>
    [TestMethod]
    public async Task ACollectingConditionIsCountedAgainFromWhatIsHeldWhenAnotherConditionIsDone()
    {
        var inventory = new Inventory { Have = { [880001] = 3 } };
        using var test = new Harness(inventory: inventory);

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () => await test.Manager.AcceptAsync(test.Player, 450, CancellationToken.None));

        Assert.AreEqual(0x03, test.Next().Command);

        var update = test.Next();

        Assert.AreEqual(0x07, update.Command);
        Assert.AreEqual(1020, Int(update.Body, 2));
        Assert.AreEqual((byte)3, update.Body[6], "The three held at the start.");

        // Two are sold: the count stays where it was, however many leave.
        inventory.Have[880001] = 1;

        // The hunt is done: the items are counted again, and one is what is held.
        await test.Run(async () => await test.Manager.OnMonsterKilledAsync(test.Player, Boar, CancellationToken.None));
        await test.Settle();

        var steps = test.Book.Find(450)!.Steps;

        Assert.AreEqual((byte)1, steps[0], "The hunt is done.");
        Assert.AreEqual((byte)1, steps[1], "The tusks are set to what is held, down from three.");

        // A person who holds none of them is left as it stands.
        await test.Run(async () => await test.Manager.OnItemCollectedAsync(test.Player, 880001, 2, CancellationToken.None));

        inventory.Have.Clear();

        await test.Run(async () => await test.Manager.AcceptAsync(test.Player, 150, CancellationToken.None));
        await test.Run(async () => await test.Manager.OnMonsterKilledAsync(test.Player, Boar, CancellationToken.None));
        await test.Settle();

        Assert.AreEqual((byte)3, test.Book.Find(450)!.Steps[1], "Nothing held, so nothing is taken off.");
    }

    [TestMethod]
    public async Task AScriptTakesAnEpisodeWithoutTheChecks()
    {
        using var test = new Harness();

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(() =>
        {
            test.Manager.AcceptEpisode(test.Player, 500);

            return ValueTask.CompletedTask;
        });

        Assert.AreEqual(0x03, test.Next().Command);
        Assert.IsTrue(test.Manager.HasActiveEpisode(test.Player, 500));
    }

    #endregion Taking

    #region Progress

    [TestMethod]
    public async Task TalkingMovesAConditionTheClientSees()
    {
        using var test = new Harness();

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () =>
        {
            await test.Manager.AcceptAsync(test.Player, 100, CancellationToken.None);
            await test.Manager.OnEventUpdateAsync(test.Player, 1001, Npc + 1, CancellationToken.None);
            await test.Manager.OnEventUpdateAsync(test.Player, 1001, Npc, CancellationToken.None);
        });

        test.Next();

        var update = test.Next();

        Assert.AreEqual(0x07, update.Command);
        Assert.AreEqual(1001, Int(update.Body, 2));
        Assert.AreEqual((byte)1, update.Body[6]);
        Assert.IsTrue(test.Quiet(), "a wrong character moves nothing and is not answered");

        Assert.IsTrue(test.Book.Episodes[0].IsDone(0));
    }

    [TestMethod]
    public async Task TheClientCanNotMakeUpAKill()
    {
        using var test = new Harness();

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () =>
        {
            await test.Manager.AcceptAsync(test.Player, 150, CancellationToken.None);
            await test.Manager.OnEventUpdateAsync(test.Player, 1010, Boar, CancellationToken.None);
        });

        test.Next();

        Assert.IsTrue(test.Quiet());
        Assert.IsTrue(test.Manager.HasActiveEpisode(test.Player, 150));
    }

    [TestMethod]
    public async Task AKillCountsForTheMonsterTheConditionNames()
    {
        using var test = new Harness();

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () =>
        {
            await test.Manager.AcceptAsync(test.Player, 100, CancellationToken.None);
            await test.Manager.OnEventUpdateAsync(test.Player, 1001, Npc, CancellationToken.None);
            await test.Manager.OnMonsterKilledAsync(test.Player, Wolf, CancellationToken.None);
            await test.Manager.OnMonsterKilledAsync(test.Player, Boar, CancellationToken.None);
        });

        test.Next();
        test.Next();

        var kill = test.Next();

        Assert.AreEqual(0x07, kill.Command);
        Assert.AreEqual(1002, Int(kill.Body, 2));
        Assert.AreEqual((byte)1, kill.Body[6]);
        Assert.IsTrue(test.Quiet(), "a boar is not a wolf");
    }

    [TestMethod]
    public async Task AnEpisodeThatEndsByItselfPaysAsSoonAsItsLastConditionIsDone()
    {
        using var test = new Harness();

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () =>
        {
            await test.Manager.AcceptAsync(test.Player, 150, CancellationToken.None);
            await test.Manager.OnMonsterKilledAsync(test.Player, Boar, CancellationToken.None);
        });

        test.Next();

        Assert.AreEqual(0x07, test.Next().Command);

        var complete = test.Next();

        Assert.AreEqual(0x05, complete.Command);
        Assert.AreEqual(150, Int(complete.Body, 0));
        Assert.AreEqual(5, Int(complete.Body, 8), "the money");

        Assert.IsFalse(test.Manager.HasActiveEpisode(test.Player, 150));
        Assert.IsTrue(test.Manager.HasFinishedEpisode(test.Player, 150));
        Assert.IsTrue(test.Saved().IsFinished(Quick.CompleteBit));
        Assert.AreEqual(5, test.Granter.Given.Single().Reward.Gold);
    }

    [TestMethod]
    public async Task AClearedMazeAndCollectedItemsCountToo()
    {
        var inventory = new Inventory();
        using var test = new Harness(inventory: inventory);

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () =>
        {
            test.World.Get<QuestComponent>(test.Player).Book!.Episodes.Add(QuestRules.Start(Late, false));
            test.World.Get<QuestComponent>(test.Player).Book!.Episodes.Add(QuestRules.Start(Gathering, false));

            await test.Manager.OnMazeClearedAsync(test.Player, 21111, CancellationToken.None);
            await test.Manager.OnItemCollectedAsync(test.Player, 880001, 2, CancellationToken.None);
            await test.Manager.OnItemCollectedAsync(test.Player, 880002, 2, CancellationToken.None);
        });

        var maze = test.Next();
        var item = test.Next();

        Assert.AreEqual(1030, Int(maze.Body, 2));
        Assert.AreEqual(1020, Int(item.Body, 2));
        Assert.AreEqual((byte)2, item.Body[6]);
        Assert.IsTrue(test.Quiet());
    }

    /// <summary>
    /// Items that come to another person in the maze count for a condition that counts for everyone in it, though the person
    /// has none of them, and for no other condition.
    /// </summary>
    [TestMethod]
    public async Task ItemsAnotherPersonInTheMazeGetsCountForTheConditionsThatCountForEveryone()
    {
        static QuestConditionRuleRow Rule(uint id, bool group)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(id);
            writer.Write(new byte[1 + 4 + 4 + 1 + 1 + 4 + 4 + 4 + 24 * 4]);
            writer.Write(group ? (byte)1 : (byte)0);
            writer.Write(new byte[1 + 8 + 2 + 8 + 2 + 12]);
            writer.Write((ushort)0);
            writer.Flush();
            stream.Position = 0;

            return new QuestConditionRuleRow(new BinaryReader(stream));
        }

        // The condition that asks for the tusks is counted for the group, the one that asks for a maze is not.
        using var shared = new Harness(rules: new ReadOnlyCollection<QuestConditionRuleRow>([Rule(1020, true)]));
        using var alone = new Harness(rules: new ReadOnlyCollection<QuestConditionRuleRow>([Rule(1020, false)]));

        foreach (var test in new[] { shared, alone })
        {
            await test.Enter();
            test.Next();
            test.Next();

            await test.Run(() =>
            {
                test.World.Get<QuestComponent>(test.Player).Book!.Episodes.Add(QuestRules.Start(Gathering, false));
                test.Manager.OnItemsArrivedForGroup(test.Player, 880001, 2);

                return ValueTask.CompletedTask;
            });

            await test.Settle();
        }

        var update = shared.Next();

        Assert.AreEqual(1020, Int(update.Body, 2));
        Assert.AreEqual((byte)2, update.Body[6], "The items count, though the person has none");
        Assert.IsTrue(alone.Quiet(), "A condition that does not count for the group counts nothing");
    }

    /// <summary>
    /// A mode won in a maze counts for the open conditions of its kind that name the maze, and for none of another kind or of
    /// another maze.
    /// </summary>
    [TestMethod]
    public async Task AModeWonCountsForTheConditionsOfItsKindThatNameTheMaze()
    {
        using var test = new Harness();

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () =>
        {
            test.World.Get<QuestComponent>(test.Player).Book!.Episodes.Add(QuestRules.Start(Holdout, false));

            await test.Manager.OnMazeModeWonAsync(test.Player, QuestConditionType.Protect, SurvivalMaze, CancellationToken.None);
            await test.Manager.OnMazeModeWonAsync(test.Player, QuestConditionType.Survival, SurvivalMaze + 1, CancellationToken.None);
        });

        Assert.IsTrue(test.Quiet(), "Another kind, another maze");

        await test.Run(async () => await test.Manager.OnMazeModeWonAsync(test.Player, QuestConditionType.Survival, SurvivalMaze, CancellationToken.None));

        var update = test.Next();

        Assert.AreEqual(1050, Int(update.Body, 2));
        Assert.AreEqual((byte)1, update.Body[6]);
    }

    /// <summary>
    /// A won mode tells whether a condition it counted counts for everyone in the maze, so that the players who did not stand
    /// are counted too.
    /// </summary>
    [TestMethod]
    public async Task AModeWonSaysWhetherTheConditionCountsForTheGroup()
    {
        static QuestConditionRuleRow Rule(uint id, bool group)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(id);
            writer.Write(new byte[1 + 4 + 4 + 1 + 1 + 4 + 4 + 4 + 24 * 4]);
            writer.Write(group ? (byte)1 : (byte)0);
            writer.Write(new byte[1 + 8 + 2 + 8 + 2 + 12]);
            writer.Write((ushort)0);
            writer.Flush();
            stream.Position = 0;

            return new QuestConditionRuleRow(new BinaryReader(stream));
        }

        using var shared = new Harness(rules: new ReadOnlyCollection<QuestConditionRuleRow>([Rule(1050, true)]));
        using var alone = new Harness(rules: new ReadOnlyCollection<QuestConditionRuleRow>([Rule(1050, false)]));
        var told = new List<bool>();

        foreach (var test in new[] { shared, alone })
        {
            await test.Enter();
            test.Next();
            test.Next();

            await test.Run(async () =>
            {
                test.World.Get<QuestComponent>(test.Player).Book!.Episodes.Add(QuestRules.Start(Holdout, false));

                told.Add(await test.Manager.OnMazeModeWonAsync(test.Player, QuestConditionType.Survival, SurvivalMaze, CancellationToken.None));
            });
        }

        CollectionAssert.AreEqual(new[] { true, false }, told);
    }

    #endregion Progress

    #region Finishing

    [TestMethod]
    public async Task AnEpisodeIsFinishedOnlyWhenItsConditionsAreDone()
    {
        using var test = new Harness();

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () =>
        {
            await test.Manager.AcceptAsync(test.Player, 100, CancellationToken.None);
            await test.Manager.CompleteAsync(test.Player, 100, 0, CancellationToken.None);
        });

        test.Next();
        test.ExpectError(0x05, QuestErrors.CannotFinish);
        Assert.IsTrue(test.Manager.HasActiveEpisode(test.Player, 100));
    }

    [TestMethod]
    public async Task FinishingAnEpisodeGivesTheRewardAndTellsIt()
    {
        using var test = new Harness();

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () =>
        {
            await test.Manager.AcceptAsync(test.Player, 100, CancellationToken.None);
            await test.Manager.OnEventUpdateAsync(test.Player, 1001, Npc, CancellationToken.None);

            for (var i = 0; i < 3; i++)
            {
                await test.Manager.OnMonsterKilledAsync(test.Player, Wolf, CancellationToken.None);
            }

            await test.Manager.CompleteAsync(test.Player, 100, 0, CancellationToken.None);
        });

        for (var i = 0; i < 5; i++)
        {
            test.Next();
        }

        var complete = test.Next();

        Assert.AreEqual(0x05, complete.Command);

        var body = complete.Body;

        Assert.AreEqual(100, Int(body, 0));
        Assert.AreEqual(1000, Int(body, 4), "experience");
        Assert.AreEqual(50, Int(body, 8), "money");
        Assert.AreEqual(3, Int(body, 12), "battle points");
        Assert.AreEqual(7, Int(body, 16), "ether");
        Assert.AreEqual(100100, Int(body, 20), "title before the name");
        Assert.AreEqual(0, Int(body, 24), "title after the name");
        Assert.AreEqual(0, Int(body, 28), "helper");
        Assert.AreEqual((byte)1, body[32]);
        Assert.AreEqual(710002, Int(body, 33));
        Assert.AreEqual((short)2, BitConverter.ToInt16(body, 37));

        var reward = test.Granter.Given.Last().Reward;

        CollectionAssert.AreEqual(new[] { 100100 }, test.Titles.Granted, "the title is kept, not only told");
        Assert.AreEqual(1000, reward.Experience);
        Assert.AreEqual(50, reward.Gold);
        Assert.AreEqual(3, reward.BattlePoints);
        Assert.AreEqual(7, reward.Ether);

        var book = test.Saved();

        Assert.IsEmpty(book.Episodes);
        Assert.IsTrue(book.IsFinished(Hard.CompleteBit));
    }

    [TestMethod]
    public async Task ARepeatedEpisodeComesBackAfterTheDayIsOver()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 9, 12, 0, 0, TimeSpan.Zero));

        using var test = new Harness(time: time);

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () =>
        {
            await test.Manager.AcceptAsync(test.Player, 700, CancellationToken.None);
            await test.Manager.OnMonsterKilledAsync(test.Player, Boar, CancellationToken.None);
            await test.Manager.CompleteAsync(test.Player, 700, 0, CancellationToken.None);
            await test.Manager.AcceptAsync(test.Player, 700, CancellationToken.None);
        });

        test.Next();
        test.Next();
        Assert.AreEqual(0x05, test.Next().Command);
        test.ExpectError(0x03, QuestErrors.DoneForToday);
        Assert.IsFalse(test.Manager.HasFinishedEpisode(test.Player, 700), "a repeated episode is never finished for good");

        // The next game day starts at nine in the morning.
        time.Advance(TimeSpan.FromHours(21));

        await test.Run(async () => await test.Manager.AcceptAsync(test.Player, 700, CancellationToken.None));

        Assert.AreEqual(0x03, test.Next().Command);
        Assert.IsTrue(test.Manager.HasActiveEpisode(test.Player, 700));
        Assert.AreEqual(1, test.Saved().Repeats.Single().Count, "the count of the day before stays until the next finish");
    }

    [TestMethod]
    public async Task ATalkTheServerCanCheckCountsOnlyWhenItHappened()
    {
        using var test = new Harness();

        test.Npcs.Reach = NpcReach.Far;

        await test.Enter();
        test.Next();
        test.Next();

        bool far = true, empty = true, done = false;

        await test.Run(async () =>
        {
            await test.Manager.AcceptAsync(test.Player, 800, CancellationToken.None);

            far = await test.Manager.OnEventUpdateAsync(test.Player, 1040, Npc, CancellationToken.None);

            test.Npcs.Reach = NpcReach.Near;

            empty = await test.Manager.OnEventUpdateAsync(test.Player, 1040, Npc, CancellationToken.None);

            test.Inventory.Have[Sword] = 1;

            done = await test.Manager.OnEventUpdateAsync(test.Player, 1040, Npc, CancellationToken.None);
        });

        Assert.IsFalse(far, "a talk far from the character does not count");
        Assert.IsFalse(empty, "handing in what the person does not have does not count");
        Assert.IsTrue(done);

        Assert.AreEqual(0x03, test.Next().Command);
        Assert.AreEqual(0x07, test.Next().Command);
        Assert.IsTrue(test.Quiet(), "the refused claims are not answered");
        CollectionAssert.AreEqual(new[] { new QuestItem(Sword, 1) }, test.Inventory.Taken, "what is handed in is taken");
    }

    [TestMethod]
    public async Task AnEpisodeThatStartsByItselfFollowsTheOneBeforeIt()
    {
        var store = new MemoryStore();

        using (var test = new Harness(store))
        {
            await test.Enter();
            test.Next();
            test.Next();

            await test.Run(async () =>
            {
                await test.Manager.AcceptAsync(test.Player, 910, CancellationToken.None);
                await test.Manager.OnMonsterKilledAsync(test.Player, Boar, CancellationToken.None);
                await test.Manager.CompleteAsync(test.Player, 910, 0, CancellationToken.None);
            });

            test.Next();
            test.Next();
            Assert.AreEqual(0x05, test.Next().Command);

            var taken = test.Next();

            Assert.AreEqual((0x15, 0x03), (taken.Group, taken.Command));
            Assert.AreEqual(920, Int(taken.Body, 0));
        }

        // A person who finished the episode before it without getting it, on another server say, gets it on entering.
        var book = new QuestBook();
        book.SetFinished(Prequel.CompleteBit, true);
        store.Documents[1] = JsonSerializer.Serialize(book);

        using var returning = new Harness(store);

        await returning.Enter();

        var list = returning.Next().Body;

        Assert.AreEqual((short)1, BitConverter.ToInt16(list));
        Assert.AreEqual(920, Int(list, 2));
        Assert.AreEqual(0x01, returning.Next().Command, "the list carries it, no message of its own");
        Assert.IsTrue(returning.Saved().Find(920) is not null);
    }

    [TestMethod]
    public async Task AFinishedEpisodeCanNotBeFinishedOrTakenAgain()
    {
        using var test = new Harness();

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () =>
        {
            await test.Manager.AcceptAsync(test.Player, 150, CancellationToken.None);
            await test.Manager.OnMonsterKilledAsync(test.Player, Boar, CancellationToken.None);
            await test.Manager.CompleteAsync(test.Player, 150, 0, CancellationToken.None);
            await test.Manager.AcceptAsync(test.Player, 150, CancellationToken.None);
        });

        test.Next();
        test.Next();
        test.Next();

        test.ExpectError(0x05, QuestErrors.CannotFinish);
        test.ExpectError(0x03, QuestErrors.AlreadyFinished);
        Assert.HasCount(1, test.Granter.Given, "the reward is given once");
    }

    [TestMethod]
    public async Task ARewardThatDoesNotFitLeavesTheEpisodeWhereItWas()
    {
        using var test = new Harness(granter: new Granter(false));

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () =>
        {
            test.Book.Episodes.Add(new QuestEpisodeState { Episode = 150, Done = 1 });

            await test.Manager.CompleteAsync(test.Player, 150, 0, CancellationToken.None);
        });

        test.ExpectError(0x05, QuestErrors.ItemsNotGiven);
        Assert.IsTrue(test.Manager.HasActiveEpisode(test.Player, 150));
        Assert.IsFalse(test.Manager.HasFinishedEpisode(test.Player, 150));
        Assert.IsTrue(test.Quiet());
    }

    [TestMethod]
    public async Task AScriptFinishesAnEpisodeWhoseConditionsAreNotDone()
    {
        using var test = new Harness();

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () =>
        {
            await test.Manager.AcceptAsync(test.Player, 100, CancellationToken.None);

            test.Manager.CompleteEpisode(test.Player, 100);
        });

        Assert.AreEqual(0x03, test.Next().Command);
        Assert.AreEqual(0x05, test.Next().Command);
        Assert.IsTrue(test.Manager.HasFinishedEpisode(test.Player, 100));
    }

    [TestMethod]
    public async Task ItemsAnEpisodeTakesAreTakenWhenItIsFinished()
    {
        var inventory = new Inventory();
        using var test = new Harness(inventory: inventory);

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () =>
        {
            await test.Manager.AcceptAsync(test.Player, 400, CancellationToken.None);
            await test.Manager.OnItemCollectedAsync(test.Player, 880001, 4, CancellationToken.None);

            // The items were counted when they came and are gone since: the episode is not finished without them.
            Assert.IsFalse(await test.Manager.CompleteAsync(test.Player, 400, 0, CancellationToken.None));
            Assert.IsEmpty(inventory.Taken);

            inventory.Have[880001] = 4;

            Assert.IsTrue(await test.Manager.CompleteAsync(test.Player, 400, 0, CancellationToken.None));
        });

        CollectionAssert.AreEqual(new[] { new QuestItem(880001, 4) }, inventory.Taken);
        Assert.IsTrue(test.Manager.HasFinishedEpisode(test.Player, 400));
    }

    #endregion Finishing

    #region Giving up, failing, helper and reset

    [TestMethod]
    public async Task GivingUpRemovesTheEpisodeAndTakesBackWhatItGave()
    {
        var inventory = new Inventory();
        using var test = new Harness(inventory: inventory);

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () =>
        {
            await test.Manager.AcceptAsync(test.Player, 100, CancellationToken.None);
            await test.Manager.GiveUpAsync(test.Player, 100, CancellationToken.None);
        });

        test.Next();

        var gone = test.Next();

        Assert.AreEqual(0x06, gone.Command);
        Assert.AreEqual(100, Int(gone.Body, 0));
        Assert.IsFalse(test.Manager.HasActiveEpisode(test.Player, 100));
        Assert.IsFalse(test.Manager.HasFinishedEpisode(test.Player, 100));
        CollectionAssert.AreEqual(new[] { new QuestItem(Sword, 1) }, inventory.Taken);
    }

    [TestMethod]
    public async Task AnEpisodeThatCanNotBeGivenUpOrIsNotThereIsRefused()
    {
        using var test = new Harness();

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () =>
        {
            await test.Manager.AcceptAsync(test.Player, 300, CancellationToken.None);
            await test.Manager.GiveUpAsync(test.Player, 300, CancellationToken.None);
            await test.Manager.GiveUpAsync(test.Player, 100, CancellationToken.None);
        });

        test.Next();
        test.ExpectError(0x06, QuestErrors.CannotGiveUp);
        test.ExpectError(0x06, QuestErrors.CannotGiveUp);
        Assert.IsTrue(test.Manager.HasActiveEpisode(test.Player, 300));
    }

    [TestMethod]
    public async Task AFailedEpisodeStaysUntilTheClientAsksToRemoveIt()
    {
        using var test = new Harness();

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () =>
        {
            await test.Manager.AcceptAsync(test.Player, 150, CancellationToken.None);
            await test.Manager.FailAsync(test.Player, 150);
            await test.Manager.OnMonsterKilledAsync(test.Player, Boar, CancellationToken.None);
            await test.Manager.RemoveFailedAsync(test.Player, 100);
        });

        test.Next();

        var fail = test.Next();

        Assert.AreEqual(0x11, fail.Command);
        Assert.AreEqual(150, Int(fail.Body, 0));
        Assert.IsTrue(test.Quiet(), "a failed episode counts nothing, and removing one that has not failed does nothing");
        Assert.IsTrue(test.Saved().Episodes.Single().Failed);

        await test.Run(async () => await test.Manager.RemoveFailedAsync(test.Player, 150));

        Assert.IsTrue(test.Quiet(), "the client is not answered when it removes the episode");
        Assert.IsFalse(test.Manager.HasActiveEpisode(test.Player, 150));
        Assert.IsEmpty(test.Saved().Episodes);
    }

    [TestMethod]
    public async Task TheHelperShowsAnEpisodeUntilItIsFull()
    {
        using var test = new Harness();

        await test.Enter();
        test.Next();
        test.Next();

        var book = test.Book;

        for (var i = 0; i < QuestRules.MaxHelperEpisodes; i++)
        {
            book.Episodes.Add(new QuestEpisodeState { Episode = 9000 + i, Helper = true });
        }

        await test.Run(async () =>
        {
            await test.Manager.AcceptAsync(test.Player, 150, CancellationToken.None);
            await test.Manager.SetHelperAsync(test.Player, QuestHelperType.Add, 150);
            await test.Manager.SetHelperAsync(test.Player, QuestHelperType.Delete, 9000);
            await test.Manager.SetHelperAsync(test.Player, QuestHelperType.Add, 150);
            await test.Manager.SetHelperAsync(test.Player, QuestHelperType.Add, 777);
        });

        var accept = test.Next();

        Assert.AreEqual((byte)0, accept.Body[4], "the helper had no room for it");

        test.ExpectError(0x09, QuestErrors.NoRoomInHelper);

        var removed = test.Next();

        Assert.AreEqual(0x09, removed.Command);
        Assert.AreEqual((byte)QuestHelperType.Delete, removed.Body[0]);
        Assert.AreEqual(9000, Int(removed.Body, 1));

        var added = test.Next();

        Assert.AreEqual((byte)QuestHelperType.Add, added.Body[0]);
        Assert.AreEqual(150, Int(added.Body, 1));

        test.ExpectError(0x09, QuestErrors.UnknownEpisode);
    }

    [TestMethod]
    public async Task ResettingAnEpisodeTakesItAndItsFinishAway()
    {
        using var test = new Harness();

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () =>
        {
            await test.Manager.AcceptAsync(test.Player, 150, CancellationToken.None);
            await test.Manager.OnMonsterKilledAsync(test.Player, Boar, CancellationToken.None);
            await test.Manager.ResetEpisodeAsync(test.Player, 150);
        });

        for (var i = 0; i < 3; i++)
        {
            test.Next();
        }

        var reset = test.Next();

        Assert.AreEqual(0x12, reset.Command);
        Assert.AreEqual(150, Int(reset.Body, 0));
        Assert.IsFalse(test.Manager.HasFinishedEpisode(test.Player, 150));
    }

    [TestMethod]
    public async Task ResettingEverythingEmptiesTheBook()
    {
        using var test = new Harness();

        await test.Enter();
        test.Next();
        test.Next();

        await test.Run(async () =>
        {
            await test.Manager.AcceptAsync(test.Player, 100, CancellationToken.None);
            test.Manager.RemoveAllActiveEpisodes(test.Player);
        });

        test.Next();

        var reset = test.Next();

        Assert.AreEqual((0x15, 0x10), (reset.Group, reset.Command));
        Assert.IsEmpty(reset.Body);

        await test.Settle();

        Assert.IsFalse(test.Manager.HasAnyActiveEpisode(test.Player));
        Assert.IsEmpty(test.Saved().Episodes);
    }

    #endregion Giving up, failing, helper and reset

    [TestMethod]
    public async Task WhatWasSavedIsThereWhenThePersonReturns()
    {
        var store = new MemoryStore();

        using (var first = new Harness(store))
        {
            await first.Enter();
            first.Next();
            first.Next();

            await first.Run(async () =>
            {
                await first.Manager.AcceptAsync(first.Player, 100, CancellationToken.None);
                await first.Manager.OnEventUpdateAsync(first.Player, 1001, Npc, CancellationToken.None);
                await first.Manager.AcceptAsync(first.Player, 150, CancellationToken.None);
                await first.Manager.OnMonsterKilledAsync(first.Player, Boar, CancellationToken.None);
            });

            // What the flush of the person writes when it leaves.
            store.Documents[1] = first.Snapshot();
        }

        using var second = new Harness(store);

        await second.Enter();

        var list = second.Next();
        var finished = second.Next();

        Assert.AreNotEqual((byte)0, finished.Body[0], "the episode that was finished is a bit of the list");
        Assert.AreEqual((short)1, BitConverter.ToInt16(list.Body));
        Assert.AreEqual(100, Int(list.Body, 2));
        Assert.AreEqual((short)1, BitConverter.ToInt16(list.Body, 7));
    }

    [TestMethod]
    public async Task APersonWhoseQuestsAreNotLoadedGetsNothingAndBreaksNothing()
    {
        using var test = new Harness();

        await test.Run(async () =>
        {
            Assert.IsFalse(await test.Manager.AcceptAsync(test.Player, 100, CancellationToken.None));
            await test.Manager.OnMonsterKilledAsync(test.Player, Wolf, CancellationToken.None);
            Assert.IsFalse(test.Manager.HasAnyActiveEpisode(test.Player));
        });

        Assert.IsTrue(test.Quiet());
    }
}
