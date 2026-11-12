using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Persons.Types;

/// <summary>
/// Relations with a group of NPCs.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct NpcCreditValue
{
    public int Group { get; init; }
    public byte Grade { get; init; }
    public int Point { get; init; }
    public short CurrentPoint { get; init; }
    public long UpdatedAt { get; init; }
}
