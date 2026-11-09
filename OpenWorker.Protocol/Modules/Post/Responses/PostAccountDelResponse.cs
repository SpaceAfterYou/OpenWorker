using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Post.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct PostAccountDelResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Post;
    private const PostOpcode Command = PostOpcode.AccountDel;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required long Serial { get; init; }
    public required short PostCount { get; init; }
}
