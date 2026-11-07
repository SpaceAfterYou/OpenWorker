using StackExchange.Redis;

namespace OpenWorker.Channel.Community;

/// <summary>
/// One key by person number and one by name, both holding the whole record, so that either kind of lookup is a single read.
/// </summary>
public sealed class RedisPresenceStore(IConnectionMultiplexer redis) : IPresenceStore
{
    private const string PersonPrefix = "ow:who:";
    private const string NamePrefix = "ow:who-name:";

    // The record starts with the instance as 32 hex digits.
    private const string RemoveIfOwned = """
        local value = redis.call('GET', KEYS[1])
        if value and string.sub(value, 1, 32) == ARGV[1] then
            return redis.call('DEL', KEYS[1])
        end
        return 0
        """;

    private IDatabase Database => redis.GetDatabase();

    public async ValueTask SetAsync(PresenceRecord record, TimeSpan lifetime)
    {
        var value = record.Encode();

        await Task.WhenAll(
            Database.StringSetAsync(PersonKey(record.Person), value, lifetime),
            Database.StringSetAsync(NameKey(record.Name), value, lifetime));
    }

    public async ValueTask RemoveAsync(PresenceRecord record)
    {
        var instance = (RedisValue)record.Instance.ToString("N");

        await Task.WhenAll(
            Database.ScriptEvaluateAsync(RemoveIfOwned, [PersonKey(record.Person)], [instance]),
            Database.ScriptEvaluateAsync(RemoveIfOwned, [NameKey(record.Name)], [instance]));
    }

    public async ValueTask<PresenceRecord?> GetAsync(int person)
    {
        return PresenceRecord.Decode(await Database.StringGetAsync(PersonKey(person)));
    }

    public async ValueTask<PresenceRecord?> GetByNameAsync(string name)
    {
        return PresenceRecord.Decode(await Database.StringGetAsync(NameKey(name)));
    }

    public async ValueTask<IReadOnlyList<PresenceRecord?>> GetManyAsync(IReadOnlyList<int> persons)
    {
        if (persons.Count == 0)
        {
            return [];
        }

        var values = await Database.StringGetAsync(persons.Select(PersonKey).ToArray());

        return values.Select(e => PresenceRecord.Decode(e)).ToArray();
    }

    private static RedisKey PersonKey(int person) => PersonPrefix + person;

    private static RedisKey NameKey(string name) => NamePrefix + PresenceRecord.NameKey(name);
}
