using System.Numerics;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

/// <summary>
/// Where a skill is used from, read in the requests of a skill and written back in its broadcast.
/// </summary>
[WireMessage(WireMethods.Both)]
public readonly partial struct SkillPositionValue : IWritableData
{
#region Message: Body

    public Vector3 Position { get; init; }
    public float Angle { get; init; }
    public short MotionClass { get; init; }

    /// <summary>
    /// TODO: Client has a field but skips when read.
    /// </summary>
    // public bool IsSwapSkill { get; init; }

    public bool HasInputFlag { get; init; }

#endregion Message: Body
}
