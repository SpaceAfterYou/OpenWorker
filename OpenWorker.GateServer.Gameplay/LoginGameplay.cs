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

public sealed partial class LoginGameplay(
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
            LogRefused(logger, "the session does not belong to the account");

            session.Send(LoginEnterGateResponse.Error);
            return;
        }

        if (request.Gate != Gate)
        {
            LogRefused(logger, "the gate is another one");

            session.Send(LoginEnterGateResponse.Error);
            return;
        }

        if (await sessions.AnyAsync(e => e.Session == request.Session.Key) is false)
        {
            LogRefused(logger, "the session is not known");

            session.Send(LoginEnterGateResponse.Error);
            return;
        }

        // A district or a maze the person still plays on lets them go first: the gate shows the person from the database.
        if (!await leases.TakeOverAsync(context.Player, request.Session.Key, request.Session.Account) || !world.IsAlive(context.Player))
        {
            LogRefused(logger, "the session is held by another server");

            session.Send(LoginEnterGateResponse.Error);
            return;
        }

        var account = await factory
            .LoadAccountAtGateAsync(request.Account, Gate, (byte)StorageGroup.AbilityEquip, context.CancellationToken);

        if (account is null)
        {
            LogRefused(logger, "the account is not found");

            session.Send(LoginEnterGateResponse.Error);
            return;
        }

        world.Set(context.Player, new ClaimsComponent(request.Session));

        world.SetLogAccount(context.Player, request.Session.Account);


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

    [LoggerMessage(Level = LogLevel.Debug, Message = "A login at the gate is refused: {Reason}.")]
    private static partial void LogRefused(ILogger logger, string reason);
}
