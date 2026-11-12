using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Rewards.Types;

/// <summary>
/// An item and its amount, as the client reads the reward lists of several windows.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial record struct RewardItemValue(int Item, short Count) : IWritableData
{
    /// <summary>
    /// A list that starts with a byte count, for a message that still writes through <see cref="BinaryWriter"/>.
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
