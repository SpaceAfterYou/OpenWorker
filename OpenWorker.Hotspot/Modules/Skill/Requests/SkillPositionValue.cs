using System.Numerics;
using OpenWorker.Hotspot.Extensions;

namespace OpenWorker.Hotspot.Modules.Skill.Requests;

public readonly struct SkillPositionValue
{
    public Vector3 xPos { get; init; } 
    public float fAngle { get; init; } 
    public short nMotionClass { get; init; } 
    
    /// <summary>
    /// TODO: Unused bSwapSkill or bInputFlag
    /// </summary>
    // public bool bSwapSkill { get; init; } 
    
    public bool bInputFlag { get; init; } 
    
    public SkillPositionValue(BinaryReader reader)
    {
        xPos = reader.ReadVector3();
        fAngle = reader.ReadSingle();
        nMotionClass = reader.ReadInt16();
        // bSwapSkill = reader.ReadBoolean();
        bInputFlag = reader.ReadBoolean();
    }
}