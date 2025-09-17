using System.Collections.ObjectModel;
using System.Diagnostics;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Modules.Items.Components;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Requests;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Modules.Items;

public readonly record struct StorageAddInfo(Entity Item, short Slot, short Count);

public readonly record struct StorageAddResponse(bool State, Entity Storage, StorageAddInfo[] Info);

public sealed class StorageManager(
    Arch.Core.World world,
    StorageItemFactory itemFactory,
    ReadOnlyCollection<InvenSlotExtendRow> inventory,
    ReadOnlyCollection<BankSlotExtendRow> bank)
{
    public void GradeUp(Entity player, StorageGroup type)
    {
        var component = world.Get<StorageComponent>(player);
        var storage = component.Collection.First(x => world.Get<StorageGroupComponent>(x).Group == type);
        var grade = world.Get<StorageGradeComponent>(storage);

        var info = GetGradeUpInfo(type, grade.Level + 1);

        var session = world.Get<ServerSessionComponent>(player);
        var content = world.Get<StorageContentComponent>(storage);
        
        switch (info.ConditionType)
        {
            case SlotExtendPaymentType.None:
            {
                break;
            }

            case SlotExtendPaymentType.Item:
            {
                var index = (short)Array.FindIndex(content.SlotList, x => Entity.Null != x && world.Get<StorageItemComponent>(x).Identifier == info.ConditionValue);
                var material = content.SlotList[index];

                var slot = world.Get<StorageSlotComponent>(material);
                var count = slot.Count - 1;
                
                if (count > 0)
                {
                    world.Set(material, slot with { Count = (short)count });
                    
                    session.Send(new ItemReduceResponse(type, index, count));
                }

                else
                {
                    world.Destroy(material);
                    
                    content.SlotList[index] = Entity.Null;
                    
                    session.Send(new ItemBreakResponse(type, index));
                }
                
                break;
            }

            case SlotExtendPaymentType.Gold:
            case SlotExtendPaymentType.Cash:
            default:
            {
                throw new NotImplementedException();
            }
        }
        
        world.Set(storage, grade = new StorageGradeComponent((byte)(grade.Level + 1)));

        var old = content.SlotList;
        var buffer = new Entity[info.TotalSlot];

        Debug.Assert(old.Length <= buffer.Length);

        Array.Copy(old, buffer, old.Length);

        for (var i = old.Length; i < buffer.Length; i++)
        {
            buffer[i] = Entity.Null;
        }

        world.Set(storage, new StorageContentComponent(buffer));

        session.Send(new ItemAddSlotResponse(type, grade.Level, info.TotalSlot));
    }

    private (byte NeedLevel, SlotExtendPaymentType ConditionType, int ConditionValue, byte TotalSlot) GetGradeUpInfo(StorageGroup type, int grade)
    {
        return type switch
        {
            StorageGroup.Common => inventory.First(e => e.Field1 == grade) switch
            {
                var info => (info.Field2, (SlotExtendPaymentType)info.Field3, info.Field4, info.Field6)
            },

            StorageGroup.Costume => inventory.First(e => e.Field1 == grade) switch
            {
                var info => (info.Field12, (SlotExtendPaymentType)info.Field13, info.Field14, info.Field16)
            },

            StorageGroup.Cube => inventory.First(e => e.Field1 == grade) switch
            {
                var info => (info.Field17, (SlotExtendPaymentType)info.Field18, info.Field19, info.Field21)
            },

            StorageGroup.CommonStorage or StorageGroup.AccountCommonStorage => bank.First(e => e.Field1 == grade) switch
            {
                var info => (info.Field2, (SlotExtendPaymentType)info.Field3, info.Field4, info.Field6)
            },

            StorageGroup.CostumeStorage or StorageGroup.AccountFashionStorage => bank.First(e => e.Field1 == grade) switch
            {
                var info => (info.Field7, (SlotExtendPaymentType)info.Field8, info.Field9, info.Field11)
            },

            _ => throw new NotImplementedException()
        };
    }

    public StorageAddResponse TryAdd(Entity player, Entity item, short count)
    {
        var component = world.Get<StorageComponent>(player);
        var storage = component.Collection.First(x => world.Get<StorageGroupComponent>(x).Group == StorageGroup.Common);
        
        var content = world.Get<StorageContentComponent>(storage);

        var index = Array.FindIndex(content.SlotList, x => Entity.Null == x);

        if (index == -1)
        {
            return new StorageAddResponse(false, Entity.Null, []);

        }

        content.SlotList[index] = item;

        return new StorageAddResponse(true, storage, [ new StorageAddInfo(item, (short)index, count) ]);
    }

    public void Break(Entity player, StorageGroup group, short index, int count)
    {
        var component = world.Get<StorageComponent>(player);

        var storage = component.Collection.First(x => world.Get<StorageGroupComponent>(x).Group == group);
        
        var content = world.Get<StorageContentComponent>(storage);
        
        var item = content.SlotList[index];
        
        var slot = world.Get<StorageSlotComponent>(item);
        
        var targetCount = slot.Count - count;

        var session = world.Get<ServerSessionComponent>(player);
        
        // Completely destroy item
        if (targetCount <= 0)
        {
            world.Destroy(item);

            content.SlotList[index] = Entity.Null;

            session.Send(new ItemBreakResponse(group, index));
        }

        // Just reduce count of item
        else
        {
            world.Set(item, slot with { Count = (short)targetCount });
            
            session.Send(new ItemReduceResponse(group, index, targetCount));
        }
    }
    
    public void Divide(Entity player, StorageGroup srcGroup, StorageGroup destGroup, short srcIndex, short destIndex, short count)
    {
        // WTF: Count is not positive
        if (count <= 0)
        {
            return;
        }

        // WTF: Move same item to self
        if (srcGroup == destGroup && srcIndex == destIndex)
        {
            return;
        }
        
        var component = world.Get<StorageComponent>(player);
        
        var srcStorage = component.Collection.First(x => world.Get<StorageGroupComponent>(x).Group == srcGroup);
        
        var srcContent = world.Get<StorageContentComponent>(srcStorage);
        
        var srcItem = srcContent.SlotList[srcIndex];

        // WTF: Source item is not exist
        if (Entity.Null == srcItem)
        {
            return;
        }
        
        var destStorage = srcGroup == destGroup 
            ? srcStorage 
            : component.Collection.First(x => world.Get<StorageGroupComponent>(x).Group == srcGroup);
        
        var destContent = srcGroup == destGroup
            ? srcContent
            : world.Get<StorageContentComponent>(destStorage);
        
        var destItem = destContent.SlotList[destIndex];
        
        var session = world.Get<ServerSessionComponent>(player);
        
        var srcSlot = world.Get<StorageSlotComponent>(srcItem);
        
        var srcTargetCount = srcSlot.Count - count;
        
        // WTF: There must be something left
        if (srcTargetCount <= 0)
        {
            return;
        }

        // WTF: Destination item is existed
        if (Entity.Null != destItem)
        {
            return;
        }

        var identifier = world.Get<StorageItemComponent>(srcItem).Identifier;

        var item = itemFactory.Create(identifier, count);

        destContent.SlotList[destIndex] = item;
        
        world.Set(srcItem, srcSlot with { Count = (short)srcTargetCount });
                    
        session.Send(new ItemDivideResponse
        {
            SrcItem = identifier,
            SrcStorage = srcGroup,
            SrcIndex = srcIndex,
            
            SrcCount = (short)srcTargetCount,
            
            DestStorage = destGroup,
            DestIndex = destIndex,
            DestItem = new ItemValue(world, item)
        });
    }

    public void Move(Entity player, StorageGroup srcGroup, StorageGroup destGroup, short srcIndex, short destIndex)
    {
        var component = world.Get<StorageComponent>(player);
        
        var srcStorage = component.Collection.First(x => world.Get<StorageGroupComponent>(x).Group == srcGroup);
        
        var srcContent = world.Get<StorageContentComponent>(srcStorage);

        var srcItem = srcContent.SlotList[srcIndex];

        // WTF: Source item is not exist
        if (Entity.Null == srcItem)
        {
            return;
        }
        
        var destStorage = srcGroup == destGroup 
            ? srcStorage 
            : component.Collection.First(x => world.Get<StorageGroupComponent>(x).Group == destGroup);
        
        var destContent = srcGroup == destGroup
            ? srcContent
            : world.Get<StorageContentComponent>(destStorage);
        
        var destItem = destContent.SlotList[destIndex];

        srcContent.SlotList[srcIndex] = destItem;
        destContent.SlotList[destIndex] = srcItem;

        var destIdentifier = world.Get<StorageItemComponent>(srcItem).Identifier;
        var srcIdentifier = Entity.Null == destItem ? -1 : world.Get<StorageItemComponent>(destItem).Identifier;
        
        var session = world.Get<ServerSessionComponent>(player);
        
        session.Send(new ItemMoveResponse([
            new ItemMoveInfoValue
            {
                Src = new MoveItemValue
                {  
                    Storage = srcGroup,
                    Item = srcIdentifier,
                    Index = srcIndex
                },
                Dest = new MoveItemValue
                {
                    Storage = destGroup,
                    Item = destIdentifier,
                    Index = destIndex
                }
            }
        ]));
    }

    public void Combine(Entity player, StorageGroup srcGroup, StorageGroup destGroup, short srcIndex, short destIndex, short count)
    {
        // WTF: Count is not positive
        if (count <= 0)
        {
            return;
        }

        // WTF: Move same item to self
        if (srcGroup == destGroup && srcIndex == destIndex)
        {
            return;
        }
        
        var component = world.Get<StorageComponent>(player);
        
        var srcStorage = component.Collection.First(x => world.Get<StorageGroupComponent>(x).Group == srcGroup);
        
        var srcContent = world.Get<StorageContentComponent>(srcStorage);
        
        var srcItem = srcContent.SlotList[srcIndex];

        // WTF: Source item is not exist
        if (Entity.Null == srcItem)
        {
            return;
        }
        
        var destStorage = srcGroup == destGroup 
            ? srcStorage 
            : component.Collection.First(x => world.Get<StorageGroupComponent>(x).Group == srcGroup);
        
        var destContent = srcGroup == destGroup
            ? srcContent
            : world.Get<StorageContentComponent>(destStorage);
        
        var destItem = destContent.SlotList[destIndex];
        
        var srcSlot = world.Get<StorageSlotComponent>(srcItem);

        var srcTargetCount = srcSlot.Count - count;
        
        if (srcTargetCount <= 0)
        {
            srcContent.SlotList[srcIndex] = Entity.Null;
            
            world.Destroy(srcItem);
            
            srcItem = Entity.Null;
        }
        
        var destSlot = world.Get<StorageSlotComponent>(destItem);

        var destTargetCount = destSlot.Count + count;
        
        // WTF: Destination slot is full
        if (destTargetCount > destSlot.Limit)
        {
            return;
        }

        if (srcTargetCount > 0)
        {
            world.Set(srcItem, srcSlot with { Count = (short)srcTargetCount });
        }

        world.Set(destItem, destSlot with { Count = (short)destTargetCount });
        
        var actor = world.Get<ActorComponent>(player);
        
        var session = world.Get<ServerSessionComponent>(player);
        
        session.Send(new ItemCombineResponse
        {
            SrcActor = actor,
            SrcStorage = srcGroup,
            SrcIndex = srcIndex,
            SrcItem = new ItemValue(world, srcItem),
            DestActor = actor,
            DestStorage = destGroup,
            DestIndex = destIndex,
            DestItem = new ItemValue(world, destItem),
        });
    }
}