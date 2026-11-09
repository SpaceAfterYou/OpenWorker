using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Npcs;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.ItemSetup.Enums;
using OpenWorker.Hotspot.Modules.ItemSetup.Responses;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Res;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.ItemSetup;

/// <summary>
/// Upgrades an item one level at an NPC. What it costs is the row of the item in the client's <c>tb_Reinforce</c>, at the
/// level the item is at; whether it goes up, stays or breaks is the row of the same id in the server's <c>tb_Reinforce</c>
/// (<see cref="ReinforceChanceRow"/>), out of ten thousand. From level 4 an item that protects the upgraded one may be
/// used up instead of the item breaking.
/// </summary>
/// <remarks>
/// Runs on the world loop. Every refusal answers the request, so the dialog of the client never waits. Without the server's
/// table (not in the server tables folder) every upgrade is refused.
/// </remarks>
public sealed class ItemUpgradeService(
    InventoryService inventory,
    ReadOnlyCollection<ReinforceRow> costs,
    ReadOnlyCollection<ReinforceChanceRow> chances,
    ItemSetupAnswers answers,
    Random? random = null,
    QuestManager? quests = null,
    NpcCreditManager? credit = null)
{
    /// <summary>The item cannot be upgraded further, or with that protection.</summary>
    public const ushort CannotUpgrade = 52201;

    /// <summary>The person has less gold than the upgrade costs.</summary>
    public const ushort NotEnoughGold = 52210;

    /// <summary>The item or the protection is wrong, the item has no tries left, or the tables have no row for it.</summary>
    public const ushort WrongItem = 52219;

    /// <summary>The person has fewer battle points than the upgrade costs.</summary>
    public const ushort NotEnoughBattlePoints = 52220;

    /// <summary>The person lacks the materials of the upgrade.</summary>
    public const ushort NotEnoughMaterials = 52223;

    /// <summary>The person has less ether than the upgrade costs.</summary>
    public const ushort NotEnoughEther = 52224;

    /// <summary>The level from which an item that protects the upgraded one may be used.</summary>
    public const int ProtectedFrom = 4;

    /// <summary>The chances are out of this.</summary>
    public const int Chances = 10_000;

    private readonly RowIndex<int, ReinforceRow> _costs = costs.IndexBy(e => e.Id);
    private readonly RowIndex<int, ReinforceChanceRow> _chances = chances.IndexBy(e => e.Id);
    private readonly Random _random = random ?? Random.Shared;

    /// <summary>
    /// Upgrades the item at the NPC <paramref name="npc"/>: the credit with its group takes a part off the gold and adds to
    /// the chance, and the try raises that credit.
    /// </summary>
    public void Upgrade(Entity player, bool protect, StorageSlot protectionSlot, StorageSlot targetSlot, ServiceNpc? npc = null)
    {
        const ItemSetupOpcode command = ItemSetupOpcode.Upgrade;

        if (inventory.InventoryOf(player) is not { } bag || bag.At(targetSlot.Storage, targetSlot.Slot) is not { } target ||
            !inventory.Catalog.TryGet(target.Item, out var item))
        {
            answers.Refuse(player, command, WrongItem);
            return;
        }

        if (target.Flag != 0)
        {
            answers.Refuse(player, command, CannotUpgrade);
            return;
        }

        if (item.Reinforce == 0 || !_costs.TryGet(item.Reinforce, out var row) || !_chances.TryGet(item.Reinforce, out var chance) ||
            row.Cost(target.Upgrade) is not { } cost)
        {
            answers.Refuse(player, command, WrongItem);
            return;
        }

        var level = target.Upgrade;

        if (level >= row.MaxLevel)
        {
            answers.Refuse(player, command, CannotUpgrade);
            return;
        }

        if (credit is not null && npc is { } at)
        {
            cost = cost with { Gold = (int)credit.Discount(player, at, CreditBenefit.UpgradeMoney, cost.Gold) };
        }

        if (inventory.Gold(player) < cost.Gold)
        {
            answers.Refuse(player, command, NotEnoughGold);
            return;
        }

        if (inventory.Balance(player, ShopCurrency.Bp) < cost.BattlePoint)
        {
            answers.Refuse(player, command, NotEnoughBattlePoints);
            return;
        }

        if (inventory.Balance(player, ShopCurrency.Ether) < cost.Ether)
        {
            answers.Refuse(player, command, NotEnoughEther);
            return;
        }

        if (!HasMaterial(player, cost.Material, cost.MaterialCount, cost.ExtraMaterial, cost.ExtraMaterialCount))
        {
            answers.Refuse(player, command, NotEnoughMaterials);
            return;
        }

        ItemInstance? protection = null;

        if (protect)
        {
            if (level < ProtectedFrom)
            {
                answers.Refuse(player, command, CannotUpgrade);
                return;
            }

            protection = bag.At(protectionSlot.Storage, protectionSlot.Slot);

            if (protection is not { Count: > 0 } || protection == target ||
                inventory.Catalog.Classify(protection.Item)?.ItemUseType != ItemUseType.UPGRADE_HELP)
            {
                answers.Refuse(player, command, WrongItem);
                return;
            }
        }

        // An item with a limit of tries spends one on every upgrade, whatever comes of it.
        var limited = target.UpgradeLimit != 0;

        if (limited && target.UpgradeLimit < target.UpgradeCount + 1)
        {
            answers.Refuse(player, command, WrongItem);
            return;
        }

        Charge(player, cost);

        if (protection is not null)
        {
            inventory.Spend(player, protection, 1);
        }

        if (limited)
        {
            target.UpgradeCount++;
        }

        var roll = _random.Next(1, Chances + 1);
        var up = chance.Up[level] + (credit is not null && npc is { } near ? credit.UpgradeBonus(player, near) : 0);
        var breaks = chance.Break[level];
        ItemSetupUpgradeResult result;

        if (roll <= up)
        {
            target.Upgrade++;
            result = ItemSetupUpgradeResult.Success;
        }
        else if (roll > up + breaks)
        {
            result = ItemSetupUpgradeResult.Failure;
        }
        else if (protection is not null)
        {
            result = ItemSetupUpgradeResult.ProtectionBroken;
        }
        else
        {
            target.Flag = ItemRestoreService.BrokenFlag;
            result = ItemSetupUpgradeResult.Broken;
        }

        bag.Touch(target);

        answers.Send(player, new ItemSetupUpgradeResponse { Result = result, Item = target.ToValue() });

        // Every try counts for the quests that ask to upgrade the item, whatever came of it, and raises the credit.
        quests?.OnEvent(player, QuestConditionType.Upgrade, QuestConditionTarget.Item, target.Item);

        if (credit is not null && npc is { } served)
        {
            credit.Raise(player, served, CreditRaise.Upgrade);
        }
    }

    private bool HasMaterial(Entity player, int material, short count, int extra, short extraCount)
    {
        if (material == extra && material != 0)
        {
            return inventory.CountOf(player, material) >= Math.Max((short)0, count) + Math.Max((short)0, extraCount);
        }

        return (material == 0 || count <= 0 || inventory.CountOf(player, material) >= count) &&
               (extra == 0 || extraCount <= 0 || inventory.CountOf(player, extra) >= extraCount);
    }

    private void Charge(Entity player, ReinforceCost cost)
    {
        if (cost.Gold > 0)
        {
            inventory.TryAddGold(player, -cost.Gold);
        }

        if (cost.BattlePoint > 0)
        {
            inventory.TryAddCurrency(player, ShopCurrency.Bp, -cost.BattlePoint);
        }

        if (cost.Ether > 0)
        {
            inventory.TryAddCurrency(player, ShopCurrency.Ether, -cost.Ether);
        }

        if (cost.Material != 0 && cost.MaterialCount > 0)
        {
            inventory.TryTake(player, cost.Material, cost.MaterialCount);
        }

        if (cost.ExtraMaterial != 0 && cost.ExtraMaterialCount > 0)
        {
            inventory.TryTake(player, cost.ExtraMaterial, cost.ExtraMaterialCount);
        }
    }
}
