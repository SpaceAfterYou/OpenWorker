using Redis.OM;
using OpenWorker.Hotspot.Cache;
using Arch.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Types;
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
    IConfiguration configuration,
    ILogger<LoginGameplay> logger,
    SessionLeaseManager leases)
{
    /// <summary>
    /// This login does not verify the ticket: it signs in as the first account in the database.
    /// Enable it only for local development.
    /// </summary>
    private bool AllowNextHumanNetworkLogin { get; } = configuration.GetValue<bool>("Auth:AllowNextHumanNetworkLogin");

    public async ValueTask TryJoinAsync(ServiceHandleContext context, LoginAuthRequest message)
    {
        await using var database = await factory
            .CreateDbContextAsync(context.CancellationToken)
            .ConfigureAwait(false);

        var session = world.Get<ServerSessionComponent>(context.Player);

        var account = await database.Accounts
            .FirstOrDefaultAsync(e => e.Username == message.Username, context.CancellationToken)
            .ConfigureAwait(false);

        if (account is null || PasswordHash.Verify(message.Password, account.PasswordHash, account.SaltHash) is false)
        {
            session.Send(new LoginResponse(LoginErrorMessageCode.WrongUsernameOrPassword));
            return;
        }

        await InternalLoginAsync(context, account.Id, account.Username, message.MacAddress).ConfigureAwait(false);
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
            .CreateDbContextAsync(context.CancellationToken)
            .ConfigureAwait(false);

        var account = await database.Accounts
            .FirstOrDefaultAsync(context.CancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            var session = world.Get<ServerSessionComponent>(context.Player);

            session.Send(new LoginResponse(LoginErrorMessageCode.BanAccount));
            session.Disconnect();

            return;
        }

        await InternalLoginAsync(context, account.Id, account.Username, message.MacAddress).ConfigureAwait(false);
    }

    public async ValueTask SaveOptionsAsync(ServiceHandleContext context, IReadOnlyCollection<byte> options)
    {
        var account = world.Get<ClaimsComponent>(context.Player).Account;

        if (account == 0)
        {
            return;
        }

        await factory
            .SavePersonOptionsAsync(account, options.ToArray(), context.CancellationToken)
            .ConfigureAwait(false);
    }

    private async ValueTask InternalLoginAsync(ServiceHandleContext context, int account, string username, string mac)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        var active = await sessions
            .Where(e => e.Account == account)
            .ToListAsync()
            .ConfigureAwait(false);

        if (active.Any(e => e.Connected))
        {
            session.Send(new LoginResponse(LoginErrorMessageCode.InGameAlready));
            return;
        }

        // Nothing holds these sessions any more, the new login replaces them.
        foreach (var previous in active)
        {
            await sessions
                .DeleteAsync(previous)
                .ConfigureAwait(false);
        }

        var claims = new SessionValue(account);

        world.Set(context.Player, new ClaimsComponent(claims));

        await leases
            .AttachAsync(context.Player, claims.Key, claims.Account)
            .ConfigureAwait(false);

        session.Send(new LoginResponse(claims, username, true, mac, LoginType.NewUser));
    }
}
