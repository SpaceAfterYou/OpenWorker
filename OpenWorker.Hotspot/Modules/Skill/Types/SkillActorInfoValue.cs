using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

public readonly struct SkillActorInfoValue(BinaryReader reader) : IWritableData
{
#region Message: Body

    public ActorValue UxActorId { get; init; } = new(reader);

    public SkillActionPosInfoValue SkillPosInfo { get; init; } = new(reader);

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.WriteActor(UxActorId);

        SkillPosInfo.Write(writer);
    }

#endregion Interface: IWritableData
}
