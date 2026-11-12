using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Friends.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Friends.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct FriendInviteAcceptResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Friend;
    private const FriendOpcode Command = FriendOpcode.InviteAccept;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    /// <summary>
    /// Zero for an answer that needs no words, otherwise the number of the text the client shows.
    /// </summary>
    public int Result { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public required string Name { get; init; }

#endregion Message: Body
}
