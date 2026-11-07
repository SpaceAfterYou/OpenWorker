using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Trade.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct TradeRequest(BinaryReader reader) : IRequestHotspotMessage, IWritableData
{
    private const GroupOpcode Group = GroupOpcode.Trade;
    private const TradeOpcode Command = TradeOpcode.Request;

    public MessageOpcode Opcode => new(Group, Command);

    public string TargetName { get; } = reader.ReadPersonName();

    public void Write(BinaryWriter writer)
    {
        writer.WritePersonName(TargetName);
    }
}
