using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.Hotspot.Messages.Response.Person.Components.Entries;
using OpenWorker.Hotspot.Modules.Items.Components;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;

public readonly struct EquipItemValueEntry
{
    public int Id { get; }
    public byte Upgrade { get; }

    public EquipItemValueEntry(Entity entity)
    {
        if (Entity.Null == entity)
        {
            Id = -1;
            Upgrade = 0;
        }

        else
        {
            Id = entity.Get<StorageItemComponent>().Identifier;
            Upgrade = entity.Get<StorageItemGradeComponent>().Level;
        }
    }

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