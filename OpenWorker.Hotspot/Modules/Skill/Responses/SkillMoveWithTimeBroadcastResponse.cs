using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SkillMoveWithTimeBroadcastResponse(BinaryReader reader) : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.SkillMoveWithTimeBt;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public ActorValue Actor { get; init; } = new(reader);
    public Vector3 Position { get; init; } = reader.ReadVector3();

    /// <example>fDuration = fDist / fMaxSpeed;</example>
    public float Time { get; init; } = reader.ReadSingle();

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Actor);
        writer.Write(Position);
        writer.Write(Time);
    }

#endregion Interface: IWritableData
}
