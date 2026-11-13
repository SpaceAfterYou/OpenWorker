using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Trade.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Trade.Requests;

[WireMessage(WireMethods.Read)]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct TradePrivateShopStartRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Trade;
    private const TradeOpcode Command = TradeOpcode.PrivateShopStart;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public StorageSlot Slot { get; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Title { get; }

    [WireArray(WireLength.Byte)]
    public PrivateShopItemValue[] Items { get; }
}
