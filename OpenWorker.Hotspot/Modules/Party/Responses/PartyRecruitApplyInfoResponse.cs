using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Party.Extensions;
using OpenWorker.Hotspot.Modules.Party.Types;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PartyRecruitApplyInfoResponse(BinaryReader reader) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitApplyInfo;

    public MessageOpcode Opcode => new(Group, Command);

    public PartyMemberInfoValue[] MemberList { get; init; } = reader.ReadPartyMemberInfoList_Int();
    public ActorValue Actor { get; init; } = new(reader);

    public void Write(BinaryWriter writer)
    {
        writer.Write_Int(MemberList);
        writer.Write(Actor);
    }
}
