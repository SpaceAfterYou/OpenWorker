using System.Globalization;
using OpenWorker.Domain.Stores;
using StackExchange.Redis;

namespace OpenWorker.Hotspot.Cache;

/// <summary>
/// The trade password state of a login in Redis, next to the lease of its session: a hash of three fields at
/// <see cref="Key"/>. The record lives as long as the lease does: every write gives it the length of a connected lease, the
/// renewal of the lease (<see cref="SessionLeaseManager"/>) extends it with the lease, and the close of the last connection
/// shortens it with the lease. A count of wrong answers and the lock it leads to are each one script, so the servers of one
/// login (a district and a maze, one after the other) never lose an answer to a race.
/// </summary>
public sealed class RedisTradePasswordStates(IConnectionMultiplexer redis) : ITradePasswordStates
{
    private const string Authenticated = "Authenticated";
    private const string Locked = "Locked";
    private const string Failed = "Failed";

    /// <summary>
    /// The key of the record of a session.
    /// </summary>
    public static RedisKey Key(long session) => "ow:trade-password:" + session.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// The right password: given unless the record is locked. Answers { authenticated, locked, failed }.
    /// </summary>
    private static RedisScript Authenticate { get; } = new("""
        if redis.call('HGET', KEYS[1], 'Locked') ~= '1' then
            redis.call('HSET', KEYS[1], 'Authenticated', 1, 'Failed', 0)
            redis.call('PEXPIRE', KEYS[1], ARGV[1])
        end
        local record = redis.call('HMGET', KEYS[1], 'Authenticated', 'Locked', 'Failed')
        return { tonumber(record[1]) or 0, tonumber(record[2]) or 0, tonumber(record[3]) or 0 }
        """);

    /// <summary>
    /// A wrong password: one more wrong answer, the given password is taken back, and the limit locks the record. Answers
    /// { authenticated, locked, failed }.
    /// </summary>
    private static RedisScript Fail { get; } = new("""
        local failed = redis.call('HINCRBY', KEYS[1], 'Failed', 1)
        redis.call('HSET', KEYS[1], 'Authenticated', 0)
        if failed >= tonumber(ARGV[2]) then
            redis.call('HSET', KEYS[1], 'Locked', 1)
        end
        redis.call('PEXPIRE', KEYS[1], ARGV[1])
        return { 0, tonumber(redis.call('HGET', KEYS[1], 'Locked')) or 0, failed }
        """);

    public async Task<TradePasswordRecord?> LoadAsync(long session, CancellationToken cancellationToken = default)
    {
        var values = await redis.GetDatabase().HashGetAsync(Key(session), [Authenticated, Locked, Failed]);

        return values[2].IsNull ? null : Read(values[0], values[1], values[2]);
    }

    public async Task<TradePasswordRecord> AuthenticateAsync(long session, CancellationToken cancellationToken = default)
    {
        var result = await Authenticate.EvaluateAsync(redis, [Key(session)], [Lease()]);

        return Read(result);
    }

    public async Task<TradePasswordRecord> FailAsync(long session, int limit, CancellationToken cancellationToken = default)
    {
        var result = await Fail.EvaluateAsync(redis, [Key(session)], [Lease(), limit]);

        return Read(result);
    }

    private static RedisValue Lease() => (long)SessionLeaseManager.ConnectedLease.TotalMilliseconds;

    private static TradePasswordRecord Read(RedisResult result)
    {
        return (RedisResult[]?)result is [var authenticated, var locked, var failed]
            ? Read((RedisValue)authenticated, (RedisValue)locked, (RedisValue)failed)
            : new TradePasswordRecord(false, false, 0);
    }

    private static TradePasswordRecord Read(RedisValue authenticated, RedisValue locked, RedisValue failed)
    {
        return new TradePasswordRecord((long)authenticated == 1, (long)locked == 1, (int)failed);
    }
}
