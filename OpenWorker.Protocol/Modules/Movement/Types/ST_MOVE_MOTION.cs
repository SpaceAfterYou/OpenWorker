using OpenWorker.Domain.Types;

namespace OpenWorker.Hotspot.Modules.Movement.Types;

public struct ST_MOVE_MOTION
{
    public ActorValue dwActorID { get; }
    public short nMotionClass { get; }
    public short nSubClass { get; }
}