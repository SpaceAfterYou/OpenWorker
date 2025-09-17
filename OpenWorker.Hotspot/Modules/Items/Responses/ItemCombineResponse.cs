using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

[HotspotMessage(Group, Command)]
public readonly struct ItemCombineResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.Combine;

    public MessageOpcode Opcode => new(Group, Command);

    public required ActorValue SrcActor { get; init; }
    public required StorageGroup SrcStorage { get; init; }
    public required short SrcIndex { get; init; }
    public required ItemValue SrcItem { get; init; }
    public required ActorValue DestActor { get; init; }
    public required StorageGroup DestStorage { get; init; }
    public required short DestIndex { get; init; }
    public required ItemValue DestItem { get; init; }

    private const bool SyncToClient = false;
    
    public void ToBinary(BinaryWriter writer)
    {
        writer.WriteActor(SrcActor);
        writer.Write(SrcStorage);
        writer.Write(SrcIndex);
        writer.Write(SrcItem);
        writer.WriteActor(DestActor);
        writer.Write(DestStorage);
        writer.Write(DestIndex);
        writer.Write(DestItem);
        writer.Write(SyncToClient);
    }
}