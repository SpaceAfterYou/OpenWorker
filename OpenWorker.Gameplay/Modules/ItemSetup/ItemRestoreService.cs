using Arch.Core;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.ItemSetup.Responses;
using OpenWorker.Res.Types;

namespace OpenWorker.Gameplay.Modules.ItemSetup;

/// <summary>
/// The tools that work on one item: a restoring item mends a broken item, an unsealing item frees a bound one, and the items
/// that lower a count take back restorations or upgrade tries. Each tool is used up one at a time, and how far an item may go
/// is in its row of <c>tb_Item</c>.
/// </summary>
/// <remarks>Runs on the world loop. Every refusal answers the request, so the dialog of the client never waits.</remarks>
public sealed class ItemRestoreService(InventoryService inventory, ItemSetupAnswers answers)
{
    /// <summary>The flag of an item that broke in an upgrade.</summary>
    public const byte BrokenFlag = 2;

    /// <summary>The unsealing failed: wrong item, not bound, nothing left to unseal, gems in its sockets.</summary>
    public const ushort UnsealFailed = 52510;

    /// <summary>The tool cannot be used on the item.</summary>
    public const ushort UseEffectFailed = 52521;

    /// <summary>A limit of unsealing that the table gives to items that may be unsealed without end.</summary>
    private const byte EndlessSeals = 255;

    /// <summary>
    /// Mends a broken item with a restoring item, as many times as its row allows.
    /// </summary>
    public void Restore(Entity player, StorageSlot toolSlot, StorageSlot targetSlot)
    {
        const ItemSetupOpcode command = ItemSetupOpcode.Restore;

        if (inventory.InventoryOf(player) is not { } bag ||
            bag.At(toolSlot.Storage, toolSlot.Slot) is not { Count: > 0 } tool ||
            bag.At(targetSlot.Storage, targetSlot.Slot) is not { } target ||
            tool == target)
        {
            answers.Refuse(player, command, ItemSetupAnswers.WrongSlot);
            return;
        }

        if (inventory.Catalog.Classify(tool.Item)?.ItemUseType != ItemUseType.WEAPON_RESTORE ||
            !inventory.Catalog.TryGet(target.Item, out var row) ||
            target.Flag != BrokenFlag ||
            target.RestoreCount >= row.RestoreLimit)
        {
            answers.Refuse(player, command, ItemSetupAnswers.WrongSlot);
            return;
        }

        target.Flag = 0;
        target.RestoreCount++;
        bag.Touch(target);
        inventory.Spend(player, tool, 1);

        answers.Send(player, new ItemSetupRestoreResponse { Serial = target.Serial, RestoreCount = target.RestoreCount });
    }

    /// <summary>
    /// Frees a bound item with an unsealing item, as many times as its row allows. An item with gems in its sockets stays bound.
    /// </summary>
    public void Unseal(Entity player, StorageSlot first, StorageSlot second)
    {
        const ItemSetupOpcode command = ItemSetupOpcode.Unseal;

        if (inventory.InventoryOf(player) is not { } bag ||
            bag.At(first.Storage, first.Slot) is not { } one ||
            bag.At(second.Storage, second.Slot) is not { } other ||
            one == other)
        {
            answers.Refuse(player, command, UnsealFailed);
            return;
        }

        // The tool comes first; a client that sends the item first is understood as well.
        var (tool, target) = IsUnsealing(one) || !IsUnsealing(other) ? (one, other) : (other, one);

        if (!IsUnsealing(tool) || tool.Count < 1 || !inventory.Catalog.TryGet(target.Item, out var row) ||
            ItemBinding.OnUnsealed(target.BindState) is not { } freed || target.Sockets.Any(e => !e.IsEmpty))
        {
            answers.Refuse(player, command, UnsealFailed);
            return;
        }

        if (row.SealLimit != EndlessSeals && target.SealCount >= row.SealLimit)
        {
            answers.Refuse(player, command, UnsealFailed);
            return;
        }

        target.BindState = freed;

        if (row.SealLimit != EndlessSeals)
        {
            target.SealCount++;
        }

        bag.Touch(target);
        inventory.Spend(player, tool, 1);

        answers.Send(player, new ItemSetupUnsealResponse { Serial = target.Serial, SealCount = target.SealCount });
    }

    /// <summary>
    /// Uses an item that lowers a count of another: the restorations or the upgrade tries, by the value of its row.
    /// </summary>
    /// <remarks>
    /// The item that lowers the count of unsealing may be used once per item, which the items do not keep yet: it is refused.
    /// </remarks>
    public void UseEffect(Entity player, StorageSlot toolSlot, StorageSlot targetSlot)
    {
        const ItemSetupOpcode command = ItemSetupOpcode.UseEffect;

        if (inventory.InventoryOf(player) is not { } bag ||
            bag.At(toolSlot.Storage, toolSlot.Slot) is not { Count: > 0 } tool ||
            bag.At(targetSlot.Storage, targetSlot.Slot) is not { } target ||
            tool == target ||
            !inventory.Catalog.TryGet(tool.Item, out var toolRow) ||
            toolRow.UseValue == 0)
        {
            answers.Refuse(player, command, UseEffectFailed);
            return;
        }

        switch (inventory.Catalog.Classify(tool.Item)?.ItemUseType)
        {
            case ItemUseType.RESTORE_COUNT_DEC:
            {
                target.RestoreCount = (byte)Math.Max(0, target.RestoreCount - toolRow.UseValue);
                break;
            }

            case ItemUseType.UPGRADE_COUNT_DEC:
            {
                target.UpgradeCount = (byte)Math.Max(0, target.UpgradeCount - toolRow.UseValue);
                break;
            }

            default:
            {
                answers.Refuse(player, command, UseEffectFailed);
                return;
            }
        }

        bag.Touch(target);
        inventory.Spend(player, tool, 1);

        answers.Send(player, new ItemUpdateResponse { List = [InventoryService.ToStorageValue(target)] });
        answers.Send(player, new ItemSetupUseEffectResponse());
    }

    private bool IsUnsealing(ItemInstance item)
    {
        return inventory.Catalog.Classify(item.Item)?.ItemUseType == ItemUseType.UNSEAL;
    }
}
