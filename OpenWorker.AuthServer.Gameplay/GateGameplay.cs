using OpenWorker.Hotspot.Cache;
using Redis.OM;
using Arch.Core;
using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot.Modules.Login.Requests;
using OpenWorker.Hotspot.Modules.Login.Responses;
using OpenWorker.Hotspot.Modules.Login.Types;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Persistence;
using Redis.OM.Searching;

namespace OpenWorker.AuthServer.App;

public sealed class GateGameplay(
    IRedisCollection<GateCache> gates,
    IDbContextFactory<PersistenceContext> factory,
    List<GateInfo> gateList,
    IRedisCollection<SessionCache> sessions,
    SessionLeaseManager leases,
    ServerContent contents,
    World world)
{
    public async ValueTask TryJoinAsync(ServiceHandleContext context, LoginGateConnectRequest message)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);
        var claims = world.Get<ClaimsComponent>(context.Player);

        if (claims.Account == 0)
        {
            session.Send(new PersonKickOutResponse { OutReason = PersonKickOutReason.NotExistUser });
            return;
        }

        var cache = await gates
            .FirstOrDefaultAsync(e => e.Identifier == message.Gate);

        if (cache is null)
        {
            session.Send(new PersonKickOutResponse
            {
                OutReason = PersonKickOutReason.UserWrongWorld,
                Account = claims.Account
            });
            return;
        }

        if (cache.Workload is GateWorkload.Busy)
        {
            session.Send(new PersonKickOutResponse
            {
                OutReason = PersonKickOutReason.ServerUserFull,
                Account = claims.Account
            });
            return;
        }

        await using var database = await factory
            .CreateDbContextAsync(context.CancellationToken);

        var persistent = await database.Gates
            .FirstOrDefaultAsync(e => e.Id == message.Gate, context.CancellationToken);

        if (persistent is null)
        {
            session.Send(new PersonKickOutResponse
            {
                OutReason = PersonKickOutReason.SystemError,
                Account = claims.Account
            });
            return;
        }

        session.Send(new LoginGateConnectResponse { Address = cache.Host, Port = cache.Port });
    }

    public async ValueTask GetListAsync(ServiceHandleContext context, LoginGateListRequest message)
    {
        var component = world.Get<ClaimsComponent>(context.Player);
        var session = world.Get<ServerSessionComponent>(context.Player);

        if (component.Account == 0)
        {
            component = await ResumeSessionAsync(context, message.Account);
        }

        if (component.Account != message.Account)
        {
            session.Send(new PersonKickOutResponse
            {
                OutReason = PersonKickOutReason.NotExistUser,
                Account = component.Account
            });
            return;
        }

        await using var database = await factory
            .CreateDbContextAsync(context.CancellationToken);

        var personList = await database.Persons
            .Include(e => e.Gate)
            .Include(e => e.Account)
            .Where(e => e.Account.Id == component.Account)
            .ToArrayAsync(context.CancellationToken);

        var groupList = personList.CountBy(e => e.Gate.Id);

        var values = gateList
            .Select(gate =>
            {
                if (gate.Workload == GateWorkload.Offline)
                {
                    return new GateDataInfo
                    {
                        Gate = gate,
                        Person = new GatePersonInfo
                        {
                            Count = 0
                        }
                    };
                }

                return new GateDataInfo
                {
                    Gate = gate,
                    Person = new GatePersonInfo
                    {
                        Count = (byte)groupList.FirstOrDefault(e => e.Key == gate.Id).Value
                    }
                };
            });

        session.Send(new LoginGateListResponse { Previous = 0, Values = values.ToArray() });

        var options = await factory
            .LoadPersonOptionsAsync(component.Account, context.CancellationToken);

        session.Send(new LoginOptionLoadResponse { PersonOptions = options, Contents = contents });
    }

    /// <summary>
    /// A player coming back from the gate reconnects without logging in again.
    /// </summary>
    private async ValueTask<ClaimsComponent> ResumeSessionAsync(ServiceHandleContext context, int account)
    {
        var active = await sessions
            .Where(e => e.Account == account)
            .ToListAsync();

        // The request names an account and carries nothing secret: the session is continued only from the address its
        // player left the game from.
        var address = world.Get<ServerSessionComponent>(context.Player).RemoteEndPoint;
        var returning = active.FirstOrDefault(e => SessionLeaseManager.MayReturn(e, address));

        if (returning is null)
        {
            return default;
        }

        if (!await leases.TakeOverAsync(context.Player, returning.Session, returning.Account) || !world.IsAlive(context.Player))
        {
            return default;
        }

        var claims = new ClaimsComponent(returning.Session);

        world.Set(context.Player, claims);

        world.SetLogAccount(context.Player, claims.Account);

        return claims;
    }
}

// https://youtu.be/si0biD5QoMY?list=RDMMgLx38RION84
