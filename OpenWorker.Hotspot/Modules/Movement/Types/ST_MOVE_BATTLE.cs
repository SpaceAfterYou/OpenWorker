using OpenWorker.Domain.Types;

namespace OpenWorker.Hotspot.Modules.Movement.Types;

public struct ST_MOVE_BATTLE //
{
    public ActorValue dwActorID { get; }
    public float fPosX { get; }
    public float fPosY { get; }
    public float fPosZ { get; }
    public float fYaw { get; }
    public byte bBattlePose { get; }
    public byte bPlayMotion { get; }
}