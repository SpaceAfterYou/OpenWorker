using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Npcs;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.ItemSetup.Responses;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.ItemSetup;

/// <summary>
/// Evolves an item upgraded to level 9 or more into another item at an NPC, by the row of <c>tb_Item_Evolution</c> its row
/// of <c>tb_Item</c> names. An evolution costs gold, battle points, ether and a material, and succeeds by a roll out of ten
/// thousand; then the item gives way to one of the items of the row, by a second roll, which keeps its upgrade level. A
/// failed evolution costs the same and leaves the item as it was.
/// </summary>
/// <remarks>Runs on the world loop. Every refusal answers the request and changes nothing.</remarks>
public sealed class ItemEvolutionService(
    InventoryService inventory,
    ReadOnlyCollection<ItemEvolutionRow> evolutions,
    ItemSetupAnswers answers,
    Random? random = null,
    NpcCreditManager? credit = null)
{
    /// <summary>The item is not there, or the person lacks the material.</summary>
    public const ushort NoItem = 52471;

    /// <summary>The item cannot evolve, or not yet: no row, or an upgrade level below <see cref="MinUpgrade"/>.</summary>
    public const ushort CannotEvolve = 52472;

    /// <summary>The person has less gold, fewer battle points or less ether than the evolution costs.</summary>
    public const ushort NotEnoughMoney = 52473;

    /// <summary>The row gives no item.</summary>
    public const ushort NoResult = 52474;

    /// <summary>The bag has no room for the new item.</summary>
    public const ushort NoRoom = 52475;

    /// <summary>The upgrade level an item needs to evolve.</summary>
    public const int MinUpgrade = 9;

    /// <summary>The chances are out of this.</summary>
    public const int Chances = 10_000;

    private readonly RowIndex<int, ItemEvolutionRow> _evolutions = evolutions.IndexBy(e => e.Id);
    private readonly Random _random = random ?? Random.Shared;

    /// <summary>
    /// Evolves the item at the NPC <paramref name="npc"/>; a try that is paid for, evolved or not, raises the credit with its
    /// group.
    /// </summary>
    public void Evolve(Entity player, StorageSlot targetSlot, ServiceNpc? npc = null)
    {
        const ItemSetupOpcode command = ItemSetupOpcode.Evolution;

        if (inventory.InventoryOf(player) is not { } bag || bag.At(targetSlot.Storage, targetSlot.Slot) is not { Count: > 0 } target)
        {
            answers.Refuse(player, command, NoItem);
            return;
        }

        // The new item goes into the bag, so the item evolves from the bag, not from the equipment.
        if (target.Storage != StorageGroup.Common)
        {
            answers.Refuse(player, command, ItemSetupAnswers.WrongSlot);
            return;
        }

        if (!inventory.Catalog.TryGet(target.Item, out var item) || item.Evolution == 0 || target.Upgrade < MinUpgrade ||
            !_evolutions.TryGet(item.Evolution, out var row))
        {
            answers.Refuse(player, command, CannotEvolve);
            return;
        }

        if (inventory.Gold(player) < row.Gold || inventory.Balance(player, ShopCurrency.Bp) < row.BattlePoint ||
            inventory.Balance(player, ShopCurrency.Ether) < row.Ether)
        {
            answers.Refuse(player, command, NotEnoughMoney);
            return;
        }

        var material = row.Material != 0 && row.MaterialCount > 0;

        if (material && inventory.CountOf(player, row.Material) < row.MaterialCount)
        {
            answers.Refuse(player, command, NoItem);
            return;
        }

        if (_random.Next(1, Chances + 1) > row.SuccessRate)
        {
            Charge(player, row, material);

            answers.Send(player, new ItemSetupEvolutionResponse { Evolved = 0, Serial = 0 });
            Raise(player, npc);
            return;
        }

        if (Pick(row) is not { } result || !inventory.Catalog.Exists(result))
        {
            answers.Refuse(player, command, NoResult);
            return;
        }

        if (!inventory.CanGiveAll(player, [(result, 1)], target))
        {
            answers.Refuse(player, command, NoRoom);
            return;
        }

        var upgrade = target.Upgrade;

        // The new item takes the slot the old one leaves (the room was counted so), and the price is paid once it is there.
        // It comes out of its row as any new item does, bound or not by its own rule: only the upgrade carries over.
        inventory.Spend(player, target, 1);

        // The new item is told to the client once, with the upgrade it keeps.
        if (!inventory.TryGive(player, result, 1, out var changes, notify: false) || bag.At(changes[^1].Storage, changes[^1].Slot) is not { } evolved)
        {
            answers.Refuse(player, command, NoRoom);
            return;
        }

        Charge(player, row, material);

        evolved.Upgrade = upgrade;
        bag.Touch(evolved);

        answers.Send(player, new ItemCreateResponse { List = [InventoryService.ToStorageValue(evolved)] });
        answers.Send(player, new ItemSetupEvolutionResponse { Evolved = 1, Serial = evolved.Serial });
        Raise(player, npc);
    }

    private void Raise(Entity player, ServiceNpc? npc)
    {
        if (credit is not null && npc is { } at)
        {
            credit.Raise(player, at, CreditRaise.Evolution);
        }
    }

    /// <summary>
    /// The item of the row whose chance covers what is left of a roll; the chances before it are taken off the roll.
    /// </summary>
    private int? Pick(ItemEvolutionRow row)
    {
        var roll = _random.Next(1, Chances + 1);

        foreach (var (result, rate) in row.Results)
        {
            if (roll <= rate)
            {
                return result == 0 ? null : result;
            }

            roll -= rate;
        }

        return null;
    }

    private void Charge(Entity player, ItemEvolutionRow row, bool material)
    {
        if (material)
        {
            inventory.TryTake(player, row.Material, row.MaterialCount);
        }

        if (row.Gold > 0)
        {
            inventory.TryAddGold(player, -row.Gold);
        }

        if (row.BattlePoint > 0)
        {
            inventory.TryAddCurrency(player, ShopCurrency.Bp, -row.BattlePoint);
        }

        if (row.Ether > 0)
        {
            inventory.TryAddCurrency(player, ShopCurrency.Ether, -row.Ether);
        }
    }
}
