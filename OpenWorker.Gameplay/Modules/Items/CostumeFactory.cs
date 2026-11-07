using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Gameplay.Modules.Items.Components;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// Puts the clothes chosen when a character was created into its shape equipment.
/// </summary>
public sealed class CostumeFactory(
    World world,
    ReadOnlyCollection<CreateClothRow> outfits,
    ReadOnlyCollection<ItemRow> items)
{
    // The item table marks the equipment slot of an item with a '1' in a string with one character
    // for each slot. Items that cannot be worn have a shorter string.
    private Dictionary<int, int> Slots { get; } = items
        .Where(e => e.Field18.Length == ItemModuleDefines.EquippedItemCount)
        .DistinctBy(e => e.Id)
        .ToDictionary(e => e.Id, e => e.Field18.IndexOf('1'));

    public void Dress(Entity storage, int outfit)
    {
        var row = outfits.FirstOrDefault(e => e.Id == outfit);

        if (row.Id == 0)
        {
            return;
        }

        var content = world.Get<StorageContentComponent>(storage);

        foreach (var item in (int[])[row.Field1, row.Field2, row.Field3, row.Field4, row.Field5])
        {
            if (Slots.TryGetValue(item, out var slot) && slot >= 0 && slot < content.SlotList.Length)
            {
                content[slot] = world.Create(new StorageItemComponent(item), new StorageItemGradeComponent(0));
            }
        }
    }
}
