using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Extensions;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct ItemUpdateInvenMoneyResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.UpdateInvenMoney;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public long Total { get; init; }
    public int Bonus { get; init; }
    public MoneyEarnFlow Type { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Total);
        writer.Write(Bonus);
        writer.Write(Type);
    }

#endregion Interface: IWritableData
}
