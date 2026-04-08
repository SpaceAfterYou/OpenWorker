using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SkillDivergenceLearnResponse(BinaryReader reader) : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.SkillDivergenceLearn;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Skill { get; init; } = reader.ReadInt32();

    public int Divergence { get; init; } = reader.ReadInt32();

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Skill);
        writer.Write(Divergence);
    }

#endregion Interface: IWritableData
}
