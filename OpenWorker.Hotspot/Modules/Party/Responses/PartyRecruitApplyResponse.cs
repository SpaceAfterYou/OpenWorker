using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Party.Enums;
using OpenWorker.Hotspot.Modules.Party.Extensions;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PartyRecruitApplyResponse(BinaryReader reader) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitApply;

    public MessageOpcode Opcode => new(Group, Command);

    public PartyApplyError ErrorCode { get; init; } = reader.ReadPartyApplyError();
    public PartyType Type { get; init; } = reader.ReadPartyType();

    public void Write(BinaryWriter writer)
    {
        writer.Write(ErrorCode);
        writer.Write(Type);
    }
}
