using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Party.Requests;

/// <summary>
/// An applicant asks who is in the party of a recruitment.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct PartyRecruitApplyInfoRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitApplyInfo;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    /// <summary>
    /// The number of the recruitment, as the board shows it.
    /// </summary>
    public ActorValue Recruit { get; }
}
