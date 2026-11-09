using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Types;

namespace OpenWorker.Hotspot.Modules.Shop.Responses;

/// <summary>
/// Items just sold, which the shop window adds to what it would sell back: the layout of the list.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct ShopRepurchaserAddResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Shop;
    private const ShopOpcode Command = ShopOpcode.RepurchaserAdd;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public required IReadOnlyList<ItemValue> Items { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        WriteItems(writer, Items);
    }

#endregion Interface: IWritableData

    /// <summary>
    /// A byte count and the items; the client reads the count as a signed byte, so no more than 127 go.
    /// </summary>
    internal static void WriteItems(BinaryWriter writer, IReadOnlyList<ItemValue> items)
    {
        var count = Math.Min(items.Count, sbyte.MaxValue);

        writer.Write((byte)count);

        for (var index = 0; index < count; index++)
        {
            writer.Write(items[index]);
        }
    }
}
