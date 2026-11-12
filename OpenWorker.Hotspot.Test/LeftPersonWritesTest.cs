using System.Diagnostics;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.Persistence;

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
        public PersistenceWriter Writer { get; }
        public PersonFlush Flush { get; }

        public Server(PersistenceOptions options)
        {
            var snapshots = new PersonSnapshots(World, [], [new NewPlace()]);

            Writer = new PersistenceWriter(Database, Loop, snapshots, NullLogger<PersistenceWriter>.Instance, Options.Create(options),
                new PersistenceMetrics(Metrics.Factory));
            Flush = new PersonFlush(World, snapshots, Writer);
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

    public TestContext TestContext { get; set; } = null!;
}
