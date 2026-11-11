using System.Collections.Concurrent;
using System.Globalization;
using Arch.Core;
using Microsoft.Extensions.Options;
using OpenWorker.Hotspot.Cache.Types;
using Redis.OM;
using Redis.OM.Searching;
using StackExchange.Redis;

namespace OpenWorker.Hotspot.Cache;

/// <summary>
/// Keeps a login session alive while a server holds its connection.<br/>
/// The client drops its connection on every move between servers, so a closed connection only
/// shortens the lease: the next server takes the session over before it expires. If a server
/// crashes, its sessions expire because nothing renews them.
/// </summary>
public sealed class SessionLeaseManager(
    IRedisCollection<SessionCache> sessions,
    IConnectionMultiplexer redis,
    World? world = null,
    WorldLoop? loop = null,
    IOptions<SessionLeaseOptions>? options = null) : ISessionCloseHandler
{
    /// <summary>
    /// The channel on which a server asks the holder of a session to let its player go.
    /// </summary>
    private static RedisChannel TakeoverChannel { get; } = RedisChannel.Literal("ow:session:takeover");

    /// <summary>
    /// How long a server waits for the server a person plays on to let them go, and how often it looks.
    /// </summary>
    public static TimeSpan TakeoverWait { get; } = TimeSpan.FromSeconds(8);

    private static TimeSpan TakeoverStep { get; } = TimeSpan.FromMilliseconds(100);

    private bool Play { get; } = options?.Value.Play ?? false;

    private Task? _listening;

    /// <summary>
    /// Makes this connection the holder of the session unless a server the person plays on holds it. Returns 1 when it is
    /// taken, -1 when there is no such session, and 0 with the holder when the holder has to let go first.
    /// </summary>
    private static RedisScript Claim { get; } = new("""
        if redis.call('EXISTS', KEYS[1]) == 0 then
            return {-1}
        end
        local owner = redis.call('HGET', KEYS[1], 'Owner')
        if owner and owner ~= ARGV[1] and redis.call('HGET', KEYS[1], 'Held') == '1' and redis.call('HGET', KEYS[1], 'Play') == '1' then
            return {0, owner}
        end
        redis.call('HSET', KEYS[1], 'Owner', ARGV[1], 'Held', '1', 'Play', ARGV[2])
        return {1}
        """);

    public static TimeSpan ConnectedLease { get; } = TimeSpan.FromSeconds(90);
    public static TimeSpan DisconnectedLease { get; } = TimeSpan.FromSeconds(60);
    public static TimeSpan RenewInterval { get; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How many leases one call renews: a thousand players are two round trips, and no call holds Redis for long.
    /// </summary>
    public const int RenewBatch = 500;

    /// <summary>
    /// The key of the record of a session, as Redis.OM names it.
    /// </summary>
    private static string KeyPrefix { get; } = typeof(SessionCache).FullName + ":";

    /// <summary>
    /// Renews every lease whose record still names this connection as its owner; another server that took the session
    /// over, or a new login that replaced it, keeps its own. The record holds the rest already (written by
    /// <see cref="AttachAsync"/>), so the renewal only moves the expiry. Each lease comes with the key of the trade password
    /// state of its session, which lives exactly as long as the lease does (<see cref="RedisTradePasswordStates"/>).
    /// </summary>
    private static RedisScript RenewIfOwner { get; } = new("""
        local renewed = 0
        for index = 1, #KEYS, 2 do
            if redis.call('HGET', KEYS[index], 'Owner') == ARGV[(index + 1) / 2 + 1] then
                redis.call('PEXPIRE', KEYS[index], ARGV[1])
                redis.call('PEXPIRE', KEYS[index + 1], ARGV[1])
                renewed = renewed + 1
            end
        end
        return renewed
        """);

    private ConcurrentDictionary<Entity, Lease> Leases { get; } = new();

    /// <summary>
    /// Takes over a session that exists for the connection of the player. A person is in one world at a time: while a
    /// district or a maze still holds the session, that server is asked to let the person go, which it does as a
    /// connection that closes (its last changes are written first), and this one waits for it. False when there is no
    /// such session, or its holder did not let go in time; the connection is then to be refused.
    /// </summary>
    public async Task<bool> TakeOverAsync(Entity player, long session, int account)
    {
        var lease = new Lease(session, account, Guid.NewGuid().ToString("N"), false, null);
        var key = (RedisKey)(KeyPrefix + session.ToString(CultureInfo.InvariantCulture));
        var asked = false;

        await ListenAsync();

        for (var waited = TimeSpan.Zero; ; waited += TakeoverStep)
        {
            var result = (RedisResult[])(await Claim.EvaluateAsync(redis, [key], [lease.Owner, Play ? 1 : 0]))!;

            switch ((int)result[0])
            {
                case 1:
                    Leases[player] = lease;

                    await WriteAsync(lease, true, ConnectedLease);

                    return true;

                case -1:
                    return false;
            }

            if (waited >= TakeoverWait)
            {
                return false;
            }

            if (!asked)
            {
                asked = true;

                await redis.GetSubscriber().PublishAsync(TakeoverChannel, (string)result[1]!);
            }

            await Task.Delay(TakeoverStep);
        }
    }

    /// <summary>
    /// Starts listening for the servers that ask this one to let a player go. Without a world there is nobody to let go.
    /// </summary>
    private Task ListenAsync()
    {
        if (world is null || loop is null)
        {
            return Task.CompletedTask;
        }

        return _listening ??= redis.GetSubscriber().SubscribeAsync(TakeoverChannel, (_, owner) => loop.Post(_ => LetGo((string?)owner), null));
    }

    /// <summary>
    /// Closes the connection of the lease another server asks for, if it is one of this server: the close writes what the
    /// person changed and then gives the session up, as any close does.
    /// </summary>
    private void LetGo(string? owner)
    {
        foreach (var (player, lease) in Leases)
        {
            if (lease.Owner == owner && world!.IsAlive(player) && world.TryGet(player, out ServerSessionComponent session))
            {
                session.Disconnect();
            }
        }
    }

    public async Task AttachAsync(Entity player, long session, int account)
    {
        await ListenAsync();

        var lease = new Lease(session, account, Guid.NewGuid().ToString("N"), false, null);

        Leases[player] = lease;

        await WriteAsync(lease, true, ConnectedLease);
    }

    /// <summary>
    /// Lets the next connection to the auth server continue this session without a login, if it comes from the address the
    /// player leaves from (<paramref name="endPoint"/>, the remote end of its connection here).
    /// </summary>
    public async Task MarkReturnToAuthAsync(Entity player, string? endPoint = null)
    {
        if (!Leases.TryGetValue(player, out var lease))
        {
            return;
        }

        lease = lease with { ReturnToAuth = true, ReturnAddress = HostOf(endPoint) };

        Leases[player] = lease;

        await WriteAsync(lease, true, ConnectedLease);
    }

    /// <summary>
    /// Renews the leases this server holds, <see cref="RenewBatch"/> in one round trip. Returns how many were renewed.
    /// </summary>
    public async Task<int> RenewAsync()
    {
        var leases = Leases.Values.ToArray();
        var renewed = 0;

        for (var start = 0; start < leases.Length; start += RenewBatch)
        {
            var count = Math.Min(RenewBatch, leases.Length - start);
            var keys = new RedisKey[count * 2];
            var values = new RedisValue[count + 1];

            values[0] = (long)ConnectedLease.TotalMilliseconds;

            for (var index = 0; index < count; index++)
            {
                var lease = leases[start + index];

                keys[index * 2] = KeyPrefix + lease.Session.ToString(CultureInfo.InvariantCulture);
                keys[(index * 2) + 1] = RedisTradePasswordStates.Key(lease.Session);
                values[index + 1] = lease.Owner;
            }

            renewed += (int)await RenewIfOwner.EvaluateAsync(redis, keys, values);
        }

        return renewed;
    }

    public async ValueTask OnSessionClosedAsync(Entity player)
    {
        if (!Leases.TryRemove(player, out var lease))
        {
            return;
        }

        var current = await sessions
            .FindByIdAsync(lease.Session.ToString(CultureInfo.InvariantCulture));

        if (current?.Owner == lease.Owner)
        {
            await WriteAsync(lease, false, DisconnectedLease);
        }
    }

    private async Task WriteAsync(Lease lease, bool connected, TimeSpan duration)
    {
        await sessions
            .InsertAsync(new SessionCache
            {
                Session = lease.Session,
                Account = lease.Account,
                UpdatedAt = DateTime.UtcNow,
                Connected = connected,
                Held = connected ? 1 : 0,
                Play = Play ? 1 : 0,
                Owner = lease.Owner,
                ReturnToAuth = lease.ReturnToAuth,
                ReturnAddress = lease.ReturnAddress
            }, duration);

        // What the servers of this login share about the trade password lasts as long as the lease; nothing happens when
        // the login has given no answer yet.
        await redis.GetDatabase().KeyExpireAsync(RedisTradePasswordStates.Key(lease.Session), duration);
    }

    /// <summary>
    /// The host of an end point as a socket prints it (<c>address:port</c>), or null when it is none.
    /// </summary>
    public static string? HostOf(string? endPoint)
    {
        return System.Net.IPEndPoint.TryParse(endPoint ?? string.Empty, out var parsed) ? parsed.Address.ToString() : null;
    }

    /// <summary>
    /// Whether a connection from <paramref name="endPoint"/> may continue the session that was marked for it: the mark
    /// names the address the player left from, and only that address continues it. A mark without an address, of a
    /// connection whose address was not known, is continued from anywhere.
    /// </summary>
    public static bool MayReturn(SessionCache session, string? endPoint)
    {
        return session.ReturnToAuth && (session.ReturnAddress is null || session.ReturnAddress == HostOf(endPoint));
    }

    private sealed record Lease(long Session, int Account, string Owner, bool ReturnToAuth, string? ReturnAddress);
}
