using System.Collections.ObjectModel;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Channel.Community;

/// <summary>
/// The levels of a league from <c>tb_League_Info</c>, read the way the client reads them: a row per level, the experience
/// that row asks to reach it from the level below, and the members a league of that level holds. The experience of a league
/// counts inside its level, so the client shows it against the row of the next level.
/// </summary>
public sealed class LeagueLevels(ReadOnlyCollection<LeagueInfoRow> rows)
{
    /// <summary>
    /// The members of a league of the first level, for a stand without the table.
    /// </summary>
    public const int FirstMemberLimit = 20;

    private readonly RowIndex<int, LeagueInfoRow> _levels = rows.IndexBy(e => e.Level);

    /// <summary>
    /// How many members a league of the level holds.
    /// </summary>
    public int MemberLimit(byte level)
    {
        return _levels.GetOrDefault(level).MemberLimit is var limit and > 0 ? limit : FirstMemberLimit;
    }

    /// <summary>
    /// The experience a league of the level needs to reach the next one, null at the last level.
    /// </summary>
    public long? Next(byte level)
    {
        return _levels.TryGet(level + 1, out var row) && row.Exp > 0 ? row.Exp : null;
    }
}
