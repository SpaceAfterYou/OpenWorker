using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Skill.Types;

namespace OpenWorker.Hotspot.Modules.Skill.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct SkillAkashicRecordRequest(BinaryReader reader) : IRequestHotspotMessage, IWritableData
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.AkashicRecordReq;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public byte Slot { get; init; } = reader.ReadByte();
    public SkillPositionValue SkillPosition { get; init; } = new(reader);

#endregion Message: Body

    public void Write(BinaryWriter writer)
    {
        writer.Write(Slot);

        SkillPosition.Write(writer);
    }
}
