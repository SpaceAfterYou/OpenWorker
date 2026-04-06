using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Gameplay.Modules.Items.Components;

namespace OpenWorker.Gameplay.Modules.Items;

public static class ItemDtoFactory
{
    public static ItemValue FromEntity(World world, Entity item)
    {
        if (Entity.Null == item)
        {
            return new ItemValue
            {
                Item = -1,
                Serial = new SerialValue(-1),
                OptionList = Enumerable
                    .Range(0, ItemModuleDefines.OptionCount)
                    .Select(_ => new ItemOption(0, 0))
                    .ToList(),
                BroachState = string.Empty
            };
        }

        var optionList = world.Has<StorageItemOptionComponent>(item)
            ? world.Get<StorageItemOptionComponent>(item).Collection
                .Select(x => new ItemOption { Type = x.Type, Option = x.Value })
                .ToList()
            : Enumerable.Range(0, ItemModuleDefines.OptionCount).Select(_ => new ItemOption(0, 0)).ToList();

        return new ItemValue
        {
            Item = world.Get<StorageItemComponent>(item).Identifier,
            Serial = new SerialValue(world.Get<StorageItemSerialComponent>(item).Serial),
            Count = world.Get<StorageSlotComponent>(item).Count,
            OptionList = optionList,
            Endurance = 100,
            BroachState = string.Empty,
            DueTo = DateTimeOffset.Now.AddDays(1)
        };
    }

    public static ItemCreateResponse CreateItemCreateResponse(World world, Entity storage, short[] indexList)
    {
        var component = world.Get<StorageContentComponent>(storage);
        var group = world.Get<StorageGroupComponent>(storage).Group;

        var list = indexList
            .Select(index => new StorageItemValue(group, index, FromEntity(world, component.SlotList[index])))
            .ToArray();

        return new ItemCreateResponse { List = list };
    }

    public static ItemInventoryInfoResponse CreateItemInventoryInfoResponse(World world, Entity entity,
        IReadOnlyCollection<StorageGroup> values)
    {
        var component = world.Get<StorageComponent>(entity);

        var list = component.Collection
            .Where(x => values.Contains(world.Get<StorageGroupComponent>(x).Group))
            .SelectMany(x =>
            {
                var group = world.Get<StorageGroupComponent>(x).Group;
                var slotList = world.Get<StorageContentComponent>(x).SlotList;

                return slotList
                    .Select((slotItem, index) => (Item: slotItem, Index: index))
                    .Where(e => Entity.Null != e.Item)
                    .Select(e => new StorageItemValue(group, (short)e.Index, FromEntity(world, e.Item)));
            })
            .ToArray();

        return new ItemInventoryInfoResponse { List = list };
    }

    public static ItemOpenSlotInfoResponse CreateOpenSlotInfoResponse(World world, Entity player,
        IReadOnlyCollection<StorageGroup> values)
    {
        var component = world.Get<StorageComponent>(player);
        var list = new List<ItemOpenSlotGroupEntry>();

        foreach (var group in values)
        {
            foreach (var storage in component.Collection)
            {
                if (world.Get<StorageGroupComponent>(storage).Group != group)
                {
                    continue;
                }

                var content = world.Get<StorageContentComponent>(storage);
                var grade = world.Has<StorageGradeComponent>(storage)
                    ? world.Get<StorageGradeComponent>(storage).Level
                    : byte.MinValue;

                list.Add(new ItemOpenSlotGroupEntry(group, (short)content.SlotList.Length, grade));
                break;
            }
        }

        return new ItemOpenSlotInfoResponse { Groups = list };
    }
}
