using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Friends.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct FriendRecruitListRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Friend;
    private const FriendOpcode Command = FriendOpcode.RecruitList;

    public int Person { get; } = reader.ReadInt32();
    public byte MinLevel { get; } = reader.ReadByte();
    public byte MaxLevel { get; } = reader.ReadByte();
    public Hero Hero { get; } = reader.ReadHero();

    public MessageOpcode Opcode => new(Group, Command);
}