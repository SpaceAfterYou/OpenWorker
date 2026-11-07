using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Components;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot;
using OpenWorker.Persistence;

namespace OpenWorker.DistrictServer.Server;

/// <summary>
/// Saves where a person stood in this district when its connection closed.
/// </summary>
public sealed class PersonPlaceStore(
    World world,
    IDbContextFactory<PersistenceContext> factory,
    IConfiguration configuration) : ISessionCloseHandler
{
    private short Location { get; } = configuration.GetLocation();

    public async ValueTask OnSessionClosedAsync(Entity player)
    {
        if (!world.Has<WorldComponent>(player))
        {
            return;
        }

        var account = world.Get<ClaimsComponent>(player).Account;
        var person = world.Get<ActorComponent>(player).Identifier;
        var place = world.Get<WorldComponent>(player);

        // The connection never finished entering the game.
        if (account == 0 || person == 0 || place.Location != Location)
        {
            return;
        }

        await using var database = await factory
            .CreateDbContextAsync()
            .ConfigureAwait(false);

        await database.Persons
            .Where(e => e.Id == person && e.Account.Id == account)
            .ExecuteUpdateAsync(e => e
                .SetProperty(x => x.Location, Location)
                .SetProperty(x => x.PositionX, place.Position.X)
                .SetProperty(x => x.PositionY, place.Position.Y)
                .SetProperty(x => x.PositionZ, place.Position.Z)
                .SetProperty(x => x.RotationX, place.Rotation))
            .ConfigureAwait(false);
    }
}
