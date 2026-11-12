using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Persons.Types;

/// <summary>
/// An achievement that has moved on.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct AchieveUpdateValue
{
    public int Index { get; init; }
    public long Count { get; init; }
    public int NextIndex { get; init; }
    public byte Category { get; init; }
    public ushort CategoryCount { get; init; }
}
