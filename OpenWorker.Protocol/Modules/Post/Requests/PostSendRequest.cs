using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Post.Types;

namespace OpenWorker.Hotspot.Modules.Post.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct PostSendRequest(BinaryReader reader) : IRequestHotspotMessage, IWritableData
{
    private const GroupOpcode Group = GroupOpcode.Post;
    private const PostOpcode Command = PostOpcode.Send;

    public MessageOpcode Opcode => new(Group, Command);

    public int Npc { get; } = reader.ReadInt32();
    public PostSendValue Value { get; } = new(reader);

    public void Write(BinaryWriter writer)
    {
        writer.Write(Npc);
        Value.Write(writer);
    }
}
