using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Shop.Requests;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct ShopCashSetRequest : IRequestHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Shop;
    private const ShopOpcode Command = ShopOpcode.CashSet;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    private const int SetSize = 10;

    public byte SetNo { get; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; }

    [WireArray(Count = SetSize)]
    public IReadOnlyList<int> IndexList { get; }

    [WireArray(Count = SetSize)]
    public IReadOnlyList<int> ItemList { get; }

#endregion Message: Body
}
