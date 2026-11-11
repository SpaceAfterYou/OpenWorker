using System.Collections.ObjectModel;
using System.Globalization;
using System.Numerics;
using System.Text;
using Arch.Core;
using Lua;
using Lua.Standard;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Modules.Chat.Responses;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Navigation;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The game modes of a maze: spawn boxes that send their monsters in waves, the survival of those waves, the defence of a
/// monster and the trial a boss sets. The maze is stepped by hand, a tenth of a second at a time. Creating entities registers
/// component types in a registry that does not take two threads at once.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MazeModeTest
{
    private const int Sector = 10;
    private const int Mazes = 21331;
    private const int Wolf = 1000;
    private const int Crab = 1001;
    private const int Bat = 1002;
    private const int Car = 2000;
    private const int Boss = 3000;
    private static readonly TimeSpan Step = TimeSpan.FromSeconds(0.1);

    private static string Vector(float x, float y, float z) => string.Create(CultureInfo.InvariantCulture, $"{x:0.000},{y:0.000},{z:0.000}");

    private static string Basic(int id, float x, float y, float z, float size = 100f) =>
        $"""<m_ID type="0" value="{id}" /> <m_iGenerateID type="0" value="{id}" /> <iLayerBitmask type="4" value="Common" /> <m_vPosTopLeft type="7" value="{Vector(x, y, z)}" /> <m_vPosBottomRight type="7" value="{Vector(x + size, y + size, z + size)}" /> <m_fRotation type="2" value="90.0000" /> <m_vSize type="7" value="{Vector(size, size, size)}" /> <m_bShowCustomEntity type="5" value="TRUE" /> <m_ColorLDR type="132" value="0,0,0,255" />""";

    /// <summary>
    /// A sector that is cleared by its script alone, so that only the modes clear it.
    /// </summary>
    private static string SectorBox(int id) =>
        $"""<VSectorBox boxtype="eEventBoxType_Sector"> {Basic(id, 0, 0, 0, 100000f)} <m_eType type="4" value="General" /> <m_szEnterLua type="12" value="" /> <m_eClearType type="4" value="None" /> <m_iClearKillRatio type="0" value="0" /> <m_szClearLua type="12" value="" /> <m_iSectorTitle type="0" value="0" /> <m_iSectorExitType type="4" value="Portal" /> <m_iSectorExitID type="0" value="0" /> <m_iRelativeSectorID type="0" value="0" /> <m_iConditionKillRatio_1Step type="0" value="0" /> <m_iConditionKillRatio_2Step type="0" value="0" /> <m_iConditionKillRatio_3Step type="0" value="0" /> <m_iConditionKillRatio_4Step type="0" value="0" /> <m_iConditionKillRatio_5Step type="0" value="0" /> </VSectorBox>""";

    private static string StartBox(int id, float x, float y) =>
        $"""<VPortalExitBox boxtype="eEventBoxType_PortalExit"> {Basic(id, x, y, 50f, 200f)} <m_iParentPortalBoxID type="0" value="0" /> </VPortalExitBox>""";

    /// <summary>
    /// A spawn box with up to ten monsters in its slots, which either appears at once or waits for a signal and sends waves.
    /// </summary>
    private static string SpawnBox(int id, int[] monsters, bool waves = false, float delay = 0f, float sequence = 0f, int count = 0, int entities = 1, string order = "All")
    {
        var text = new StringBuilder($"""<VMonsterSpawnBox boxtype="eEventBoxType_MonsterSpawn"> {Basic(id, 500, 500, 100)} """);

        for (var slot = 1; slot <= 10; slot++)
        {
            var monster = slot <= monsters.Length ? monsters[slot - 1] : 0;

            text.Append(CultureInfo.InvariantCulture, $"""<m_eType{slot} type="4" value="Monster" /> <m_iMonsterID{slot} type="0" value="{monster}" /> <m_iChance{slot} type="0" value="{(monster == 0 ? 0 : 10000)}" /> """);
        }

        text.Append(CultureInfo.InvariantCulture, $"""<m_eCreationPositionType type="4" value="Random" /> <m_eMoveType type="4" value="Free" /> <m_eCreationCondition type="4" value="{(waves ? "WaitSignal" : "Immediate")}" /> <m_CreationEffectFile type="12" value="" /> <m_fWaitCreationDelayTime type="2" value="{delay.ToString("0.0000", CultureInfo.InvariantCulture)}" /> <m_fWaitCreationSequenceTime type="2" value="{sequence.ToString("0.0000", CultureInfo.InvariantCulture)}" /> <m_iWaitCreationMaxWave type="0" value="{count}" /> <m_iMaxEntityCount type="0" value="{entities}" /> <m_eWaitCreationSequenceType type="4" value="{order}" /> <m_iWaypoint type="0" value="0" /> <m_iAggroGroupID type="0" value="0" /> <m_iAggroDistance type="0" value="0" /> <m_iAggroMaxCount type="0" value="0" /> <m_fLookRatio type="2" value="3.0000" /> <m_bShowSight type="5" value="FALSE" /> <m_szObjectKey type="12" value="" /> <m_eScriptType type="4" value="None" /> <m_iCheckScriptHP1 type="0" value="0" /> <m_iCheckScriptHP2 type="0" value="0" /> <m_iCheckScriptHP3 type="0" value="0" /> <m_iCheckScriptHP4 type="0" value="0" /> <m_iCheckScriptHP5 type="0" value="0" /> <m_iSectorID type="0" value="{Sector}" /> <m_ChangeSpawnAction type="12" value="" /> <m_ProtectionTarget type="0" value="0" /> <m_RespawnTime type="2" value="1.0000" /> <m_iStep type="0" value="1" /> <m_eRespawnType type="4" value="None" /> <m_iRespawnCondition type="0" value="0" /> <m_iGroupID type="0" value="0" /> </VMonsterSpawnBox>""");

        return text.ToString();
    }

    private static string Map(params string[] boxes) =>
        "<root><Batchs eventtype=\"EventBox\">" + string.Concat(boxes) + "</Batchs><Batchs eventtype=\"EventPoint\"/></root>";

    private static ReadOnlyCollection<T> Rows<T>(params Action<BinaryWriter>[] rows)
    {
        return new ReadOnlyCollection<T>(rows.Select(write =>
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            write(writer);
            writer.Flush();
            stream.Position = 0;

            return (T)Activator.CreateInstance(typeof(T), new BinaryReader(stream))!;
        }).ToArray());
    }

    private static void Text(BinaryWriter writer, string value)
    {
        writer.Write((ushort)value.Length);
        writer.Write(Encoding.Unicode.GetBytes(value));
    }

    private static void Boxes(BinaryWriter writer, params int[] boxes)
    {
        for (var slot = 0; slot < 10; slot++)
        {
            writer.Write((ushort)(slot < boxes.Length ? boxes[slot] : 0));
        }
    }

    private static Action<BinaryWriter> SurvivalRow(byte id, int sector, params int[] boxes) => writer =>
    {
        writer.Write(id);
        writer.Write(0u);
        writer.Write((uint)Mazes);
        writer.Write((uint)sector);
        Text(writer, "Scripts\\Scene\\Maze");
        Boxes(writer, boxes);
    };

    private static Action<BinaryWriter> DefenceRow(byte id, int defended, int start, params int[] boxes) => writer =>
    {
        writer.Write(id);
        writer.Write(0u);
        writer.Write((uint)Mazes);
        writer.Write(0u);
        writer.Write((uint)defended);
        writer.Write(1000u);
        writer.Write(2000u);
        writer.Write((ushort)4000);
        writer.Write((uint)start);
        Text(writer, "Scripts\\Maze");
        Boxes(writer, boxes);
    };

    private static readonly int[] Shares = [80, 70, 60, 50, 40];

    private static Action<BinaryWriter> OperationRow(byte id, int boss, params int[] objects) => writer =>
    {
        writer.Write(id);
        writer.Write((uint)boss);

        foreach (var share in Shares)
        {
            writer.Write((byte)share);
        }

        writer.Write((ushort)Mazes);
        writer.Write((ushort)Sector);
        writer.Write(10u);
        writer.Write(40122u);

        foreach (var second in new ushort[] { 2, 5, 12 })
        {
            writer.Write(second);
        }

        foreach (var text in new uint[] { 40123, 40124, 40125 })
        {
            writer.Write(text);
        }

        writer.Write((uint)boss);
        writer.Write(30032u);
        writer.Write(30033u);

        for (var slot = 0; slot < 5; slot++)
        {
            writer.Write((uint)(slot < objects.Length ? objects[slot] : 0));
        }

        writer.Write(40126u);
        writer.Write(40127u);
    };

    private static GameModes Modes(ReadOnlyCollection<SurvivalModeRow>? survival = null, ReadOnlyCollection<DefenceModeRow>? defence = null, ReadOnlyCollection<OperationModeRow>? operation = null) =>
        new(survival ?? TestMazes.None<SurvivalModeRow>(), defence ?? TestMazes.None<DefenceModeRow>(), operation ?? TestMazes.None<OperationModeRow>());

    private static Entity NewPlayer(World world, int person, RecordingConnection connection, int health = 100)
    {
        var life = new AbilityComponentEntry { Current = health, Max = 100 };

        return world.Create(
            new ActorComponent(person, ActorType.User),
            new ServerSessionComponent(connection),
            new WorldComponent { Location = 0, Position = Vector3.Zero, Rotation = 0f, Jump = 0 },
            new LevelComponent(20, 0),
            new PersonInfoComponent { Name = "Member" + person, Hero = Hero.Haru },
            new AbilityComponent { Health = life, SoulGain = life, SoulVapor = life, Stamina = life, SuperArmor = life, Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f } });
    }

    /// <summary>
    /// What a mode tells the rest of the game about a mode that was won.
    /// </summary>
    private sealed class Reporter(Func<Entity, bool> shared) : MazeReporter(null!, null!, null!, null!, null!, NullLogger<MazeReporter>.Instance)
    {
        public List<(Entity Player, QuestConditionType Type, int Maze)> Won { get; } = [];

        public override ValueTask<bool> ModeWonAsync(Entity player, QuestConditionType type, int maze)
        {
            Won.Add((player, type, maze));

            return ValueTask.FromResult(shared(player));
        }
    }

    private sealed class Test : IDisposable
    {
        public Test(string map, GameModes modes, string script = "", params int[] standing)
        {
            World = World.Create();
            Connection = new RecordingConnection();
            Player = NewPlayer(World, 1, Connection);
            Batch = TestMazes.Map(map);

            var tables = new CombatTables(TestMazes.None<MonsterRow>(), TestMazes.None<StatusRow>(), TestMazes.None<SkillRow>(), TestMazes.None<CharacterInfoRow>());
            var services = new CreatureServices(null!, new Gameplay.Ai.MonsterBehaviorTables(TestMazes.None<MonsterRow>(), TestMazes.None<SkillRow>()), null!, TestMazes.Clock(), new NavMeshProvider(), modes: modes);

            Creatures = new LuaCreatureManager(World, Batch, tables, services);
            Reports = new Reporter(_ => false);
            State = LuaState.Create();
            State.OpenStandardLibraries();
            Maze = TestMazes.Create(World, Player, state: State, batch: Batch, creatures: Creatures, reports: Reports);

            State.Environment["pMaze"] = new LuaValue(Maze);

            if (script.Length > 0)
            {
                State.DoStringAsync(script).AsTask().GetAwaiter().GetResult();
            }
        }

        private int _sentinels;

        public World World { get; }
        public RecordingConnection Connection { get; }
        public Entity Player { get; }
        public global::OpenWorker.Batch.VBatchFile Batch { get; }
        public LuaCreatureManager Creatures { get; }
        public Reporter Reports { get; set; }
        public LuaState State { get; }
        public LuaMaze Maze { get; }

        public void Run(string code) => State.DoStringAsync(code).AsTask().GetAwaiter().GetResult();

        /// <summary>
        /// Lets the time of the maze pass, a tenth of a second at a time.
        /// </summary>
        public void Pass(double seconds)
        {
            for (var elapsed = 0d; elapsed < seconds - 0.0001; elapsed += Step.TotalSeconds)
            {
                Maze.Advance(Step);
            }
        }

        public Entity[] Monsters() => Creatures.FindMonsters((_, _) => true);

        public double Number(string name) => State.Environment[name].TryRead<double>(out var value) ? value : double.NaN;

        /// <summary>
        /// Everything the player was sent so far. Sending is queued, so a line is sent behind what is already there and waited for.
        /// </summary>
        public PlayerFrame[] Told()
        {
            var sentinel = new ChatNoticeResponse { Message = "end" };

            World.Get<ServerSessionComponent>(Player).Send(sentinel);

            var deadline = DateTime.UtcNow.AddSeconds(2);

            _sentinels++;

            while (Connection.Frames.Select(PlayerFrame.Open).Count(e => e.Opcode.Value == sentinel.Opcode.Value) < _sentinels && DateTime.UtcNow < deadline)
            {
                Thread.Sleep(5);
            }

            return Connection.Frames.Select(PlayerFrame.Open).Where(e => e.Opcode.Value != sentinel.Opcode.Value).ToArray();
        }

        public PlayerFrame[] Told<T>(T message) where T : struct, IHotspotMessage
        {
            return Told().Where(e => e.Opcode.Value == message.Opcode.Value).ToArray();
        }

        public void Dispose()
        {
            Maze.Close();
            World.Destroy(World);
        }
    }

    // ---- Waves ----------------------------------------------------------------------------------

    /// <summary>
    /// A box that waits for a signal sends its first wave after its delay and the others one sequence time apart, as many as it
    /// says, and counts as appeared only with the last: a sector that waits for the box is not cleared between two waves.
    /// </summary>
    [TestMethod]
    public void AWaveComesAfterTheDelayAndTheOthersOneSequenceApart()
    {
        using var test = new Test(Map(SectorBox(Sector), SpawnBox(101, [Wolf], waves: true, delay: 1f, sequence: 2f, count: 3, entities: 2)), Modes());
        var waves = new SpawnWaves(test.Creatures, test.Batch);

        Assert.IsTrue(waves.Start(101, false));
        Assert.IsFalse(waves.Start(101, false), "A box is started once by a mode");

        waves.Step(0.9f);
        Assert.IsEmpty(test.Monsters());

        waves.Step(0.2f);
        Assert.HasCount(2, test.Monsters(), "Two monsters in the first wave");

        waves.Step(2f);
        Assert.HasCount(4, test.Monsters());
        Assert.IsFalse(test.Creatures.HasAppeared(101), "Not yet: a wave is still to come");

        waves.Step(2f);
        Assert.HasCount(6, test.Monsters());
        Assert.IsTrue(test.Creatures.HasAppeared(101));
        Assert.IsFalse(waves.IsRunning);
    }

    /// <summary>
    /// A box that sends one slot at a time takes the slots in turn, one in each wave.
    /// </summary>
    [TestMethod]
    public void ABoxThatSendsOneSlotAtATimeTakesThemInTurn()
    {
        using var test = new Test(Map(SectorBox(Sector), SpawnBox(101, [Wolf, Crab, Bat], waves: true, delay: 1f, sequence: 1f, count: 3, order: "OnebyOne")), Modes());
        var waves = new SpawnWaves(test.Creatures, test.Batch);
        var seen = new List<int>();

        waves.Start(101, false);

        for (var wave = 0; wave < 3; wave++)
        {
            waves.Step(1.1f);

            seen.Add(test.World.Get<CreatureComponent>(test.Monsters().Last()).Identifier);
        }

        CollectionAssert.AreEqual(new[] { Wolf, Crab, Bat }, seen);
        Assert.HasCount(3, test.Monsters());
    }

    // ---- Survival -------------------------------------------------------------------------------

    private const string SurvivalScript = "Reached = 0 function OnSurvivalModeSuccess(sector, maze) Reached = sector end";

    /// <summary>
    /// The survival is won only when the last wave has come and no monster is left in the sector: the sector is cleared, the
    /// script hears of it, the players are told, and the quests of the kind are counted for the players on their feet.
    /// </summary>
    [TestMethod]
    public void SurvivalIsWonWhenTheLastWaveIsDead()
    {
        using var test = new Test(
            Map(SectorBox(Sector), SpawnBox(101, [Wolf], waves: true, delay: 1f, sequence: 2f, count: 2)),
            Modes(Rows<SurvivalModeRow>(SurvivalRow(10, Sector, 101))),
            SurvivalScript);

        test.Run("pMaze:StartSurvivalMode(10)");

        Assert.IsTrue(test.Maze.HasMode);
        Assert.HasCount(1, test.Told(new MazeNoticeResponse()), "The start is told");

        test.Pass(1.5);

        foreach (var monster in test.Monsters())
        {
            test.Creatures.Remove(monster);
        }

        // The first wave is dead and the second has not come: the mode goes on.
        test.Pass(1.2);
        Assert.IsTrue(test.Maze.HasMode);
        Assert.AreEqual(0d, test.Number("Reached"));

        test.Pass(3);
        Assert.HasCount(1, test.Monsters(), "The second wave");
        Assert.IsTrue(test.Maze.HasMode, "The last wave is alive");

        test.Creatures.Remove(test.Monsters().Single());
        test.Pass(1.2);

        Assert.IsFalse(test.Maze.HasMode);
        Assert.AreEqual((double)Sector, test.Number("Reached"));
        Assert.IsTrue(test.Maze.Progress.IsSectorCleared(Sector));
        Assert.HasCount(2, test.Told(new MazeNoticeResponse()), "The end is told too");
        CollectionAssert.AreEqual(new[] { (test.Player, QuestConditionType.Survival, (int)test.Maze.Maze) }, test.Reports.Won.ToArray());
    }

    /// <summary>
    /// A script that names a mode the server has no row for starts nothing and tells nobody.
    /// </summary>
    [TestMethod]
    public void AModeTheTableLacksStartsNothing()
    {
        using var test = new Test(Map(SectorBox(Sector)), Modes(Rows<SurvivalModeRow>(SurvivalRow(10, Sector))));

        test.Run("pMaze:StartSurvivalMode(11) pMaze:StartDefenceMode(10) pMaze:StartOperationMode(300)");

        Assert.IsFalse(test.Maze.HasMode);
        Assert.IsEmpty(test.Told(new MazeNoticeResponse()));
    }

    // ---- Defence --------------------------------------------------------------------------------

    private const string DefenceScript = "Won = 0 Lost = 0 function OnDefenceModeSuccess(sector, maze) Won = 1 end function OnDefenceModeFail(sector, maze) Lost = 1 end";

    private static Test Defended(params string[] extra) => new(
        Map([SectorBox(Sector), StartBox(991, 20000f, 20000f), SpawnBox(50, [Car]), SpawnBox(102, [Wolf], waves: true, delay: 1f, sequence: 1f, count: 1), .. extra]),
        Modes(defence: Rows<DefenceModeRow>(DefenceRow(10, Car, 991, 102))),
        DefenceScript);

    /// <summary>
    /// The defence puts the players in the box of the table, tells them it has begun, and makes the monster they must keep alive
    /// an enemy of the monsters that the players cannot hit.
    /// </summary>
    [TestMethod]
    public void TheDefenceGathersThePlayersAndGivesTheMonstersSomethingToHit()
    {
        using var test = Defended();

        test.Maze.SpawnSector(Sector);
        test.Maze.MarkLoaded(test.Player);

        var car = test.Monsters().Single();
        var actor = (int)test.World.Get<ActorComponent>(car);

        Assert.IsNull(test.Creatures.Helpers.FindTarget(actor), "Nobody defends it yet");

        test.Run("pMaze:StartDefenceMode(10)");

        var place = test.World.Get<WorldComponent>(test.Player).Position;

        Assert.IsTrue(place.X is >= 20000f and <= 20200f && place.Y is >= 20000f and <= 20200f, "In the box");
        Assert.HasCount(1, test.Told(new WorldWarpResponse()), "The player is told where it stands");
        Assert.HasCount(1, test.Told(new MazeNoticeResponse()));
        Assert.IsNotNull(test.Creatures.Helpers.FindTarget(actor), "The monsters see it as an enemy");
        Assert.IsEmpty(test.Monsters(), "The players cannot hit it");
    }

    /// <summary>
    /// The defence is lost when the monster falls: the script hears of it, the clients are told it failed, and no quest counts.
    /// </summary>
    [TestMethod]
    public void TheDefenceIsLostWhenTheMonsterFalls()
    {
        using var test = Defended();

        test.Maze.SpawnSector(Sector);
        test.Run("pMaze:StartDefenceMode(10)");

        var car = test.Creatures.Living.First(e => test.World.Get<CreatureComponent>(e).Identifier == Car);

        test.World.Set(car, test.World.Get<CreatureComponent>(car) with { Health = 0 });
        test.Pass(1.2);

        Assert.IsFalse(test.Maze.HasMode);
        Assert.AreEqual(1d, test.Number("Lost"));
        Assert.AreEqual(0d, test.Number("Won"));
        Assert.IsEmpty(test.Reports.Won);

        var notices = test.Told(new MazeNoticeResponse());

        Assert.HasCount(2, notices);
        Assert.AreEqual(2, BitConverter.ToInt32(notices[1].Body.AsSpan()[4..]), "The notice of the end names the fall (2)");
    }

    /// <summary>
    /// The defence is won when the last wave has come and is dead and a player stands: the players on their feet have the quests
    /// of the kind counted.
    /// </summary>
    [TestMethod]
    public void TheDefenceIsWonWhenTheWavesAreDeadAndAPlayerStands()
    {
        using var test = Defended();

        test.Maze.SpawnSector(Sector);
        test.Run("pMaze:StartDefenceMode(10)");
        test.Pass(1.5);

        // The monsters of the tables hold their sector; the ones of a test without tables do not.
        var wolf = test.Monsters().Single();

        test.World.Set(wolf, test.World.Get<MonsterCombatComponent>(wolf) with { HoldsSector = true });
        test.Pass(1);

        Assert.IsTrue(test.Maze.HasMode, "A wolf is alive");

        test.Creatures.Remove(test.Monsters().Single());
        test.Pass(1.2);

        Assert.IsFalse(test.Maze.HasMode);
        Assert.AreEqual(1d, test.Number("Won"));
        CollectionAssert.AreEqual(new[] { (test.Player, QuestConditionType.Protect, (int)test.Maze.Maze) }, test.Reports.Won.ToArray());
    }

    /// <summary>
    /// The defence is lost when no player stands, whatever has become of the waves.
    /// </summary>
    [TestMethod]
    public void TheDefenceIsLostWhenNoPlayerStands()
    {
        using var test = Defended();

        test.Maze.SpawnSector(Sector);
        test.Run("pMaze:StartDefenceMode(10)");
        test.Creatures.Vitals.Hurt(test.Player, int.MaxValue);
        test.Pass(1.2);

        Assert.AreEqual(1d, test.Number("Lost"));
        Assert.IsEmpty(test.Reports.Won);
    }

    // ---- Operation ------------------------------------------------------------------------------

    private const int FirstObject = 20195;
    private const int SecondObject = 20196;

    private static Test Trial() => new(
        Map(SectorBox(Sector), SpawnBox(60, [Boss])),
        Modes(operation: Rows<OperationModeRow>(OperationRow(11, Boss, FirstObject, SecondObject))));

    private static Entity StartTrial(Test test, int shareOfLife)
    {
        test.Maze.SpawnSector(Sector);
        test.Run("pMaze:StartOperationMode(11)");

        var boss = test.Creatures.Living.Single();

        test.World.Set(boss, test.World.Get<MonsterCombatComponent>(boss) with { Stats = test.World.Get<MonsterCombatComponent>(boss).Stats with { MaxHealth = 100f } });
        test.World.Set(boss, test.World.Get<CreatureComponent>(boss) with { Health = shareOfLife });

        return boss;
    }

    /// <summary>
    /// A boss that has lost enough of its life asks the players for an object, once for each share it passes, and tells them
    /// how long they have. A boss with its life whole asks nothing.
    /// </summary>
    [TestMethod]
    public void ABossAsksForAnObjectWhenItsLifeFallsUnderAShare()
    {
        using var test = Trial();

        StartTrial(test, 100);
        test.Pass(0.5);

        Assert.IsEmpty(test.Told(new MazeNoticeResponse()));

        var boss = test.Creatures.Living.Single();

        test.World.Set(boss, test.World.Get<CreatureComponent>(boss) with { Health = 75 });
        test.Pass(0.3);

        var notices = test.Told(new MazeNoticeResponse());

        Assert.HasCount(1, notices);
        Assert.AreEqual(61, BitConverter.ToInt32(notices[0].Body.AsSpan()[0..]), "The line of the trial");
        Assert.AreEqual(40122, BitConverter.ToInt32(notices[0].Body.AsSpan()[4..]), "Its text");
        Assert.AreEqual(10f, BitConverter.ToSingle(notices[0].Body.AsSpan()[8..]), "The time the players have");

        // The same share is not asked again while the life stays where it is.
        test.Pass(0.5);
        Assert.HasCount(1, test.Told(new MazeNoticeResponse()));
    }

    /// <summary>
    /// The player who clicks the object that is asked for gets the aura, and the trial is over; the other object does nothing.
    /// </summary>
    [TestMethod]
    public void TheObjectThatIsAskedForGivesTheAuraToWhoClicksIt()
    {
        using var test = Trial();

        StartTrial(test, 75);
        test.Pass(0.3);

        Assert.IsEmpty(test.Told(new SkillAuraUpdateBroadcastResponse()));

        // Which of the two objects is asked for is not told; one of them is.
        test.Maze.ModeInteracted(test.Player, FirstObject);
        test.Maze.ModeInteracted(test.Player, SecondObject);

        Assert.HasCount(1, test.Told(new SkillAuraUpdateBroadcastResponse()), "The aura goes once, for the right object");

        var notices = test.Told(new MazeNoticeResponse());

        Assert.AreEqual(40126, BitConverter.ToInt32(notices[^1].Body.AsSpan()[4..]), "The line of the success");

        // The line goes after five seconds.
        test.Pass(5.2);

        Assert.AreEqual(-1f, BitConverter.ToSingle(test.Told(new MazeNoticeResponse())[^1].Body.AsSpan()[8..]));
    }

    /// <summary>
    /// A trial that nobody answers in time gives the boss the aura, and tells the players that they failed.
    /// </summary>
    [TestMethod]
    public void ATrialNobodyAnswersInTimeGivesTheBossItsAura()
    {
        using var test = Trial();

        StartTrial(test, 75);
        test.Pass(10.5);

        Assert.HasCount(1, test.Told(new SkillAuraUpdateBroadcastResponse()), "The aura of the boss");

        var notices = test.Told(new MazeNoticeResponse());

        Assert.AreEqual(40127, BitConverter.ToInt32(notices[^1].Body.AsSpan()[4..]), "The line of the failure");
    }

    /// <summary>
    /// The mode ends when the boss is gone, and takes its lines from the players' screens.
    /// </summary>
    [TestMethod]
    public void TheTrialEndsWithTheBoss()
    {
        using var test = Trial();

        var boss = StartTrial(test, 100);

        test.Creatures.Remove(boss);
        test.Pass(1.2);

        Assert.IsFalse(test.Maze.HasMode);
        Assert.AreEqual(-1f, BitConverter.ToSingle(test.Told(new MazeNoticeResponse())[^1].Body.AsSpan()[8..]), "The line is taken away");
    }
}
