using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Party.Types;

namespace OpenWorker.Hotspot.Modules.Party.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct PartyRecruitAddRequest(BinaryReader reader) : IRequestHotspotMessage, IWritableData
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitAdd;

    public MessageOpcode Opcode => new(Group, Command);

    public PartyRecruitMemberValue Member { get; init; } = new(reader);

    public void Write(BinaryWriter writer)
    {
        Member.Write(writer);
    }
}
