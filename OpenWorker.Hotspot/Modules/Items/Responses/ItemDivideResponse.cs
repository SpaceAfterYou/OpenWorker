using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Types;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

[HotspotMessage(Group, Command)]
public readonly record struct ItemDivideResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.Divide;

    public MessageOpcode Opcode => new(Group, Command);

    public required int SrcItem { get; init; }
    public required StorageGroup SrcStorage { get; init; }
    public required short SrcIndex { get; init; }
    public required short SrcCount { get; init; }
    public required StorageGroup DestStorage { get; init; }
    public required short DestIndex { get; init; }
    public required ItemValue DestItem { get; init; }
    
    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(SrcItem);
        writer.Write(SrcStorage);
        writer.Write(SrcIndex);
        writer.Write(SrcCount);
        writer.Write(DestStorage);
        writer.Write(DestIndex);
        writer.Write(DestItem);
    }
}