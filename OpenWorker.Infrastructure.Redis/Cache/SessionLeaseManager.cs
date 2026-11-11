using System.Collections.Concurrent;
using System.Globalization;
using Arch.Core;
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
public sealed class SessionLeaseManager(IRedisCollection<SessionCache> sessions, IConnectionMultiplexer redis) : ISessionCloseHandler
{
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

    public async Task AttachAsync(Entity player, long session, int account)
    {
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
