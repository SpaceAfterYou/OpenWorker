using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Party.Requests;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct PartyInviteRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.Invite;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    [WireString(WireStringFormat.Utf16Unicode)]
    public string TargetName { get; }
    [WireString(WireStringFormat.Utf16Unicode)]
    public string RequesterName { get; }
    public ActorValue RequesterActor { get; }
    public ActorValue TargetActor { get; }
    public int Server { get; }
    public int Result { get; }
    public byte Select { get; }
}
