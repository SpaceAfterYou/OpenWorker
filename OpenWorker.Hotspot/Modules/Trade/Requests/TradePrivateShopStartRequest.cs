using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Trade.Extensions;
using OpenWorker.Hotspot.Modules.Trade.Types;

namespace OpenWorker.Hotspot.Modules.Trade.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct TradePrivateShopStartRequest(BinaryReader reader) : IRequestHotspotMessage, IWritableData
{
    private const GroupOpcode Group = GroupOpcode.Trade;
    private const TradeOpcode Command = TradeOpcode.PrivateShopStart;

    public MessageOpcode Opcode => new(Group, Command);

    public StorageSlot Slot { get; } = new(reader);
    public string Title { get; } = reader.ReadUtf8UnicodeString(64);
    public PrivateShopItemValue[] Items { get; } = reader.ReadPrivateShopItemList();

    public void Write(BinaryWriter writer)
    {
        Slot.Write(writer);
        writer.WriteUtf16UnicodeString(Title, 64);
        writer.Write(Items);
    }
}
