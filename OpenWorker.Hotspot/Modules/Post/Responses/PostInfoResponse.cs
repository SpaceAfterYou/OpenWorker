using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Post.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PostInfoResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Post;
    private const PostOpcode Command = PostOpcode.Info;

    public MessageOpcode Opcode => new(Group, Command);

    public required short NewPostCount { get; init; }
    public required short NewAccountPostCount { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(NewPostCount);
        writer.Write(NewAccountPostCount);
    }
}
