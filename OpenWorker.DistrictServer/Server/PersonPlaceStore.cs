using Arch.Core;
using Microsoft.Extensions.Options;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Stores;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Persistence;

namespace OpenWorker.DistrictServer.Server;

/// <summary>
/// Says where a person stands in this district, for the flush of the person: every round writes the place if it has
/// changed, and the flush on close writes it last, so that the next login starts there and a crash loses one interval
/// of walking at most. A place in another location (the player is on the way out) is not this district's to keep.
/// </summary>
public sealed class PersonPlaceStore(IOptions<InstanceOptions> instance) : IPersonPlaceSource
{
    private short Location { get; } = instance.Value.Location;

    public bool TryGetPlace(World world, Entity player, out PersonPlace place)
    {
        if (!world.TryGet<WorldComponent>(player, out var current) || current.Location != Location)
        {
            place = default;

            return false;
        }

        place = new PersonPlace(Location, current.Position.X, current.Position.Y, current.Position.Z, current.Rotation);

        return true;
    }
}
