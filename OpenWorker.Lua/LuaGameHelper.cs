using Lua;

namespace OpenWorker.Lua;

/// <summary>
/// What a script holds on to after it has put a trap in the maze.
/// </summary>
[LuaObject]
public partial class LuaTrap : ILuaUserData
{
    /// <summary>
    /// How long the trap stays, in seconds.
    /// </summary>
    [LuaMember]
    public void SetLifeTime(float time)
    {
        LifeTime = time;
    }

    [LuaIgnoreMember]
    public float LifeTime { get; private set; }
}

/// <summary>
/// The helper that scripts get from <c>Soulworker.GetHelper()</c>. A trap of a script hurts the player, which
/// nothing here does yet, so the traps are accepted and then forgotten: a script goes on as if they were there.
/// </summary>
[LuaObject]
public partial class LuaGameHelper : ILuaUserData
{
    [LuaMember]
    public void CreateEntity(LuaMaze maze, VVector3 position, string kind, string name, string animation)
    {
    }

    [LuaMember]
    public void CreateProjectile(LuaMaze maze, VVector3 position, VVector3 direction, int skill, float speed)
    {
    }

    [LuaMember]
    public LuaTrap CreateTrap(LuaMaze maze, VVector3 position, int skill)
    {
        return new LuaTrap();
    }

    [LuaMember]
    public LuaTrap CreateTrapEx(LuaMaze maze, int box, int skill)
    {
        return new LuaTrap();
    }

    [LuaMember]
    public LuaTrap CreateTrapLoop(LuaMaze maze, VVector3 position, int skill, int count)
    {
        return new LuaTrap();
    }

    [LuaMember]
    public LuaTrap CreateTrapExLoop(LuaMaze maze, int box, int skill, int count)
    {
        return new LuaTrap();
    }

    [LuaMember]
    public LuaTrap CreateTrapGroup(LuaMaze maze, int box, int skill, int count)
    {
        return new LuaTrap();
    }

    /// <summary>
    /// The number the client knows an object of a map by: the object itself, as long as there is a single layer.
    /// </summary>
    [LuaMember]
    public int GetEventUniqueID(int identifier, int layer)
    {
        return identifier;
    }
}
