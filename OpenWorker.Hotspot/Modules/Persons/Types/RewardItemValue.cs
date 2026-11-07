namespace OpenWorker.Hotspot.Modules.Persons.Types;

/// <summary>
/// An item of a reward.
/// </summary>
public readonly struct RewardItemValue
{
    public int Item { get; init; }
    public short Count { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Item);
        writer.Write(Count);
    }
}
