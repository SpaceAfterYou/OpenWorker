using System.Globalization;
using System.Numerics;
using System.Text;
using Arch.Core;
using Lua;
using Lua.Standard;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Gameplay.Modules.Quests.Components;
using OpenWorker.Hotspot.Modules.Chat.Responses;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Hotspot.Modules.Maze.Types;
using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua;
using OpenWorker.Lua.Managers;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The escort of a maze: a monster the script names walks the escort points of the map, the clients hear of every point, and the
/// quest of the party is counted when it arrives and fails when it falls. The maze runs on a clock stepped by hand.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class EscortTest
{
    private const int Sector = 10;
    private const int Escorted = 5000;
    private const int Episode = 100;
    private const int FirstGuard = 1;
    private const int SecondGuard = 2;

    private static string Vector(float x, float y, float z) => string.Create(CultureInfo.InvariantCulture, $"{x:0.000},{y:0.000},{z:0.000}");

    private static string Basic(int id, Vector3 low, Vector3 high, Vector3 size) =>
        $"""<m_ID type="0" value="{id}" /> <m_iGenerateID type="0" value="{id}" /> <iLayerBitmask type="4" value="Common" /> <m_vPosTopLeft type="7" value="{Vector(low.X, low.Y, low.Z)}" /> <m_vPosBottomRight type="7" value="{Vector(high.X, high.Y, high.Z)}" /> <m_fRotation type="2" value="0.0000" /> <m_vSize type="7" value="{Vector(size.X, size.Y, size.Z)}" /> <m_bShowCustomEntity type="5" value="TRUE" /> <m_ColorLDR type="132" value="0,0,0,255" />""";

    private static string SectorBox() =>
        $"""<VSectorBox boxtype="eEventBoxType_Sector"> {Basic(Sector, Vector3.Zero, new Vector3(100000f), new Vector3(100000f))} <m_eType type="4" value="General" /> <m_szEnterLua type="12" value="" /> <m_eClearType type="4" value="None" /> <m_iClearKillRatio type="0" value="0" /> <m_szClearLua type="12" value="" /> <m_iSectorTitle type="0" value="0" /> <m_iSectorExitType type="4" value="Portal" /> <m_iSectorExitID type="0" value="0" /> <m_iRelativeSectorID type="0" value="0" /> <m_iConditionKillRatio_1Step type="0" value="0" /> <m_iConditionKillRatio_2Step type="0" value="0" /> <m_iConditionKillRatio_3Step type="0" value="0" /> <m_iConditionKillRatio_4Step type="0" value="0" /> <m_iConditionKillRatio_5Step type="0" value="0" /> </VSectorBox>""";

    /// <summary>
    /// The box the escorted appears in at once, on the way that starts at point 401.
    /// </summary>
    private static string SpawnBox()
    {
        var text = new StringBuilder($"""<VMonsterSpawnBox boxtype="eEventBoxType_MonsterSpawn"> {Basic(101, new Vector3(500f, 500f, 0f), new Vector3(600f, 600f, 100f), new Vector3(100f))} """);

        for (var slot = 1; slot <= 10; slot++)
        {
            var monster = slot == 1 ? Escorted : 0;

            text.Append(CultureInfo.InvariantCulture, $"""<m_eType{slot} type="4" value="Monster" /> <m_iMonsterID{slot} type="0" value="{monster}" /> <m_iChance{slot} type="0" value="{(monster == 0 ? 0 : 10000)}" /> """);
        }

        text.Append(CultureInfo.InvariantCulture, $"""<m_eCreationPositionType type="4" value="Center" /> <m_eMoveType type="4" value="Free" /> <m_eCreationCondition type="4" value="Immediate" /> <m_CreationEffectFile type="12" value="" /> <m_fWaitCreationDelayTime type="2" value="0.0000" /> <m_fWaitCreationSequenceTime type="2" value="0.0000" /> <m_iWaitCreationMaxWave type="0" value="1" /> <m_iMaxEntityCount type="0" value="1" /> <m_eWaitCreationSequenceType type="4" value="All" /> <m_iWaypoint type="0" value="401" /> <m_iAggroGroupID type="0" value="0" /> <m_iAggroDistance type="0" value="0" /> <m_iAggroMaxCount type="0" value="0" /> <m_fLookRatio type="2" value="1.0000" /> <m_bShowSight type="5" value="FALSE" /> <m_szObjectKey type="12" value="" /> <m_eScriptType type="4" value="None" /> <m_iCheckScriptHP1 type="0" value="0" /> <m_iCheckScriptHP2 type="0" value="0" /> <m_iCheckScriptHP3 type="0" value="0" /> <m_iCheckScriptHP4 type="0" value="0" /> <m_iCheckScriptHP5 type="0" value="0" /> <m_iSectorID type="0" value="{Sector}" /> <m_ChangeSpawnAction type="12" value="" /> <m_ProtectionTarget type="0" value="0" /> <m_RespawnTime type="2" value="0.0000" /> <m_iStep type="0" value="1" /> <m_eRespawnType type="4" value="None" /> <m_iRespawnCondition type="0" value="0" /> <m_iGroupID type="0" value="0" /> </VMonsterSpawnBox>""");

        return text.ToString();
    }

    /// <summary>
    /// An escort point: a box of the size around its centre, as the maps write them.
    /// </summary>
    private static string Point(int id, float x, float size, int next, string function = "")
    {
        var centre = new Vector3(x, 550f, 0f);

        return $"""<VEscortPoint pointtype="eEventPointType_Escort"> {Basic(id, centre, centre, new Vector3(size, size, 200f))} <m_eType type="4" value="StartReturnGo" /> <m_eBattleType type="4" value="None" /> <m_iBeforePoint type="0" value="0" /> <m_iNextPoint type="0" value="{next}" /> <m_iNextPoint2 type="0" value="0" /> <m_iNextPoint3 type="0" value="0" /> <m_iNextPoint4 type="0" value="0" /> <m_szIdleAction type="12" value="" /> <m_uiIdleActionRatio type="0" value="0" /> <m_uiDelayTime type="0" value="0" /> <m_RepeatCount type="0" value="0" /> <m_szFunction type="12" value="{function}" /> <m_sEnableEffectPath type="12" value="" /> <m_sDisableEffectPath type="12" value="" /> </VEscortPoint>""";
    }

    private static string Map() =>
        "<root><Batchs eventtype=\"EventBox\">" + SectorBox() + SpawnBox() + "</Batchs><Batchs eventtype=\"EventPoint\">" +
        Point(401, 550f, 400f, 402) + Point(402, 1150f, 100f, 403) + Point(403, 1750f, 100f, 0, "OnArrived") + "</Batchs></root>";

    private static QuestCatalog Catalog { get; } = QuestCatalog.From(
        [new QuestDefinition(Episode, 1, QuestContents.Side, 0, 0, [], false, false, [], [], QuestRewardDefinition.None, [FirstGuard, SecondGuard])],
        [
            new QuestConditionDefinition(FirstGuard, QuestConditionType.Guard, QuestConditionTarget.Monster, Escorted, 0, 1, 0, [], []),
            new QuestConditionDefinition(SecondGuard, QuestConditionType.Guard, QuestConditionTarget.Monster, Escorted, FirstGuard, 1, 0, [], [])
        ]);

    private static readonly string Script = $"""
        Arrived = 0 Told = 0 Calls = 0 Boxes = 0
        function OnMazeCreated(actor, maze)
            maze:SetEscortMonster({Episode}, "{Escorted}", "")
            maze:InitPartyQuest({Episode}, {FirstGuard}, {Sector})
            maze:SetEscortCondition({FirstGuard})
        end
        function OnArrived(point, maze) Arrived = point end
        function OnPartyQuestEvent(sector, maze) Told = sector Calls = Calls + 1 end
        function OnBox(box, maze) Boxes = box end
        """;

    private sealed class Test : IDisposable
    {
        private readonly FakeTimeProvider _time = new();
        private readonly SimulationClock _clock;
        private int _sentinels;

        public Test()
        {
            World = World.Create();
            Connection = new RecordingConnection();
            Player = World.Create(
                new ActorComponent(1, ActorType.User),
                new ServerSessionComponent(Connection),
                new WorldComponent { Location = 0, Position = new Vector3(100f, 100f, 0f), Rotation = 0f, Jump = 0 },
                new PersonInfoComponent { Name = "Member", Hero = Hero.Haru },
                new QuestComponent { Book = new QuestBook { Episodes = [new QuestEpisodeState { Episode = Episode }] } });

            var batch = TestMazes.Map(Map());
            var tables = new CombatTables(TestMazes.None<MonsterRow>(), TestMazes.None<StatusRow>(), TestMazes.None<SkillRow>(), TestMazes.None<CharacterInfoRow>());

            Quests = new QuestManager(World, null!, Catalog, null!, null!, null!, null!, NullLogger<QuestManager>.Instance);
            Creatures = new LuaCreatureManager(World, batch, tables);
            State = LuaState.Create();
            State.OpenStandardLibraries();

            _clock = TestMazes.Clock(_time);

            Maze = TestMazes.Create(World, Player, state: State, batch: batch, quests: Quests, creatures: Creatures, clock: _clock);

            State.DoStringAsync(Script).AsTask().GetAwaiter().GetResult();
            Run("OnMazeCreated(nil, pMaze)");
        }

        public World World { get; }
        public RecordingConnection Connection { get; }
        public Entity Player { get; }
        public QuestManager Quests { get; }
        public LuaCreatureManager Creatures { get; }
        public LuaState State { get; }
        public LuaMaze Maze { get; }

        public QuestEpisodeState Book => World.Get<QuestComponent>(Player).Book!.Find(Episode)!;

        public void Run(string code)
        {
            State.Environment["pMaze"] = new LuaValue(Maze);
            State.DoStringAsync(code).AsTask().GetAwaiter().GetResult();
        }

        /// <summary>
        /// The player has loaded the maze, the maze runs and the escorted appears.
        /// </summary>
        public void Start()
        {
            Maze.MarkLoaded(Player);
            Maze.TryStart();
            Maze.SpawnSector(Sector);
        }

        public void Pass(double seconds)
        {
            for (var elapsed = 0d; elapsed < seconds; elapsed += SimulationClock.Period.TotalSeconds)
            {
                _time.Advance(SimulationClock.Period);
                _clock.Tick();
            }
        }

        public double Number(string name) => State.Environment[name].TryRead<double>(out var value) ? value : double.NaN;

        public Entity? Escort => Creatures.Living.Where(e => World.IsAlive(e) && World.Get<CreatureComponent>(e).Identifier == Escorted).Cast<Entity?>().FirstOrDefault();

        /// <summary>
        /// The escort statuses the player was told so far, as point and status.
        /// </summary>
        public (int Point, EscortStatus Status)[] Statuses()
        {
            var sentinel = new ChatNoticeResponse { Message = "end" };

            World.Get<ServerSessionComponent>(Player).Send(sentinel);

            var deadline = DateTime.UtcNow.AddSeconds(2);

            _sentinels++;

            while (Connection.Frames.Select(PlayerFrame.Open).Count(e => e.Opcode.Value == sentinel.Opcode.Value) < _sentinels && DateTime.UtcNow < deadline)
            {
                Thread.Sleep(5);
            }

            var opcode = new MazeEscortStatusResponse().Opcode.Value;

            return Connection.Frames
                .Select(PlayerFrame.Open)
                .Where(e => e.Opcode.Value == opcode)
                .Select(e => (BitConverter.ToInt32(e.Body, 0), (EscortStatus)e.Body[4]))
                .ToArray();
        }

        public void Dispose()
        {
            Maze.Close();
            World.Destroy(World);
        }
    }

    /// <summary>
    /// The escorted starts its way when it appears, passes each point, and at the last one runs the function of the point and leaves
    /// the maze; the condition of the escort is done for the player a moment later, not at once.
    /// </summary>
    [TestMethod]
    public void TheEscortedWalksItsPointsAndItsArrivalDoesTheCondition()
    {
        using var test = new Test();

        test.Start();
        test.Pass(0.2);

        Assert.IsNotNull(test.Escort);
        Assert.IsEmpty(test.Creatures.FindMonsters((_, _) => true), "The players cannot hit the escorted");

        test.Pass(6);

        CollectionAssert.AreEqual(
            new[] { (401, EscortStatus.Started), (401, EscortStatus.Passed), (402, EscortStatus.Passed), (403, EscortStatus.Arrived) },
            test.Statuses());
        Assert.AreEqual(403d, test.Number("Arrived"));
        Assert.IsNull(test.Escort, "The escorted has left");
        Assert.IsFalse(test.Book.IsDone(0), "The condition waits");

        test.Pass(5);

        Assert.IsTrue(test.Book.IsDone(0));
        Assert.IsFalse(test.Book.Failed);
    }

    /// <summary>
    /// An escorted that falls ends its way where it is, and the quest of the party fails for the player who has its episode.
    /// </summary>
    [TestMethod]
    public void AFallenEscortFailsTheQuestOfTheParty()
    {
        using var test = new Test();

        test.Start();
        test.Pass(0.2);

        var escort = test.Escort!.Value;

        test.World.Set(escort, test.World.Get<CreatureComponent>(escort) with { Health = 0 });
        test.Pass(0.2);

        Assert.AreEqual((402, EscortStatus.Ended), test.Statuses()[^1]);
        Assert.IsNull(test.Escort);
        Assert.IsTrue(test.Book.Failed);
    }

    /// <summary>
    /// A player who loads the maze in the sector of the quest of the party while working on its first condition starts it: a box of
    /// the quest then tells the script the sector, once. Any other box runs its own function with its number.
    /// </summary>
    [TestMethod]
    public void TheQuestOfThePartyTellsTheScriptItsSectorOnce()
    {
        using var test = new Test();

        test.Maze.RunFunctionBoxAsync(test.Player, "OnPartyQuestEvent", 9101).AsTask().GetAwaiter().GetResult();

        Assert.AreEqual(0d, test.Number("Calls"), "The quest has not started");

        test.Start();
        test.Maze.RunFunctionBoxAsync(test.Player, "OnPartyQuestEvent", 9101).AsTask().GetAwaiter().GetResult();
        test.Maze.RunFunctionBoxAsync(test.Player, "OnPartyQuestEvent", 9102).AsTask().GetAwaiter().GetResult();
        test.Maze.RunFunctionBoxAsync(test.Player, "OnBox", 9103).AsTask().GetAwaiter().GetResult();

        Assert.AreEqual((double)Sector, test.Number("Told"));
        Assert.AreEqual(1d, test.Number("Calls"));
        Assert.AreEqual(9103d, test.Number("Boxes"));
    }
}
