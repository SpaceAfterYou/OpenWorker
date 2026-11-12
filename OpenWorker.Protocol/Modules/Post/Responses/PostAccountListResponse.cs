using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Post.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Post.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct PostAccountListResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Post;
    private const PostOpcode Command = PostOpcode.AccountList;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    [WireArray(WireLength.Signed16)]
    public required PostAccountValue[] Values { get; init; }
    public required byte Flag { get; init; }
}
