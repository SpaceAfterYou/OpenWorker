using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Post.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Post.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct PostRecvListResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Post;
    private const PostOpcode Command = PostOpcode.RecvList;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    [WireArray(WireLength.Byte)]
    public required PostValue[] Values { get; init; }
    public required byte Flag { get; init; }
}
