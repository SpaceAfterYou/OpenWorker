using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Shop.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Shop.Responses;

/// <summary>
/// The products of the cash shop, in parts: a flag before each part says whether it is the last. Written by hand, as the
/// products are.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct ShopCashLoadResponse : IResponseHotspotMessage, IWireWritable
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Shop;
    private const ShopOpcode Command = ShopOpcode.CashLoad;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    /// <summary>
    /// The last part of the list.
    /// </summary>
    public bool Last { get; init; }

    public IReadOnlyList<CashItemValue> ItemList { get; init; }

#endregion Message: Body

#region Interface: IWireWritable

    public void Write(ref PacketWriter writer)
    {
        var items = ItemList ?? [];

        writer.WriteBoolean(Last);
        writer.WriteLength(WireLength.Signed32, items.Count);

        for (var index = 0; index < items.Count; index++)
        {
            items[index].Write(ref writer);
        }
    }

#endregion Interface: IWireWritable
}
