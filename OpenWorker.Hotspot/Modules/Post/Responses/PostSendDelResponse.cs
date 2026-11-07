using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Post.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PostSendDelResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Post;
    private const PostOpcode Command = PostOpcode.SendDel;

    public MessageOpcode Opcode => new(Group, Command);

    public required long Serial { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Serial);
    }
}
