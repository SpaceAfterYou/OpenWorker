using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Post.Extensions;
using OpenWorker.Hotspot.Modules.Post.Types;

namespace OpenWorker.Hotspot.Modules.Post.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PostSendListResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Post;
    private const PostOpcode Command = PostOpcode.SendList;

    public MessageOpcode Opcode => new(Group, Command);

    public required PostValue[] Values { get; init; }
    public required byte Flag { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Values);
        writer.Write(Flag);
    }
}
