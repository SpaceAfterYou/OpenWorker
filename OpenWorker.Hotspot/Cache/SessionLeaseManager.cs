using System.Collections.Concurrent;
using Arch.Core;
using OpenWorker.Hotspot.Cache.Types;
using Redis.OM;
using Redis.OM.Searching;

namespace OpenWorker.Hotspot.Cache;

/// <summary>
/// Keeps a login session alive while a server holds its connection.<br/>
/// The client drops its connection on every move between servers, so a closed connection only
/// shortens the lease: the next server takes the session over before it expires. If a server
/// crashes, its sessions expire because nothing renews them.
/// </summary>
public sealed class SessionLeaseManager(IRedisCollection<SessionCache> sessions) : ISessionCloseHandler
{
    public static TimeSpan ConnectedLease { get; } = TimeSpan.FromSeconds(90);
    public static TimeSpan DisconnectedLease { get; } = TimeSpan.FromSeconds(60);
    public static TimeSpan RenewInterval { get; } = TimeSpan.FromSeconds(30);

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

    public async Task RenewAsync()
    {
        foreach (var lease in Leases.Values)
        {
            var current = await sessions
                .FindByIdAsync(lease.Session.ToString());

            // Another connection took this session over, or a new login replaced it.
            if (current?.Owner != lease.Owner)
            {
                continue;
            }

            await WriteAsync(lease, true, ConnectedLease);
        }
    }

    public async ValueTask OnSessionClosedAsync(Entity player)
    {
        if (!Leases.TryRemove(player, out var lease))
        {
            return;
        }

        var current = await sessions
            .FindByIdAsync(lease.Session.ToString());

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
