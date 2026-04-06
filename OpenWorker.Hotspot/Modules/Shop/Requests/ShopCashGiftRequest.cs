using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Shop.Extensions;
using OpenWorker.Hotspot.Modules.Shop.Types;

namespace OpenWorker.Hotspot.Modules.Shop.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct ShopCashGiftRequest(BinaryReader reader) : IRequestHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Shop;
    private const ShopOpcode Command = ShopOpcode.CashGift;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public string Name { get; } = reader.ReadUtf16UnicodeString();
    public IReadOnlyList<CashItemEntry> ItemList { get; } = reader.ReadCashItemList();

#endregion Message: Body
}
