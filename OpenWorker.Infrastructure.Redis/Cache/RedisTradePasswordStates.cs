using System.Globalization;
using OpenWorker.Domain.Stores;
using StackExchange.Redis;

namespace OpenWorker.Hotspot.Cache;

/// <summary>
/// Whether the trade password was given on a login, in Redis next to the lease of its session: a key that is set once the
/// right password is given and gone when a wrong one takes it back. The key lives as long as the lease does: every write
/// gives it the length of a connected lease, the renewal of the lease (<see cref="SessionLeaseManager"/>) extends it with
/// the lease, and the close of the last connection shortens it with the lease. A new login finds nothing: the player gives
/// the password again.
/// </summary>
public sealed class RedisTradePasswordStates(IConnectionMultiplexer redis) : ITradePasswordStates
{
    /// <summary>
    /// The key of a session.
    /// </summary>
    public static RedisKey Key(long session) => "ow:trade-password-given:" + session.ToString(CultureInfo.InvariantCulture);

    public async Task<bool> IsGivenAsync(long session, CancellationToken cancellationToken = default)
    {
        return await redis.GetDatabase().KeyExistsAsync(Key(session));
    }

    public async Task GiveAsync(long session, CancellationToken cancellationToken = default)
    {
        await redis.GetDatabase().StringSetAsync(Key(session), 1, SessionLeaseManager.ConnectedLease);
    }

    public async Task TakeBackAsync(long session, CancellationToken cancellationToken = default)
    {
        await redis.GetDatabase().KeyDeleteAsync(Key(session));
    }
}
