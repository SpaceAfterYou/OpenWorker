using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Friends.Requests;

[HotspotMessage(Group, Command)]
public readonly struct FriendInviteAcceptRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Friend;
    private const FriendOpcode Command = FriendOpcode.InviteAccept;

    public int PersonRequested { get; } = reader.ReadInt32();
    public int PersonTarget { get; } = reader.ReadInt32();
    public string TargetName { get; } = reader.ReadPersonName();
    public bool IsAccepted { get; } = reader.ReadBoolean();

    public MessageOpcode Opcode => new(Group, Command);
}