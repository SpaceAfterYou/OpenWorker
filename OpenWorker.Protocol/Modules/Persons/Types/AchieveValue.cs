namespace OpenWorker.Hotspot.Modules.Persons.Types;

/// <summary>
/// How far an achievement has gone.
/// </summary>
public readonly struct AchieveValue
{
    public int Index { get; init; }
    public long Count { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Index);
        writer.Write(Count);
    }
}
