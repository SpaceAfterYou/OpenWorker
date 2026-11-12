using System.Diagnostics;
using OpenWorker.Domain.Stores;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;

namespace OpenWorker.Gameplay.Modules.Persistence;

/// <summary>
/// Writes what the flushes took, off the world loop, one batch after another. Everything queued while a batch is written
/// goes into the next one, so a thousand players closing at once is a few transactions, not a thousand. Writes keep the
/// order in which they were queued, and the snapshots are taken on the loop in that same order, so an older state of a
/// person never lands after a newer one.<br/>
/// What a player who leaves the server changed, and the database does not take, is kept here and tried again after the
/// player is gone (<see cref="LeaveAsync"/>), within <see cref="PersistenceOptions.KeepTimeout"/> and
/// <see cref="PersistenceOptions.KeepLimit"/>; the session of the player stays held meanwhile.
/// </summary>
public sealed partial class PersistenceWriter(
    IPersonStore store,
    WorldLoop loop,
    PersonSnapshots snapshots,
    ILogger<PersistenceWriter> logger,
    IOptions<PersistenceOptions>? options = null,
    PersistenceMetrics? metrics = null) : IHostedService
{
    /// <summary>
    /// How often the writes of one person are tried before they are given up: a database that is down for a while
    /// is waited for, a write the database refuses for good does not hold the others back for ever.
    /// </summary>
    public const int MaxAttempts = 5;

    private static TimeSpan RetryDelay { get; } = TimeSpan.FromSeconds(2);

    private readonly Channel<Request> _queue = Channel.CreateUnbounded<Request>(new UnboundedChannelOptions { SingleReader = true });

    private Task _consumer = Task.CompletedTask;

    private readonly TimeSpan _keepTimeout = (options?.Value ?? new PersistenceOptions()).KeepTimeout;
    private readonly int _keepLimit = (options?.Value ?? new PersistenceOptions()).KeepLimit;

    /// <summary>
    /// The persons whose connections are closing, while their last writes are on their way. Read and changed on the world
    /// loop only.
    /// </summary>
    private readonly HashSet<int> _leaving = [];

    /// <summary>
    /// What persons who have left changed and the database has not taken yet, by person. Changed by the writer, read by
    /// <see cref="LeaveAsync"/> on the loop and given up by <see cref="StopAsync"/>: under <see cref="_keptLock"/>.
    /// </summary>
    private readonly Dictionary<int, Kept> _kept = [];

    private readonly Lock _keptLock = new();

    private int KeptCount
    {
        get
        {
            lock (_keptLock)
            {
                return _kept.Count;
            }
        }
    }

    /// <summary>
    /// Queues the writes. The task ends when they are in the database, or given up (false). Called on the world loop,
    /// right after the snapshots are taken, so that nothing is queued between the snapshot and the queue.
    /// </summary>
    public Task<bool> EnqueueAsync(IReadOnlyList<PersonWrite> writes)
    {
        var request = new Request(writes);

        if (!_queue.Writer.TryWrite(request))
        {
            if (writes.Count > 0)
            {
                LogStopped(logger, writes.Count);
            }

            return Task.FromResult(writes.Count == 0);
        }

        return request.Done.Task;
    }

    /// <summary>
    /// Queues the last writes of a person whose connection closes, and ends as <see cref="EnqueueAsync"/> does. Writes of the
    /// person that the database does not take in time are not given back to the player, who will be gone, but kept and tried
    /// again. The task it returns ends when they are written, or given up at the bound; until then nobody may load the
    /// person. It has ended already when nothing was kept. Called on the world loop.
    /// </summary>
    public async Task<Task> LeaveAsync(int person, IReadOnlyList<PersonWrite> writes)
    {
        _leaving.Add(person);

        try
        {
            await EnqueueAsync(writes);
        }

        finally
        {
            _leaving.Remove(person);
        }

        lock (_keptLock)
        {
            return _kept.TryGetValue(person, out var kept) ? kept.Done.Task : Task.CompletedTask;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _consumer = Task.Run(ConsumeAsync, CancellationToken.None);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Takes nothing new and writes what is queued and what is kept of the players who left. The host stops this after the
    /// drain, whose sessions write as they close, and its timeout bounds the wait: what is kept then is lost, and logged.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _queue.Writer.TryComplete();

        try
        {
            await _consumer.WaitAsync(cancellationToken);
        }

        catch (OperationCanceledException)
        {
            LogStopTimedOut(logger);

            GiveUpKept();
        }
    }

    private async Task ConsumeAsync()
    {
        var reader = _queue.Reader;
        var pending = new List<Pending>();
        var keptTried = Stopwatch.GetTimestamp();

        while (pending.Count > 0 || KeptCount > 0 || await reader.WaitToReadAsync())
        {
            var batch = new List<Pending>(pending);

            pending.Clear();

            while (reader.TryRead(out var request))
            {
                if (request.Writes.Count == 0)
                {
                    // A barrier: it ends when what was queued before it is written.
                    batch.Add(new Pending(request, null, 0));
                    continue;
                }

                foreach (var _ in request.Writes)
                {
                    request.Add();
                }

                foreach (var write in request.Writes)
                {
                    if (KeepAfter(write))
                    {
                        request.Settle(false);
                        continue;
                    }

                    batch.Add(new Pending(request, write, 0));
                }
            }

            try
            {
                await WriteAsync(batch, pending);

                if (KeptCount > 0 && Stopwatch.GetElapsedTime(keptTried) >= RetryDelay)
                {
                    keptTried = Stopwatch.GetTimestamp();

                    await WriteKeptAsync();
                }
            }

            catch (Exception e)
            {
                // Nothing of a write escapes here; this is a fault of the writer itself.
                LogWriterFailed(logger, e);
            }

            if (pending.Count > 0)
            {
                await Task.Delay(RetryDelay);
            }

            else if (KeptCount > 0)
            {
                await WaitForRequestsAsync(reader);
            }
        }
    }

    /// <summary>
    /// Waits for the next request, or for the next try of what is kept, whichever comes first.
    /// </summary>
    private static async Task WaitForRequestsAsync(ChannelReader<Request> reader)
    {
        using var timeout = new CancellationTokenSource(RetryDelay);

        try
        {
            if (!await reader.WaitToReadAsync(timeout.Token))
            {
                // Nothing more comes: the writer is stopping and waits only for what is kept.
                await Task.Delay(RetryDelay, timeout.Token);
            }
        }

        catch (OperationCanceledException)
        {
            // Time for the next try.
        }
    }

    private async Task WriteAsync(List<Pending> batch, List<Pending> retry)
    {
        var writes = batch.Where(e => e.Write is not null).ToList();

        if (writes.Count > 0)
        {
            try
            {
                await store.SaveChangesAsync(writes.Select(e => e.Write!.Changes).ToList());

                Complete(writes, true);
            }

            catch (Exception e)
            {
                LogBatchFailed(logger, e, writes.Count);

                await WriteOneByOneAsync(writes, retry);
            }
        }

        // A barrier completes once everything queued before it has been settled: while a write waits to be tried again, the
        // barriers wait with it, so that a player sent to another server is not loaded there before its last write.
        foreach (var barrier in batch.Where(e => e.Write is null))
        {
            if (retry.Count > 0)
            {
                retry.Add(barrier);
            }

            else
            {
                barrier.Request.Done.TrySetResult(true);
            }
        }
    }

    /// <summary>
    /// After a failed batch, every person on its own: one whose rows the database refuses does not hold back the others.
    /// The writes of one person stay together and in order.
    /// </summary>
    private async Task WriteOneByOneAsync(List<Pending> writes, List<Pending> retry)
    {
        foreach (var person in writes.GroupBy(e => e.Write!.Changes.Person))
        {
            var mine = person.ToList();

            try
            {
                await store.SaveChangesAsync(mine.Select(e => e.Write!.Changes).ToList());

                Complete(mine, true);
            }

            catch (Exception e)
            {
                var attempt = mine.Max(x => x.Attempt) + 1;

                if (attempt < MaxAttempts)
                {
                    LogPersonFailed(logger, e, person.Key, attempt);

                    retry.AddRange(mine.Select(x => x with { Attempt = attempt }));

                    continue;
                }

                if (await GiveBackAsync(mine))
                {
                    Keep(person.Key, mine, e);
                }

                else
                {
                    LogPersonGivenUp(logger, e, person.Key, attempt);
                }

                Complete(mine, false);
            }
        }
    }

    /// <summary>
    /// Marks the parts that were given up as changed again, so that the next flush tries them as they are then. A person
    /// whose connection is closing does not get them back, as it will be gone: true, and they are to be kept.
    /// </summary>
    private async Task<bool> GiveBackAsync(List<Pending> given)
    {
        var person = given[0].Write!.Changes.Person;

        try
        {
            var lost = await loop.InvokeAsync(() => _leaving.Contains(person) ? -1 : given.Count(e => !snapshots.Restore(e.Write!)));

            if (lost < 0)
            {
                return true;
            }

            if (lost > 0)
            {
                LogLost(logger, person);
            }
        }

        catch (Exception e)
        {
            // The loop is gone: the server is stopping.
            LogLostOnStop(logger, e, person);
        }

        return false;
    }

    /// <summary>
    /// Keeps the writes of a person who leaves, in their order, to be tried again after the person is gone; or loses them
    /// when the server keeps as many persons as it may already.
    /// </summary>
    private void Keep(int person, List<Pending> given, Exception failure)
    {
        var kept = new Kept(person);

        kept.Writes.AddRange(given.Select(e => e.Write!));

        bool full;

        lock (_keptLock)
        {
            if (_kept.TryGetValue(person, out var earlier))
            {
                earlier.Writes.AddRange(kept.Writes);

                return;
            }

            full = _kept.Count >= _keepLimit;

            if (!full)
            {
                _kept[person] = kept;
            }
        }

        if (full)
        {
            LogLostAtLimit(logger, failure, person, _keepLimit, kept.Sections, kept.Items);

            metrics?.Refused();

            return;
        }

        LogKept(logger, failure, person, kept.Sections, _keepTimeout);

        metrics?.Kept();
    }

    /// <summary>
    /// A write of a person whose earlier writes are kept goes after them, never before: an older state does not land
    /// after a newer one.
    /// </summary>
    private bool KeepAfter(PersonWrite write)
    {
        lock (_keptLock)
        {
            if (!_kept.TryGetValue(write.Changes.Person, out var kept))
            {
                return false;
            }

            kept.Writes.Add(write);

            return true;
        }
    }

    /// <summary>
    /// Tries what is kept: every person in one transaction, and when that fails each on its own, so that one the database
    /// refuses does not hold back the others. A person still not written after <see cref="PersistenceOptions.KeepTimeout"/>
    /// is given up.
    /// </summary>
    private async Task WriteKeptAsync()
    {
        Kept[] kept;

        lock (_keptLock)
        {
            kept = [.. _kept.Values];
        }

        if (kept.Length > 1)
        {
            try
            {
                await store.SaveChangesAsync(kept.SelectMany(e => e.Writes).Select(e => e.Changes).ToList());

                foreach (var one in kept)
                {
                    End(one, null);
                }

                return;
            }

            catch (Exception)
            {
                // Each on its own below; what failed is logged when a person is given up.
            }
        }

        foreach (var one in kept)
        {
            try
            {
                await store.SaveChangesAsync(one.Writes.Select(e => e.Changes).ToList());

                End(one, null);
            }

            catch (Exception e)
            {
                if (Stopwatch.GetElapsedTime(one.Since) >= _keepTimeout)
                {
                    End(one, e);
                }
            }
        }
    }

    /// <summary>
    /// What was kept of a person is written (<paramref name="failure"/> null) or lost: the session of the person is let go.
    /// </summary>
    private void End(Kept kept, Exception? failure)
    {
        lock (_keptLock)
        {
            // The stop of the server may have given it up already.
            if (!_kept.TryGetValue(kept.Person, out var found) || found != kept)
            {
                return;
            }

            _kept.Remove(kept.Person);
        }

        var elapsed = Stopwatch.GetElapsedTime(kept.Since);

        if (failure is null)
        {
            LogKeptWritten(logger, kept.Person, elapsed);
        }

        else
        {
            LogKeptLost(logger, failure, kept.Person, elapsed, kept.Sections, kept.Items);
        }

        metrics?.Ended(failure is null);

        kept.Done.TrySetResult(failure is null);
    }

    /// <summary>
    /// The host stopped waiting: what is still kept is lost.
    /// </summary>
    private void GiveUpKept()
    {
        Kept[] kept;

        lock (_keptLock)
        {
            kept = [.. _kept.Values];

            _kept.Clear();
        }

        foreach (var one in kept)
        {
            LogKeptLostOnStop(logger, one.Person, one.Sections, one.Items);

            metrics?.Ended(false);

            one.Done.TrySetResult(false);
        }
    }

    private static void Complete(List<Pending> writes, bool written)
    {
        foreach (var write in writes)
        {
            write.Request.Settle(written);
        }
    }

    private sealed class Request(IReadOnlyList<PersonWrite> writes)
    {
        private int _open;
        private bool _failed;

        public IReadOnlyList<PersonWrite> Writes { get; } = writes;

        public TaskCompletionSource<bool> Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Add()
        {
            _open++;
        }

        /// <summary>
        /// Called on the writer only, once for each write of the request.
        /// </summary>
        public void Settle(bool written)
        {
            _failed |= !written;

            if (--_open == 0)
            {
                Done.TrySetResult(!_failed);
            }
        }
    }

    private sealed record Pending(Request Request, PersonWrite? Write, int Attempt);

    /// <summary>
    /// The writes of a person who has left, in their order, since the moment the first was kept.
    /// </summary>
    private sealed class Kept(int person)
    {
        public int Person { get; } = person;

        public long Since { get; } = Stopwatch.GetTimestamp();

        public List<PersonWrite> Writes { get; } = [];

        public TaskCompletionSource<bool> Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public PersonSections Sections => Writes.Aggregate(PersonSections.None, (sections, write) => sections | write.Sections);

        /// <summary>
        /// The items added, changed and removed.
        /// </summary>
        public int Items => Writes.Sum(e => e.Changes.Added.Count + e.Changes.Changed.Count + e.Changes.Removed.Count);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Writing the changes of {Count} persons together failed, they are written one by one")]
    private static partial void LogBatchFailed(ILogger logger, Exception exception, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Writing the changes of person {Person} failed (attempt {Attempt}), it is tried again")]
    private static partial void LogPersonFailed(ILogger logger, Exception exception, int person, int attempt);

    [LoggerMessage(Level = LogLevel.Error, Message = "Writing the changes of person {Person} failed {Attempt} times and is given up; what is still on the server is written by the next flush")]
    private static partial void LogPersonGivenUp(ILogger logger, Exception exception, int person, int attempt);

    [LoggerMessage(Level = LogLevel.Error, Message = "Person {Person} has left the server, and the changes that could not be written are lost")]
    private static partial void LogLost(ILogger logger, int person);

    [LoggerMessage(Level = LogLevel.Error, Message = "The changes of person {Person} that could not be written are lost: the server is stopping")]
    private static partial void LogLostOnStop(ILogger logger, Exception exception, int person);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Person {Person} has left and the database did not take what it changed ({Sections}); it is kept and tried again for up to {Timeout}, and the session stays held until then")]
    private static partial void LogKept(ILogger logger, Exception exception, int person, PersonSections sections, TimeSpan timeout);

    [LoggerMessage(Level = LogLevel.Information, Message = "What person {Person} changed before it left is written {Elapsed} late; the session is let go")]
    private static partial void LogKeptWritten(ILogger logger, int person, TimeSpan elapsed);

    [LoggerMessage(Level = LogLevel.Error, Message = "What person {Person} changed before it left could not be written in {Elapsed} and is lost: {Sections}, {Items} items")]
    private static partial void LogKeptLost(ILogger logger, Exception exception, int person, TimeSpan elapsed, PersonSections sections, int items);

    [LoggerMessage(Level = LogLevel.Error, Message = "Person {Person} has left and what it changed cannot be kept, {Limit} persons wait already; it is lost: {Sections}, {Items} items")]
    private static partial void LogLostAtLimit(ILogger logger, Exception exception, int person, int limit, PersonSections sections, int items);

    [LoggerMessage(Level = LogLevel.Error, Message = "The server stopped before what person {Person} changed before it left could be written; it is lost: {Sections}, {Items} items")]
    private static partial void LogKeptLostOnStop(ILogger logger, int person, PersonSections sections, int items);

    [LoggerMessage(Level = LogLevel.Error, Message = "The writer of the changes of persons failed")]
    private static partial void LogWriterFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "The changes of {Count} persons came after the writer stopped and are lost")]
    private static partial void LogStopped(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "The host stopped waiting for the changes of persons to be written; what was still queued is lost")]
    private static partial void LogStopTimedOut(ILogger logger);
}
