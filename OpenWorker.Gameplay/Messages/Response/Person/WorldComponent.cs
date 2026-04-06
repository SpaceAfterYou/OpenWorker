using System.Numerics;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Messages.Response.Person;

namespace OpenWorker.Gameplay.Messages.Response.Person;

[EntityComponent(EntityComponentService.District)]
public readonly record struct WorldComponent
{
    public required short Location { get; init; }
    public MapValue Map { get; init; }
    public required Vector3 Position { get; init; }
    public required float Rotation { get; init; }
    public required int Jump { get; init; }
    
    public WorldComponent(WorldValue value)
    {
        Location = value.Location;
        Map = value.Map;
        Position = value.Position;
        Rotation = value.Rotation;

        // First VStartEventBox in vBatch table.
        // TODO: In district 6 VStartEventBox must be selected by general district.
        Jump = Location * 100 + 1;
    }
}