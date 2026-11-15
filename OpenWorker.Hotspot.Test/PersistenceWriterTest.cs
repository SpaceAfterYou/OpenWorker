using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What the writer gives back to a player when the database does not take a write.
/// </summary>
[TestClass]
public sealed class PersistenceWriterTest
{
    /// <summary>
    /// A database that takes every write and, as many times as the test says, loses the answer.
    /// </summary>
    private sealed class LostAnswers : IPersonStore
    {
        private readonly Lock _lock = new();
        private readonly List<PersonChanges> _written = [];

        public volatile int Lost;

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
            lock (_lock)
            {
                _written.AddRange(batch);
            }

            return Interlocked.Decrement(ref Lost) >= 0
                ? Task.FromException(new InvalidOperationException("The write is in, its answer is lost."))
                : Task.CompletedTask;
        }

        public Task<List<ItemPersistent>> LoadItemsAsync(int person, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<long[]> ReserveSerialsAsync(int count, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<string?> LoadDocumentAsync(int person, PersonStateKind kind, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Dictionary<PersonStateKind, string>> LoadDocumentsAsync(int person, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    /// <summary>
    /// A new item whose write is given up may still be in the database, since only the answer may have been lost: destroyed
    /// after, it is deleted there too, and does not come back at the next entry.
    /// </summary>
    [TestMethod]
    public async Task AnItemGivenBackAfterALostAnswerIsDeletedWhenItIsDestroyed()
    {
        using var world = World.Create();
        using var loop = new WorldLoop(NullLogger<WorldLoop>.Instance);

        var time = new FakeTimeProvider();
        var store = new LostAnswers { Lost = int.MaxValue };
        var snapshots = new PersonSnapshots(world, [], []);
        var writer = new PersistenceWriter(store, loop, snapshots, NullLogger<PersistenceWriter>.Instance, time: time);
        var flush = new PersonFlush(world, snapshots, writer);

        await writer.StartAsync(CancellationToken.None);

        var player = await loop.InvokeAsync(() =>
        {
            var dirty = new PersonDirty(7);
            var bag = new Inventory(7) { Dirty = dirty };

            bag.Add(StorageGroup.Common, 4);
            bag.Place(StorageGroup.Common, 0, new ItemInstance { Serial = 81, Item = 1, Count = 1 });

            return world.Create(new DirtyComponent(dirty), new InventoryComponent(bag), new LevelComponent(1, 0), new CurrencyComponent());
        });

        var first = loop.InvokeAsync(() => flush.FlushAsync(player).AsTask()).Unwrap();

        await MetricAssertions.UntilAsync(() =>
        {
            time.Advance(PersistenceWriter.RetryDelay);

            return first.IsCompleted;
        }, "the write is given up after its attempts");

        Assert.IsFalse(await first);
        Assert.IsTrue(store.Written.All(e => e.Added.Single().Id == 81), "every attempt reached the database");

        store.Lost = 0;

        var second = await loop.InvokeAsync(() =>
        {
            world.Get<InventoryComponent>(player).Value!.Destroy(StorageGroup.Common, 0);

            return flush.FlushAsync(player).AsTask();
        }).Unwrap();

        Assert.IsTrue(second);
        CollectionAssert.Contains(store.Written[^1].Removed.ToArray(), 81L, "the destroyed item is deleted from the database");

        await writer.StopAsync(CancellationToken.None);
    }
}
