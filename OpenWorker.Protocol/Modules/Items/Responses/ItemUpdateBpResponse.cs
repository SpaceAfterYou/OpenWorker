using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

/// <summary>
/// The battle points of the player after they changed.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct ItemUpdateBpResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.UpdateBp;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Actor { get; init; }
    public long Total { get; init; }

    /// <summary>
    /// A text the client reads between the total and the type; the server sends it empty.
    /// </summary>
    [WireString(WireStringFormat.Utf16Unicode)]
    public string Text { get; init; }

    public byte Type { get; init; }
}
