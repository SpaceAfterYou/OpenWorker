using Lua;

namespace OpenWorker.Lua;

public partial class LuaMaze
{
    /// <summary>
    /// Monsters do not turn into others here: the script goes on with the old ones.
    /// </summary>
    [LuaMember]
    public void ChangeMonster(string from, string to)
    {
        NotDone("ChangeMonster");
        Logger.IgnoredChangeMonster(from, to);
    }

    [LuaMember]
    public void SetMonsterAllowPassiveType(int monster, int type)
    {
        NotDone("SetMonsterAllowPassiveType");
        Logger.IgnoredSetMonsterAllowPassiveType(monster, type);
    }

    [LuaMember]
    public void SetMonsterDefenceType(int monster, int type, float time)
    {
        NotDone("SetMonsterDefenceType");
        Logger.IgnoredSetMonsterDefenceType(monster, type, time);
    }
}
