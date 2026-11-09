using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.ItemSetup.Requests;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct ItemSetupAkashicGetInfoAddRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.AkashicGetInfoAdd;

    public MessageOpcode Opcode => new(Group, (byte)Command);
}