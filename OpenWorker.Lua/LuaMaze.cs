using System.Diagnostics;
using Arch.Core;
using Lua;
using Microsoft.Extensions.Logging;
using OpenWorker.Batch;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Buffs;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Chat.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua.Buffs;
using OpenWorker.Lua.Managers;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Lua;

public enum MazeState
{
    None = 0x0,
    Run = 0x1,
    StartBoss = 0x2,
    WaitToDie = 0x3,
    ModeFail = 0x4,
    Wait = 0x5
}

[LuaObject]
public partial class LuaMaze(
    LuaState state, 
    World ecs, 
    Entity player, 
    LuaCreatureManager creatureManager,
    BuffManager buffManager,
    QuestManager questManager,
    VBatchFile batch,
    RowIndex<int, InteractionObjectRow> interactions,
    SkillActionProvider actions,
    CombatTables combat,
    MazeInfoRow info,
    MazeReporter reports,
    SimulationClock clock,
    ILogger<LuaMaze> logger
) : ILuaUserData, ISimulation
{
    /// <summary>
    /// Everything the maze waits for: delayed actions of its script, the next moment of a skill, the hits of a trap,
    /// the creatures that appear later. <see cref="Close"/> calls all of it off.
    /// </summary>
    private readonly ScheduleGroup _schedule = Share(creatureManager, clock.CreateGroup());

    private static ScheduleGroup Share(LuaCreatureManager creatures, ScheduleGroup schedule)
    {
        creatures.Schedule = schedule;

        return schedule;
    }

    /// <summary>
    /// The clock that steps the maze and runs what it has scheduled.
    /// </summary>
    internal SimulationClock Clock { get; } = clock;

    /// <summary>
    /// What the maze has waiting on the clock.
    /// </summary>
    public int Scheduled => _schedule.Count;

    public LuaState State { get; } = state;
    
    public World Ecs { get; } = ecs;
    public VBatchFile Batch { get; } = batch;

    /// <summary>
    /// Where the maze, its script events and its script calls write to.
    /// </summary>
    internal ILogger Logger { get; } = logger;

    public MazeProgress Progress { get; } = new(batch, interactions);

    /// <summary>
    /// The monsters of the maze and the players they fight.
    /// </summary>
    public LuaCreatureManager Creatures => creatureManager;

    /// <summary>
    /// The longest a script or a table can make the maze wait for something.
    /// </summary>
    private static readonly TimeSpan LongestWait = TimeSpan.FromDays(1);

    /// <summary>
    /// A player in the maze and what they have going: the skill being played, its trap, its projectiles and chains.
    /// </summary>
    private sealed class Fighter(Entity player)
    {
        public Entity Player { get; } = player;
        public int SkillGeneration { get; set; }
        public int CurrentSkill { get; set; }
        public TrapSite? MouseTrap { get; set; }
        public SkillRun? Playback { get; set; }
        public CarryRun? Carry { get; set; }
        public ScheduledAction SkillWake { get; set; }
        public Queue<bool> PressedKeys { get; } = [];
        public uint ProjectileSession { get; set; }
        public List<PlayerFlight> Flights { get; } = [];
        public List<ChainRun> Chains { get; } = [];

        /// <summary>
        /// When a claim of the client was last counted against its error budget, on the clock of the maze.
        /// </summary>
        public TimeSpan? RefusedAt { get; set; }

        /// <summary>
        /// The combo of the player and when its last attack hit, in milliseconds of the clock of the maze.
        /// </summary>
        public int Combo { get; set; }

        public long LastHit { get; set; } = long.MinValue;

        /// <summary>
        /// What the script hears of the quests of the player, when it listens.
        /// </summary>
        public QuestWatch? Quests { get; set; }

        /// <summary>
        /// The client of the player has loaded the maze and hears what happens in it.
        /// </summary>
        public bool Loaded { get; set; }

        /// <summary>
        /// The player has left the maze: what they set going stops.
        /// </summary>
        public bool Gone { get; set; }
    }

    /// <summary>
    /// The players of the maze in the order they came: the one who opened it, then those of the party who joined.
    /// </summary>
    private readonly List<Fighter> _members = First(creatureManager, player);

    /// <summary>
    /// The buffs of the players and the monsters of the maze.
    /// </summary>
    private readonly MazeBuffs _buffs = CreateBuffs(ecs, creatureManager, buffManager, combat, player);

    private static List<Fighter> First(LuaCreatureManager creatures, Entity player)
    {
        creatures.Attach(player);

        return [new Fighter(player)];
    }

    /// <summary>
    /// A monster of the maze was killed: the player who killed it, its id in the table and the place where it fell.
    /// </summary>
    public event Action<Entity, int, System.Numerics.Vector3>? MonsterKilled;

    /// <summary>
    /// The maze has been won: its last sector is cleared.
    /// </summary>
    public event Action? Completed;

    /// <summary>
    /// An attack of a player has hit monsters: the player, how many it hit and the damage it did in all.
    /// </summary>
    public event Action<Entity, int, long>? MonstersHit;

    private bool Started { get; set; }
    private long StartedAt { get; set; }
    private bool Closed { get; set; }

    /// <summary>
    /// The whole seconds since the first player started the maze, 0 before.
    /// </summary>
    public int PlayedSeconds => Started ? (int)Stopwatch.GetElapsedTime(StartedAt).TotalSeconds : 0;

    public MazeState MazeState { get; private set; } = MazeState.None;

    /// <summary>
    /// The player has left: timers must not touch an entity that is gone, or that belongs to someone else by now.
    /// </summary>
    public void Close()
    {
        Closed = true;

        // Nothing the maze has waiting runs any more, and nothing it asks for from now on is kept.
        _schedule.Close();
        Clock.Remove(this);

        Timers.Clear();

        foreach (var member in _members)
        {
            member.Gone = true;
            member.Flights.Clear();
            member.Chains.Clear();
            member.Playback = null;
            _buffs.Detach(member.Player);
        }

        creatureManager.Close();
    }

    /// <summary>
    /// The instance the maze is: the players of one party share it, a player alone has one of their own.
    /// </summary>
    public Guid Instance { get; init; }

    /// <summary>
    /// The party whose maze it is, or zero for a player alone.
    /// </summary>
    public int Party { get; init; }

    /// <summary>
    /// How many players are in the maze.
    /// </summary>
    public int MemberCount => _members.Count;

    /// <summary>
    /// The players in the maze, in the order they came.
    /// </summary>
    public IReadOnlyList<Entity> Members => creatureManager.Members;

    private Fighter? Find(Entity player)
    {
        foreach (var member in _members)
        {
            if (member.Player == player)
            {
                return member;
            }
        }

        return null;
    }

    /// <summary>
    /// A player of the party comes into the maze where it stands. False when the maze is over or the player is in already.
    /// </summary>
    [LuaIgnoreMember]
    public bool Join(Entity player)
    {
        if (Closed || Find(player) is not null)
        {
            return false;
        }

        _members.Add(new Fighter(player) { Quests = _questCatalog is { } catalog ? new QuestWatch(Ecs, player, catalog) : null });
        creatureManager.Attach(player);
        _buffs.Attach(player);

        return true;
    }

    /// <summary>
    /// A player leaves the maze: what they set going stops, the others see them go, and the monsters forget them. The maze
    /// goes on for the rest; the last one out ends it. True when the maze has ended.
    /// </summary>
    [LuaIgnoreMember]
    public bool Leave(Entity player)
    {
        if (Find(player) is not { } member)
        {
            return Closed;
        }

        // The helpers of the player go with it, and its client forgets those of the others.
        creatureManager.Helpers.OnLeaving(player);

        member.Gone = true;
        member.SkillWake.Cancel();
        member.Flights.Clear();
        member.Chains.Clear();
        member.Playback = null;

        // What the player carries is let go where it stands: nobody steps the carry of a player who is gone.
        EndCarry(member);

        _members.Remove(member);
        creatureManager.Detach(player);
        _buffs.Detach(player);

        // The others do not wait for a player who is gone.
        _portalWait?.Remove(player);

        if (_members.Count == 0)
        {
            Close();

            return true;
        }

        if (member.Loaded && Ecs.IsAlive(player) && Ecs.Has<ActorComponent>(player))
        {
            creatureManager.Broadcast(new WorldOutInfoPcResponse { Actors = [Ecs.Get<ActorComponent>(player)] });
        }

        return false;
    }

    /// <summary>
    /// The client of the player has loaded the maze. True only the first time, so the player is shown to the others once.
    /// </summary>
    [LuaIgnoreMember]
    public bool MarkLoaded(Entity player)
    {
        if (Find(player) is not { Loaded: false } member)
        {
            return false;
        }

        member.Loaded = true;

        return true;
    }

    /// <summary>
    /// One step of the maze on the clock: the timers of its script count, the projectiles and chains of the player
    /// fly, and the monsters think.
    /// </summary>
    [LuaIgnoreMember]
    public void Advance(TimeSpan elapsed)
    {
        if (Closed)
        {
            return;
        }

        StepTimers((float)elapsed.TotalSeconds);
        StepQuests();
        StepMode((float)elapsed.TotalSeconds);
        _portalWait?.Advance((float)elapsed.TotalSeconds);

        // A player who leaves during the step is taken out of the list, so the list is walked by index.
        for (var index = 0; index < _members.Count && !Closed; index++)
        {
            StepFlights(_members[index]);
            StepChains(_members[index]);
            StepCarry(_members[index]);
        }

        // The gauges of those who have loaded the maze fill with the time of the fight.
        if (creatureManager.Services?.Gauges is { } gauges)
        {
            for (var index = 0; index < _members.Count && !Closed; index++)
            {
                if (_members[index] is { Loaded: true, Gone: false } member)
                {
                    gauges.Advance(member.Player, (float)elapsed.TotalSeconds);
                }
            }
        }

        _buffs.Step((float)elapsed.TotalSeconds);

        creatureManager.Step(elapsed);
    }

    /// <summary>
    /// The name of the script of the maze, as the maze table gives it.
    /// </summary>
    internal string Script => info.Script ?? string.Empty;

    /// <summary>
    /// The player has left or the script has failed: nothing of the maze runs any more.
    /// </summary>
    public bool IsClosed => Closed;

    /// <summary>
    /// The maze of the table this instance plays.
    /// </summary>
    public short Maze => info.Id;

    /// <summary>
    /// The script called something the server does not do: the call does nothing, and the log says so once for the
    /// script and the function, however many mazes of the script call it how often.
    /// </summary>
    internal void NotDone(string function)
    {
        if (ScriptWarnings.First(Script, function))
        {
            Logger.NotDoneByServer(function, Script);
        }
    }

    /// <summary>
    /// What the player reads when the script of the maze has failed.
    /// </summary>
    private const string FailureNotice = "The maze has stopped: its script failed. Leave it through the menu.";

    /// <summary>
    /// The script failed inside a call of the server: the maze ends for its players. Nothing of it runs any more, its
    /// requests are refused as those of a player who is in no maze, the player is told, and the way out of the maze
    /// stays open.
    /// </summary>
    internal void Fail(string function, Exception exception)
    {
        if (Closed)
        {
            return;
        }

        Logger.ScriptFailed(exception, function, Script);

        var members = Present();

        Close();

        foreach (var player in members)
        {
            if (!Ecs.IsAlive(player))
            {
                continue;
            }

            if (Ecs.TryGet(player, out LuaMaze? current) && ReferenceEquals(current, this))
            {
                Ecs.Remove<LuaMaze>(player);
            }

            if (Ecs.TryGet(player, out ServerSessionComponent session))
            {
                session.Send(new ChatNoticeResponse { Type = 0, Message = FailureNotice, Color = "" });
            }
        }
    }

    /// <summary>
    /// How long to wait for a number of seconds a script or a table gives: nothing for what is not a number or not
    /// positive, a day at most.
    /// </summary>
    private static TimeSpan Wait(float seconds)
    {
        if (!float.IsFinite(seconds) || seconds <= 0f)
        {
            return TimeSpan.Zero;
        }

        return seconds >= LongestWait.TotalSeconds ? LongestWait : TimeSpan.FromSeconds(seconds);
    }

    /// <summary>
    /// Starts work that may wait for the database or a script, and logs it if it fails: nobody waits for it.
    /// </summary>
    private void Run(ValueTask work)
    {
        if (!work.IsCompletedSuccessfully)
        {
            _ = FinishAsync(work);
        }
    }

    private async Task FinishAsync(ValueTask work)
    {
        try
        {
            await work;
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            Logger.DelayedActionFailed(e);
        }
    }

    /// <summary>
    /// Runs <paramref name="action"/> once the game time has moved on by <paramref name="seconds"/>, at the next tick at
    /// the soonest, unless the maze is closed first.
    /// </summary>
    private void Later(float seconds, Func<ValueTask> action)
    {
        _schedule.Schedule(Wait(seconds), () =>
        {
            if (!Closed)
            {
                Run(action());
            }
        });
    }

    /// <summary>
    /// The players in the maze now, written out: what is done for each of them may wait, and one may leave meanwhile.
    /// </summary>
    private Entity[] Present()
    {
        var players = new Entity[_members.Count];

        for (var index = 0; index < players.Length; index++)
        {
            players[index] = _members[index].Player;
        }

        return players;
    }

    /// <summary>
    /// True only the first time: the script must see a player enter once.
    /// </summary>
    public bool TryStart()
    {
        if (Started)
        {
            return false;
        }

        Started = true;
        StartedAt = Stopwatch.GetTimestamp();
        _buffs.MonsterKilled += OnBuffKill;
        creatureManager.PlayerFrames = FrameDefenseOf;

        // The quests the player has when the maze starts are where the script begins to follow them.
        foreach (var member in _members)
        {
            member.Quests?.Look();
        }

        // From now on the maze steps with the clock: its timers, the projectiles of the player and the monsters.
        if (!Closed)
        {
            Clock.Add(this);
        }

        return true;
    }
}
