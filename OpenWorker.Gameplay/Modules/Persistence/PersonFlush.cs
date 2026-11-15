using Arch.Core;
using OpenWorker.Hotspot;

namespace OpenWorker.Gameplay.Modules.Persistence;

/// <summary>
/// Writes what a player changed now, and returns when it is in the database: true, or false when the writer gave it up
/// after its retries (the parts it took are marked changed again by then). A server calls it before the client is sent to
/// another server (the reservation), so that the next server loads what this one had. A handler that takes gold or items
/// for something another store writes (a listing, a purchase, a letter) calls it between the take and that store and
/// refuses the request when it is false: a crash in between then loses what the player paid, and never leaves it both with
/// the player and in the store.
/// </summary>
public interface IPersonFlush
{
    ValueTask<bool> FlushAsync(Entity player);
}

/// <summary>
/// A server that keeps nothing of its players (the gate) has nothing to write.
/// </summary>
public sealed class NoPersonFlush : IPersonFlush
{
    public static NoPersonFlush Instance { get; } = new();

    public ValueTask<bool> FlushAsync(Entity player) => ValueTask.FromResult(true);
}

/// <summary>
/// The flush of one player: on a move to another server, before a store writes what the player paid for, and when its
/// connection closes (logout, a kick, the drain of a stopping server). With nothing changed it still waits for the writes
/// of the player that are queued already.
/// </summary>
public sealed class PersonFlush(World world, PersonSnapshots snapshots, PersistenceWriter writer) : IPersonFlush, ISessionCloseHandler
{
    public async ValueTask<bool> FlushAsync(Entity player)
    {
        // The snapshot and the queue in one go on the loop: nothing of the player changes in between.
        IReadOnlyList<PersonWrite> writes = snapshots.Take(player) is { } write ? [write] : [];

        return await writer.EnqueueAsync(writes);
    }

    /// <summary>
    /// The last flush of the player. What the database does not take in time is kept by the writer after the player is
    /// gone, and the session stays held until it is written or given up (<see cref="SessionHoldComponent"/>).
    /// </summary>
    public async ValueTask OnSessionClosedAsync(Entity player)
    {
        if (world.DirtyOf(player) is not { } dirty)
        {
            await FlushAsync(player);

            return;
        }

        IReadOnlyList<PersonWrite> writes = snapshots.Take(player) is { } write ? [write] : [];

        var kept = await writer.LeaveAsync(dirty.Person, writes);

        if (!kept.IsCompleted && world.IsAlive(player))
        {
            world.Add(player, new SessionHoldComponent(kept));
        }
    }
}
