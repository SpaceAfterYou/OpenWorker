using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Post.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PostSendBackResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Post;
    private const PostOpcode Command = PostOpcode.SendBack;

    public MessageOpcode Opcode => new(Group, Command);

    public required int Result { get; init; }
    public required long Serial { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Result);
        writer.Write(Serial);
    }
}
