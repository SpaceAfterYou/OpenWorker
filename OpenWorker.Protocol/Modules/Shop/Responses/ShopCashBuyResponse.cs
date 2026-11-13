using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Shop.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Shop.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct ShopCashBuyResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Shop;
    private const ShopOpcode Command = ShopOpcode.CashBuy;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    /// <summary>
    /// The appearances the purchase opened.
    /// </summary>
    [WireArray(WireLength.Byte)]
    public required IReadOnlyList<CashAppearanceValue> Appearances { get; init; }

    /// <summary>
    /// The products that were not bought; the client names them, or says the purchase is done when there are none.
    /// </summary>
    [WireArray(WireLength.Byte)]
    public required IReadOnlyList<int> Failed { get; init; }

#endregion Message: Body
}
