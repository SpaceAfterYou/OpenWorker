using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Post.Requests;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly struct PostInfoRequest : IRequestHotspotMessage, IWritableData
{
    private const GroupOpcode Group = GroupOpcode.Post;
    private const PostOpcode Command = PostOpcode.Info;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public void Write(BinaryWriter writer)
    {
    }
}
