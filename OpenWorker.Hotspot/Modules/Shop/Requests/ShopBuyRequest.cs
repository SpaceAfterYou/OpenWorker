using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Shop.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct ShopBuyRequest(BinaryReader reader) : IRequestHotspotMessage
{
#region Interface: IHotspotMessage
    
    private const GroupOpcode Group = GroupOpcode.Shop;
    private const ShopOpcode Command = ShopOpcode.Buy;
    
    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage
        
#region Message: Body
    
    public ActorValue Npc { get; } = new(reader);
    public int Index { get; } = reader.ReadInt32();
    public short Count { get; } = reader.ReadInt16();

#endregion Message: Body
}