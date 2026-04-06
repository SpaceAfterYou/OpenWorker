using OpenWorker.Hotspot.Messages.Response.Person.Components.Entries;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;

public readonly struct EquipItemValueEntry
{
    public int Id { get; }
    public byte Upgrade { get; }

    public EquipItemValueEntry(InventoryComponentEntry entry)
    {
        Id = entry.Id;
        Upgrade = entry.Upgrade;
    }

    public EquipItemValueEntry(EquipItemComponentEntry entry)
    {
        Id = entry.Id;
        Upgrade = entry.Upgrade;
    }

    public EquipItemValueEntry(BinaryReader reader)
    {
        Id = reader.ReadInt32();
        Upgrade = reader.ReadByte();
    }

    public EquipItemValueEntry(int id, byte upgrade = 0)
    {
        Id = id;
        Upgrade = upgrade;
    }
}
