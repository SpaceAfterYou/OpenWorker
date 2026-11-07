using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Post.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct PostRecvDelRequest(BinaryReader reader) : IRequestHotspotMessage, IWritableData
{
    private const GroupOpcode Group = GroupOpcode.Post;
    private const PostOpcode Command = PostOpcode.RecvDel;

    public MessageOpcode Opcode => new(Group, Command);

    public int Npc { get; } = reader.ReadInt32();
    public long Serial { get; } = reader.ReadInt64();
    public byte State { get; } = reader.ReadByte();

    public void Write(BinaryWriter writer)
    {
        writer.Write(Npc);
        writer.Write(Serial);
        writer.Write(State);
    }
}
