using OpenWorker.Domain.Types;

namespace OpenWorker.Hotspot.Modules.Skill.Requests;

public readonly struct ActiveSkillValue
{
    public int nSkillID { get; init; }
    public ActorValue uxUseActorID { get; init; }
    public SkillPositionValue Position { get; init; }
    public int nDivergenceID { get; init; }
    public int nParentSkillID { get; init; }
    public byte bySkillDeckIndex { get; init; }
    public int nRandomKey { get; init; }
    
    public ActiveSkillValue(BinaryReader reader)
    {
        nSkillID = reader.ReadInt32();
        uxUseActorID = new ActorValue(reader);
        Position = new SkillPositionValue(reader);
        nDivergenceID = reader.ReadInt32();
        nParentSkillID = reader.ReadInt32();
        bySkillDeckIndex = reader.ReadByte();  
        nRandomKey = reader.ReadInt32();
    }
}