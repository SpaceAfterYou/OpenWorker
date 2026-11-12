using Arch.Core;

namespace OpenWorker.Gameplay.Modules.Persistence;

/// <summary>
/// Writes what a player changed now and says whether the database took it. A handler that takes gold or items from a
/// player for something another store writes (a listing, a purchase, a letter) calls it between the take and that store,
/// and refuses the request when it is false: a crash in between then loses what the player paid, and never leaves it both
/// with the player and in the store.
/// </summary>
public interface IPersonCommit
{
    ValueTask<bool> CommitAsync(Entity player);
}

/// <summary>
/// The write of one player through the write-behind, awaited. False when the writer gave it up after its retries: the parts
/// it took are marked changed again by then, so what the handler gives back to the player is written by a later flush.
/// </summary>
public sealed class PersonCommit(PersonSnapshots snapshots, PersistenceWriter writer) : IPersonCommit
{
    public async ValueTask<bool> CommitAsync(Entity player)
    {
        // The snapshot and the queue in one go on the loop: nothing of the player changes in between.
        IReadOnlyList<PersonWrite> writes = snapshots.Take(player) is { } write ? [write] : [];

        return await writer.EnqueueAsync(writes);
    }
}
