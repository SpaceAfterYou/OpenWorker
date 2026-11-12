using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Shop.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Shop.Responses;

/// <summary>
/// The items of the cash shop. A flag that is always set goes before the list, so the writer is written by hand.
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

    public IReadOnlyList<CashItemValue> ItemList { get; init; }

#endregion Message: Body

#region Interface: IWireWritable

    public void Write(ref PacketWriter writer)
    {
        var items = ItemList ?? [];

        // What the flag means is not known; it was always sent set.
        writer.WriteBoolean(true);
        writer.WriteLength(WireLength.Signed32, items.Count);

        for (var index = 0; index < items.Count; index++)
        {
            items[index].Write(ref writer);
        }
    }

    public void Write(BinaryWriter writer)
    {
        WireBridge.Write(writer, this);
    }

#endregion Interface: IWireWritable
}
