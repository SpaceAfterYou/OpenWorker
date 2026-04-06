using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Friends.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct FriendRecruitListResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Friend;
    private const FriendOpcode Command = FriendOpcode.RecruitList;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public required FriendRecruitValue[] List { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)List.Length);

        foreach (var value in List)
        {
            writer.WritePersonName(value.Name);
            writer.Write(value.Person);
            writer.Write(value.Level);
            writer.Write(value.Hero);
            writer.Write(value.Status);
            writer.WriteCommunityMemo(value.Memo);
            writer.Write(value.Channel);
            writer.Write(value.World);
            writer.Write(value.IsLoggedIn);
            writer.Write(value.tLogOut);
            writer.Write(value.tAddTime);
        }
    }

#endregion Interface: IWritableData
}
