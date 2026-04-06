using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Shop.Types;

namespace OpenWorker.Hotspot.Modules.Shop.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct ShopBannerLoadResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Shop;
    private const ShopOpcode Command = ShopOpcode.BannerLoad;
    
    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public required IReadOnlyCollection<ShopBannerValue> BannerList { get; init; }
    
#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(BannerList.Count);
        
        foreach (var banner in BannerList)
        {
            banner.Write(writer);
        }
    }

#endregion Interface: IWritableData
}