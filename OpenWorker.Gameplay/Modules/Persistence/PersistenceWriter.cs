using System.Data.Common;
using System.Net.Sockets;
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
    PersistenceMetrics? metrics = null,
    TimeProvider? time = null) : IHostedService
{
    /// <summary>
    /// How often the writes of one person are tried before they are given up: a database that is down for a while
    /// is waited for, a write the database refuses for good does not hold the others back for ever.
    /// </summary>
    public const int MaxAttempts = 5;

    /// <summary>
    /// How long the writer waits before it tries again what failed, and between two rounds of what is kept.
    /// </summary>
    public static TimeSpan RetryDelay { get; } = TimeSpan.FromSeconds(2);

    private readonly TimeProvider _time = time ?? TimeProvider.System;

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

    /// <summary>
    /// The stop of the server has given up what was kept: what would be kept after it is lost at once. Under
    /// <see cref="_keptLock"/>.
    /// </summary>
    private bool _givenUp;

    /// <summary>
    /// Counts the tries of kept persons on their own, so that the one tried longest ago goes first. Writer only.
    /// </summary>
    private long _turn;

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

    /// <summary>
    /// Whether what the person changed before it left this server still waits here for the database. Such a person may not
    /// enter again until it is written or given up: its entry would read an older state, and the kept writes would land
    /// over what it changes then.
    /// </summary>
    public bool Keeps(int person)
    {
        lock (_keptLock)
        {
            return _kept.ContainsKey(person);
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
        var keptTried = _time.GetTimestamp();

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

                if (KeptCount > 0 && _time.GetElapsedTime(keptTried) >= RetryDelay)
                {
                    keptTried = _time.GetTimestamp();

                    await WriteKeptAsync(reader);
                }
            }

            catch (Exception e)
            {
                // Nothing of a write escapes here; this is a fault of the writer itself.
                LogWriterFailed(logger, e);
            }

            if (pending.Count > 0)
            {
                await Task.Delay(RetryDelay, _time);
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
    private async Task WaitForRequestsAsync(ChannelReader<Request> reader)
    {
        using var timeout = new CancellationTokenSource(RetryDelay, _time);

        try
        {
            if (!await reader.WaitToReadAsync(timeout.Token))
            {
                // Nothing more comes: the writer is stopping and waits only for what is kept.
                await Task.Delay(Timeout.InfiniteTimeSpan, timeout.Token);
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

            catch (Exception e) when (IsUnreachable(e))
            {
                // The database did not answer: one by one, each would wait as long again. All are tried in the next round.
                LogBatchUnreachable(logger, e, writes.Count);

                foreach (var person in writes.GroupBy(x => x.Write!.Changes.Person))
                {
                    await FailAsync(person.Key, [.. person], e, retry);
                }
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
                await FailAsync(person.Key, mine, e, retry);
            }
        }
    }

    /// <summary>
    /// The writes of one person failed: they are tried again, or after the last attempt given back to the player, or kept
    /// when the player is leaving.
    /// </summary>
    private async Task FailAsync(int person, List<Pending> mine, Exception failure, List<Pending> retry)
    {
        var attempt = mine.Max(x => x.Attempt) + 1;

        if (attempt < MaxAttempts)
        {
            LogPersonFailed(logger, failure, person, attempt);

            retry.AddRange(mine.Select(x => x with { Attempt = attempt }));

            return;
        }

        if (await GiveBackAsync(mine))
        {
            Keep(person, mine, failure);
        }

        else
        {
            LogPersonGivenUp(logger, failure, person, attempt);
        }

        Complete(mine, false);
    }

    /// <summary>
    /// Whether a write failed because the database could not be reached or did not answer, rather than because it refused
    /// the data: then every other write of the moment fails the same way, and trying them one by one only multiplies the
    /// wait.
    /// </summary>
    private static bool IsUnreachable(Exception failure)
    {
        for (var cause = failure; cause is not null; cause = cause.InnerException)
        {
            if (cause is TimeoutException or SocketException or IOException or DbException { IsTransient: true })
            {
                return true;
            }
        }

        return false;
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
        var kept = new Kept(person, _time.GetTimestamp(), failure);

        kept.Writes.AddRange(given.Select(e => e.Write!));

        bool full;
        bool stopped;

        lock (_keptLock)
        {
            if (_kept.TryGetValue(person, out var earlier))
            {
                earlier.Writes.AddRange(kept.Writes);

                return;
            }

            stopped = _givenUp;
            full = _kept.Count >= _keepLimit;

            if (!full && !stopped)
            {
                _kept[person] = kept;
            }
        }

        if (stopped)
        {
            // The stop of the server has given up what was kept already; nothing would try this one any more.
            LogKeptLostOnStop(logger, person, kept.Sections, kept.Items);

            metrics?.Refused();

            return;
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
    /// A round of what is kept. A person whose time is up is given up first, without a try. The others are tried in one
    /// transaction; when the database did not answer, that is all for the round, since each on its own would wait as long
    /// again. When it refused the data, each person is tried on its own, the one tried longest ago first, so that one the
    /// database refuses does not hold back the others; and the round stops as soon as new writes wait, so that the players
    /// still on the server are not held back by those who left. The rest have their turn in the next round.
    /// </summary>
    private async Task WriteKeptAsync(ChannelReader<Request> reader)
    {
        EndExpired();

        var kept = Begin(null);

        if (kept.Length == 0)
        {
            return;
        }

        try
        {
            await store.SaveChangesAsync(kept.SelectMany(e => e.Writes).Select(e => e.Changes).ToList());

            foreach (var one in kept)
            {
                Settle(one, null);
            }

            return;
        }

        catch (Exception e)
        {
            foreach (var one in kept)
            {
                Settle(one, e);
            }

            if (kept.Length == 1 || IsUnreachable(e))
            {
                return;
            }
        }

        foreach (var one in kept.OrderBy(e => e.Turn))
        {
            if (Begin(one).Length == 0)
            {
                // Given up meanwhile: its time is up, or the server stops.
                continue;
            }

            one.Turn = ++_turn;

            try
            {
                await store.SaveChangesAsync(one.Writes.Select(e => e.Changes).ToList());

                Settle(one, null);
            }

            catch (Exception e)
            {
                Settle(one, e);
            }

            if (reader.TryPeek(out _))
            {
                return;
            }
        }
    }

    /// <summary>
    /// Gives up, without a try, every kept person whose time is up.
    /// </summary>
    private void EndExpired()
    {
        Kept[] expired;

        lock (_keptLock)
        {
            expired = [.. _kept.Values.Where(e => _time.GetElapsedTime(e.Since) >= _keepTimeout)];

            foreach (var one in expired)
            {
                _kept.Remove(one.Person);
            }
        }

        foreach (var one in expired)
        {
            End(one, one.Failure);
        }
    }

    /// <summary>
    /// Takes the persons about to be tried, <paramref name="one"/> or every kept person, and marks them as on their way to
    /// the database, so that the stop of the server knows their outcome is not known yet. One no longer kept is left out.
    /// </summary>
    private Kept[] Begin(Kept? one)
    {
        lock (_keptLock)
        {
            Kept[] taken = one is null
                ? [.. _kept.Values]
                : _kept.TryGetValue(one.Person, out var found) && found == one ? [one] : [];

            foreach (var each in taken)
            {
                each.Writing = true;
            }

            return taken;
        }
    }

    /// <summary>
    /// A try of a kept person has answered: written (<paramref name="failure"/> null), or failed. The person is ended when
    /// it is written, when its time is up, and when the stop of the server gave it up during the try; else it waits for the
    /// next round.
    /// </summary>
    private void Settle(Kept kept, Exception? failure)
    {
        lock (_keptLock)
        {
            kept.Writing = false;
            kept.Failure = failure ?? kept.Failure;

            if (kept.GivenUpWhileWriting)
            {
                kept.GivenUpWhileWriting = false;
            }

            else if (!_kept.TryGetValue(kept.Person, out var found) || found != kept)
            {
                return;
            }

            else if (failure is not null && _time.GetElapsedTime(kept.Since) < _keepTimeout)
            {
                return;
            }

            else
            {
                _kept.Remove(kept.Person);
            }
        }

        End(kept, failure);
    }

    /// <summary>
    /// What was kept of a person is written (<paramref name="failure"/> null) or lost: the session of the person is let go.
    /// No longer kept when this is called.
    /// </summary>
    private void End(Kept kept, Exception? failure)
    {
        var elapsed = _time.GetElapsedTime(kept.Since);

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
    /// The host stopped waiting: what is still kept is lost, nothing of it is tried any more, and what would be kept from
    /// now on is lost at once. A person whose try is on its way to the database at this moment may still land there: its
    /// session is let go only when the database has answered, or expires by itself when the process ends first.
    /// </summary>
    private void GiveUpKept()
    {
        (Kept Kept, bool Writing, PersonSections Sections, int Items)[] kept;

        lock (_keptLock)
        {
            _givenUp = true;

            kept = [.. _kept.Values.Select(e => (e, e.Writing, e.Sections, e.Items))];

            _kept.Clear();

            foreach (var (one, writing, _, _) in kept)
            {
                one.GivenUpWhileWriting = writing;
            }
        }

        foreach (var (one, writing, sections, items) in kept)
        {
            if (writing)
            {
                LogKeptUnsettledOnStop(logger, one.Person, sections, items);

                continue;
            }

            LogKeptLostOnStop(logger, one.Person, sections, items);

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
    private sealed class Kept(int person, long since, Exception failure)
    {
        public int Person { get; } = person;

        public long Since { get; } = since;

        /// <summary>
        /// Why the last try failed: named when the person is given up.
        /// </summary>
        public Exception Failure { get; set; } = failure;

        /// <summary>
        /// The turn of its last try on its own: the person tried longest ago goes first. Writer only.
        /// </summary>
        public long Turn { get; set; }

        /// <summary>
        /// A try is on its way to the database. Under the lock of the kept persons.
        /// </summary>
        public bool Writing { get; set; }

        /// <summary>
        /// The stop of the server gave the person up while its try was on its way: it is ended when the try answers. Under the
        /// lock of the kept persons.
        /// </summary>
        public bool GivenUpWhileWriting { get; set; }

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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Writing the changes of {Count} persons together failed because the database did not answer; all are tried again later, not one by one")]
    private static partial void LogBatchUnreachable(ILogger logger, Exception exception, int count);

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

    [LoggerMessage(Level = LogLevel.Error, Message = "The server stopped while what person {Person} changed before it left was on its way to the database; whether it lands is not known: {Sections}, {Items} items. The session is let go when the database answers, or expires by itself")]
    private static partial void LogKeptUnsettledOnStop(ILogger logger, int person, PersonSections sections, int items);

    [LoggerMessage(Level = LogLevel.Error, Message = "The writer of the changes of persons failed")]
    private static partial void LogWriterFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "The changes of {Count} persons came after the writer stopped and are lost")]
    private static partial void LogStopped(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "The host stopped waiting for the changes of persons to be written; what was still queued is lost")]
    private static partial void LogStopTimedOut(ILogger logger);
}
