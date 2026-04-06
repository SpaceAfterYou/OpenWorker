using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Friends.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct FriendDeleteRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Friend;
    private const FriendOpcode Command = FriendOpcode.Delete;

    public int Friend { get; } = reader.ReadInt32();
    public bool IsShowMessage { get; } = reader.ReadBoolean();

    public MessageOpcode Opcode => new(Group, Command);
}