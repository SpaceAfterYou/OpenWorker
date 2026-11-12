using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Domain.Stores;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// Gives out the serials of new items. It never waits for the database on the world loop: a supply that has run dry refuses
/// instead, and the caller refuses what it was about to make.
/// </summary>
public interface IItemSerials
{
    /// <summary>
    /// The serial of a new item. Never waits: when no serial is ready it throws <see cref="ItemSerialsExhaustedException"/>.
    /// A caller that can refuse cleanly asks <see cref="HasReady"/> or <see cref="TryNextSerial"/> before it changes anything.
    /// </summary>
    long NextSerial();

    /// <summary>
    /// The serial of a new item, or <see langword="false"/> when none is ready. Never waits.
    /// </summary>
    bool TryNextSerial(out long serial);

    /// <summary>
    /// Whether that many serials are ready now, so that many items can be made without a refusal. Never waits.
    /// </summary>
    bool HasReady(int count);

    /// <summary>
    /// Waits until that many serials are ready. A caller that is about to make several items at once, and can wait,
    /// asks here first: <see cref="NextSerial"/> then finds them all. It never throws: if the database does not answer it
    /// returns with fewer ready, and the caller's own check refuses.
    /// </summary>
    ValueTask EnsureAsync(int count);
}

/// <summary>
/// No serial was ready for a new item: the database has not answered for the supply in time. Nothing waits for it; the
/// request that wanted the item fails.
/// </summary>
public sealed class ItemSerialsExhaustedException()
    : InvalidOperationException("No serial is ready for a new item: the supply ran dry before the database answered.");

/// <summary>
/// Serials from a counter in memory, for tests and for a server without a database.
/// </summary>
public sealed class LocalItemSerials : IItemSerials
{
    private long _next = 1000;

    public long NextSerial()
    {
        return Interlocked.Increment(ref _next);
    }

    public bool TryNextSerial(out long serial)
    {
        serial = NextSerial();

        return true;
    }

    public bool HasReady(int count)
    {
        return true;
    }

    public ValueTask EnsureAsync(int count)
    {
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Serials taken from the database in blocks. An item is made in the middle of a packet or of a simulation step, so the
/// next block is fetched in the background ahead of time, and the game never waits for the database for one: a supply
/// that runs dry anyway (the database does not answer) refuses the item instead.
/// </summary>
public sealed partial class DatabaseItemSerials(IPersonStore store, ILogger<DatabaseItemSerials>? logger = null) : IItemSerials
{
    /// <summary>
    /// The serials one query takes.
    /// </summary>
    public const int Block = 4096;

    /// <summary>
    /// The supply below which the next block is asked for. The rule for its size: the block takes one query to arrive
    /// (milliseconds), and until it does the supply must cover what the world loop makes meanwhile. One request makes at
    /// most the free slots of one storage (a few hundred); a boss that dies before a full party drops a few tens of things
    /// for every member. The reserve covers several of the largest of either before the block arrives, so only a database
    /// that stops answering lets it run dry.
    /// </summary>
    public const int Reserve = 2048;

    private readonly ConcurrentQueue<long> _ready = new();
    private readonly ILogger _logger = (ILogger?)logger ?? NullLogger.Instance;
    private readonly Lock _lock = new();

    // The refill in flight, shared by the background and by everyone who waits in EnsureAsync: one query at a time.
    private Task? _refill;

    // Set when an item was refused or a refill failed, so each is logged once until a refill succeeds.
    private int _dry;
    private int _failing;

    public long NextSerial()
    {
        return TryNextSerial(out var serial) ? serial : throw new ItemSerialsExhaustedException();
    }

    public bool TryNextSerial(out long serial)
    {
        RefillIfLow();

        if (_ready.TryDequeue(out serial))
        {
            return true;
        }

        if (Interlocked.Exchange(ref _dry, 1) == 0)
        {
            LogRanDry(_logger, Reserve);
        }

        return false;
    }

    public bool HasReady(int count)
    {
        RefillIfLow();

        return _ready.Count >= count;
    }

    public async ValueTask EnsureAsync(int count)
    {
        while (_ready.Count < count)
        {
            try
            {
                await RefillAsync();
            }

            catch (Exception)
            {
                // Logged by the refill. The caller's own check (HasReady, or a refusal of NextSerial) refuses what cannot be made.
                return;
            }
        }
    }

    /// <summary>
    /// Fills the supply before the first item is made.
    /// </summary>
    public Task PrimeAsync()
    {
        return RefillAsync();
    }

    /// <summary>
    /// Starts a refill in the background when the supply is below <see cref="Reserve"/>; the caller does not wait for it.
    /// A refill that fails is logged by itself, and the next caller starts another.
    /// </summary>
    private void RefillIfLow()
    {
        if (_ready.Count < Reserve && Volatile.Read(ref _refill) is not { IsCompleted: false })
        {
            _ = ObserveAsync(RefillAsync());
        }
    }

    private static async Task ObserveAsync(Task refill)
    {
        try
        {
            await refill;
        }

        catch (Exception)
        {
            // Logged by the refill; the next caller asks again.
        }
    }

    private Task RefillAsync()
    {
        lock (_lock)
        {
            if (_refill is { IsCompleted: false } running)
            {
                return running;
            }

            return _refill = Task.Run(FillAsync);
        }
    }

    private async Task FillAsync()
    {
        try
        {
            // One block may already be used up when it arrives: ask until the supply is whole again.
            do
            {
                foreach (var serial in await store.ReserveSerialsAsync(Block))
                {
                    _ready.Enqueue(serial);
                }
            }
            while (_ready.Count < Reserve);

            Interlocked.Exchange(ref _failing, 0);
            Interlocked.Exchange(ref _dry, 0);
        }

        catch (Exception exception)
        {
            if (Interlocked.Exchange(ref _failing, 1) == 0)
            {
                LogRefillFailed(_logger, exception, _ready.Count);
            }

            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Error,
        Message = "The supply of item serials ran dry: new items are refused until the database answers (the reserve is {Reserve}).")]
    private static partial void LogRanDry(ILogger logger, int reserve);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Taking item serials from the database failed with {Left} left in the supply; the next new item asks again.")]
    private static partial void LogRefillFailed(ILogger logger, Exception exception, int left);
}
