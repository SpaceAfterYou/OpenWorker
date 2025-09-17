using Arch.Core;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Components;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Extensions;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

[HotspotMessage(Group, Command)]
public readonly struct ItemOpenSlotInfoResponse(Arch.Core.World world, Entity entity, IReadOnlyCollection<StorageGroup> values) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.OpenSlotInfo;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        var component = world.Get<StorageComponent>(entity);
        
        writer.Write((byte)values.Count);
        
        foreach (var group in values)
        {
            writer.Write(group);

            foreach (var storage in component.Collection)
            {
                if ((world.Get<StorageGroupComponent>(storage).Group == group) is false)
                {
                    continue;
                }
                
                var content = world.Get<StorageContentComponent>(storage);
            
                writer.Write((short)content.SlotList.Length);

                if (world.Has<StorageGradeComponent>(storage))
                {
                    var grade = world.Get<StorageGradeComponent>(storage);

                    writer.Write(grade.Level);
                }

                else
                {
                    writer.Write(byte.MinValue);
                }

                break;
            }
        }
    }
}