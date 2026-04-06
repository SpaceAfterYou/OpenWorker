using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Shop.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct ShopCashSetRequest(BinaryReader reader) : IRequestHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Shop;
    private const ShopOpcode Command = ShopOpcode.CashSet;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public byte SetNo { get; } = reader.ReadByte();
    public string Name { get; } = reader.ReadUtf16UnicodeString();

    public IReadOnlyList<int> IndexList { get; } = Enumerable
        .Range(0, 10)
        .Select(_ => reader.ReadInt32())
        .ToArray();

    public IReadOnlyList<int> ItemList { get; } = Enumerable
        .Range(0, 10)
        .Select(_ => reader.ReadInt32())
        .ToArray();

#endregion Message: Body
}
