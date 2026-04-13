using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Party.Types;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PartyRecruitAddResponse(BinaryReader reader) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitAdd;

    public MessageOpcode Opcode => new(Group, Command);

    public bool IsSuccess { get; init; } = reader.ReadBoolean();

    /// <summary>
    /// TODO: Time? (fTime / 60, (fTime % 60))
    /// </summary>
    public float Unknown { get; init; } = reader.ReadSingle();

    public PartyRecruitMemberValue Member { get; init; } = new(reader);

    public void Write(BinaryWriter writer)
    {
        writer.Write(IsSuccess);
        writer.Write(Unknown);

        Member.Write(writer);
    }
}
