using System.Collections.ObjectModel;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Drops;

/// <summary>
/// What one member did in a run of a maze, as the score counts it.
/// </summary>
/// <param name="Maze">The maze.</param>
/// <param name="Hero">The hero of the member: some heroes count their combo higher.</param>
/// <param name="Level">The level of the member.</param>
/// <param name="Combo">The longest combo.</param>
/// <param name="Kills">The monsters the member killed.</param>
/// <param name="DamageShare">The share of the damage of all the members that the member did, in percent.</param>
/// <param name="HitsTaken">How many times the member was hit.</param>
/// <param name="PartySize">How many members cleared the maze together, one alone.</param>
/// <param name="Milliseconds">How long the run took.</param>
public readonly record struct MazeRun(int Maze, int Hero, int Level, int Combo, int Kills, int DamageShare, int HitsTaken, int PartySize, long Milliseconds);

/// <summary>
/// The score of a run and what its rank gives.
/// </summary>
/// <param name="PlayPoint">The score.</param>
/// <param name="ClearPoint">The part of the score the clear itself is worth.</param>
/// <param name="Rank">From 0 (F) to 5 (SS).</param>
/// <param name="ExperienceShare">The share of the experience of the maze the rank gives.</param>
/// <param name="GoldShare">The share of the gold of the maze the rank gives.</param>
public readonly record struct MazeScore(float PlayPoint, int ClearPoint, byte Rank, float ExperienceShare, float GoldShare);

/// <summary>
/// The score of a run of a maze and its rank, from the reward tables of the server.
/// <list type="number">
/// <item>Points: the clear, the size of the party, the longest combo (times the factor of the hero, at most 500), whether
/// enough monsters were killed, the share of the damage; all of it kept at the percent the hits taken leave.</item>
/// <item>Times one plus the value of each step of the time left of the clear time of the maze (one step when it ran over).</item>
/// <item>Times the factor of the difficulty of the maze, then of how far the level of the member is above the level of the
/// maze.</item>
/// <item>The rank is the first step whose points the score does not pass, SS above them all.</item>
/// </list>
/// A maze without a row of points has no score: rank F, and the whole of its experience and gold, as the reference leaves
/// it. A factor whose row is missing counts as one.
/// </summary>
public sealed class MazeScoring(
    ReadOnlyCollection<MazeRewardStandardRow> standards,
    ReadOnlyCollection<MazeRankShareRow> ranks,
    ReadOnlyCollection<MazeTimeScoreRow> times,
    ReadOnlyCollection<MazeDifficultyScoreRow> difficulties,
    ReadOnlyCollection<MazeLevelScoreRow> levels,
    ReadOnlyCollection<MazePlayScoreRow> normals,
    ReadOnlyCollection<MazePartyShareRow> partyValues,
    ReadOnlyCollection<MazeComboRevisionRow> revisions,
    ReadOnlyCollection<MazeRuleRow> rules,
    ReadOnlyCollection<MazeInfoRow> mazes)
{
    public const byte LowestRank = 0;
    public const byte HighestRank = 5;

    /// <summary>The longest combo that still adds points.</summary>
    public const int MaxCombo = 500;

    private readonly RowIndex<short, MazeRewardStandardRow> _standards = standards.IndexBy(e => e.Maze);
    private readonly RowIndex<byte, MazeRankShareRow> _ranks = ranks.IndexBy(e => e.Id);
    private readonly MazeTimeScoreRow? _time = times.Count > 0 ? times[0] : null;
    private readonly RowIndex<byte, MazeDifficultyScoreRow> _difficulties = difficulties.IndexBy(e => e.Id);
    private readonly RowIndex<byte, MazeLevelScoreRow> _levels = levels.IndexBy(e => e.Id);
    private readonly RowIndex<ushort, MazePlayScoreRow> _normals = normals.IndexBy(e => e.Id);
    private readonly RowIndex<byte, MazePartyShareRow> _partyValues = partyValues.IndexBy(e => e.Id);
    private readonly RowIndex<ushort, MazeComboRevisionRow> _revisions = revisions.IndexBy(e => e.Hero);
    private readonly RowIndex<ushort, MazeRuleRow> _rules = rules.IndexBy(e => e.Id);
    private readonly RowIndex<short, MazeInfoRow> _mazes = mazes.IndexBy(e => e.Id);

    /// <summary>
    /// The server's row of the maze: what a clear is worth, the time it is measured against, what entering asks for.
    /// </summary>
    public bool TryGetRule(int maze, out MazeRuleRow rule)
    {
        return _rules.TryGet((ushort)Math.Clamp(maze, 0, ushort.MaxValue), out rule);
    }

    public MazeScore Score(in MazeRun run)
    {
        if (run.Maze is <= 0 or > short.MaxValue || !_standards.TryGet((short)run.Maze, out var standard))
        {
            return new MazeScore(0f, 0, LowestRank, 1f, 1f);
        }

        var hasRule = TryGetRule(run.Maze, out var rule);
        var points = (float)standard.ClearPoint;

        points += standard.PartyPoints[Math.Clamp(run.PartySize, 1, MazeRewardStandardRow.PartySizes) - 1];

        var combo = Math.Max(0, run.Combo);

        if (run.Hero is > 0 and <= ushort.MaxValue && _revisions.TryGet((ushort)run.Hero, out var revision))
        {
            combo = (int)(combo * revision.Value);
        }

        if (Normal(Math.Min(combo, MaxCombo) + 1) is { } comboRow)
        {
            points += comboRow.Combo;
        }

        points += run.Kills >= standard.Kills ? standard.KillsReached : standard.KillsMissed;

        if (Normal(run.DamageShare) is { } damageRow)
        {
            points += damageRow.Damage;
        }

        if (Normal(run.HitsTaken) is { } hitsRow)
        {
            points = points * hitsRow.HitsTakenRate / 100f;
        }

        if (hasRule && _time is { Step: > 0 } time)
        {
            // The whole seconds left of the clear time; a run that took longer counts as one second left.
            var left = (int)((rule.ClearTime * 1000L - run.Milliseconds) / 1000);
            var steps = Math.Min(Math.Max(left, 1) / time.Step, time.MaxSteps);

            if (steps > 0)
            {
                points *= steps * time.Value + 1f;
            }
        }

        var difficulty = hasRule ? rule.Difficulty : _mazes.TryGet((short)run.Maze, out var info) ? info.Difficulty : (byte)0;

        if (_difficulties.TryGet((byte)Math.Min(difficulty + 1, byte.MaxValue), out var difficultyRow))
        {
            points *= difficultyRow.Value;
        }

        var mazeLevel = hasRule ? rule.RequiredLevel : _mazes.TryGet((short)run.Maze, out var row) ? row.RequiredLevel : (byte)0;
        var gap = Math.Clamp(run.Level - mazeLevel, 0, byte.MaxValue);

        if (_levels.TryGet((byte)gap, out var levelRow))
        {
            points *= levelRow.Value;
        }

        var rank = HighestRank;

        for (var step = 0; step < MazeRewardStandardRow.RankSteps; step++)
        {
            if (standard.RankPoints[step] >= points)
            {
                rank = (byte)step;
                break;
            }
        }

        // A rank without its row leaves the run as the reference leaves it: F, with the whole of the maze.
        return _ranks.TryGet(rank, out var rankRow)
            ? new MazeScore(points, standard.ClearPoint, rank, rankRow.Experience, rankRow.Gold)
            : new MazeScore(points, standard.ClearPoint, LowestRank, 1f, 1f);
    }

    /// <summary>
    /// What a member of a party gets on top of its rank, by the size of the party and its place in the order of the scores
    /// (from 1). Nothing for a player alone or a size the table does not have.
    /// </summary>
    public (float Experience, float Gold) PartyShare(int members, int place)
    {
        if (members < 2 || place < 1 || !_partyValues.TryGet((byte)Math.Min(members, MazePartyShareRow.Places), out var row))
        {
            return (0f, 0f);
        }

        var index = Math.Min(place, MazePartyShareRow.Places) - 1;

        return (row.Experience[index], row.Gold[index]);
    }

    private MazePlayScoreRow? Normal(int count)
    {
        return count is > 0 and <= ushort.MaxValue && _normals.TryGet((ushort)count, out var row) ? row : null;
    }
}
