using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Rewards.Types;

/// <summary>
/// An item and its amount, as the client reads the reward lists of several windows.
/// </summary>
public readonly record struct RewardItemValue(int Item, short Count) : IWritableData
{
    public void Write(BinaryWriter writer)
    {
        writer.Write(Item);
        writer.Write(Count);
    }

    /// <summary>
    /// A list that starts with a byte count.
    /// </summary>
    public static void WriteList(BinaryWriter writer, IReadOnlyCollection<RewardItemValue> list)
    {
        writer.Write(checked((byte)list.Count));

        foreach (var value in list)
        {
            value.Write(writer);
        }
    }
}
