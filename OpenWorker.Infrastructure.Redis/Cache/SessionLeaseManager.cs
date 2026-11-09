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
    /// <see cref="AttachAsync"/>), so the renewal only moves the expiry.
    /// </summary>
    private static RedisScript RenewIfOwner { get; } = new("""
        local renewed = 0
        for index, key in ipairs(KEYS) do
            if redis.call('HGET', key, 'Owner') == ARGV[index + 1] then
                redis.call('PEXPIRE', key, ARGV[1])
                renewed = renewed + 1
            end
        end
        return renewed
        """);

    private ConcurrentDictionary<Entity, Lease> Leases { get; } = new();

    public async Task AttachAsync(Entity player, long session, int account)
    {
        var lease = new Lease(session, account, Guid.NewGuid().ToString("N"), false);

        Leases[player] = lease;

        await WriteAsync(lease, true, ConnectedLease);
    }

    /// <summary>
    /// Lets the next connection to the auth server continue this session without a login.
    /// </summary>
    public async Task MarkReturnToAuthAsync(Entity player)
    {
        if (!Leases.TryGetValue(player, out var lease))
        {
            return;
        }

        lease = lease with { ReturnToAuth = true };

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
            var keys = new RedisKey[count];
            var values = new RedisValue[count + 1];

            values[0] = (long)ConnectedLease.TotalMilliseconds;

            for (var index = 0; index < count; index++)
            {
                var lease = leases[start + index];

                keys[index] = KeyPrefix + lease.Session.ToString(CultureInfo.InvariantCulture);
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
                ReturnToAuth = lease.ReturnToAuth
            }, duration);
    }

    private sealed record Lease(long Session, int Account, string Owner, bool ReturnToAuth);
}
