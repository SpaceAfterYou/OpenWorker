namespace OpenWorker.Hotspot.Modules.World.Responses;

public readonly struct CreatureComponent : IEquatable<CreatureComponent>
{
    public int Identifier { get; init; }
    public byte Level { get; init; }

    public static implicit operator int(CreatureComponent obj)
    {
        return obj.Identifier;
    }

    public bool Equals(CreatureComponent other)
    {
        return Identifier == other.Identifier;
    }

    public override bool Equals(object? obj)
    {
        return obj is CreatureComponent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Identifier;
    }
    
    public static bool operator ==(CreatureComponent left, CreatureComponent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CreatureComponent left, CreatureComponent right)
    {
        return !(left == right);
    }
}