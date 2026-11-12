namespace OpenWorker.Domain.Persistent;

public enum RankingKind : byte
{
    /// <summary>The fastest clear of a map.</summary>
    TimeAttack = 1,

    /// <summary>Kills in the arena, for a week or for a season.</summary>
    Pvp = 2,

    /// <summary>The fastest clear of a stage of the infinite tower; the scope is the maze of the stage.</summary>
    InfiniteTower = 3
}

/// <summary>
/// What a person did best in a ranking. A person is in a ranking once.
/// </summary>
public sealed class RankingEntryPersistent : BasicPersistent
{
    public int Id { get; init; }

    public required RankingKind Kind { get; init; }

    /// <summary>
    /// The map of a time attack, the kind of period of the arena (1 for a week, 2 for a season), the maze of a stage of the
    /// tower.
    /// </summary>
    public required int Scope { get; init; }

    public required int PersonId { get; init; }

    /// <summary>
    /// How long the clear took, in seconds.
    /// </summary>
    public int Time { get; set; }

    public int Score { get; set; }
}
