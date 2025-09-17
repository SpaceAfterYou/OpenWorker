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

[HotspotMessage(Group, Command)]
public readonly struct ItemCreateResponse(StorageItemValue[] list) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.Create;

    public MessageOpcode Opcode => new(Group, Command);

    public static ItemCreateResponse Create(Arch.Core.World world, Entity storage, short[] indexList)
    {
        var component = world.Get<StorageContentComponent>(storage);
        var group = world.Get<StorageGroupComponent>(storage).Group;
        
        var list = indexList
            .Select(index => new StorageItemValue(group, index, new ItemValue(world, component.SlotList[index])))
            .ToArray();
        
        return new ItemCreateResponse(list);
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