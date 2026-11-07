using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Persistence;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// Gives out the serials of new items.
/// </summary>
public interface IItemSerials
{
    long Next();
}

/// <summary>
/// Serials from a counter in memory, for tests and for a server without a database.
/// </summary>
public sealed class LocalItemSerials : IItemSerials
{
    private long _next = 1000;

    public long Next()
    {
        return Interlocked.Increment(ref _next);
    }
}

/// <summary>
/// Serials taken from the database in blocks. An item is created in the middle of a packet, so the
/// next block is fetched ahead of time and the game never waits for the database for one.
/// </summary>
public sealed class DatabaseItemSerials(IDbContextFactory<PersistenceContext> factory) : IItemSerials
{
    private const int Block = 256;
    private const int Reserve = 64;

    private readonly ConcurrentQueue<long> _ready = new();
    private int _fetching;

    public long Next()
    {
        if (_ready.Count < Reserve)
        {
            Fetch();
        }

        long serial;

        while (!_ready.TryDequeue(out serial))
        {
            // The block was used up before the next one arrived, wait for it. That takes one query.
            Task.Run(() => FetchAsync()).GetAwaiter().GetResult();

            if (_ready.TryDequeue(out serial))
            {
                return serial;
            }
        }

        return serial;
    }

    /// <summary>
    /// Fills the supply before the first item is made.
    /// </summary>
    public Task PrimeAsync()
    {
        return FetchAsync();
    }

    private void Fetch()
    {
        if (Interlocked.CompareExchange(ref _fetching, 1, 0) != 0)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await FetchAsync();
            }
            catch
            {
                // The next call asks again.
            }
            finally
            {
                Interlocked.Exchange(ref _fetching, 0);
            }
        });
    }

    private async Task FetchAsync()
    {
        foreach (var serial in await factory.ReserveSerialsAsync(Block))
        {
            _ready.Enqueue(serial);
        }
    }
}
