using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Types;

namespace OpenWorker.Hotspot.Modules.Trade.Requests;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly struct TradeUpdateItemRequest(BinaryReader reader) : IRequestHotspotMessage, IWritableData
{
    private const GroupOpcode Group = GroupOpcode.Trade;
    private const TradeOpcode Command = TradeOpcode.UpdateItem;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public byte UpdateType { get; } = reader.ReadByte();
    public StorageSlot Slot { get; } = new(reader);

    public void Write(BinaryWriter writer)
    {
        writer.Write(UpdateType);
        Slot.Write(writer);
    }
}
