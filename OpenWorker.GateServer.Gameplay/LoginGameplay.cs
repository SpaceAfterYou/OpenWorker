using OpenWorker.Hotspot.Cache;
using OpenWorker.Gameplay.Modules.Login.Extensions;
using Arch.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
using OpenWorker.Hotspot.Modules.Items.Enums;
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
    SessionLeaseManager leases,
    IDbContextFactory<PersistenceContext> factory,
    PersonRegistry registry,
    SecondPasswordGameplay secondPassword,
    IOptions<GateOptions> gate,
    ILogger<LoginGameplay> logger)
{
    private short Gate { get; } = gate.Value.Id;

    public async ValueTask TryJoinAsync(ServiceHandleContext context, LoginEnterServerRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        if (request.Session.Account != request.Account)
        {
            logger.LogDebug("Session does not belong to the account.");

            session.Send(LoginEnterGateResponse.Error);
            return;
        }

        if (request.Gate != Gate)
        {
            logger.LogDebug("Gate not found.");

            session.Send(LoginEnterGateResponse.Error);
            return;
        }

        if (await sessions.AnyAsync(e => e.Session == request.Session.Key) is false)
        {
            logger.LogDebug("Session not found.");

            session.Send(LoginEnterGateResponse.Error);
            return;
        }

        var account = await factory
            .LoadAccountAtGateAsync(request.Account, Gate, (byte)StorageGroup.AbilityEquip, context.CancellationToken);

        if (account is null)
        {
            logger.LogDebug("Account not found.");

            session.Send(LoginEnterGateResponse.Error);
            return;
        }

        world.Set(context.Player, new ClaimsComponent(request.Session));

        world.SetLogAccount(context.Player, request.Session.Account);

        await leases
            .AttachAsync(context.Player, request.Session.Key, request.Session.Account);

        secondPassword.Load(context.Player, account);

        world.Set(context.Player, account.ToTradePasswordComponent());

        var list = registry.CreatePersonListComponent(account.Persons, account.OptionList);

        world.Set(context.Player, list);

        session.Send(new LoginEnterGateResponse
        {
            HasError = false,
            Account = world.Get<ClaimsComponent>(context.Player).Account
        });
        session.Send(new WorldCurrentDateClientMessage());
    }
}
