namespace OpenWorker.Domain.Persistent;

/// <summary>
/// A person in a league. A person is in one league at most.
/// </summary>
public sealed class LeagueMemberPersistent : BasicPersistent
{
    public required int PersonId { get; init; }

    public required int LeagueId { get; set; }

    public byte Position { get; set; }

    public long Exp { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A person who asked to join a league and waits for an answer.
/// </summary>
public sealed class LeagueApplicantPersistent : BasicPersistent
{
    public int Id { get; init; }

    public required int LeagueId { get; init; }

    public required int PersonId { get; init; }
}

/// <summary>
/// A note on the board of a league.
/// </summary>
public sealed class LeagueBoardPersistent : BasicPersistent
{
    public int Id { get; init; }

    public required int LeagueId { get; init; }

    public required int PersonId { get; init; }

    public required string Message { get; init; }
}
