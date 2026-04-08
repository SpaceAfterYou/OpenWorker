using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SkillBuffChangeBroadcastResponse(BinaryReader reader) : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.BuffChangeBt;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public ActorValue Target { get; init; } = new(reader);
    public short Buff { get; init; } = reader.ReadInt16();
    public short NewBuff { get; init; } = reader.ReadInt16();
    public float Time { get; init; } = reader.ReadSingle();
    public byte Count { get; init; } =  reader.ReadByte();
    public ActorValue Owner { get; init; } = new(reader);

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Target);
        writer.Write(Buff);
        writer.Write(NewBuff);
        writer.Write(Time);
        writer.Write(Count);
        writer.Write(Owner);
    }

#endregion Interface: IWritableData
}
