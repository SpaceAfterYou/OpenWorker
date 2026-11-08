using System.Runtime.CompilerServices;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Domain.Types;

/// <summary>
/// An actor on the wire: one 32-bit number, the identifier in the low 29 bits and the type in the high 3.
/// </summary>
public readonly struct ActorValue : IWireReadable<ActorValue>, IWireWritable
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

    public static ActorValue Read(ref PacketReader reader) => new(reader.ReadInt32());

    public void Write(ref PacketWriter writer) => writer.WriteInt32(this);

    private ActorValue(int value)
    {
        Identifier = (int)(value & 0x1FFFFFFFu);
        Type = (ActorType)(byte)((value >> 29) & 0x7);
    }
    
    public ActorValue(int identifier, ActorType type)
    {
        Identifier = identifier;
        Type = type;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ActorValue(int value) => new(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ActorComponent(ActorValue obj) => new(obj.Identifier, obj.Type);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(ActorValue obj)
    {
        var value = 0;
        
        value |= (int)(obj.Identifier & 0x1FFFFFFFu);
        value |= ((byte)obj.Type & 0x7) << 29;
        
        return value;
    }
}