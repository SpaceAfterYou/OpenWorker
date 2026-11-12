using System.Numerics;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

[WireMessage]
public readonly partial struct SkillActionPosInfoValue
{
#region Message: Body

    public Vector3 Position { get; init; }
    public float Angle { get; init; }
    public short MotionClass { get; init; }

    // The client's struct has a flag of a swapped skill here, which it does not read: it is not sent.

    public bool HasInputFlag { get; init; }

#endregion Message: Body
}
