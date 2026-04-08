using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Responses;

/// <summary>
/// Client <c>sub_95DD30</c>: dword + float (applied when dword matches local actor).
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SkillCooltimeReduceResponse(BinaryReader reader) : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.SkillCooltimeReduce;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    /// <summary>Compared to local player id in <c>sub_95DD30</c>.</summary>
    public int ActorId { get; init; } = reader.ReadInt32();

    /// <summary>Passed to <c>sub_63E6C0</c> (cooldown scale / reduction factor).</summary>
    public float CooldownFactor { get; init; } = reader.ReadSingle();

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(ActorId);
        writer.Write(CooldownFactor);
    }

#endregion Interface: IWritableData
}
