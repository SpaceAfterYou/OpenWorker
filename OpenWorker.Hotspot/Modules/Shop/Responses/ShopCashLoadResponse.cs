using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Shop.Types;

namespace OpenWorker.Hotspot.Modules.Shop.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct ShopCashLoadResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Shop;
    private const ShopOpcode Command = ShopOpcode.CashLoad;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public IReadOnlyList<CashItemValue> ItemList { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(true); // idk
        writer.Write(ItemList.Count);

        foreach (var item in ItemList)
        {
            item.Write(writer);
        }
    }

#endregion Interface: IWritableData
}
