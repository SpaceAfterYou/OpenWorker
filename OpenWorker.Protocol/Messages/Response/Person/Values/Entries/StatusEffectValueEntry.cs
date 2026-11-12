using OpenWorker.Hotspot.Messages.Response.Person.Components.Entries;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;

[WireMessage(WireMethods.Write)]
public readonly partial struct StatusEffectValueEntry : IWireReadable<StatusEffectValueEntry>
{
    public int Id { get; }
    public float Time { get; }
    public byte Count { get; }
    public int Owner { get; }

    public StatusEffectValueEntry(StatusEffectComponentEntry componentEntry)
    {
        Id = componentEntry.Id;
        Time = componentEntry.Time;
        Count = componentEntry.Count;
        Owner = componentEntry.Owner;
    }

    private StatusEffectValueEntry(int id, float time, byte count, int owner)
    {
        Id = id;
        Time = time;
        Count = count;
        Owner = owner;
    }

    public static StatusEffectValueEntry Read(ref PacketReader reader)
    {
        var id = reader.ReadInt32();
        var time = reader.ReadSingle();
        var count = reader.ReadByte();
        var owner = reader.ReadInt32();

        return new StatusEffectValueEntry(id, time, count, owner);
    }
}