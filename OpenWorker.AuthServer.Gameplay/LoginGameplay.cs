using Redis.OM;
using OpenWorker.Hotspot.Cache;
using Arch.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorker.Domain.Types;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot.Modules.Login.Enums;
using OpenWorker.Hotspot.Modules.Login.Requests;
using OpenWorker.Hotspot.Modules.Login.Responses;
using OpenWorker.Persistence;
using OpenWorker.Persistence.Utils;
using Redis.OM.Searching;

namespace OpenWorker.AuthServer.App;

public sealed class LoginGameplay(
    IRedisCollection<SessionCache> sessions,
    IDbContextFactory<PersistenceContext> factory,
    World world,
    IOptions<AuthOptions> auth,
    ILogger<LoginGameplay> logger,
    SessionLeaseManager leases)
{
    /// <summary>
    /// This login does not verify the ticket: it signs in as the first account in the database.
    /// Enable it only for local development.
    /// </summary>
    private bool AllowNextHumanNetworkLogin { get; } = auth.Value.AllowNextHumanNetworkLogin;

    public async ValueTask TryJoinAsync(ServiceHandleContext context, LoginAuthRequest message)
    {
        await using var database = await factory
            .CreateDbContextAsync(context.CancellationToken);

        var session = world.Get<ServerSessionComponent>(context.Player);

        var account = await database.Accounts
            .FirstOrDefaultAsync(e => e.Username == message.Username, context.CancellationToken);

        var password = message.Password;

        // Checking a password takes long enough to delay everybody else on the world loop.
        if (account is null || !await Task.Run(() => PasswordHash.Verify(password, account.PasswordHash, account.SaltHash)))
        {
            session.Send(new LoginResponse(LoginErrorMessageCode.WrongUsernameOrPassword));
            return;
        }

        await InternalLoginAsync(context, account.Id, account.Username, message.MacAddress);
    }

    public async ValueTask TryJoinAsync(ServiceHandleContext context, LoginNextHumanNetworkRequest message)
    {
        if (AllowNextHumanNetworkLogin is false)
        {
            logger.LogWarning("Next Human Network login rejected: Auth:AllowNextHumanNetworkLogin is off.");

            var rejected = world.Get<ServerSessionComponent>(context.Player);

            rejected.Send(new LoginResponse(LoginErrorMessageCode.WrongUsernameOrPassword));
            rejected.Disconnect();

            return;
        }

        await using var database = await factory
            .CreateDbContextAsync(context.CancellationToken);

        var account = await database.Accounts
            .FirstOrDefaultAsync(context.CancellationToken);

        if (account is null)
        {
            var session = world.Get<ServerSessionComponent>(context.Player);

            session.Send(new LoginResponse(LoginErrorMessageCode.BanAccount));
            session.Disconnect();

            return;
        }

        await InternalLoginAsync(context, account.Id, account.Username, message.MacAddress);
    }

    public async ValueTask SaveOptionsAsync(ServiceHandleContext context, IReadOnlyCollection<byte> options)
    {
        var account = world.Get<ClaimsComponent>(context.Player).Account;

        if (account == 0)
        {
            return;
        }

        await factory
            .SavePersonOptionsAsync(account, options.ToArray(), context.CancellationToken);
    }

    private async ValueTask InternalLoginAsync(ServiceHandleContext context, int account, string username, string mac)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        var active = await sessions
            .Where(e => e.Account == account)
            .ToListAsync();

        if (active.Any(e => e.Connected))
        {
            session.Send(new LoginResponse(LoginErrorMessageCode.InGameAlready));
            return;
        }

        // Nothing holds these sessions any more, the new login replaces them.
        foreach (var previous in active)
        {
            await sessions
                .DeleteAsync(previous);
        }

        var claims = new SessionValue(account);

        world.Set(context.Player, new ClaimsComponent(claims));

        world.SetLogAccount(context.Player, claims.Account);

        await leases
            .AttachAsync(context.Player, claims.Key, claims.Account);

        session.Send(new LoginResponse(claims, username, true, mac, LoginType.NewUser));
    }
}
