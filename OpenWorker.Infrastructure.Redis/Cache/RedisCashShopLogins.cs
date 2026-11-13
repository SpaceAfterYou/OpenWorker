using System.Globalization;
using OpenWorker.Domain.Stores;
using StackExchange.Redis;

namespace OpenWorker.Hotspot.Cache;

/// <summary>
/// Whether a login has been given the cash shop's window, as a key of its session in Redis. The key outlives most logins:
/// one that outlives the key is given the window again at its next entry, which its client takes without harm (it drops a
/// second list of products and replaces the balance and the sets).
/// </summary>
public sealed class RedisCashShopLogins(IConnectionMultiplexer redis) : ICashShopLogins
{
    private static TimeSpan Lifetime { get; } = TimeSpan.FromDays(1);

    /// <summary>
    /// The key of a session.
    /// </summary>
    public static RedisKey Key(long session) => "ow:cash-shop-given:" + session.ToString(CultureInfo.InvariantCulture);

    public async Task<bool> IsGivenAsync(long session, CancellationToken cancellationToken = default)
    {
        return await redis.GetDatabase().KeyExistsAsync(Key(session));
    }

    public async Task GiveAsync(long session, CancellationToken cancellationToken = default)
    {
        await redis.GetDatabase().StringSetAsync(Key(session), 1, Lifetime);
    }
}
