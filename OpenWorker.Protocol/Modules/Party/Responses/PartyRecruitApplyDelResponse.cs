using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

/// <summary>
/// An application to a recruitment is gone: the client takes the applicant off the list it shows for that recruitment.
/// The client never asks for it; the server does not send it yet, since the applications of the recruitment board are not
/// kept.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct PartyRecruitApplyDelResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitApplyDel;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public ActorValue Applicant { get; init; }
    public int Recruit { get; init; }
}
