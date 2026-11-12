using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Shop.Responses;

/// <summary>
/// An item bought back: the slot it came into and the item. The client puts it there and drops the entry of the shop window
/// with the serial of the item.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct ShopRepurchaserResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Shop;
    private const ShopOpcode Command = ShopOpcode.Repurchaser;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    [WireArray(WireLength.Signed32)]
    public required IReadOnlyCollection<StorageItemValue> Items { get; init; }

#endregion Message: Body
}
