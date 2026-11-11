using Arch.Core;
using Lua;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Hotspot.Modules.Quests.Enums;

namespace OpenWorker.Lua;

/// <summary>
/// The game modes a script starts (<c>StartSurvivalMode</c>, <c>StartDefenceMode</c>, <c>StartOperationMode</c>). A mode is a
/// rule about the maze that runs beside its script: it counts time with the maze, looks at how things stand once a second and ends
/// when they decide it. The maze holds one at a time; a mode started while another runs replaces it, as in the original.
/// </summary>
public partial class LuaMaze
{
    /// <summary>
    /// The notices the client knows the modes by (<see cref="MazeNoticeResponse"/>).
    /// </summary>
    internal static class ModeNotice
    {
        public const int DefenceStarted = 1;
        public const int DefenceEnded = 2;
        public const int SurvivalStarted = 11;
        public const int SurvivalEnded = 12;

        /// <summary>
        /// A line for the players to read: the value is the text, the time how long it stays (a negative time takes it away).
        /// </summary>
        public const int Trial = 61;
    }

    /// <summary>
    /// How often, in seconds, a mode looks whether it is over.
    /// </summary>
    private const float ModeCheckSeconds = 1f;

    private GameMode? _mode;
    private float _modeClock;
    private bool _modeChecking;
    private SpawnWaves? _waves;

    /// <summary>
    /// The boxes of the maze that send their monsters in waves once told to.
    /// </summary>
    private SpawnWaves Waves => _waves ??= new SpawnWaves(creatureManager, Batch);

    /// <summary>
    /// Whether a mode is being played.
    /// </summary>
    [LuaIgnoreMember]
    public bool HasMode => _mode is not null;

    /// <summary>
    /// What a mode is: time it counts, and a look at the maze that says whether it is over.
    /// </summary>
    private abstract class GameMode(LuaMaze maze)
    {
        protected LuaMaze Maze { get; } = maze;

        /// <summary>
        /// How the mode ended: 1 won, anything else lost.
        /// </summary>
        public byte Result { get; protected set; }

        /// <summary>
        /// The notice the clients get when the mode ends with its result as the value, or zero for none.
        /// </summary>
        public virtual int EndNotice => 0;

        /// <summary>
        /// Time passes in the maze.
        /// </summary>
        public virtual void Tick(float seconds)
        {
        }

        /// <summary>
        /// A player clicked an object of the maze.
        /// </summary>
        public virtual void Interact(Entity player, int box)
        {
        }

        /// <summary>
        /// Looks at the maze; true when the mode has ended (and has done everything its end asks for).
        /// </summary>
        public abstract ValueTask<bool> CheckEndAsync();
    }

    /// <summary>
    /// Time passes: the waves that wait for it come, the mode counts it, and once a second the mode is asked whether it is over.
    /// </summary>
    private void StepMode(float seconds)
    {
        _waves?.Step(seconds);

        if (_mode is not { } mode)
        {
            return;
        }

        mode.Tick(seconds);

        _modeClock += seconds;

        if (_modeClock <= ModeCheckSeconds || _modeChecking)
        {
            return;
        }

        _modeClock = 0f;
        _modeChecking = true;

        Run(CheckModeAsync(mode));
    }

    private async ValueTask CheckModeAsync(GameMode mode)
    {
        try
        {
            if (Closed || !ReferenceEquals(_mode, mode) || !await mode.CheckEndAsync() || Closed)
            {
                return;
            }

            if (ReferenceEquals(_mode, mode))
            {
                _mode = null;
            }

            if (mode.EndNotice != 0)
            {
                Notice(mode.EndNotice, mode.Result);
            }
        }

        finally
        {
            _modeChecking = false;
        }
    }

    /// <summary>
    /// Starts a mode in place of the one that runs, if any.
    /// </summary>
    private void Begin(GameMode mode)
    {
        _mode = mode;
        _modeClock = 0f;
    }

    private void Notice(int type, int value, float time = 0f)
    {
        creatureManager.Broadcast(new MazeNoticeResponse { Type = type, Value = value, Time = time });
    }

    /// <summary>
    /// The mode was won while the players stood: the quests that ask for it count for each of them on their feet, and for the others
    /// of the maze when one of those counts for the group.
    /// </summary>
    private async ValueTask ReportModeWonAsync(QuestConditionType type)
    {
        var fallen = new List<Entity>();
        var shared = false;

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

            if (creatureManager.Vitals.IsAlive(player))
            {
                shared |= await reports.ModeWonAsync(player, type, info.Id);
            }
            else
            {
                fallen.Add(player);
            }
        }

        if (!shared)
        {
            return;
        }

        foreach (var player in fallen)
        {
            if (Closed)
            {
                return;
            }

            if (Ecs.IsAlive(player))
            {
                await reports.ModeWonAsync(player, type, info.Id);
            }
        }
    }

    /// <summary>
    /// Whether any player of the maze is on their feet.
    /// </summary>
    private bool AnyoneStands()
    {
        foreach (var member in _members)
        {
            if (Ecs.IsAlive(member.Player) && creatureManager.Vitals.IsAlive(member.Player))
            {
                return true;
            }
        }

        return false;
    }
}
