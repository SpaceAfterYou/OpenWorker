using Lua;

namespace OpenWorker.Lua;

public partial class LuaMaze
{
    [LuaMember]
    public void SetTrapLifeTime(int index, float time)
    {
        NotDone("SetTrapLifeTime");
        Logger.IgnoredSetTrapLifeTime(index, time);
    }
}
