using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Post.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PostAccountReceiptResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Post;
    private const PostOpcode Command = PostOpcode.AccountReceipt;

    public MessageOpcode Opcode => new(Group, Command);

    public required long Serial { get; init; }
    public required byte PostFlag { get; init; }
    public required long RemainTime { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Serial);
        writer.Write(PostFlag);
        writer.Write(RemainTime);
    }
}
