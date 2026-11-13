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
}

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
