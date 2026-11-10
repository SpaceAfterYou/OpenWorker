using System.Diagnostics;
using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Lua;
using Microsoft.Extensions.Logging;
using OpenWorker.Batch;
using OpenWorker.Batch.Actions;
using OpenWorker.Batch.Entities;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Gameplay.Buffs;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Hotspot;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Domain.Batch.Enums;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Hotspot.Modules.Maze.Types;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Hotspot.Modules.Skill.Requests;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Hotspot.Modules.Chat.Responses;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Lua.Buffs;
using OpenWorker.Lua.Enums;
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
    /// Where a trap stands. It is a class because the player can move it while it hits.
    /// </summary>
    private sealed class TrapSite(Vector3 position)
    {
        public Vector3 Position { get; set; } = position;
    }

    /// <summary>
    /// A timer of the script that fails must not stop the others, so errors end here.
    /// </summary>
    private sealed class TimerHost(LuaMaze maze) : IMazeTimerHost
    {
        public async ValueTask CallAsync(string name, int first, int second, int third)
        {
            if (maze.Closed)
            {
                return;
            }

            try
            {
                await maze.State.OnTimerExComplete(name, first, second, third, maze);
            }

            catch (Exception e) when (e is not OperationCanceledException)
            {
                maze.Logger.TimerFunctionFailed(e, name);
            }
        }

        public async ValueTask CompleteAsync(int timer)
        {
            if (maze.Closed)
            {
                return;
            }

            try
            {
                await maze.State.OnTimerComplete(timer, maze);
            }

            catch (Exception e) when (e is not OperationCanceledException)
            {
                maze.Logger.TimerFailed(e, timer);
            }
        }

        public void ShowTimer(int value, float time, float following)
        {
            maze.ShowTimer(value, time, following);
        }
    }

    // The boxes of the cubes that are taken by holding the click have this in the name of their object.
    private const string CubeKey = "VaccumCube";

    // A monster that counts hits takes them no faster than this.
    private const long CountedHitMilliseconds = 500;

    /// <summary>
    /// The longest a script or a table can make the maze wait for something.
    /// </summary>
    private static readonly TimeSpan LongestWait = TimeSpan.FromDays(1);

    /// <summary>
    /// A skill that is being played: the moments of its attacks are counted on the game time from its start.
    /// </summary>
    private sealed class SkillRun(int skill, SkillPlayback playback, SkillPositionValue position, int generation, TimeSpan began)
    {
        public int Skill { get; } = skill;
        public SkillPlayback Playback { get; } = playback;
        public SkillPositionValue Position { get; } = position;
        public int Generation { get; } = generation;
        public TimeSpan Began { get; } = began;

        /// <summary>
        /// The latest moment the playback was told; the clock can stand a moment behind a reading taken just before a tick.
        /// </summary>
        public float Last { get; set; }
    }

    /// <summary>
    /// A projectile of the player on its way, stepped with the maze.
    /// </summary>
    private sealed class PlayerFlight(int skill, AttackTrigger trigger, ProjectileFlight flight, SkillPositionValue position, TimeSpan launched)
    {
        public int Skill { get; } = skill;
        public AttackTrigger Trigger { get; } = trigger;
        public ProjectileFlight Flight { get; } = flight;
        public SkillPositionValue Position { get; } = position;
        public TimeSpan Last { get; set; } = launched;

        /// <summary>
        /// Its step failed, and it flies no further.
        /// </summary>
        public bool Failed { get; set; }
    }

    /// <summary>
    /// A chain between monsters, stepped with the maze while its skill lasts.
    /// </summary>
    private sealed class ChainRun(int skill, AttackTrigger trigger, ChainLightning chain, uint session, SkillPositionValue position, int generation, TimeSpan fired)
    {
        public int Skill { get; } = skill;
        public AttackTrigger Trigger { get; } = trigger;
        public ChainLightning Chain { get; } = chain;
        public uint Session { get; } = session;
        public SkillPositionValue Position { get; } = position;
        public int Generation { get; } = generation;
        public TimeSpan Last { get; set; } = fired;
        public List<ChainHit> Hits { get; } = [];
        public List<ChainChange> Changes { get; } = [];

        /// <summary>
        /// Its step failed, and it holds nobody any more.
        /// </summary>
        public bool Failed { get; set; }
    }

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
        public ScheduledAction SkillWake { get; set; }
        public Queue<bool> PressedKeys { get; } = [];
        public uint ProjectileSession { get; set; }
        public List<PlayerFlight> Flights { get; } = [];
        public List<ChainRun> Chains { get; } = [];

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
    /// How long a townsman takes to turn, after which a script that waits for it hears of it.
    /// </summary>
    private const float NpcMoveSeconds = 1f;

    /// <summary>
    /// A group of mazes has four episodes.
    /// </summary>
    private const int MaxEpisode = 4;

    private const byte CutsceneStopped = 0;
    private const byte CutscenePlaying = 2;

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
    private MazeTimers? _timers;

    /// <summary>
    /// A round of the timers waits for a script that has not finished; the steps until then are added to the next round.
    /// </summary>
    private bool _timersBusy;
    private float _timersOwed;

    /// <summary>
    /// What the script has stored with <c>SetLuaValue</c>: numbers that stay for as long as the maze does.
    /// </summary>
    private Dictionary<int, int> LuaValues { get; } = [];

    private List<string> LuaKeys { get; } = [];
    private Dictionary<int, bool> SwitchNpcs { get; } = [];

    /// <summary>
    /// The latest walk of each townsman, by its table id: a townsman sent somewhere else on the way arrives only there.
    /// </summary>
    private Dictionary<int, int> NpcWalks { get; } = [];

    /// <summary>
    /// What the quests of the player do, for a script that follows them; none for a script that does not.
    /// </summary>

    /// <summary>
    /// The cutscene the player is watching, if any.
    /// </summary>
    private string? Cutscene { get; set; }

    /// <summary>
    /// The maze is won: its last sector has been cleared. Doors that open from now on say so to the client.
    /// </summary>
    public bool IsComplete { get; private set; }

    public MazeState MazeState { get; private set; } = MazeState.None;

    private MazeTimers Timers => _timers ??= new MazeTimers(new TimerHost(this));

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
        _portalWait?.Advance((float)elapsed.TotalSeconds);

        // A player who leaves during the step is taken out of the list, so the list is walked by index.
        for (var index = 0; index < _members.Count && !Closed; index++)
        {
            StepFlights(_members[index]);
            StepChains(_members[index]);
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

    private void StepTimers(float seconds)
    {
        if (_timers is null || _timers.IsEmpty)
        {
            return;
        }

        if (_timersBusy)
        {
            _timersOwed += seconds;

            return;
        }

        seconds += _timersOwed;
        _timersOwed = 0f;

        var round = _timers.UpdateAsync(seconds);

        if (!round.IsCompletedSuccessfully)
        {
            _timersBusy = true;

            _ = FinishTimersAsync(round);
        }
    }

    private async Task FinishTimersAsync(ValueTask round)
    {
        try
        {
            await round;
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            Logger.TimersFailed(e);
        }

        finally
        {
            _timersBusy = false;
        }
    }

    /// <summary>
    /// The script hears what happens to the quests of the player (<c>OnUpdateQuest</c>): the episodes taken and finished
    /// and the conditions done, from whichever part of the game they come. A script without the function is not told.
    /// </summary>
    [LuaIgnoreMember]
    public void FollowQuests(QuestCatalog catalog)
    {
        if (!State.Environment["OnUpdateQuest"].TryRead<LuaFunction>(out _))
        {
            return;
        }

        _questCatalog = catalog;

        foreach (var member in _members)
        {
            member.Quests ??= new QuestWatch(Ecs, member.Player, catalog);
        }
    }

    /// <summary>
    /// The quests of every member are followed, and each change names the member it is of.
    /// </summary>
    private QuestCatalog? _questCatalog;

    private void StepQuests()
    {
        if (_questCatalog is null || !Started)
        {
            return;
        }

        foreach (var member in _members)
        {
            if (member.Quests is not { } quests || !Ecs.IsAlive(member.Player))
            {
                continue;
            }

            var changes = quests.Look();

            if (changes.Count > 0)
            {
                Run(TellQuestsAsync((int)Ecs.Get<ActorComponent>(member.Player).Identifier, changes.ToArray()));
            }
        }
    }

    private async ValueTask TellQuestsAsync(int user, (QuestUpdateType Type, int Identifier)[] changes)
    {
        foreach (var (type, identifier) in changes)
        {
            if (Closed)
            {
                return;
            }

            await State.OnUpdateQuest(user, type, identifier, this);
        }
    }

    /// <summary>
    /// Spawns what a sector starts with. Boxes that wait for a signal are started by the script.
    /// </summary>
    [LuaIgnoreMember]
    public void SpawnSector(int sector)
    {
        var kills = creatureManager.GetKillProgress(sector);

        kills.MarkCurrentStepSpawned();

        foreach (var box in Batch.EventBox.SpawnsOf(sector).Where(e => e.CreationCondition == CreationConditionType.Immediate && e.Step == kills.Step + 1))
        {
            creatureManager.SpawnOnce(box.Id);
        }
    }

    /// <summary>
    /// The client only says that a skill has started, so the server decides what it hits and when.
    /// A new skill interrupts the previous one, along with the attacks it has not reached yet.
    /// </summary>
    [LuaIgnoreMember]
    public void StartSkill(Entity player, int skill, SkillPositionValue position)
    {
        if (!Started || Find(player) is not { } member)
        {
            return;
        }

        // The script hears of every skill the player uses.
        Run(State.OnActionSkill(skill, this));

        _buffs.OnCondition(player, MazeBuffs.OnSkill);

        var generation = ++member.SkillGeneration;
        var hero = Ecs.Get<PersonInfoComponent>(player).Hero;

        member.CurrentSkill = skill;

        // A trap follows the cursor only while its skill is being played.
        member.MouseTrap = null;
        member.Playback = null;
        member.SkillWake.Cancel();

        if (actions.GetAnimations(skill, hero) is not { } animations)
        {
            return;
        }

        var run = new SkillRun(skill, new SkillPlayback(animations), position, generation, Clock.Time);

        member.Playback = run;
        member.PressedKeys.Clear();

        Play(member, run);
    }

    /// <summary>
    /// The player holds the attack key or lets it go. What it means depends on the skill that is being played.
    /// </summary>
    [LuaIgnoreMember]
    public void PressKey(Entity player, bool pressed)
    {
        if (Find(player) is { Playback: { Playback.IsDone: false } run } member)
        {
            member.PressedKeys.Enqueue(pressed);
            member.SkillWake.Cancel();

            // The key cuts the step short now, not at the next tick.
            Play(member, run);
        }
    }

    /// <summary>
    /// Follows a skill through its steps: each attack is judged at its moment, and the key can cut a step short. It
    /// runs when the skill starts, when the key changes and at the tick of the next moment of the skill, so an attack
    /// lands at the first tick at or after its moment.
    /// </summary>
    private void Play(Fighter member, SkillRun run)
    {
        try
        {
            var now = Math.Max(run.Last, (float)(Clock.Time - run.Began).TotalSeconds);

            run.Last = now;

            while (!Closed && !member.Gone && run.Generation == member.SkillGeneration && run.Playback.NextTime is { } next)
            {
                if (next > now && member.PressedKeys.Count == 0)
                {
                    member.SkillWake = _schedule.ScheduleAt(run.Began + Wait(next), () => Play(member, run));

                    return;
                }

                foreach (var hit in run.Playback.Advance(now))
                {
                    if (hit.Trigger.IsTrap)
                    {
                        PlaceTrap(member, run.Skill, hit.Trigger, run.Position);
                    }
                    else
                    {
                        Run(ApplyHitAsync(member, run.Skill, hit, run.Position));
                    }
                }

                while (member.PressedKeys.TryDequeue(out var pressed))
                {
                    run.Playback.Press(pressed, now);
                }
            }

            // The skill is over, and a trap that followed the cursor stays where it is.
            if (run.Generation == member.SkillGeneration)
            {
                member.MouseTrap = null;
            }
        }

        catch (Exception e)
        {
            Logger.SkillPlaybackFailed(e);
        }
    }

    private ValueTask ApplyHitAsync(Fighter member, int skill, SkillHit hit, SkillPositionValue position)
    {
        var attacker = Ecs.Get<WorldComponent>(member.Player);
        var area = new AttackArea(hit.Trigger, attacker.Position, attacker.Rotation);
        var targets = creatureManager.FindMonsters((place, body) => area.Touches(place, body.BodyRadius, body.BodyHeight));

        return ApplyDamageAsync(member, skill, hit.Trigger, targets, position);
    }

    /// <summary>
    /// A trap outlives the skill that placed it: a new skill does not take it away.
    /// </summary>
    private void PlaceTrap(Fighter member, int skill, AttackTrigger trigger, SkillPositionValue position)
    {
        var info = trigger.Projectile!;
        var attacker = Ecs.Get<WorldComponent>(member.Player);
        var (sin, cos) = MathF.SinCos(attacker.Rotation * MathF.PI / 180f);
        var site = new TrapSite(attacker.Position);

        if (actions.FollowsMouse(skill))
        {
            // One trap follows the cursor at a time, a second attack of the skill only keeps it company.
            if (member.MouseTrap is not null)
            {
                return;
            }

            member.MouseTrap = site;
        }

        if (!actions.IsFlownByClient(skill))
        {
            creatureManager.Broadcast(new SkillProjectileBroadcastResponse
            {
                Owner = Ecs.Get<ActorComponent>(member.Player),
                Projectile = default(ProjectileShotValue) with
                {
                    Skill = skill,
                    Attack = (short)trigger.Id,
                    Position = attacker.Position,
                    Direction = new Vector3(sin, -cos, 0f),
                    Session = ++member.ProjectileSession
                }
            });
        }

        StrikeTrap(member, skill, trigger, position, site, attacker.Rotation, info.LifeTime);
    }

    /// <summary>
    /// One hit of a trap. A trap that hits again and again waits for its next hit on the clock, as long as its life lasts.
    /// </summary>
    private void StrikeTrap(Fighter member, int skill, AttackTrigger trigger, SkillPositionValue position, TrapSite site, float yaw, float left)
    {
        if (Closed || member.Gone)
        {
            return;
        }

        var info = trigger.Projectile!;

        // The place is read again at every hit: the player may have moved the trap since the last one.
        var area = new AttackArea(trigger, site.Position, yaw);
        var targets = creatureManager.FindMonsters((place, body) => area.Touches(place, body.BodyRadius, body.BodyHeight));

        Run(ApplyDamageAsync(member, skill, trigger, targets, position));

        if (!info.IsContinuous || info.ContinuousTime <= 0f || left <= info.ContinuousTime)
        {
            return;
        }

        _schedule.Schedule(Wait(info.ContinuousTime), () => StrikeTrap(member, skill, trigger, position, site, yaw, left - info.ContinuousTime));
    }

    /// <summary>
    /// The client fires a projectile when its animation gets there, and the server follows it to see whom it hits.
    /// </summary>
    [LuaIgnoreMember]
    public void FireProjectile(Entity player, SkillProjectileRequest request)
    {
        if (!Started || Find(player) is not { } member)
        {
            return;
        }

        var hero = Ecs.Get<PersonInfoComponent>(player).Hero;

        // Only the skill that is being played fires its projectiles: a skill the server refused fires nothing.
        if (request.Skill != member.CurrentSkill || !actions.TryGetProjectile(request.Skill, hero, request.TriggerIndex, out var trigger))
        {
            return;
        }

        var flight = ProjectileFlight.Launch(trigger, request.Position, request.Right, request.Yaw, request.Direction, request.Target.Identifier);
        var session = ++member.ProjectileSession;

        if (!actions.IsFlownByClient(request.Skill))
        {
            creatureManager.Broadcast(new SkillProjectileBroadcastResponse
            {
                Owner = Ecs.Get<ActorComponent>(player),
                Projectile = default(ProjectileShotValue) with
                {
                    Skill = request.Skill,
                    Attack = request.TriggerIndex,
                    Position = flight.Position,
                    Direction = flight.Direction,
                    Session = session
                }
            });
        }

        var position = default(SkillPositionValue) with { Position = request.Position, Angle = request.Yaw };

        // It flies with the steps of the maze from now on.
        member.Flights.Add(new PlayerFlight(request.Skill, trigger, flight, position, Clock.Time));
    }

    /// <summary>
    /// The player drags the trap of a skill with the mouse. The trap hits where it is; the others see only the hits.
    /// </summary>
    [LuaIgnoreMember]
    public void MoveTrap(Entity player, Vector3 place)
    {
        if (Started && Find(player) is { MouseTrap: { } trap })
        {
            trap.Position = place;
        }
    }

    /// <summary>
    /// The client lets a chain go when its animation gets there. The chain jumps between monsters on the server,
    /// and the client draws it from what it is told: who it holds and when it lets go.
    /// </summary>
    [LuaIgnoreMember]
    public void FireChain(Entity player, SkillProjectileValue request)
    {
        if (!Started || Find(player) is not { } member)
        {
            return;
        }

        var hero = Ecs.Get<PersonInfoComponent>(player).Hero;

        // Only the skill that is being played may let a chain go.
        if (request.Skill != member.CurrentSkill || !actions.TryGetChain(request.Skill, hero, request.TriggerIndex, out var trigger))
        {
            return;
        }

        var attacker = Ecs.Get<WorldComponent>(player);
        var chain = ChainLightning.Fire(trigger, attacker.Position, attacker.Rotation, creatureManager.GetFlightTargets());
        var session = ++member.ProjectileSession;
        var first = chain.Targets.Count > 0 ? new ActorValue(chain.Targets[0], ActorType.Monster) : (ActorValue)(-1);

        creatureManager.Broadcast(new SkillChainBroadcastResponse
        {
            Owner = Ecs.Get<ActorComponent>(player),
            SkillChainPosition = default(SkillChainPosition) with
            {
                Skill = request.Skill,
                TriggerIndex = request.TriggerIndex,
                Position = attacker.Position,
                Direction = new Vector3(0f, -1f, 0f) * actions.GetMaxRange(request.Skill),
                Session = session,
                Target = first
            }
        });

        var position = default(SkillPositionValue) with { Position = attacker.Position, Angle = attacker.Rotation };

        member.Chains.Add(new ChainRun(request.Skill, trigger, chain, session, position, member.SkillGeneration, Clock.Time));
    }

    /// <summary>
    /// The chains of the player step with the maze. A chain belongs to the skill that let it go: it lives its time, or
    /// until the player starts another skill.
    /// </summary>
    private void StepChains(Fighter member)
    {
        var chains = member.Chains;

        if (chains.Count == 0)
        {
            return;
        }

        var now = Clock.Time;

        for (var index = 0; index < chains.Count && !Closed && !member.Gone; index++)
        {
            var run = chains[index];

            if (run.Failed || run.Chain.IsOver || run.Generation != member.SkillGeneration)
            {
                continue;
            }

            try
            {
                var seconds = (float)Math.Max(0d, (now - run.Last).TotalSeconds);
                var owner = Ecs.Get<WorldComponent>(member.Player);

                run.Last = now;
                run.Hits.Clear();
                run.Changes.Clear();

                run.Chain.Advance(seconds, creatureManager.GetFlightTargets(), owner.Position, owner.Rotation, run.Hits, run.Changes);

                foreach (var change in run.Changes)
                {
                    creatureManager.Broadcast(new SkillChainUpdateTargetResponse
                    {
                        Session = run.Session,
                        IsAdd = change.Added,
                        Target = new ActorValue(change.Target, ActorType.Monster)
                    });
                }

                if (run.Hits.Count > 0)
                {
                    Run(StrikeChainAsync(member, run.Skill, run.Trigger, run.Hits, run.Position));
                }
            }

            catch (Exception e)
            {
                Logger.ChainFailed(e);

                run.Failed = true;
            }
        }

        chains.RemoveAll(e => e.Failed || e.Chain.IsOver || e.Generation != member.SkillGeneration);
    }

    /// <summary>
    /// Everyone the chain reaches at one moment is hit together. The links after the first carry a share of the damage.
    /// </summary>
    private ValueTask StrikeChainAsync(Fighter member, int skill, AttackTrigger trigger, List<ChainHit> hits, SkillPositionValue position)
    {
        var targets = new List<Entity>();
        var shares = new List<float>();

        foreach (var hit in hits)
        {
            if (creatureManager.FindMonster(hit.Target) is { } monster)
            {
                targets.Add(monster);
                shares.Add(hit.Order > 0 ? trigger.Chain!.DamageMultiple : 1f);
            }
        }

        return ApplyDamageAsync(member, skill, trigger, [.. targets], position, [.. shares]);
    }

    /// <summary>
    /// The projectiles of the player step with the maze; one that has hit what it can or lived its time is gone.
    /// </summary>
    private void StepFlights(Fighter member)
    {
        var flights = member.Flights;

        if (flights.Count == 0)
        {
            return;
        }

        var now = Clock.Time;

        for (var index = 0; index < flights.Count && !Closed && !member.Gone; index++)
        {
            var flight = flights[index];

            try
            {
                var seconds = (float)Math.Max(0d, (now - flight.Last).TotalSeconds);

                flight.Last = now;

                foreach (var explosion in flight.Flight.Advance(seconds, creatureManager.GetFlightTargets()))
                {
                    Run(ExplodeAsync(member, flight.Skill, flight.Trigger, explosion, flight.Position));
                }
            }

            catch (Exception e)
            {
                Logger.ProjectileFlightFailed(e, flight.Skill);

                flight.Failed = true;
            }
        }

        flights.RemoveAll(static e => e.Failed || e.Flight.IsOver);
    }

    private ValueTask ExplodeAsync(Fighter member, int skill, AttackTrigger trigger, Explosion explosion, SkillPositionValue position)
    {
        var yaw = Ecs.Get<WorldComponent>(member.Player).Rotation;
        var area = new AttackArea(trigger, explosion.Position, yaw);
        var targets = trigger.Projectile is { TargetOnly: true }
            ? []
            : creatureManager.FindMonsters((place, body) => area.Touches(place, body.BodyRadius, body.BodyHeight));

        if (explosion.Collider is { } key && creatureManager.FindMonster(key) is { } collider && !targets.Contains(collider))
        {
            targets = [.. targets, collider];
        }

        return ApplyDamageAsync(member, skill, trigger, targets, position);
    }

    /// <param name="shares">The share of the damage each target takes, by their place in the list. Everyone takes all of it without.</param>
    private async ValueTask ApplyDamageAsync(Fighter member, int skill, AttackTrigger trigger, Entity[] targets, SkillPositionValue position, float[]? shares = null)
    {
        var player = member.Player;

        // An attack for the side of the player, or one that only gives a buff, hurts nobody.
        if (GivesBuffOnly(member, trigger, targets))
        {
            return;
        }

        // A hit that was on its way when the player left lands on nothing.
        if (targets.Length == 0 || !trigger.AffectsTargets || member.Gone || !Ecs.IsAlive(player))
        {
            return;
        }

        var attacker = Ecs.Get<WorldComponent>(player);

        var attackerStats = combat.GetPlayer(Ecs, player);
        var skillDamage = combat.GetSkill(skill);
        var hits = targets.Select((target, i) => ApplyDamage(player, target, attackerStats, skillDamage, trigger.DamageRate * (shares?[i] ?? 1f), trigger.ReactionType)).ToArray();

        // The combo of the player goes into the message of the hit: the client shows it.
        var now = (long)Clock.Time.TotalMilliseconds;
        member.Combo = Gameplay.Combat.Combo.Next(member.Combo, member.LastHit, hits.Length, now);
        member.LastHit = now;

        MonstersHit?.Invoke(player, hits.Length, hits.Sum(e => (long)e.Damage));

        // The buffs of the player and of the monsters it hit that answer to a hit: blood, reflection, soul absorption, the cards
        // that wait for a hit, a critical hit or a miss, and the combo.
        for (var i = 0; i < targets.Length; i++)
        {
            if (hits[i].Damage > 0 && Ecs.IsAlive(targets[i]))
            {
                _buffs.AfterHit(player, targets[i], i, hits[i].Damage, Ecs.Get<MonsterCombatComponent>(targets[i]).Stats);
            }

            var flags = (HitResults)hits[i].DamageFlags;

            _buffs.OnCondition(player, flags.HasFlag(HitResults.Miss) ? MazeBuffs.OnMissedAttack : MazeBuffs.OnAttack);

            if (flags.HasFlag(HitResults.Critical))
            {
                _buffs.OnCondition(player, MazeBuffs.OnCriticalAttack);
            }
        }

        _buffs.OnHits(player, hits.Count(e => (e.DamageFlags & (byte)HitResults.Miss) == 0));

        // Every target hit gives the soul gauge of the attacker a share of what the skill absorbs; the player is told once.
        if (creatureManager.Services?.Gauges is { } gauges && skillDamage.SoulGaugeAbsorb > 0f)
        {
            var soul = gauges.Of(player).SoulGauge;
            var absorbed = 0f;

            foreach (var target in targets)
            {
                absorbed += SoulGauge.Absorbed(skillDamage.SoulGaugeAbsorb, Ecs.Get<MonsterCombatComponent>(target).Stats.SoulGaugeAbsorbRate, soul + absorbed, attackerStats.SoulGaugeMax);
            }

            gauges.Absorb(player, absorbed, attackerStats.SoulGaugeMax);
        }

        creatureManager.Broadcast(new SkillActionBroadcastResponse
        {
            SkillDmgValueList = hits,
            SkillAction = default(SkillActionValue) with
            {
                SkillActorInfo = default(SkillActorInfoValue) with
                {
                    UxActorId = Ecs.Get<ActorComponent>(player),
                    SkillPosInfo = default(SkillActionPosInfoValue) with
                    {
                        Position = attacker.Position,
                        Angle = attacker.Rotation,
                        MotionClass = position.MotionClass,
                        HasInputFlag = position.HasInputFlag
                    }
                },
                Skill = skill,
                TriggerIndex = (short)trigger.Id,
                ContinuousHit = (ushort)Math.Min(member.Combo, ushort.MaxValue)
            }
        });

        // Every blow that lands may wear the weapon of the player.
        if (creatureManager.Services?.Wear is { } wear)
        {
            foreach (var hit in hits)
            {
                if (hit.Damage > 0)
                {
                    wear.Attacked(player);
                }
            }
        }

        // The buff of the attack reaches the targets it hit and left standing.
        _buffs.GiveFromHit(player, trigger, targets);

        // Another hit can land while a script runs, so the dead leave the fight before the first script starts.
        var dead = targets
            .Select((target, i) => (Target: target, Hit: hits[i]))
            .Where(e => Ecs.Get<CreatureComponent>(e.Target).Health == 0)
            .Select(e => (Removed: RemoveDead(e.Target), e.Hit))
            .ToArray();

        // What the player killed pays out at once; monsters that a script or the end of the maze takes away do not.
        foreach (var ((identifier, _, fell), _) in dead)
        {
            MonsterKilled?.Invoke(player, identifier, fell);
        }

        var kind = actions.GetPassiveType(skill);

        foreach (var ((identifier, place, _), hit) in dead)
        {
            if (Closed)
            {
                return;
            }

            // The killer may have left while a script ran; the death still counts for the maze.
            await ReportKillAsync(player, identifier, MazeReporter.GetKillConditions(kind, (hit.DamageFlags & (byte)HitResults.Critical) != 0));

            await State.OnDie(identifier, place.SpawnBox, this);
        }

        foreach (var sector in dead.Select(e => e.Removed.Place.Sector).Distinct())
        {
            if (Closed)
            {
                return;
            }

            await CheckSectorAsync(sector);
        }
    }

    private SkillDmgValue ApplyDamage(Entity player, Entity target, in CombatStats attacker, in SkillDamage skill, float attackRate, int reaction)
    {
        var creature = Ecs.Get<CreatureComponent>(target);
        var body = Ecs.Get<MonsterCombatComponent>(target);
        // A super armor that was broken before this hit lets half as much again through.
        var broken = body.MaxSuperArmor > 0f && body.SuperArmor <= 0f;
        var result = DamageCalculator.Calculate(attacker, _buffs.StatsOf(target, body.Stats), skill, _buffs.DefenseOf(target, body.Defense), attackRate, Random.Shared, broken);

        result = result with { Damage = _buffs.Adjust(player, target, result.Damage) };

        var lost = result.Damage;

        if (body.CountsHits)
        {
            var now = Environment.TickCount64;

            lost = result.Damage > 0 && now - body.LastCountedHit >= CountedHitMilliseconds ? 1 : 0;

            if (lost > 0)
            {
                Ecs.Set(target, body with { LastCountedHit = now });
            }
        }

        var health = Math.Max(0, creature.Health - lost);

        Ecs.Set(target, creature with { Health = health });

        creatureManager.OnMonsterHit(target, player, result.Damage);

        var effect = creatureManager.AbsorbHit(target, skill.SuperArmorAttack, reaction, health > 0);

        return default(SkillDmgValue) with
        {
            UxActorId = Ecs.Get<ActorComponent>(target),
            ReactionType = effect.Reaction,
            DamageFlags = (byte)result.Flags,
            Damage = result.Damage,
            Health = health,
            DefenseType = effect.Defense,
            CurSuperArmorGauge = effect.SuperArmor
        };
    }

    private async ValueTask CheckSectorAsync(int sector)
    {
        var kills = creatureManager.GetKillProgress(sector);

        if (kills.IsCleared(creatureManager.CountBoxesToCome(sector)))
        {
            await CompleteSectorAsync(sector);
        }

        if (!Closed)
        {
            await CheckSpawnStepAsync(sector);
        }
    }

    /// <summary>
    /// Enough dead open the next wave of a sector, unless the script holds it back.
    /// </summary>
    private async ValueTask CheckSpawnStepAsync(int sector)
    {
        var kills = creatureManager.GetKillProgress(sector);

        if (kills.Stopped)
        {
            return;
        }

        if (!kills.IsCurrentStepSpawned)
        {
            SpawnSector(sector);

            return;
        }

        if (!kills.TryOpenNextStep())
        {
            return;
        }

        await State.OnCompleteSpawnStepCondition(sector, kills.Step, this);

        if (!Closed && !kills.Stopped)
        {
            SpawnSector(sector);
        }
    }

    private void WakeSectorBehindGate(int gate, bool open)
    {
        creatureManager.SetGate(gate, open);

        if (open && Batch.EventBox.ServerGates.Find(gate) is { NextSector: > 0 } box)
        {
            SpawnSector(box.NextSector);
        }
    }

    private (int Identifier, NonPlayableCreatureComponent Place, System.Numerics.Vector3 Position) RemoveDead(Entity target)
    {
        var identifier = Ecs.Get<CreatureComponent>(target).Identifier;
        var place = Ecs.Get<NonPlayableCreatureComponent>(target);
        var position = Ecs.Get<WorldComponent>(target).Position;

        creatureManager.Broadcast(new MonsterDieResponse { Actor = Ecs.Get<ActorComponent>(target) });

        creatureManager.Remove(target);

        return (identifier, place, position);
    }

    /// <summary>
    /// A cleared sector tells its script, then the client, and opens its way out. What lies behind comes to life.
    /// </summary>
    private async ValueTask CompleteSectorAsync(int sector)
    {
        var box = Batch.EventBox.Sectors.Find(sector);

        // Sectors of other kinds are finished by their script, or by the quests of the player.
        if (box is null || !Progress.ClearSector(sector))
        {
            return;
        }

        await FinishSectorAsync(box);
    }

    private async ValueTask FinishSectorAsync(SectorBox box)
    {
        await State.OnCompleteSector(box.Id, this);

        if (Closed)
        {
            return;
        }

        creatureManager.Broadcast(new MazeClearSectorResponse { Sector = box.Id, Status = true });

        foreach (var player in Present())
        {
            if (Ecs.IsAlive(player))
            {
                await reports.SectorClearedAsync(player, info.Id, box.Id);
            }

            if (Closed)
            {
                return;
            }
        }

        // The way out of the last sector leads out of the maze: the maze is won before the door says so.
        if (IsLastSector(box))
        {
            await CompleteMazeAsync(box);
        }

        OpenExit(box);
    }

    /// <summary>
    /// The client learns which maze of its group is cleared, and that the next episode is open. The mazes cleared
    /// before are not kept anywhere yet, so only this one is told.
    /// </summary>
    private void SendClearInfo()
    {
        var group = new MazeClearValue(info.Group, (byte)Math.Min(info.Episode + 1, MaxEpisode), [(info.Id, 1)]);

        foreach (var member in _members)
        {
            member.Player.Get<ServerSessionComponent>().Send(new MazeClearInfoResponse { Groups = [group], Person = Ecs.Get<ActorComponent>(member.Player), IsReset = false });
        }
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
    /// The way out of a sector is a door or a portal, and it opens once the sector is cleared.
    /// </summary>
    private void OpenExit(SectorBox box)
    {
        if (box.Exit == 0)
        {
            return;
        }

        if (box.ExitType == SectorExitType.Gate)
        {
            SetGateFlag(box.Exit, true);
        }
        else
        {
            SetPotalFlag(box.Exit, true);
        }
    }

    /// <summary>
    /// The last sector is the one with the boss, or the one whose portal leads out of the maze. The maze is won
    /// when it is cleared, even if a calm sector with the way out lies behind it.
    /// </summary>
    private bool IsLastSector(SectorBox box)
    {
        if (box.Type == SectorType.Boss)
        {
            return true;
        }

        return box.ExitType == SectorExitType.Portal
            && Batch.EventBox.Portals.Find(box.Exit) is { JumpType: not JumpType.JumpPos };
    }

    /// <summary>
    /// What happens once, when the last sector is cleared: the rest of its monsters go, the clock stops and the
    /// client is told that the maze is won.
    /// </summary>
    private async ValueTask CompleteMazeAsync(SectorBox last)
    {
        if (IsComplete)
        {
            return;
        }

        IsComplete = true;
        MazeState = MazeState.None;

        SendClearInfo();

        Completed?.Invoke();

        foreach (var player in Present())
        {
            if (Closed)
            {
                return;
            }

            if (!Ecs.IsAlive(player))
            {
                continue;
            }

            await reports.MazeClearedAsync(
                player,
                info,
                (int)Stopwatch.GetElapsedTime(StartedAt).TotalSeconds,
                (int)Ecs.Get<PersonInfoComponent>(player).Hero,
                Ecs.TryGet<LevelComponent>(player, out var level) ? level.Level : PersonModuleDefines.DefaultLevel);
        }

        if (Closed)
        {
            return;
        }

        await DestroyAsync(creatureManager.FindMonsters((_, _) => true).Where(e => Ecs.Get<NonPlayableCreatureComponent>(e).Sector == last.Id).ToArray(), false);
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

    /// <summary>
    /// The objects of soul metries stand in the maze all the time, but only the ones the person is to find this
    /// time are shown. This is called before the client is told what the maze holds.
    /// </summary>
    public void HideUnfoundSoulObjects(Entity player)
    {
        var objects = reports.GetSoulObjects(info.Id);

        if (objects.Count == 0)
        {
            return;
        }

        var shown = reports.GetSoulObjectsToShow(player, info.Id);

        foreach (var box in Batch.EventBox.InterActions.Where(e => objects.Contains(e.Interaction) && !shown.Contains(e.Interaction)))
        {
            Progress.HideInteraction(box.Id);
        }
    }

    /// <summary>
    /// The person clicked an object of the maze: it counts for the quests, for everyone in the maze when the object says so,
    /// and if it is one of a soul metry, the soul metry finds it.
    /// </summary>
    public async ValueTask CollectSoulObjectAsync(Entity player, int box)
    {
        if (Progress.CountsForEveryone(box))
        {
            foreach (var member in _members.Select(e => e.Player).Where(Ecs.IsAlive).ToArray())
            {
                await reports.ObjectUsedAsync(member, box);

                if (Closed)
                {
                    return;
                }
            }
        }

        else
        {
            await reports.ObjectUsedAsync(player, box);
        }

        if (Closed)
        {
            return;
        }

        if (Batch.EventBox.InterActions.Find(box) is { } found)
        {
            await reports.CollectSoulObjectAsync(player, info.Id, found.Interaction);
        }
    }

    /// <summary>
    /// The cubes of the maze, which the person takes by holding the click for the time the table gives the object.
    /// </summary>
    public void RegisterCubes(Entity player)
    {
        foreach (var box in Batch.EventBox.InterActions.Where(e => e.ObjectKey.Contains(CubeKey, StringComparison.OrdinalIgnoreCase)))
        {
            var row = interactions.GetOrDefault(box.Interaction);

            reports.RegisterCube(player, box.Id, box.Interaction, TimeSpan.FromMilliseconds(row.ActionDelay));
        }
    }
    
    [LuaMember]
    public void AcceptQuest(int episode)
    {
        foreach (var member in _members)
        {
            questManager.AcceptEpisode(member.Player, episode);
        }
    }

    /// <summary>
    /// Gives a timer with steps more time.
    /// </summary>
    [LuaMember]
    public void AddEventMaxTime(string function, float time)
    {
        Timers.Extend(function, time);
    }

    /// <param name="function">Called when the time of a step is up. It also names the timer.</param>
    /// <param name="step">Called when the script finishes a step early.</param>
    /// <param name="ready">Called when the waiting before a step begins.</param>
    /// <param name="type">Only timers with steps are known.</param>
    /// <param name="time">How long a step takes.</param>
    /// <param name="steps">How many steps there are.</param>
    /// <param name="wait">How long it waits before each step.</param>
    [LuaMember]
    public void AddEventTimer(string function, string step, string ready, int type, float time, int steps, int wait)
    {
        if (type != MazeTimer.Stepped)
        {
            Logger.UnknownEventTimerType(type, function);
            return;
        }

        Timers.AddStepped(function, step, ready, time, steps, wait);
    }

    [LuaMember]
    public void AddLuaValue(int index, int value)
    {
        LuaValues[index] = LuaValues.GetValueOrDefault(index) + value;
    }

    [LuaMember]
    public void AddTimer(int identifier, float time)
    {
        Logger.CallAddTimer(identifier, time);

        Timers.AddPlain(identifier, time);
    }

    [LuaMember]
    public void AddTimerEx(string function, float time, int param1, int param2, int param3)
    {
        Logger.CallAddTimerEx(function, time, param1, param2, param3);

        Timers.AddPlain(function, time, param1, param2, param3);
    }

    /// <summary>
    /// Everything that lives in the sector dies at once and without a reward.
    /// </summary>
    [LuaMember]
    public void AllDestroySectorMonster(int sector)
    {
        Later(0f, () => DestroyAsync(MonstersOf(sector), false));
    }

    [LuaMember]
    public void AllUserWarpInSector(VVector3 position, float rotation)
    {
        var place = new Vector3(position.X, position.Y, position.Z);

        foreach (var member in _members)
        {
            Ecs.Set(member.Player, Ecs.Get<WorldComponent>(member.Player) with { Position = place, Rotation = rotation });
        }

        creatureManager.Broadcast(new MazeWarpInSectorResponse { Position = place, Rotation = rotation });
    }

    [LuaMember]
    public void ApplyBuff(int actor, int buff)
    {
        Logger.CallApplyBuff(actor, buff);

        // Everybody in the maze sees the buff on the one it is for.
        if (!ApplyScriptBuff(actor, buff))
        {
            Logger.UnknownBuff(actor, buff);
        }
    }

    /// <summary>
    /// Monsters do not turn into others here: the script goes on with the old ones.
    /// </summary>
    [LuaMember]
    public void ChangeMonster(string from, string to)
    {
        NotDone("ChangeMonster");
        Logger.IgnoredChangeMonster(from, to);
    }

    [LuaMember]
    public void ChangeNpcAnimation(int npc, string animation)
    {
        NotDone("ChangeNpcAnimation");
        Logger.IgnoredChangeNpcAnimation(npc, animation);
    }

    /// <summary>
    /// A message to one player, or to everyone when the number is zero.
    /// </summary>
    [LuaMember]
    public void ChatMessage(int user, string message)
    {
        var notice = new ChatNoticeResponse { Type = 0, Message = message, Color = "" };

        if (user == 0)
        {
            creatureManager.Broadcast(notice);

            return;
        }

        foreach (var member in _members)
        {
            if (user == (int)Ecs.Get<ActorComponent>(member.Player))
            {
                member.Player.Get<ServerSessionComponent>().Send(notice);
            }
        }
    }

    [LuaMember]
    public void ClearAllTimers()
    {
        Timers.Clear();
    }

    [LuaMember]
    public void CompleteCondition(int condition)
    {
        foreach (var member in _members)
        {
            questManager.CompleteCondition(member.Player, condition);
        }
    }

    [LuaMember]
    public void CompleteQuest(int episode)
    {
        foreach (var member in _members)
        {
            questManager.CompleteEpisode(member.Player, episode);
        }
    }

    /// <summary>
    /// The script says a step of a timer is done before its time is up.
    /// </summary>
    [LuaMember]
    public void CompleteTimerStep(string function, int step)
    {
        Timers.Complete(function, step);
    }

    [LuaMember]
    public void EnableInteractionBox(int box, bool enable)
    {
        if (Progress.SetInteraction(box, enable) is { } value)
        {
            creatureManager.Broadcast(new MazeInteractionEnableResponse
            {
                Show = value.Show,
                Enable = value.Enable,
                BoxIndex = value.Box,
                CallCount = value.CallCount
            });
        }
    }

    [LuaMember("ExcutEventSpawnGroupLua")]
    public void ExecuteEventSpawnGroupLua(int group)
    {
        NotDone("ExcutEventSpawnGroupLua");
        Logger.IgnoredExecuteEventSpawnGroup(group);
    }

    [LuaMember("ExcutEventSpawnLua")]
    public void ExecuteEventSpawnLua(int box)
    {
        Logger.CallExecuteEventSpawn(box);

        creatureManager.Create(box);
    }

    /// <summary>
    /// Makes the monsters of a table die, wherever they are. Monsters are the type 2, townsfolk the type 1.
    /// </summary>
    [LuaMember]
    public void ExecuteDestroy(int type, string table, string animation, bool suicide = true)
    {
        if (type != 2 || !int.TryParse(table, out var identifier))
        {
            NotDone("ExecuteDestroy");
            Logger.IgnoredExecuteDestroy(type, table, animation, suicide);
            return;
        }

        Later(0f, () => DestroyAsync(
            creatureManager.FindMonsters((_, _) => true).Where(e => Ecs.Get<CreatureComponent>(e).Identifier == identifier).ToArray(),
            false));
    }

    [LuaMember]
    public VVector3 GetCommonPositionBox(int box)
    {
        Logger.CallGetCommonPositionBox(box);

        // A script that names a box the batch does not have keeps running, with the origin.
        if (Batch.EventBox.CommonPositions.Find(box) is not { } position)
        {
            Logger.UnknownPositionBox(box);

            return new VVector3();
        }

        return position.GetRandomPosition();
    }

    [LuaMember]
    public int GetLuaValue(int index)
    {
        return LuaValues.GetValueOrDefault(index);
    }

    /// <summary>
    /// A quest of the party. There is no party in a maze of one.
    /// </summary>
    [LuaMember]
    public void InitPartyQuest(int quest, int condition, int sector)
    {
        NotDone("InitPartyQuest");
        Logger.IgnoredInitPartyQuest(quest, condition, sector);
    }

    /// <summary>
    /// Whether anybody in the maze has a quest that counts the condition.
    /// </summary>
    [LuaMember]
    public bool IsHaveCondition(int conditionId)
    {
        return _members.Exists(e => questManager.ActiveEpisodeUsesCondition(e.Player, conditionId));
    }

    /// <summary>
    /// Whether anybody in the maze has the quest.
    /// </summary>
    [LuaMember]
    public bool IsHaveQuest(int episodeId)
    {
        return _members.Exists(e => questManager.HasActiveEpisode(e.Player, episodeId));
    }

    /// <summary>
    /// The client runs a function of its own script when it hears this.
    /// </summary>
    [LuaMember]
    public void LuaClientSync(int type, int value, int sector = 0, float time = 0)
    {
        creatureManager.Broadcast(new MazeLuaClientSyncResponse { Type = type, Value = value });
    }

    /// <summary>
    /// A townsman walks to a point of the map, and the script hears when it is there (<c>OnNpcWayPoint</c>). It walks
    /// straight, and only the latest walk it was given ends.
    /// </summary>
    [LuaMember]
    public void MoveNpcToWayPoint(int npc, int waypoint)
    {
        Logger.CallMoveNpcToWayPoint(npc, waypoint);

        if (!creatureManager.TryFindCreature(npc, out var creature))
        {
            Logger.UnknownNpcToMove(npc, waypoint);
            return;
        }

        if (FindWayPoint(waypoint) is not { } point)
        {
            Logger.UnknownWayPoint(npc, waypoint);
            return;
        }

        var target = (point.PosTopLeft + point.PosBottomRight) / 2f;
        var walk = NpcWalks[npc] = NpcWalks.GetValueOrDefault(npc) + 1;
        var seconds = creatureManager.Walk(creature, target);

        Later(seconds, () =>
        {
            if (NpcWalks.GetValueOrDefault(npc) != walk || !Ecs.IsAlive(creature))
            {
                return ValueTask.CompletedTask;
            }

            creatureManager.Arrive(creature, target);

            return State.OnNpcWayPoint(npc, waypoint, this);
        });
    }

    private WayPoint? FindWayPoint(int waypoint)
    {
        foreach (var point in Batch.EventPoint.WayPoints)
        {
            if (point.Id == waypoint)
            {
                return point;
            }
        }

        return null;
    }

    [LuaMember]
    public void RemoveQuestAll()
    {
        foreach (var member in _members)
        {
            questManager.RemoveAllActiveEpisodes(member.Player);
        }
    }

    /// <summary>
    /// Stops a timer without telling the script, and says how long it had left.
    /// </summary>
    [LuaMember]
    public float RemoveTimerEx(string function)
    {
        return Timers.Cancel(function);
    }

    /// <summary>
    /// A sector the script clears is finished like one that was cleared by the fight.
    /// </summary>
    [LuaMember]
    public void SectorClear(int sector)
    {
        Later(0f, () => CompleteSectorAsync(sector));
    }

    [LuaMember]
    public void SetEscortCondition(int condition)
    {
        NotDone("SetEscortCondition");
        Logger.IgnoredSetEscortCondition(condition);
    }

    [LuaMember]
    public void SetEscortMonster(LuaValue monster, LuaValue condition, LuaValue name)
    {
        NotDone("SetEscortMonster");
        Logger.IgnoredSetEscortMonster();
    }

    [LuaMember]
    public void SetGateFlag(int gate, bool open)
    {
        if (Progress.SetGate(gate, open))
        {
            var next = Batch.EventBox.ServerGates.Find(gate)?.NextSector ?? 0;

            creatureManager.Broadcast(new WorldGateEnableResponse { Gate = gate, Open = open, IsMazeComplete = IsComplete, NextSector = next });

            WakeSectorBehindGate(gate, open);
        }
    }

    [LuaMember]
    public void SetGateFlagNoSend(int gate, bool open)
    {
        if (Progress.SetGate(gate, open))
        {
            WakeSectorBehindGate(gate, open);
        }
    }

    /// <summary>
    /// A key stays in the maze. A number goes under an index: <c>SetLuaValue(index, value)</c>.
    /// </summary>
    [LuaMember("SetLuaValue")]
    public void SetLuaValue(LuaValue keyOrIndex, LuaValue value)
    {
        if (keyOrIndex.TryRead<string>(out var key))
        {
            LuaKeys.Add(key);
            return;
        }

        // A script that gives no number under no index stores nothing; a value that is no number is zero, as in the scripts
        // that compare what they stored with numbers.
        if (!keyOrIndex.TryRead<int>(out var index))
        {
            Logger.UnreadableLuaValue();
            return;
        }

        LuaValues[index] = value.TryRead<int>(out var number) ? number : 0;
    }

    /// <summary>
    /// Lua-CSharp is not supports enum as parameter right now
    /// </summary>
    [LuaMember]
    public void SetMazeState(int state)
    {
        if (Enum.IsDefined(typeof(MazeState), state))
        {
            SetMazeState((MazeState)state);
        }
        else
        {
            Logger.UnknownMazeState(state);
        }
    }

    /// <summary>
    /// The state only tells scripts what the maze is doing. The client does not read it, and a maze waiting
    /// to die goes on by itself.
    /// </summary>
    [LuaIgnoreMember]
    public void SetMazeState(MazeState state)
    {
        Logger.CallSetMazeState(state);

        MazeState = state;

        if (state == MazeState.WaitToDie)
        {
            Later(0f, () =>
            {
                if (MazeState == MazeState.WaitToDie)
                {
                    MazeState = MazeState.Run;
                }

                return ValueTask.CompletedTask;
            });
        }
    }

    [LuaMember]
    public void SetMonsterAllowPassiveType(int monster, int type)
    {
        NotDone("SetMonsterAllowPassiveType");
        Logger.IgnoredSetMonsterAllowPassiveType(monster, type);
    }

    [LuaMember]
    public void SetMonsterDefenceType(int monster, int type, float time)
    {
        NotDone("SetMonsterDefenceType");
        Logger.IgnoredSetMonsterDefenceType(monster, type, time);
    }

    /// <summary>
    /// The townsman turns to the angle, and the script hears when it has (<c>OnNpcRotation</c>) a moment later.
    /// </summary>
    [LuaMember]
    public void SetNpcRotation(int npc, float angle, int key)
    {
        if (float.IsFinite(angle) && creatureManager.TryFindCreature(npc, out var creature))
        {
            creatureManager.Turn(creature, angle);
        }

        Later(NpcMoveSeconds, () => State.OnNpcRotation(npc, key, this));
    }

    [LuaMember]
    public void SetPotalFlag(int box, bool flag)
    {
        if (Progress.SetPortal(box, flag))
        {
            creatureManager.Broadcast(new WorldWarpEnableResponse { Box = box, Open = flag, IsMazeComplete = IsComplete });

            if (flag && Batch.EventBox.Portals.Find(box) is { NextSector: > 0 } portal)
            {
                SpawnSector(portal.NextSector);
            }
        }
    }

    [LuaMember]
    public void SetSectorStepStop(int sector, bool stop)
    {
        var kills = creatureManager.GetKillProgress(sector);

        if (kills.Stopped == stop)
        {
            return;
        }

        kills.Stopped = stop;

        if (!stop)
        {
            Later(0f, () => CheckSpawnStepAsync(sector));
        }
    }

    [LuaMember]
    public void SetTrapLifeTime(int index, float time)
    {
        NotDone("SetTrapLifeTime");
        Logger.IgnoredSetTrapLifeTime(index, time);
    }

    /// <summary>
    /// A countdown on the screen: nothing counts without the client's own clock, so the number is the time to show.
    /// A negative value hides it.
    /// </summary>
    [LuaMember]
    public void ShowCasualRaidTimer(int value, float time, float next = 0f)
    {
        ShowTimer(value, time, next);
    }

    /// <summary>
    /// The game modes of waves and survival have tables of their own that are not read here.
    /// </summary>
    [LuaMember]
    public void StartDefenceMode(int table)
    {
        NotDone(nameof(StartDefenceMode));
        Logger.IgnoredStartMode(nameof(StartDefenceMode), table);
    }

    [LuaMember]
    public void StartEventTimer(string function)
    {
        Timers.Start(function);
    }

    [LuaMember]
    public void StartOperationMode(int table)
    {
        NotDone(nameof(StartOperationMode));
        Logger.IgnoredStartMode(nameof(StartOperationMode), table);
    }

    [LuaMember]
    public void StartSurvivalMode(int table)
    {
        NotDone(nameof(StartSurvivalMode));
        Logger.IgnoredStartMode(nameof(StartSurvivalMode), table);
    }

    /// <summary>
    /// No more monsters come to a sector. The sector itself is the current one when the number is zero.
    /// </summary>
    [LuaMember]
    public void TerminateSpawn(int sector)
    {
        if (sector == 0)
        {
            sector = CurrentSector;
        }

        creatureManager.GetKillProgress(sector).Stopped = true;

        foreach (var box in Batch.EventBox.SpawnsOf(sector))
        {
            creatureManager.Terminate(box.Id);
        }

        Later(0f, () => CheckSectorAsync(sector));
    }

    /// <summary>
    /// A box that has not appeared yet never will. What has appeared stays.
    /// </summary>
    [LuaMember]
    public void TerminateSpawnBox(int box)
    {
        creatureManager.Terminate(box);

        if (Batch.EventBox.MonsterSpawns.Find(box) is { } spawn)
        {
            Later(0f, () => CheckSectorAsync(spawn.Sector));
        }
    }

    /// <summary>
    /// The sector the first player of the maze stands in.
    /// </summary>
    private int CurrentSector => _members.Count == 0
        ? 0
        : Batch.EventBox.Sectors.FirstOrDefault(e => Contains(e, Ecs.Get<WorldComponent>(_members[0].Player).Position))?.Id ?? 0;

    private static bool Contains(SectorBox box, Vector3 position)
    {
        return position.X >= Math.Min(box.PosTopLeft.X, box.PosBottomRight.X) &&
               position.X <= Math.Max(box.PosTopLeft.X, box.PosBottomRight.X) &&
               position.Y >= Math.Min(box.PosTopLeft.Y, box.PosBottomRight.Y) &&
               position.Y <= Math.Max(box.PosTopLeft.Y, box.PosBottomRight.Y);
    }

    private Entity[] MonstersOf(int sector)
    {
        return creatureManager.FindMonsters((_, _) => true).Where(e => Ecs.Get<NonPlayableCreatureComponent>(e).Sector == sector).ToArray();
    }

    /// <summary>
    /// Kills what is given: the client sees them die, and the sector counts them.
    /// </summary>
    /// <param name="scripts">Whether the script hears of each death.</param>
    private async ValueTask DestroyAsync(Entity[] targets, bool scripts)
    {
        var dead = targets.Where(Ecs.IsAlive).Select(RemoveDead).ToArray();

        foreach (var (identifier, place, _) in dead)
        {
            if (Closed)
            {
                return;
            }

            if (scripts)
            {
                await State.OnDie(identifier, place.SpawnBox, this);
            }
        }

        foreach (var sector in dead.Select(e => e.Place.Sector).Distinct())
        {
            if (Closed)
            {
                return;
            }

            await CheckSectorAsync(sector);
        }
    }

    /// <summary>
    /// Kills what a sector holds and finishes it, for a command that wants a sector done at once.
    /// </summary>
    [LuaIgnoreMember]
    public async ValueTask ClearSectorAsync(int sector)
    {
        if (sector == 0)
        {
            sector = CurrentSector;
        }

        await DestroyAsync(MonstersOf(sector), true);

        if (!Closed)
        {
            await CompleteSectorAsync(sector);
        }
    }

    /// <summary>
    /// A cutscene starts or is skipped on the client. The maze waits for the boss while one plays, and its script
    /// hears when it is over.
    /// </summary>
    [LuaIgnoreMember]
    public async ValueTask UpdateCutsceneAsync(Entity player, bool play, string name)
    {
        if (!Started || Find(player) is null)
        {
            return;
        }

        var session = player.Get<ServerSessionComponent>();

        if (play)
        {
            Cutscene = name;

            SetMazeState(MazeState.StartBoss);

            session.Send(new PersonPlayCutsceneResponse { State = CutscenePlaying });

            return;
        }

        session.Send(new PersonPlayCutsceneResponse { State = CutsceneStopped });

        if (Cutscene is null)
        {
            return;
        }

        Cutscene = null;

        SetMazeState(MazeState.WaitToDie);

        await State.OnCutsceneOff(name, this);
    }

    /// <summary>
    /// A townsman that works as a switch stands one way or the other, and a click turns it over.
    /// </summary>
    [LuaIgnoreMember]
    public void ClickSwitchNpc(int npc)
    {
        var status = !SwitchNpcs.GetValueOrDefault(npc);

        SwitchNpcs[npc] = status;

        creatureManager.Broadcast(new MazeSwitchNpcStatusResponse { Npc = npc, Status = status ? (byte)1 : (byte)0 });
    }

    private void ShowTimer(int value, float time, float next)
    {
        creatureManager.Broadcast(new MazeTimerResponse { Value = value, Time = time, Next = next });
    }
}
