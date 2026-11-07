namespace OpenWorker.Hotspot.Modules.Persons.Types;

/// <summary>
/// Progress of a district.
/// </summary>
public readonly struct DistrictStateValue
{
    public int District { get; init; }
    public int Point { get; init; }
    public bool Reward { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(District);
        writer.Write(Point);
        writer.Write(Reward);
    }
}
