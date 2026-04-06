using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.ItemSetup.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct ItemSetupRepairEquipRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.RepairEquip;

    public int Npc { get; } = reader.ReadInt32();
    
    public MessageOpcode Opcode => new(Group, Command);
}