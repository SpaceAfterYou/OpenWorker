using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Post.Types;

namespace OpenWorker.Hotspot.Modules.Post.Responses;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct PostSendResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Post;
    private const PostOpcode Command = PostOpcode.Send;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required int Result { get; init; }
    public required PostValue Value { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Result);
        Value.Write(writer);
    }
}
