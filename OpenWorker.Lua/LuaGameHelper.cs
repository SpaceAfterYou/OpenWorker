using System.Numerics;
using Lua;
using Microsoft.Extensions.Logging;

namespace OpenWorker.Lua;

/// <summary>
/// What a script holds on to after it has put a trap in the maze: nothing when the trap could not be laid.
/// </summary>
[LuaObject]
public partial class LuaTrap : ILuaUserData
{
    [LuaIgnoreMember]
    internal MapTrap? Trap { get; init; }

    /// <summary>
    /// The trap dies after this many seconds.
    /// </summary>
    [LuaMember]
    public void SetLifeTime(float time)
    {
        Trap?.LiveFor(time);
    }
}

/// <summary>
/// The helper that scripts get from <c>Soulworker.GetHelper()</c>. The traps it lays belong to the scene attacker of the
/// maze; a script that hands over no maze lays none.
/// </summary>
[LuaObject]
public partial class LuaGameHelper(ILogger<LuaGameHelper> logger) : ILuaUserData
{
    [LuaMember]
    public void CreateEntity(LuaMaze maze, VVector3 position, string kind, string name, string animation)
    {
        NotDone(maze, nameof(CreateEntity));
    }

    [LuaMember]
    public void CreateProjectile(LuaMaze maze, VVector3 position, VVector3 direction, int skill, float speed)
    {
        NotDone(maze, nameof(CreateProjectile));
    }

    [LuaMember]
    public LuaTrap CreateTrap(LuaMaze maze, VVector3 position, int skill)
    {
        return new LuaTrap { Trap = maze?.LayTrap(nameof(CreateTrap), Place(position), skill) };
    }

    /// <summary>
    /// A trap in the middle of a position box of the map, facing the way the box faces.
    /// </summary>
    [LuaMember]
    public LuaTrap CreateTrapEx(LuaMaze maze, int box, int skill)
    {
        return new LuaTrap { Trap = maze?.LayTrapInBox(nameof(CreateTrapEx), box, skill) };
    }

    /// <summary>
    /// A trap the script ends later by its number.
    /// </summary>
    [LuaMember]
    public LuaTrap CreateTrapLoop(LuaMaze maze, VVector3 position, int skill, int index)
    {
        return new LuaTrap { Trap = maze?.LayTrap(nameof(CreateTrapLoop), Place(position), skill, index) };
    }

    [LuaMember]
    public LuaTrap CreateTrapExLoop(LuaMaze maze, int box, int skill, int index)
    {
        return new LuaTrap { Trap = maze?.LayTrapInBox(nameof(CreateTrapExLoop), box, skill, number: index) };
    }

    /// <summary>
    /// A trap the script ends later with the others of its group.
    /// </summary>
    [LuaMember]
    public LuaTrap CreateTrapGroup(LuaMaze maze, int box, int skill, int group)
    {
        return new LuaTrap { Trap = maze?.LayTrapInBox(nameof(CreateTrapGroup), box, skill, group: group) };
    }

    private static Vector3 Place(VVector3? position)
    {
        return position is null ? Vector3.Zero : new Vector3(position.X, position.Y, position.Z);
    }

    /// <summary>
    /// A script may hand over no maze at all; the warning then names no script.
    /// </summary>
    private void NotDone(LuaMaze? maze, string function)
    {
        if (maze is not null)
        {
            maze.NotDone(function);
        }
        else if (ScriptWarnings.First(string.Empty, function))
        {
            logger.NotDoneByServer(function, string.Empty);
        }
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
