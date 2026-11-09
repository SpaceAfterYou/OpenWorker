using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SkillSummonAkashicResponse(BinaryReader reader) : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.SkillSummonAkashic;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public ActorValue Owner { get; init; } = new(reader);
    public ActorValue Creature { get; init; } = new(reader);
    public float AlphaValuea { get; init; } = reader.ReadSingle();
    public float BlendingTime { get; init; } = reader.ReadSingle();
    public Vector3 Position { get; init; } = reader.ReadVector3();
    public bool ApplyRotation { get; init; } = reader.ReadBoolean();

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Owner);
        writer.Write(Creature);
        writer.Write(AlphaValuea);
        writer.Write(BlendingTime);
        writer.Write(Position);
        writer.Write(ApplyRotation);
    }

#endregion Interface: IWritableData
}
