using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Skill.Types;

namespace OpenWorker.Hotspot.Modules.Skill.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct SkillActiveSkillRequest(BinaryReader reader) : IRequestHotspotMessage, IWritableData
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.ActiveSkillReq;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public ActiveSkillValue Skill { get; } = new(reader);

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        Skill.Write(writer);
    }

#endregion Interface: IWritableData
}
