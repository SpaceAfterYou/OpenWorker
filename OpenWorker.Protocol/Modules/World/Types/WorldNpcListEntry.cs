using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.World.Types;

[WireMessage(WireMethods.Write)]
public readonly partial record struct WorldNpcListEntry(
    ActorValue Actor,
    Vector3 Position,
    float Rotation,
    int Health,
    int Waypoint,
    int Sector,
    byte Level,
    int Prototype);
