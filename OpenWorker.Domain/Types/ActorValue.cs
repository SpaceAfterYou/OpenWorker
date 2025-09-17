using System.Runtime.CompilerServices;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Domain.Types;

public readonly struct ActorValue
{
    /// <summary>
    /// 29 bits
    /// </summary>
    public int Identifier { get; init; }

    /// <summary>
    /// 3 bits
    /// </summary>
    public ActorType Type { get; init; }

    public ActorValue(BinaryReader reader) : this(reader.ReadInt32()) {}
    
    public ActorValue(int raw)
    {
        Identifier = (int)(raw & 0x1FFFFFFFu);
        Type = (ActorType)(byte)((raw >> 29) & 0x7);
    }
    
    public ActorValue(int identifier, ActorType type)
    {
        Identifier = identifier;
        Type = type;
    }
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static bool operator==(ActorValue right, ActorComponent left) => left.Identifier == right.Identifier && left.Type == right.Type;
    //
    // public static bool operator !=(ActorValue right, ActorComponent left) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ActorValue(int value) => new(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ActorComponent(ActorValue obj) => new(obj.Identifier, obj.Type);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(ActorValue obj)
    {
        int value = 0;
        
        value |= (int)(obj.Identifier & 0x1FFFFFFFu);
        value |= ((int)((byte)obj.Type & 0x7)) << 29;
        
        return value;
    }
}