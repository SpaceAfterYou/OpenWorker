using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenWorker.Hotspot;
using OpenWorker.Persistence;

namespace OpenWorker.Gameplay.Modules.Persistence;

/// <summary>
/// Writes what the flushes took, off the world loop, one batch after another. Everything queued while a batch is written
/// goes into the next one, so a thousand players closing at once is a few transactions, not a thousand. Writes keep the
/// order in which they were queued, and the snapshots are taken on the loop in that same order, so an older state of a
/// person never lands after a newer one.
/// </summary>
public sealed partial class PersistenceWriter(
    IDbContextFactory<PersistenceContext> factory,
    WorldLoop loop,
    PersonSnapshots snapshots,
    ILogger<PersistenceWriter> logger) : IHostedService
{
    /// <summary>
    /// How often the writes of one person are tried before they are given up: a database that is down for a while
    /// is waited for, a write the database refuses for good does not hold the others back for ever.
    /// </summary>
    public const int MaxAttempts = 5;

    private static TimeSpan RetryDelay { get; } = TimeSpan.FromSeconds(2);

    private readonly Channel<Request> _queue = Channel.CreateUnbounded<Request>(new UnboundedChannelOptions { SingleReader = true });

    private Task _consumer = Task.CompletedTask;

    /// <summary>
    /// Queues the writes. The task ends when they are in the database, or given up (false). Called on the world loop,
    /// right after the snapshots are taken, so that nothing is queued between the snapshot and the queue.
    /// </summary>
    public Task<bool> EnqueueAsync(IReadOnlyList<PersonWrite> writes)
    {
        var request = new Request(writes);

        if (!_queue.Writer.TryWrite(request))
        {
            LogStopped(logger, writes.Count);

            return Task.FromResult(false);
        }

        return request.Done.Task;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _consumer = Task.Run(ConsumeAsync, CancellationToken.None);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Takes nothing new and writes what is queued. The host stops this after the drain, whose sessions write as they close.
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
        }
    }

    private async Task ConsumeAsync()
    {
        var reader = _queue.Reader;
        var pending = new List<Pending>();

        while (pending.Count > 0 || await reader.WaitToReadAsync())
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

                foreach (var write in request.Writes)
                {
                    request.Add();
                    batch.Add(new Pending(request, write, 0));
                }
            }

            try
            {
                await WriteAsync(batch, pending);
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
        }
    }

    private async Task WriteAsync(List<Pending> batch, List<Pending> retry)
    {
        var writes = batch.Where(e => e.Write is not null).ToList();

        if (writes.Count > 0)
        {
            try
            {
                await factory.SaveChangesAsync(writes.Select(e => e.Write!.Changes).ToList());

                Complete(writes, true);
            }

            catch (Exception e)
            {
                LogBatchFailed(logger, e, writes.Count);

                await WriteOneByOneAsync(writes, retry);
            }
        }

        // A barrier completes once everything queued before it has been settled.
        foreach (var barrier in batch.Where(e => e.Write is null))
        {
            barrier.Request.Done.TrySetResult(true);
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
                await factory.SaveChangesAsync(mine.Select(e => e.Write!.Changes).ToList());

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

                LogPersonGivenUp(logger, e, person.Key, attempt);

                await GiveBackAsync(mine);

                Complete(mine, false);
            }
        }
    }

    /// <summary>
    /// Marks the parts that were given up as changed again, so that the next flush tries them as they are then.
    /// </summary>
    private async Task GiveBackAsync(List<Pending> given)
    {
        try
        {
            var lost = await loop.InvokeAsync(() => given.Count(e => !snapshots.Restore(e.Write!)));

            if (lost > 0)
            {
                LogLost(logger, given[0].Write!.Changes.Person);
            }
        }

        catch (Exception e)
        {
            // The loop is gone: the server is stopping.
            LogLostOnStop(logger, e, given[0].Write!.Changes.Person);
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

    [LoggerMessage(Level = LogLevel.Error, Message = "The writer of the changes of persons failed")]
    private static partial void LogWriterFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "The changes of {Count} persons came after the writer stopped and are lost")]
    private static partial void LogStopped(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "The host stopped waiting for the changes of persons to be written; what was still queued is lost")]
    private static partial void LogStopTimedOut(ILogger logger);
}
