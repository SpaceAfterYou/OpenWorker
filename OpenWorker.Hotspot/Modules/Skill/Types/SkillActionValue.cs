using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

public readonly struct SkillActionValue(BinaryReader reader) : IWritableData
{
#region Message: Body

    public SkillActorInfoValue SkillActorInfo { get; init; } = new(reader);

    public int Skill { get; init; } = reader.ReadInt32();

    public short TriggerIndex { get; init; } = reader.ReadInt16();

    public ushort ContinuousHit { get; init; } = reader.ReadUInt16();

    public bool IsCounterAttack { get; init; } = reader.ReadBoolean();

    public bool IsPenetrate { get; init; } = reader.ReadBoolean();

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        SkillActorInfo.Write(writer);

        writer.Write(Skill);
        writer.Write(TriggerIndex);
        writer.Write(ContinuousHit);
        writer.Write(IsCounterAttack);
        writer.Write(IsPenetrate);
    }

#endregion Interface: IWritableData
}
