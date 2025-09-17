using OpenWorker.Domain.Types;

namespace OpenWorker.Hotspot.Modules.Movement.Types;

public struct ST_MOVE_IGNORE_MOTION_DELTA //
{
    public ActorValue dwActorID { get; }
    public float fPosX { get; }
    public float fPosY { get; }
    public float fPosZ { get; }
    public float fYaw { get; }
    public float fPitch { get; }
    public int bForced { get; }
}