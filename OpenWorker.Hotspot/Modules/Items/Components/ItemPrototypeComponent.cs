using Arch.Core;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.Hotspot.Modules.Items.Components;

public readonly record struct StorageItemPrototypeComponent(int Index);

public readonly record struct StorageItemComponent(int Identifier);

public readonly record struct StorageItemGradeComponent(byte Level);
public readonly record struct StorageItemOptionValue(short Type, int Value);
public readonly record struct StorageItemOptionComponent(StorageItemOptionValue[] Collection);

public readonly record struct StorageGradeComponent(byte Level);

public readonly record struct StorageSlotComponent(short Count, short Limit);

public readonly record struct StorageContentComponent(Entity[] SlotList)
{
    public Entity this[AbilityEquipSlot index]
    {
        get => SlotList[(int)index];
        set => SlotList[(int)index] = value;
    }

    public Entity this[int index]
    {
        get => SlotList[index];
        set => SlotList[index] = value;
    }
}

public readonly record struct StorageGroupComponent(StorageGroup Group);

[EntityComponent(EntityComponentService.District)]
public readonly record struct StorageComponent(Entity[] Collection);

internal readonly struct StorageArchetype
{
    public required StorageGroup Group { get; init; }
    public required short Size { get; init; }
    public required ComponentType[] Components { get; init; }
}