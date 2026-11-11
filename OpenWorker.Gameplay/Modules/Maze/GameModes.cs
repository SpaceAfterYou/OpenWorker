using System.Collections.ObjectModel;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Maze;

/// <summary>
/// The game modes a maze script can start: the survival of waves, the defence of a monster and the trial of a boss. Each is a
/// row of a table of the server, found by the number the script gives. A server without the tables starts no mode.
/// </summary>
public sealed class GameModes(
    ReadOnlyCollection<SurvivalModeRow> survival,
    ReadOnlyCollection<DefenceModeRow> defence,
    ReadOnlyCollection<OperationModeRow> operation)
{
    private readonly RowIndex<byte, SurvivalModeRow> _survival = survival.IndexBy(e => e.Id);
    private readonly RowIndex<byte, DefenceModeRow> _defence = defence.IndexBy(e => e.Id);
    private readonly RowIndex<byte, OperationModeRow> _operation = operation.IndexBy(e => e.Id);

    /// <summary>
    /// The mode a script names. A number that is no byte, or that the table lacks, starts nothing.
    /// </summary>
    public bool TryGetSurvival(int table, out SurvivalModeRow row)
    {
        row = default;

        return table is >= byte.MinValue and <= byte.MaxValue && _survival.TryGet((byte)table, out row);
    }

    public bool TryGetDefence(int table, out DefenceModeRow row)
    {
        row = default;

        return table is >= byte.MinValue and <= byte.MaxValue && _defence.TryGet((byte)table, out row);
    }

    public bool TryGetOperation(int table, out OperationModeRow row)
    {
        row = default;

        return table is >= byte.MinValue and <= byte.MaxValue && _operation.TryGet((byte)table, out row);
    }
}
