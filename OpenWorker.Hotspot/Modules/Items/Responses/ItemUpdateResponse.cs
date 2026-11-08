using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Extensions;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

/// <summary>
/// Items that stay where they are and have changed: a count, a level of upgrade or the equipped item.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct ItemUpdateResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.Update;

    public MessageOpcode Opcode => new(Group, Command);

    public required StorageItemValue[] List { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(List.Length);

        foreach (var item in List)
        {
            writer.Write(item);
        }
    }
}
