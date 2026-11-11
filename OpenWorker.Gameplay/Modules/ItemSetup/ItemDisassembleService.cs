using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.ItemSetup.Responses;
using OpenWorker.Hotspot.Modules.ItemSetup.Types;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Res;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.ItemSetup;

/// <summary>
/// Takes items apart: one with a tool from the bag, or up to twenty at an NPC. An item may be taken apart when its row of
/// <c>tb_Item</c> names a row of the server's <c>tb_Disassemble</c> (<see cref="DisassembleResultRow"/>), which gives an
/// amount of ether between two bounds and, by a roll out of ten thousand, one of its items in a count between two bounds.
/// </summary>
/// <remarks>
/// Runs on the world loop. Everything is rolled and checked before anything is taken, so a refusal changes nothing, and
/// every refusal answers the request. Without the server's table no item can be taken apart.
/// </remarks>
public sealed class ItemDisassembleService(
    InventoryService inventory,
    ReadOnlyCollection<DisassembleResultRow> results,
    ItemSetupAnswers answers,
    Random? random = null,
    QuestManager? quests = null)
{
    /// <summary>The person would hold more ether than a wallet can.</summary>
    public const ushort TooMuchEther = 52058;

    /// <summary>Nothing to take apart, or more items than one request may take apart.</summary>
    public const ushort WrongList = 52270;

    /// <summary>The tool is not in the bag, or the item is not in a bag of items.</summary>
    public const ushort WrongStorage = 52271;

    /// <summary>The tool is not one that takes items apart, or the item cannot be taken apart.</summary>
    public const ushort CannotDisassemble = 52272;

    /// <summary>The bag has no room for what comes out.</summary>
    public const ushort NoRoom = 52273;

    /// <summary>The items one request at an NPC may take apart.</summary>
    public const int MaxTargets = 20;

    /// <summary>The chances are out of this.</summary>
    public const int Chances = 10_000;

    private readonly RowIndex<int, DisassembleResultRow> _results = results.IndexBy(e => e.Id);
    private readonly Random _random = random ?? Random.Shared;

    /// <summary>
    /// Takes one item apart with a tool that lies in the bag; the tool is used up.
    /// </summary>
    public void Disassemble(Entity player, StorageSlot toolSlot, StorageSlot targetSlot)
    {
        const ItemSetupOpcode command = ItemSetupOpcode.Disassemble;

        if (inventory.InventoryOf(player) is not { } bag ||
            bag.At(toolSlot.Storage, toolSlot.Slot) is not { Count: > 0 } tool ||
            bag.At(targetSlot.Storage, targetSlot.Slot) is not { Count: > 0 } target)
        {
            answers.Refuse(player, command, ItemSetupAnswers.WrongSlot);
            return;
        }

        if (tool == target || inventory.Catalog.Classify(tool.Item)?.ItemUseType != ItemUseType.DISASSEMBLE)
        {
            answers.Refuse(player, command, CannotDisassemble);
            return;
        }

        if (tool.Storage != StorageGroup.Common || !InBag(target))
        {
            answers.Refuse(player, command, WrongStorage);
            return;
        }

        if (Roll(target) is not { } result)
        {
            answers.Refuse(player, command, CannotDisassemble);
            return;
        }

        if (!inventory.CanReceive(player, ShopCurrency.Ether, result.Ether))
        {
            answers.Refuse(player, command, TooMuchEther);
            return;
        }

        if (result.Count > 0 && !inventory.CanGiveAll(player, [(result.Result, result.Count)], target))
        {
            answers.Refuse(player, command, NoRoom);
            return;
        }

        var item = target.Item;

        inventory.Spend(player, tool, 1);
        Give(player, target, result);

        answers.Send(player, new ItemSetupDisassembleResponse { Results = [result] });

        quests?.OnEvent(player, QuestConditionType.Disassemble, QuestConditionTarget.Item, item);
    }

    /// <summary>
    /// Takes several items apart at an NPC, without a tool; each gives its own roll.
    /// </summary>
    public void DisassembleMany(Entity player, IReadOnlyList<StorageSlot> targetSlots)
    {
        const ItemSetupOpcode command = ItemSetupOpcode.DisassembleEx;

        if (targetSlots.Count is 0 or > MaxTargets)
        {
            answers.Refuse(player, command, WrongList);
            return;
        }

        if (inventory.InventoryOf(player) is not { } bag)
        {
            answers.Refuse(player, command, ItemSetupAnswers.WrongSlot);
            return;
        }

        var targets = new List<ItemInstance>(targetSlots.Count);

        foreach (var slot in targetSlots)
        {
            // A stack named twice gives one of its items each time, as long as it has them.
            if (bag.At(slot.Storage, slot.Slot) is not { Count: > 0 } target || targets.Count(e => e == target) >= target.Count)
            {
                answers.Refuse(player, command, ItemSetupAnswers.WrongSlot);
                return;
            }

            if (!InBag(target))
            {
                answers.Refuse(player, command, WrongStorage);
                return;
            }

            targets.Add(target);
        }

        var rolled = new ItemSetupDisassembleResult[targets.Count];
        var ether = 0L;

        for (var i = 0; i < targets.Count; i++)
        {
            if (Roll(targets[i]) is not { } result)
            {
                answers.Refuse(player, command, CannotDisassemble);
                return;
            }

            rolled[i] = result;
            ether += result.Ether;
        }

        if (!inventory.CanReceive(player, ShopCurrency.Ether, ether))
        {
            answers.Refuse(player, command, TooMuchEther);
            return;
        }

        // The room is counted as if the items taken apart stayed in their slots: a full bag that their slots would have
        // made room in is refused.
        var given = rolled.Where(e => e.Count > 0).Select(e => (e.Result, (int)e.Count)).ToArray();

        if (given.Length > 0 && !inventory.CanGiveAll(player, given))
        {
            answers.Refuse(player, command, NoRoom);
            return;
        }

        var taken = targets.Select(e => e.Item).ToArray();

        for (var i = 0; i < targets.Count; i++)
        {
            Give(player, targets[i], rolled[i]);
        }

        answers.Send(player, new ItemSetupDisassembleExResponse { Results = rolled });

        // Every item taken apart counts for the quests that ask to take it apart.
        foreach (var item in taken)
        {
            quests?.OnEvent(player, QuestConditionType.Disassemble, QuestConditionTarget.Item, item);
        }
    }

    private static bool InBag(ItemInstance item)
    {
        return item.Storage is StorageGroup.Common or StorageGroup.Costume or StorageGroup.Cash;
    }

    /// <summary>
    /// What one item gives, or <c>null</c> when it cannot be taken apart: no row, or a count the table cannot give.
    /// </summary>
    private ItemSetupDisassembleResult? Roll(ItemInstance target)
    {
        if (!inventory.Catalog.TryGet(target.Item, out var item) || item.Disassemble == 0 ||
            !_results.TryGet(item.Disassemble, out var row))
        {
            return null;
        }

        var ether = Between(row.MinEther, row.MaxEther);
        var roll = _random.Next(1, Chances + 1);

        // The first item whose chance covers what is left of the roll; the chances before it are taken off the roll.
        for (var i = 0; i < DisassembleResultRow.Results; i++)
        {
            if (roll <= row.Rates[i] && row.Items[i] != 0 && row.Rates[i] != 0)
            {
                var count = Between(row.MinCounts[i], row.MaxCounts[i]);

                if (!inventory.Catalog.Exists(row.Items[i]) || count < 1 || count > inventory.Catalog.StackLimit(row.Items[i]))
                {
                    return null;
                }

                return new ItemSetupDisassembleResult { Item = target.Item, Result = row.Items[i], Count = (short)count, Ether = ether };
            }

            roll -= row.Rates[i];
        }

        return new ItemSetupDisassembleResult { Item = target.Item, Ether = ether };
    }

    private int Between(int min, int max)
    {
        return max <= min ? min : _random.Next(min, max + 1);
    }

    private void Give(Entity player, ItemInstance target, ItemSetupDisassembleResult result)
    {
        inventory.Spend(player, target, 1);

        if (result.Count > 0)
        {
            inventory.TryGive(player, result.Result, result.Count);
        }

        if (result.Ether > 0)
        {
            inventory.TryAddCurrency(player, ShopCurrency.Ether, result.Ether);
        }
    }
}
