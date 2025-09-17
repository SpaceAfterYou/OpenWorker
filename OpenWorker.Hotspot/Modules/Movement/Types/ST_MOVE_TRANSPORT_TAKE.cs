using OpenWorker.Domain.Types;

namespace OpenWorker.Hotspot.Modules.Movement.Types;

public struct ST_MOVE_TRANSPORT_TAKE
{
    public ActorValue dwActorID { get; }
    public short wTransportTableIdx { get; }
    public int dwNpcID { get; }
    public float fStartTime { get; }
}