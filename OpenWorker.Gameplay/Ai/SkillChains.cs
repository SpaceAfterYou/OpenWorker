using System.Collections.ObjectModel;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Ai;

/// <summary>
/// The combos of the monsters: the skill a monster may go on with after one, and the chance it does, from the table of the server.
/// Without the table no monster makes a combo.
/// </summary>
public sealed class SkillChains(ReadOnlyCollection<SkillAggroRow> rows)
{
    private const int Percent = 100;

    public static SkillChains None { get; } = new(new ReadOnlyCollection<SkillAggroRow>([]));

    private readonly RowIndex<uint, SkillAggroRow> _rows = rows.IndexBy(e => e.Id);

    /// <summary>
    /// Rolls whether a monster that starts the skill goes on with the next one of its combo. Zero when it does not.
    /// </summary>
    public int Roll(int skill, Random random)
    {
        if (skill <= 0 || !_rows.TryGet((uint)skill, out var row) || row.ChainSkill is 0 or > int.MaxValue)
        {
            return 0;
        }

        return random.Next(Percent) < row.ChainRate ? (int)row.ChainSkill : 0;
    }
}
