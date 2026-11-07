namespace OpenWorker.Hotspot.Modules.Persons.Types;

/// <summary>
/// An achievement that has moved on.
/// </summary>
public readonly struct AchieveUpdateValue
{
    public int Index { get; init; }
    public long Count { get; init; }
    public int NextIndex { get; init; }
    public byte Category { get; init; }
    public ushort CategoryCount { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Index);
        writer.Write(Count);
        writer.Write(NextIndex);
        writer.Write(Category);
        writer.Write(CategoryCount);
    }
}
