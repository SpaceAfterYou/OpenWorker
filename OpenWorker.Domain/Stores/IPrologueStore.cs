namespace OpenWorker.Domain.Stores;

/// <summary>
/// Whether a character still has the prologue ahead (<see cref="Persistent.PersonPersistent.ProloguePending"/>). The gate
/// asks it when the character is selected, and the maze clears it when the character leaves the tutorial.
/// </summary>
public interface IPrologueStore
{
    /// <summary>
    /// True when the character of the account on the gate has not played the prologue; false for one that has, or for
    /// a character the account does not own there.
    /// </summary>
    Task<bool> IsPendingAsync(int person, int account, short gate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the prologue of the character as played. True only for the call that changed it: two servers that finish it
    /// at once write it once.
    /// </summary>
    Task<bool> FinishAsync(int person, CancellationToken cancellationToken = default);
}
