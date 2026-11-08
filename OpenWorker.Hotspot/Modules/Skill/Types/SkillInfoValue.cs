using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

/// <summary>
/// A skill and its divergence, read in requests and written in responses.
/// </summary>
[WireMessage(WireMethods.Both)]
public readonly partial struct SkillInfoValue : IWritableData
{
    public int Skill { get; init; }
    public int Divergence { get; init; }
}
