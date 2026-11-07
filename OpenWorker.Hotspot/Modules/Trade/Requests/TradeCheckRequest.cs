using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Trade.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct TradeCheckRequest(BinaryReader reader) : IRequestHotspotMessage, IWritableData
{
    private const GroupOpcode Group = GroupOpcode.Trade;
    private const TradeOpcode Command = TradeOpcode.Check;

    public MessageOpcode Opcode => new(Group, Command);

    public byte IsChecked { get; } = reader.ReadByte();

    public void Write(BinaryWriter writer)
    {
        writer.Write(IsChecked);
    }
}
