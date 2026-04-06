using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Messages.Response.Person;

namespace OpenWorker.Hotspot.Modules.World.Responses;

public readonly struct STNpcInfo
{
    public required ActorValue uxActorID { get; init; }
    public required WorldValue stPosInfo { get; init; }
    public required int nHP { get; init; }
    public required int nWayPointID { get; init; }
    public required int nSectorID { get; init; }
    public required byte byLevel { get; init; }
    public required int nTableID { get; init; }
}