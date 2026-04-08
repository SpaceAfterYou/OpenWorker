using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Skill.Requests;
using OpenWorker.Hotspot.Modules.Skill.Types;

namespace OpenWorker.Hotspot.Modules.Skill.Responses;


[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SkillLoadInfoResponse(BinaryReader reader) : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.SkillLoadInfo;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public SkillSnapshotValue Snapshot { get; init; } = new(reader);

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        Snapshot.Write(writer);
    }

#endregion Interface: IWritableData
}
