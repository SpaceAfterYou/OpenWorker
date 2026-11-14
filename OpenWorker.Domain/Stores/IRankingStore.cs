using OpenWorker.Domain.Persistent;

namespace OpenWorker.Domain.Stores;

/// <summary>
/// The rankings the client's ranking window shows, as the game records results in them.
/// </summary>
public interface IRankingStore
{
    /// <summary>
    /// Records a result of the person, keeping the better of it and the one it had: a faster clear in a time attack (with the
    /// same time, a higher score), a higher score in the arena. True when the result was written: the first one, or a better one.
    /// </summary>
    Task<bool> SubmitAsync(RankingKind kind, int scope, int person, int time, int score, CancellationToken cancellationToken = default);

    /// <summary>
    /// The best of a ranking, in order, and the place of the person; a person who is not in it has no row.
    /// </summary>
    Task<(IReadOnlyList<RankingRow> Top, RankingRow? Mine)> RankingAsync(RankingKind kind, int scope, int person, CancellationToken cancellationToken = default);

    /// <summary>
    /// The first places of the period of a ranking that ends at <paramref name="endedAt"/> (UTC), in order: the results recorded
    /// before it. Null when that period has ended already.
    /// </summary>
    Task<IReadOnlyList<RankingPlace>?> PeriodPlacesAsync(RankingKind kind, int scope, DateTime endedAt, int places, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends the period at <paramref name="endedAt"/> (UTC) at once: writes the letters with its rewards to the accounts and takes
    /// the results recorded before it off the list. The letters as they were stored; null, and nothing written, when the
    /// period has ended already.
    /// </summary>
    Task<IReadOnlyList<PostPersistent>?> EndPeriodAsync(RankingKind kind, int scope, DateTime endedAt, IReadOnlyList<PostAccountDraft> letters, CancellationToken cancellationToken = default);
}

/// <summary>
/// A place of a ranking at the end of its period, with the account its reward goes to.
/// </summary>
/// <param name="Place">The place, from 1.</param>
public sealed record RankingPlace(int Place, int Person, int Account);

/// <summary>
/// A row of a ranking with what the window shows of its person.
/// </summary>
/// <param name="Order">The place, from 1.</param>
/// <param name="Level">The level of the person as its row keeps it.</param>
public sealed record RankingRow(int Order, int Person, string Name, byte Hero, int Time, int Score, byte Level = 1);

/// <summary>
/// The fastest clear of a stage of the infinite tower by a person, with what the ranking window shows of the person.
/// </summary>
/// <param name="Maze">The maze of the stage.</param>
/// <param name="Time">The clear in seconds.</param>
/// <param name="Order">Which of two equal times came first: the lower one.</param>
public sealed record TowerClear(int Person, string Name, byte Hero, byte Level, int Maze, int Time, int Order);
