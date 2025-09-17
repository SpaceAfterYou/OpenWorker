using OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;

namespace OpenWorker.Hotspot.Messages.Response.Person.Components.Entries;

public sealed class EquipItemComponentEntry(int id = -1, byte upgrade = 0)
{
    public EquipItemComponentEntry(EquipItemValueEntry valueEntry) : this(valueEntry.Id, valueEntry.Upgrade)
    {
    }

    public int Id { get; } = id;
    public byte Upgrade { get; set; } = upgrade;
}