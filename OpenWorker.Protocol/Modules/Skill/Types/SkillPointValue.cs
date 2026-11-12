using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

[WireMessage]
public readonly partial struct SkillPointValue
{
#region Message: Body

    public short TotalSkillPoint { get; init; }
    public short FreeSkillPoint { get; init; }

#endregion Message: Body
}
