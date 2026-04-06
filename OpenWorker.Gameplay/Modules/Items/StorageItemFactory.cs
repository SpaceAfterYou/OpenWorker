using System.Collections;
using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Gameplay.Modules.Items.Components;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Items;

public sealed class StorageItemFactory(
    Arch.Core.World world,
    ReadOnlyCollection<RandomOptionRow> randomOptionList,
    ReadOnlyCollection<ItemRow> itemList,
    ReadOnlyCollection<ItemClassifyRow> classifyList, 
    ReadOnlyCollection<SocketRow> socketList, 
    ReadOnlyCollection<CostumeSocketRow> costumeSocketList,
    ReadOnlyCollection<ReinforceRow> reinforceList)
{
    private long Identifier { get; set; }

    private StorageItemArchetype[] Archetypes { get; } = [];
    
    public Entity Create(int prototype, short count)
    {
        var proto = itemList.First(x => x.Id == prototype);
        var classify = classifyList.First(x => x.Id == proto.Classify);

        var identifier = new BitArray((int)StorageItemBit.Max);
        
        if (proto.Socket > 0)
        {
            identifier.Set((int)StorageItemBit.HasSocket, true);
        }
        
        if (proto.Reinforce > 0)
        {
            identifier.Set((int)StorageItemBit.HasReinforce, true);
        }
        
        // var archetype = Archetypes.First(x => x.Identifier == identifier);
        
        // var item = world.Create(archetype);
        var item = world.Create();
        
        if (proto.Socket > 0)
        {
            switch (classify.ItemUseType)
            {
                case ItemUseType.ABILITY:
                {
                    var socket = socketList.First(x => x.Id == proto.Socket);
                
                    // gear
                    break;
                }
                
                case ItemUseType.SHAPE:
                {
                    var socket = costumeSocketList.First(x => x.Id == proto.Socket);
                
                    // clothes
                    break;
                }
            }
        }

        if (proto.Reinforce > 0)
        {
            var row = reinforceList.First(x => x.Id == proto.Reinforce);
            
            world.Add(item, new StorageItemReinforceComponent
            {
                MaxLevel = row.MaxLevel,
                TryCount = row.TryCount,
                MaxTryCount = row.MaxTryCount,
            });
        }
        
        world.Add(item, new StorageItemComponent(prototype));
        world.Add(item, new StorageItemSerialComponent(Identifier++));
        world.Add(item, new StorageSlotComponent(count, proto.Stack));

        var optionRow = randomOptionList.FirstOrDefault(x => x.Id == proto.Option);
        if (optionRow.Id != 0)
        {
            var options = Enumerable
                .Range(0, ItemModuleDefines.OptionCount)
                .Select(_ => new StorageItemOptionValue())
                .ToArray();

            // Stats types
            var types = new short[]{ 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F, 0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F, 0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F, 0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A };
            
            for (int i = 0, end = Math.Min(options.Length, Random.Shared.Next(optionRow.Min, optionRow.Max)); i < end; i++)
            {
                options[i] = new StorageItemOptionValue(types[Random.Shared.NextInt64(0, types.Length)], 10 + i);
            }
            
            world.Add(item, new StorageItemOptionComponent(options));
        }

        return item;
    }
}