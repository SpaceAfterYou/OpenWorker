using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.ItemSetup.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct ItemSetupAkashicGetInfoLoadRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.AkashicGetInfoLoad;

    public MessageOpcode Opcode => new(Group, Command);
}