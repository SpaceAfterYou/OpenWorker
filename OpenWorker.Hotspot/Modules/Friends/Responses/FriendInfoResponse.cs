using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Persons.Types;

namespace OpenWorker.Hotspot.Modules.Friends.Responses;

/// <summary>
/// What a friend looks like, for the window that shows another player.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct FriendInfoResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Friend;
    private const FriendOpcode Command = FriendOpcode.Info;

    public MessageOpcode Opcode => new(Group, Command);

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

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Result);
        Person.Write(writer);
        writer.Write(Title.Value);
    }

#endregion Interface: IWritableData
}
