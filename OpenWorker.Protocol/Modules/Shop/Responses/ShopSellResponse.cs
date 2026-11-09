using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Responses;

namespace OpenWorker.Hotspot.Modules.Shop.Responses;

/// <summary>
/// The items a shop bought: the slot each lay in and the item as it was. The client empties the slots and writes a line of
/// the chat for each; the gold comes with the update of the money.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct ShopSellResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Shop;
    private const ShopOpcode Command = ShopOpcode.Sell;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public required IReadOnlyCollection<StorageItemValue> Items { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Items.Count);

        foreach (var item in Items)
        {
            writer.Write(item);
        }
    }

#endregion Interface: IWritableData
}
