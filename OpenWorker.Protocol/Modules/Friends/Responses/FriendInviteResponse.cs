using System.Diagnostics;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Friends.Responses;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct FriendInviteResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Friend;
    private const FriendOpcode Command = FriendOpcode.Invite;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    /// <summary>
    /// Zero when the request went out, otherwise the number of the text the client shows.
    /// </summary>
    public int Result { get; init; }
    public required string Name { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Result);

        Debug.Assert(Name.Length <= 21);
        writer.WriteUtf16UnicodeString(Name);
    }

#endregion Interface: IWritableData
}
