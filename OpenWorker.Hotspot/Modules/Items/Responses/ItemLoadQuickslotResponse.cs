using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

/// <summary>
/// The quick slots of the person: five skill cards, two items and one more number the client keeps.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct ItemLoadQuickslotResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.LoadQuickSlot;

    public MessageOpcode Opcode => new(Group, Command);

    public required int[] Cards { get; init; }
    public required int[] Items { get; init; }

    public void Write(BinaryWriter writer)
    {
        for (var i = 0; i < 5; i++)
        {
            writer.Write(i < Cards.Length ? Cards[i] : 0);
        }

        for (var i = 0; i < 2; i++)
        {
            writer.Write(i < Items.Length ? Items[i] : 0);
        }

        writer.Write(0);
    }
}
