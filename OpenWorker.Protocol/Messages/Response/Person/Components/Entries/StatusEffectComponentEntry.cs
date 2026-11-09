using OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;

namespace OpenWorker.Hotspot.Messages.Response.Person.Components.Entries;

public sealed class StatusEffectComponentEntry(int id, float time, byte count, int owner)
{
    public StatusEffectComponentEntry(StatusEffectValueEntry value) :
        this(value.Id, value.Time, value.Count, value.Owner)
    {
    }

    public int Id { get; init; } = id;
    public float Time { get; set; } = time;
    public byte Count { get; set; } = count;
    public int Owner { get; init; } = owner;
}