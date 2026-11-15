using System.Numerics;
using OpenWorker.Hotspot;

namespace OpenWorker.Gameplay.Modules.SocialItems;

/// <summary>
/// An object a player has put down in a channel of the district: everybody who sees it may use it until its time runs out or
/// its owner leaves.
/// </summary>
public sealed class SocialObject
{
    internal SocialObject(int actor, short channel, int owner, int item, long serial, ushort socialItem, ushort aura, Vector3 position,
        float rotation, float radius, int seats)
    {
        Actor = actor;
        Channel = channel;
        Owner = owner;
        Item = item;
        Serial = serial;
        SocialItem = socialItem;
        Aura = aura;
        Position = position;
        Rotation = rotation;
        Radius = radius;
        Seats = seats;
    }

    public int Actor { get; }

    /// <summary>The index of the channel the object stands in.</summary>
    public short Channel { get; }

    /// <summary>The actor of the player who put it down.</summary>
    public int Owner { get; }

    public int Item { get; }

    /// <summary>The stack of the owner's bag the object was put down from.</summary>
    public long Serial { get; }

    public ushort SocialItem { get; }

    /// <summary>The aura a player carries while using the object; zero for none.</summary>
    public ushort Aura { get; }

    public Vector3 Position { get; }

    public float Rotation { get; }

    /// <summary>Half the width of the ground it takes.</summary>
    public float Radius { get; }

    /// <summary>How many players besides the owner may use it at once.</summary>
    public int Seats { get; }

    /// <summary>The actors who use it, in the order they came.</summary>
    public List<int> Users { get; } = [];

    /// <summary>The game time the object goes at; nothing for an object without an end.</summary>
    public TimeSpan? Ends { get; internal set; }

    internal ScheduledAction End { get; set; }

    internal int CellX { get; set; }

    internal int CellY { get; set; }

    /// <summary>
    /// Whether another player may use the object: the owner always may, the others while a seat is free. The owner's seat is
    /// its own, so the others have as many seats with or without it.
    /// </summary>
    public bool HasRoomFor(int actor)
    {
        if (Users.Contains(actor))
        {
            return false;
        }

        if (actor == Owner)
        {
            return true;
        }

        var others = Users.Contains(Owner) ? Users.Count - 1 : Users.Count;

        return others < Seats;
    }
}
