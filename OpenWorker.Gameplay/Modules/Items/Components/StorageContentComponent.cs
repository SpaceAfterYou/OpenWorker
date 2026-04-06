using Arch.Core;
using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.Gameplay.Modules.Items.Components;

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