using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Trade.Requests;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly struct TradeAcceptRequest(BinaryReader reader) : IRequestHotspotMessage, IWritableData
{
    private const GroupOpcode Group = GroupOpcode.Trade;
    private const TradeOpcode Command = TradeOpcode.Accept;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public ActorValue Target { get; } = new(reader);
    public byte IsAccept { get; } = reader.ReadByte();

    public void Write(BinaryWriter writer)
    {
        writer.Write(Target);
        writer.Write(IsAccept);
    }
}
