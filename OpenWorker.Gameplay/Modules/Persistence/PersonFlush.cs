using Arch.Core;
using OpenWorker.Hotspot;

namespace OpenWorker.Gameplay.Modules.Persistence;

/// <summary>
/// Writes what a player changed now, and returns when it is in the database. A server calls it before the client is
/// sent to another server (the reservation), so that the next server loads what this one had.
/// </summary>
public interface IPersonFlush
{
    ValueTask FlushAsync(Entity player);
}

/// <summary>
/// A server that keeps nothing of its players (the gate) has nothing to write.
/// </summary>
public sealed class NoPersonFlush : IPersonFlush
{
    public static NoPersonFlush Instance { get; } = new();

    public ValueTask FlushAsync(Entity player) => ValueTask.CompletedTask;
}

/// <summary>
/// The flush of one player: on a move to another server and when its connection closes (logout, a kick, the drain of a
/// stopping server). With nothing changed it still waits for the writes of the player that are queued already.
/// </summary>
public sealed class PersonFlush(World world, PersonSnapshots snapshots, PersistenceWriter writer) : IPersonFlush, ISessionCloseHandler
{
    public async ValueTask FlushAsync(Entity player)
    {
        // The snapshot and the queue in one go on the loop: nothing of the player changes in between.
        IReadOnlyList<PersonWrite> writes = snapshots.Take(player) is { } write ? [write] : [];

        await writer.EnqueueAsync(writes);
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
