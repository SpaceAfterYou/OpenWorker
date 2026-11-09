using System.Globalization;
using OpenWorker.Hotspot.Cache.Types;
using StackExchange.Redis;

namespace OpenWorker.Hotspot.Cache;

/// <summary>
/// The maze servers of each gate in a sorted set by their players, next to their records (<see cref="MazeCache"/>), so
/// that the least busy one is one call away instead of a search that returns them all. A member is the identifier of
/// the server; its record expires when the server stops renewing it, and the pick drops a member whose record is gone.
/// </summary>
public sealed class MazeDirectory(IConnectionMultiplexer redis)
{
    /// <summary>
    /// The key of the record of a maze server, as Redis.OM names it.
    /// </summary>
    private static string RecordPrefix { get; } = typeof(MazeCache).FullName + ":";

    /// <summary>
    /// The server with the fewest players whose record is still there: its host and port, or nothing.
    /// </summary>
    private static RedisScript PickLeastBusy { get; } = new("""
        for _, member in ipairs(redis.call('ZRANGE', KEYS[1], 0, -1)) do
            local address = redis.call('HMGET', ARGV[1] .. member, 'Host', 'Port')
            if address[1] then
                return address
            end
            redis.call('ZREM', KEYS[1], member)
        end
        return false
        """);

    public static RedisKey Key(short gate) => "ow:mazes:" + gate.ToString(CultureInfo.InvariantCulture);

    public Task RecordAsync(short gate, Guid instance, int players)
    {
        return redis.GetDatabase().SortedSetAddAsync(Key(gate), Member(instance), players);
    }

    public Task RemoveAsync(short gate, Guid instance)
    {
        return redis.GetDatabase().SortedSetRemoveAsync(Key(gate), Member(instance));
    }

    public async Task<(string Host, short Port)?> PickAsync(short gate)
    {
        var result = await PickLeastBusy.EvaluateAsync(redis, [Key(gate)], [RecordPrefix]);

        if (result.IsNull || (RedisResult[]?)result is not [var hostValue, var portValue])
        {
            return null;
        }

        return short.TryParse((string?)portValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port) && (string?)hostValue is { } host
            ? (host, port)
            : null;
    }

    /// <summary>
    /// The identifier as Redis.OM writes it into the key of the record.
    /// </summary>
    private static RedisValue Member(Guid instance) => instance.ToString();
}
