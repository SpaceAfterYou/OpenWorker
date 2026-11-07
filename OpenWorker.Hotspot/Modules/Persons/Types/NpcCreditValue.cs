namespace OpenWorker.Hotspot.Modules.Persons.Types;

/// <summary>
/// Relations with a group of NPCs.
/// </summary>
public readonly struct NpcCreditValue
{
    public int Group { get; init; }
    public byte Grade { get; init; }
    public int Point { get; init; }
    public short CurrentPoint { get; init; }
    public long UpdatedAt { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Group);
        writer.Write(Grade);
        writer.Write(Point);
        writer.Write(CurrentPoint);
        writer.Write(UpdatedAt);
    }
}
