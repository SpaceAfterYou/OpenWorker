using System.Numerics;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Skill.Responses;

[WireMessage]
public readonly partial struct SkillTrapPosition
{
#region Message: Body

    public int Skill { get; init; }
    public short TriggerIndex { get; init; }
    public Vector3 Position { get; init; }

#endregion Message: Body
}
