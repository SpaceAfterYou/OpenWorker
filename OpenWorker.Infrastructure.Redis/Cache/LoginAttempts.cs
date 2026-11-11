using StackExchange.Redis;

namespace OpenWorker.Hotspot.Cache;

/// <summary>
/// Counts the wrong passwords given for a name, on every auth server together. After <see cref="Limit"/> of them within
/// <see cref="Window"/> the name is not tried again until the window is over, whatever the password: guessing a password
/// costs the window for every few tries. A right password before the limit forgets the count.
/// </summary>
public sealed class LoginAttempts(IConnectionMultiplexer redis)
{
    public const int Limit = 10;

    public static TimeSpan Window { get; } = TimeSpan.FromMinutes(10);

    // KEYS: the count of the name. ARGV: the window in ms. The window starts with the first wrong password.
    private static RedisScript Count { get; } = new("""
        local failed = redis.call('INCR', KEYS[1])
        if failed == 1 then
            redis.call('PEXPIRE', KEYS[1], ARGV[1])
        end
        return failed
        """);

    /// <summary>
    /// Whether the name has had its wrong passwords for now.
    /// </summary>
    public async Task<bool> IsClosedAsync(string username)
    {
        var failed = await redis.GetDatabase().StringGetAsync(Key(username));

        return failed.TryParse(out int count) && count >= Limit;
    }

    public async Task FailedAsync(string username)
    {
        await Count.EvaluateAsync(redis, [Key(username)], [(long)Window.TotalMilliseconds]);
    }

    public Task SucceededAsync(string username)
    {
        return redis.GetDatabase().KeyDeleteAsync(Key(username));
    }

    private static RedisKey Key(string username) => "ow:login-failed:" + username.Trim().ToLowerInvariant();
}
