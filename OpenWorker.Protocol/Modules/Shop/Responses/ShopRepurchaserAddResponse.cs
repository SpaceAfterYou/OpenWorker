using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Shop.Responses;

/// <summary>
/// Items just sold, which the shop window adds to what it would sell back: the layout of the list.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct ShopRepurchaserAddResponse : IResponseHotspotMessage, IWireWritable
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Shop;
    private const ShopOpcode Command = ShopOpcode.RepurchaserAdd;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public required IReadOnlyList<ItemValue> Items { get; init; }

#endregion Message: Body

#region Interface: IWireWritable

    public void Write(ref PacketWriter writer)
    {
        WriteItems(ref writer, Items);
    }

    /// <summary>
    /// A byte count and the items; the client reads the count as a signed byte, so no more than 127 go. Written by hand:
    /// a list of the generator would refuse the longer list instead of cutting it.
    /// </summary>
    internal static void WriteItems(ref PacketWriter writer, IReadOnlyList<ItemValue>? items)
    {
        var count = Math.Min(items?.Count ?? 0, sbyte.MaxValue);

        writer.WriteByte((byte)count);

        for (var index = 0; index < count; index++)
        {
            items![index].Write(ref writer);
        }
    }

#endregion Interface: IWireWritable
}
