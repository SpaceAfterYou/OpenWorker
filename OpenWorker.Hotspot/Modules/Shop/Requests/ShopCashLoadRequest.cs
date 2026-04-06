using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Shop.Requests;

/// <summary>
/// This packet doesn't have any content.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct ShopCashLoadRequest(BinaryReader reader) : IRequestHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Shop;
    private const ShopOpcode Command = ShopOpcode.CashLoad;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage
}
