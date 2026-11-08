using StackExchange.Redis;

namespace OpenWorker.Hotspot.Cache;

/// <summary>
/// A Lua script that is sent once (<c>SCRIPT LOAD</c>) and then run by its hash (<c>EVALSHA</c>), so a call carries the
/// keys and the arguments only. A Redis that has forgotten it (a restart, <c>SCRIPT FLUSH</c>) answers <c>NOSCRIPT</c>,
/// and the script is loaded again and the call repeated once.
/// </summary>
public sealed class RedisScript(string text)
{
    private byte[]? _hash;

    public string Text { get; } = text;

    public async Task<RedisResult> EvaluateAsync(IConnectionMultiplexer redis, RedisKey[] keys, RedisValue[] values)
    {
        var database = redis.GetDatabase();
        var hash = _hash ?? await LoadAsync(redis);

        try
        {
            return await database.ScriptEvaluateAsync(hash, keys, values);
        }

        catch (RedisServerException e) when (e.Message.StartsWith("NOSCRIPT", StringComparison.Ordinal))
        {
            return await database.ScriptEvaluateAsync(await LoadAsync(redis), keys, values);
        }
    }

    private async Task<byte[]> LoadAsync(IConnectionMultiplexer redis)
    {
        byte[]? hash = null;

        foreach (var endpoint in redis.GetEndPoints())
        {
            hash = await redis.GetServer(endpoint).ScriptLoadAsync(Text);
        }

        return _hash = hash ?? throw new InvalidOperationException("Redis has no endpoint to load the script on.");
    }
}
