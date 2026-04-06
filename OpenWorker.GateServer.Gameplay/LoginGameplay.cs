using Arch.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Types;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Gameplay;
using OpenWorker.Hotspot.Messages.Response.World;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot.Modules.Login.Requests;
using OpenWorker.Hotspot.Modules.Login.Responses;
using OpenWorker.Persistence;
using Redis.OM.Searching;

namespace OpenWorker.GateServer.Gameplay;

public sealed record SceneComponent(World World)
{
    private HashSet<Entity> Members { get; } = [];

    public void Add(Entity actor)
    {
        Members.Add(actor);
    }

    public void Remove(ActorValue actor)
    {
        var entity = Members.FirstOrDefault(x => World.Get<ActorComponent>(x).Identifier == actor.Identifier);
        Members.Remove(entity);
    }
}

public sealed class LoginGameplay(
    World world,
    IRedisCollection<SessionCache> sessions,
    IDbContextFactory<PersistenceContext> factory,
    PersonRegistry registry,
    IConfiguration configuration,
    ILogger<LoginGameplay> logger)
{
    private short Gate { get; } = configuration.GetGate();

    public async ValueTask TryJoinAsync(ServiceHandleContext context, LoginEnterServerRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.GetPlayerEntity());

        if (request.Gate != Gate)
        {
            logger.LogDebug("Gate not found.");
            
            session.Send(LoginEnterGateResponse.Error);
            return;
        }

        if (await sessions.AnyAsync(e => e.Session == request.Session.Key).ConfigureAwait(false) is false)
        {
            logger.LogDebug("Session not found.");

            session.Send(LoginEnterGateResponse.Error);
            return;
        }

        await using var database = await factory
            .CreateDbContextAsync(context.CancellationToken)
            .ConfigureAwait(false);

        var account = await database.Accounts
            .Include(e => e.Persons)
            .FirstOrDefaultAsync(e => e.Id == request.Account, context.CancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            logger.LogDebug("Account not found.");

            session.Send(LoginEnterGateResponse.Error);
            return;
        }

        world.Set(context.GetPlayerEntity(), new ClaimsComponent(request.Session));

        var list = registry.CreatePersonListComponent(account.Persons);
        
        world.Add(context.GetPlayerEntity(), list);

        session.Send(new LoginEnterGateResponse
        {
            HasError = false,
            Account = world.Get<ClaimsComponent>(context.GetPlayerEntity()).Account
        });
        session.Send(new WorldCurrentDateClientMessage());
    }
}