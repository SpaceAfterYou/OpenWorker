using Lua;

namespace OpenWorker.Lua;

public partial class LuaMaze
{
    [LuaMember]
    public void SetEscortCondition(int condition)
    {
        NotDone("SetEscortCondition");
        Logger.IgnoredSetEscortCondition(condition);
    }

    [LuaMember]
    public void SetEscortMonster(LuaValue monster, LuaValue condition, LuaValue name)
    {
        NotDone("SetEscortMonster");
        Logger.IgnoredSetEscortMonster();
    }
}
