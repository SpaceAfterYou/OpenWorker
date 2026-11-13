using Arch.Core;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.DistrictServer.Trade;

/// <summary>
/// The items of a person that are offered in its trade or in its private shop. The shops are taken when first asked: they
/// reach the item locks back through the audience of the channel.
/// </summary>
public sealed class TradeItemLocks(Trades trades, Lazy<PrivateShops> shops) : IItemLocks
{
    public bool IsLocked(Entity player, StorageGroup storage, short slot)
    {
        return trades.SessionOf(player)?.SideOf(player).IndexOf(storage, slot) >= 0 || shops.Value.ShopOf(player)?.IndexOf(storage, slot) >= 0;
    }
}
