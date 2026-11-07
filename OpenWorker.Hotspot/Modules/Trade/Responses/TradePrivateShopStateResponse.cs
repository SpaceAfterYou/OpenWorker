using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Trade.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct TradePrivateShopStateResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Trade;
    private const TradeOpcode Command = TradeOpcode.PrivateShopState;

    public MessageOpcode Opcode => new(Group, Command);

    public required ActorValue Actor { get; init; }
    public required byte State { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Actor);
        writer.Write(State);
    }
}
