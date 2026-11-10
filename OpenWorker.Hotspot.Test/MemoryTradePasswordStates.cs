using OpenWorker.Domain.Stores;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The trade password states of the logins of a test that needs no Redis: the same rules as the Redis ones, in a dictionary.
/// </summary>
internal sealed class MemoryTradePasswordStates : ITradePasswordStates
{
    private readonly Dictionary<long, TradePasswordRecord> _records = [];

    public Task<TradePasswordRecord?> LoadAsync(long session, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_records.TryGetValue(session, out var record) ? record : (TradePasswordRecord?)null);
    }

    public Task<TradePasswordRecord> AuthenticateAsync(long session, CancellationToken cancellationToken = default)
    {
        var record = _records.GetValueOrDefault(session);

        return Task.FromResult(_records[session] = record.Locked ? record : new TradePasswordRecord(true, false, 0));
    }

    public Task<TradePasswordRecord> FailAsync(long session, int limit, CancellationToken cancellationToken = default)
    {
        var failed = _records.GetValueOrDefault(session).Failed + 1;

        return Task.FromResult(_records[session] = new TradePasswordRecord(false, failed >= limit || _records.GetValueOrDefault(session).Locked, failed));
    }
}
