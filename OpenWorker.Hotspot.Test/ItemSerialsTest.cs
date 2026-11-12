using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Modules.Items;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class ItemSerialsTest
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    /// <summary>
    /// A database that hands out serials only once the test lets it answer.
    /// </summary>
    private sealed class HeldStore : IPersonStore
    {
        private long _next;
        private int _asked;

        public TaskCompletionSource Answer { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Asked => Volatile.Read(ref _asked);

        public async Task<long[]> ReserveSerialsAsync(int count, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _asked);

            await Answer.Task;

            return Enumerable.Range(0, count).Select(_ => Interlocked.Increment(ref _next)).ToArray();
        }

        public Task<List<ItemPersistent>> LoadItemsAsync(int person, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<string?> LoadDocumentAsync(int person, PersonStateKind kind, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task SaveChangesAsync(IReadOnlyList<PersonChanges> batch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [TestMethod]
    public async Task ADrySupplyRefusesAtOnceInsteadOfWaitingForTheDatabase()
    {
        var store = new HeldStore();
        var serials = new DatabaseItemSerials(store);

        // The database does not answer. Whatever the loop asks for comes back at once: a refusal, never a wait.
        await Task.Run(() =>
        {
            Assert.IsFalse(serials.TryNextSerial(out _), "no serial is ready");
            Assert.IsFalse(serials.HasReady(1));
            Assert.ThrowsExactly<ItemSerialsExhaustedException>(() => serials.NextSerial());
        }).WaitAsync(Patience);

        // A request that can wait does, without holding anyone else up; it shares the one refill in flight.
        var waiting = serials.EnsureAsync(DatabaseItemSerials.Block + 1);

        Assert.IsFalse(waiting.IsCompleted, "the wait lasts until the database answers");

        store.Answer.SetResult();

        await waiting.AsTask().WaitAsync(Patience);

        Assert.IsTrue(serials.HasReady(DatabaseItemSerials.Block + 1));
        Assert.IsTrue(serials.TryNextSerial(out var serial));
        Assert.AreNotEqual(serial, serials.NextSerial(), "every serial is given once");
        Assert.AreEqual(2, store.Asked, "one query while the database was held, one more for a wait larger than a block");
    }
}
