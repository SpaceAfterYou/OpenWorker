using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Trade.Requests;

[WireMessage(WireMethods.Read)]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct TradeRequest : IRequestHotspotMessage, IWritableData
{
    private const GroupOpcode Group = GroupOpcode.Trade;
    private const TradeOpcode Command = TradeOpcode.Request;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    [WireString(WireStringFormat.Utf8Unicode)]
    public string TargetName { get; }

    public void Write(BinaryWriter writer)
    {
        writer.WritePersonName(TargetName);
    }
}
