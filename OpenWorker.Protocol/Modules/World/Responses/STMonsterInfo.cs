using OpenWorker.Domain.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.World.Responses;

/// <summary>
/// A monster in the lists of the monsters of the world.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct STMonsterInfo
{
    public required STNpcInfo stNpcInfo { get; init; }
    public ActorValue uxParentActorID { get; init; }
    public required int nSpawnBoxID { get; init; }
    public int nMotionClass { get; init; }
    public required bool bBattlePos { get; init; }
    public required float fCurSuperArmor { get; init; }
    public required float fMaxSuperArmor { get; init; }
    [WireArray(WireLength.Byte)]
    public required StatKeyValuePair[] vecStat { get; init; }
}
