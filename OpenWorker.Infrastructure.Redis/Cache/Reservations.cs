using System.Globalization;
using StackExchange.Redis;

namespace OpenWorker.Hotspot.Cache;

/// <summary>
/// Uses up the reservation a server wrote for a person who is on the way to another server. A reservation lets one
/// connection in: of several that come with it at the same moment, only the one that takes it away enters.
/// </summary>
public sealed class Reservations(IConnectionMultiplexer redis)
{
    /// <summary>
    /// Takes the reservation of the kind <typeparamref name="T"/> with this number away. False when another connection
    /// has taken it already, or it has run out.
    /// </summary>
    public Task<bool> TakeAsync<T>(int identifier)
    {
        return redis.GetDatabase().KeyDeleteAsync(Key<T>(identifier));
    }

    /// <summary>
    /// The key the collection of <typeparamref name="T"/> keeps the record under.
    /// </summary>
    public static RedisKey Key<T>(int identifier) => typeof(T).FullName + ":" + identifier.ToString(CultureInfo.InvariantCulture);
}
