using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Trade.Requests;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly struct TradeUpdateMoneyRequest(BinaryReader reader) : IRequestHotspotMessage, IWritableData
{
    private const GroupOpcode Group = GroupOpcode.Trade;
    private const TradeOpcode Command = TradeOpcode.UpdateMoney;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public long Money { get; } = reader.ReadInt64();

    public void Write(BinaryWriter writer)
    {
        writer.Write(Money);
    }
}
