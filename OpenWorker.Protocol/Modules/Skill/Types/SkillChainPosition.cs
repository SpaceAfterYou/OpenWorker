using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

[WireMessage]
public readonly partial struct SkillChainPosition
{
#region Message: Body

    public int Skill { get; init; }
    public short TriggerIndex { get; init; }
    public Vector3 Position { get; init; }
    public Vector3 Direction { get; init; }
    public uint Session { get; init; }
    public ActorValue Target { get; init; }

#endregion Message: Body
}
