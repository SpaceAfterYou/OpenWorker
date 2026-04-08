using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Responses;

/// <summary>
/// Client <c>sub_95DDD0</c>: <c>UXActorID</c>, byte, float.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SkillDefenceTypeResponse(BinaryReader reader) : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.SkillDefenceType;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public ActorValue Actor { get; init; } = new(reader);

    public byte DefenseType { get; init; } = reader.ReadByte();

    /// <summary>Third argument to <c>CMover::SetDefenseType</c> (e.g. gauge / blend).</summary>
    public float DefenseParam { get; init; } = reader.ReadSingle();

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Actor);
        writer.Write(DefenseType);
        writer.Write(DefenseParam);
    }

#endregion Interface: IWritableData
}
