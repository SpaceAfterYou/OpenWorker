using Arch.Core;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Components;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Types;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

public readonly record struct StorageItemValue(StorageGroup Group, short Index, ItemValue Item);

[HotspotMessage(Group, Command)]
public readonly struct ItemInventoryInfoResponse(StorageItemValue[] list) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.InventoryInfo;

    public MessageOpcode Opcode => new(Group, Command);

    public static ItemInventoryInfoResponse Create(Arch.Core.World world, Entity entity, IReadOnlyCollection<StorageGroup> values)
    {
        var component = world.Get<StorageComponent>(entity);
        
        var list = component.Collection
            .Where(x => values.Contains(world.Get<StorageGroupComponent>(x).Group))
            .SelectMany(x =>
            {
                var group = world.Get<StorageGroupComponent>(x).Group;
                var list = world.Get<StorageContentComponent>(x).SlotList;

                return list
                    .Select((item, index) => new { Item = item, Index = index })
                    .Where(e => Entity.Null != e.Item)
                    .Select(e => new StorageItemValue(group, (short)e.Index, new ItemValue(world, e.Item)));
            })
            .ToArray();
        
        return new ItemInventoryInfoResponse(list);
    }
    
    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(list.Length);
        
        foreach (var item in list)
        {
            writer.Write(item);
        }
    }
}