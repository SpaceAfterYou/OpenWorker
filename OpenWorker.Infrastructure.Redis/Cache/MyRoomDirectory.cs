using System.Globalization;
using OpenWorker.Hotspot.Cache.Types;
using StackExchange.Redis;

namespace OpenWorker.Hotspot.Cache;

/// <summary>
/// Where the room of an account is played: the maze server and the instance on it, so that the owner and every visitor, from
/// any district or room, are sent into the same one. The record decides alone: a server that sends a player in takes the
/// instance it names, an instance opens and keeps the room only while the record names it, and each of these is one script,
/// so two servers that ask at once get one answer. The record also tells how many visitors are in and whether the owner
/// arranges, for the server that sends a player in; the maze server decides at the door.
/// </summary>
public sealed class MyRoomDirectory(IConnectionMultiplexer redis)
{
    /// <summary>
    /// How long a record lives without the instance that holds it renewing it. A sent player who is on the way when it runs
    /// out takes it again where it lands.
    /// </summary>
    public static TimeSpan Lifetime { get; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// How often an instance renews the records of its rooms.
    /// </summary>
    public static TimeSpan RenewInterval { get; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The key of the record of a maze server, as Redis.OM names it: without it the server is gone.
    /// </summary>
    private static string ServerPrefix { get; } = typeof(MazeCache).FullName + ":";

    /// <summary>
    /// The record as it stands when the instance it names runs on a server that is still there (or on a server that does not
    /// register); otherwise a new one, on the server picked by the caller.
    /// </summary>
    private static RedisScript Route { get; } = new("""
        local r = redis.call('HMGET', KEYS[1], 'instance', 'server', 'host', 'port', 'arranging', 'visitors')
        if r[1] and (r[2] == '' or redis.call('EXISTS', ARGV[1] .. r[2]) == 1) then
            return r
        end
        redis.call('DEL', KEYS[1])
        redis.call('HSET', KEYS[1], 'instance', ARGV[2], 'holder', '', 'server', ARGV[3], 'host', ARGV[4], 'port', ARGV[5],
            'arranging', '0', 'visitors', '0')
        redis.call('PEXPIRE', KEYS[1], ARGV[6])
        return { ARGV[2], ARGV[3], ARGV[4], ARGV[5], '0', '0' }
        """);

    /// <summary>
    /// The instance holds the room when the record names it or names nothing: the record is written (a claim also names the
    /// holder) and lives on. When it names another instance, that one is answered.
    /// </summary>
    private static RedisScript Hold { get; } = new("""
        local r = redis.call('HMGET', KEYS[1], 'instance', 'server', 'host', 'port', 'arranging', 'visitors')
        if r[1] and r[1] ~= ARGV[1] then
            return r
        end
        if ARGV[9] == '1' or not r[1] then
            redis.call('HSET', KEYS[1], 'holder', ARGV[2])
        end
        redis.call('HSET', KEYS[1], 'instance', ARGV[1], 'server', ARGV[3], 'host', ARGV[4], 'port', ARGV[5],
            'arranging', ARGV[6], 'visitors', ARGV[7])
        redis.call('PEXPIRE', KEYS[1], ARGV[8])
        return false
        """);

    /// <summary>
    /// The record goes only when it still names the instance and the state of the room that held it.
    /// </summary>
    private static RedisScript Release { get; } = new("""
        local r = redis.call('HMGET', KEYS[1], 'instance', 'holder')
        if r[1] == ARGV[1] and r[2] == ARGV[2] then
            return redis.call('DEL', KEYS[1])
        end
        return 0
        """);

    public static RedisKey Key(int account, short gate) =>
        string.Create(CultureInfo.InvariantCulture, $"ow:room:{gate}-{account}");

    /// <summary>
    /// The instance the room plays in now, or a new instance on <paramref name="server"/> when the record is gone or names a
    /// server that is gone.
    /// </summary>
    public async Task<MyRoomCache> RouteAsync(int account, short gate, (Guid Server, string Host, short Port) server)
    {
        var instance = Guid.NewGuid();

        var result = await Route.EvaluateAsync(redis, [Key(account, gate)],
        [
            ServerPrefix, Text(instance), Text(server.Server), server.Host, Number(server.Port), (long)Lifetime.TotalMilliseconds
        ]);

        return Read(result) ?? new MyRoomCache(instance, server.Server, server.Host, server.Port, false, 0);
    }

    /// <summary>
    /// The instance takes the room as the state of the room is opened in it, under <paramref name="holder"/>, or keeps it and
    /// tells how it stands. Nothing when it holds the room; the record of the instance that does otherwise.
    /// </summary>
    public async Task<MyRoomCache?> HoldAsync(int account, short gate, Guid instance, Guid holder, (Guid Server, string Host, short Port) server,
        bool arranging, int visitors, bool claim)
    {
        var result = await Hold.EvaluateAsync(redis, [Key(account, gate)],
        [
            Text(instance), Text(holder), Text(server.Server), server.Host, Number(server.Port), arranging ? "1" : "0",
            Number(visitors), (long)Lifetime.TotalMilliseconds, claim ? "1" : "0"
        ]);

        return Read(result);
    }

    /// <summary>
    /// The state of the room in the instance ended: the record goes, if it is still the one this state held.
    /// </summary>
    public Task ReleaseAsync(int account, short gate, Guid instance, Guid holder)
    {
        return Release.EvaluateAsync(redis, [Key(account, gate)], [Text(instance), Text(holder)]);
    }

    private static MyRoomCache? Read(RedisResult result)
    {
        if (result.IsNull || (RedisResult[]?)result is not [var instance, var server, var host, var port, var arranging, var visitors])
        {
            return null;
        }

        return new MyRoomCache(
            Guid.TryParse((string?)instance, CultureInfo.InvariantCulture, out var parsedInstance) ? parsedInstance : Guid.Empty,
            Guid.TryParse((string?)server, CultureInfo.InvariantCulture, out var parsedServer) ? parsedServer : Guid.Empty,
            (string?)host ?? string.Empty,
            short.TryParse((string?)port, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPort) ? parsedPort : (short)0,
            (string?)arranging == "1",
            int.TryParse((string?)visitors, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedVisitors) ? parsedVisitors : 0);
    }

    /// <summary>
    /// An identifier as the key of the record of a maze server writes it; the empty one is no server.
    /// </summary>
    private static string Text(Guid value) => value == Guid.Empty ? string.Empty : value.ToString();

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
