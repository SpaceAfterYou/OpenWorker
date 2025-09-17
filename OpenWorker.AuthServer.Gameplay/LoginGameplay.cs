using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Login.Components;
using OpenWorker.Hotspot.Modules.Login.Enums;
using OpenWorker.Hotspot.Modules.Login.Requests;
using OpenWorker.Hotspot.Modules.Login.Responses;
using OpenWorker.Persistence;
using OpenWorker.Persistence.Utils;
using Redis.OM.Searching;

namespace OpenWorker.AuthServer.App;

public sealed class LoginGameplay(IRedisCollection<SessionCache> sessions, IDbContextFactory<PersistenceContext> factory, World world) {
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

    private async ValueTask InternalLoginAsync(ServiceHandleContext context, int account, string username, string mac)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        // TODO: Uncomment when production        
        // if (await sessions.AnyAsync(e => e.Account == account).ConfigureAwait(false))
        // {
        //     session.Send(new LoginResponse(LoginErrorMessageCode.InGameAlready));
        //     return;
        // }

        var claims = new SessionValue(account);

        world.Set(context.Player, new ClaimsComponent(claims));

        var cache = new SessionCache
        {
            Session = claims.Key,
            Account = claims.Account,
            UpdatedAt = DateTime.UtcNow
        };

        await sessions
            .InsertAsync(cache)
            .ConfigureAwait(false);

        session.Send(new LoginResponse(claims, username, true, mac, LoginType.NewUser));
    }
}