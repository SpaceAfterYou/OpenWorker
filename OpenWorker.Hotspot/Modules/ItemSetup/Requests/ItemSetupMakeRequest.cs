using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.ItemSetup.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct ItemSetupMakeRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.Make;

    public int Npc { get; } = reader.ReadInt32();
    public int MakeIndex { get; } = reader.ReadInt32();
    public short Unknown { get; } = reader.ReadInt16();
    public byte MakeCount { get; } = reader.ReadByte();
    
    public MessageOpcode Opcode => new(Group, Command);
}