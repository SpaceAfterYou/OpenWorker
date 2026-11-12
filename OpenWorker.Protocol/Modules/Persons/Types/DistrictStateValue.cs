using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Persons.Types;

/// <summary>
/// Progress of a district.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct DistrictStateValue
{
    public int District { get; init; }
    public int Point { get; init; }
    public bool Reward { get; init; }
}
