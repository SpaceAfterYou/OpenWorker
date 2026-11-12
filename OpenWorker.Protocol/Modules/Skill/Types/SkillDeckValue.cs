using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

[WireMessage]
public readonly partial struct SkillDeckValue
{
#region Message: Body

    public ushort Position { get; init; }

    public int Skill1 { get; init; }
    public int Skill2 { get; init; }
    public int Skill3 { get; init; }
    public int Skill4 { get; init; }

#endregion Message: Body

    public IEnumerable<int> GetList()
    {
        yield return Skill1;
        yield return Skill2;
        yield return Skill3;
        yield return Skill4;
    }
}
