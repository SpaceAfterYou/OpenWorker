using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Post.Requests;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly struct PostAccountDelRequest(BinaryReader reader) : IRequestHotspotMessage, IWritableData
{
    private const GroupOpcode Group = GroupOpcode.Post;
    private const PostOpcode Command = PostOpcode.AccountDel;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Npc { get; } = reader.ReadInt32();
    public long Serial { get; } = reader.ReadInt64();

    public void Write(BinaryWriter writer)
    {
        writer.Write(Npc);
        writer.Write(Serial);
    }
}
