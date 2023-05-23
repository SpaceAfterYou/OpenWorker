using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.DatabaseModel;
using OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;
using OpenWorker.Infrastructure.Database;
using OpenWorker.Infrastructure.Gameplay;
using OpenWorker.Infrastructure.Gameplay.Cache.Models;
using OpenWorker.Infrastructure.Gameplay.Realm.Components;
using OpenWorker.Services.Gate.Infrastructure.Gameplay.Abstractions;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using SoulWorkerResearch.SoulCore.Defines;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Client.Login;

namespace OpenWorker.Services.Gate.Infrastructure.Gameplay.Services;

internal sealed record AuthService : IAuthService
{
    private readonly ushort _id;

    private readonly IHotSpotSession _session;
    private readonly IRedisCollection<SessionModel> _sessions;
    private readonly IDbContextFactory<PersistentContext> _factory;

    private readonly ILogger _logger;

    public AuthService(IConfiguration configuration, IHotSpotSession session, IRedisConnectionProvider provider, IDbContextFactory<PersistentContext> factory, ILogger<AuthService> logger)
    {
        _id = configuration.GetValue<ushort>("Id");

        _session = session;
        _sessions = provider.RedisCollection<SessionModel>();
        _factory = factory;

        _logger = logger;
    }

    public async ValueTask JoinAsync(int account, ushort gate, long key, CancellationToken ct = default)
    {
        if (gate != _id)
        {
            _logger.LogWarning("Bad gate id ({})", gate);

            await _session.SendAsync(new LoginResponseEnterServerClientMessage { Result = GateEnterResult.Failure }, ct);
            return;
        }

        var claims = new SessionClaims(key);

        if (await _sessions.FirstOrDefaultAsync(e => e.Claims == claims) is not SessionModel sessionModel)
        {
            _logger.LogWarning("No registered session with account ({}) and key ({})", claims.Account, claims.Key);

            await _session.SendAsync(new LoginResponseEnterServerClientMessage { Result = GateEnterResult.Failure }, ct);
            return;
        }

        // TODO: Kick user with message about expiration
        if (sessionModel.IsInAnyServer || sessionModel.IsKeyExpired)
        {
            _logger.LogWarning("Session expired with account ({}) and key ({})", claims.Account, claims.Key);

            await _session.SendAsync(new LoginResponseEnterServerClientMessage { Result = GateEnterResult.Failure }, ct);
            return;
        }

        await using var db = await _factory.CreateDbContextAsync(ct);

        if (await db.Account.AsNoTracking().FirstOrDefaultAsync(e => e.Id == claims.Account, ct) is not AccountModel accountModel)
        {
            _logger.LogWarning("No account with id ({})", claims.Account);

            await _session.SendAsync(new LoginResponseEnterServerClientMessage { Result = GateEnterResult.Failure }, ct);
            return;
        }

        await _session.SendAsync(new LoginResponseEnterServerClientMessage { Account = accountModel.Id, Result = GateEnterResult.Success }, ct);

        _session.Entity.Set(new AccountComponent { Id = accountModel.Id, Key = claims });
    }
}
