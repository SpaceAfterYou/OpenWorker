using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Friends.Responses;

enum ENUM_FRIEND_STATE : byte
{
    eFRIEND_STATE_NONE = 0x0,
    eFRIEND_TYPE_PARTY = 0x1,
}

public readonly struct FriendRecruitValue
{
    public string Name { get; init; }
    
    /// <summary>
    /// TODO
    /// </summary>
    public int Person { get; init; }
    public byte Level { get; init; }
    public Hero Hero { get; init; }
    public byte Status { get; init; }
    public string Memo { get; init; }
    public byte Channel { get; init; }
    public short World { get; init; }
    public bool IsLoggedIn { get; init; }
    public ulong tLogOut { get; init; }
    public ulong tAddTime { get; init; }
}

[HotspotMessage(Group, Command)]
public readonly struct FriendRecruitListResponse(FriendRecruitValue[] list) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Friend;
    private const FriendOpcode Command = FriendOpcode.RecruitList;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write((byte)list.Length);
        
        foreach (var value in list)
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
}