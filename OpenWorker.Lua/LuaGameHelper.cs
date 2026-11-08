using Lua;
using Microsoft.Extensions.Logging;

namespace OpenWorker.Lua;

[LuaObject]
public partial class LuaGameHelper(ILogger<LuaGameHelper> logger) : ILuaUserData
{
    [LuaMember]
    public void CreateEntity()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void CreateProjectile()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void CreateTrap(LuaMaze maze, VVector3 position, int skill)
    {
        logger.IgnoredCreateTrap(position.X, position.Y, position.Z, skill);
    }

    [LuaMember]
    public void CreateTrapEx(int box, int skill)
    {
        logger.IgnoredCreateTrapEx(box, skill);
    }

    [LuaMember]
    public void CreateTrapLoop()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void CreateTrapExLoop(int box, int skill, int index)
    {
        logger.IgnoredCreateTrapExLoop(box, skill, index);
    }

    [LuaMember]
    public void CreateTrapGroup()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void FindUser()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void FindArea()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void GetEventUniqueID()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void Cast()
    {
        throw new NotImplementedException();
    }
}