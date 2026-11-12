using OpenWorker.Hotspot.Messages.Response.Person.Components.Entries;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;

public readonly struct EquipItemValueEntry : IWireReadable<EquipItemValueEntry>, IWireWritable
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
        this = WireBridge.Read<EquipItemValueEntry>(reader);
    }

    /// <summary>
    /// The upgrade first, then the item.
    /// </summary>
    public static EquipItemValueEntry Read(ref PacketReader reader)
    {
        var upgrade = reader.ReadByte();
        var id = reader.ReadInt32();

        return new EquipItemValueEntry(id, upgrade);
    }

    public EquipItemValueEntry(int id, byte upgrade = 0)
    {
        Id = id;
        Upgrade = upgrade;
    }

    /// <summary>
    /// The upgrade first, then the item: the order the client reads, not the one of the properties.
    /// </summary>
    public void Write(ref PacketWriter writer)
    {
        writer.WriteByte(Upgrade);
        writer.WriteInt32(Id);
    }
}
