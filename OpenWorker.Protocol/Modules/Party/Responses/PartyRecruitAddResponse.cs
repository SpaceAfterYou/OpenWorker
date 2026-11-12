using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Party.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct PartyRecruitAddResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitAdd;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public bool IsSuccess { get; init; }

    /// <summary>
    /// TODO: Time? (fTime / 60, (fTime % 60))
    /// </summary>
    public int RemainTime { get; init; }

    public PartyRecruitMemberValue Member { get; init; }
}
