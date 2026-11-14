namespace OpenWorker.Domain.Persistent;

/// <summary>
/// A period of a ranking that has ended: its places were given their rewards and its list was cleared. A ranking ends a
/// period once.
/// </summary>
public sealed class RankingPeriodPersistent : BasicPersistent
{
    public int Id { get; init; }

    public required RankingKind Kind { get; init; }

    /// <summary>
    /// The map of a time attack.
    /// </summary>
    public required int Scope { get; init; }

    /// <summary>
    /// When the period ended, in UTC: the results recorded before it belong to it.
    /// </summary>
    public required DateTime EndedAt { get; init; }

    /// <summary>
    /// How many letters with rewards it sent.
    /// </summary>
    public int Letters { get; init; }
}
