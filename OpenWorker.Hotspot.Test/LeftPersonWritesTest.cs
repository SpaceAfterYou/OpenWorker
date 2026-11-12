using System.Diagnostics;
using System.Threading.Channels;
using Arch.Core;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Extensions.Options;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What a player changed and the database did not take before the player left is kept by the writer, written once the
/// database takes it, and given up at the bounds of time, of number and of the stop of the server. The session of the
/// player stays held while it waits (<see cref="SessionHoldComponent"/>).
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class LeftPersonWritesTest
{
    private static PersonPlace Place { get; } = new(10003, 1, 2, 3, 0);

    /// <summary>
    /// A database that refuses every write while it is down.
    /// </summary>
    private sealed class Database : IPersonStore
    {
        private readonly Lock _lock = new();
        private readonly List<PersonChanges> _written = [];

        public volatile bool Down = true;

        public PersonChanges[] Written
        {
            get
            {
                lock (_lock)
                {
                    return [.. _written];
                }
            }
        }

        public Task SaveChangesAsync(IReadOnlyList<PersonChanges> batch, CancellationToken cancellationToken = default)
        {
            if (Down)
            {
                throw new InvalidOperationException("The database is down.");
            }

            lock (_lock)
            {
                _written.AddRange(batch);
            }

            return Task.CompletedTask;
        }

        public Task<List<ItemPersistent>> LoadItemsAsync(int person, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<long[]> ReserveSerialsAsync(int count, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<string?> LoadDocumentAsync(int person, PersonStateKind kind, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Dictionary<PersonStateKind, string>> LoadDocumentsAsync(int person, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    /// <summary>
    /// The player stands where it was not written yet, so its last flush has something to write.
    /// </summary>
    private sealed class NewPlace : IPersonPlaceSource
    {
        public bool TryGetPlace(World world, Entity player, out PersonPlace place)
        {
            place = Place;

            return true;
        }
    }

    /// <summary>
    /// The write-behind of one server over <see cref="Database"/>, with the close of a connection as the server runs it.
    /// </summary>
    private sealed class Server : IDisposable
    {
        public Database Database { get; } = new();
        public World World { get; } = World.Create();
        public WorldLoop Loop { get; } = new(NullLogger<WorldLoop>.Instance);
        public MetricsHost Metrics { get; } = new();
        public PersonSnapshots Snapshots { get; }
        public PersistenceWriter Writer { get; }
        public PersonFlush Flush { get; }

        public Server(PersistenceOptions options, IPersonStore? store = null, TimeProvider? time = null)
        {
            Snapshots = new PersonSnapshots(World, [], [new NewPlace()]);

            Writer = new PersistenceWriter(store ?? Database, Loop, Snapshots, NullLogger<PersistenceWriter>.Instance, Options.Create(options),
                new PersistenceMetrics(Metrics.Factory), time);
            Flush = new PersonFlush(World, Snapshots, Writer);
        }

        public Task<Entity> EnterAsync(int person) => Loop.InvokeAsync(() => World.Create(new DirtyComponent(new PersonDirty(person))));

        /// <summary>
        /// The connection closes as the server closes it: the close handlers, then the end of the entity. Returns what
        /// holds the session, or null when the session is let go at once.
        /// </summary>
        public Task<Task?> LeaveAsync(Entity player) => Loop.InvokeAsync(async () =>
        {
            await Flush.OnSessionClosedAsync(player);

            Task? until = World.TryGet(player, out SessionHoldComponent hold) ? hold.Until : null;

            World.Destroy(player);

            return until;
        }).Unwrap();

        public void Dispose()
        {
            Loop.Dispose();
            World.Dispose();
            Metrics.Dispose();
        }
    }

    [TestMethod]
    public async Task WhatAPlayerChangedIsWrittenAfterTheyLeftWhileTheDatabaseWasDown()
    {
        using var server = new Server(new PersistenceOptions());
        using var ended = server.Metrics.Listen<long>("persistence.kept.ended");

        await server.Writer.StartAsync(CancellationToken.None);

        var player = await server.EnterAsync(7);
        var until = await server.LeaveAsync(player);

        Assert.IsNotNull(until, "The session is held while the write waits");
        Assert.IsFalse(until.IsCompleted);
        Assert.IsEmpty(server.Database.Written);

        server.Database.Down = false;

        await until.WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        Assert.AreEqual(Place, server.Database.Written.Single().Place, "The write is in before the session is let go");
        Assert.AreEqual(1, ended.Sum("outcome", "written"));

        await server.Writer.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// A database that stays down neither holds a session for ever nor makes the server keep every player who leaves: past
    /// the time the changes are lost and the session let go, and one player more than the limit is lost at once.
    /// </summary>
    [TestMethod]
    public async Task WhatAPlayerWhoLeftChangedIsGivenUpAtTheBoundsOfTimeAndNumber()
    {
        using var server = new Server(new PersistenceOptions { KeepTimeout = TimeSpan.FromSeconds(1), KeepLimit = 1 });
        using var kept = server.Metrics.Listen<long>("persistence.kept");
        using var ended = server.Metrics.Listen<long>("persistence.kept.ended");

        await server.Writer.StartAsync(CancellationToken.None);

        var first = await server.EnterAsync(7);
        var second = await server.EnterAsync(8);

        var leaving = await Task.WhenAll(server.LeaveAsync(first), server.LeaveAsync(second));
        var held = leaving.OfType<Task>().ToArray();

        Assert.HasCount(1, held, "One player is kept, the one beyond the limit is lost at once");

        await held[0].WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        Assert.IsEmpty(server.Database.Written);
        Assert.AreEqual(2, ended.Sum("outcome", "lost"));
        Assert.AreEqual(0, kept.Sum(), "Nothing waits any more");

        await server.Writer.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// A server that stops waits for what is kept within its time, then gives it up and lets the sessions go.
    /// </summary>
    [TestMethod]
    public async Task AStoppingServerWaitsForWhatIsKeptWithinItsTime()
    {
        using var server = new Server(new PersistenceOptions());
        using var ended = server.Metrics.Listen<long>("persistence.kept.ended");

        await server.Writer.StartAsync(CancellationToken.None);

        var player = await server.EnterAsync(7);
        var until = await server.LeaveAsync(player);

        Assert.IsNotNull(until);

        using var host = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        var timer = Stopwatch.StartNew();

        await server.Writer.StopAsync(host.Token);

        Assert.IsGreaterThanOrEqualTo(900, timer.ElapsedMilliseconds, "The stop waited for the kept write");
        Assert.IsTrue(until.IsCompleted, "The session is let go when the stop gives up");
        Assert.AreEqual(1, ended.Sum("outcome", "lost"));
    }

    /// <summary>
    /// What the writer does, in its order: a write it sends to the database, or a wait it begins.
    /// </summary>
    private abstract record Step;

    private sealed record Saved(int[] Persons, DateTimeOffset At) : Step;

    private sealed record Waiting : Step;

    /// <summary>
    /// The fake clock of the writer; it tells when the writer begins to wait.
    /// </summary>
    private sealed class Clock(ChannelWriter<Step> steps) : FakeTimeProvider
    {
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = base.CreateTimer(callback, state, dueTime, period);

            steps.TryWrite(new Waiting());

            return timer;
        }
    }

    /// <summary>
    /// A database whose answer to each write the test decides, and which tells the test of each write.
    /// </summary>
    private sealed class ScriptedDatabase(ChannelWriter<Step> steps, TimeProvider clock) : IPersonStore
    {
        public Func<int[], Task> Answer { get; set; } = _ => Task.CompletedTask;

        public Task SaveChangesAsync(IReadOnlyList<PersonChanges> batch, CancellationToken cancellationToken = default)
        {
            var persons = batch.Select(e => e.Person).Distinct().Order().ToArray();

            // The answer first: what it changes is seen by the test together with the write.
            var answer = Answer(persons);

            steps.TryWrite(new Saved(persons, clock.GetUtcNow()));

            return answer;
        }

        public Task<List<ItemPersistent>> LoadItemsAsync(int person, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<long[]> ReserveSerialsAsync(int count, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<string?> LoadDocumentAsync(int person, PersonStateKind kind, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Dictionary<PersonStateKind, string>> LoadDocumentsAsync(int person, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    /// <summary>
    /// Runs the writer step by step on the fake clock: every wait it begins passes at once, every write is recorded.
    /// </summary>
    private sealed class Script
    {
        private readonly Channel<Step> _steps = System.Threading.Channels.Channel.CreateUnbounded<Step>();

        public Script()
        {
            Clock = new Clock(_steps.Writer);
            Database = new ScriptedDatabase(_steps.Writer, Clock);
        }

        public Clock Clock { get; }

        public ScriptedDatabase Database { get; }

        public List<Saved> Saves { get; } = [];

        /// <summary>
        /// Takes the steps of the writer until <paramref name="done"/> holds; <paramref name="wake"/> ends when it may hold
        /// while the writer does nothing.
        /// </summary>
        public async Task RunUntilAsync(Func<bool> done, Task? wake = null)
        {
            while (!done())
            {
                if (_steps.Reader.TryRead(out var step))
                {
                    if (step is Saved saved)
                    {
                        Saves.Add(saved);
                    }

                    else
                    {
                        Clock.Advance(PersistenceWriter.RetryDelay);
                    }

                    continue;
                }

                var next = _steps.Reader.WaitToReadAsync().AsTask();

                // A liveness bound only: every wait of the writer passes on the fake clock.
                await Task.WhenAny(next, wake ?? next).WaitAsync(TimeSpan.FromSeconds(10));
            }
        }
    }

    private static Task Unreachable() => Task.FromException(new InvalidOperationException("No answer.", new TimeoutException()));

    /// <summary>
    /// Players leave together before the writer starts, so that their writes go in one batch.
    /// </summary>
    private static async Task<Task<Task?[]>> LeaveTogetherAsync(Server server, params int[] persons)
    {
        var players = new List<Entity>();

        foreach (var person in persons)
        {
            players.Add(await server.EnterAsync(person));
        }

        var leaving = Task.WhenAll(players.Select(server.LeaveAsync));

        // The loop takes callbacks in order: after this one, every close has queued its writes.
        await server.Loop.InvokeAsync(() => { });

        return leaving;
    }

    /// <summary>
    /// Players leave together while the database does not take their writes; the writer runs until it keeps them all, and
    /// then the clock stands while their closes end, so that no bound passes before a close sees what holds its session.
    /// Returns what holds each session.
    /// </summary>
    private static async Task<Task[]> LeaveAndKeepAsync(Script script, Server server, params int[] persons)
    {
        using var kept = server.Metrics.Listen<long>("persistence.kept");

        var leaving = await LeaveTogetherAsync(server, persons);

        await server.Writer.StartAsync(CancellationToken.None);
        await script.RunUntilAsync(() => kept.GetMeasurementSnapshot().Count(e => e.Value > 0) == persons.Length);

        var holds = await leaving.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.IsTrue(holds.All(e => e is { IsCompleted: false }), "Every session is held");

        return [.. holds.Select(e => e!)];
    }

    /// <summary>
    /// A database that does not answer costs the players who left one try together each round, never one each, and none at
    /// all once their time is up: they are let go at the bound, not a try later.
    /// </summary>
    [TestMethod]
    public async Task ADatabaseThatDoesNotAnswerCostsThePlayersWhoLeftOneTryARoundAndNoneAfterTheirTime()
    {
        var script = new Script();
        var timeout = TimeSpan.FromSeconds(20);

        using var server = new Server(new PersistenceOptions { KeepTimeout = timeout }, script.Database, script.Clock);
        using var kept = new MetricCollector<long>(server.Metrics.Factory, OpenWorkerMeter.Name, "persistence.kept", script.Clock);
        using var ended = server.Metrics.Listen<long>("persistence.kept.ended");

        script.Database.Answer = _ => Unreachable();

        var holds = await LeaveAndKeepAsync(script, server, 7, 8, 9);

        await script.RunUntilAsync(() => holds.All(e => e.IsCompleted), Task.WhenAll(holds));

        var keptAt = kept.GetMeasurementSnapshot().Where(e => e.Value > 0).Select(e => e.Timestamp).Distinct().Single();

        Assert.IsTrue(script.Saves.All(e => e.Persons.SequenceEqual([7, 8, 9])), "No try of a person on its own after the database did not answer");
        Assert.IsTrue(script.Saves.Any(e => e.At > keptAt), "The kept writes were tried");
        Assert.IsTrue(script.Saves.All(e => e.At < keptAt + timeout), "No try once the time is up");
        Assert.AreEqual(3, ended.Sum("outcome", "lost"));

        await server.Writer.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// The players who left do not hold back the writes of the players still on the server: a round of them tried one by
    /// one stops as soon as another write waits, and the next round begins with the one tried longest ago.
    /// </summary>
    [TestMethod]
    public async Task ThePlayersWhoLeftDoNotHoldBackTheWritesOfThoseWhoStay()
    {
        var script = new Script();

        using var server = new Server(new PersistenceOptions(), script.Database, script.Clock);

        var staying = await server.EnterAsync(9);
        var write = (await server.Loop.InvokeAsync(() => server.Snapshots.Take(staying)))!;
        var tries = 0;
        Task<bool>? flushed = null;

        script.Database.Answer = persons =>
        {
            // The first try of person 7 on its own after it was kept: the player who stays flushes meanwhile.
            if (persons is [7] && ++tries == PersistenceWriter.MaxAttempts + 1)
            {
                flushed = server.Writer.EnqueueAsync([write]);
            }

            return persons.Contains(7) || persons.Contains(8)
                ? Task.FromException(new InvalidOperationException("The database refuses the rows."))
                : Task.CompletedTask;
        };

        var leaving = await LeaveTogetherAsync(server, 7, 8);

        await server.Writer.StartAsync(CancellationToken.None);

        var first = -1;

        await script.RunUntilAsync(() =>
        {
            first = flushed is null ? -1 : script.Saves.FindIndex(e => e.Persons is [9]) - 1;

            return first >= 0 && script.Saves.Count >= first + 4;
        });

        Assert.IsTrue(await flushed!, "The write of the player who stays is in");
        CollectionAssert.AreEqual(new[] { 7 }, script.Saves[first].Persons, "The round was stopped right after person 7");
        CollectionAssert.AreEqual(new[] { 7, 8 }, script.Saves[first + 2].Persons, "The next round tries both together first");
        CollectionAssert.AreEqual(new[] { 8 }, script.Saves[first + 3].Persons, "and then the one tried longest ago");

        await server.Writer.StopAsync(new CancellationToken(true));
    }

    public TestContext TestContext { get; set; } = null!;
}
