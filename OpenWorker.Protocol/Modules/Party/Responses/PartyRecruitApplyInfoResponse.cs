using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Party.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct PartyRecruitApplyInfoResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitApplyInfo;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Party { get; init; }
    [WireArray(WireLength.Signed32)]
    public PartyMemberInfoValue[] MemberList { get; init; }
    public ActorValue Actor { get; init; }
}
