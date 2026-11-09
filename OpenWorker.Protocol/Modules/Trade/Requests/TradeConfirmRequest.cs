using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Trade.Requests;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly struct TradeConfirmRequest : IRequestHotspotMessage, IWritableData
{
    private const GroupOpcode Group = GroupOpcode.Trade;
    private const TradeOpcode Command = TradeOpcode.Confirm;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public void Write(BinaryWriter writer)
    {
    }
}
