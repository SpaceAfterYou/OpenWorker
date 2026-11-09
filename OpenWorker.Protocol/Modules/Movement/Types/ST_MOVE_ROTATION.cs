using OpenWorker.Domain.Types;

namespace OpenWorker.Hotspot.Modules.Movement.Types;

public struct ST_MOVE_ROTATION
{
    public ActorValue dwActorID { get; }
    public float fYaw { get; }
}