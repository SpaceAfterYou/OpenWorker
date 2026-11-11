using Arch.Core;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Hotspot.Modules.Shop.Enums;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// Gives the rewards of the features that hand out items: into the bags of the person, with its gold, ether and battle
/// points. Experience is not money: the feature that gives it raises the level through the experience of the person.
/// </summary>
public sealed class InventoryRewardGranter(InventoryService inventory) : IRewardGranter
{
    public ValueTask<bool> TryGrantAsync(Entity player, Reward reward, RewardSource source, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(TryGrant(player, reward));
    }

    /// <summary>
    /// Gives all of the reward or none of it.
    /// </summary>
    public bool TryGrant(Entity player, Reward reward)
    {
        if (inventory.InventoryOf(player) is null ||
            !inventory.CanReceive(player, ShopCurrency.Gold, reward.Gold) ||
            !inventory.CanReceive(player, ShopCurrency.Ether, reward.Ether) ||
            !inventory.CanReceive(player, ShopCurrency.Bp, reward.BattlePoints))
        {
            return false;
        }

        var items = reward.Items
            .Where(e => e.Item > 0 && e.Count > 0)
            .Select(e => (e.Item, (int)e.Count))
            .ToList();

        if (!inventory.CanGiveAll(player, items))
        {
            return false;
        }

        foreach (var (item, count) in items)
        {
            inventory.TryGive(player, item, count);
        }

        if (reward.Gold != 0)
        {
            inventory.TryAddCurrency(player, ShopCurrency.Gold, reward.Gold);
        }

        if (reward.Ether != 0)
        {
            inventory.TryAddCurrency(player, ShopCurrency.Ether, reward.Ether);
        }

        if (reward.BattlePoints != 0)
        {
            inventory.TryAddCurrency(player, ShopCurrency.Bp, reward.BattlePoints);
        }

        return true;
    }
}
