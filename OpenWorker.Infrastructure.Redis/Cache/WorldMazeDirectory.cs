using System.Globalization;
using StackExchange.Redis;

namespace OpenWorker.Hotspot.Cache;

/// <summary>
/// The instance an open maze of the world plays in for a channel, and how many players are in it.
/// </summary>
public readonly record struct WorldMazeRoute(Guid Instance, Guid Server, string Host, short Port, int Players);

/// <summary>
/// Where the open maze of the world (District 6) is played on each channel: the maze server and the instance on it, so that
/// everybody who comes on that channel, from any district, is sent into the same one. The record decides alone: a server that
/// sends a player in takes the instance it names, an instance keeps the record while it runs and gives it back when it ends,
/// and each of these is one script, so two servers that ask at once get one answer.
/// </summary>
public sealed class WorldMazeDirectory(IConnectionMultiplexer redis)
{
    /// <summary>
    /// How long a record lives without the instance that holds it renewing it.
    /// </summary>
    public static TimeSpan Lifetime { get; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// How often an instance renews its record.
    /// </summary>
    public static TimeSpan RenewInterval { get; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The key of the record of a maze server, as Redis.OM names it: without it the server is gone.
    /// </summary>
    private static string ServerPrefix { get; } = typeof(Types.MazeCache).FullName + ":";

    /// <summary>
    /// The record as it stands, counting the player who is sent in, when its server is still there and it has room; nothing
    /// when it is full; otherwise a new instance on the server picked by the caller.
    /// </summary>
    private static RedisScript Route { get; } = new("""
        local r = redis.call('HMGET', KEYS[1], 'instance', 'server', 'host', 'port', 'players')
        if r[1] and (r[2] == '' or redis.call('EXISTS', ARGV[1] .. r[2]) == 1) then
            if tonumber(r[5] or '0') >= tonumber(ARGV[7]) then
                return false
            end
            r[5] = tostring(redis.call('HINCRBY', KEYS[1], 'players', 1))
            return r
        end
        redis.call('DEL', KEYS[1])
        redis.call('HSET', KEYS[1], 'instance', ARGV[2], 'server', ARGV[3], 'host', ARGV[4], 'port', ARGV[5], 'players', '1')
        redis.call('PEXPIRE', KEYS[1], ARGV[6])
        return { ARGV[2], ARGV[3], ARGV[4], ARGV[5], '1' }
        """);

    /// <summary>
    /// The instance keeps the record when it names it or names nothing, with the number of players it has; it is told no when
    /// the record names another instance.
    /// </summary>
    private static RedisScript Hold { get; } = new("""
        local r = redis.call('HGET', KEYS[1], 'instance')
        if r and r ~= ARGV[1] then
            return 0
        end
        redis.call('HSET', KEYS[1], 'instance', ARGV[1], 'server', ARGV[2], 'host', ARGV[3], 'port', ARGV[4], 'players', ARGV[5])
        redis.call('PEXPIRE', KEYS[1], ARGV[6])
        return 1
        """);

    /// <summary>
    /// The record goes only when it still names the instance.
    /// </summary>
    private static RedisScript Release { get; } = new("""
        if redis.call('HGET', KEYS[1], 'instance') == ARGV[1] then
            return redis.call('DEL', KEYS[1])
        end
        return 0
        """);

    public static RedisKey Key(short gate, short location, int channel) =>
        string.Create(CultureInfo.InvariantCulture, $"ow:world:{gate}-{location}-{channel}");

    /// <summary>
    /// The instance the maze plays in on the channel, counting the player sent in; a new instance on <paramref name="server"/>
    /// when there is none or its server is gone. Nothing when it already has <paramref name="capacity"/> players.
    /// </summary>
    public async Task<WorldMazeRoute?> RouteAsync(short gate, short location, int channel, (Guid Server, string Host, short Port) server,
        int capacity)
    {
        var instance = Guid.NewGuid();

        var result = await Route.EvaluateAsync(redis, [Key(gate, location, channel)],
        [
            ServerPrefix, Text(instance), Text(server.Server), server.Host, Number(server.Port), (long)Lifetime.TotalMilliseconds,
            Number(capacity)
        ]);

        return Read(result);
    }

    /// <summary>
    /// The instance renews the record with the players it has. False when the record names another instance.
    /// </summary>
    public async Task<bool> HoldAsync(short gate, short location, int channel, Guid instance, (Guid Server, string Host, short Port) server,
        int players)
    {
        var result = await Hold.EvaluateAsync(redis, [Key(gate, location, channel)],
        [
            Text(instance), Text(server.Server), server.Host, Number(server.Port), Number(players), (long)Lifetime.TotalMilliseconds
        ]);

        return (long)result == 1;
    }

    /// <summary>
    /// The instance ended: the record goes, if it still names it.
    /// </summary>
    public Task ReleaseAsync(short gate, short location, int channel, Guid instance)
    {
        return Release.EvaluateAsync(redis, [Key(gate, location, channel)], [Text(instance)]);
    }

    private static WorldMazeRoute? Read(RedisResult result)
    {
        if (result.IsNull || (RedisResult[]?)result is not [var instance, var server, var host, var port, var players])
        {
            return null;
        }

        return new WorldMazeRoute(
            Guid.TryParse((string?)instance, CultureInfo.InvariantCulture, out var parsedInstance) ? parsedInstance : Guid.Empty,
            Guid.TryParse((string?)server, CultureInfo.InvariantCulture, out var parsedServer) ? parsedServer : Guid.Empty,
            (string?)host ?? string.Empty,
            short.TryParse((string?)port, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPort) ? parsedPort : (short)0,
            int.TryParse((string?)players, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPlayers) ? parsedPlayers : 0);
    }

    /// <summary>
    /// An identifier as the key of the record of a maze server writes it; the empty one is no server.
    /// </summary>
    private static string Text(Guid value) => value == Guid.Empty ? string.Empty : value.ToString();

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
