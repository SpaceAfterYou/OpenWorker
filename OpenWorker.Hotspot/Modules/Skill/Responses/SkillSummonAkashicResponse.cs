using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Skill.Requests;

namespace OpenWorker.Hotspot.Modules.Skill.Responses;

/// <summary>
/// Client <c>sub_95DAF0</c> then virtual call <c>(**)(actor, v13, v6, &amp;threeInts, flags, firstRead)</c>.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SkillSummonAkashicResponse(BinaryReader reader) : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.SkillSummonAkashic;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    /// <summary><c>UXActorID</c> — summoning actor.</summary>
    public ActorValue Actor { get; init; } = new(reader);

    /// <summary>Last argument to client vfunc — often card / session id (see <c>sub_95DAF0</c>).</summary>
    public int SummonContextId { get; init; } = reader.ReadInt32();

    /// <summary>Second vfunc parameter (<c>v13</c>).</summary>
    public int VirtualArg0 { get; init; } = reader.ReadInt32();

    /// <summary>Third vfunc parameter (<c>v6</c>).</summary>
    public int VirtualArg1 { get; init; } = reader.ReadInt32();

    /// <summary>Block passed as <c>int*</c> into the same vfunc.</summary>
    public AkashicSummonVirtualArgs VirtualPayload { get; init; } = new(reader);

    /// <summary>Byte flags consumed by the vfunc (<c>v2</c>).</summary>
    public byte SummonFlags { get; init; } = reader.ReadByte();

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Actor);
        writer.Write(SummonContextId);
        writer.Write(VirtualArg0);
        writer.Write(VirtualArg1);
        VirtualPayload.Write(writer);
        writer.Write(SummonFlags);
    }

#endregion Interface: IWritableData
}
