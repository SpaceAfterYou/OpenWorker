using System.Numerics;
using OpenWorker.Domain.Types;

namespace OpenWorker.Hotspot.Modules.World.Types;

public readonly record struct WorldNpcListEntry(
    ActorValue Actor,
    Vector3 Position,
    float Rotation,
    int Health,
    int Waypoint,
    int Sector,
    byte Level,
    int Prototype);
