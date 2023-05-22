using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.DatabaseModel;
using OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;
using OpenWorker.Infrastructure.Database;
using OpenWorker.Infrastructure.Database.Helpers;
using OpenWorker.Infrastructure.Gameplay;
using OpenWorker.Infrastructure.Gameplay.Cache.Models;
using OpenWorker.Infrastructure.Gameplay.Realm.Components;
using OpenWorker.Services.Auth.Infrastructure.Gameplay.Abstractions;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using SoulWorkerResearch.SoulCore.Defines;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Client.Login;
using System.Diagnostics;
using System.Security.Cryptography;

namespace OpenWorker.Services.Auth.Infrastructure.Gameplay.Services;

internal sealed record AuthService : IAuthService
{
    private readonly IHotSpotSession _session;
    private readonly IRedisCollection<SessionModel> _sessions;
    private readonly IDbContextFactory<PersistentContext> _factory;

    public AuthService(IHotSpotSession session, IRedisConnectionProvider provider, IDbContextFactory<PersistentContext> factory)
    {
        _session = session;
        _sessions = provider.RedisCollection<SessionModel>();
        _factory = factory;
    }

    public async ValueTask<AccountComponent?> JoinAsync(string login, string password, string mac, CancellationToken ct = default)
    {
        if (await TryGetAccount(login, password, mac, ct) is not AccountModel account)
        {
            return null;
        }

        if (await TryRegisterSession(account, ct) is not SessionModel registry)
        {
            return null;
        }

        await _session.SendAsync(new LoginResultClientMessage { AccountId = account.Id, SessionKey = registry.Key, Mac = mac }, ct);

        return new()
        {
            Id = account.Id,
            Key = new(registry.Key)
        };
    }

    private async ValueTask<SessionModel?> TryRegisterSession(AccountModel account, CancellationToken ct = default)
    {
        if (await _sessions.FirstOrDefaultAsync(e => e.Account == account.Id) is SessionModel response && response.IsOnline)
        {
            await _session.SendAsync(new LoginResultClientMessage { ErrorCode = AuthLoginErrorMessageCode.InGameAlready }, ct);
            return null;
        }

        var claims = SessionClaims.Create(account.Id);
        var model = new SessionModel { Account = claims.Account, Key = claims.Key };

        if (await _sessions.InsertAsync(model) is null)
        {
            await _session.SendAsync(new LoginResultClientMessage { ErrorCode = AuthLoginErrorMessageCode.InGameAlready }, ct);
            return null;
        }

        return model;
    }

    private async ValueTask<AccountModel?> TryGetAccount(string login, string password, string mac, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        if (await db.Account.FirstOrDefaultAsync(e => e.Nickname == login, ct) is not AccountModel model)
        {
            await _session.SendAsync(new LoginResultClientMessage { ErrorCode = AuthLoginErrorMessageCode.WrongUsernameOrPassword }, ct);
            return null;
        }

        if (model.IsBanned)
        {
            await _session.SendAsync(new LoginResultClientMessage { ErrorCode = AuthLoginErrorMessageCode.BlockAccountId }, ct);
            return null;
        }

        if (await db.BannedMac.AnyAsync(e => e.Mac == mac, ct))
        {
            await _session.SendAsync(new LoginResultClientMessage { ErrorCode = AuthLoginErrorMessageCode.BlockMac }, ct);
            return null;
        }

        Debug.Assert(model.Password.Length <= AccountHelper.KEY_SIZE);
        var hash = AccountHelper.ComputeHash(password, model.Salt);

        if (CryptographicOperations.FixedTimeEquals(hash, model.Password) is false)
        {
            await _session.SendAsync(new LoginResultClientMessage { ErrorCode = AuthLoginErrorMessageCode.WrongUsernameOrPassword }, ct);
            return null;
        }

        return model;
    }
}
