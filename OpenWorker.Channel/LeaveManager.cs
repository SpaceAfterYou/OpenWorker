using Arch.Core;
using Microsoft.Extensions.Options;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Types;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using Redis.OM.Searching;

namespace OpenWorker.Channel;

/// <summary>
/// Sends a player out of the current server, to the server list or to the character selection.
/// </summary>
public sealed class LeaveManager(
    World world,
    IOptions<GateOptions> gate,
    IOptions<AuthEndpointOptions> auth,
    IRedisCollection<GateCache> gates,
    SessionLeaseManager leases,
    IPersonFlush persons)
{
    private short Gate { get; } = gate.Value.Id;

    private AuthEndpointOptions Auth { get; } = auth.Value;

    public async ValueTask LeaveAsync(Entity player, LeaveDestination destination)
    {
        var claims = world.Get<ClaimsComponent>(player);

        // Nobody has entered through this connection yet.
        if (claims.Account == 0)
        {
            return;
        }

        var (address, port, result) = destination switch
        {
            LeaveDestination.ServerList => await LeaveToServerListAsync(player),
            LeaveDestination.CharacterSelect => await FindGateAsync(),
            _ => (Auth.Host, (short)0, false)
        };

        // The gate shows the person from the database: what this server has must be there first.
        await persons.FlushAsync(player);

        if (!world.IsAlive(player))
        {
            return;
        }

        ActorValue actor = world.Has<ActorComponent>(player) ? world.Get<ActorComponent>(player).Identifier : 0;

        world.Get<ServerSessionComponent>(player).Send(new PersonGobackLobbyResponse
        {
            Actor = actor,
            Account = claims.Account,
            Address = address,
            Port = port,
            Destination = destination,
            Result = result
        });
    }

    private async ValueTask<(string Address, short Port, bool Result)> LeaveToServerListAsync(Entity player)
    {
        await leases
            .MarkReturnToAuthAsync(player);

        return (Auth.Host, (short)Auth.Port, true);
    }

    private async ValueTask<(string Address, short Port, bool Result)> FindGateAsync()
    {
        var gate = await gates
            .FirstOrDefaultAsync(e => e.Identifier == Gate);

        // The address is written even for a refusal because the client reads it anyway.
        return gate is null
            ? (Auth.Host, (short)0, false)
            : (gate.Host, gate.Port, true);
    }
}
