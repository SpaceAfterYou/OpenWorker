using Arch.Core;
using Lua;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Lua;

/// <summary>
/// The operation mode, a trial that a boss sets the players: as its life falls past the shares the table names, one of the
/// objects the table lists is asked for. The player who clicks it in time gets an aura; when time runs out the boss gets one.
/// The trial is told to the players by lines of text; the mode ends when the boss is gone, and neither wins nor loses the maze.
/// </summary>
public partial class LuaMaze
{
    /// <summary>
    /// A line stays this long, in seconds, once the trial it told of is over.
    /// </summary>
    private const float TrialLineSeconds = 5f;

    /// <summary>
    /// The script starts the trial of the table of that number. A number the server has no row for starts nothing.
    /// </summary>
    [LuaIgnoreMember]
    internal void BeginOperation(int table)
    {
        if (Closed || creatureManager.Services?.Modes is not { } modes || !modes.TryGetOperation(table, out var row))
        {
            Logger.UnknownMode(nameof(BeginOperation), table);

            return;
        }

        var mode = new OperationMode(this, row);

        Begin(mode);
        mode.Start();
    }

    /// <summary>
    /// A player clicked an object of the maze: the trial hears it.
    /// </summary>
    [LuaIgnoreMember]
    public void ModeInteracted(Entity player, int box)
    {
        _mode?.Interact(player, box);
    }

    private sealed class OperationMode(LuaMaze maze, OperationModeRow row) : GameMode(maze)
    {
        private Entity? _boss;
        private int _state = -1;
        private bool _trial;
        private float _trialTime;
        private int _reminder;
        private int _object;
        private int _box;
        private float _lineLeft;

        public void Start()
        {
            foreach (var creature in Maze.Creatures.Living)
            {
                if (Maze.Ecs.IsAlive(creature) && Maze.Ecs.Get<ActorComponent>(creature).Type == ActorType.Monster
                    && Maze.Ecs.Get<CreatureComponent>(creature).Identifier == row.Boss)
                {
                    _boss = creature;

                    break;
                }
            }
        }

        /// <summary>
        /// The trial starts at the first share of life the boss has gone under that it has not been asked for yet.
        /// </summary>
        private void WatchBoss()
        {
            if (_trial || _boss is not { } boss || !Maze.Ecs.IsAlive(boss))
            {
                return;
            }

            var health = Maze.Ecs.Get<CreatureComponent>(boss).Health;
            var most = (long)Maze.Ecs.Get<MonsterCombatComponent>(boss).Stats.MaxHealth;

            if (most <= 0)
            {
                return;
            }

            var share = 100L * health / most;
            var reached = -1;

            for (var index = 0; index < OperationModeRow.Conditions; index++)
            {
                if (index >= _state && share < row.LifeConditions[index])
                {
                    reached = index;
                }
            }

            if (reached != -1)
            {
                _state = reached + 1;

                StartTrial();
            }
        }

        /// <summary>
        /// One of the objects of the table is asked for: the clients are told, and the object can be used.
        /// </summary>
        private void StartTrial()
        {
            _trialTime = 0f;
            _reminder = 0;
            _trial = true;

            var count = row.Objects.Count(e => e != 0);

            _object = count == 0 ? 0 : Maze.Creatures.Random.Next(count);

            for (var index = 0; index < row.Objects.Length; index++)
            {
                if (row.Objects[index] != 0 && index == _object)
                {
                    _box = (int)row.Objects[index];

                    Maze.EnableInteractionBox(_box, true);

                    break;
                }
            }

            Maze.Notice(ModeNotice.Trial, (int)row.StartText, row.Seconds);
        }

        public override void Tick(float seconds)
        {
            if (_lineLeft > 0f)
            {
                _lineLeft -= seconds;

                if (_lineLeft < 0f)
                {
                    _lineLeft = 0f;

                    Maze.Notice(ModeNotice.Trial, 0, -1f);
                }
            }

            WatchBoss();

            // The reminders stop the clock of the trial after the third: the original goes on that way, and a trial whose reminders
            // come after its time is never failed.
            if (!_trial || _reminder > OperationModeRow.Reminders - 1)
            {
                return;
            }

            _trialTime += seconds;

            if (_trialTime > row.Seconds)
            {
                Fail();
            }
            else if (_reminder < OperationModeRow.Reminders && _trialTime > row.ReminderSeconds[_reminder])
            {
                _lineLeft = 0f;

                Maze.Notice(ModeNotice.Trial, (int)row.ReminderTexts[_reminder]);

                _reminder++;
            }
        }

        /// <summary>
        /// The player clicked an object. It is the one asked for: the player gets the aura, and the trial is over.
        /// </summary>
        public override void Interact(Entity player, int box)
        {
            if (_object >= OperationModeRow.MaxObjects || box != row.Objects[_object])
            {
                return;
            }

            if (Maze.Ecs.IsAlive(player))
            {
                Maze.Buffs.SetAura(player, (int)row.SuccessAura);
            }

            Maze.Notice(ModeNotice.Trial, (int)row.SuccessText, -1f);

            _lineLeft = TrialLineSeconds;

            EndTrial();
        }

        /// <summary>
        /// Time ran out: the boss gets the aura, the object cannot be used, and the players read that they failed.
        /// </summary>
        private void Fail()
        {
            if (_boss is { } boss && Maze.Ecs.IsAlive(boss))
            {
                Maze.Buffs.SetAura(boss, (int)row.FailAura);

                if (_box != 0)
                {
                    Maze.EnableInteractionBox(_box, false);
                }

                Maze.Notice(ModeNotice.Trial, (int)row.FailText, -1f);

                _lineLeft = TrialLineSeconds;

                EndTrial();
            }
        }

        private void EndTrial()
        {
            _trial = false;
            _box = 0;
        }

        public override ValueTask<bool> CheckEndAsync()
        {
            if (_boss is { } boss && Maze.Ecs.IsAlive(boss) && Maze.Ecs.Get<CreatureComponent>(boss).Health > 0)
            {
                return ValueTask.FromResult(false);
            }

            if (_box != 0)
            {
                Maze.EnableInteractionBox(_box, false);
            }

            Maze.Notice(ModeNotice.Trial, 0, -1f);

            return ValueTask.FromResult(true);
        }
    }
}
