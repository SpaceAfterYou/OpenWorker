using OpenWorker.Domain.Persistent;

namespace OpenWorker.Domain.Stores;

/// <summary>
/// The rankings the client's ranking window shows, as the game records results in them.
/// </summary>
public interface IRankingStore
{
    /// <summary>
    /// Records a result of the person, keeping the better of it and the one it had: a faster clear in a time attack (with the
    /// same time, a higher score), a higher score in the arena.
    /// </summary>
    Task SubmitAsync(RankingKind kind, int scope, int person, int time, int score, CancellationToken cancellationToken = default);
}

/// <summary>
/// The fastest clear of a stage of the infinite tower by a person, with what the ranking window shows of the person.
/// </summary>
/// <param name="Maze">The maze of the stage.</param>
/// <param name="Time">The clear in seconds.</param>
/// <param name="Order">Which of two equal times came first: the lower one.</param>
public sealed record TowerClear(int Person, string Name, byte Hero, byte Level, int Maze, int Time, int Order);
