using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Friends.Requests;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct FriendInfoRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Friend;
    private const FriendOpcode Command = FriendOpcode.Info;

    public int Friend { get; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}