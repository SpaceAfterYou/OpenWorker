using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Messages.Response.Person;

namespace OpenWorker.Hotspot.Modules.Movement.Types;

public struct ST_MOVE_POSITION
{
    public ActorValue dwActorID { get; }
    public MapValue nMapID { get; }
    public float fPosX { get; }
    public float fPosY { get; }
    public float fPosZ { get; }
    public float fYaw { get; }
    public float fPitch { get; }
}