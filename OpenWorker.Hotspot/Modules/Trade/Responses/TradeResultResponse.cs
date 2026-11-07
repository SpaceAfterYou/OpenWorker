using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Trade.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct TradeResultResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Trade;
    private const TradeOpcode Command = TradeOpcode.Result;

    public MessageOpcode Opcode => new(Group, Command);

    public required byte Result { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Result);
    }
}
