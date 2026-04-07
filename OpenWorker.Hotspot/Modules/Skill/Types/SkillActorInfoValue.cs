using System.IO;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Requests;

public readonly struct SkillActorInfoValue(BinaryReader reader) : IWritableData
{
    public ActorValue UxActorId { get; init; } = new(reader);

    public SkillActionPosInfoValue SkillPosInfo { get; init; } = new(reader);

    public void Write(BinaryWriter writer)
    {
        writer.WriteActor(UxActorId);

        SkillPosInfo.Write(writer);
    }
}
