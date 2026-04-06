using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Hotspot.Modules.Shop.Extensions;

namespace OpenWorker.Hotspot.Modules.Shop.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct ShopBuyResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Shop;
    private const ShopOpcode Command = ShopOpcode.Buy;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public IReadOnlyCollection<StorageValue> StorageList { get; init; }
    public long Spent { get; init; }
    public ShopCurrency Currency { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(StorageList);
        writer.Write(Spent);
        writer.Write(Currency);
    }

#endregion Interface: IWritableData
}
