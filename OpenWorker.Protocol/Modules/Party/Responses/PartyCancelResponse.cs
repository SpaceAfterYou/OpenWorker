using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct PartyCancelResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.Cancel;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public ActorValue ReqActor { get; init; }
    public ActorValue RejActor { get; init; }
    [WireString(WireStringFormat.Utf16Unicode)]
    public required string RejName { get; init; }
    public int ErrorCode { get; init; }
}
