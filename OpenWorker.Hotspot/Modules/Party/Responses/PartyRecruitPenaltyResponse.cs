using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PartyRecruitPenaltyResponse(BinaryReader reader) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitPenalty;

    public MessageOpcode Opcode => new(Group, Command);

    public int Time { get; init; } = reader.ReadInt32();

    public void Write(BinaryWriter writer)
    {
        writer.Write(Time);
    }
}
