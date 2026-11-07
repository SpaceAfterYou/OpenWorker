using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Trade.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct TradePrivateShopNameResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Trade;
    private const TradeOpcode Command = TradeOpcode.PrivateShopName;

    public MessageOpcode Opcode => new(Group, Command);

    public required ActorValue Actor { get; init; }
    public required string Title { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Actor);
        writer.WriteUtf16UnicodeString(Title, 22);
    }
}
