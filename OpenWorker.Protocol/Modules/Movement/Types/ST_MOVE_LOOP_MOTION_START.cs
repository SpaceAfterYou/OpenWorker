using OpenWorker.Domain.Types;

namespace OpenWorker.Hotspot.Modules.Movement.Types;

public struct ST_MOVE_LOOP_MOTION_START
{
    public ActorValue dwActorID { get; }
    public float fPosX { get; }
    public float fPosY { get; }
    public float fPosZ { get; }
    public float fYaw { get; }
    public int dwStartAniID { get; }
    public int dwLoopAniID { get; }
    public int dwEndAniID { get; }
    public int dwIdleAniID { get; }
}