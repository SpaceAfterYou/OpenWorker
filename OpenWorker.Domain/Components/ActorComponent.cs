using System.Runtime.CompilerServices;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;

namespace OpenWorker.Domain.Components;

[EntityComponent(EntityComponentService.District)]
public readonly struct ActorComponent
{
    /// <summary>
    /// 29 bits
    /// </summary>
    public int Identifier { get; init; }

    /// <summary>
    /// 3 bits
    /// </summary>
    public ActorType Type { get; init; }
    
    public ActorComponent(int raw)
    {
        Identifier = (int)(raw & 0x1FFFFFFFu);
        Type = (ActorType)(byte)((raw >> 29) & 0x7);
    }
    
    public ActorComponent(int identifier, ActorType type)
    {
        Identifier = identifier;
        Type = type;
    }
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static bool operator==(ActorComponent left, ActorValue right) => left.Identifier == right.Identifier && left.Type == right.Type;
    //
    // public static bool operator !=(ActorComponent left, ActorValue right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ActorComponent(int value) => new(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ActorValue(ActorComponent obj) => new(obj.Identifier, obj.Type);
    
    public static implicit operator int(ActorComponent obj) 
    {
        var value = 0;
        
        value |= (int)(obj.Identifier & 0x1FFFFFFFu);
        value |= ((int)((byte)obj.Type & 0x7)) << 29;
        
        return value;
    }
}