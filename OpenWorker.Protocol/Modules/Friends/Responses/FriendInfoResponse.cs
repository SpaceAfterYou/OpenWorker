using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Friends.Responses;

/// <summary>
/// What a friend looks like, for the window that shows another player.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct FriendInfoResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Friend;
    private const FriendOpcode Command = FriendOpcode.Info;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    /// <summary>
    /// The client does not look at it.
    /// </summary>
    public int Result { get; init; }

    public required OtherPersonValue Person { get; init; }

    /// <summary>
    /// The titles the window shows next to the name.
    /// </summary>
    public TitleValue Title { get; init; }

#endregion Message: Body
}
