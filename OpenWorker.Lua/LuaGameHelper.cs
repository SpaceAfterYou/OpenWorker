using Lua;

namespace OpenWorker.Lua;

[LuaObject]
public partial class LuaGameHelper : ILuaUserData
{
    public LuaTable? Metatable { get; set; }
    
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
        Console.WriteLine($"CreateTrap: {maze}, {position.X}/{position.Y}/{position.Z}, {skill}");
    }

    [LuaMember]
    public void CreateTrapEx(int box, int skill)
    {
        Console.WriteLine($"CreateTrapEx: {box}, {skill}");
    }

    [LuaMember]
    public void CreateTrapLoop()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void CreateTrapExLoop(int box, int skill, int index)
    {
        Console.WriteLine($"CreateTrapExLoop: {box}, {skill}, {index}");
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