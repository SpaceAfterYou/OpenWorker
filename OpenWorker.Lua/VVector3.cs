using System.Numerics;
using Lua;

namespace OpenWorker.Lua;

[LuaObject]
public partial class VVector3
{
    [LuaIgnoreMember]
    private float[] Data { get; } = new float[3];

    [LuaMember("x")]
    public float X
    {
        get => Data[0];
        set => Data[0] = value;
    }

    [LuaMember("y")]
    public float Y
    {
        get => Data[1]; 
        set => Data[1] = value;
    }

    [LuaMember("z")]
    public float Z
    {
        get => Data[2]; 
        set => Data[2] = value;
    }
    
    [LuaMember("r")]
    public float R
    {
        get => Data[0];
        set => Data[0] = value;
    }

    [LuaMember("g")]
    public float G
    {
        get => Data[1]; 
        set => Data[1] = value;
    }

    [LuaMember("b")]
    public float B
    {
        get => Data[2]; 
        set => Data[2] = value;
    }
    
    [LuaIgnoreMember]
    public static implicit operator VVector3(Vector3 value)
    {
        return new VVector3
        {
            X = value.X,
            Y = value.Y,
            Z = value.Z
        };
    }
}