using OpenWorker.Domain.Stores;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The trade password states of the logins of a test that needs no Redis: the same rules as the Redis ones, in a set.
/// </summary>
internal sealed class MemoryTradePasswordStates : ITradePasswordStates
{
    private readonly HashSet<long> _given = [];

    public Task<bool> IsGivenAsync(long session, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_given.Contains(session));
    }

    public Task GiveAsync(long session, CancellationToken cancellationToken = default)
    {
        _given.Add(session);

        return Task.CompletedTask;
    }

    public Task TakeBackAsync(long session, CancellationToken cancellationToken = default)
    {
        _given.Remove(session);

        return Task.CompletedTask;
    }
}

/// <summary>
/// The wrong tries of the accounts of a test that needs no database: the same rules as the database ones, in a dictionary.
/// </summary>
internal sealed class MemoryTradePasswordTries : ITradePasswordTries
{
    private readonly Dictionary<int, TradePasswordTries> _tries = [];

    public Task<TradePasswordTries> LoadAsync(int account, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_tries.GetValueOrDefault(account));
    }

    public Task<int> FailAsync(int account, DateTime now, CancellationToken cancellationToken = default)
    {
        var failed = _tries.GetValueOrDefault(account).Failed + 1;

        _tries[account] = new TradePasswordTries(failed, now);

        return Task.FromResult(failed);
    }

    public Task ClearAsync(int account, CancellationToken cancellationToken = default)
    {
        _tries.Remove(account);

        return Task.CompletedTask;
    }

    public Task ResetAsync(int account, CancellationToken cancellationToken = default)
    {
        _tries.Remove(account);

        return Task.CompletedTask;
    }
}
