using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Types;

namespace OpenWorker.Hotspot.Modules.Trade.Requests;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly struct TradePrivateShopItemRequest(BinaryReader reader) : IRequestHotspotMessage, IWritableData
{
    private const GroupOpcode Group = GroupOpcode.Trade;
    private const TradeOpcode Command = TradeOpcode.PrivateShopItem;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public StorageSlot Slot { get; } = new(reader);
    public long Money { get; } = reader.ReadInt64();

    public void Write(BinaryWriter writer)
    {
        Slot.Write(writer);
        writer.Write(Money);
    }
}
