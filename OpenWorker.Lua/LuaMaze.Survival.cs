using Lua;
using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Lua;

/// <summary>
/// The survival mode: the spawn boxes of a table send their monsters in waves, and when the last wave has come and the sector holds
/// no monster, the mode is won. There is no way to lose it: the players that fall are revived or leave, and the maze waits.
/// </summary>
public partial class LuaMaze
{
    /// <summary>
    /// The script starts the survival of the table of that number. A number the server has no row for starts nothing.
    /// </summary>
    [LuaIgnoreMember]
    internal void BeginSurvival(int table)
    {
        if (Closed || creatureManager.Services?.Modes is not { } modes || !modes.TryGetSurvival(table, out var row))
        {
            Logger.UnknownMode(nameof(BeginSurvival), table);

            return;
        }

        var mode = new SurvivalMode(this, row);

        Begin(mode);
        mode.Start();

        Notice(ModeNotice.SurvivalStarted, 0);
    }

    private sealed class SurvivalMode(LuaMaze maze, SurvivalModeRow row) : GameMode(maze)
    {
        private readonly int _sector = (int)row.Sector;
        private float _passed;
        private float _lastWave;

        public override int EndNotice => ModeNotice.SurvivalEnded;

        /// <summary>
        /// Starts the boxes of the table; the mode is over only once the last of their waves has come.
        /// </summary>
        public void Start()
        {
            foreach (var box in row.SpawnBoxes)
            {
                if (box == 0)
                {
                    break;
                }

                var duration = Maze.Waves.Duration(box);

                if (duration <= 0f)
                {
                    continue;
                }

                Maze.Waves.Start(box, false);

                _lastWave = Math.Max(_lastWave, duration);
            }
        }

        public override void Tick(float seconds)
        {
            _passed += seconds;
        }

        public override async ValueTask<bool> CheckEndAsync()
        {
            if (_passed < _lastWave || Maze.Creatures.CountMonsters(_sector) > 0)
            {
                return false;
            }

            Result = 1;

            await Maze.CompleteSectorAsync(_sector);

            if (Maze.Closed)
            {
                return true;
            }

            await Maze.State.OnModeEnded("OnSurvivalModeSuccess", _sector, Maze);

            if (!Maze.Closed)
            {
                await Maze.ReportModeWonAsync(QuestConditionType.Survival);
            }

            return true;
        }
    }
}
