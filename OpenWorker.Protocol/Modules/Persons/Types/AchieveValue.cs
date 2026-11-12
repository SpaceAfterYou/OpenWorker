using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Persons.Types;

/// <summary>
/// How far an achievement has gone.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct AchieveValue
{
    public int Index { get; init; }
    public long Count { get; init; }
}
