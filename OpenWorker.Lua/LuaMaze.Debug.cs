using Lua;

namespace OpenWorker.Lua;

/// <summary>
/// What the chat commands for testing do to a maze that the script of the maze does not.
/// </summary>
public partial class LuaMaze
{
    /// <summary>
    /// Brings a spawn box of the map to life now, whatever its condition says, once.
    /// </summary>
    [LuaIgnoreMember]
    public void SpawnBox(int box)
    {
        if (!Closed)
        {
            creatureManager.SpawnOnce(box);
        }
    }
}
