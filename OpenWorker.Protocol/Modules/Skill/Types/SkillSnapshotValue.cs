using OpenWorker.Domain.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

[WireMessage]
public readonly partial struct SkillSnapshotValue
{
#region Message: Body

    public ActorValue Target { get; init; }
    public SkillPointValue SkillPoint { get; init; }
    public short DeckSlotCount { get; init; }
    [WireArray(WireLength.Byte)]
    public SkillInfoValue[] SkillList { get; init; }
    [WireArray(WireLength.Byte)]
    public SkillDeckValue[] DeckSlotList { get; init; }
    [WireArray(Count = SkillModuleDefines.SkillDeckBonusCount)]
    public ushort[] DeckBonus { get; init; }

#endregion Message: Body
}
