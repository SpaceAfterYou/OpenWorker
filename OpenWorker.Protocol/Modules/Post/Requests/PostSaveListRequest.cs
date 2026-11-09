using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Post.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct PostSaveListRequest(BinaryReader reader) : IRequestHotspotMessage, IWritableData
{
    private const GroupOpcode Group = GroupOpcode.Post;
    private const PostOpcode Command = PostOpcode.SaveList;

    public MessageOpcode Opcode => new(Group, Command);

    public int Npc { get; } = reader.ReadInt32();

    public void Write(BinaryWriter writer)
    {
        writer.Write(Npc);
    }
}
