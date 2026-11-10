using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

/// <summary>
/// How often the person has used each kind of item that is limited for a day, sent when the person enters. The client reads
/// the count of the fatigue items from it to tell the person that the day's limit is reached.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct ItemUseInfoListResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.UseInfoList;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    /// <summary>
    /// When the list was last brought up to date, seconds since 1970.
    /// </summary>
    public long UpdatedAt { get; init; }

    [WireArray(WireLength.Byte)]
    public IReadOnlyList<ItemUseInfoValue> List { get; init; }
}
