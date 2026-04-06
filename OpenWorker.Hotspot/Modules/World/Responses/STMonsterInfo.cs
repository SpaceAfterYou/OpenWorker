using OpenWorker.Domain.Types;

namespace OpenWorker.Hotspot.Modules.World.Responses;

public readonly struct STMonsterInfo
{
    public required STNpcInfo stNpcInfo { get; init; }
    public ActorValue uxParentActorID { get; init; }
    public required int nSpawnBoxID { get; init; }
    public int nMotionClass { get; init; }
    public required bool bBattlePos { get; init; }
    public required float fCurSuperArmor { get; init; }
    public required float fMaxSuperArmor { get; init; }
    public required StatKeyValuePair[] vecStat { get; init; }
}