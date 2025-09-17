using OpenWorker.Hotspot.Messages.Response.Person.Components.Entries;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;

public readonly struct StatusEffectValueEntry
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

    public StatusEffectValueEntry(BinaryReader reader)
    {
        Id = reader.ReadInt32();
        Time = reader.ReadSingle();
        Count = reader.ReadByte();
        Owner = reader.ReadInt32();
    }
}